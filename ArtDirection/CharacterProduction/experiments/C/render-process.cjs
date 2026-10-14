/* Render the authored drawing sequence; this is not a screen recording. */
const fs=require('fs');
const path=require('path');
const sharp=require('sharp');
const root=__dirname;
const data=JSON.parse(fs.readFileSync(path.join(root,'drawing-data.json'),'utf8'));
const original=fs.readFileSync(path.join(root,'C.svg'),'utf8');
const defs=original.slice(0,original.indexOf('<g id='));
const titles=['Phác hình và tỷ lệ','Mảng màu lớn','Bóng và khối','Mắt, mặt và phụ kiện','Nét và hoàn thiện'];
const timings=[0,.14,.32,.55,.72,1];
const groups=data.slots;
const drawing=data.elements;
const phases=Array.from({length:5},(_,i)=>drawing.filter(e=>e.stage===i+1));
const draft=(data.sketch.match(/<path[^>]+\/>/g)||[]);

function state(t){
 let phase=timings.findIndex((x,i)=>i<5&&t>=x&&t<timings[i+1]);
 if(phase<0)phase=4;
 const f=Math.min(1,Math.max(0,(t-timings[phase])/(timings[phase+1]-timings[phase])));
 const done=new Set(phases.slice(0,phase).flat().map(e=>e.id));
 phases[phase].slice(0,Math.ceil(phases[phase].length*f)).forEach(e=>done.add(e.id));
 const sketch=phase===0?`<g fill="none" stroke="#6c94a5" stroke-width="2.6" opacity=".75">${draft.slice(0,Math.max(1,Math.ceil(draft.length*f))).join('')}</g>`:'';
 return {phase,f,done,sketch};
}
function art(t){
 const s=state(t);
 let svg=defs+s.sketch;
 for(const slot of groups){
  svg+=`<g id="${slot}">`;
  svg+=drawing.filter(e=>e.slot===slot&&s.done.has(e.id)).map(e=>e.markup).join('');
  svg+='</g>';
 }
 return {svg:svg+'</svg>',...s};
}
function frameSvg(t){
 const a=art(t);
 const picture=a.svg.replace(/<svg[^>]*>/,'<svg x="20" y="88" width="600" height="600" viewBox="0 0 1024 1024">');
 return `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="750" viewBox="0 0 640 750">
 <rect width="640" height="750" fill="#f1efe9"/>
 <g font-family="Arial,sans-serif" fill="#253348"><text x="26" y="32" font-size="20" font-weight="700">NGỰ THÚ · VẼ BẰNG SVG</text>
 <text x="26" y="59" font-size="14">${a.phase+1}/5 · ${titles[a.phase]}</text>
 <text x="614" y="59" text-anchor="end" font-size="12">${a.done.size} / 1600 phần tử</text></g>
 <rect x="20" y="82" width="600" height="606" rx="12" fill="#e2e7e7"/>
 ${picture}
 <rect x="26" y="706" width="588" height="4" rx="2" fill="#d3dbd9"/>
 <rect x="26" y="706" width="${(588*t).toFixed(2)}" height="4" rx="2" fill="#388b7c"/>
 <text x="26" y="733" font-size="11" font-family="Arial,sans-serif" fill="#536372">Mô phỏng các bước vẽ vector · Không phải video thao tác trong Krita</text>
 </svg>`;
}
async function run(){
 fs.mkdirSync(path.join(root,'process-frames'),{recursive:true});
 // 36 seconds at 8 fps, followed by a still final frame in the GIF encoder.
 for(let i=0;i<=288;i++){
  await sharp(Buffer.from(frameSvg(i/288))).png().toFile(path.join(root,'process-frames',String(i).padStart(4,'0')+'.png'));
 }
 const stages=[];
 for(let i=0;i<5;i++) stages.push({title:titles[i],start:timings[i],end:timings[i+1]});
 fs.writeFileSync(path.join(root,'process-timeline.json'),JSON.stringify({kind:'illustrated reconstruction of drawing stages, built after the static drawing',screen_recording:false,seconds:36,fps:8,final_hold_seconds:3,stages},null,2)+'\n');
 const script=`const host=document.getElementById('player'),range=document.getElementById('progress'),play=document.getElementById('play'),label=document.getElementById('phase'),count=document.getElementById('count');
 const ends=${JSON.stringify(timings)}, names=${JSON.stringify(titles)}, records=${JSON.stringify(drawing.map(e=>({id:e.id,stage:e.stage})))}, shapes=records.map(e=>({node:document.getElementById(e.id),stage:e.stage}));
 const draft=document.getElementById('draft'),draftPaths=[...draft.querySelectorAll('path')];
 let t=0,playing=false,last=0;
 function show(value){t=Math.max(0,Math.min(1,value)); let phase=ends.findIndex((x,i)=>i<5&&t>=x&&t<ends[i+1]);if(phase<0)phase=4; const frac=Math.min(1,(t-ends[phase])/(ends[phase+1]-ends[phase])); let visible=0;const same=shapes.filter(e=>e.stage===phase+1),take=Math.ceil(same.length*frac),allowed=new Set(same.slice(0,take)); for(const e of shapes){const on=e.stage<phase+1||allowed.has(e);e.node.style.display=on?'':'none';if(on)visible++;}draft.style.display=phase===0?'':'none';draftPaths.forEach((p,i)=>p.style.display=i<Math.max(1,Math.ceil(draftPaths.length*frac))?'':'none');range.value=Math.round(t*1000);label.textContent=(phase+1)+'/5 · '+names[phase];count.textContent=visible+' / 1600 phần tử';}
 function stop(){playing=false;play.textContent='Phát';play.setAttribute('aria-pressed','false');}
 play.addEventListener('click',()=>{if(playing){stop();return;}if(t>=1)show(0);playing=true;last=performance.now();play.textContent='Tạm dừng';play.setAttribute('aria-pressed','true');requestAnimationFrame(tick);});
 function tick(now){if(!playing)return;show(t+(now-last)/36000);last=now;if(t>=1){stop();return;}requestAnimationFrame(tick);}
 range.addEventListener('input',()=>{stop();show(Number(range.value)/1000);});
 document.querySelectorAll('[data-phase]').forEach(b=>b.addEventListener('click',()=>{stop();show(ends[Number(b.dataset.phase)]-.00001);}));
 document.getElementById('restart').addEventListener('click',()=>{stop();show(0);});
 document.getElementById('zoom').addEventListener('change',e=>{host.classList.toggle('large',e.target.checked);});
 show(1);`;
 const sketch=data.sketch.replace('<g ','<g id="draft" ');
 const embedded=original.replace('<g id=',sketch+'<g id=');
 const page=`<!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Ngự Thú — quá trình vẽ SVG</title><style>
 *{box-sizing:border-box}body{margin:0;background:#f1efe9;color:#253348;font:15px/1.5 system-ui,sans-serif}main{max-width:1100px;margin:24px auto;padding:0 24px}header{display:flex;justify-content:space-between;gap:24px;align-items:center}h1{font-size:24px;margin:0 0 4px}p{margin:0;color:#5c6872}.workspace{display:grid;grid-template-columns:minmax(0,1fr) 290px;gap:24px;margin-top:20px}.canvas{background:#e2e7e7;border-radius:16px;overflow:auto;max-height:76vh}.canvas svg{display:block;width:100%;height:auto}.canvas.large svg{min-width:1200px}aside{padding-top:8px}.controls{display:flex;gap:8px;flex-wrap:wrap;margin:16px 0}button{font:inherit;padding:8px 14px;border:1px solid #c2ceca;border-radius:9px;background:#fff;color:#253348;cursor:pointer}button.primary{background:#347c6f;color:white;border-color:#347c6f}button:focus-visible,input:focus-visible{outline:3px solid #518ead;outline-offset:3px}input[type=range]{width:100%;accent-color:#347c6f}label{display:block;margin-top:16px}small{display:block;color:#61717c;margin-top:16px}.steps{display:grid;gap:8px;margin-top:20px}.steps button{text-align:left;background:transparent}.metrics{margin-top:24px;border-top:1px solid #cbd4d1;padding-top:14px}.metrics strong{font-size:24px}a{color:#257164}footer{margin-top:16px;font-size:13px;color:#61717c}@media(max-width:700px){.workspace{grid-template-columns:1fr}.canvas{max-height:none}header{display:block}h1{font-size:21px}aside{padding:0}.steps{grid-template-columns:1fr 1fr}.metrics{margin-top:20px}}
 </style></head><body><main><header><div><h1>Mẫu C · Nhân vật vector</h1><p>Cùng thiết kế tham chiếu A · 17 nhóm tách riêng</p></div><a href="C-editable.svg" download>Tải SVG dễ biên tập</a></header>
 <div class="workspace"><div id="player" class="canvas" aria-label="Quá trình vẽ nhân vật">${embedded}</div><aside><strong id="phase">5/5 · Nét và hoàn thiện</strong><p id="count">1600 / 1600 phần tử</p><div class="controls"><button id="play" class="primary" aria-pressed="false">Phát</button><button id="restart">Về đầu</button></div><label for="progress">Tiến trình</label><input id="progress" type="range" min="0" max="1000" value="1000" aria-label="Tiến trình vẽ"><div class="steps">${titles.map((t,i)=>`<button data-phase="${i+1}">${i+1}. ${t}</button>`).join('')}</div><label><input type="checkbox" id="zoom"> Phóng lớn để xem nét</label><div class="metrics"><strong>177</strong><p>đường vector trong bản dễ biên tập</p><small>Bản 1600 phần tử gồm 129 mảng màu và 1471 đoạn nét. Nét được chia để phát lại; không phải 1600 chi tiết thiết kế độc lập.</small></div><small>Animation mô phỏng các bước vẽ, được tạo sau bản hình tĩnh. Không phải video quay thao tác; chưa có rig nhân vật.</small></aside></div><footer>Vector được viết theo ảnh tham chiếu, không nhúng raster và không dùng thuật toán trace ảnh.</footer></main><script>${script}</script></body></html>`;
 fs.writeFileSync(path.join(root,'process.html'),page);
 console.log('289 frames and interactive replay saved');
}
run().catch(e=>{console.error(e);process.exitCode=1});
