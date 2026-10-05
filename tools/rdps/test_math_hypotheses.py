"""Executable counterexamples for the research note, not AION 2 damage rules.

Exact rational arithmetic isolates model ambiguity from numerical rounding.
These experiments must not be wired to a real fight or called a calibrated rDPS.
"""

from fractions import Fraction as F
import unittest


class AttributionHypotheses(unittest.TestCase):
    def test_single_known_final_multiplier_conserves_damage(self):
        actual = F(12000)
        personal = actual / F(6, 5)
        provided = actual - personal
        self.assertEqual((personal, provided), (10000, 2000))
        self.assertEqual(personal + provided, actual)
        # Subtracting 20% of the final hit would incorrectly credit 2400.
        self.assertNotEqual(provided, actual * F(1, 5))

    def test_nominal_additive_buff_is_not_a_final_multiplier(self):
        # Hypothetical additive damage-boost bucket, NOT a validated game rule.
        actual, nominal = F(12000), F(1, 5)
        without_buff = [actual * (1 + base) / (1 + base + nominal) for base in (F(1, 5), F(4, 5))]
        self.assertEqual(without_buff, [F(72000, 7), F(10800)])
        self.assertNotEqual(*without_buff)
        # Same observed hit and nominal buff produce different contributions.
        self.assertTrue(all(value != actual / (1 + nominal) for value in without_buff))

    def test_separate_marginal_removal_double_counts_overlap(self):
        # Two independent hypothetical multipliers: 1.1 and 1.2.
        base, a, b = F(10000), F(11, 10), F(6, 5)
        actual = base * a * b
        marginal_a, marginal_b = actual - actual / a, actual - actual / b
        self.assertEqual(actual - base, 3200)
        self.assertEqual(marginal_a + marginal_b, 3400)
        # Averaging both attribution orders is ONE convention, not a game fact.
        credit_a = (base * (a - 1) + base * b * (a - 1)) / 2
        credit_b = (base * (b - 1) + base * a * (b - 1)) / 2
        self.assertEqual((credit_a, credit_b), (1100, 2100))
        self.assertEqual(base + credit_a + credit_b, actual)


if __name__ == "__main__":
    unittest.main()
