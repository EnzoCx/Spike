# Source attribution

Selected Aion2 protocol/capture sources, DamageEvent and ICombatSource from
https://github.com/SkeeveAN/Aion-DPS-Meter
Commit: 2f237fed139fe4a173c9de9d43cdeb0eb52453f8
Copyright (c) 2026 SkeeveTV — MIT, retained in LICENSE.

No upstream updater, uploader, UI, launcher or process-elevation code is included.
Changes: disable frame dumps, expose counters, bound pending queue, restrict capture
to configured game ports, extract embedded lookup data to this app's own cache.
The unused raw SegmentRecording utility is excluded from our source distribution.
Game identifiers and localized names remain the property of NCSOFT. Separate desktop
artwork is documented in GameArt/CREDITS.txt and the root THIRD-PARTY-NOTICES.md.

Spike 0.5.2: host cache references renamed from DPSMeter.Engine to Spike.Engine;
upstream namespaces and protocol behavior unchanged.

Spike 0.5.7 behavioral changes:
- Remove class-only/nearby-cast summon ownership and class/skill-frequency identity guesses.
- Accept explicit owner IDs or unique observed owner names; freeze attribution at receipt,
  retaining original source IDs and evidence in DamageEvent. No retroactive reassignment.
- Clear ownership at every spawn, invalidate conflicting names and reset contextual evidence
  on local character records / decoded zone changes. Expire roster evidence against observed time.
- Count observed positive-to-zero HP transitions, deduplicating repeated zeros. Missing HP
  remains unknown; this is not complete death detection. No kill-opcode completeness claimed.

Spike 0.5.11 beta behavior:
- The decoder calls the independently authored host `BetaBuffTracker` for candidate
  2A38/2B38 frames and attaches an optional frozen contribution to damage events.
- Pause and detected stream desynchronization clear aura evidence. Original damage
  remains unchanged. Only two aura families are modeled with a provisional 10.5%
  level-one final multiplier; no Global protocol or accuracy validation is claimed.
- Wire-format research references and limitations: docs/RDPS-RESEARCH.md and docs/RDPS.md.
  No GPL source files were incorporated; no raw packet recording was enabled.
