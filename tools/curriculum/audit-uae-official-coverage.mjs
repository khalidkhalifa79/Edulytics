import fs from "node:fs";
import path from "node:path";
const root="C:/Users/khali/Downloads/Edulytics-UAE-Audit";
const bp=path.join(root,"src/Edulytics.Core/Curriculum/LessonBlueprints/Packs");
const cp=path.join(root,"src/Edulytics.Core/Curriculum/LessonContent/Packs");
let total=0,supporting=0,official=0,missing=0;
const courses=[];
for(const f of fs.readdirSync(bp).filter(x=>/^uae-.*\.lesson-blueprint\.json$/i.test(x)).sort()){
 const b=JSON.parse(fs.readFileSync(path.join(bp,f),"utf8"));
 const cfile=path.join(cp,f.replace(".lesson-blueprint.json",".lesson-content-pack.json"));
 const c=fs.existsSync(cfile)?JSON.parse(fs.readFileSync(cfile,"utf8")):{Lessons:[]};
 const cm=new Map(c.Lessons.map(x=>[x.LessonCode,x]));
 let s=0,o=0,m=0;
 for(const l of b.Lessons){total++;const x=cm.get(l.LessonCode);if(!x){missing++;m++;continue;}if(x.IsSupporting){supporting++;s++;}else if((l.OutcomeCodes||[]).length||l.OfficialReferenceCode){official++;o++;}}
 courses.push({file:f,lessons:b.Lessons.length,official:o,supporting:s,missing:m});
}
console.log(JSON.stringify({total,official,supporting,missing,courses},null,2));
