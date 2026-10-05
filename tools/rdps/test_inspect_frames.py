"""Synthetic protocol hypotheses only; no captured bytes or game identifiers."""

import json
from pathlib import Path
import struct
import tempfile
import unittest

from inspect_frames import candidate_buff, inspect, MAX_SAMPLES


def varuint(value):
    result = bytearray()
    while value >= 128:
        result.append((value & 127) | 128)
        value >>= 7
    result.append(value)
    return result


def frame(opcode=0x2A38, target=300, caster=400, duration=10000):
    return (opcode.to_bytes(2, "big") + varuint(target) + bytes([0, 1])
            + varuint(7) + struct.pack("<IIII", 123456789, duration, 0, 42)
            + varuint(caster))


class CandidateChecks(unittest.TestCase):
    def report(self, records):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "synthetic.jsonl"
            path.write_text("\n".join(json.dumps(record) for record in records), encoding="utf-8")
            return inspect(path)

    def test_both_opcodes_and_endianness(self):
        for opcode in (0x2A38, 0x2B38):
            result = candidate_buff(frame(opcode))
            self.assertEqual((result["target"], result["caster"]), (300, 400))
            self.assertEqual(result["durationMsCandidate"], 10000)
            self.assertEqual(result["effectId"], 123456789)
            self.assertEqual(result["unparsedBytes"], 0)
        self.assertIsNone(candidate_buff(frame(0x382A)))

    def test_every_truncation_is_rejected(self):
        complete = frame()
        for length in range(len(complete)):
            with self.subTest(length=length), self.assertRaises(ValueError):
                candidate_buff(complete[:length])

    def test_uint32_boundaries(self):
        self.assertEqual(candidate_buff(frame(target=0xFFFFFFFF))["target"], 0xFFFFFFFF)
        for invalid in (b"\x80" * 5, b"\xff\xff\xff\xff\x10", b"\x80\x00"):
            with self.assertRaises(ValueError):
                candidate_buff(b"\x2a\x38" + invalid + bytes(32))

    def test_zero_duration_does_not_claim_removal(self):
        report = self.report([{"atMs": 0, "frameHex": frame(duration=0).hex()}])
        self.assertEqual(report["counts"]["zeroDurationCandidates"], 1)
        self.assertFalse(report["usableForRdps"])
        self.assertNotIn("removed", report["samples"][0])

    def test_report_never_contains_raw_frames_or_original_entity_ids(self):
        encoded = frame(target=123456, caster=654321).hex()
        report = self.report([{"atMs": 0, "frameHex": encoded}])
        serialized = json.dumps(report)
        self.assertNotIn(encoded, serialized)
        self.assertEqual(report["samples"][0]["target"], "E1")
        self.assertEqual(report["samples"][0]["caster"], "E2")
        self.assertNotIn("clockCandidate", serialized)

    def test_missing_identity_and_trailing_bytes_are_visible(self):
        report = self.report([{"atMs": 0, "frameHex": (frame(caster=0) + b"\x00").hex()}])
        self.assertEqual(report["counts"]["candidatesWithUnknownIdentity"], 1)
        self.assertEqual(report["counts"]["candidatesWithUnparsedBytes"], 1)
        self.assertIsNone(report["samples"][0]["caster"])

    def test_malformed_candidates_do_not_disappear_silently(self):
        report = self.report([{"atMs": 0, "frameHex": "2a38"}, {"atMs": 1, "frameHex": "ffff00"}])
        self.assertEqual(report["counts"]["malformedCandidates"], 1)
        self.assertEqual(report["counts"]["unexpandedBundles"], 1)

    def test_bad_input_is_rejected_without_echoing_content(self):
        for record in ({"atMs": True, "frameHex": "2a38"}, {"atMs": 0, "frameHex": "private"}, [], {},
                       {"atMs": -1, "frameHex": "2a38"}, {"atMs": 86_400_001, "frameHex": "2a38"}):
            with self.subTest(record=record), self.assertRaisesRegex(ValueError, "invalid frame record"):
                self.report([record])

    def test_out_of_order_time_is_rejected(self):
        with self.assertRaises(ValueError):
            self.report([{"atMs": at, "frameHex": "043800"} for at in (1, 0)])

    def test_samples_are_bounded_without_losing_counts(self):
        report = self.report([{"atMs": at, "frameHex": frame().hex()} for at in range(MAX_SAMPLES + 1)])
        self.assertEqual(len(report["samples"]), MAX_SAMPLES)
        self.assertEqual(report["counts"]["structuralCandidates"], MAX_SAMPLES + 1)
        self.assertTrue(report["samplesTruncated"])

    def test_empty_input_does_not_become_validated(self):
        self.assertFalse(self.report([])["usableForRdps"])


if __name__ == "__main__":
    unittest.main()
