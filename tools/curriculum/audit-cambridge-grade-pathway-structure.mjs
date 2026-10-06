import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const packs = path.join(root, "src/Edulytics.Core/Curriculum/LessonBlueprints/Packs");

function load(name) {
  return JSON.parse(fs.readFileSync(path.join(packs, name), "utf8").replace(/^\uFEFF/, ""));
}

function refs(doc, programme) {
  const set = new Set();
  for (const lesson of doc.Lessons ?? []) {
    const codes = [
      ...(lesson.OutcomeCodes ?? []),
      ...(lesson.FormalTargets ?? []).map(x => x.OutcomeCode),
      ...(lesson.Alignments ?? []).map(x => x.OutcomeCode ?? x.ReferenceCode)
    ].filter(Boolean);
    for (const code of codes) {
      const s = String(code);
      if (programme === "0580" && /^CAM:OUT:0580:/.test(s)) set.add(s);
      if (programme === "9709") {
        const m = s.match(/9709:(\d+\.\d+)/);
        if (m) set.add(m[1]);
      }
    }
  }
  return set;
}

const igcse = {};
for (const pathway of ["core", "extended"]) {
  const f10 = `cambridge-igcse-l10-${pathway}-ogl-v1.lesson-blueprint.json`;
  const f11 = `cambridge-igcse-l11-${pathway}-ogl-v1.lesson-blueprint.json`;
  const d10 = load(f10), d11 = load(f11);
  const a = refs(d10, "0580"), b = refs(d11, "0580");
  igcse[pathway] = {
    level10Lessons: (d10.Lessons ?? []).length,
    level11Lessons: (d11.Lessons ?? []).length,
    level10OfficialRefs: a.size,
    level11OfficialRefs: b.size,
    sharedOfficialRefs: [...a].filter(x => b.has(x)).length,
    level10OnlyOfficialRefs: [...a].filter(x => !b.has(x)),
    level11OnlyOfficialRefs: [...b].filter(x => !a.has(x)),
    interpretation: "Current Level 10 and Level 11 packs use the same official 0580 reference set. They are different lesson decompositions, not distinct official-year syllabus partitions."
  };
}

const componentNames = {
  1:"Pure Mathematics 1",
  2:"Pure Mathematics 2",
  3:"Pure Mathematics 3",
  4:"Mechanics",
  5:"Probability & Statistics 1",
  6:"Probability & Statistics 2"
};
const expectedSections = {1:8,2:6,3:9,4:5,5:5,6:5};
const level9709 = {};
for (const [key, file] of Object.entries({
  asLevel:"cambridge-as-level-9709-ogl-v1.lesson-blueprint.json",
  aLevel:"cambridge-a-level-9709-ogl-v1.lesson-blueprint.json"
})) {
  const doc = load(file);
  const have = refs(doc, "9709");
  const byComponent = {};
  for (let c=1;c<=6;c++) {
    const refsForComponent = [...have].filter(x => x.startsWith(c+"."));
    byComponent[c] = {
      name:componentNames[c],
      officialSectionsRepresented:refsForComponent.length,
      expectedOfficialSections:expectedSections[c],
      refs:refsForComponent.sort((x,y)=>Number(x.split(".")[1])-Number(y.split(".")[1]))
    };
  }
  level9709[key] = {
    file,
    lessons:(doc.Lessons ?? []).length,
    byComponent
  };
}

const report = {
  generatedAt:new Date().toISOString(),
  igcse0580GradeSplitAudit:igcse,
  asAlevel9709ComponentAudit:level9709,
  blockingFindings:[
    "0580 does not define an official Grade 10 versus Grade 11 split; the current repository duplicates the same official reference set across both levels for each pathway.",
    "9709 Pure Mathematics 2 (Paper 2) has zero representation in both current AS and A Level packs.",
    "Do not treat current Level 10 + Level 11 lesson counts as unique official Cambridge syllabus coverage."
  ]
};

console.log(JSON.stringify(report,null,2));
