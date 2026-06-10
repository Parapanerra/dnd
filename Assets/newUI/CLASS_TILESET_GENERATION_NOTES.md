# Class UI Tileset Generation Notes

This note documents the workflow that produced the cleaner Ranger V2 assets.
Use it as the baseline for the next DnD class themes.

## Current Good Output

Ranger clean version:

`Assets/newUI/ranger/RangerTilesets_NoSlice_V2_Clean/`

Files:

- `01_square_icon_tileset_transparent.png`
- `02_orb_token_tileset_transparent.png`
- `03_dice_buttons_d100_tileset_transparent.png`
- `04_bar_input_tileset_transparent.png`
- `05_empty_square_tileset_transparent.png`
- `06_feather_quill_map_tileset_transparent.png`
- `07_calculator_rounded_tileset_transparent.png`

All are full tileset atlases, not sliced elements.

## Important Lessons

- Treat the source folder as a tileset reference, not as single images.
- Generate atlas sheets with many complete UI elements on one canvas.
- Do not slice generated output.
- Keep the original `Assets/newUI` and existing class folders untouched.
- Save each new class into its own folder, for example:
  `Assets/newUI/<className>/<ClassName>Tilesets_NoSlice_V1`
- Use a new clean version folder when iterating:
  `RangerTilesets_NoSlice_V2_Clean`

## Style That Worked

The cleaner Ranger V2 worked better because the prompt asked for:

- clean readable game UI
- low noise
- minimal scratches
- smooth dark surfaces
- bigger readable silhouettes
- controlled texture
- no tiny details
- no heavy grunge
- no clutter

For small UI elements, avoid heavy detail. It falls apart when scaled down.

## Prompt Pattern

Use this pattern for every tileset:

```text
Use case: game asset tileset atlas.
Asset type: Unity DnD <ClassName> <tileset type>, clean readable version, no slicing.
Create a NEW original CLEAN atlas sheet on a perfectly flat solid <chroma-key> chroma-key background.
Style: <ClassName> dark fantasy UI, <class materials/colors>, polished readable game UI.
IMPORTANT: low noise, minimal scratches, clean surfaces, no clutter, no tiny decorative overload.
Layout: one atlas sheet with separated complete tiles and generous whitespace.
Include: <specific list of tile roles>.
Strict avoid: ruby gems, diamond ornaments, center ornaments on frame edges, loose rods, corner fragments, construction-kit pieces, random junk, watermark.
Background must be exactly uniform <chroma-key> with no shadows, gradients, floor, texture, or lighting variation.
Do not use <chroma-key> inside assets.
```

## Chroma Key Choice

Use `#ff00ff` for Ranger or any class that needs green/nature colors.

Use `#00ff00` only when the generated assets do not contain green.

After generation, convert the chroma-key background to alpha and keep the original generated images in the Codex generated-images folder.

## Tileset Groups To Generate

For each class, create these seven atlas groups:

1. `01_square_icon_tileset_transparent.png`
   - empty square tiles
   - profile/silhouette
   - class weapons/tools
   - class feature icons
   - save/load tiles
   - menu/list tile
   - empty action slot

2. `02_orb_token_tileset_transparent.png`
   - empty round token
   - class resource orbs
   - selected/disabled/warning states
   - plus/minus/check round buttons

3. `03_dice_buttons_d100_tileset_transparent.png`
   - `d4`
   - `d6`
   - `d8`
   - `d10`
   - `d12`
   - `d20`
   - `d100`
   - compact versions
   - empty dice button frame

4. `04_bar_input_tileset_transparent.png`
   - long input/label bar
   - filled dark bar
   - class-colored resource bar frame
   - class fill strip
   - secondary fill strip
   - gray fill strip
   - short/medium labels
   - dropdown bar
   - save/load selector
   - raised/pressed/disabled long buttons

5. `05_empty_square_tileset_transparent.png`
   - blank square and rounded tiles
   - filled variants
   - open-center variants
   - disabled variant
   - selected variant
   - small item slot

6. `06_feather_quill_map_tileset_transparent.png`
   - class-flavored quill/feather tools
   - map/scroll/book tiles
   - empty quill slot
   - tracking/knowledge mark

7. `07_calculator_rounded_tileset_transparent.png`
   - calculator tile
   - compact calculator tile
   - keypad container
   - empty rounded tiles
   - disabled/selected rounded tiles
   - small blank rounded tile

## Things To Avoid

These caused bad outputs:

- too much grunge
- too much scratch/noise texture
- tiny decorations on every edge
- central gems or ornaments on stretchable edges
- loose rods or spear-like strips
- L-shaped corners
- separate edge/corner construction pieces
- random decorative junk
- making only 1-2 pictures instead of covering the whole tileset set
- forgetting `d100`

## Ranger V2 Chroma Removal

The successful save path used magenta removal:

- key color: `#ff00ff`
- condition: high red + high blue + low green
- output format: PNG with alpha
- validation: all four corners alpha `0`

PowerShell validation pattern:

```powershell
Add-Type -AssemblyName System.Drawing
Get-ChildItem -Path '<output-folder>' -File -Filter *.png | Sort-Object Name | ForEach-Object {
  $img=[System.Drawing.Bitmap]::FromFile($_.FullName)
  $corners=@(
    $img.GetPixel(0,0).A,
    $img.GetPixel($img.Width-1,0).A,
    $img.GetPixel(0,$img.Height-1).A,
    $img.GetPixel($img.Width-1,$img.Height-1).A
  )
  "$($_.Name)`t$($img.Width)x$($img.Height)`tcorners alpha: $($corners -join ',')"
  $img.Dispose()
}
```

## Ranger Theme Notes

Ranger theme that worked:

- dark wood
- worn leather
- blackened iron
- muted forest green
- small moss accents
- amber secondary accents
- bow, quiver, compass, paw, leaf, trap, campfire, map, backpack

Keep the surfaces cleaner than Blood Hunter. Ranger should read as dark forest utility, not noisy horror.
