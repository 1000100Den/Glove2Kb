import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                "..", "src"))

import math
import unittest

from glove2kb_py import vmc_receiver
from glove2kb_py.engine import InputEngine
from glove2kb_py.gestures import GestureFrame
from glove2kb_py.simulator import ScriptedTransport, SimulatorTransport


def axis_quat(axis_x, angle_deg):
    half = math.radians(angle_deg) / 2.0
    return (axis_x * math.sin(half), 0.0, 0.0, math.cos(half))


class BonesToFrameTest(unittest.TestCase):
    def test_no_hands_returns_none(self):
        self.assertIsNone(vmc_receiver.bones_to_frame({}))

    def test_identity_hands_give_neutral_frame(self):
        bones = {"LeftHand": (0.0, 0.0, 0.0, 1.0),
                 "RightHand": (0.0, 0.0, 0.0, 1.0)}
        frame = vmc_receiver.bones_to_frame(bones)
        self.assertAlmostEqual(frame.left_roll_deg, 0.0)
        self.assertAlmostEqual(frame.right_roll_deg, 0.0)
        self.assertEqual(frame.grip_amount, 0.0)

    def test_wrist_roll_from_hand_quaternion(self):
        bones = {"RightHand": axis_quat(1.0, 60.0)}
        frame = vmc_receiver.bones_to_frame(bones)
        self.assertAlmostEqual(frame.right_roll_deg, 60.0, places=4)

    def test_finger_flexion_full_bend(self):
        bones = {"RightHand": (0.0, 0.0, 0.0, 1.0)}
        for joint in ("Proximal", "Intermediate", "Distal"):
            bones[f"RightIndex{joint}"] = axis_quat(1.0, 80.0)
        frame = vmc_receiver.bones_to_frame(bones)
        # 3 joints * 80 deg = 240 deg = the index finger maximum.
        self.assertAlmostEqual(frame.index, 1.0, places=4)
        self.assertEqual(frame.thumb, 0.0)


class PortValidationTest(unittest.TestCase):
    def test_valid_ports(self):
        self.assertEqual(vmc_receiver.validate_port(39539), 39539)
        self.assertEqual(vmc_receiver.validate_port(1), 1)
        self.assertEqual(vmc_receiver.validate_port(65535), 65535)

    def test_invalid_ports(self):
        for bad in (0, 65536, -1, "39539", None, True):
            with self.assertRaises(ValueError):
                vmc_receiver.validate_port(bad)


class SimulatorSmokeTest(unittest.TestCase):
    """Headless end-to-end: scripted frames -> engine -> text."""

    def test_scripted_transport_types_a_letter(self):
        # Grip with neutral wrists selects the centre group (row 1,
        # col 1); key 1 there is 'q' on the letters page.
        grip = GestureFrame(middle=1.0, ring=1.0, little=1.0)
        frames = [grip] * 5 + [GestureFrame()] * 2
        transport = ScriptedTransport(frames)
        engine = InputEngine(debounce_frames=3)
        for _ in range(len(frames)):
            engine.process_frame(transport.get_frame())
        self.assertEqual(engine.text, "q")

    def test_simulator_presets(self):
        sim = SimulatorTransport()
        sim.apply_preset("key4")
        frame = sim.get_frame()
        self.assertEqual(frame.grip_amount, 1.0)
        self.assertEqual((frame.thumb, frame.index), (1.0, 1.0))
        sim.apply_preset("release")
        self.assertEqual(sim.get_frame().grip_amount, 0.0)
        with self.assertRaises(ValueError):
            sim.apply_preset("nope")


if __name__ == "__main__":
    unittest.main()
