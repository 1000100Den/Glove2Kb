"""Tkinter GUI for Glove2Kb-Py.

Layout:

* Connection bar: VMC port field + Connect/Disconnect (UDP listener).
  When disconnected, the simulator transport drives the engine instead.
* Grid view: the 3x3 key groups of the current page; the selected group
  is highlighted and the candidate key inside it is marked.
* Text output: the committed text buffer.
* Status line: engine state, current selection, last committed key.
* Simulator panel: sliders for both wrist rolls and five finger
  flexions, plus quick-gesture buttons (Grip / Release / Neutral /
  Key 1-4 combos), so the full flow -- grip, rotate, combo, release or
  return to neutral to commit -- works with no hardware at all.

This module is the only one that imports tkinter; the core logic never
does, so tests run headless.
"""

from __future__ import annotations

import tkinter as tk
from tkinter import messagebox
from typing import Dict, Optional, Tuple

from . import layout
from .engine import InputEngine
from .gestures import GestureFrame
from .simulator import PRESETS, SimulatorTransport
from .vmc_receiver import DEFAULT_PORT, VmcReceiver, validate_port

POLL_MS = 40

_GROUP_BG = "#dde7f5"
_KEY_BG = "#f4f4f4"
_KEY_ACTIVE_BG = "#ffd54d"


