"""Fetch the reviewed AION 2 Global artwork snapshot. Build-time only; requires Pillow.

Keeps source URLs and hashes. Existing hashes prevent silent upstream replacement.
The WebP atlas is only transcoded to PNG for Windows WPF; pixels are unchanged.
"""
from pathlib import Path
from urllib.request import Request, urlopen
from urllib.parse import quote
from io import BytesIO
from hashlib import sha256
import json
import re
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "src/Spike.Desktop/GameArt"
MANIFEST = DEST / "sources.json"
DEST.mkdir(parents=True, exist_ok=True)
previous = json.loads(MANIFEST.read_text("utf-8")) if MANIFEST.exists() else {}
sources = []

def fetch(url):
    with urlopen(Request(url, headers={"User-Agent": "Spike-artwork-build/0.4.1"}), timeout=30) as response:
        data = response.read(8 * 1024 * 1024 + 1)
    if len(data) > 8 * 1024 * 1024:
        raise ValueError("Artwork exceeds size limit")
    digest = sha256(data).hexdigest()
    old = next((s for s in previous.get("files", []) if s["url"] == url), None)
    if old and old["sha256"] != digest:
        raise ValueError(f"Source changed; review before updating: {url}")
    sources.append({"url": url, "sha256": digest, "bytes": len(data)})
    return data

catalog_url = "https://notmeter.com/assets/global-details/icons.json?v=20261004-ui-bindings"
atlas_url = "https://notmeter.com/assets/global-details/icons.webp?v=20261004-ui-bindings"
catalog_bytes = fetch(catalog_url)
catalog = json.loads(catalog_bytes)
atlas = Image.open(BytesIO(fetch(atlas_url)))
if atlas.width % catalog["columns"] or atlas.height % catalog["rows"]:
    raise ValueError("Atlas dimensions do not match catalog")
if atlas.width // catalog["columns"] != atlas.height // catalog["rows"]:
    raise ValueError("Expected square tiles")
for group in ("skills", "buffs"):
    if not all(0 <= index < catalog["columns"] * catalog["rows"] for index in catalog[group].values()):
        raise ValueError(f"Invalid tile in {group}")
(DEST / "catalog.json").write_bytes(catalog_bytes)
atlas.save(DEST / "atlas.png", optimize=True)
with Image.open(DEST / "atlas.png") as png:
    if png.convert("RGBA").tobytes() != atlas.convert("RGBA").tobytes():
        raise ValueError("Atlas transcode changed pixels")

# Names and PERFORMANCE_JOB_COLORS from the public NotMeter site, reviewed 2026-10-04.
classes = [
    ("Gladiator", "검성", "#69bcd0"), ("Templar", "수호성", "#84aef0"),
    ("Assassin", "살성", "#9ccc63"), ("Ranger", "궁성", "#5dc79a"),
    ("Sorcerer", "마도성", "#b392e8"), ("Spiritmaster", "정령성", "#d17acb"),
    ("Cleric", "치유성", "#e2c56d"), ("Chanter", "호법성", "#d9a95f"),
    ("Brawler", "권성", "#e48870"),
]
palette_url = "https://notmeter.com/assets/app.js?v=20261004-ranker-region"
palette_script = fetch(palette_url).decode("utf-8")
palette_match = re.search(r"const PERFORMANCE_JOB_COLORS\s*=\s*(\{.*?\});", palette_script, re.S)
if not palette_match:
    raise ValueError("Reviewed class palette is no longer present")
palette = json.loads(re.sub(r",\s*}", "}", palette_match.group(1)))
if any(palette.get(korean) != color for _, korean, color in classes):
    raise ValueError("Class colors changed; review before updating")
(DEST / "classes").mkdir(exist_ok=True)
for name, korean, _ in classes:
    data = fetch(f"https://notmeter.com/assets/jobs/{quote(korean)}.png")
    with Image.open(BytesIO(data)) as icon:
        if icon.format != "PNG" or max(icon.size) > 512:
            raise ValueError(f"Unexpected class artwork: {name}")
    (DEST / "classes" / f"{name}.png").write_bytes(data)
(DEST / "classes.json").write_text(json.dumps({name: color for name, _, color in classes}, indent=2) + "\n", encoding="utf-8")
manifest = {"snapshot": "2026-10-04", "owner": "AION 2 artwork: NCSOFT; source host: NotMeter",
    "paletteSource": palette_url,
    "paletteMeaning": "NotMeter class conventions, not an official NCSOFT color specification",
    "files": sources, "pngSha256": sha256((DEST / "atlas.png").read_bytes()).hexdigest()}
MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Saved {len(classes)} class icons; {len(catalog['skills'])} skill and {len(catalog['buffs'])} buff mappings; atlas {atlas.size}.")
