const fs = require("fs");
const path = require("path");
const sharp = require("C:/Users/Tom/AppData/Roaming/npm/node_modules/prompt-to-asset/node_modules/sharp");

const srcDir = "D:/dnd2.0test/dnd/Assets/newUI/bloodHunter";
const outDir = "D:/dnd2.0test/dnd/Assets/newUI/bloodHunter/PromptToAssetDemo/batch_blood_hunter_restyle";

fs.mkdirSync(outDir, { recursive: true });

function safeName(name) {
  return (
    name
      .replace(/\.[^.]+$/, "")
      .replace(/[^a-zA-Z0-9]+/g, "_")
      .replace(/^_+|_+$/g, "")
      .slice(0, 70) || "asset"
  );
}

function tonePixel(r, g, b, a, hasAlpha) {
  if (a === 0) return [0, 0, 0, 0];

  const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const sat = max - min;
  const isRed = (r > g * 1.1 && r > b * 1.05) || (r > 90 && g < 65 && b < 75);
  const isGreen = g > r * 1.12 && g > b * 1.05;
  const isBlue = b > r * 1.1 && b > g * 1.05;

  let nr;
  let ng;
  let nb;

  if (isRed) {
    const k = Math.min(1, Math.max(0, (lum - 20) / 150));
    nr = 58 + 95 * k;
    ng = 7 + 24 * k;
    nb = 9 + 22 * k;
  } else if (lum > 175 && sat < 70) {
    const k = Math.min(1, (lum - 175) / 80);
    nr = 116 + 56 * k;
    ng = 78 + 40 * k;
    nb = 50 + 28 * k;
  } else if (lum > 125) {
    const k = (lum - 125) / 80;
    nr = 68 + 54 * k;
    ng = 50 + 38 * k;
    nb = 41 + 28 * k;
  } else if (isBlue || isGreen) {
    const k = Math.min(1, lum / 170);
    nr = 18 + 30 * k;
    ng = 15 + 24 * k;
    nb = 16 + 23 * k;
  } else {
    const k = Math.min(1, lum / 150);
    nr = 8 + 42 * k;
    ng = 8 + 36 * k;
    nb = 9 + 35 * k;
  }

  const edgeBoost = hasAlpha && a > 0 && a < 240 ? 18 : 0;
  return [
    Math.max(0, Math.min(255, Math.round(nr + edgeBoost))),
    Math.max(0, Math.min(255, Math.round(ng + edgeBoost * 0.65))),
    Math.max(0, Math.min(255, Math.round(nb + edgeBoost * 0.45))),
    a,
  ];
}

function esc(s) {
  return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

async function main() {
  const files = fs
    .readdirSync(srcDir)
    .filter((f) => /\.(png|jpg|jpeg)$/i.test(f))
    .sort((a, b) => a.localeCompare(b));
  const outputs = [];

  for (let idx = 0; idx < files.length; idx += 1) {
    const file = files[idx];
    const input = path.join(srcDir, file);
    const meta = await sharp(input).metadata();
    const obj = await sharp(input).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const data = obj.data;
    const outBuffer = Buffer.alloc(data.length);

    for (let i = 0; i < data.length; i += 4) {
      const p = tonePixel(data[i], data[i + 1], data[i + 2], data[i + 3], Boolean(meta.hasAlpha));
      outBuffer[i] = p[0];
      outBuffer[i + 1] = p[1];
      outBuffer[i + 2] = p[2];
      outBuffer[i + 3] = meta.hasAlpha ? p[3] : 255;
    }

    const out = path.join(outDir, `${String(idx + 1).padStart(2, "0")}_${safeName(file)}.png`);
    await sharp(outBuffer, { raw: obj.info }).png().toFile(out);
    outputs.push({ file, out, width: meta.width, height: meta.height, alpha: Boolean(meta.hasAlpha) });
  }

  const cols = 5;
  const thumbW = 180;
  const thumbH = 150;
  const labelH = 44;
  const composites = [];

  for (let i = 0; i < outputs.length; i += 1) {
    const item = outputs[i];
    const image = await sharp(item.out)
      .resize({ width: thumbW, height: thumbH, fit: "contain", background: { r: 20, g: 20, b: 20, alpha: 1 } })
      .flatten({ background: { r: 20, g: 20, b: 20 } })
      .png()
      .toBuffer();
    const label = Buffer.from(
      `<svg xmlns="http://www.w3.org/2000/svg" width="${thumbW}" height="${labelH}">` +
        `<rect width="100%" height="100%" fill="#101010"/>` +
        `<text x="6" y="15" font-family="Arial" font-size="11" fill="#ddd">${String(i + 1).padStart(2, "0")}. ${esc(
          path.basename(item.out).slice(3, 28),
        )}</text>` +
        `<text x="6" y="32" font-family="Arial" font-size="10" fill="#aaa">${item.width}x${item.height}</text>` +
        `</svg>`,
    );
    const tile = await sharp({ create: { width: thumbW, height: thumbH + labelH, channels: 3, background: "#101010" } })
      .composite([
        { input: image, left: 0, top: 0 },
        { input: label, left: 0, top: thumbH },
      ])
      .png()
      .toBuffer();

    composites.push({
      input: tile,
      left: (i % cols) * thumbW,
      top: Math.floor(i / cols) * (thumbH + labelH),
    });
  }

  const rows = Math.ceil(outputs.length / cols);
  const sheet = path.join(outDir, "00_contact_sheet.png");
  await sharp({ create: { width: cols * thumbW, height: rows * (thumbH + labelH), channels: 3, background: "#080808" } })
    .composite(composites)
    .png()
    .toFile(sheet);

  console.log(JSON.stringify({ outDir, count: outputs.length, sheet }, null, 2));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
