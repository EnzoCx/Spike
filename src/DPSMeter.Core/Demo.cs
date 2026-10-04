namespace DPSMeter.Core;

public static class Demo
{
    public static CombatLog Create()
    {
        Actor[] actors = [new("a", "Aster", "Gladiateur"), new("b", "Lyra", "Sorcier"),
            new("c", "Soren", "Rôdeur"), new("d", "Naya", "Clerc")];
        string[][] skills = [["Frappe de démonstration", "Enchaînement de démonstration"],
            ["Flamme de démonstration", "Éclat de démonstration"],
            ["Flèche de démonstration", "Salve de démonstration"],
            ["Lumière de démonstration", "Châtiment de démonstration"]];
        var random = new Random(42);
        var hits = new List<Hit>();
        for (var second = 0; second < 120; second++)
            for (var player = 0; player < actors.Length; player++)
                hits.Add(new($"hit-{second}-{player}", second * 1000, actors[player].Id,
                    skills[player][second % 2], random.Next(15_000, 30_000) * (4 - player), second % 4 == 0));
        return new(1, "demo-120s", "Global", "synthetic-v1", "Gardien d'entraînement — fictif",
            "Démonstration", 120_000, true, actors, hits.ToArray());
    }
}
