import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const bpDir = path.join(root, "src/Edulytics.Core/Curriculum/LessonBlueprints/Packs");
const mappingPath = path.join(root, "src/Edulytics.Core/Mathematics/Curriculum/lesson-skill-mappings.v1.json");

const officialByLesson = new Map();
for (const file of fs.readdirSync(bpDir).filter(x => /^cambridge-.*\.lesson-blueprint\.json$/i.test(x))) {
  const doc = JSON.parse(fs.readFileSync(path.join(bpDir, file), "utf8").replace(/^\uFEFF/, ""));
  for (const lesson of doc.Lessons ?? []) {
    const codes = [...new Set([
      ...(lesson.OutcomeCodes ?? []),
      ...(lesson.FormalTargets ?? []).map(x => x.OutcomeCode),
      ...(lesson.Alignments ?? []).map(x => x.OutcomeCode)
    ].filter(Boolean))];
    if (codes.length) officialByLesson.set(lesson.LessonCode, codes);
  }
}

const doc = JSON.parse(fs.readFileSync(mappingPath, "utf8").replace(/^\uFEFF/, ""));
let upgraded = 0, unresolved = 0;
for (const row of doc.mappings ?? []) {
  if (!String(row.lessonCode ?? "").includes("CAMBRIDGE-INTL-MATH")) continue;
  const codes = officialByLesson.get(row.lessonCode) ?? [];
  if (!codes.length) { unresolved++; continue; }
  row.sourceType = "OfficialMappedPedagogicalLesson";
  row.officialOutcomeMapped = true;
  row.officialOutcomeCodes = codes;
  row.mappingConfidence = "High";
  const evidence = Array.isArray(row.evidence) ? row.evidence.filter(Boolean) : [];
  const marker = "Official Cambridge outcome mapping is sourced from the canonical Cambridge lesson blueprint; no synthetic curriculum outcome is created.";
  if (!evidence.includes(marker)) evidence.push(marker);
  row.evidence = evidence;
  upgraded++;
}
doc.mappingVersion = "1.1-cambridge-official-outcome-closure";
fs.writeFileSync(mappingPath, JSON.stringify(doc, null, 2) + "\n");
console.log(JSON.stringify({ upgraded, unresolved, totalCambridgeMappings: [...officialByLesson.keys()].length }, null, 2));
