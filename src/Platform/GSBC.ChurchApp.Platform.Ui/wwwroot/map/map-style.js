const looks = {
  pockets: {
    light: {
      water: "#cde2e3", land: "#f7f4ee", park: "#d9e6cf", notHomes: "#ebe2d2", building: "#ece6dc", buildingEdge: "#e0d8cb",
      minor: "#ffffff", minorEdge: "#ddd5c7", major: "#fbf6ee", majorEdge: "#a99c88", path: "#c8bfae", label: "#545e70", place: "#626a79", halo: "#f7f4ee",
    },
    dark: {
      water: "#123247", land: "#141d31", park: "#1c3528", notHomes: "#2e2c2f", building: "#1a2337", buildingEdge: "#1f293f",
      minor: "#26324b", minorEdge: "#1f293f", major: "#283450", majorEdge: "#3b4763", path: "#2f3a52", label: "#b2b8c5", place: "#9ca2b1", halo: "#141d31",
    },
  },
  sand: {
    light: {
      water: "#dfe7e3", land: "#f4efe7", park: "#f4efe7", notHomes: "#f4efe7", building: "#f4efe7", buildingEdge: "#f4efe7",
      minor: "#ffffff", minorEdge: "#e6ddcf", major: "#ffffff", majorEdge: "#d5c9b7", path: "#c2b6a2", label: "#626a79", place: "#7a8291", halo: "#f4efe7",
    },
    dark: {
      water: "#17223a", land: "#1d2840", park: "#1d2840", notHomes: "#1d2840", building: "#1d2840", buildingEdge: "#1d2840",
      minor: "#2a3652", minorEdge: "#1d2840", major: "#2f3c5b", majorEdge: "#1d2840", path: "#46516b", label: "#9ca2b1", place: "#8d93a3", halo: "#1d2840",
    },
  },
  homes: {
    light: {
      water: "#d9e5e9", land: "#fbfaf7", park: "#f0f2ea", notHomes: "#f3f2f2", building: "#d8c8ae", buildingEdge: "#c4b090",
      minor: "#ffffff", minorEdge: "#dfe0e4", major: "#ffffff", majorEdge: "#c9ccd4", path: "#a9afbb", label: "#545e70", place: "#626a79", halo: "#fbfaf7",
    },
    dark: {
      water: "#0f1b2d", land: "#141d31", park: "#161f33", notHomes: "#151d31", building: "#4a4437", buildingEdge: "#5b5443",
      minor: "#222d45", minorEdge: "#141d31", major: "#2a3550", majorEdge: "#141d31", path: "#4f5971", label: "#b2b8c5", place: "#9ca2b1", halo: "#141d31",
    },
  },
};

const parks = ["park", "nature_reserve", "national_park", "wood", "forest", "grass", "golf_course", "cemetery", "garden", "playground", "pitch", "recreation_ground", "village_green"];
const notHomes = ["school", "college", "university", "hospital", "commercial", "retail", "industrial", "military", "railway", "aerodrome", "sports_centre", "stadium", "zoo"];
const minorRoads = ["minor_road", "other"];
const regular = ["montserrat-regular"];
const semibold = ["montserrat-semibold"];

const byZoom = stops => ["interpolate", ["exponential", 1.6], ["zoom"], ...stops.flat()];
const kind = (...kinds) => ["in", ["get", "kind"], ["literal", kinds]];
const detail = (...details) => ["in", ["get", "kind_detail"], ["literal", details]];
const not = filter => ["!", filter];
const homesStreets = ["all", kind(...minorRoads), not(detail("service"))];

export const defaultLook = "pockets";

