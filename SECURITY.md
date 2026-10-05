# Security and privacy

Spike passively observes game traffic and stores derived combat records locally.
It has no automatic upload or telemetry. Raw traffic is not written by the application.
Local fight files contain character names; sharing them is a separate, explicit action.
Export replaces player names and entity IDs, but free-text metadata can still identify
someone. Inspect exports before sharing them.

## Reporting a vulnerability

Use GitHub private vulnerability reporting on this repository when available. Do not
attach raw traffic, tokens, account details or unredacted fights to a public issue.
If private reporting is unavailable, open a minimal issue requesting a private contact,
without exploit details or personal data.

The current main branch is maintained on a best-effort basis; there is no response-time
or security certification claim. Automated dependency checks complement manual review.

## Trust boundaries

Imports are untrusted. Size, text, duration, entity IDs and amounts are validated.
Imported records do not prove authenticity and must not silently become ranked results.
Game artwork/catalogs and the decoder can become outdated after a game update.
The app does not install Npcap, elevate itself or bypass game protections.
