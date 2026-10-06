import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const registryPath = path.join(root, "tools/curriculum/cambridge-teaching-source-registry.v1.json");
const registry = JSON.parse(fs.readFileSync(registryPath, "utf8"));

const expected = [];
for (const programme of registry.programmes ?? []) {
  if (programme.books) {
    for (const book of programme.books) {
      expected.push({
        programme: programme.programme,
        stage: book.stage,
        title: book.title,
        access: book.contentAccess,
        localPath: book.localSample ? path.join(root, book.localSample) : null
      });
    }
  }
  if (programme.localSamples) {
    for (const rel of programme.localSamples) {
      expected.push({
        programme: programme.programme,
        stage: null,
        title: "Local endorsed sample",
        access: "OFFICIAL_SAMPLE_LOCAL",
        localPath: path.join(root, rel)
      });
    }
  }
  if (programme.officialSupportLocal) {
    expected.push({
      programme: programme.programme,
      stage: null,
      title: "Official Cambridge support document",
      access: "OFFICIAL_SUPPORT_LOCAL",
      localPath: path.join(root, programme.officialSupportLocal)
    });
  }
}

const checked = expected.map(x => ({
  ...x,
  exists: x.localPath ? fs.existsSync(x.localPath) : false,
  sizeBytes: x.localPath && fs.existsSync(x.localPath) ? fs.statSync(x.localPath).size : null
}));

const report = {
  generatedAt: new Date().toISOString(),
  policy: {
    fullLessonBuildRequiresOfficialScope: true,
    fullLessonBuildRequiresEndorsedOrOfficialTeachingSource: true,
    samplesDoNotConstituteFullBookAccess: true
  },
  checked,
  summary: {
    registeredTeachingArtifacts: checked.length,
    localArtifactsPresent: checked.filter(x => x.exists).length,
    missingRegisteredLocalArtifacts: checked.filter(x => x.localPath && !x.exists).length,
    fullBookAccessReady: false
  },
  gate: "BLOCK_FULL_CAMBRIDGE_LESSON_BUILD_UNTIL_STAGE_LEVEL_TEACHING_SOURCE_ACCESS_IS_COMPLETE"
};

console.log(JSON.stringify(report, null, 2));
