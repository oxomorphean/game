-- Linh Thu Ky: hand-authored ground clusters, native 16px cells, PPU16.
-- Run through Aseprite Lua. All pixels come from the locked 33-colour palette.
local out = '/private/tmp/nguthu-garden-ground-v7/'
local locked={'1b1f2e','263046','41405d','5e5a7e','3f4d70','55678f','899cbd','8e8a6e','b1ad87','e8ddb6','f7f0d8','fdfbf2','2f5a4b','4c8063','83b865','b4d67e','b86a86','e6a0b3','f6ced6','8a3a52','c45e72','c98b4a','f1c86d','fbe7a1','5a3a3a','8a5a44','b8805c','f2c9a8','3e7fa6','8fcbe0','cfeff5','3fa88b','e8774a'}
local h={G='83b865',L='b4d67e',D='4c8063',C='e8ddb6',W='f7f0d8',S='b1ad87',E='f2c9a8'}
local c={}
for k,v in pairs(h) do c[k]=app.pixelColor.rgba(tonumber(v:sub(1,2),16),tonumber(v:sub(3,4),16),tonumber(v:sub(5,6),16),255) end
local pal=Palette(#locked)
for i,v in ipairs(locked) do pal:setColor(i-1,Color{r=tonumber(v:sub(1,2),16),g=tonumber(v:sub(3,4),16),b=tonumber(v:sub(5,6),16),a=255}) end
local names={'grass-a','grass-b','grass-c','grass-d','paving-a','paving-b','paving-moss','paving-edge','earth-path'}
local spr=Sprite(48,48,ColorMode.RGB);spr:setPalette(pal)
local layerNames={'01 Ground base','02 Stone bevels','03 Short cracks','04 Grass clusters','05 Moss and edge','06 Earth planes'}
local layers,ims={},{}
for i,n in ipairs(layerNames) do local l=i==1 and spr.layers[1] or spr:newLayer();l.name=n;layers[i]=l;ims[i]=Image(48,48,ColorMode.RGB) end
local function origin(i) return ((i-1)%3)*16,math.floor((i-1)/3)*16 end
local function px(layer,i,x,y,k)
  assert(x>=0 and x<16 and y>=0 and y<16,'tile-local coordinate')
  local ox,oy=origin(i);ims[layer]:drawPixel(ox+x,oy+y,c[k])
end
local function rect(layer,i,x,y,w,h,k) for yy=y,y+h-1 do for xx=x,x+w-1 do px(layer,i,xx,yy,k) end end end
local function points(layer,i,k,pts) for _,p in ipairs(pts) do px(layer,i,p[1],p[2],k) end end
for i=1,9 do rect(1,i,0,0,16,16,i<=4 and 'G' or (i==9 and 'E' or 'C')) end

-- Sparse connected grass marks; all four outer edges remain the same green.
points(4,1,'D',{{4,6},{5,6},{6,6},{11,12},{12,12}})
points(4,1,'L',{{4,4},{4,5},{6,5},{10,11},{11,11}})
points(4,2,'D',{{10,5},{11,5},{12,5},{3,11},{4,11}})
points(4,2,'L',{{10,3},{10,4},{12,4},{3,10},{4,9},{4,10}})
points(4,3,'D',{{6,11},{7,11},{8,11},{12,5},{13,5}})
points(4,3,'L',{{6,9},{6,10},{8,10},{11,4},{12,4},{13,3}})
points(4,4,'D',{{3,5},{4,5},{10,10},{11,10},{12,10}})
points(4,4,'L',{{3,3},{3,4},{5,4},{10,9},{11,8},{11,9}})

-- Broad cream slabs. Broken bevels avoid closed square grout outlines.
for _,i in ipairs({5,7}) do
  rect(2,i,3,2,6,1,'W');rect(2,i,2,3,1,2,'W')
  rect(2,i,5,13,6,1,'S');px(2,i,11,12,'S')
end
points(3,5,'S',{{11,4},{10,5},{10,6}})
-- The second slab changes edge direction instead of repeating square corners.
rect(2,6,6,1,6,1,'W');px(2,6,5,2,'W')
rect(2,6,13,7,1,5,'S');rect(2,6,11,12,2,1,'S')
points(3,6,'S',{{3,9},{3,10},{4,10}})
rect(2,8,6,1,5,1,'W');rect(2,8,14,10,1,4,'S')

-- Moss occupies corners in connected patches; the middle remains walkable.
points(5,7,'D',{{2,12},{3,12},{4,13},{5,13}})
points(5,7,'G',{{1,10},{2,10},{1,11},{2,11},{3,11},{1,12},{2,12},{3,12},{3,13},{4,13},{5,13},{4,14}})
points(5,7,'L',{{1,10},{2,10},{2,11},{4,12}})
points(5,7,'G',{{12,3},{13,3},{12,4},{13,4},{13,5}})
points(5,7,'D',{{12,5},{13,5}})

-- Edge tile: grass on its left, cream on its right; top/bottom profiles match.
for y=0,15 do
  rect(5,8,0,y,3,1,'G')
  if y>=3 and y<=6 then px(5,8,3,y,'G') end
  if y>=10 and y<=12 then px(5,8,3,y,'G');px(5,8,4,y,'G') end
end
points(5,8,'S',{{3,1},{3,2},{4,7},{3,8},{5,11},{5,12},{4,13}})
points(5,8,'D',{{2,11},{3,11},{3,12},{2,14}})
points(5,8,'L',{{1,4},{2,4},{1,5},{2,12},{3,12}})
points(2,8,'W',{{5,2},{6,2},{7,2}})

-- Quiet warm earth: two shallow planes, no stippled noise or dark pebbles.
points(6,9,'C',{{3,5},{4,5},{5,5},{3,6},{4,6},{10,10},{11,10},{12,10},{11,11},{12,11}})

for i,l in ipairs(layers) do spr:newCel(l,spr.frames[1],ims[i],Point(0,0)) end
for i,n in ipairs(names) do local x,y=origin(i);local sl=spr:newSlice(Rectangle(x,y,16,16));sl.name=n;sl.pivot=Point(8,8) end
spr.gridBounds=Rectangle(0,0,16,16)
spr:saveAs(out..'garden-ground.aseprite')
local atlas=Image(48,48,ColorMode.RGB);atlas:drawSprite(spr,1)
atlas:saveAs(out..'garden-ground-atlas.png')
local tiles={}
for i,n in ipairs(names) do
  local x0,y0=origin(i);local im=Image(16,16,ColorMode.RGB)
  for y=0,15 do for x=0,15 do im:drawPixel(x,y,atlas:getPixel(x0+x,y0+y)) end end
  im:saveAs(out..n..'.png');tiles[i]=im
end
local function scaled(im,factor,path)
  local im2=Image(im.width*factor,im.height*factor,ColorMode.RGB)
  for y=0,im2.height-1 do for x=0,im2.width-1 do im2:drawPixel(x,y,im:getPixel(math.floor(x/factor),math.floor(y/factor))) end end
  im2:saveAs(path)
end
scaled(atlas,4,out..'garden-ground-atlas4x.png')
local preview=Image(256,160,ColorMode.RGB)
-- A fixed nonperiodic placement; this list is art composition, not pixel noise.
local grass={{1,2,4,1,3,2,1,4},{3,1,2,3,4,1,3,2},{4,3,1,2,1,4,2,3},{1,4,3,1,2,3,4,1},{2,1,4,3,1,2,1,3},{3,2,1,4,2,1,3,4},{1,3,2,1,4,3,2,1},{4,1,3,2,1,4,1,2},{2,3,1,4,3,1,2,3},{1,4,2,3,1,2,4,1}}
local paving={{8,5,6,5,6,5,5,6},{8,6,5,5,6,5,6,5},{8,5,5,6,5,6,5,6},{8,6,5,5,5,6,5,5},{8,5,6,5,6,5,6,5},{8,6,5,6,5,5,5,6},{8,5,5,6,5,6,5,5},{8,6,5,5,6,5,5,6},{8,7,6,5,5,7,6,5},{8,5,5,6,7,5,5,7}}
for row=1,10 do for col=1,8 do
  preview:drawImage(tiles[grass[row][col]],Point((col-1)*16,(row-1)*16))
  preview:drawImage(tiles[paving[row][col]],Point(128+(col-1)*16,(row-1)*16))
end end
-- Two earth rows show its calm repeated texture next to a sparse grass field.
for row=8,9 do for col=2,6 do preview:drawImage(tiles[9],Point((col-1)*16,(row-1)*16)) end end
preview:saveAs(out..'tiled-preview1x.png');scaled(preview,4,out..'tiled-preview4x.png')
print('OK: 9 native 16x16 opaque tiles; 48x48 source, 6 editable layers, 9 named slices; 33 locked palette colours installed. Exported atlas and tiled 1x/4x previews.')