export function mapStyle({ tiles, glyphs, scheme = "light", look = defaultLook, attribution = "" }) {
  const colour = looks[look][scheme];
  const files = Array.isArray(tiles) ? tiles : [tiles];
  const sourceOf = index => (index === 0 ? "streets" : `streets-${index}`);
  const eachFile = layer => files.map((_, index) => ({ ...layer, id: index === 0 ? layer.id : `${layer.id}-${index}`, source: sourceOf(index) }));
  return {
    version: 8,
    glyphs,
    sources: Object.fromEntries(files.map((url, index) => [sourceOf(index), { type: "vector", url, attribution }])),
    layers: [
      { id: "water-ground", type: "background", paint: { "background-color": colour.water } },
      ...[
        { id: "land", type: "fill", source: "streets", "source-layer": "earth", paint: { "fill-color": colour.land } },
        { id: "parks", type: "fill", source: "streets", "source-layer": "landuse", filter: kind(...parks), paint: { "fill-color": colour.park } },
        { id: "not-homes", type: "fill", source: "streets", "source-layer": "landuse", filter: kind(...notHomes), paint: { "fill-color": colour.notHomes } },
        { id: "water", type: "fill", source: "streets", "source-layer": "water", filter: ["==", ["geometry-type"], "Polygon"], paint: { "fill-color": colour.water } },
        { id: "waterways", type: "line", source: "streets", "source-layer": "water", filter: ["==", ["geometry-type"], "LineString"],
          paint: { "line-color": colour.water, "line-width": byZoom([[12, 0.5], [18, 4]]) } },
        { id: "buildings", type: "fill", source: "streets", "source-layer": "buildings", minzoom: 14,
          paint: { "fill-color": colour.building, "fill-outline-color": colour.buildingEdge, "fill-opacity": ["interpolate", ["linear"], ["zoom"], 14, 0, 15, 1] } },
        { id: "paths", type: "line", source: "streets", "source-layer": "roads", minzoom: 15, filter: ["all", kind("path"), not(detail("sidewalk", "crossing"))],
          paint: { "line-color": colour.path, "line-width": byZoom([[15, 0.6], [18, 1.4]]), "line-dasharray": [2, 1.5] } },
        { id: "service", type: "line", source: "streets", "source-layer": "roads", minzoom: 14, filter: ["all", kind(...minorRoads), detail("service")],
          layout: { "line-cap": "round", "line-join": "round" }, paint: { "line-color": colour.minorEdge, "line-width": byZoom([[14, 0.6], [16, 3], [18, 9]]) } },
        { id: "minor-edge", type: "line", source: "streets", "source-layer": "roads", filter: homesStreets, layout: { "line-cap": "round", "line-join": "round" },
          paint: { "line-color": colour.minorEdge, "line-width": byZoom([[12, 0.8], [14, 3], [16, 9], [18, 26]]) } },
        { id: "major-edge", type: "line", source: "streets", "source-layer": "roads", filter: kind("medium_road", "major_road", "highway"), layout: { "line-cap": "round", "line-join": "round" },
          paint: { "line-color": colour.majorEdge, "line-width": byZoom([[10, 1.2], [14, 5], [16, 13], [18, 34]]) } },
        { id: "minor", type: "line", source: "streets", "source-layer": "roads", filter: homesStreets, layout: { "line-cap": "round", "line-join": "round" },
          paint: { "line-color": colour.minor, "line-width": byZoom([[12, 0.4], [14, 2], [16, 7.5], [18, 23]]) } },
        { id: "major", type: "line", source: "streets", "source-layer": "roads", filter: kind("medium_road", "major_road", "highway"), layout: { "line-cap": "round", "line-join": "round" },
          paint: { "line-color": colour.major, "line-width": byZoom([[10, 0.8], [14, 3.8], [16, 11], [18, 31]]) } },
        { id: "street-names", type: "symbol", source: "streets", "source-layer": "roads", minzoom: 14, filter: kind(...minorRoads, "medium_road", "major_road"),
          layout: { "symbol-placement": "line", "text-field": ["get", "name"], "text-font": regular, "text-size": ["interpolate", ["linear"], ["zoom"], 14, 11, 16, 13, 18, 15], "text-max-angle": 30 },
          paint: { "text-color": colour.label, "text-halo-color": colour.halo, "text-halo-width": 1.6, "text-halo-blur": 0.5 } },
        { id: "suburbs", type: "symbol", source: "streets", "source-layer": "places", filter: kind("locality", "neighbourhood", "macrohood", "suburb"), maxzoom: 15,
          layout: { "text-field": ["get", "name"], "text-font": semibold, "text-size": byZoom([[10, 11], [14, 14]]), "text-transform": "uppercase", "text-letter-spacing": 0.12 },
          paint: { "text-color": colour.place, "text-halo-color": colour.halo, "text-halo-width": 1.6 } },
      ].flatMap(eachFile),
    ],
  };
}
