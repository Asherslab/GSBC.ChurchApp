import { copyFileSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { build } from "esbuild";

const here = dirname(fileURLToPath(import.meta.url));
const out = join(here, "..", "..", "..", "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/map/vendor");
const maplibre = join(here, "node_modules/maplibre-gl/dist");
mkdirSync(out, { recursive: true });
const withoutSourceMap = (text) => text.replace(/\n\/\/# sourceMappingURL=\S+\s*$/, "\n");
for (const file of ["maplibre-gl.mjs", "maplibre-gl-worker.mjs"])
  writeFileSync(join(out, file), withoutSourceMap(readFileSync(join(maplibre, file), "utf8")));
const css = readFileSync(join(maplibre, "maplibre-gl.css"), "utf8").replace(/\/\*# sourceMappingURL=\S+\s*\*\/\s*$/, "");
writeFileSync(join(out, "maplibre-gl.css.mjs"), `export default ${JSON.stringify(css)};\n`);
copyFileSync(join(here, "node_modules/maplibre-gl/LICENSE.txt"), join(out, "maplibre-gl.LICENSE.txt"));

await build({
  stdin: { contents: "export { PMTiles, Protocol, FetchSource } from 'pmtiles';", resolveDir: here },
  bundle: true,
  format: "esm",
  minify: true,
  legalComments: "eof",
  target: ["safari16", "chrome110"],
  outfile: join(out, "pmtiles.mjs"),
});
