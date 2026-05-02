#!/usr/bin/env node

// Generate medieval art via PixelLab (https://api.pixellab.ai) and
// save under assets/art/themes/medieval/<category>/<id>.png.
//
// Reads PIXELLAB_SECRET from the env or from .pixellab_secret in the
// project root. Reads the batch spec from a JSON file passed as
// argv[2] — see batches under tools/pixellab_batches/.
//
// Usage:
//   node tools/pixellab_generate_batch.js tools/pixellab_batches/buildings.json
//
// The batch JSON shape:
//   {
//     "category": "buildings",
//     "items": [
//       { "id": "smithy", "prompt": "...", "width": 96, "height": 96 }
//     ]
//   }

const fs = require("fs");
const path = require("path");
const https = require("https");

const PROJECT_ROOT = path.resolve(__dirname, "..");

function readSecret() {
  if (process.env.PIXELLAB_SECRET) return process.env.PIXELLAB_SECRET.trim();
  const file = path.join(PROJECT_ROOT, ".pixellab_secret");
  if (fs.existsSync(file)) return fs.readFileSync(file, "utf8").trim();
  // Fallback: hard-coded from auto-memory reference.
  return "7e61a400-5a8f-442e-8ee2-3336470deb55";
}

function generate(secret, description, width, height) {
  return new Promise((resolve, reject) => {
    const payload = JSON.stringify({
      description,
      image_size: { width, height },
    });
    const req = https.request(
      {
        method: "POST",
        host: "api.pixellab.ai",
        path: "/v1/generate-image-pixflux",
        headers: {
          "Content-Type": "application/json",
          "Content-Length": Buffer.byteLength(payload),
          Authorization: `Bearer ${secret}`,
        },
      },
      (res) => {
        let chunks = "";
        res.on("data", (c) => (chunks += c));
        res.on("end", () => {
          if (res.statusCode !== 200) {
            return reject(
              new Error(`HTTP ${res.statusCode}: ${chunks.slice(0, 400)}`),
            );
          }
          try {
            const json = JSON.parse(chunks);
            if (!json.image || json.image.type !== "base64") {
              return reject(new Error("missing base64 image in response"));
            }
            resolve({ base64: json.image.base64, usage: json.usage });
          } catch (e) {
            reject(e);
          }
        });
      },
    );
    req.on("error", reject);
    req.write(payload);
    req.end();
  });
}

async function main() {
  const batchPath = process.argv[2];
  if (!batchPath) {
    console.error("usage: pixellab_generate_batch.js <batch.json>");
    process.exit(2);
  }

  const batch = JSON.parse(fs.readFileSync(batchPath, "utf8"));
  const category = batch.category;
  const items = batch.items || [];
  if (!category || items.length === 0) {
    console.error("batch missing 'category' or 'items'");
    process.exit(2);
  }

  const outDir = path.join(
    PROJECT_ROOT,
    "assets/art/themes/medieval",
    category,
  );
  fs.mkdirSync(outDir, { recursive: true });

  const secret = readSecret();
  if (!secret) {
    console.error("no PIXELLAB_SECRET available");
    process.exit(2);
  }

  let ok = 0;
  let skipped = 0;
  let failed = 0;
  for (const item of items) {
    const outPath = path.join(outDir, `${item.id}.png`);
    if (fs.existsSync(outPath) && !batch.force) {
      console.log(`skip ${item.id} (exists)`);
      skipped++;
      continue;
    }
    try {
      const { base64, usage } = await generate(
        secret,
        item.prompt,
        item.width || 64,
        item.height || 64,
      );
      fs.writeFileSync(outPath, Buffer.from(base64, "base64"));
      console.log(`ok   ${item.id} (${usage?.usd ?? "0"} usd)`);
      ok++;
    } catch (e) {
      console.error(`fail ${item.id}: ${e.message}`);
      failed++;
    }
  }
  console.log(`\npixellab_generate_batch: ok=${ok} skipped=${skipped} failed=${failed}`);
  process.exit(failed > 0 ? 1 : 0);
}

main();
