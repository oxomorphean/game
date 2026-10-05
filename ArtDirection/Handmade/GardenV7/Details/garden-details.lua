-- Linh Thu Ky v7 garden detail sprites. Native Aseprite pixel clusters only.
local out='/private/tmp/nguthu-garden-detail-v7/'
local colors={
  navy='263046',shade='41405d',stoneD='8e8a6e',stoneM='b1ad87',stone='e8ddb6',cream='f7f0d8',white='fdfbf2',
  greenD='2f5a4b',greenM='4c8063',green='83b865',greenL='b4d67e',pinkD='b86a86',pink='e6a0b3',pinkL='f6ced6',
  wood='8a5a44',amberD='c98b4a',amber='f1c86d',amberL='fbe7a1',water='8fcbe0',waterL='cfeff5'
}
local C={}
for k,v in pairs(colors) do C[k]=app.pixelColor.rgba(tonumber(v:sub(1,2),16),tonumber(v:sub(3,4),16),tonumber(v:sub(5,6),16),255) end
local palette={}
for line in io.lines('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/Palette/linhthu-v1.hex') do palette[#palette+1]=line end
local function new(w,h,name)
  local s=Sprite(w,h,ColorMode.RGB)
  s.layers[1].name=name
  local p=Palette(#palette)
  for i,v in ipairs(palette) do p:setColor(i-1,Color{r=tonumber(v:sub(1,2),16),g=tonumber(v:sub(3,4),16),b=tonumber(v:sub(5,6),16),a=255}) end
  s:setPalette(p)
  local im=Image(w,h,ColorMode.RGB)
  s:newCel(s.layers[1],s.frames[1],im,Point(0,0))
  return s,s.cels[1].image
end
local function px(im,x,y,c) if x>=0 and y>=0 and x<im.width and y<im.height then im:putPixel(x,y,C[c]) end end
local function rect(im,x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do px(im,xx,yy,c) end end end
local function line(im,x0,y0,x1,y1,c)
  local dx,dy=math.abs(x1-x0),math.abs(y1-y0)
  local sx,sy=x0<x1 and 1 or -1,y0<y1 and 1 or -1
  local e=dx-dy
  while true do px(im,x0,y0,c); if x0==x1 and y0==y1 then break end; local e2=e*2; if e2>-dy then e=e-dy; x0=x0+sx end; if e2<dx then e=e+dx; y0=y0+sy end end
end
local function poly(im,p,c)
  local minx,miny,maxx,maxy=im.width,im.height,0,0
  for _,v in ipairs(p) do minx=math.min(minx,v[1]);maxx=math.max(maxx,v[1]);miny=math.min(miny,v[2]);maxy=math.max(maxy,v[2]) end
  for y=miny,maxy do for x=minx,maxx do
    local inside=false; local j=#p
    for i=1,#p do local a,b=p[i],p[j]; if (a[2]>y)~=(b[2]>y) and x<(b[1]-a[1])*(y-a[2])/(b[2]-a[2])+a[1] then inside=not inside end; j=i end
    if inside then px(im,x,y,c) end
  end end
  for i=1,#p do local a,b=p[i],p[i%#p+1];line(im,a[1],a[2],b[1],b[2],c) end
end
local assets={}
local function save(s,im,name,w,h,anchor,footprint,maxc)
  s:saveAs(out..name..'.aseprite');s:saveAs(out..name..'.png')
  assets[#assets+1]={name=name,im=im,w=w,h=h,anchor=anchor,footprint=footprint,maxc=maxc}
  print('EXPORTED '..name..' '..w..'x'..h)
end
-- Low grass: three broad blade clusters, a small leaf bed, no dark contour.
do local s,im=new(16,16,'grass-tuft-a')
  poly(im,{{3,12},{5,11},{11,11},{13,12},{11,13},{5,13}},'greenM')
  poly(im,{{6,12},{4,9},{3,6},{5,7},{7,11}},'green')
  poly(im,{{7,12},{6,8},{7,3},{8,6},{9,12}},'greenM')
  poly(im,{{8,11},{8,6},{9,4},{10,7},{9,12}},'green')
  poly(im,{{9,12},{11,8},{13,6},{12,10},{11,12}},'green')
  line(im,7,6,7,9,'greenL');line(im,10,10,11,8,'greenL');rect(im,5,11,2,1,'greenL')
  save(s,im,'grass-tuft-a',16,16,{8,14},{4,11,8,3},16)
end
do local s,im=new(16,16,'grass-tuft-b')
  poly(im,{{2,12},{6,11},{11,11},{13,12},{10,13},{5,13}},'greenM')
  poly(im,{{5,12},{3,9},{2,7},{5,8},{7,12}},'green')
  poly(im,{{6,12},{5,7},{4,5},{7,7},{8,12}},'greenM')
  poly(im,{{8,12},{8,8},{10,5},{10,8},{9,12}},'green')
  poly(im,{{9,12},{12,9},{13,9},{12,11},{11,12}},'greenL')
  line(im,5,8,6,10,'greenL');line(im,9,8,9,10,'greenL');rect(im,6,12,3,1,'green')
  save(s,im,'grass-tuft-b',16,16,{8,14},{3,11,10,3},16)
end
do local s,im=new(16,16,'flower-cluster')
  poly(im,{{3,12},{5,10},{8,11},{11,9},{13,11},{12,13},{5,13}},'greenM')
  poly(im,{{4,12},{3,10},{6,10},{8,12}},'green');poly(im,{{9,12},{11,10},{13,10},{11,13}},'green')
  line(im,5,12,5,7,'greenM');line(im,9,11,10,5,'greenM')
  rect(im,3,6,5,2,'pinkD');rect(im,4,5,3,4,'pink');rect(im,3,6,2,1,'pinkL');rect(im,5,6,1,1,'cream')
  rect(im,8,4,5,2,'pinkD');rect(im,9,3,3,4,'pink');rect(im,9,3,2,1,'pinkL');rect(im,10,4,1,1,'cream')
  rect(im,9,8,4,2,'cream');rect(im,10,7,2,4,'cream');px(im,10,8,'amber');px(im,11,9,'stone')
  save(s,im,'flower-cluster',16,16,{8,14},{3,11,10,3},16)
end
-- Fern: curved, visibly separated fronds with grouped paired leaves.
do local s,im=new(32,32,'fern')
  poly(im,{{9,27},{12,25},{19,25},{23,27},{21,29},{12,29}},'greenM')
  local fronds={{{15,27},{10,21},{6,16}},{{15,27},{12,17},{10,10}},{{16,27},{16,16},{17,6}},{{17,27},{22,20},{25,12}},{{18,27},{25,24},{28,19}}}
  for _,f in ipairs(fronds) do line(im,f[1][1],f[1][2],f[2][1],f[2][2],'greenD');line(im,f[2][1],f[2][2],f[3][1],f[3][2],'greenM') end
  local leaves={
   {{6,16},{4,14},{8,15},{10,19}},{{7,19},{4,18},{7,17},{11,21}},{{10,23},{6,22},{9,21},{13,24}},
   {{10,11},{8,9},{11,9},{12,15}},{{11,15},{7,13},{10,13},{13,18}},{{12,19},{8,17},{11,17},{14,22}},
   {{17,7},{16,5},{19,6},{17,12}},{{16,12},{13,9},{15,9},{17,16}},{{17,13},{20,10},{21,11},{17,18}},
   {{16,18},{12,15},{14,14},{17,22}},{{17,18},{21,15},{23,16},{18,23}},
   {{25,13},{24,10},{27,11},{24,17}},{{23,18},{22,14},{24,15},{23,21}},{{22,20},{27,16},{28,17},{23,23}},
   {{25,24},{29,20},{29,22},{26,25}},{{23,25},{25,21},{27,22},{25,26}}}
  for i,p in ipairs(leaves) do poly(im,p,i%3==0 and 'greenL' or 'green') end
  line(im,15,25,16,19,'greenM');line(im,16,22,16,17,'greenL');line(im,12,19,11,15,'greenL')
  poly(im,{{12,28},{14,25},{17,26},{21,28},{17,29}},'green')
  save(s,im,'fern',32,32,{16,30},{10,25,13,5},16)
end
-- Simple cultivation garden stone lantern, warm light inside a cream body.
do local s,im=new(32,48,'lantern')
  poly(im,{{10,39},{21,39},{25,41},{23,44},{8,44},{6,42}},'stoneD')
  poly(im,{{8,38},{23,38},{25,40},{23,42},{8,42},{6,40}},'stoneM')
  poly(im,{{8,38},{21,38},{23,40},{8,40},{6,39}},'cream')
  rect(im,13,25,6,13,'stoneM');rect(im,13,25,3,13,'stone');rect(im,13,26,1,10,'cream');rect(im,18,26,1,12,'stoneD')
  poly(im,{{10,24},{21,24},{23,26},{21,28},{10,28},{8,26}},'stoneD')
  poly(im,{{10,23},{21,23},{23,25},{21,26},{10,26},{8,25}},'stone')
  line(im,9,24,19,24,'cream')
  rect(im,9,13,14,11,'stoneD');rect(im,9,13,12,10,'stone');rect(im,9,14,2,9,'cream')
  rect(im,12,15,8,7,'wood');rect(im,13,16,6,5,'amberD');rect(im,13,16,5,4,'amber');rect(im,13,16,2,3,'amberL')
  rect(im,15,16,1,5,'stoneM');rect(im,12,21,8,1,'stoneD');rect(im,20,14,2,8,'stoneM')
  poly(im,{{7,12},{25,12},{23,15},{9,15}},'stoneD')
  poly(im,{{8,10},{24,10},{26,12},{23,13},{9,13},{5,12}},'stoneM')
  poly(im,{{15,5},{18,5},{20,8},{24,10},{21,11},{9,11},{7,10},{12,8}},'stone')
  poly(im,{{15,5},{17,5},{18,8},{13,9},{9,10},{7,10},{12,8}},'cream')
  line(im,8,11,19,11,'cream');line(im,22,11,24,11,'stoneD')
  rect(im,14,3,4,3,'stoneM');rect(im,14,3,2,2,'cream');rect(im,15,2,2,1,'stone')
  rect(im,10,39,9,1,'white');rect(im,10,41,7,1,'stone')
  save(s,im,'lantern',32,48,{16,45},{7,38,18,7},20)
end
do local s,im=new(8,8,'petal')
  rect(im,3,2,2,1,'pinkL');rect(im,2,3,3,2,'pink');rect(im,3,3,2,1,'pinkL');rect(im,3,5,1,1,'pinkD')
  save(s,im,'petal',8,8,{4,4},{2,2,3,4},16)
end
-- Four looping partial ellipses. Each frame leaves >85% of its cell transparent.
do local s,im=new(64,16,'water-ripple')
  local frames={
   {{{3,7,3,'waterL'},{10,7,3,'waterL'},{2,8,2,'water'},{12,8,2,'water'},{3,10,3,'cream'},{10,10,3,'waterL'},{6,11,4,'waterL'},{5,6,5,'water'}}},
   {{{4,6,4,'waterL'},{10,7,3,'waterL'},{2,8,2,'water'},{12,8,2,'water'},{3,10,3,'waterL'},{9,11,3,'cream'},{6,11,3,'waterL'},{4,7,2,'white'}}},
   {{{5,6,5,'waterL'},{3,7,2,'white'},{11,7,2,'waterL'},{2,8,2,'water'},{12,8,2,'water'},{3,10,4,'waterL'},{9,10,4,'cream'},{6,11,4,'water'}}},
   {{{4,6,5,'water'},{3,7,3,'waterL'},{10,7,3,'white'},{2,8,2,'water'},{12,8,2,'water'},{3,10,3,'cream'},{10,10,3,'waterL'},{6,11,4,'waterL'}}}
  }
  for f,v in ipairs(frames) do for _,seg in ipairs(v[1]) do rect(im,(f-1)*16+seg[1],seg[2],seg[3],1,seg[4]) end end
  save(s,im,'water-ripple',64,16,{8,8},{2,6,12,6},6)
end
-- A native contact sheet keeps the same pixel grid; second row contains pond effects.
do local s,im=new(160,80,'native-contact-sheet')
  rect(im,0,0,160,80,'cream');rect(im,4,4,100,46,'green');rect(im,4,53,152,23,'water')
  local positions={{10,23},{31,23},{52,23},{71,12},{117,2},{142,11},{43,57}}
  for i,a in ipairs(assets) do local p=positions[i];im:drawImage(a.im,Point(p[1],p[2])) end
  s:saveAs(out..'preview-1x.png')
  local big=Image(640,320,ColorMode.RGB)
  for y=0,79 do for x=0,159 do local c=im:getPixel(x,y);for yy=0,3 do for xx=0,3 do big:putPixel(x*4+xx,y*4+yy,c) end end end end
  local b=Sprite(640,320,ColorMode.RGB);b:newCel(b.layers[1],b.frames[1],big,Point(0,0));b:saveAs(out..'preview-4x.png')
end
local f=assert(io.open(out..'metadata.json','w'))
f:write('{"ppu":16,"alpha":[0,255],"pixel_origin":"top-left","unity_pivot_origin":"bottom-left","water_frames":4,"water_cell":[16,16],"sprites":[\n')
for i,a in ipairs(assets) do
  local seen={};local minx,miny,maxx,maxy=a.w,a.h,-1,-1;local count=0
  for y=0,a.h-1 do for x=0,a.w-1 do local c=a.im:getPixel(x,y);if app.pixelColor.rgbaA(c)>0 then if not seen[c] then seen[c]=true;count=count+1 end;minx=math.min(minx,x);miny=math.min(miny,y);maxx=math.max(maxx,x);maxy=math.max(maxy,y) end end end
  local pw,ph=a.name=='water-ripple' and 16 or a.w,a.h
  f:write(string.format('{"file":"%s.png","source":"%s.aseprite","size":[%d,%d],"colors":%d,"max_colors":%d,"foot_anchor_px":[%d,%d],"unity_pivot":[%.6f,%.6f],"footprint_px":[%s],"opaque_bbox_px":[%d,%d,%d,%d]}%s\n',a.name,a.name,a.w,a.h,count,a.maxc,a.anchor[1],a.anchor[2],a.anchor[1]/pw,1-a.anchor[2]/ph,table.concat(a.footprint,','),minx,miny,maxx+1,maxy+1,i==#assets and '' or ','))
end
f:write(']}\n');f:close()
print('DONE 7 PNGs, 7 editable native sources, 1x/4x contact sheets, metadata')
