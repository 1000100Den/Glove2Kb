"""Application entry point."""

from __future__ import annotations

import sys


def main() -> int:
    """Launch the GUI; returns a process exit code."""
    try:
        from .gui import main as gui_main
        gui_main()
    except Exception as exc:  # tkinter missing, or no display (TclError)
        print(f"Glove2Kb-Py: GUI unavailable ({exc}). "
              "The core engine can still be used programmatically.",
              file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
