import { build } from "esbuild";

await build({
  entryPoints: ["entry.js"],
  bundle: true,
  format: "esm",
  minify: true,
  legalComments: "eof",
  target: ["safari16", "chrome110"],
  outfile: "../../src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/js/codemirror.js",
});
