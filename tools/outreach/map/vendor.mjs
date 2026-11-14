import { copyFileSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { build } from "esbuild";

const here = dirname(fileURLToPath(import.meta.url));
const out = join(here, "..", "..", "..", "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/map/vendor");
const maplibre = join(here, "node_modules/maplibre-gl/dist");
mkdirSync(out, { recursive: true });
for (const file of ["maplibre-gl.mjs", "maplibre-gl-worker.mjs", "maplibre-gl.css"])
  writeFileSync(join(out, file), readFileSync(join(maplibre, file), "utf8").replace(/\n\/\/# sourceMappingURL=\S+\s*$/, "\n"));
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
