"""Keyboard layout: a 3x3 grid of key groups, up to 4 keys per group.

Selection model (original design for this reimplementation):

* Right wrist roll selects the *row band* (counterclockwise / neutral /
  clockwise -> rows 0 / 1 / 2).
* Left wrist roll selects the *column band* (counterclockwise / neutral /
  clockwise -> columns 0 / 1 / 2).
* While gripping, the key inside the group is chosen by finger
  combination, counted from the first key of the group:

    1. grip only                -> key 1
    2. grip + index             -> key 2
    3. grip + thumb             -> key 3
    4. grip + index + thumb     -> key 4

A 3x3x4 grid has 36 slots, which cannot hold a-z + 0-9 + editing keys +
symbols at once, so the layout has two *pages* (letters / numbers) and one
slot is a page-toggle key (label "123" on the letters page, "ABC" on the
numbers page).  Every other required key lives on exactly one page:

* letters page: a-z, space, backspace, enter, symbols . , ! ? - '
* numbers page: 0-9, space, backspace, enter, a wider symbol set

This module is pure data plus lookup helpers; it has no I/O.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Optional, Tuple

# Action identifiers for non-printing keys.
ACTION_BACKSPACE = "backspace"
ACTION_ENTER = "enter"
ACTION_TOGGLE_PAGE = "toggle_page"

PAGE_LETTERS = "letters"
PAGE_NUMBERS = "numbers"
PAGES = (PAGE_LETTERS, PAGE_NUMBERS)

GRID_ROWS = 3
GRID_COLS = 3
KEYS_PER_GROUP = 4


@dataclass(frozen=True)
class Key:
    """One key slot.

    ``label`` is what the GUI shows, ``text`` is the character(s) committed
    to the text buffer (None for action keys), and ``action`` is one of the
    ACTION_* constants or None for ordinary printing keys.
    """

    label: str
    text: Optional[str] = None
    action: Optional[str] = None

    @property
    def is_action(self) -> bool:
        return self.action is not None


def _char(ch: str) -> Key:
    return Key(label=ch, text=ch)


_SPACE = Key(label="Space", text=" ")
_BACKSPACE = Key(label="BS", action=ACTION_BACKSPACE)
_ENTER = Key(label="Enter", action=ACTION_ENTER)
_TO_NUMBERS = Key(label="123", action=ACTION_TOGGLE_PAGE)
_TO_LETTERS = Key(label="ABC", action=ACTION_TOGGLE_PAGE)

# A group is a tuple of 1..4 keys.  A page is a 3x3 tuple of groups,
# indexed [row][col].
Group = Tuple[Key, ...]
Page = Tuple[Tuple[Group, ...], ...]

LETTERS_PAGE: Page = (
    (  # row 0 (right wrist counterclockwise)
        (_char("a"), _char("b"), _char("c"), _char("d")),
        (_char("e"), _char("f"), _char("g"), _char("h")),
        (_char("i"), _char("j"), _char("k"), _char("l")),
    ),
    (  # row 1 (right wrist neutral)
        (_char("m"), _char("n"), _char("o"), _char("p")),
        (_char("q"), _char("r"), _char("s"), _char("t")),
        (_char("u"), _char("v"), _char("w"), _char("x")),
    ),
    (  # row 2 (right wrist clockwise)
        (_char("y"), _char("z"), _SPACE, _BACKSPACE),
        (_char("."), _char(","), _char("!"), _char("?")),
        (_ENTER, _TO_NUMBERS, _char("-"), _char("'")),
    ),
)

NUMBERS_PAGE: Page = (
    (
        (_char("1"), _char("2"), _char("3"), _char("4")),
        (_char("5"), _char("6"), _char("7"), _char("8")),
        (_char("9"), _char("0"), _char("+"), _char("-")),
    ),
    (
        (_char("."), _char(","), _char("!"), _char("?")),
        (_char("@"), _char("#"), _char("%"), _char("&")),
        (_char("*"), _char("("), _char(")"), _char("/")),
    ),
    (
        (_SPACE, _BACKSPACE, _ENTER, _char(":")),
        (_TO_LETTERS, _char("_"), _char("="), _char(";")),
        (_char('"'), _char("<"), _char(">"), _char("$")),
    ),
)

_LAYOUTS = {
    PAGE_LETTERS: LETTERS_PAGE,
    PAGE_NUMBERS: NUMBERS_PAGE,
}


def other_page(page: str) -> str:
    """Return the page reached by the toggle key from ``page``."""
    return PAGE_NUMBERS if page == PAGE_LETTERS else PAGE_LETTERS


def get_page(page: str) -> Page:
    """Return the 3x3 page structure; raises ValueError for bad names."""
    try:
        return _LAYOUTS[page]
    except KeyError:
        raise ValueError(f"unknown page: {page!r}") from None


def get_group(page: str, row: int, col: int) -> Group:
    """Return the group at (row, col) on ``page`` (indices 0..2)."""
    if not (0 <= row < GRID_ROWS and 0 <= col < GRID_COLS):
        raise IndexError(f"group index out of range: row={row}, col={col}")
    return get_page(page)[row][col]


def get_key(page: str, row: int, col: int, key_index: int) -> Optional[Key]:
    """Return the key at the given position, or None if the slot is empty."""
    group = get_group(page, row, col)
    if 0 <= key_index < len(group):
        return group[key_index]
    return None


def find_key(page: str, text: str) -> Optional[Tuple[int, int, int]]:
    """Locate a printing key by its text; returns (row, col, key_index)."""
    for row in range(GRID_ROWS):
        for col in range(GRID_COLS):
            for idx, key in enumerate(get_group(page, row, col)):
                if key.text == text and key.action is None:
                    return (row, col, idx)
    return None


def find_action(page: str, action: str) -> Optional[Tuple[int, int, int]]:
    """Locate an action key; returns (row, col, key_index) or None."""
    for row in range(GRID_ROWS):
        for col in range(GRID_COLS):
            for idx, key in enumerate(get_group(page, row, col)):
                if key.action == action:
                    return (row, col, idx)
    return None


def all_keys(page: str) -> Tuple[Key, ...]:
    """Return every key on a page, in grid order."""
    out = []
    for row in range(GRID_ROWS):
        for col in range(GRID_COLS):
            out.extend(get_group(page, row, col))
    return tuple(out)
