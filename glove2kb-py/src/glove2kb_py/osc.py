"""A strict, bounds-checked OSC 1.0 parser/encoder (subset).

Only what VMC traffic needs: messages with int32, float32, float64,
int64, string, blob, char, and the no-argument types T / F / N, plus
bundles (``#bundle``) containing messages.

Security posture:

* No ``eval``/``exec`` anywhere; parsing is explicit byte slicing.
* Every length is checked against the remaining buffer before use.
* Packets larger than :data:`MAX_PACKET_SIZE` are rejected.
* Strings must be NUL-terminated and 4-byte aligned, exactly as OSC 1.0
  requires; malformed alignment, unknown type tags, truncated data and
  non-zero trailing garbage are all rejected with :class:`OSCError`.
"""

from __future__ import annotations

import struct
from typing import List, Tuple, Union

MAX_PACKET_SIZE = 16384  # bytes; a VMC bone message is a few hundred bytes

ArgValue = Union[int, float, str, bytes, bool, None]
Message = Tuple[str, List[ArgValue]]


class OSCError(ValueError):
    """Raised when a packet is malformed, oversized, or unsupported."""


def _pad4(length: int) -> int:
    """Round a length up to the next multiple of 4."""
    return (length + 3) & ~3


def _read_padded_string(data: bytes, offset: int) -> Tuple[str, int]:
    if offset >= len(data):
        raise OSCError("string starts past end of packet")
    end = data.find(b"\x00", offset)
    if end == -1:
        raise OSCError("unterminated string")
    raw = data[offset:end]
    next_offset = offset + _pad4(len(raw) + 1)
    if next_offset > len(data):
        raise OSCError("string padding runs past end of packet")
    if any(data[end + 1:next_offset]):
        raise OSCError("non-zero string padding")
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError as exc:
        raise OSCError("string is not valid UTF-8") from exc
    return text, next_offset


def _encode_padded_string(text: str) -> bytes:
    raw = text.encode("utf-8") + b"\x00"
    return raw + b"\x00" * (_pad4(len(raw)) - len(raw))


def build_message(address: str, args: List[ArgValue]) -> bytes:
    """Encode one OSC message.  Inverse of :func:`parse_message`."""
    if not address.startswith("/"):
        raise OSCError("address must start with '/'")
    tags = [","]
    body = bytearray()
    for arg in args:
        if isinstance(arg, bool):  # before int: bool is an int subclass
            tags.append("T" if arg else "F")
        elif isinstance(arg, int):
            tags.append("i")
            body += struct.pack(">i", arg)
        elif isinstance(arg, float):
            tags.append("f")
            body += struct.pack(">f", arg)
        elif isinstance(arg, str):
            tags.append("s")
            body += _encode_padded_string(arg)
        elif isinstance(arg, (bytes, bytearray)):
            blob = bytes(arg)
            tags.append("b")
            body += struct.pack(">i", len(blob))
            body += blob
            body += b"\x00" * (_pad4(len(blob)) - len(blob))
        elif arg is None:
            tags.append("N")
        else:
            raise OSCError(f"unsupported argument type: {type(arg)!r}")
    packet = _encode_padded_string(address)
    packet += _encode_padded_string("".join(tags))
    packet += bytes(body)
    if len(packet) > MAX_PACKET_SIZE:
        raise OSCError("encoded packet exceeds maximum size")
    return packet


def parse_message(data: bytes) -> Message:
    """Parse exactly one OSC message; rejects bundles and trailing data."""
    if not data:
        raise OSCError("empty packet")
    if len(data) > MAX_PACKET_SIZE:
        raise OSCError("packet exceeds maximum size")
    if len(data) % 4 != 0:
        raise OSCError("packet length is not a multiple of 4")

    address, offset = _read_padded_string(data, 0)
    if not address.startswith("/"):
        raise OSCError("address must start with '/'")
    if offset >= len(data):
        raise OSCError("missing type-tag string")
    tags, offset = _read_padded_string(data, offset)
    if not tags.startswith(","):
        raise OSCError("type-tag string must start with ','")

    args: List[ArgValue] = []
    for tag in tags[1:]:
        if tag == "i":
            _require(data, offset, 4)
            args.append(struct.unpack_from(">i", data, offset)[0])
            offset += 4
        elif tag == "f":
            _require(data, offset, 4)
            args.append(struct.unpack_from(">f", data, offset)[0])
            offset += 4
        elif tag == "d":
            _require(data, offset, 8)
            args.append(struct.unpack_from(">d", data, offset)[0])
            offset += 8
        elif tag == "h":
            _require(data, offset, 8)
            args.append(struct.unpack_from(">q", data, offset)[0])
            offset += 8
        elif tag == "c":
            _require(data, offset, 4)
            value = struct.unpack_from(">i", data, offset)[0]
            if not (0 <= value <= 0x10FFFF):
                raise OSCError("char argument out of range")
            args.append(chr(value))
            offset += 4
        elif tag == "s":
            text, offset = _read_padded_string(data, offset)
            args.append(text)
        elif tag == "b":
            _require(data, offset, 4)
            size = struct.unpack_from(">i", data, offset)[0]
            offset += 4
            if size < 0:
                raise OSCError("negative blob size")
            _require(data, offset, _pad4(size))
            args.append(bytes(data[offset:offset + size]))
            offset += _pad4(size)
        elif tag == "T":
            args.append(True)
        elif tag == "F":
            args.append(False)
        elif tag == "N":
            args.append(None)
        else:
            raise OSCError(f"unsupported type tag: {tag!r}")

    if offset != len(data):
        if any(data[offset:]):
            raise OSCError("non-zero trailing data after message")
        raise OSCError("trailing padding after message")
    return address, args


def _require(data: bytes, offset: int, count: int) -> None:
    if offset < 0 or offset + count > len(data):
        raise OSCError("argument runs past end of packet")


def parse_packet(data: bytes) -> List[Message]:
    """Parse a packet that is either one message or a bundle of messages.

    Bundles are unpacked recursively; nesting depth and element sizes are
    strictly checked.
    """
    if not data:
        raise OSCError("empty packet")
    if len(data) > MAX_PACKET_SIZE:
        raise OSCError("packet exceeds maximum size")
    if data.startswith(b"#bundle"):
        return _parse_bundle(data, depth=0)
    return [parse_message(data)]


def _parse_bundle(data: bytes, depth: int) -> List[Message]:
    if depth > 4:
        raise OSCError("bundle nesting too deep")
    if len(data) % 4 != 0:
        raise OSCError("bundle length is not a multiple of 4")
    _, offset = _read_padded_string(data, 0)  # "#bundle"
    _require(data, offset, 8)  # timetag
    offset += 8
    messages: List[Message] = []
    while offset < len(data):
        _require(data, offset, 4)
        size = struct.unpack_from(">i", data, offset)[0]
        offset += 4
        if size <= 0 or size % 4 != 0:
            raise OSCError("invalid bundle element size")
        _require(data, offset, size)
        element = data[offset:offset + size]
        offset += size
        if element.startswith(b"#bundle"):
            messages.extend(_parse_bundle(element, depth + 1))
        else:
            messages.append(parse_message(element))
    if offset != len(data):
        raise OSCError("bundle has trailing data")
    return messages
