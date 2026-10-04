# Working on DPSMeter

Read this file, `docs/HANDOFF.md`, and the files you intend to change before development.
Keep code simple (KISS), responsibilities explicit and changes reviewable.
Do not create a Git worktree unless the user explicitly requests one.
Do not delegate to other agents unless requested by the user or applicable instructions.

## Product and safety boundaries

- Windows desktop meter for AION 2 Global. French, English and Spanish; three themes.
- The user may be playing during development. Never stop/restart the game, move the cursor,
  steal focus, install drivers, change network settings or close their running meter.
- Passive capture using an already installed Npcap driver is the implemented method.
  This is not an assertion of publisher approval. No injection, game-memory access,
  packet modification, protection bypass or gameplay automation.
- Routine checks are offline and invisible. Live diagnostics require an explicit user
  request; never invoke `--verify-live` or `--probe` during an ordinary build/review.
- Never commit real fights, raw traffic, settings, screenshots of private sessions,
  secrets, credentials, certificates, crash dumps or local machine paths.
- No telemetry or automatic uploads. A future community website is a separate feature.
- Do not add paid services, infrastructure or driver redistribution without authorization.

## Architecture

- `DPSMeter.Core`: encounter models, calculations, validation, storage, source classification.
- `DPSMeter.Engine`: passive acquisition, upstream decoder and game lookup catalogs.
- `DPSMeter.Desktop`: WPF application, report, history, overlay, settings, presentation.
- `*.Verification.cs`: offscreen UI regression checks, kept apart from interaction logic.
- `Vendor/`: upstream MIT code. Keep attribution and document behavioral changes in ORIGIN.md.
- `Combat.cs` / `Demo.cs` / the v1 sample: legacy prototype fixtures, not the live import format.

## Behavioral invariants

- Preserve existing version 2 fight files and preference defaults. New metadata is optional.
- Boss DPS uses the first-to-last outgoing damage window of that target, minimum one second.
  Waiting for the 12-second inactivity timeout must not dilute DPS.
- Unidentified sources retain damage, timing and details. Never guess owners from class alone,
  delete their events or claim a confirmed five-person roster from a display heuristic.
- History selection must remain stable while live capture updates.
- Keep overlay rows stable during refresh so hover details do not reset each second.
- All UI text uses `Text.cs`; implement each change in FR/EN/ES and the three themes.
- Never show synthetic data as a real fight. Public previews use the labeled demo only.
- Branding: see `.impeccable.md`, `brand/README.md`; native resources, no runtime image fetches.

## Required verification

On Windows with .NET 9 SDK, run `powershell -NoProfile -File tools/Verify.ps1`.
It restores locked dependencies, builds, checks core/protocol behavior, publishes and
runs the invisible UI checks. It never starts capture. Results go to ignored `artifacts/`.
For formatting use `dotnet format DPSMeter.sln --exclude src/DPSMeter.Engine/Vendor`.
Avoid unrelated upstream formatting changes. Keep CI and package lock files current.

After changes, report what changed, checks actually run and unresolved limitations.
Do not claim exhaustive protocol correctness or guaranteed CGU compliance.

## Git and publication

The initial public publication is authorized by the project owner. This does not authorize
future deployments, paid services, messages to others or automatic publication of fights.
Review the staged files before pushing. Preserve third-party notices: our MIT license does
not relicense NCSOFT artwork/catalogs, PacketDotNet (MPL-2.0), or OFL fonts.
Use concise commits that explain the resulting behavior; never commit build outputs.
