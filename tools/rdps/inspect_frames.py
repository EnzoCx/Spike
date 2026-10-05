"""Offline research only: inspect candidate buff frames, never capture traffic.

Input: JSONL records with atMs and frameHex (opcode first, length prefix removed,
bundles already expanded). No production parser or game coefficient is supplied.
See docs/RDPS-RESEARCH.md for the protocol hypothesis and its provenance.
"""

import argparse
from collections import Counter
import json
from pathlib import Path
import struct

BUFF_OPCODES = {0x2A38, 0x2B38}
MAX_FRAME_BYTES = 16_384
MAX_FILE_BYTES = 64 * 1024 * 1024
MAX_RECORDS = 250_000
MAX_SAMPLES = 1_000


def candidate_buff(frame):
    """Return a structural candidate only. Matching bytes do not validate semantics."""
    if len(frame) < 2 or len(frame) > MAX_FRAME_BYTES:
        raise ValueError("invalid frame length")
    opcode = int.from_bytes(frame[:2], "big")
    if opcode not in BUFF_OPCODES:
        return None
    position = 2

    def read(fmt):
        nonlocal position
        size = struct.calcsize(fmt)
        if position + size > len(frame):
            raise ValueError("truncated candidate")
        value = struct.unpack_from(fmt, frame, position)[0]
        position += size
        return value

    def varuint():
        value = 0
        for index in range(5):
            byte = read("B")
            if index == 4 and byte > 15:
                raise ValueError("varuint exceeds 32 bits")
            value |= (byte & 127) << (7 * index)
            if byte < 128:
                if index > 0 and byte == 0:
                    raise ValueError("noncanonical varuint")
                return value
        raise ValueError("unterminated varuint")

    target = varuint()
    flag = read("B")
    kind = read("B")
    instance = varuint()
    effect = read("<I")
    duration = read("<I")
    unknown = read("<I")
    clock = read("<I")
    caster = varuint()
    return {
        "opcode": f"0x{opcode:04X}", "target": target, "caster": caster,
        "flag": flag, "kind": kind, "instanceCandidate": instance,
        "effectId": effect, "catalogIdCandidate": effect // 10,
        "durationMsCandidate": duration, "unknownWord": unknown,
        "clockCandidate": clock, "unparsedBytes": len(frame) - position,
    }


def inspect(path):
    """Keep raw frames and original entity identifiers out of the generated report."""
    if path.stat().st_size > MAX_FILE_BYTES:
        raise ValueError("input exceeds 64 MiB")
    counts = Counter()
    opcodes = Counter()
    aliases = {}
    samples = []
    previous_time = -1
    consumed = 0
    with path.open("rb") as stream:
        for index in range(MAX_RECORDS + 1):
            line = stream.readline(MAX_FRAME_BYTES * 2 + 1025)
            if not line:
                break
            consumed += len(line)
            if index == MAX_RECORDS or consumed > MAX_FILE_BYTES:
                raise ValueError("input exceeds record or byte limit")
            if len(line) > MAX_FRAME_BYTES * 2 + 1024:
                raise ValueError(f"record {index + 1}: line too long")
            try:
                record = json.loads(line)
                if not isinstance(record, dict):
                    raise ValueError()
                at = record["atMs"]
                encoded = record["frameHex"]
                if type(at) is not int or not 0 <= at <= 86_400_000 or at < previous_time:
                    raise ValueError()
                if not isinstance(encoded, str) or len(encoded) > MAX_FRAME_BYTES * 2:
                    raise ValueError()
                frame = bytes.fromhex(encoded)
                if not 2 <= len(frame) <= MAX_FRAME_BYTES:
                    raise ValueError()
            except (ValueError, KeyError, TypeError):
                raise ValueError(f"record {index + 1}: invalid frame record") from None
            previous_time = at
            counts["frames"] += 1
            opcode = int.from_bytes(frame[:2], "big")
            opcodes[f"0x{opcode:04X}"] += 1
            if opcode == 0xFFFF:
                counts["unexpandedBundles"] += 1
            try:
                candidate = candidate_buff(frame)
            except ValueError:
                counts["malformedCandidates"] += 1
                continue
            if candidate is None:
                continue
            counts["structuralCandidates"] += 1
            if candidate["unparsedBytes"]:
                counts["candidatesWithUnparsedBytes"] += 1
            if candidate["durationMsCandidate"] == 0:
                counts["zeroDurationCandidates"] += 1
            if not candidate["caster"] or not candidate["target"]:
                counts["candidatesWithUnknownIdentity"] += 1
            if len(samples) < MAX_SAMPLES:
                for field in ("target", "caster"):
                    entity = candidate[field]
                    candidate[field] = None if entity == 0 else aliases.setdefault(entity, f"E{len(aliases) + 1}")
                # Uninterpreted values are not needed in a shareable summary.
                for field in ("unknownWord", "clockCandidate", "instanceCandidate"):
                    del candidate[field]
                samples.append({"atMs": at, **candidate})
        else:
            raise ValueError("input exceeds record limit")
    return {
        "status": "research-only-unvalidated", "usableForRdps": False,
        "limitations": [
            "No Global validation; a structural match is not a confirmed buff.",
            "Removal, refresh, snapshot, level and stacking semantics are unknown.",
            "No coefficient, buff coverage guarantee or rDPS calculation.",
            "Entity aliases do not prove ownership or survive entity ID reuse.",
        ],
        "counts": dict(counts), "opcodes": dict(sorted(opcodes.items())),
        "samplesTruncated": counts["structuralCandidates"] > MAX_SAMPLES,
        "samples": samples,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path)
    args = parser.parse_args()
    try:
        report = inspect(args.input)
    except (ValueError, OSError) as error:
        # Do not echo filesystem paths or frame contents on failure.
        parser.exit(2, f"Cannot inspect input: {error if isinstance(error, ValueError) else 'file access failed'}\n")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
