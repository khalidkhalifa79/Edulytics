import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const bpDir = path.join(root, "src/Edulytics.Core/Curriculum/LessonBlueprints/Packs");
const contentDir = path.join(root, "src/Edulytics.Core/Curriculum/LessonContent/Packs");
const sourcePackPath = path.join(root, "src/Edulytics.Core/Curriculum/Packs/cambridge-intl-math.curriculum-pack.json");
const skillMapPath = path.join(root, "src/Edulytics.Core/Mathematics/Curriculum/lesson-skill-mappings.v1.json");
const requireComplete = process.argv.includes("--require-complete");

const parse = file => JSON.parse(fs.readFileSync(file, "utf8").replace(/^\uFEFF/, ""));
const sourcePack = parse(sourcePackPath);
const officialNodes = new Map(
  (sourcePack.Nodes ?? [])
    .filter(x => x.IsOfficial && (x.Kind === "Outcome" || x.Kind === "Reference"))
    .map(x => [x.Code, x])
);
const blueprintFiles = fs.readdirSync(bpDir)
  .filter(x => /^cambridge-.*\.lesson-blueprint\.json$/i.test(x))
  .sort();

function lessonCodes(lesson) {
  return [...new Set([
    ...(lesson.OutcomeCodes ?? []),
    ...(lesson.FormalTargets ?? []).map(x => x.OutcomeCode),
    ...(lesson.Alignments ?? []).map(x => x.OutcomeCode)
  ].filter(Boolean))];
}

const docs = blueprintFiles.map(file => ({
  file,
  doc: parse(path.join(bpDir, file))
}));

const lessonMap = new Map();
const documentRows = [];
let missingContent = 0;
let lessonsWithoutOfficialMapping = 0;
let invalidLessonCodes = 0;

for (const { file, doc } of docs) {
  const contentFile = path.join(contentDir, file.replace(".lesson-blueprint.json", ".lesson-content-pack.json"));
  const content = fs.existsSync(contentFile) ? parse(contentFile) : { Lessons: [] };
  const contentCodes = new Set((content.Lessons ?? []).map(x => x.LessonCode));
  let documentMissingContent = 0;
  let documentMissingOfficial = 0;
  let documentInvalidCodes = 0;

  for (const lesson of doc.Lessons ?? []) {
    const codes = lessonCodes(lesson);
    lessonMap.set(lesson.LessonCode, { file, doc, lesson, codes });
    if (!contentCodes.has(lesson.LessonCode)) {
      missingContent++;
      documentMissingContent++;
    }
    if (!codes.length) {
      lessonsWithoutOfficialMapping++;
      documentMissingOfficial++;
    }
    for (const code of codes) {
      if (!officialNodes.has(code)) {
        invalidLessonCodes++;
        documentInvalidCodes++;
      }
    }
  }

  documentRows.push({
    file,
    logicalLevel: doc.LogicalLevel,
    nativeLevel: doc.NativeLevel,
    pathway: doc.Pathway ?? null,
    lessons: (doc.Lessons ?? []).length,
    missingContent: documentMissingContent,
    lessonsWithoutOfficialMapping: documentMissingOfficial,
    invalidOfficialCodes: documentInvalidCodes
  });
}

function coveredCodes(predicate) {
  const result = new Set();
  for (const { doc } of docs.filter(x => predicate(x.doc, x.file))) {
    for (const lesson of doc.Lessons ?? []) {
      for (const code of lessonCodes(lesson)) result.add(code);
    }
  }
  return result;
}

const coverage = [];
for (let stage = 1; stage <= 9; stage++) {
  const programme = stage <= 6 ? "0096" : "0862";
  const official = [...officialNodes.values()]
    .filter(x => x.Kind === "Outcome" &&
      x.Code.startsWith(`CAM:OUT:${programme}:`) &&
      !x.Code.includes(":TWM.") &&
      x.LogicalLevelFrom === stage)
    .map(x => x.Code)
    .sort();
  const covered = coveredCodes(doc =>
    doc.LogicalLevel === stage ||
    (stage === 1 && doc.NativeLevel === "Cambridge Primary Stage 1"));
  const missing = official.filter(code => !covered.has(code));
  coverage.push({
    scope: `Stage ${stage}`,
    official: official.length,
    covered: official.length - missing.length,
    missing: missing.length,
    missingCodes: missing
  });
}

