const fs = require("fs");
const path = require("path");
const sharp = require("C:/Users/Tom/AppData/Roaming/npm/node_modules/prompt-to-asset/node_modules/sharp");

const srcDir = "D:/dnd2.0test/dnd/Assets/newUI/bloodHunter/PromptToAssetDemo/batch_blood_hunter_restyle";
const outDir = "D:/dnd2.0test/dnd/Assets/newUI/bloodHunter/PromptToAssetDemo/batch_blood_hunter_restyle_fullsize";

fs.mkdirSync(outDir, { recursive: true });

function esc(text) {
  return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

async function main() {
  const files = fs
    .readdirSync(srcDir)
    .filter((file) => /\.png$/i.test(file) && file !== "00_contact_sheet.png")
    .sort((a, b) => a.localeCompare(b));

  for (const file of files) {
    const input = path.join(srcDir, file);
    const meta = await sharp(input).metadata();
    const output = path.join(outDir, file.replace(/\.png$/i, "_fullsize.png"));
    await sharp(input)
      .resize({
        width: meta.width * 4,
        height: meta.height * 4,
        fit: "fill",
        kernel: "lanczos3",
      })
      .png()
      .toFile(output);
  }

  const outFiles = fs
    .readdirSync(outDir)
    .filter((file) => /\.png$/i.test(file) && file !== "00_contact_sheet_fullsize.png")
    .sort((a, b) => a.localeCompare(b));
  const cols = 4;
  const thumbW = 220;
  const thumbH = 180;
  const labelH = 34;
  const composites = [];

  for (let i = 0; i < outFiles.length; i += 1) {
    const file = outFiles[i];
    const input = path.join(outDir, file);
    const meta = await sharp(input).metadata();
    const image = await sharp(input)
      .resize({ width: thumbW, height: thumbH, fit: "contain", background: { r: 18, g: 18, b: 18, alpha: 1 } })
      .flatten({ background: { r: 18, g: 18, b: 18 } })
      .png()
      .toBuffer();
    const label = Buffer.from(
      `<svg xmlns="http://www.w3.org/2000/svg" width="${thumbW}" height="${labelH}">` +
        `<rect width="100%" height="100%" fill="#101010"/>` +
        `<text x="6" y="14" font-family="Arial" font-size="10" fill="#ddd">${esc(file.slice(0, 31))}</text>` +
        `<text x="6" y="28" font-family="Arial" font-size="10" fill="#aaa">${meta.width}x${meta.height}</text>` +
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

  const rows = Math.ceil(outFiles.length / cols);
  const sheet = path.join(outDir, "00_contact_sheet_fullsize.png");
  await sharp({ create: { width: cols * thumbW, height: rows * (thumbH + labelH), channels: 3, background: "#080808" } })
    .composite(composites)
    .png()
    .toFile(sheet);

  console.log(JSON.stringify({ outDir, count: outFiles.length, sheet }, null, 2));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
