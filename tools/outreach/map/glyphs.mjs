import { readFileSync, mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { chromium } from "playwright-core";

const here = dirname(fileURLToPath(import.meta.url));
const top = join(here, "..", "..", "..");
const fonts = join(top, "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/fonts");
const out = join(top, "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/map/glyphs");
const stacks = [
  { name: "montserrat-regular", file: "montserrat-latin-400-normal.woff2", weight: 400 },
  { name: "montserrat-semibold", file: "montserrat-latin-600-normal.woff2", weight: 600 },
];
const rangesTheLatinSubsetFills = [0, 8192];
const maplibreTinySdfBaselineShift = 27.5;
const tinySdf = readFileSync(createRequire(import.meta.url).resolve("@mapbox/tiny-sdf"), "utf8").replace("export default class", "class");

const browser = await chromium.launch();
const page = await browser.newPage();
const faces = stacks.map(stack =>
  `@font-face{font-family:Map${stack.weight};font-weight:${stack.weight};src:url(data:font/woff2;base64,${readFileSync(join(fonts, stack.file)).toString("base64")}) format("woff2")}`).join("");
await page.setContent(`<style>${faces}</style><script>${tinySdf};window.TinySDF=TinySDF;</script>`);

for (const stack of stacks) {
  for (const start of rangesTheLatinSubsetFills) {
    const glyphs = await page.evaluate(async ({ weight, start }) => {
      await document.fonts.load(`${weight} 24px Map${weight}`);
      const sdf = new TinySDF({ fontSize: 24, buffer: 3, radius: 8, cutoff: 0.25, fontFamily: `Map${weight}`, fontWeight: String(weight) });
      const probe = document.createElement("canvas").getContext("2d");
      const widthIn = (fallback, text) => { probe.font = `${weight} 24px Map${weight}, ${fallback}`; return probe.measureText(text).width; };
      const inTheFont = text => widthIn("monospace", text) === widthIn("serif", text);
      const glyphs = [];
      for (let id = start; id < start + 256; id++) {
        const text = String.fromCharCode(id);
        if (id > 32 && !inTheFont(text)) continue;
        const glyph = sdf.draw(text);
        glyphs.push({ id, width: glyph.glyphWidth, height: glyph.glyphHeight, left: glyph.glyphLeft, top: glyph.glyphTop,
          advance: glyph.glyphAdvance, bitmap: glyph.glyphWidth && glyph.glyphHeight ? Array.from(glyph.data) : [] });
      }
      return glyphs;
    }, { weight: stack.weight, start });
    const file = join(out, stack.name, `${start}-${start + 255}.pbf`);
    mkdirSync(dirname(file), { recursive: true });
    writeFileSync(file, encode(stack.name, `${start}-${start + 255}`, glyphs));
    console.log(`${file}: ${glyphs.length} glyphs`);
  }
}
await browser.close();

function encode(name, range, glyphs) {
  const stack = [...field(1, text(name)), ...field(2, text(range))];
  for (const glyph of glyphs) {
    const body = [...varintField(1, glyph.id)];
    if (glyph.bitmap.length) body.push(...field(2, glyph.bitmap));
    body.push(...varintField(3, glyph.width), ...varintField(4, glyph.height), ...varintField(5, zigzag(glyph.left)),
      ...varintField(6, zigzag(Math.round(glyph.top - maplibreTinySdfBaselineShift))), ...varintField(7, Math.round(glyph.advance)));
    stack.push(...field(3, body));
  }
  return Buffer.from(field(1, stack));
}

function text(value) { return [...Buffer.from(value, "utf8")]; }
function zigzag(value) { return value >= 0 ? value * 2 : -value * 2 - 1; }
function varint(value) { const bytes = []; while (value > 127) { bytes.push((value & 127) | 128); value = Math.floor(value / 128); } bytes.push(value); return bytes; }
function varintField(number, value) { return [...varint(number << 3), ...varint(value)]; }
function field(number, bytes) { return [...varint((number << 3) | 2), ...varint(bytes.length), ...bytes]; }
