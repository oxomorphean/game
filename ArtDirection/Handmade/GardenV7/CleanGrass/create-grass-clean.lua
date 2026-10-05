-- Flat grass and two connected broad ground patches, native Aseprite artwork.
local out='/private/tmp/nguthu-grass-clean-v7/'
local hex={'1b1f2e','263046','41405d','5e5a7e','3f4d70','55678f','899cbd','8e8a6e','b1ad87','e8ddb6','f7f0d8','fdfbf2','2f5a4b','4c8063','83b865','b4d67e','b86a86','e6a0b3','f6ced6','8a3a52','c45e72','c98b4a','f1c86d','fbe7a1','5a3a3a','8a5a44','b8805c','f2c9a8','3e7fa6','8fcbe0','cfeff5','3fa88b','e8774a','79aa5e'}
local pal=Palette(#hex)
for i,v in ipairs(hex) do pal:setColor(i-1,Color{r=tonumber(v:sub(1,2),16),g=tonumber(v:sub(3,4),16),b=tonumber(v:sub(5,6),16),a=255}) end
local green=app.pixelColor.rgba(0x83,0xb8,0x65,255)
local shade=app.pixelColor.rgba(0x79,0xaa,0x5e,255)
local function sprite(name,w,h,im)
  local s=Sprite(w,h,ColorMode.RGB);s:setPalette(pal);s.layers[1].name=name
  s:newCel(s.layers[1],s.frames[1],im,Point(0,0))
  local sl=s:newSlice(Rectangle(0,0,w,h));sl.name=name;sl.pivot=Point(w//2,h//2)
  s:saveAs(out..name..'.aseprite');im:saveAs(out..name..'.png')
end
local flat=Image(16,16,ColorMode.RGB)
for y=0,15 do for x=0,15 do flat:drawPixel(x,y,green) end end
sprite('grass-clean',16,16,flat)
local function patch(name,rows,y0)
  local im=Image(64,32,ColorMode.RGB)
  for i,p in ipairs(rows) do for x=p[1],p[2] do im:drawPixel(x,y0+i-1,shade) end end
  sprite(name,64,32,im);return im
end
local a=patch('grass-shade-a',{{20,36},{15,40},{12,44},{10,47},{10,49},{11,49},{12,49},{10,48},{11,46},{13,44},{15,42},{19,39},{22,35},{25,30}},9)
local b=patch('grass-shade-b',{{18,32},{13,38},{11,42},{9,46},{8,49},{7,52},{7,55},{8,56},{10,56},{11,55},{9,53},{10,51},{12,49},{14,47},{17,43},{20,39},{24,37},{28,34}},7)
local preview=Image(160,96,ColorMode.RGB)
for y=0,95 do for x=0,159 do preview:drawPixel(x,y,green) end end
preview:drawImage(a,Point(10,10));preview:drawImage(b,Point(80,48))
preview:saveAs(out..'preview1x.png')
local scaled=Image(640,384,ColorMode.RGB)
for y=0,383 do for x=0,639 do scaled:drawPixel(x,y,preview:getPixel(x//4,y//4)) end end
scaled:saveAs(out..'preview4x.png')
local swatch=Image(17*12,2*12,ColorMode.RGB)
for i,v in ipairs(hex) do
  local rgba=app.pixelColor.rgba(tonumber(v:sub(1,2),16),tonumber(v:sub(3,4),16),tonumber(v:sub(5,6),16),255)
  for yy=0,11 do for xx=0,11 do swatch:drawPixel(((i-1)%17)*12+xx,((i-1)//17)*12+yy,rgba) end end
end
swatch:saveAs(out..'swatch.png')
print('OK: flat 16x16 grass, two 64x32 single-colour connected patches, three native editable sources; palette v2 34 colours.')
