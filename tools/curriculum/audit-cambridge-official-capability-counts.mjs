import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const outDir = path.join(root, "artifacts/cambridge-official-audit");
fs.mkdirSync(outDir, { recursive: true });

const primaryLower = {
  1: 36, 2: 48, 3: 53, 4: 46, 5: 51, 6: 54,
  7: 63, 8: 61, 9: 55
};

const igcse = {
  coreAtomicContentPoints: 100,
  extendedAtomicContentPoints: 159,
  comprehensiveUniquePathTarget: 159,
  note: "Cambridge 0580 Extended contains Core plus additional content; do not sum 100+159 for a comprehensive pathway."
};

const level9709ByComponent = {
  1: { name: "Pure Mathematics 1", topLevelCandidateAbilities: 34 },
  2: { name: "Pure Mathematics 2", topLevelCandidateAbilities: 18 },
  3: { name: "Pure Mathematics 3", topLevelCandidateAbilities: 41 },
  4: { name: "Mechanics", topLevelCandidateAbilities: 22 },
  5: { name: "Probability & Statistics 1", topLevelCandidateAbilities: 17 },
  6: { name: "Probability & Statistics 2", topLevelCandidateAbilities: 21 }
};

const primaryLowerTotal = Object.values(primaryLower).reduce((a,b)=>a+b,0);
const level9709Total = Object.values(level9709ByComponent)
  .reduce((sum,x)=>sum+x.topLevelCandidateAbilities,0);

const report = {
  generatedAt: new Date().toISOString(),
  sourceBasis: {
    "0096": "Cambridge Primary Mathematics Curriculum Framework",
    "0862": "Cambridge Lower Secondary Mathematics Curriculum Framework",
    "0580": "Cambridge IGCSE Mathematics syllabus 2025-2027",
    "9709": "Cambridge International AS & A Level Mathematics syllabus 2026-2027"
  },
  officialOutcomeUnits: {
    primaryAndLowerStages1to9: primaryLowerTotal,
    byStage: primaryLower,
    igcse0580: igcse,
    asAlevel9709AllComponentsTopLevelCandidateAbilities: level9709Total,
    byComponent9709: level9709ByComponent,
    comprehensivePathwayMinimumOutcomeUnits:
      primaryLowerTotal + igcse.comprehensiveUniquePathTarget + level9709Total
  },
  interpretation: [
    "779 is a minimum official outcome-unit count under a one-outcome-unit-per-teachable-unit model, not a Cambridge-prescribed lesson count.",
    "Cambridge 0580 does not prescribe an official Grade 10 versus Grade 11 split. A Scheme of Work or official/endorsed sequencing resource is required to allocate the two-year syllabus into school-year lessons.",
    "9709 Paper 2 is an official AS-only alternative route and is largely a subset of Paper 3. If Edulytics supports every official route, Paper 2 still needs explicit representation.",
    "Learner-facing Edulytics lessons may combine multiple official outcomes or split one broad official outcome into multiple lessons, but every lesson must retain an auditable official Cambridge reference."
  ]
};

const target = path.join(outDir, "cambridge-official-capability-counts.v1.json");
fs.writeFileSync(target, JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
