import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const bp = path.join(root, "src/Edulytics.Core/Curriculum/LessonBlueprints/Packs");
const cp = path.join(root, "src/Edulytics.Core/Curriculum/LessonContent/Packs");
const requireZeroSupporting = process.argv.includes("--require-zero-supporting");

let total = 0, supporting = 0, official = 0, missing = 0;
const courses = [];
for (const f of fs.readdirSync(bp).filter(x => /^uae-.*\.lesson-blueprint\.json$/i.test(x)).sort()) {
  const b = JSON.parse(fs.readFileSync(path.join(bp, f), "utf8"));
  const cfile = path.join(cp, f.replace(".lesson-blueprint.json", ".lesson-content-pack.json"));
  const c = fs.existsSync(cfile) ? JSON.parse(fs.readFileSync(cfile, "utf8")) : { Lessons: [] };
  const cm = new Map(c.Lessons.map(x => [x.LessonCode, x]));
  let sCount = 0, oCount = 0, mCount = 0;
  for (const l of b.Lessons) {
    total++;
    const x = cm.get(l.LessonCode);
    if (!x) {
      missing++; mCount++; continue;
    }
    if (x.IsSupporting) {
      supporting++; sCount++; continue;
    }
    if ((l.OutcomeCodes || []).length || l.OfficialReferenceCode) {
      official++; oCount++; continue;
    }
    missing++; mCount++;
  }
  courses.push({ file: f, lessons: b.Lessons.length, official: oCount, supporting: sCount, missing: mCount });
}

console.log(JSON.stringify({ total, official, supporting, missing, courses }, null, 2));
if (requireZeroSupporting && (supporting !== 0 || missing !== 0)) {
  process.exitCode = 1;
}
