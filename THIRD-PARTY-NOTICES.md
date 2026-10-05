# Third-party notices

The repository's MIT license covers original Spike code and branding. It does not
relicense third-party components, game artwork or game data.

| Component | Version/source | License / notice |
| --- | --- | --- |
| Protocol and capture source | SkeeveAN/Aion-DPS-Meter, commit 2f237fed139fe4a173c9de9d43cdeb0eb52453f8 | MIT; retained in src/Spike.Engine/Vendor/LICENSE |
| SharpPcap | NuGet 6.3.0 | MIT; Tamir Gal, Chris Morgan and contributors; https://github.com/dotpcap/sharppcap |
| PacketDotNet | NuGet 1.4.7 | MPL-2.0; Chris Morgan and contributors; source: https://github.com/dotpcap/packetnet/tree/v1.4.7 |
| .NET runtime / Microsoft runtime packages | Versions resolved in packages.lock.json | MIT and accompanying runtime notices; https://github.com/dotnet/runtime |
| Barlow and Barlow Condensed | Bundled unmodified fonts | SIL OFL 1.1; full notices in src/Spike.Desktop/Fonts |
| AION 2 artwork and catalogs | Public NotMeter resources and upstream catalogs | Original rights retained by NCSOFT / respective owners; not licensed under our MIT grant |

PacketDotNet is used unmodified as a separate dependency. Its source and license remain
available from the upstream repository and the NuGet package metadata. MPL-2.0 text:
https://www.mozilla.org/en-US/MPL/2.0/

The .NET self-contained publication also includes runtime components. Keep their notices
when redistributing builds. Npcap itself is not bundled or installed by this repository.

For the artwork's exact URLs, transformations and SHA-256 hashes, see
`src/Spike.Desktop/GameArt/sources.json` and `CREDITS.txt`.
No affiliation with or endorsement by NCSOFT or NotMeter is claimed. Public availability
of artwork is not represented here as an MIT license or a redistribution permission.