for (const pathway of ["Core", "Extended"]) {
  const official = [...officialNodes.values()]
    .filter(x => x.Kind === "Outcome" &&
      x.Code.startsWith("CAM:OUT:0580:") &&
      x.Pathway === pathway)
    .map(x => x.Code)
    .sort();
  const covered = coveredCodes(doc =>
    /IGCSE/i.test(doc.NativeLevel ?? "") && doc.Pathway === pathway);
  const missing = official.filter(code => !covered.has(code));
  coverage.push({
    scope: `0580 ${pathway}`,
    official: official.length,
    covered: official.length - missing.length,
    missing: missing.length,
    missingCodes: missing
  });
}

const refs9709 = [...officialNodes.values()]
  .filter(x => x.Kind === "Reference" && x.Code.startsWith("CAM:REF:9709:"))
  .map(x => x.Code)
  .sort();
const covered9709 = coveredCodes((_doc, file) => /9709/i.test(file));
const missing9709 = refs9709.filter(code => !covered9709.has(code));
coverage.push({
  scope: "9709",
  official: refs9709.length,
  covered: refs9709.length - missing9709.length,
  missing: missing9709.length,
  missingCodes: missing9709
});

const skillMap = parse(skillMapPath);
const cambridgeSkillRows = (skillMap.mappings ?? [])
  .filter(x => String(x.lessonCode ?? "").includes("CAMBRIDGE-INTL-MATH"));
let supportingSkillMappings = 0;
let skillMappingsNotOfficial = 0;
let skillMappingCodeMismatches = 0;
const skillProblems = [];

for (const row of cambridgeSkillRows) {
  const source = lessonMap.get(row.lessonCode);
  const mappedCodes = new Set(row.officialOutcomeCodes ?? []);
  const blueprintCodes = new Set(source?.codes ?? []);
  const mismatches = [...mappedCodes].filter(x => !blueprintCodes.has(x) || !officialNodes.has(x));
  const supporting = row.sourceType === "SupportingLesson";
  const notOfficial = row.officialOutcomeMapped !== true || mappedCodes.size === 0;
  if (supporting) supportingSkillMappings++;
  if (notOfficial) skillMappingsNotOfficial++;
  if (mismatches.length) skillMappingCodeMismatches++;
  if (supporting || notOfficial || mismatches.length) {
    skillProblems.push({
      lessonCode: row.lessonCode,
      sourceType: row.sourceType,
      officialOutcomeMapped: row.officialOutcomeMapped,
      officialOutcomeCodes: [...mappedCodes],
      mismatches
    });
  }
}

const report = {
  documents: documentRows.length,
  lessons: lessonMap.size,
  officialSourceNodes: officialNodes.size,
  documentRows,
  lessonIntegrity: {
    missingContent,
    lessonsWithoutOfficialMapping,
    invalidLessonCodes
  },
  skillMappings: {
    totalCambridgeMappings: cambridgeSkillRows.length,
    supportingSkillMappings,
    skillMappingsNotOfficial,
    skillMappingCodeMismatches,
    problems: skillProblems
  },
  coverage,
  summary: {
    totalOfficialCoverageMissing: coverage.reduce((sum, x) => sum + x.missing, 0),
    zeroSupportingSkillMappings: supportingSkillMappings === 0,
    everyLessonHasOfficialMapping: lessonsWithoutOfficialMapping === 0,
    everyLessonCodeIsOfficial: invalidLessonCodes === 0,
    everyBlueprintHasContent: missingContent === 0,
    everyCambridgeSkillMappingIsOfficial:
      skillMappingsNotOfficial === 0 && skillMappingCodeMismatches === 0,
    fullOfficialCoverage: coverage.every(x => x.missing === 0)
  }
};

console.log(JSON.stringify(report, null, 2));

if (requireComplete && (
  missingContent !== 0 ||
  lessonsWithoutOfficialMapping !== 0 ||
  invalidLessonCodes !== 0 ||
  supportingSkillMappings !== 0 ||
  skillMappingsNotOfficial !== 0 ||
  skillMappingCodeMismatches !== 0 ||
  coverage.some(x => x.missing !== 0)
)) {
  process.exitCode = 1;
}
