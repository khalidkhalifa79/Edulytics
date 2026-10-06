import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),"../..");
const file=path.join(root,"tools/curriculum/cambridge-teaching-source-registry.v1.json");
const d=JSON.parse(fs.readFileSync(file,"utf8"));
d.purpose="Cambridge Mathematics source registry. Official framework/syllabus proves scope; official Scheme of Work or syllabus component structure controls sequence/scope; publisher books are supplemental references for depth and examples and are not a build blocker.";
d.policy={
  officialScopeRequired:true,
  officialSequenceOrComponentStructureRequired:true,
  supplementalBookReferenceRecommended:true,
  supplementalBookFullAccessRequiredForBuild:false,
  noSyntheticSupportingInsideOfficialCurriculum:true,
  everyLearnerLessonRequiresOfficialMapping:true,
  sourceAuthorityOrder:[
    "Cambridge official framework/syllabus",
    "Cambridge official Scheme of Work or syllabus component structure",
    "Cambridge-endorsed/published textbook as supplemental reference",
    "Edulytics independently authored learner-facing lesson"
  ]
};
d.programmes=[
 {
  programme:"Cambridge Primary Mathematics 0096",
  stages:[1,2,3,4,5,6],
  officialScopeSource:"Cambridge Primary Mathematics Curriculum Framework 0096 v2.1 / June 2025",
  officialScopeLocal:"artifacts/curriculum-sources/cambridge/verified-mirrors/CAMBRIDGE_PRIMARY_MATHEMATICS_0096_FRAMEWORK_V2.1.pdf",
  sequenceAuthority:"Cambridge Official Scheme of Work",
  officialSchemeOfWorkLocal:[1,2,3,4,5,6].map(s=>`artifacts/curriculum-sources/cambridge/teaching-sources/primary/0096_STAGE${s}_SCHEME_OF_WORK.docx`),
  supplementalSeries:"Cambridge Primary Mathematics Learner's Books, Second Edition",
  supplementalPublisher:"Cambridge University Press",
  supplementalBooks:[
   [1,"9781108746410"],[2,"9781108746441"],[3,"9781108746489"],
   [4,"9781108745291"],[5,"9781108760034"],[6,"9781108746328"]
  ].map(([stage,isbn])=>({stage,isbn,verification:"USER_LOCAL_REFERENCE_VERIFIED_NOT_COMMITTED"})),
  readiness:"BUILD_READY_OFFICIAL_SCOPE_AND_SEQUENCE",
  blocker:null
 },
 {
  programme:"Cambridge Lower Secondary Mathematics 0862",
  stages:[7,8,9],
  officialScopeSource:"Cambridge Lower Secondary Mathematics Curriculum Framework 0862 v3.0",
  officialScopeLocal:"artifacts/curriculum-sources/cambridge/verified-mirrors/CAMBRIDGE_LOWER_SECONDARY_MATHEMATICS_0862_FRAMEWORK_V3.0.pdf",
  sequenceAuthority:"Cambridge Official Scheme of Work",
  officialSchemeOfWorkLocal:[7,8,9].map(s=>`artifacts/curriculum-sources/cambridge/teaching-sources/lower-secondary/0862_STAGE${s}_SCHEME_OF_WORK.docx`),
  supplementalSeries:"Cambridge Lower Secondary Mathematics Learner's Books, Second Edition",
  supplementalPublisher:"Cambridge University Press",
  supplementalBooks:[
   [7,"9781108746342"],[8,"9781108771528"],[9,"9781108783774"]
  ].map(([stage,isbn])=>({stage,isbn,verification:"USER_LOCAL_REFERENCE_VERIFIED_NOT_COMMITTED"})),
  readiness:"BUILD_READY_OFFICIAL_SCOPE_AND_SEQUENCE",
  blocker:null
 },
 {
  programme:"Cambridge IGCSE Mathematics 0580",
  stages:["Upper Secondary / two-year course"],
  officialScopeSource:"Cambridge IGCSE Mathematics 0580 syllabus 2025-2027",
  officialScopeLocal:"artifacts/curriculum-sources/cambridge/official/CAMBRIDGE_IGCSE_MATHEMATICS_0580_2025-2027_SYLLABUS.pdf",
  sequenceAuthority:"Edulytics two-year progression over official 0580 syllabus scope",
  officialSupportLocal:"artifacts/curriculum-sources/cambridge/teaching-sources/0580_SUPPORT.pdf",
  supplementalBook:{
   title:"Cambridge IGCSE Core and Extended Mathematics Fifth Edition",
   publisher:"Hachette Learning",
   year:2023,
   isbn:"9781398373914",
   verification:"USER_LOCAL_REFERENCE_VERIFIED_NOT_COMMITTED"
  },
  readiness:"BUILD_READY_OFFICIAL_SCOPE_AND_TWO_YEAR_PROGRESSION",
  blocker:null
 },
 {
  programme:"Cambridge International AS & A Level Mathematics 9709",
  stages:["AS","A Level"],
  officialScopeSource:"Cambridge International AS & A Level Mathematics 9709 syllabus 2026-2027",
  officialScopeLocal:"artifacts/curriculum-sources/cambridge/official/CAMBRIDGE_AS_A_LEVEL_MATHEMATICS_9709_2026-2027_SYLLABUS.pdf",
  sequenceAuthority:"Cambridge 9709 official component and route structure",
  officialSupportLocal:"artifacts/curriculum-sources/cambridge/teaching-sources/support9709.pdf",
  supplementalTitles:[
   "Pure Mathematics 1","Pure Mathematics 2 & 3","Mechanics",
   "Probability & Statistics 1","Probability & Statistics 2"
  ],
  supplementalReferenceVerification:"USER_LOCAL_REFERENCE_VERIFIED_NOT_COMMITTED",
  readiness:"BUILD_READY_OFFICIAL_SCOPE_AND_ROUTE_STRUCTURE",
  blocker:null
 }
];
fs.writeFileSync(file,JSON.stringify(d,null,2)+"\n");
