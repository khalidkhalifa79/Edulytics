import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const sowDir = path.join(root, "artifacts/cambridge-official-audit/sow-text");
const packDir = path.join(root, "src/Edulytics.Core/Curriculum/LessonBlueprints/Packs");
const outDir = path.join(root, "artifacts/cambridge-official-audit");
fs.mkdirSync(outDir, { recursive: true });

function loadJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8").replace(/^\uFEFF/, ""));
}
function officialRefs(doc, programme) {
  const refs = new Map();
  for (const lesson of doc.Lessons ?? []) {
    const codes = [
      ...(lesson.OutcomeCodes ?? []),
      ...(lesson.FormalTargets ?? []).map(x => x.OutcomeCode),
      ...(lesson.Alignments ?? []).map(x => x.OutcomeCode ?? x.ReferenceCode)
    ].filter(Boolean);
    for (const raw of codes) {
      const s = String(raw);
      const prefix = programme === "0096" ? "CAM:OUT:0096:" : "CAM:OUT:0862:";
      if (!s.startsWith(prefix)) continue;
      const code = s.slice(prefix.length);
      if (!refs.has(code)) refs.set(code, []);
      refs.get(code).push(lesson.LessonCode ?? lesson.Code ?? lesson.Title ?? "UNKNOWN");
    }
  }
  return refs;
}

function stagePack(stage) {
  const files = fs.readdirSync(packDir);
  const name = files.find(x => stage <= 6
    ? x.startsWith(`cambridge-primary-stage${stage}-`)
    : x.startsWith(`cambridge-lower-stage${stage}-`));
  if (!name) throw new Error(`No pack for stage ${stage}`);
  return { name, doc: loadJson(path.join(packDir, name)) };
}

function parseStage(stage) {
  const programme = stage <= 6 ? "0096" : "0862";
  const textFile = path.join(sowDir, `${programme}_STAGE${stage}_SOW.txt`);
  const text = fs.readFileSync(textFile, "utf8");
  const { name, doc } = stagePack(stage);
  const represented = officialRefs(doc, programme);

  const unitMarker = new RegExp(`Learning objectives covered in Unit ${stage}\\.(\\d+) and topic summary:`, "g");
  const markers = [...text.matchAll(unitMarker)];
  const units = [];
  const seenCodes = new Set();

  for (let i = 0; i < markers.length; i++) {
    const unitNo = Number(markers[i][1]);
    const start = markers[i].index;
    const end = i + 1 < markers.length ? markers[i + 1].index : text.length;
    const section = text.slice(start, end);

    const headingBefore = text.slice(Math.max(0, start - 500), start);
    const headingMatches = [...headingBefore.matchAll(new RegExp(`Unit ${stage}\\.${unitNo} ([^\\r\\n]+)`, "g"))];
    const unitTitle = headingMatches.length ? headingMatches.at(-1)[1].trim() : null;

    const topicRegex = new RegExp(`Unit ${stage}\\.${unitNo} Topic (\\d+) ([^\\r\\n]+)`, "g");
    const topicMatches = [...section.matchAll(topicRegex)];
    const topics = [];
    for (let t = 0; t < topicMatches.length; t++) {
      const topicNo = Number(topicMatches[t][1]);
      const title = topicMatches[t][2].trim();
      if (topics.some(x => x.topic === topicNo && x.title === title)) continue;
      const tStart = topicMatches[t].index;
      const next = topicMatches.slice(t + 1).find(m => Number(m[1]) !== topicNo || m[2].trim() !== title);
      const tEnd = next ? next.index : section.length;
      const topicSection = section.slice(tStart, tEnd);
      const codeRegex = new RegExp(`\\b${stage}(?:Nc|Ni|Nm|Np|Nf|Gt|Gg|Gp|Ss|Sp|Ae|As)\\.\\d{2}\\b`, "g");
      const codes = [...new Set(topicSection.match(codeRegex) ?? [])];
      topics.push({ topic: topicNo, title, objectiveCodes: codes });
    }

    const codeRegex = new RegExp(`\\b${stage}(?:Nc|Ni|Nm|Np|Nf|Gt|Gg|Gp|Ss|Sp|Ae|As)\\.\\d{2}\\b`, "g");
    const unitCodes = [...new Set(section.match(codeRegex) ?? [])];
    unitCodes.forEach(c => seenCodes.add(c));

    units.push({
      unit: unitNo,
      title: unitTitle,
      objectiveCodes: unitCodes,
      topics
    });
  }

  const allOfficialCodes = [...seenCodes].sort();
  const missing = allOfficialCodes.filter(c => !represented.has(c));
  const covered = allOfficialCodes.filter(c => represented.has(c));

  return {
    stage,
    programme,
    pack: name,
    currentLessons: (doc.Lessons ?? []).length,
    units,
    unitCount: units.length,
    topicCount: units.reduce((n,u)=>n+u.topics.length,0),
    officialObjectivesInSow: allOfficialCodes.length,
    representedObjectives: covered.length,
    missingObjectives: missing.length,
    missingCodes: missing,
    coveragePercent: allOfficialCodes.length ? Number((covered.length / allOfficialCodes.length * 100).toFixed(1)) : 0
  };
}

const stages = Array.from({length:9}, (_,i)=>parseStage(i+1));
const report = {
  generatedAt: new Date().toISOString(),
  source: "Cambridge official Schemes of Work for 0096 Stages 1-6 and 0862 Stages 7-9",
  stages,
  totals: {
    currentLessons: stages.reduce((s,x)=>s+x.currentLessons,0),
    units: stages.reduce((s,x)=>s+x.unitCount,0),
    topics: stages.reduce((s,x)=>s+x.topicCount,0),
    officialObjectivesInSow: stages.reduce((s,x)=>s+x.officialObjectivesInSow,0),
    representedObjectives: stages.reduce((s,x)=>s+x.representedObjectives,0),
    missingObjectives: stages.reduce((s,x)=>s+x.missingObjectives,0)
  }
};
const out = path.join(outDir, "cambridge-stages1-9-canonical-inventory.v1.json");
fs.writeFileSync(out, JSON.stringify(report, null, 2));
console.log(JSON.stringify({
  totals: report.totals,
  stages: stages.map(x => ({
    stage:x.stage,
    lessons:x.currentLessons,
    units:x.unitCount,
    topics:x.topicCount,
    official:x.officialObjectivesInSow,
    covered:x.representedObjectives,
    missing:x.missingObjectives,
    coverage:x.coveragePercent
  }))
}, null, 2));
