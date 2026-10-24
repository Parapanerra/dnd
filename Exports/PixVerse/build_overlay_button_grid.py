"""Build a fixed 1280x720 grid from the overlay's actual Unity sprites."""

import json
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT = Path(__file__).resolve().parents[2]
OUTPUT = Path(__file__).resolve().parent
SPRITESHEET = PROJECT / "Assets/newUI/3156413215.png"
META = SPRITESHEET.with_suffix(".png.meta").read_text(encoding="utf-8")
SHEET = Image.open(SPRITESHEET).convert("RGBA")

# These IDs are the Image.m_Sprite references in the overlay scene instances.
# The menu launcher uses a scene override, not its prefab's default sprite.
BUTTONS = [
    ("character", -296671430),
    ("wild_shape", 93829037),
    ("magic", -2146064185),
    ("character_info", 1238176567),
    ("spellbook", 532315730),
    ("inventory", 1484865309),
    ("album", PROJECT / "Assets/newUI/AlbumOverlayButton.png"),
    ("main_menu", 1894582092),
    ("reset", -1698232919),
    ("menu_launcher", -931637994),
    ("calculator_launcher", PROJECT / "Assets/newUI/CalculatorOverlayButton.png"),
]


def source_image(reference):
    if isinstance(reference, Path):
        return Image.open(reference).convert("RGBA"), str(reference.relative_to(PROJECT))

    match = re.search(rf"\binternalID: {reference}\b", META)
    if not match:
        raise ValueError(f"Sprite {reference} missing from {SPRITESHEET}")
    rect_start = META.rfind("      rect:", 0, match.start())
    rect_text = META[rect_start : match.start()]
    dimensions = {}
    for key in ("x", "y", "width", "height"):
        value = re.search(rf"^        {key}: (\d+)$", rect_text, re.MULTILINE)
        if not value:
            raise ValueError(f"Missing {key} for sprite {reference}")
        dimensions[key] = int(value.group(1))
    x, y, width, height = (dimensions[key] for key in ("x", "y", "width", "height"))
    top = SHEET.height - y - height
    return SHEET.crop((x, top, x + width, top + height)), f"Assets/newUI/3156413215.png#{reference}"


canvas = Image.new("RGB", (1280, 720), (11, 13, 24))
review = canvas.copy()
review_draw = ImageDraw.Draw(review)
font_path = Path("C:/Windows/Fonts/arial.ttf")
font = ImageFont.truetype(str(font_path), 17) if font_path.exists() else ImageFont.load_default()
manifest = []

# Reserve the top-right cell for the PixVerse watermark. The character-info
# icon moves into the otherwise unused bottom-right cell.
POSITIONS = [(0, 0), (1, 0), (2, 0), (3, 2),
             (0, 1), (1, 1), (2, 1), (3, 1),
             (0, 2), (1, 2), (2, 2)]

for index, (name, reference) in enumerate(BUTTONS):
    column, row = POSITIONS[index]
    cell_x, cell_y = 36 + column * 307, 32 + row * 226
    cell = (cell_x, cell_y, cell_x + 287, cell_y + 204)

    original, source = source_image(reference)
    original.thumbnail((174, 174), Image.Resampling.LANCZOS)
    x = cell_x + (287 - original.width) // 2
    y = cell_y + (204 - original.height) // 2
    canvas.paste(original, (x, y), original)
    review.paste(original, (x, y), original)
    review_draw.text((cell_x + 9, cell_y + 7), f"{index + 1}. {name}", fill=(255, 255, 255), font=font)
    manifest.append({"name": name, "row": row + 1, "column": column + 1, "cell": list(cell), "source": source})

canvas.save(OUTPUT / "overlay_buttons_720p.png", optimize=True)
review.save(OUTPUT / "overlay_buttons_720p_review.png", optimize=True)
(OUTPUT / "overlay_buttons_720p_layout.json").write_text(
    json.dumps({"size": [1280, 720], "buttons": manifest}, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)
