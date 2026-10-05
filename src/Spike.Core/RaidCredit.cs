namespace Spike.Core;

// Compatibility with optional metadata written by versions 0.5.11–0.5.14.
// Retained for archive round trips only; never generated or used in damage calculations.
public sealed record RaidCredit(int Provider, int SkillId, long Bonus)
{
    public const string LegacyModel = "auras-level1-v1";
}
