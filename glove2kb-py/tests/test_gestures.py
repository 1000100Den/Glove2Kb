import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                "..", "src"))

import unittest

from glove2kb_py.gestures import (
    FingerLatch,
    GestureFrame,
    GripDetector,
    ZoneTracker,
    ZONE_CCW,
    ZONE_CW,
    ZONE_NEUTRAL,
    key_index_from_combo,
)


def grip_frame(amount):
    return GestureFrame(middle=amount, ring=amount, little=amount)


class ZoneTrackerTest(unittest.TestCase):
    def test_neutral_below_enter_threshold(self):
        tracker = ZoneTracker()
        self.assertEqual(tracker.update(0.0), ZONE_NEUTRAL)
        self.assertEqual(tracker.update(24.9), ZONE_NEUTRAL)

    def test_enter_cw_and_ccw(self):
        tracker = ZoneTracker()
        self.assertEqual(tracker.update(25.0), ZONE_CW)
        tracker.reset()
        self.assertEqual(tracker.update(-25.0), ZONE_CCW)

    def test_hysteresis_holds_zone_between_thresholds(self):
        tracker = ZoneTracker()
        tracker.update(40.0)
        # Between exit (15) and enter (25): zone must not drop to neutral.
        self.assertEqual(tracker.update(20.0), ZONE_CW)
        self.assertEqual(tracker.update(15.0), ZONE_CW)
        self.assertEqual(tracker.update(14.9), ZONE_NEUTRAL)

    def test_hysteresis_on_ccw_side(self):
        tracker = ZoneTracker()
        tracker.update(-40.0)
        self.assertEqual(tracker.update(-20.0), ZONE_CCW)
        self.assertEqual(tracker.update(-14.9), ZONE_NEUTRAL)

    def test_direct_side_to_side_switch(self):
        tracker = ZoneTracker()
        tracker.update(40.0)
        self.assertEqual(tracker.update(-40.0), ZONE_CCW)

    def test_invalid_thresholds_rejected(self):
        with self.assertRaises(ValueError):
            ZoneTracker(enter_deg=10.0, exit_deg=10.0)


class GripDetectorTest(unittest.TestCase):
    def test_grip_enters_at_on_threshold(self):
        detector = GripDetector()
        self.assertFalse(detector.update(grip_frame(0.59)))
        self.assertTrue(detector.update(grip_frame(0.60)))

    def test_grip_hysteresis(self):
        detector = GripDetector()
        detector.update(grip_frame(1.0))
        self.assertTrue(detector.update(grip_frame(0.50)))   # above off=0.45
        self.assertFalse(detector.update(grip_frame(0.44)))  # below off

    def test_grip_uses_mean_of_three_fingers(self):
        detector = GripDetector()
        frame = GestureFrame(middle=1.0, ring=1.0, little=0.0)  # mean 0.667
        self.assertTrue(detector.update(frame))
        frame = GestureFrame(middle=1.0, ring=0.0, little=0.0)   # mean 0.333
        detector.reset()
        self.assertFalse(detector.update(frame))


class ComboTest(unittest.TestCase):
    def test_combo_order(self):
        self.assertEqual(key_index_from_combo(False, False), 0)
        self.assertEqual(key_index_from_combo(False, True), 1)
        self.assertEqual(key_index_from_combo(True, False), 2)
        self.assertEqual(key_index_from_combo(True, True), 3)

    def test_finger_latch_hysteresis(self):
        latch = FingerLatch()
        self.assertFalse(latch.update(0.54))
        self.assertTrue(latch.update(0.55))
        self.assertTrue(latch.update(0.45))   # above off=0.40
        self.assertFalse(latch.update(0.39))

    def test_frame_clamps_flexion(self):
        frame = GestureFrame(thumb=1.7, index=-0.2)
        self.assertEqual(frame.thumb, 1.0)
        self.assertEqual(frame.index, 0.0)


if __name__ == "__main__":
    unittest.main()
