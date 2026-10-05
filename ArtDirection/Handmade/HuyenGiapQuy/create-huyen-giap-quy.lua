-- Huyen Giap Quy: original hand-authored pixel clusters for Linh Thu Ky.
-- Run in Aseprite: source has separate anatomy, armour, and facial layers.
-- No external images. RGB pixels use only linhthu-v1 palette; alpha is binary.
-- Canvas 64x64, ground pixel y52, Unity pivot (32,12), PPU16.
local out = '/private/tmp/huyen-giap-quy-v6/'
local spr = Sprite(64,64,ColorMode.RGB)
local hex = {
  O='1b1f2e', N='263046', P='41405d', Q='3f4d70',
  D='2f5a4b', G='4c8063', J='3fa88b', L='83b865',
  C='e8ddb6', W='f7f0d8', A='c98b4a', Y='f1c86d', Z='fbe7a1',
}
local c={}
for k,v in pairs(hex) do c[k]=app.pixelColor.rgba(tonumber(v:sub(1,2),16),tonumber(v:sub(3,4),16),tonumber(v:sub(5,6),16),255) end
local locked={'1b1f2e','263046','41405d','5e5a7e','3f4d70','55678f','899cbd','8e8a6e','b1ad87','e8ddb6','f7f0d8','fdfbf2','2f5a4b','4c8063','83b865','b4d67e','b86a86','e6a0b3','f6ced6','8a3a52','c45e72','c98b4a','f1c86d','fbe7a1','5a3a3a','8a5a44','b8805c','f2c9a8','3e7fa6','8fcbe0','cfeff5','3fa88b','e8774a'}
local pal=Palette(#locked)
for i,v in ipairs(locked) do pal:setColor(i-1,Color{r=tonumber(v:sub(1,2),16),g=tonumber(v:sub(3,4),16),b=tonumber(v:sub(5,6),16),a=255}) end
spr:setPalette(pal)
local function px(im,x,y,col)
  if x>=0 and x<64 and y>=0 and y<64 then im:drawPixel(x,y,c[col]) end
end
local function line(im,x0,y0,x1,y1,col)
  local dx,dy=math.abs(x1-x0),-math.abs(y1-y0)
  local sx,sy=x0<x1 and 1 or -1,y0<y1 and 1 or -1
  local er=dx+dy
  while true do
    px(im,x0,y0,col)
    if x0==x1 and y0==y1 then break end
    local e=2*er
    if e>=dy then er=er+dy;x0=x0+sx end
    if e<=dx then er=er+dx;y0=y0+sy end
  end
end
local function rect(im,x,y,w,h,col)
  for yy=y,y+h-1 do for xx=x,x+w-1 do px(im,xx,yy,col) end end
end
local function poly(im,pts,col,edge)
  local y0,y1=64,-1
  for _,p in ipairs(pts) do y0=math.min(y0,p[2]);y1=math.max(y1,p[2]) end
  for y=y0,y1 do
    local xs={}
    for i,p in ipairs(pts) do
      local q=pts[i%#pts+1]
      if (p[2]<=y and q[2]>y) or (q[2]<=y and p[2]>y) then xs[#xs+1]=p[1]+(y-p[2])*(q[1]-p[1])/(q[2]-p[2]) end
    end
    table.sort(xs)
    for i=1,#xs-1,2 do for x=math.ceil(xs[i]),math.floor(xs[i+1]) do px(im,x,y,col) end end
  end
  if edge then for i,p in ipairs(pts) do local q=pts[i%#pts+1];line(im,p[1],p[2],q[1],q[2],edge) end end
end
local function at(pts,dx,dy)
  local p={};for _,v in ipairs(pts) do p[#p+1]={v[1]+dx,v[2]+dy} end;return p
end
local names={'01 Tail','02 Far legs','03 Belly','04 Black shell','05 Golden armour and seal','06 Near legs','07 Head and neck','08 Horns','09 Face'}
local layers={}
for i,name in ipairs(names) do local l=i==1 and spr.layers[1] or spr:newLayer();l.name=name;layers[i]=l end
for i=2,10 do spr:newEmptyFrame() end
local stride={-2,0,2,2,0,-2}
local opposite={2,0,-2,-2,0,2}
local bobs={0,-1,0,0,-1,0}
for f=1,10 do
  local walking=f>=5
  local k=walking and f-4 or 1
  local bob=walking and bobs[k] or (f==2 and -1 or 0)
  local dxA=walking and stride[k] or 0
  local dxB=walking and opposite[k] or 0
  local liftA=walking and ((k==2 or k==3) and -2 or 0) or 0
  local liftB=walking and ((k==5 or k==6) and -2 or 0) or 0
  local hy=walking and bob or (f==4 and 1 or bob)
  local blink=not walking and f==3
  local ims={};for i=1,#layers do ims[i]=Image(64,64,ColorMode.RGB) end
  local tail,far,belly,shell,gold,feet,head,horns,face=table.unpack(ims)

  -- Short swept tail; its tiny fin reads at the back of the heavy shell.
  poly(tail,at({{7,41},{11,39},{16,41},{18,44},{15,46},{12,44},{9,44}},0,bob),'G','O')
  line(tail,10,41+bob,14,42+bob,'L')
  line(tail,12,43+bob,16,44+bob,'D')

  -- Opposite diagonal leg pairs support the planted ground line.
  poly(far,at({{16,39},{20,39},{24,43},{24,47},{19,48},{16,46}},dxB+5,-1+liftB),'D','O')
  poly(far,at({{34,39},{40,39},{44,43},{44,47},{39,48},{36,46}},dxA+5,-1+liftA),'D','O')
  line(far,23+dxB,45+liftB,27+dxB,45+liftB,'G')
  line(far,44+dxA,45+liftA,47+dxA,45+liftA,'G')
  rect(far,25+dxB,46+liftB,2,1,'C');rect(far,45+dxA,46+liftA,2,1,'C')

  -- Small warm belly below the black carapace, rather than a bright shell.
  poly(belly,at({{13,36},{23,34},{40,35},{45,39},{43,45},{37,48},{22,47},{15,44}},0,bob),'D','O')
  poly(belly,at({{17,41},{23,40},{40,40},{42,44},{37,46},{22,46},{17,44}},0,bob),'C',nil)
  poly(belly,at({{19,42},{28,42},{29,45},{22,45},{19,44}},0,bob),'W',nil)
  line(belly,29,43+bob,38,43+bob,'A')

  -- Tall domed shell. Three dark material planes remain broad and empty.
  poly(shell,at({{15,24},{19,21},{25,19},{33,19},{39,21},{43,25},{46,30},{47,36},{45,40},{40,44},{20,44},{13,41},{10,36},{10,31},{12,27}},0,bob),'O','O')
  poly(shell,at({{15,25},{20,22},{26,20},{32,20},{36,22},{34,25},{27,26},{23,30},{17,31},{12,31}},0,bob),'N',nil)
  poly(shell,at({{16,25},{21,22},{26,21},{30,21},{24,23},{22,25},{17,28},{13,29}},0,bob),'P',nil)
  line(shell,20,23+bob,25,21+bob,'Q')
  poly(shell,at({{35,23},{40,26},{44,31},{45,36},{41,40},{35,40},{34,35},{37,29}},0,bob),'O',nil)
  poly(shell,at({{12,34},{16,36},{24,37},{27,40},{37,41},{41,39},{43,40},{39,43},{21,43},{14,40},{11,36}},0,bob),'O',nil)
  poly(shell,at({{23,29},{27,26},{33,26},{36,29},{34,35},{28,38},{22,34}},0,bob),'O',nil)
  poly(shell,at({{24,29},{27,27},{32,27},{28,30},{23,32}},0,bob),'N',nil)

  -- Gold follows scute joins, with one compact seal; dark gaps stay open.
  line(gold,14,27+bob,19,23+bob,'Y')
  line(gold,19,23+bob,25,21+bob,'Y')
  line(gold,25,21+bob,32,21+bob,'A')
  line(gold,32,21+bob,38,23+bob,'A')
  line(gold,13,31+bob,18,32+bob,'A')
  line(gold,18,32+bob,23,29+bob,'Y')
  line(gold,23,29+bob,27,26+bob,'Y')
  line(gold,27,26+bob,33,26+bob,'Y')
  line(gold,33,26+bob,37,29+bob,'A')
  line(gold,37,29+bob,35,35+bob,'A')
  line(gold,35,35+bob,28,38+bob,'A')
  line(gold,28,38+bob,22,34+bob,'Y')
  line(gold,22,34+bob,23,29+bob,'Y')
  line(gold,20,24+bob,20,29+bob,'A')
  line(gold,33,22+bob,33,25+bob,'A')
  line(gold,38,29+bob,42,27+bob,'A')
  line(gold,36,35+bob,43,38+bob,'A')
  line(gold,17,33+bob,16,37+bob,'A')
  line(gold,22,35+bob,23,39+bob,'A')
  line(gold,28,39+bob,29,41+bob,'A')
  -- The lower rim is bevelled armour, not a continuous bright bracelet.
  poly(gold,at({{12,36},{15,39},{22,41},{38,41},{44,37},{44,40},{39,43},{22,43},{15,41},{12,38}},0,bob),'A','O')
  line(gold,13,37+bob,16,40+bob,'Y')
  line(gold,16,40+bob,22,42+bob,'Y')
  line(gold,23,42+bob,28,42+bob,'Y')
  line(gold,31,42+bob,36,42+bob,'Y')
  line(gold,39,41+bob,42,39+bob,'Y')
  rect(gold,19,40+bob,2,1,'Z');px(gold,34,41+bob,'Z')
  -- A four-point talisman is the only ornament inside the central scute.
  poly(gold,at({{29,28},{32,31},{29,34},{26,31}},0,bob),'A',nil)
  line(gold,29,29+bob,31,31+bob,'Y')
  line(gold,31,31+bob,29,33+bob,'Y')
  line(gold,29,33+bob,27,31+bob,'Y')
  line(gold,27,31+bob,29,29+bob,'Y')
  px(gold,29,30+bob,'Z');px(gold,29,31+bob,'O');px(gold,29,32+bob,'Y')

  -- Near feet remain chunky; two restrained claws per foot establish turtle anatomy.
  poly(feet,at({{16,42},{21,41},{25,44},{25,47},{24,48},{27,50},{26,52},{19,52},{16,50},{16,46}},dxA,liftA),'G','O')
  poly(feet,at({{34,42},{39,41},{43,43},{43,47},{42,48},{45,50},{44,52},{37,52},{34,50},{33,46}},dxB,liftB),'G','O')
  poly(feet,at({{17,44},{20,43},{23,45},{22,48},{19,48},{17,47}},dxA,liftA),'L',nil)
  poly(feet,at({{35,44},{38,43},{41,45},{40,48},{37,48},{35,47}},dxB,liftB),'L',nil)
  line(feet,19+dxA,49+liftA,24+dxA,49+liftA,'D')
  line(feet,37+dxB,49+liftB,42+dxB,49+liftB,'D')
  rect(feet,21+dxA,51+liftA,2,1,'C');px(feet,25+dxA,51+liftA,'C')
  rect(feet,39+dxB,51+liftB,2,1,'C');px(feet,43+dxB,51+liftB,'C')
  px(feet,18+dxA,44+liftA,'J');px(feet,36+dxB,44+liftB,'J')

  -- Forward head and broad jaw; two short horns are part of its silhouette.
  poly(head,at({{38,35},{42,34},{47,37},{49,42},{45,47},{40,46},{37,42}},0,hy),'D','O')
  poly(head,at({{42,29},{47,27},{52,28},{55,31},{56,34},{58,36},{58,40},{55,44},{49,45},{44,43},{42,39},{41,34}},0,hy),'G','O')
  poly(head,at({{43,31},{46,29},{50,29},{51,31},{47,32},{44,36},{43,38}},0,hy),'L',nil)
  poly(head,at({{44,36},{49,35},{55,36},{57,38},{55,42},{49,44},{45,42}},0,hy),'J',nil)
  poly(head,at({{45,40},{48,39},{56,39},{55,42},{51,44},{47,43}},0,hy),'C',nil)
  line(head,42,42+hy,45,44+hy,'G')
  line(head,45,41+hy,46,42+hy,'W')

  -- Two curved gold horn tips: no antlers, jewellery, or particle effects.
  poly(horns,at({{43,29},{42,26},{42,23},{44,21},{44,25},{46,27},{46,29}},0,hy),'A','O')
  line(horns,43,23+hy,43,26+hy,'Z');px(horns,44,27+hy,'Y')
  poly(horns,at({{49,28},{49,25},{51,23},{51,26},{53,28},{52,29}},0,hy),'Y','O')
  px(horns,50,25+hy,'Z');px(horns,51,27+hy,'A')

  -- Proud, friendly gold eye under a slightly raised brow; a brief idle blink.
  line(face,48,32+hy,52,31+hy,'D')
  if blink then
    line(face,49,35+hy,53,35+hy,'O')
    line(face,49,34+hy,51,34+hy,'Y')
  else
    poly(face,at({{49,33},{52,32},{54,34},{54,36},{52,38},{49,37}},0,hy),'O',nil)
    rect(face,50,33+hy,3,4,'Y')
    rect(face,52,34+hy,1,3,'O')
    px(face,50,33+hy,'W');px(face,50,36+hy,'A')
  end
  px(face,57,36+hy,'O');px(face,56,37+hy,'D')
  line(face,51,41+hy,55,41+hy,'D');px(face,55,40+hy,'D')
  -- Compact forehead diamond links the living animal to its shell seal.
  poly(face,at({{46,30},{48,28},{49,30},{47,32}},0,hy),'A',nil)
  px(face,47,30+hy,'Y');px(face,48,29+hy,'Z')

  for i,l in ipairs(layers) do spr:newCel(l,spr.frames[f],ims[i],Point(0,0)) end
  spr.frames[f].duration=walking and 0.14 or (blink and 0.14 or 0.38)
end
local idle=spr:newTag(spr.frames[1],spr.frames[4]);idle.name='idle'
local walk=spr:newTag(spr.frames[5],spr.frames[10]);walk.name='walk'
spr:saveAs(out..'huyen-giap-quy.aseprite')
print('OK: Huyen Giap Quy 64x64, 10 frames, '..#layers..' editable layers. idle 1-4, walk 5-10, y52, 13 palette colours.')
