"""VMC (VirtualMotionCapture protocol) receiver over UDP/OSC.

DivingStation forwards ContactGlove tracking as VMC messages; the ones
this project consumes are::

    /VMC/Ext/Bone/Pos  <bone-name:str> <px,py,pz:float> <qx,qy,qz,qw:float>

Security choices:

* The UDP socket binds to **127.0.0.1 only** by default -- VMC traffic is
  a local pipeline and must not be exposed to the network.
* Datagrams larger than the OSC maximum are dropped unparsed.
* All parsing goes through the strict parser in :mod:`.osc`; malformed
  packets are counted and discarded, never raised into the receive loop.

Simplified conversion to :class:`GestureFrame` (approximation, documented
in the README):

* Wrist roll: from the ``LeftHand`` / ``RightHand`` bone quaternion,
  roll = atan2(2(w x + y z), 1 - 2(x^2 + y^2)) in degrees -- the rotation
  about the hand's local X axis, used as a proxy for wrist roll.
* Finger flexion (right hand): for each finger, the joint bend angles of
  the Proximal / Intermediate / Distal bones are summed.  A joint's bend
  angle is the rotation angle of its quaternion relative to the hand
  bone's quaternion (2*acos(|w|) of the relative quaternion).  The sum
  is normalised by a per-finger maximum (thumb 150 deg, other fingers
  240 deg) and clamped to 0..1.  This is a coarse approximation: real
  VMC finger data depends on the sender's calibration.
"""

from __future__ import annotations

import math
import socket
import threading
from typing import Dict, Optional, Tuple

from . import osc
from .gestures import GestureFrame

DEFAULT_PORT = 39539          # VMC receive port commonly used with DivingStation
DEFAULT_HOST = "127.0.0.1"    # localhost only, by design
BONE_POS_ADDRESS = "/VMC/Ext/Bone/Pos"

Quaternion = Tuple[float, float, float, float]
_IDENTITY: Quaternion = (0.0, 0.0, 0.0, 1.0)

_FINGERS = ("Thumb", "Index", "Middle", "Ring", "Little")
_JOINTS = ("Proximal", "Intermediate", "Distal")
_FINGER_MAX_DEG = {
    "Thumb": 150.0,
    "Index": 240.0,
    "Middle": 240.0,
    "Ring": 240.0,
    "Little": 240.0,
}


def validate_port(port: int) -> int:
    """Validate a UDP port number; raises ValueError when out of range."""
    if isinstance(port, bool) or not isinstance(port, int):
        raise ValueError("port must be an integer")
    if not (1 <= port <= 65535):
        raise ValueError("port must be in 1..65535")
    return port


def _quat_multiply(a: Quaternion, b: Quaternion) -> Quaternion:
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (
        aw * bx + ax * bw + ay * bz - az * by,
        aw * by - ax * bz + ay * bw + az * bx,
        aw * bz + ax * by - ay * bx + az * bw,
        aw * bw - ax * bx - ay * by - az * bz,
    )


def _quat_conjugate(q: Quaternion) -> Quaternion:
    return (-q[0], -q[1], -q[2], q[3])


def _quat_normalized(q: Quaternion) -> Quaternion:
    norm = math.sqrt(sum(c * c for c in q))
    if norm == 0.0:
        return _IDENTITY
    return (q[0] / norm, q[1] / norm, q[2] / norm, q[3] / norm)


def quat_angle_deg(q: Quaternion) -> float:
    """Rotation angle of a quaternion, in degrees (0..180)."""
    _, _, _, w = _quat_normalized(q)
    return math.degrees(2.0 * math.acos(min(1.0, abs(w))))


def wrist_roll_deg(hand_quat: Quaternion) -> float:
    """Approximate wrist roll (deg) from a hand bone quaternion."""
    x, y, z, w = _quat_normalized(hand_quat)
    roll = math.atan2(2.0 * (w * x + y * z), 1.0 - 2.0 * (x * x + y * y))
    return math.degrees(roll)


def finger_flexion(bones: Dict[str, Quaternion], hand: str,
                   finger: str) -> float:
    """Approximate flexion 0..1 of one finger from its joint bones."""
    hand_quat = bones.get(f"{hand}Hand", _IDENTITY)
    total = 0.0
    for joint in _JOINTS:
        quat = bones.get(f"{hand}{finger}{joint}")
        if quat is None:
            continue
        relative = _quat_multiply(_quat_conjugate(hand_quat), quat)
        total += quat_angle_deg(relative)
    return max(0.0, min(1.0, total / _FINGER_MAX_DEG[finger]))


