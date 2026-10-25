"""Pack the approved overlay button video into small, instantly playable UI atlases."""

import subprocess
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Exports/PixVerse/OverlayButtonsAsync.mp4"
OUTPUT = ROOT / "Assets/Resources"
FFMPEG = Path(r"D:\programs\KMPlayer\ffmpeg.exe")
SOURCE_WIDTH, SOURCE_HEIGHT = 1280, 720
ICON_SIZE = 96
ICON_COUNT = 11
FRAMES_PER_ATLAS = 40
FRAME_COUNT = 120
CROPS = [
    (92, 47, 174, 173),      # Character
    (402, 47, 169, 174),     # Wild shape
    (706, 49, 174, 170),     # Magic
    (1013, 499, 174, 173),  # Character information
    (92, 273, 174, 173),     # Spellbook
    (400, 273, 173, 174),    # Inventory
    (706, 273, 174, 174),    # Album
    (1013, 274, 174, 171),  # Main menu
    (93, 499, 173, 174),     # Reset
    (399, 500, 174, 171),    # Menu launcher
    (706, 499, 174, 174),    # Calculator launcher
]


def main():
    if not SOURCE.exists() or not FFMPEG.exists():
        raise RuntimeError("Overlay video or FFmpeg is missing")

    atlases = [Image.new("RGB", (ICON_COUNT * ICON_SIZE, FRAMES_PER_ATLAS * ICON_SIZE))
               for _ in range(FRAME_COUNT // FRAMES_PER_ATLAS)]
    frame_bytes = SOURCE_WIDTH * SOURCE_HEIGHT * 3
    process = subprocess.Popen(
        [str(FFMPEG), "-v", "error", "-i", str(SOURCE),
         "-f", "rawvideo", "-pix_fmt", "rgb24", "-"],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    )
    try:
        for frame_index in range(FRAME_COUNT):
            pixels = process.stdout.read(frame_bytes)
            if len(pixels) != frame_bytes:
                raise RuntimeError(f"Only {frame_index} frames decoded")
            frame = Image.frombytes("RGB", (SOURCE_WIDTH, SOURCE_HEIGHT), pixels)
            atlas = atlases[frame_index // FRAMES_PER_ATLAS]
            row = frame_index % FRAMES_PER_ATLAS
            for column, (x, y, width, height) in enumerate(CROPS):
                icon = frame.crop((x, y, x + width, y + height))
                icon = icon.resize((ICON_SIZE, ICON_SIZE), Image.Resampling.LANCZOS)
                atlas.paste(icon, (column * ICON_SIZE, row * ICON_SIZE))
    finally:
        process.stdout.close()
    errors = process.stderr.read().decode("utf-8", "replace")
    if process.wait():
        raise RuntimeError(errors)

    for index, atlas in enumerate(atlases):
        target = OUTPUT / f"OverlayButtonsAtlas{index}.png"
        atlas.save(target, optimize=True)
        print(f"{target}: {target.stat().st_size / 1024 / 1024:.2f} MiB")


if __name__ == "__main__":
    main()
