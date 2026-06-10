# Blood Hunter Asset Handoff

This file describes the current image workflow so another Codex chat can continue without guessing.

## Project Paths

- Workspace: `D:\dnd2.0test\dnd`
- Source assets: `D:\dnd2.0test\dnd\Assets\newUI\bloodHunter`
- Final/demo outputs: `D:\dnd2.0test\dnd\Assets\newUI\bloodHunter\PromptToAssetDemo`
- Main style reference image from user: `C:\Users\Tom\Desktop\image(1).png`
- Built-in image generation outputs may appear under `C:\Users\Tom\.codex\generated_images\...`, but that is only a temporary generator cache. Copy/save every usable project asset into `D:\dnd2.0test\dnd\Assets\newUI\bloodHunter\PromptToAssetDemo` before considering it done.

## User Preferences

- Continue one image at a time.
- Do not batch-generate/recolor everything. The batch attempt looked like a red filter and should be treated as a rough mistake.
- Do not make SVG assets, do not use PromptToAsset for generation, and do not use `_codex_tmp` for final work.
- For transparent/alpha UI assets, prefer native transparent PNG generation only. Do not use green chroma-key backgrounds; they leave colored fringes. If native alpha fails, generate on a pure flat white `#ffffff` background so the user can cut it out manually.
- Save normal large PNG files, not tiny original-size previews.
- Do not shrink final outputs to the source UI size. Use about `4x` the source dimensions unless the user says otherwise.
- Preserve the function of each original UI element:
  - If it is a filled panel, keep the center filled.
  - If it is an HP fill, make only the fill strip, not a full bar.
  - If it is a bar frame, leave the internal channel dark/empty so a fill can sit inside.
  - If it is a button, make a button, not a label.
  - If the original has no alpha, usually keep it as a filled square/background with no alpha.
- Style should match the Blood Hunter reference: mostly black/charcoal, scratched iron, dark leather/stone surfaces, dim worn copper edges, subtle crimson cracks/grooves.
- Keep the images cleaner and lower-noise than the first background attempt: larger readable silhouettes, broader calm dark surfaces, fewer tiny scratches/chains/branches/ornaments, and enough quiet space for UI overlays.
- Avoid: cartoon, mobile glossy, Minecraft/blocky, low-poly, full red recolor, bright blue/teal fantasy glow unless the source is explicitly a scene that needs light.

## Good Prompt Pattern

Use the current source image as shape/function reference. Prompt the image generator like:

```text
Create one high-resolution raster PNG game UI asset based on the shown source.
Keep the same function and proportions: <describe what the UI element is>.
Style must match the Blood Hunter UI reference: mostly black and charcoal, blackened scratched iron, dark matte leather/stone, dim worn copper edge highlights, subtle crimson cracks only in grooves, low brightness, gritty aged Dungeons & Dragons interface.
Do not make the whole asset red. No text, no watermark, no cartoon, no pixel art, no Minecraft/blocky geometry, no glossy mobile-game look.
```

For alpha assets, add:

```text
Transparent outside the asset only. Center must remain filled if the source center is filled.
No checkerboard. Do not draw a transparency preview pattern; the outside pixels must be actual alpha.
```

For non-alpha backgrounds/panels, add:

```text
This is a filled square/background asset, not a transparent cutout.
```

## Post-Processing Workflow

1. Inspect source with `view_image`.
2. Read source dimensions with `sharp`.
3. Generate one raster PNG with `image_gen`.
4. Find the latest generated image in the current `C:\Users\Tom\.codex\generated_images\...` run folder only as a temporary source.
5. For alpha assets, reject fake checkerboard/chroma-key results. Use native transparent PNG output; do not cut out chroma-key backgrounds.
   If native transparent output is unavailable, use a pure flat white background, not green.
6. Resize final output to approximately `source width * 4` and `source height * 4`.
7. Save/copy the final asset into `Assets\newUI\bloodHunter\PromptToAssetDemo`. Do not treat `.codex\generated_images` as a final asset folder.
8. Show the result with `view_image`.

Useful Node/sharp module path:

```js
const sharp = require("C:/Users/Tom/AppData/Roaming/npm/node_modules/prompt-to-asset/node_modules/sharp");
```

## Checkerboard Removal Pattern

Do not use this for new alpha assets unless the user explicitly allows cleanup. The user prefers direct transparent output because chroma-key/checkerboard cleanup wastes time and can leave edge artifacts.

Use this kind of logic, not a global white delete, so bright parts of the asset are not destroyed:

```js
function bg(d, i) {
  const r = d[i], g = d[i + 1], b = d[i + 2];
  const mx = Math.max(r, g, b), mn = Math.min(r, g, b);
  return mn > 184 && (mx - mn) < 18;
}

// Flood fill from image edges through bg pixels only.
// Set those seen pixels to alpha 0.
// Keep all other pixels alpha 255.
```

## Completed Final-ish Assets

These are the better one-by-one outputs. Prefer these over the batch folders.

- `blood_hunter_check_slot_filled_11_final.png`  
  Source: `_59b7a82e-e760-4963-9141-913d1f8420c92.png`  
  Purpose: circular filled slot where a checkmark can sit.

- `blood_hunter_spiked_check_slot_filled_12.png`  
  Source: `_65bde603-972f-4901-a6e0-ab37e2a4115f1.png`  
  Purpose: spiked filled circular slot.

- `blood_hunter_horizontal_nameplate_13.png`  
  Source: `_65bde603-972f-4901-a6e0-ab37e2a4115f2.png`  
  Purpose: horizontal button/nameplate.

