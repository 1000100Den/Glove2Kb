"""Input engine: the gesture-to-text state machine.

Pure logic -- no GUI, no network, no hardware.  Feed it one
:class:`~glove2kb_py.gestures.GestureFrame` per tick via
:meth:`InputEngine.process_frame`; it maintains a text buffer and returns
the keys committed by that frame.

States
------
IDLE
    No grip.  Nothing is selected.
SELECTING
    The user is gripping.  The wrist zones pick the key group and the
    thumb/index combination picks the key inside the group.

Commit rules (the operation flow)
---------------------------------
1. Grip to enter SELECTING.
2. Rotate the wrists to choose the group, bend thumb/index to choose the
   key.  A selection must stay unchanged for ``debounce_frames``
   consecutive frames before it becomes the *active* selection; this
   debouncing keeps pass-through positions (crossed on the way to the
   target group) from being committed.
3. Commit happens when either:
   * the grip is **released** -- the active selection is committed, or
   * **both wrists return to neutral** while still gripping, after having
     been in a non-neutral zone -- the selection that was active just
     before the return is committed.
   A selection is never committed twice for one hold: after a
   neutral-return commit, the same selection will not be committed again
   when the grip is later released, and the centre group does not become
   a new selection while the wrists stay neutral (a "neutral lock").
   Moving the wrists out of neutral clears the lock and re-arms
   committing for the next selection.

Note: gripping with both wrists already neutral selects the centre group,
and releasing the grip commits its key -- that is how the centre group is
typed.
"""

from __future__ import annotations

import enum
from typing import List, Optional, Tuple

from . import layout
from .gestures import (
    FingerLatch,
    GestureFrame,
    GripDetector,
    ZoneTracker,
    key_index_from_combo,
)

# A selection is (page, row, col, key_index).
Selection = Tuple[str, int, int, int]


class EngineState(enum.Enum):
    IDLE = "idle"
    SELECTING = "selecting"


class InputEngine:
    """Gesture-driven text input state machine."""

    def __init__(self, debounce_frames: int = 3) -> None:
        if debounce_frames < 1:
            raise ValueError("debounce_frames must be >= 1")
        self.debounce_frames = int(debounce_frames)

        self._text: str = ""
        self.page: str = layout.PAGE_LETTERS
        self.state: EngineState = EngineState.IDLE

        self._right_zone = ZoneTracker()
        self._left_zone = ZoneTracker()
        self._grip = GripDetector()
        self._thumb = FingerLatch()
        self._index = FingerLatch()

        self._candidate: Optional[Selection] = None
        self._candidate_count = 0
        self._active: Optional[Selection] = None
        self._active_committed = False
        self._was_non_neutral = False
        self._prev_neutral = True
        self._neutral_lock = False

    # -- read-only views -------------------------------------------------

    @property
    def text(self) -> str:
        """The committed text buffer."""
        return self._text

    @property
    def current_selection(self) -> Optional[Selection]:
        """The debounced (active) selection, or None when not selecting."""
        if self.state is EngineState.SELECTING:
            return self._active
        return None

    @property
    def candidate_selection(self) -> Optional[Selection]:
        """The raw, not-yet-debounced selection under the hand right now."""
        if self.state is EngineState.SELECTING:
            return self._candidate
        return None

    def current_key(self) -> Optional[layout.Key]:
        """The key that would be committed now, or None."""
        sel = self.current_selection
        if sel is None:
            return None
        return layout.get_key(*sel)

    # -- main entry point -------------------------------------------------

    def process_frame(self, frame: GestureFrame) -> List[layout.Key]:
        """Advance the state machine by one frame.

        Returns the list of keys committed by this frame (usually empty,
        at most one key).
        """
        committed: List[layout.Key] = []

        right_zone = self._right_zone.update(frame.right_roll_deg)
        left_zone = self._left_zone.update(frame.left_roll_deg)
        gripping = self._grip.update(frame)

        if not gripping:
            # Grip released (or never started): commit on the IDLE edge.
            if self.state is EngineState.SELECTING:
                if self._active is not None and not self._active_committed:
                    key = self._commit(self._active)
                    if key is not None:
                        committed.append(key)
                self._reset_selection()
            self.state = EngineState.IDLE
            self._thumb.reset()
            self._index.reset()
            return committed

        # Gripping: SELECTING.
        self.state = EngineState.SELECTING
        neutral_now = (right_zone == 0 and left_zone == 0)
        if not neutral_now:
            self._neutral_lock = False
        if self._neutral_lock and neutral_now:
            # A neutral-return commit just happened and the wrists are
            # still neutral: do not let the centre group become a new
            # selection (it would be committed again on release).  The
            # lock clears when the wrists leave neutral or grip ends.
            self._prev_neutral = True
            return committed
        thumb_bent = self._thumb.update(frame.thumb)
        index_bent = self._index.update(frame.index)
        row = right_zone + 1   # CCW -> 0, neutral -> 1, CW -> 2
        col = left_zone + 1
        selection: Selection = (
            self.page,
            row,
            col,
            key_index_from_combo(thumb_bent, index_bent),
        )

        # Debounce: a selection becomes active only after it is stable.
        if selection == self._candidate:
            self._candidate_count += 1
        else:
            self._candidate = selection
            self._candidate_count = 1
        if self._candidate_count >= self.debounce_frames:
            if selection != self._active:
                self._active = selection
                self._active_committed = False

        neutral = (right_zone == 0 and left_zone == 0)
        if not neutral:
            self._was_non_neutral = True
        elif self._prev_neutral is False and self._was_non_neutral:
            # Both wrists just returned to neutral while gripping:
            # commit the selection that was active before the return.
            # The centre-group candidate has not had time to debounce
            # yet, so _active still holds the pre-return selection.
            if self._active is not None and not self._active_committed:
                key = self._commit(self._active)
                if key is not None:
                    committed.append(key)
                self._active_committed = True
                self._neutral_lock = True
            self._was_non_neutral = False
        self._prev_neutral = neutral

        return committed

    # -- internals ---------------------------------------------------------

    def _reset_selection(self) -> None:
        self._candidate = None
        self._candidate_count = 0
        self._active = None
        self._active_committed = False
        self._was_non_neutral = False
        self._prev_neutral = True
        self._neutral_lock = False

    def _commit(self, selection: Selection) -> Optional[layout.Key]:
        """Apply a selection to the text buffer; returns the key used."""
        key = layout.get_key(*selection)
        if key is None:
            return None
        if key.action == layout.ACTION_BACKSPACE:
            self._text = self._text[:-1]
        elif key.action == layout.ACTION_ENTER:
            self._text += "\n"
        elif key.action == layout.ACTION_TOGGLE_PAGE:
            self.page = layout.other_page(self.page)
            # Selections are page-scoped; drop the stale candidate/active
            # so the next frames re-debounce on the new page.
            self._candidate = None
            self._candidate_count = 0
            self._active = None
        elif key.text is not None:
            self._text += key.text
        return key
