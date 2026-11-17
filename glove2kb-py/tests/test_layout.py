import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                "..", "src"))

import unittest

from glove2kb_py import layout


class LayoutLookupTest(unittest.TestCase):
    def test_first_key_is_a(self):
        key = layout.get_key(layout.PAGE_LETTERS, 0, 0, 0)
        self.assertEqual(key.text, "a")

    def test_h_position(self):
        # 'h' is the 4th key of group (row 0, col 1): e f g h
        self.assertEqual(layout.find_key(layout.PAGE_LETTERS, "h"), (0, 1, 3))
        self.assertEqual(layout.get_key(layout.PAGE_LETTERS, 0, 1, 3).text, "h")

    def test_all_letters_present_exactly_once(self):
        texts = [k.text for k in layout.all_keys(layout.PAGE_LETTERS)
                 if k.text and len(k.text) == 1 and k.text.isalpha()]
        self.assertEqual(sorted(texts), list("abcdefghijklmnopqrstuvwxyz"))

    def test_all_digits_present_on_numbers_page(self):
        texts = [k.text for k in layout.all_keys(layout.PAGE_NUMBERS)]
        for digit in "0123456789":
            self.assertIn(digit, texts)

    def test_editing_keys_on_both_pages(self):
        for page in layout.PAGES:
            self.assertIsNotNone(layout.find_key(page, " "))
            self.assertIsNotNone(layout.find_action(page, layout.ACTION_BACKSPACE))
            self.assertIsNotNone(layout.find_action(page, layout.ACTION_ENTER))
            self.assertIsNotNone(
                layout.find_action(page, layout.ACTION_TOGGLE_PAGE))

    def test_groups_hold_at_most_four_keys(self):
        for page in layout.PAGES:
            for row in range(3):
                for col in range(3):
                    group = layout.get_group(page, row, col)
                    self.assertGreaterEqual(len(group), 1)
                    self.assertLessEqual(len(group), layout.KEYS_PER_GROUP)

    def test_empty_slot_returns_none(self):
        self.assertIsNone(layout.get_key(layout.PAGE_LETTERS, 0, 0, 99))

    def test_group_out_of_range(self):
        with self.assertRaises(IndexError):
            layout.get_group(layout.PAGE_LETTERS, 3, 0)

    def test_unknown_page(self):
        with self.assertRaises(ValueError):
            layout.get_page("nope")

    def test_symbols_on_letters_page(self):
        texts = [k.text for k in layout.all_keys(layout.PAGE_LETTERS)]
        for symbol in ".,!?":
            self.assertIn(symbol, texts)

    def test_other_page_round_trip(self):
        self.assertEqual(
            layout.other_page(layout.other_page(layout.PAGE_LETTERS)),
            layout.PAGE_LETTERS)


if __name__ == "__main__":
    unittest.main()
