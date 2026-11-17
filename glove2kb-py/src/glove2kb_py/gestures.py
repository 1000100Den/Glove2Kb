"""Gesture primitives: frames, wrist zones, grip and finger combos.

A :class:`GestureFrame` is one sample of the (simplified) sensor state:
both wrist rolls in degrees and five right-hand finger flexion values in
0.0..1.0.  Everything here is pure logic so it can be unit-tested without
hardware, a GUI, or a network.

Hysteresis is used throughout so a hand hovering near a threshold does not
make the selection flicker: each detector has separate on/enter and
off/exit thresholds and keeps its previous state in between.
"""

from __future__ import annotations

from dataclasses import dataclass

# Zone identifiers for a wrist roll.
ZONE_CCW = -1      # counterclockwise (negative roll)
ZONE_NEUTRAL = 0
ZONE_CW = 1        # clockwise (positive roll)

# Default thresholds.
ROLL_ENTER_DEG = 25.0    # |roll| needed to leave the neutral zone
ROLL_EXIT_DEG = 15.0     # |roll| below which a side zone returns to neutral
GRIP_ON = 0.60           # mean(middle, ring, little) to enter grip
GRIP_OFF = 0.45          # mean below which grip is released
FINGER_ON = 0.55         # flexion counted as "bent" for thumb/index combos
FINGER_OFF = 0.40        # flexion below which a combo finger is "straight"


def _clamp01(value: float) -> float:
    return 0.0 if value < 0.0 else 1.0 if value > 1.0 else float(value)


@dataclass
class GestureFrame:
    """One sensor sample.

    Rolls are in degrees: positive = clockwise rotation of that wrist
    (seen by the wearer looking along the forearm), negative =
    counterclockwise.  Flexion values are 0.0 (straight) .. 1.0 (fully
    bent) for the *right* hand fingers; the left hand contributes only its
    wrist roll in this design.
    """

    left_roll_deg: float = 0.0
    right_roll_deg: float = 0.0
    thumb: float = 0.0
    index: float = 0.0
    middle: float = 0.0
    ring: float = 0.0
    little: float = 0.0

    def __post_init__(self) -> None:
        self.thumb = _clamp01(self.thumb)
        self.index = _clamp01(self.index)
        self.middle = _clamp01(self.middle)
        self.ring = _clamp01(self.ring)
        self.little = _clamp01(self.little)

    @property
    def grip_amount(self) -> float:
        """Mean flexion of the three grip fingers (middle/ring/little)."""
        return (self.middle + self.ring + self.little) / 3.0


class ZoneTracker:
    """Stateful roll -> zone conversion with hysteresis.

    Moving from neutral into a side zone requires |roll| >= enter_deg;
    once in a side zone, the roll must come back inside exit_deg before
    the zone returns to neutral.  Switching directly from one side zone
    to the other requires passing the opposite enter threshold.
    """

    def __init__(self, enter_deg: float = ROLL_ENTER_DEG,
                 exit_deg: float = ROLL_EXIT_DEG) -> None:
        if not (0.0 <= exit_deg < enter_deg):
            raise ValueError("need 0 <= exit_deg < enter_deg")
        self.enter_deg = float(enter_deg)
        self.exit_deg = float(exit_deg)
        self.zone = ZONE_NEUTRAL

    def reset(self) -> None:
        self.zone = ZONE_NEUTRAL

    def update(self, roll_deg: float) -> int:
        roll = float(roll_deg)
        if self.zone == ZONE_NEUTRAL:
            if roll >= self.enter_deg:
                self.zone = ZONE_CW
            elif roll <= -self.enter_deg:
                self.zone = ZONE_CCW
        elif self.zone == ZONE_CW:
            if roll <= -self.enter_deg:
                self.zone = ZONE_CCW
            elif roll < self.exit_deg:
                self.zone = ZONE_NEUTRAL
        else:  # ZONE_CCW
            if roll >= self.enter_deg:
                self.zone = ZONE_CW
            elif roll > -self.exit_deg:
                self.zone = ZONE_NEUTRAL
        return self.zone


class GripDetector:
    """Grip = middle+ring+little fingers bent together, with hysteresis."""

    def __init__(self, on: float = GRIP_ON, off: float = GRIP_OFF) -> None:
        if not (0.0 <= off < on <= 1.0):
            raise ValueError("need 0 <= off < on <= 1")
        self.on = float(on)
        self.off = float(off)
        self.gripping = False

    def reset(self) -> None:
        self.gripping = False

    def update(self, frame: GestureFrame) -> bool:
        amount = frame.grip_amount
        if self.gripping:
            if amount < self.off:
                self.gripping = False
        elif amount >= self.on:
            self.gripping = True
        return self.gripping


class FingerLatch:
    """One combo finger (thumb or index) as a boolean, with hysteresis."""

    def __init__(self, on: float = FINGER_ON, off: float = FINGER_OFF) -> None:
        if not (0.0 <= off < on <= 1.0):
            raise ValueError("need 0 <= off < on <= 1")
        self.on = float(on)
        self.off = float(off)
        self.bent = False

    def reset(self) -> None:
        self.bent = False

    def update(self, value: float) -> bool:
        value = _clamp01(value)
        if self.bent:
            if value < self.off:
                self.bent = False
        elif value >= self.on:
            self.bent = True
        return self.bent


def key_index_from_combo(thumb_bent: bool, index_bent: bool) -> int:
    """Map the combo fingers to a key index inside a group.

    Order (from the first key of the group):
    grip only -> 0, grip+index -> 1, grip+thumb -> 2,
    grip+index+thumb -> 3.
    """
    if thumb_bent and index_bent:
        return 3
    if thumb_bent:
        return 2
    if index_bent:
        return 1
    return 0
