import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                "..", "src"))

import struct
import unittest

from glove2kb_py import osc


class RoundTripTest(unittest.TestCase):
    def test_round_trip_mixed_args(self):
        args = [1, -25, 1.5, "RightHand", True, False, None, b"\x01\x02\x03"]
        packet = osc.build_message("/VMC/Ext/Bone/Pos", args)
        address, parsed = osc.parse_message(packet)
        self.assertEqual(address, "/VMC/Ext/Bone/Pos")
        self.assertEqual(parsed[0], 1)
        self.assertEqual(parsed[1], -25)
        self.assertAlmostEqual(parsed[2], 1.5, places=6)
        self.assertEqual(parsed[3], "RightHand")
        self.assertEqual(parsed[4:], [True, False, None, b"\x01\x02\x03"])

    def test_round_trip_no_args(self):
        packet = osc.build_message("/test", [])
        self.assertEqual(osc.parse_message(packet), ("/test", []))

    def test_vmc_bone_pos_shape(self):
        args = ["LeftHand", 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0]
        packet = osc.build_message("/VMC/Ext/Bone/Pos", args)
        messages = osc.parse_packet(packet)
        self.assertEqual(len(messages), 1)
        self.assertEqual(messages[0][1][0], "LeftHand")

    def test_bundle_round_trip(self):
        inner = osc.build_message("/a", [1])
        bundle = (osc._encode_padded_string("#bundle") + b"\x00" * 8
                  + struct.pack(">i", len(inner)) + inner)
        messages = osc.parse_packet(bundle)
        self.assertEqual(messages, [("/a", [1])])


class MalformedTest(unittest.TestCase):
    def test_empty_packet_rejected(self):
        with self.assertRaises(osc.OSCError):
            osc.parse_message(b"")

    def test_oversized_packet_rejected(self):
        with self.assertRaises(osc.OSCError):
            osc.parse_message(b"\x00" * (osc.MAX_PACKET_SIZE + 4))

    def test_non_multiple_of_four_rejected(self):
        with self.assertRaises(osc.OSCError):
            osc.parse_message(b"/a\x00")

    def test_address_without_slash_rejected(self):
        packet = osc._encode_padded_string("abc") + osc._encode_padded_string(",")
        with self.assertRaises(osc.OSCError):
            osc.parse_message(packet)

    def test_missing_typetags_rejected(self):
        packet = osc._encode_padded_string("/a")
        with self.assertRaises(osc.OSCError):
            osc.parse_message(packet)

    def test_truncated_argument_rejected(self):
        # Remove the whole 4-byte int argument: the type tag promises
        # data the packet does not contain.
        packet = osc.build_message("/a", [12345])[:-4]
        with self.assertRaises(osc.OSCError):
            osc.parse_message(packet)

    def test_unknown_type_tag_rejected(self):
        packet = (osc._encode_padded_string("/a")
                  + osc._encode_padded_string(",z") + b"\x00" * 4)
        with self.assertRaises(osc.OSCError):
            osc.parse_message(packet)

    def test_unterminated_string_rejected(self):
        packet = b"/abc" + osc._encode_padded_string(",")
        with self.assertRaises(osc.OSCError):
            osc.parse_message(packet)

    def test_trailing_garbage_rejected(self):
        packet = osc.build_message("/a", [1]) + b"\x01\x00\x00\x00"
        with self.assertRaises(osc.OSCError):
            osc.parse_message(packet)

    def test_negative_blob_size_rejected(self):
        packet = (osc._encode_padded_string("/a")
                  + osc._encode_padded_string(",b")
                  + struct.pack(">i", -1))
        with self.assertRaises(osc.OSCError):
            osc.parse_message(packet)

    def test_build_rejects_bad_address(self):
        with self.assertRaises(osc.OSCError):
            osc.build_message("no-slash", [])


if __name__ == "__main__":
    unittest.main()