def bones_to_frame(bones: Dict[str, Quaternion]) -> Optional[GestureFrame]:
    """Convert a bone-quaternion snapshot to a GestureFrame.

    Returns None until at least one hand bone has been seen.
    """
    if "LeftHand" not in bones and "RightHand" not in bones:
        return None
    frame = GestureFrame(
        left_roll_deg=wrist_roll_deg(bones.get("LeftHand", _IDENTITY)),
        right_roll_deg=wrist_roll_deg(bones.get("RightHand", _IDENTITY)),
    )
    frame.thumb = finger_flexion(bones, "Right", "Thumb")
    frame.index = finger_flexion(bones, "Right", "Index")
    frame.middle = finger_flexion(bones, "Right", "Middle")
    frame.ring = finger_flexion(bones, "Right", "Ring")
    frame.little = finger_flexion(bones, "Right", "Little")
    # GestureFrame clamps flexion in __post_init__ only for constructor
    # arguments; re-clamp after direct assignment.
    for attr in ("thumb", "index", "middle", "ring", "little"):
        setattr(frame, attr, max(0.0, min(1.0, getattr(frame, attr))))
    return frame


def _parse_bone_pos(args) -> Optional[Tuple[str, Quaternion]]:
    """Validate one /VMC/Ext/Bone/Pos argument list."""
    if len(args) < 8:
        return None
    name = args[0]
    if not isinstance(name, str) or not name:
        return None
    values = args[4:8]
    if not all(isinstance(v, (int, float)) and not isinstance(v, bool)
               for v in values):
        return None
    quat = tuple(float(v) for v in values)
    if not all(math.isfinite(v) for v in quat):
        return None
    return name, quat  # type: ignore[return-value]


class VmcReceiver:
    """Background-thread UDP receiver producing GestureFrames.

    Usage::

        rx = VmcReceiver(port=39539)
        rx.start()
        frame = rx.get_frame()   # latest frame or None
        rx.stop()
    """

    def __init__(self, host: str = DEFAULT_HOST, port: int = DEFAULT_PORT) -> None:
        self.host = host
        self.port = validate_port(port)
        self._bones: Dict[str, Quaternion] = {}
        self._lock = threading.Lock()
        self._thread: Optional[threading.Thread] = None
        self._stop = threading.Event()
        self._sock: Optional[socket.socket] = None
        self.packets_received = 0
        self.packets_rejected = 0

    @property
    def running(self) -> bool:
        return self._thread is not None and self._thread.is_alive()

    def start(self) -> None:
        if self.running:
            return
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        sock.bind((self.host, self.port))
        sock.settimeout(0.25)
        self._sock = sock
        self._stop.clear()
        self._thread = threading.Thread(
            target=self._run, name="vmc-receiver", daemon=True)
        self._thread.start()

    def stop(self) -> None:
        self._stop.set()
        if self._thread is not None:
            self._thread.join(timeout=2.0)
            self._thread = None
        if self._sock is not None:
            self._sock.close()
            self._sock = None

    def get_frame(self) -> Optional[GestureFrame]:
        """Return the latest GestureFrame, or None if no hand data yet."""
        with self._lock:
            snapshot = dict(self._bones)
        return bones_to_frame(snapshot)

    # -- internals ---------------------------------------------------------

    def _run(self) -> None:
        assert self._sock is not None
        while not self._stop.is_set():
            try:
                data, _addr = self._sock.recvfrom(osc.MAX_PACKET_SIZE + 1)
            except socket.timeout:
                continue
            except OSError:
                break
            if len(data) > osc.MAX_PACKET_SIZE:
                self.packets_rejected += 1
                continue
            try:
                messages = osc.parse_packet(data)
            except osc.OSCError:
                self.packets_rejected += 1
                continue
            self.packets_received += 1
            updates: Dict[str, Quaternion] = {}
            for address, args in messages:
                if address != BONE_POS_ADDRESS:
                    continue
                parsed = _parse_bone_pos(args)
                if parsed is not None:
                    updates[parsed[0]] = parsed[1]
            if updates:
                with self._lock:
                    self._bones.update(updates)
