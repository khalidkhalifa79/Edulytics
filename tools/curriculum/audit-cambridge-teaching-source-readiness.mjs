import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),"../..");
const registry=JSON.parse(fs.readFileSync(path.join(root,"tools/curriculum/cambridge-teaching-source-registry.v1.json"),"utf8"));
const artifacts=[];
for(const p of registry.programmes??[]){
 const required=[];
 if(p.officialScopeLocal) required.push(["Official scope",p.officialScopeLocal]);
 for(const x of p.officialSchemeOfWorkLocal??[]) required.push(["Official Scheme of Work",x]);
 if(p.officialSupportLocal) required.push(["Official support",p.officialSupportLocal]);
 for(const [kind,rel] of required){
  const full=path.join(root,rel);
  artifacts.push({programme:p.programme,kind,relativePath:rel,exists:fs.existsSync(full),sizeBytes:fs.existsSync(full)?fs.statSync(full).size:null});
 }
}
const missing=artifacts.filter(x=>!x.exists);
const programmeReady=(registry.programmes??[]).every(p=>String(p.readiness??"").startsWith("BUILD_READY_")&&!p.blocker);
const report={
 generatedAt:new Date().toISOString(),
 policy:registry.policy,
 artifacts,
 summary:{
  requiredOfficialArtifacts:artifacts.length,
  officialArtifactsPresent:artifacts.length-missing.length,
  missingOfficialArtifacts:missing.length,
  programmeReadinessDeclared:programmeReady,
  supplementalFullBookAccessIsBuildBlocker:registry.policy?.supplementalBookFullAccessRequiredForBuild===true,
  officialBuildSourceReady:missing.length===0&&programmeReady
 },
 gate:missing.length===0&&programmeReady?"READY_FOR_CAMBRIDGE_LESSON_BUILD":"BLOCK_CAMBRIDGE_LESSON_BUILD"
};
console.log(JSON.stringify(report,null,2));
if(process.argv.includes("--require-ready")&&!report.summary.officialBuildSourceReady)process.exitCode=1;
