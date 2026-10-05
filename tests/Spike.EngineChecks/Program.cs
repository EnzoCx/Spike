using System.Text.Json;
using AionDPS.Aion2;
using AionDPS.Aion2.Capture;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat.Sources;

var protocol = Aion2Protocol.Load();
if (!protocol.IsCalibrated || !protocol.ServerPorts.SequenceEqual(new[] { 13328 }))
    throw new Exception("Embedded protocol configuration did not load.");
if (Aion2SkillNames.Load().Count < 100) throw new Exception("Skill catalog missing.");
Console.WriteLine("PASS embedded protocol and skill catalogs");
var reassembler = new TcpReassembler();
var layout = new FrameLayout(0, 2, true, true, 4, 2, 2, 1024);
var now = DateTime.Now;
var first = new TcpSegment(now, "server:13328", "local:50000", 100, new byte[] { 6, 0, 1 }, true);
if (reassembler.Push(first, layout).Count != 0) throw new Exception("Fragment prematurely decoded.");
if (reassembler.Push(first with { Sequence = 103, Payload = new byte[] { 0, 42, 43 } }, layout).Count != 1) throw new Exception("Fragment reassembly failed.");
if (reassembler.Push(first, layout).Count != 0) throw new Exception("Retransmit duplicated.");
Console.WriteLine("PASS fragmented TCP and retransmission handling");
// Regression frames from upstream MIT SelfCheckAion2.cs; no identity or network endpoints.
using (var replay = new Aion2PacketCombatSource(protocol))
{
    var frames = new[] {
        "04388484030600be08fe26a8004f02000002433baf4101000000ba4ffd020100",
        "04388484032600be08fe26a8005703000002433baf4101000000ba4fcc0401020100",
        "0438be080400be0857fcb2004a020792ea4501000000ba4f410100"
    };
    var wire = frames.SelectMany(hex => { var body = Convert.FromHexString(hex); return new[] { (byte)(body.Length + 4) }.Concat(body); }).ToArray();
    var segment = new TcpSegment(now, "server:13328", "local:50000", 900, wire, true);
    replay.Ingest(segment);
    var hits = replay.Poll(false).Damage;
    if (hits.Count != 3 || hits[0].Amount != 381 || hits[1].Amount != 588 || !hits[1].IsCritical || !hits[2].IsHeal || hits[2].Amount != 65)
        throw new Exception("Damage/critical/healing regression.");
    replay.Ingest(segment);
    if (!replay.Poll(false).IsEmpty) throw new Exception("Network duplicate counted twice.");
    replay.Ingest(segment with { Sequence = (uint)(900 + wire.Length) });
    if (!replay.Poll(true).IsEmpty || !replay.Poll(false).IsEmpty) throw new Exception("Pause replayed discarded events.");
    Console.WriteLine("PASS real protocol regression: damage, critical, heal, duplicates, pause");
}
if (!Aion2Lz4.TryDecompress(new byte[] { 0x30, 1, 2, 3 }, 3, out var literal) || !literal.SequenceEqual(new byte[] { 1, 2, 3 })
    || Aion2Lz4.TryDecompress(new byte[] { 0x11, 9, 5, 0 }, 10, out _) || Aion2Lz4.TryDecompress([], int.MaxValue, out _))
    throw new Exception("LZ4 bounds/regression.");
Console.WriteLine("PASS LZ4 decode and bounds");
LiveMeterChecks.Run(protocol);
BossAttemptChecks.Run(protocol);
EvidenceChecks.Run(protocol);
RaidChecks.Run(protocol);
if (args.Length == 0) return;
if (args.Length != 3 || args[0] != "--probe") throw new ArgumentException("--probe seconds output.json");
var seconds = Math.Clamp(int.Parse(args[1]), 5, 120);
using var source = new Aion2PacketCombatSource(protocol, adapterId: "all");
var state = SourceState.Idle;
source.StatusChanged += status => state = status.State;
source.Start();
long damage = 0, healing = 0, events = 0;
var actors = new HashSet<int>();
var stopAt = DateTime.UtcNow.AddSeconds(seconds);
while (DateTime.UtcNow < stopAt && state != SourceState.Error)
{
    await Task.Delay(500);
    foreach (var hit in source.Poll(false).Damage)
    {
        events++; actors.Add(hit.SourceObjectId);
        if (hit.IsHeal) healing += hit.Amount; else damage += hit.Amount;
    }
}
var report = new
{
    seconds,
    state = state.ToString(),
    source.Packets,
    source.Frames,
    source.Resyncs,
    source.DecodedEvents,
    source.CallbackErrors,
    source.DroppedEvents,
    events,
    actorCount = actors.Count,
    damage,
    healing,
    driverInstalled = NpcapAvailability.Detect().IsInstalled,
    rawPacketsSaved = false
};
File.WriteAllText(args[2], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(report));