- `blood_hunter_vertical_panel_button_14.png`  
  Source: `_65bde603-972f-4901-a6e0-ab37e2a4115f3 1.png`  
  Purpose: vertical panel with bottom button.

- `blood_hunter_vertical_panel_full_15.png`  
  Source: `_65bde603-972f-4901-a6e0-ab37e2a4115f3.png`  
  Purpose: vertical filled panel without bottom button.

- `blood_hunter_skull_icon_full_16.png`  
  Source: `_706e47a1-617d-47af-9d3c-0e59290ddaa4.png`  
  Purpose: skull icon. Kept bone/grey, not red.

- `blood_hunter_square_panel_full_17.png`  
  Source: `_76fe2649-8ccf-4b06-aabe-435786d7e10a.png`  
  Purpose: square filled panel/frame, no alpha.

- `blood_hunter_dark_square_texture_full_18.png`  
  Source: `_76fe2649-8ccf-4b06-aabe-435786d7e10a3.png`  
  Purpose: plain dark square panel texture, no frame, no alpha.

- `blood_hunter_hp_fill_red_full_19.png`  
  Source: `_7a10695d-9552-4174-9263-5c873eb8a2dc1.png`  
  Purpose: red HP bar fill only, not a frame.

- `blood_hunter_hp_bar_frame_inset_red_cross_full_20_v3.png`  
  Source: `_7a10695d-9552-4174-9263-5c873eb8a2dc2.png`  
  Purpose: HP/resource bar frame. Use v3: red cross kept from original silhouette, not low-poly.

- `blood_hunter_background_scene_full_21_v2_clean.png`  
  Source: `_9432f3fd-3f1a-4a98-a5b2-e5c058bc4b64.jpg`  
  Purpose: square Blood Hunter background scene. Prefer v2 clean over the first `_21` version because it has less visual noise and fewer tiny decorations.

- `blood_hunter_heart_token_full_22.png`  
  Source: `_b1bb8615-c2b0-48ef-926b-a222f1efb582.png`  
  Purpose: round heart/life token with transparent outside alpha. This one is already saved in `PromptToAssetDemo`; use it as the current good result.

- `blood_hunter_heart_icon_white_bg_23.png`  
  Source: `_be4d685f-04a7-4f4c-98f7-63f2e3246367.png`  
  Purpose: standalone heart icon on pure white background for manual cutout. Prefer this over `blood_hunter_heart_icon_full_23.png`, which is a bad local recolor.

- `blood_hunter_long_nameplate_white_bg_24.png`  
  Source: `_c72d57da-6adc-4d06-b635-a4a5680198fc.png`  
  Purpose: long horizontal nameplate/input bar. Use the white-bg version; do not prefer the `.rgba.png` cleanup because it left white strips.

- `blood_hunter_convex_button_white_bg_25.png`  
  Source: `_ca5ab790-13a1-483e-b789-dcb3f99c6d39111.png`  
  Purpose: compact raised/convex button on pure white background. This source is a raised button, not an input field; future prompts must say convex/raised button and avoid recessed/inset center.

Do not use these bad attempts:

- `blood_hunter_heart_icon_full_23.png`: bad local recolor.
- `blood_hunter_short_input_white_bg_25.png`: wrong function; it reads as a recessed input, but the source should be a raised convex button.
- `blood_hunter_short_nameplate_transparent_25/` and `_25_v2/`: bad SVG/PromptToAsset attempts. Do not use SVG for this workflow.

## Rough/Do Not Prefer

- `batch_blood_hunter_restyle`
- `batch_blood_hunter_restyle_4x_preview`
- `batch_blood_hunter_restyle_fullsize`

These were automated recolors and looked like a red filter over everything. Do not use as final style.

Also be careful with these older tests:

- `blood_hunter_checkmark_ref_style_10.png` and SVG variants: user disliked the checkmark direction and said to skip the checkmark.
- `blood_hunter_hp_bar_frame_inset_red_cross_full_20.png` and `_v2.png`: cross looked like a flat/low-poly plus. Prefer `_v3`.
- `blood_hunter_segmented_bar_full_19.png` and `blood_hunter_hp_fill_segmented_full_19.png`: superseded by `blood_hunter_hp_fill_red_full_19.png`.

## Current Continuation Point

The next source file to work on is:

```text
D:\dnd2.0test\dnd\Assets\newUI\bloodHunter\_9432f3fd-3f1a-4a98-a5b2-e5c058bc4b64.jpg
```

Source properties:

```text
1024x1024, alpha=false
```

It is a square fantasy background/scene, not a button or frame. The user interrupted before generation. Continue from here.

Suggested prompt idea:

```text
Create one high-resolution square background scene based on the shown source: a gothic Blood Hunter menu/background scene. Keep the same function: square illustrated background, no UI overlay, no text, no transparent cutout. Replace the blue magical tone with a dark Blood Hunter mood: black stone, grim gothic doorway/ritual chamber, subtle red moon/blood glow, low brightness, dark trees or iron architecture, worn copper/dark red accents. Do not make it a UI frame, button, or red filter. No text, no watermark, no cartoon, no glossy mobile-game look.
```

Save as something like:

```text
D:\dnd2.0test\dnd\Assets\newUI\bloodHunter\PromptToAssetDemo\blood_hunter_background_scene_full_21.png
```

Since source is `1024x1024`, final should be around `4096x4096` unless too heavy; `2048x2048` is also acceptable if generation/copy size becomes annoying.
