"""Simulator transport: GestureFrames without hardware.

Two transports share the receiver's interface (``get_frame()``):

* :class:`SimulatorTransport` -- a mutable frame whose values are driven
  by the GUI sliders/buttons (or by test code).
* :class:`ScriptedTransport` -- replays a fixed list of frames, one per
  ``get_frame()`` call, then keeps returning the last one.  Handy for
  headless smoke tests of the engine.
"""

from __future__ import annotations

import dataclasses
from typing import Dict, List, Optional

from .gestures import GestureFrame

# Quick-gesture presets for the simulator panel.  Values are partial
# GestureFrame fields; "grip" bends the three grip fingers, combos set
# thumb/index on top of an active grip.
PRESETS: Dict[str, Dict[str, float]] = {
    "neutral": {
        "left_roll_deg": 0.0, "right_roll_deg": 0.0,
        "thumb": 0.0, "index": 0.0, "middle": 0.0, "ring": 0.0,
        "little": 0.0,
    },
    "grip": {"middle": 1.0, "ring": 1.0, "little": 1.0},
    "release": {"middle": 0.0, "ring": 0.0, "little": 0.0,
                "thumb": 0.0, "index": 0.0},
    "key1": {"middle": 1.0, "ring": 1.0, "little": 1.0,
             "thumb": 0.0, "index": 0.0},
    "key2": {"middle": 1.0, "ring": 1.0, "little": 1.0,
             "thumb": 0.0, "index": 1.0},
    "key3": {"middle": 1.0, "ring": 1.0, "little": 1.0,
             "thumb": 1.0, "index": 0.0},
    "key4": {"middle": 1.0, "ring": 1.0, "little": 1.0,
             "thumb": 1.0, "index": 1.0},
}


class SimulatorTransport:
    """A frame source controlled programmatically (GUI sliders)."""

    def __init__(self, frame: Optional[GestureFrame] = None) -> None:
        self._frame = frame if frame is not None else GestureFrame()

    def set_frame(self, frame: GestureFrame) -> None:
        self._frame = frame

    def update(self, **fields: float) -> GestureFrame:
        """Set individual fields, e.g. ``update(thumb=1.0)``."""
        self._frame = dataclasses.replace(self._frame, **fields)
        return self._frame

    def apply_preset(self, name: str) -> GestureFrame:
        """Apply one of the PRESETS by name."""
        if name not in PRESETS:
            raise ValueError(f"unknown preset: {name!r}")
        return self.update(**PRESETS[name])

    def get_frame(self) -> GestureFrame:
        return dataclasses.replace(self._frame)


class ScriptedTransport:
    """Replays a fixed sequence of frames, one per call."""

    def __init__(self, frames: List[GestureFrame]) -> None:
        if not frames:
            raise ValueError("frames must not be empty")
        self._frames = list(frames)
        self._index = 0

    def get_frame(self) -> GestureFrame:
        frame = self._frames[min(self._index, len(self._frames) - 1)]
        self._index += 1
        return dataclasses.replace(frame)
