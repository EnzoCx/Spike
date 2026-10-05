using AionDPS.Aion2.Protocol;
using Spike.Core;

namespace Spike.Desktop;

/// <summary>Invented combat data with real catalog skills, for labeled public previews only.</summary>
internal static class PublicPreviewFixture
{
    internal static Encounter Create()
    {
        Participant[] players =
        [
            new(1, "Aster", "Assassin", true),
            new(2, "Lyra", "Sorcerer", true),
            new(3, "Soren", "Ranger", true),
            new(4, "Naya", "Chanter", true),
            new(5, "Elian", "Cleric", true),
            new(100, "Training guardian", "", false, true)
        ];
        var events = new List<CombatEvent>();
        Add(1, 13350000, 640_000, 96);
        Add(1, 13130000, 510_000, 48);
        Add(1, 13040000, 360_000, 156);
        Add(1, 13110000, 290_000, 60);
        Add(1, 13010000, 260_000, 120);
        Add(1, 13030000, 220_000, 144);
        Add(2, 15010000, 2_120_000, 180);
        Add(3, 14010000, 1_950_000, 200);
        Add(4, 18010000, 1_710_000, 180);
        Add(5, 17010000, 1_280_000, 140);
        return new(2, Guid.Parse("57100000-0000-4000-8000-000000000001"),
            new DateTimeOffset(2026, 10, 5, 18, 0, 0, TimeSpan.Zero), "Global", "preview",
            "demo", "Training arena", "preview", 180_000, players, events.OrderBy(e => e.AtMs).ToArray());

        void Add(int player, int skill, long total, int hits)
        {
            if (GameArtwork.SkillSource(skill) is null)
                throw new InvalidOperationException($"Public preview skill {skill} has no embedded icon.");
            for (var i = 0; i < hits; i++)
                events.Add(new(i * 180_000L / (hits - 1), player, 100, skill, Aion2SkillNames.NameOf(skill),
                    total / hits + (i < total % hits ? 1 : 0), false, i % 4 == 0, false));
        }
    }
}
