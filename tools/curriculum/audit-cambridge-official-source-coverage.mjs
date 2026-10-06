import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const packs = path.join(root, "src/Edulytics.Core/Curriculum/LessonBlueprints/Packs");

const primaryLowerMaxima = {
  1: { Nc:6, Ni:6, Nm:1, Np:4, Nf:4, Gt:3, Gg:8, Gp:1, Ss:3 },
  2: { Nc:6, Ni:7, Nm:2, Np:5, Nf:6, Gt:3, Gg:12, Gp:2, Ss:3, Sp:2 },
  3: { Nc:6, Ni:10, Nm:2, Np:5, Nf:8, Gt:4, Gg:11, Gp:2, Ss:3, Sp:2 },
  4: { Nc:5, Ni:8, Np:5, Nf:7, Gt:4, Gg:9, Gp:3, Ss:3, Sp:2 },
  5: { Nc:4, Ni:8, Np:5, Nf:11, Gt:4, Gg:8, Gp:4, Ss:4, Sp:3 },
  6: { Nc:4, Ni:8, Np:4, Nf:13, Gt:1, Gg:11, Gp:5, Ss:4, Sp:4 },
  7: { Ni:6, Np:2, Nf:11, Ae:7, As:7, Gg:14, Gp:6, Ss:5, Sp:5 },
  8: { Ni:7, Np:2, Nf:11, Ae:7, As:7, Gg:12, Gp:6, Ss:5, Sp:4 },
  9: { Ni:4, Np:2, Nf:8, Ae:7, As:7, Gg:11, Gp:7, Ss:5, Sp:4 }
};

const igcseCounts = {
  C: {
    1:[1,1,1,2,1,1,2,3,3,1,1,3,4,3,3,2],
    2:[2,3,0,2,4,1,3,0,2,2,1,0,0],
    3:[1,1,1,0,1,1,0],
    4:[3,3,2,1,1,3,1,0],
    5:[1,1,2,1,2],
    6:[1,2,0,0,0,0],
    7:[4,0,0,0],
    8:[3,2,1,0],
    9:[1,3,1,1,3,0,0]
  },
  E: {
    1:[1,1,1,2,1,1,2,3,3,2,1,3,5,3,3,2,1,2],
    2:[2,5,2,2,7,4,3,1,4,3,1,4,3],
    3:[1,1,2,2,1,1,1],
    4:[3,3,2,3,2,3,1,1],
    5:[1,1,2,1,2],
    6:[1,4,2,2,2,1],
    7:[4,3,1,4],
    8:[4,2,1,1],
    9:[1,3,3,1,3,2,2]
  }
};

function expectedStage(stage) {
  const out = [];
  for (const [strand, max] of Object.entries(primaryLowerMaxima[stage])) {
    for (let i=1; i<=max; i++) out.push(`${stage}${strand}.${String(i).padStart(2,"0")}`);
  }
  return out;
}

function expectedIgcse(prefix) {
  const out = [];
  for (const [topic, counts] of Object.entries(igcseCounts[prefix])) {
    counts.forEach((count, i) => {
      if (!count) return;
      const ref = `${prefix}${topic}.${i+1}`;
      if (count === 1) out.push(ref);
      else for (let n=1; n<=count; n++) out.push(`${ref}.${n}`);
    });
  }
  return out;
}

function officialCodes(doc) {
  const out = new Set();
  for (const lesson of doc.Lessons ?? []) {
    const codes = [
      ...(lesson.OutcomeCodes ?? []),
      ...(lesson.FormalTargets ?? []).map(x => x.OutcomeCode),
      ...(lesson.Alignments ?? []).map(x => x.OutcomeCode ?? x.ReferenceCode)
    ].filter(Boolean);
    for (const code of codes) out.add(String(code));
  }
  return out;
}

function load(name) {
  return JSON.parse(fs.readFileSync(path.join(packs, name), "utf8").replace(/^\uFEFF/, ""));
}

const rows = [];
for (let stage=1; stage<=9; stage++) {
  const name = fs.readdirSync(packs).find(x =>
    stage <= 6 ? x.startsWith(`cambridge-primary-stage${stage}-`) : x.startsWith(`cambridge-lower-stage${stage}-`));
  const doc = load(name);
  const prefix = stage <= 6 ? "CAM:OUT:0096:" : "CAM:OUT:0862:";
  const have = new Set([...officialCodes(doc)].filter(x => x.startsWith(prefix)).map(x => x.slice(prefix.length)));
  const expected = new Set(expectedStage(stage));
  rows.push({
    programme: stage <= 6 ? "0096" : "0862",
    stage,
    file: name,
    expected: expected.size,
    represented: [...expected].filter(x => have.has(x)).length,
    missing: [...expected].filter(x => !have.has(x)),
    unexpected: [...have].filter(x => !expected.has(x))
  });
}

for (const [kind, prefix] of [["core","C"],["extended","E"]]) {
  const expected = new Set(expectedIgcse(prefix));
  const names = [10,11].map(level => `cambridge-igcse-l${level}-${kind}-ogl-v1.lesson-blueprint.json`);
  for (const name of names) {
    const have = new Set([...officialCodes(load(name))]
      .map(x => x.match(/^CAM:OUT:0580:([CE]\d+\.\d+(?:\.\d+)?)$/i)?.[1]?.toUpperCase())
      .filter(Boolean));
    rows.push({
      programme:"0580", pathway:kind, file:name,
      expected:expected.size,
      represented:[...expected].filter(x=>have.has(x)).length,
      missing:[...expected].filter(x=>!have.has(x)),
      unexpected:[...have].filter(x=>!expected.has(x))
    });
  }
}

const expected9709 = [];
for (const [component,max] of [[1,8],[2,6],[3,9],[4,5],[5,5],[6,5]]) {
  for (let i=1;i<=max;i++) expected9709.push(`${component}.${i}`);
}
const asName="cambridge-as-level-9709-ogl-v1.lesson-blueprint.json";
const aName="cambridge-a-level-9709-ogl-v1.lesson-blueprint.json";
const have9709 = new Set();
for (const name of [asName,aName]) {
  for (const x of officialCodes(load(name))) {
    const m=x.match(/9709:(\d+\.\d+)/);
    if (m) have9709.add(m[1]);
  }
}
rows.push({
  programme:"9709", pathway:"all-routes-union", files:[asName,aName],
  expected:expected9709.length,
  represented:expected9709.filter(x=>have9709.has(x)).length,
  missing:expected9709.filter(x=>!have9709.has(x)),
  unexpected:[...have9709].filter(x=>!expected9709.includes(x))
});

const summary = {
  generatedAt:new Date().toISOString(),
  sourceContract:{
    "0096":"Cambridge Primary Mathematics 0096 Curriculum Framework",
    "0862":"Cambridge Lower Secondary Mathematics 0862 Curriculum Framework",
    "0580":"Cambridge IGCSE Mathematics 0580 syllabus 2025-2027",
    "9709":"Cambridge International AS & A Level Mathematics 9709 syllabus 2026-2027"
  },
  rows
};
console.log(JSON.stringify(summary,null,2));
