#!/usr/bin/env node

// Walk assets/art/themes/medieval/<category>/*.png and inject the
// resulting manifest into assets/art/themes/medieval/theme.json under
// a top-level `assets` key. Also updates `items_theming[*].icon_path`
// when an items/<id>.png file exists. Idempotent: re-running just
// regenerates the same shape.

const fs = require("fs");
const path = require("path");

const ROOT = path.resolve(__dirname, "..");
const THEME_PATH = path.join(ROOT, "assets/art/themes/medieval/theme.json");
const ART_ROOT = path.join(ROOT, "assets/art/themes/medieval");
const ASSET_BASE = "res://assets/art/themes/medieval";

function listPngs(dirAbsolute, dirRelative) {
  if (!fs.existsSync(dirAbsolute)) return [];
  return fs
    .readdirSync(dirAbsolute, { withFileTypes: true })
    .filter((entry) => entry.isFile() && entry.name.toLowerCase().endsWith(".png"))
    .map((entry) => entry.name)
    .sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));
}

// Collect every category directory under the medieval art root + an
// alphabetised file list per category. Skip nested theme dirs that
// snuck under here (boarding_school/, western/) — those are
// historical noise that don't belong in the medieval manifest.
const SKIP_DIRS = new Set(["boarding_school", "western", "space"]);
function buildManifest() {
  const manifest = {};
  for (const entry of fs.readdirSync(ART_ROOT, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    if (SKIP_DIRS.has(entry.name)) continue;
    const files = listPngs(path.join(ART_ROOT, entry.name), entry.name);
    if (files.length === 0) continue;
    manifest[entry.name] = {
      base_path: `${ASSET_BASE}/${entry.name}`,
      count: files.length,
      files,
    };
  }
  return manifest;
}

function variantsForKind(files) {
  // Group filenames by base "kind" — strips a trailing _<single
  // letter> suffix (smithy_a / smithy_b -> kind smithy). Mirrors
  // the runtime logic in scripts/Art/ThemedArtRegistry.cs.
  const groups = {};
  for (const file of files) {
    const stem = file.replace(/\.png$/i, "");
    const m = stem.match(/^(.*)_([a-z])$/i);
    const kind = m && m[1].length > 0 ? m[1] : stem;
    if (!groups[kind]) groups[kind] = [];
    groups[kind].push(file);
  }
  for (const k of Object.keys(groups)) groups[k].sort();
  return groups;
}

function main() {
  const theme = JSON.parse(fs.readFileSync(THEME_PATH, "utf8"));
  const manifest = buildManifest();

  // Build the assets block: a flat manifest plus a per-kind variant
  // index for buildings/structures/decals/environment so consumers
  // can pick a variant by base kind without re-walking the dir.
  const variantCategories = [
    "buildings",
    "structures",
    "decals",
    "environment",
    "tiles",
  ];
  const kindIndex = {};
  for (const cat of variantCategories) {
    if (!manifest[cat]) continue;
    kindIndex[cat] = variantsForKind(manifest[cat].files);
  }

  theme.assets = {
    base_path: ASSET_BASE,
    generated_at: new Date().toISOString(),
    categories: manifest,
    variant_index: kindIndex,
    total_files: Object.values(manifest).reduce((sum, c) => sum + c.count, 0),
  };

  // Annotate items_theming with concrete icon paths when a matching
  // items/<id>.png exists. Don't drop existing `rename` / `icon_hint`
  // metadata.
  if (theme.items_theming && manifest.items) {
    const itemFiles = new Set(manifest.items.files.map((f) => f.replace(/\.png$/i, "")));
    for (const [itemId, meta] of Object.entries(theme.items_theming)) {
      if (itemFiles.has(itemId)) {
        theme.items_theming[itemId] = {
          ...meta,
          icon_path: `${ASSET_BASE}/items/${itemId}.png`,
        };
      }
    }
    // Also auto-populate items_theming entries for items that have a
    // PNG but no existing rename metadata (so consumers see the icon
    // path even if the design doesn't define a renamed display).
    for (const fileStem of itemFiles) {
      if (!theme.items_theming[fileStem]) {
        theme.items_theming[fileStem] = {
          icon_path: `${ASSET_BASE}/items/${fileStem}.png`,
        };
      }
    }
  }

  fs.writeFileSync(THEME_PATH, JSON.stringify(theme, null, 2) + "\n", "utf8");
  console.log(`Updated ${THEME_PATH}`);
  console.log(`  categories: ${Object.keys(manifest).length}`);
  console.log(`  total files: ${theme.assets.total_files}`);
  console.log(`  items_theming entries: ${Object.keys(theme.items_theming || {}).length}`);
}

main();
