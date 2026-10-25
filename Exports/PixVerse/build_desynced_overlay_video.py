"""Offset and gently slow the eleven approved button animations.

The output is still one 1280x720 H.264 video, so Unity uses one decoder.
"""

import json
import random
import subprocess
from pathlib import Path

from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Exports/PixVerse/character_pixverse_motion_no_sweep_preview.mp4"
OUTPUT = ROOT / "Exports/PixVerse/OverlayButtonsAsync.mp4"
LAYOUT = ROOT / "Exports/PixVerse/overlay_buttons_720p_layout.json"
FFMPEG = Path(r"D:\programs\KMPlayer\ffmpeg.exe")
WIDTH, HEIGHT, FPS, SOURCE_FRAMES = 1280, 720, 30, 90
OUTPUT_FRAMES = 120  # 4 seconds at 30 fps; source lasts 3 seconds.
FRAME_BYTES = WIDTH * HEIGHT * 3


def decode_frames():
    command = [str(FFMPEG), "-v", "error", "-i", str(SOURCE),
               "-f", "rawvideo", "-pix_fmt", "rgb24", "-vsync", "0", "-"]
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    frames = []
    try:
        while True:
            frame = process.stdout.read(FRAME_BYTES)
            if not frame:
                break
            if len(frame) != FRAME_BYTES:
                raise RuntimeError("Incomplete decoded frame")
            frames.append(frame)
    finally:
        process.stdout.close()
    errors = process.stderr.read().decode("utf-8", "replace")
    if process.wait() or len(frames) != SOURCE_FRAMES:
        raise RuntimeError(f"Decode failed: {len(frames)} frames; {errors}")
    return frames


def fade_mask(width, height):
    mask = Image.new("L", (width, height), 0)
    # Keep the original cell edges still; blend just outside the icon.
    from PIL import ImageDraw
    ImageDraw.Draw(mask).rectangle((12, 8, width - 13, height - 9), fill=255)
    return mask.filter(ImageFilter.GaussianBlur(6))


def main():
    buttons = json.loads(LAYOUT.read_text(encoding="utf-8"))["buttons"]
    if len(buttons) != 11 or not SOURCE.exists() or not FFMPEG.exists():
        raise RuntimeError("Expected source video, FFmpeg and eleven layout cells")

    frames = decode_frames()
    # Spread the starting phases across the source cycle, then assign them
    # to buttons in a fixed random order so the result is reproducible.
    phases = list(range(0, 88, 8))
    random.Random(251024).shuffle(phases)
    # These two launchers are visible before the menu opens. Start them at
    # moving parts of the approved source instead of its quiet intervals.
    for index, button in enumerate(buttons):
        if button["name"] in ("menu_launcher", "calculator_launcher"):
            phases[index] = 30
    print(dict(zip((button["name"] for button in buttons), phases)))
    masks = [fade_mask(cell[2] - cell[0], cell[3] - cell[1])
             for cell in (button["cell"] for button in buttons)]
    still = Image.frombytes("RGB", (WIDTH, HEIGHT), frames[0])

    temporary_output = OUTPUT.with_name("OverlayButtonsAsync.building.mp4")
    command = [str(FFMPEG), "-y", "-v", "error", "-f", "rawvideo",
               "-pix_fmt", "rgb24", "-s", f"{WIDTH}x{HEIGHT}", "-r", str(FPS),
               "-i", "-", "-an", "-c:v", "libx264", "-preset", "medium",
               "-crf", "19", "-pix_fmt", "yuv420p", "-profile:v", "baseline",
               "-level", "3.1", "-movflags", "+faststart", str(temporary_output)]
    encoder = subprocess.Popen(command, stdin=subprocess.PIPE, stderr=subprocess.PIPE)
    try:
        for output_frame in range(OUTPUT_FRAMES):
            image = still.copy()
            for button, phase, mask in zip(buttons, phases, masks):
                bounds = tuple(button["cell"])
                source_position = output_frame * SOURCE_FRAMES / OUTPUT_FRAMES + phase
                first = int(source_position) % SOURCE_FRAMES
                second = (first + 1) % SOURCE_FRAMES
                blend = source_position - int(source_position)
                first_cell = Image.frombytes("RGB", (WIDTH, HEIGHT), frames[first]).crop(bounds)
                second_cell = Image.frombytes("RGB", (WIDTH, HEIGHT), frames[second]).crop(bounds)
                old_cell = image.crop(bounds)
                new_cell = Image.blend(first_cell, second_cell, blend)
                image.paste(Image.composite(new_cell, old_cell, mask), bounds)
            encoder.stdin.write(image.tobytes())
    finally:
        encoder.stdin.close()
    errors = encoder.stderr.read().decode("utf-8", "replace")
    if encoder.wait():
        raise RuntimeError("Encode failed: " + errors)
    temporary_output.replace(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
