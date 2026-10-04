namespace DPSMeter.Core;

/// <summary>Separates unresolved damage sources without dropping or reallocating their events.</summary>
public static class EncounterSources
{
    public static Encounter Classify(Encounter encounter)
    {
        // Older local records only retained a class inferred from skills, not spawn/owner evidence.
        // These families were observed on anonymous effect-only actors in the Atiel report.
        // They justify an "unidentified source" label, never assigning an owner by class alone.
        if (encounter.Origin != "live") return encounter;
        var eventsBySource = encounter.Events.ToLookup(e => e.Source);
        var changed = false;
        var participants = encounter.Participants.Select(actor =>
        {
            if (!actor.IsPlayer || actor.IsSelf || actor.IsBoss || actor.IsUnidentifiedSource
                || actor.Name != $"Player #{actor.Id}") return actor;
            var events = eventsBySource[actor.Id].ToArray();
            if (events.Length == 0 || events.Any(e => e.Heal || !IsEffectFamily(e.SkillId))) return actor;
            changed = true;
            return actor with { IsPlayer = false, IsUnidentifiedSource = true };
        }).ToArray();
        return changed ? encounter with { Participants = participants } : encounter;
    }

    private static bool IsEffectFamily(int skillId) => skillId / 10000 is 1539 or 1520 or 1417;
}
