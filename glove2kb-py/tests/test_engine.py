import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                "..", "src"))

import unittest

from glove2kb_py import layout
from glove2kb_py.engine import EngineState, InputEngine
from glove2kb_py.gestures import GestureFrame

HOLD = 5  # frames to hold each pose; comfortably above the debounce of 3


def frame_for(row, col, key_index, gripping=True):
    """Build the GestureFrame that selects (row, col, key_index)."""
    right_roll = (-45.0, 0.0, 45.0)[row]
    left_roll = (-45.0, 0.0, 45.0)[col]
    thumb = 1.0 if key_index in (2, 3) else 0.0
    index = 1.0 if key_index in (1, 3) else 0.0
    grip = 1.0 if gripping else 0.0
    return GestureFrame(
        left_roll_deg=left_roll, right_roll_deg=right_roll,
        thumb=thumb, index=index,
        middle=grip, ring=grip, little=grip)


def type_key(engine, page, text_or_action, is_action=False):
    """Drive the engine through one full grip-select-release cycle."""
    if is_action:
        pos = layout.find_action(page, text_or_action)
    else:
        pos = layout.find_key(page, text_or_action)
    assert pos is not None, f"key not on page {page}: {text_or_action}"
    frame = frame_for(*pos)
    for _ in range(HOLD):
        engine.process_frame(frame)
    engine.process_frame(frame_for(*pos, gripping=False))


class EngineFlowTest(unittest.TestCase):
    def setUp(self):
        self.engine = InputEngine(debounce_frames=3)

    def test_types_hi_then_fixes_with_backspace(self):
        type_key(self.engine, layout.PAGE_LETTERS, "h")
        type_key(self.engine, layout.PAGE_LETTERS, "i")
        self.assertEqual(self.engine.text, "hi")
        # Type a wrong letter, then erase it with backspace.
        type_key(self.engine, layout.PAGE_LETTERS, "x")
        self.assertEqual(self.engine.text, "hix")
        type_key(self.engine, layout.PAGE_LETTERS,
                 layout.ACTION_BACKSPACE, is_action=True)
        self.assertEqual(self.engine.text, "hi")

    def test_space_and_enter(self):
        type_key(self.engine, layout.PAGE_LETTERS, "a")
        type_key(self.engine, layout.PAGE_LETTERS, " ")
        type_key(self.engine, layout.PAGE_LETTERS, "b")
        type_key(self.engine, layout.PAGE_LETTERS,
                 layout.ACTION_ENTER, is_action=True)
        self.assertEqual(self.engine.text, "a b\n")

    def test_page_toggle_and_digit(self):
        type_key(self.engine, layout.PAGE_LETTERS,
                 layout.ACTION_TOGGLE_PAGE, is_action=True)
        self.assertEqual(self.engine.page, layout.PAGE_NUMBERS)
        self.assertEqual(self.engine.text, "")
        type_key(self.engine, layout.PAGE_NUMBERS, "5")
        self.assertEqual(self.engine.text, "5")

    def test_backspace_on_empty_buffer_is_noop(self):
        type_key(self.engine, layout.PAGE_LETTERS,
                 layout.ACTION_BACKSPACE, is_action=True)
        self.assertEqual(self.engine.text, "")

    def test_commit_by_returning_wrists_to_neutral(self):
        # Select 'e' (row 0, col 1, key 1): grip and rotate.
        frame = frame_for(0, 1, 0)
        for _ in range(HOLD):
            self.engine.process_frame(frame)
        # Return both wrists to neutral while still gripping.
        neutral_grip = GestureFrame(middle=1.0, ring=1.0, little=1.0)
        committed = []
        for _ in range(HOLD):
            committed += self.engine.process_frame(neutral_grip)
        self.assertEqual([k.text for k in committed], ["e"])
        self.assertEqual(self.engine.text, "e")
        # Releasing the grip afterwards must not commit a second time.
        self.engine.process_frame(GestureFrame())
        self.assertEqual(self.engine.text, "e")

    def test_selection_rearms_after_neutral_commit(self):
        # Commit 'e' by returning to neutral, keep gripping, move to
        # 'a' (row 0, col 0, key 1) and release: both letters appear.
        frame_e = frame_for(0, 1, 0)
        for _ in range(HOLD):
            self.engine.process_frame(frame_e)
        neutral_grip = GestureFrame(middle=1.0, ring=1.0, little=1.0)
        for _ in range(HOLD):
            self.engine.process_frame(neutral_grip)
        self.assertEqual(self.engine.text, "e")
        frame_a = frame_for(0, 0, 0)
        for _ in range(HOLD):
            self.engine.process_frame(frame_a)
        self.engine.process_frame(frame_for(0, 0, 0, gripping=False))
        self.assertEqual(self.engine.text, "ea")

    def test_debounce_ignores_pass_through_selection(self):
        # Briefly (1 frame < debounce) pass over 'a' on the way to 'h',
        # then hold 'h' and release: only 'h' may be committed.
        for _ in range(1):
            self.engine.process_frame(frame_for(0, 0, 0))
        frame_h = frame_for(0, 1, 3)
        for _ in range(HOLD):
            self.engine.process_frame(frame_h)
        self.engine.process_frame(frame_for(0, 1, 3, gripping=False))
        self.assertEqual(self.engine.text, "h")

    def test_no_commit_without_grip(self):
        frame = GestureFrame(left_roll_deg=-45.0, right_roll_deg=-45.0,
                             thumb=1.0, index=1.0)
        for _ in range(HOLD):
            committed = self.engine.process_frame(frame)
            self.assertEqual(committed, [])
        self.assertEqual(self.engine.state, EngineState.IDLE)
        self.assertEqual(self.engine.text, "")

    def test_state_transitions(self):
        self.assertEqual(self.engine.state, EngineState.IDLE)
        self.engine.process_frame(frame_for(1, 1, 0))
        self.assertEqual(self.engine.state, EngineState.SELECTING)
        self.engine.process_frame(GestureFrame())
        self.assertEqual(self.engine.state, EngineState.IDLE)

    def test_current_key_tracks_selection(self):
        frame = frame_for(0, 1, 3)  # 'h'
        for _ in range(HOLD):
            self.engine.process_frame(frame)
        key = self.engine.current_key()
        self.assertIsNotNone(key)
        self.assertEqual(key.text, "h")


if __name__ == "__main__":
    unittest.main()
