import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";

const root="C:/Users/khali/Downloads/Edulytics-UAE-Audit";
const packPath=path.join(root,"src/Edulytics.Core/Curriculum/Packs/uae-moe-math.curriculum-pack.json");
const bpDir=path.join(root,"src/Edulytics.Core/Curriculum/LessonBlueprints/Packs");
const cpDir=path.join(root,"src/Edulytics.Core/Curriculum/LessonContent/Packs");
const hash=s=>crypto.createHash("sha256").update(s).digest("hex");

const pack=JSON.parse(fs.readFileSync(packPath,"utf8"));
const active=new Map();
const activeCatalogs=new Set();

for(const f of fs.readdirSync(bpDir)){
  if(!/^uae-g.*\.lesson-blueprint\.json$/i.test(f)) continue;
  const bp=JSON.parse(fs.readFileSync(path.join(bpDir,f),"utf8"));
  const contentFile=path.join(cpDir,f.replace(".lesson-blueprint.json",".lesson-content-pack.json"));
  if(!fs.existsSync(contentFile)) continue;
  const cp=JSON.parse(fs.readFileSync(contentFile,"utf8"));
  const content=new Map(cp.Lessons.map(x=>[x.LessonCode,x]));

  for(const l of bp.Lessons){
    if(!l.OfficialReferenceCode) continue;
    const bits=l.OfficialReferenceCode.split(":");
    if(bits[0]!=="UAE"||bits[1]!=="REF"||bits[2]!=="TEXTBOOK"||!/^G\d+$/.test(bits[3]||"")) continue;
    const grade=+bits[3].slice(1);
    const pathway=(bits[4]||String(bp.Pathway||"COMMON")).toUpperCase();
    const term=(bits[5]||"ANNUAL").toUpperCase();
    const catalog=`UAE:CATALOG:G${grade}:${pathway}:${term}`;
    activeCatalogs.add(catalog);
    active.set(l.OfficialReferenceCode,{
      grade,pathway,term,catalog,title:l.Title,
      content:content.get(l.LessonCode),
      lessonCode:l.LessonCode
    });
  }
}

const obsolete=new Set();
for(const n of pack.Nodes){
  const code=n.Code||"";
  if(/^UAE:REF:TEXTBOOK:G\d+:/.test(code) && !active.has(code)) obsolete.add(code);
  if(/^UAE:CATALOG:G\d+:/.test(code) && !activeCatalogs.has(code)) obsolete.add(code);
  if(/^UAE:(?:REF:TEXTBOOK|CATALOG):G[56]:ADVANCED:/.test(code)) obsolete.add(code);
}

pack.Nodes=pack.Nodes.filter(n=>!obsolete.has(n.Code));
pack.Links=pack.Links.filter(l=>!obsolete.has(l.FromCode)&&!obsolete.has(l.ToCode));

const codes=new Set(pack.Nodes.map(n=>n.Code));
let sort=Math.max(0,...pack.Nodes.map(n=>n.SortOrder||0));

for(const catalog of [...activeCatalogs].sort()){
  if(codes.has(catalog)) continue;
  const sample=[...active.values()].find(x=>x.catalog===catalog);
  const source=sample?.content?.SourceUrl||"https://minhaji.moe.gov.ae/";
  const pathway=sample.pathway[0]+sample.pathway.slice(1).toLowerCase();
  const n={
    Code:catalog,Kind:"SourceCatalog",ParentCode:null,
    LogicalLevelFrom:sample.grade,LogicalLevelTo:sample.grade,
    NativeLevel:"Grade "+sample.grade,Pathway:pathway,
    Title:`Grade ${sample.grade} — ${pathway} — ${sample.term} verified textbook catalog`,
    OfficialText:null,
    AuthorDescription:"Verified UAE textbook scope catalog. Previous-official fallback is explicitly identified where a current complete edition is unavailable.",
    SourceAuthority:"UAE Ministry of Education",
    SourceUrl:source,SourceLocator:source,
    Attribution:"UAE Ministry of Education textbook provenance; external retrieval pages are discovery channels only.",
    IsOfficial:false,IsActive:true,SortOrder:++sort,ContentHash:""
  };
  n.ContentHash=hash([n.Code,n.Title,n.AuthorDescription,n.SourceAuthority,n.SourceUrl,n.SourceLocator,n.Attribution].join("|"));
  pack.Nodes.push(n);codes.add(catalog);
}

let added=0;
for(const [code,x] of [...active.entries()].sort(([a],[b])=>a.localeCompare(b))){
  if(codes.has(code)) continue;
  const c=x.content||{};
  const pathway=x.pathway[0]+x.pathway.slice(1).toLowerCase();
  const source=c.SourceUrl||"https://minhaji.moe.gov.ae/";
  const n={
    Code:code,Kind:"Reference",ParentCode:x.catalog,
    LogicalLevelFrom:x.grade,LogicalLevelTo:x.grade,
    NativeLevel:"Grade "+x.grade,Pathway:pathway,
    Title:`Grade ${x.grade} ${pathway} textbook lesson reference — ${x.title}`,
    OfficialText:null,
    AuthorDescription:"Reference-only evidence that the verified UAE Student Edition covers this lesson. This is not an Outcome or Standard code and does not reproduce textbook body text.",
    SourceAuthority:"UAE Ministry of Education",
    SourceUrl:source,
    SourceLocator:c.SourceLocator||(`Grade ${x.grade} / ${pathway} / ${x.term}`),
    Attribution:"UAE Ministry of Education textbook reference only; learner-facing lesson content remains independently authored by Edulytics.",
    IsOfficial:true,IsActive:true,SortOrder:++sort,ContentHash:""
  };
  n.ContentHash=hash([n.Code,n.ParentCode,n.Title,n.AuthorDescription,n.SourceAuthority,n.SourceUrl,n.SourceLocator,n.Attribution].join("|"));
  pack.Nodes.push(n);codes.add(code);added++;
}

pack.NodeCount=pack.Nodes.length;
pack.LinkCount=pack.Links.length;
pack.OfficialNodeCount=pack.Nodes.filter(n=>n.IsOfficial&&["Standard","Outcome","Reference"].includes(n.Kind)).length;
pack.ContentDigest=hash(JSON.stringify({nodes:pack.Nodes,links:pack.Links}));
fs.writeFileSync(packPath,JSON.stringify(pack,null,2)+"\n");

console.log(JSON.stringify({
  activeReferences:active.size,
  activeCatalogs:activeCatalogs.size,
  removed:obsolete.size,
  added,
  nodeCount:pack.NodeCount,
  officialNodeCount:pack.OfficialNodeCount,
  linkCount:pack.LinkCount,
  contentDigest:pack.ContentDigest
},null,2));