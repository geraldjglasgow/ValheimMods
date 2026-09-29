"""MD4 (RFC 1320) in plain Python: Unity derives a DLL script's fileID from it, and Python's hashlib dropped it with
OpenSSL 3. Only hashes short class names, so speed does not matter."""
import struct

_MASK = 0xFFFFFFFF
_ROUNDS = (
    (lambda x, y, z: (x & y) | (~x & z), 0x00000000, range(16), (3, 7, 11, 19)),
    (lambda x, y, z: (x & y) | (x & z) | (y & z), 0x5A827999, (0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15),
     (3, 5, 9, 13)),
    (lambda x, y, z: x ^ y ^ z, 0x6ED9EBA1, (0, 8, 4, 12, 2, 10, 6, 14, 1, 9, 5, 13, 3, 11, 7, 15), (3, 9, 11, 15)),
)


def md4(data):
    state = [0x67452301, 0xEFCDAB89, 0x98BADCFE, 0x10325476]
    padded = _pad(bytes(data))
    for offset in range(0, len(padded), 64):
        block = struct.unpack("<16I", padded[offset:offset + 64])
        start = list(state)
        for function, constant, order, shifts in _ROUNDS:
            _round(state, block, function, constant, order, shifts)
        state = [(s + t) & _MASK for s, t in zip(state, start)]
    return struct.pack("<4I", *state)


def _pad(data):
    padded = bytearray(data) + b"\x80"
    padded += b"\0" * ((56 - len(padded)) % 64)
    return bytes(padded + struct.pack("<Q", len(data) * 8))


def _round(state, block, function, constant, order, shifts):
    """Sixteen steps; each updates one word (a, d, c, b in turn) from the other three."""
    for step, index in enumerate(order):
        target = (-step) % 4
        a, b, c, d = (state[(target + k) % 4] for k in range(4))
        value = (a + function(b, c, d) + block[index] + constant) & _MASK
        shift = shifts[step % 4]
        state[target] = ((value << shift) | (value >> (32 - shift))) & _MASK