class Glove2KbApp:
    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        root.title("Glove2Kb-Py")

        self.engine = InputEngine()
        self.simulator = SimulatorTransport()
        self.receiver: Optional[VmcReceiver] = None

        self._group_frames: Dict[Tuple[int, int], tk.Frame] = {}
        self._key_labels: Dict[Tuple[int, int, int], tk.Label] = {}
        self._page_shown: Optional[str] = None

        self._build_connection_bar(root)
        self._build_grid(root)
        self._build_output(root)
        self._build_simulator(root)
        self.status = tk.Label(root, text="Simulator mode -- grip to start",
                               anchor="w")
        self.status.pack(fill="x", padx=6, pady=4)

        self._poll()

    # -- construction ------------------------------------------------------

    def _build_connection_bar(self, root: tk.Tk) -> None:
        bar = tk.Frame(root)
        bar.pack(fill="x", padx=6, pady=4)
        tk.Label(bar, text="VMC port:").pack(side="left")
        self.port_var = tk.StringVar(value=str(DEFAULT_PORT))
        tk.Entry(bar, textvariable=self.port_var, width=7).pack(side="left")
        self.connect_btn = tk.Button(bar, text="Connect",
                                     command=self._toggle_connection)
        self.connect_btn.pack(side="left", padx=4)
        self.conn_label = tk.Label(bar, text="not connected (simulator)")
        self.conn_label.pack(side="left")

    def _build_grid(self, root: tk.Tk) -> None:
        self.grid_frame = tk.Frame(root)
        self.grid_frame.pack(padx=6, pady=4)

    def _render_grid(self, page: str) -> None:
        for child in self.grid_frame.winfo_children():
            child.destroy()
        self._group_frames.clear()
        self._key_labels.clear()
        for row in range(layout.GRID_ROWS):
            for col in range(layout.GRID_COLS):
                frame = tk.Frame(self.grid_frame, bd=2, relief="groove",
                                 padx=4, pady=4)
                frame.grid(row=row, column=col, padx=3, pady=3)
                self._group_frames[(row, col)] = frame
                group = layout.get_group(page, row, col)
                for idx in range(layout.KEYS_PER_GROUP):
                    text = group[idx].label if idx < len(group) else ""
                    label = tk.Label(frame, text=text, width=6,
                                     bg=_KEY_BG, relief="ridge")
                    label.grid(row=idx // 2, column=idx % 2, padx=1, pady=1)
                    self._key_labels[(row, col, idx)] = label
        self._page_shown = page

    def _build_output(self, root: tk.Tk) -> None:
        tk.Label(root, text="Output:").pack(anchor="w", padx=6)
        self.output = tk.Text(root, height=5, width=60)
        self.output.pack(fill="x", padx=6)

    def _build_simulator(self, root: tk.Tk) -> None:
        panel = tk.LabelFrame(root, text="Simulator (used while not connected)")
        panel.pack(fill="x", padx=6, pady=4)
        self.sliders: Dict[str, tk.DoubleVar] = {}

        def add_slider(name: str, label: str, from_: float, to: float,
                       row: int) -> None:
            var = tk.DoubleVar(value=0.0)
            self.sliders[name] = var
            tk.Label(panel, text=label, width=14, anchor="w").grid(
                row=row, column=0, sticky="w")
            tk.Scale(panel, variable=var, from_=from_, to=to,
                     orient="horizontal", length=260,
                     resolution=1 if to > 1 else 0.05).grid(row=row, column=1)

        add_slider("left_roll_deg", "Left wrist roll", -90, 90, 0)
        add_slider("right_roll_deg", "Right wrist roll", -90, 90, 1)
        add_slider("thumb", "Thumb", 0, 1, 2)
        add_slider("index", "Index", 0, 1, 3)
        add_slider("middle", "Middle (grip)", 0, 1, 4)
        add_slider("ring", "Ring (grip)", 0, 1, 5)
        add_slider("little", "Little (grip)", 0, 1, 6)

        buttons = tk.Frame(panel)
        buttons.grid(row=7, column=0, columnspan=2, pady=4)
        for name, label in (("neutral", "Neutral"), ("grip", "Grip"),
                            ("release", "Release"), ("key1", "Key 1"),
                            ("key2", "Key 2"), ("key3", "Key 3"),
                            ("key4", "Key 4")):
            tk.Button(buttons, text=label,
                      command=lambda n=name: self._apply_preset(n)).pack(
                          side="left", padx=2)

    # -- behaviour -----------------------------------------------------------

    def _apply_preset(self, name: str) -> None:
        self.simulator.apply_preset(name)
        frame = self.simulator.get_frame()
        for field, var in self.sliders.items():
            var.set(getattr(frame, field))

    def _toggle_connection(self) -> None:
        if self.receiver is not None:
            self.receiver.stop()
            self.receiver = None
            self.connect_btn.config(text="Connect")
            self.conn_label.config(text="not connected (simulator)")
            return
        try:
            port = validate_port(int(self.port_var.get()))
        except (ValueError, TypeError):
            messagebox.showerror("Invalid port",
                                 "Port must be an integer in 1..65535.")
            return
        receiver = VmcReceiver(port=port)
        try:
            receiver.start()
        except OSError as exc:
            messagebox.showerror("Connect failed", str(exc))
            return
        self.receiver = receiver
        self.connect_btn.config(text="Disconnect")
        self.conn_label.config(text=f"listening on 127.0.0.1:{port} (VMC/UDP)")

    def _current_frame(self) -> GestureFrame:
        if self.receiver is not None:
            frame = self.receiver.get_frame()
            return frame if frame is not None else GestureFrame()
        frame = GestureFrame(**{name: var.get()
                                for name, var in self.sliders.items()})
        self.simulator.set_frame(frame)
        return frame

    def _poll(self) -> None:
        frame = self._current_frame()
        committed = self.engine.process_frame(frame)
        self._refresh(frame, committed)
        self.root.after(POLL_MS, self._poll)

    def _refresh(self, frame: GestureFrame, committed) -> None:
        if self._page_shown != self.engine.page:
            self._render_grid(self.engine.page)
        selection = self.engine.current_selection
        candidate = self.engine.candidate_selection
        active = selection if selection is not None else candidate

        for (row, col), group_frame in self._group_frames.items():
            is_group = active is not None and active[1] == row and active[2] == col
            group_frame.config(bg=_GROUP_BG if is_group else
                               self.root.cget("bg"))
        for (row, col, idx), label in self._key_labels.items():
            is_key = (selection is not None and selection[1] == row
                      and selection[2] == col and selection[3] == idx)
            label.config(bg=_KEY_ACTIVE_BG if is_key else _KEY_BG)

        text = self.engine.text
        if self.output.get("1.0", "end-1c") != text:
            self.output.delete("1.0", "end")
            self.output.insert("1.0", text)

        key = self.engine.current_key()
        key_desc = key.label if key is not None else "-"
        mode = "VMC" if self.receiver is not None else "Simulator"
        last = f" | committed: {committed[-1].label}" if committed else ""
        self.status.config(
            text=f"[{mode}] state={self.engine.state.value} "
                 f"page={self.engine.page} key={key_desc}{last}")


def main() -> None:
    root = tk.Tk()
    Glove2KbApp(root)
    root.mainloop()
