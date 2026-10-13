"""Pack Blender renders with alpha gutters; preserves one ground pivot for all poses."""
import json
import argparse
from pathlib import Path
from PIL import Image, ImageDraw

p=argparse.ArgumentParser()
p.add_argument('renders',type=Path)
p.add_argument('destination',type=Path)
args=p.parse_args()
manifest=json.loads((args.renders/'manifest.json').read_text())
args.destination.mkdir(parents=True,exist_ok=True)
tile=128;inner=112;padding=8
manifest['renderFrameSize']=manifest['frameSize']
manifest['frameSize']=tile
manifest['pixelsPerUnit']=58
manifest['pivot']=[(padding+v*inner)/tile for v in manifest['pivot']]
for clip in manifest['clips']:
    count=clip['frames']
    sheet=Image.new('RGBA',(count*tile,8*tile))
    for d in range(8):
        for f in range(count):
            path=args.renders/'frames'/clip['id']/f'{d:02}-{f:02}.png'
            im=Image.open(path).convert('RGBA').resize((inner,inner),Image.Resampling.LANCZOS)
            sheet.paste(im,(f*tile+padding,d*tile+padding))
    sheet.save(args.destination/(clip['id']+'.png'),optimize=True)
(args.destination/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
# Contact sheet is authoring evidence, not part of the game's downloadable art.
contact=Image.new('RGB',(4*192,2*218),(229,238,224));draw=ImageDraw.Draw(contact)
for i,d in enumerate((6,7,0,1,2,3,4,5)):
    im=Image.open(args.renders/'frames'/'idle'/f'{d:02}-00.png')
    x=i%4*192;y=i//4*218
    contact.paste(im,(x,y),im)
    draw.text((x+10,y+194),manifest['directions'][d],fill=(26,51,62))
contact.save(args.renders/'eight-directions.png')
print('Packed',sum(c['frames']*8 for c in manifest['clips']),'frames;',sum(f.stat().st_size for f in args.destination.glob('*.png')),'PNG bytes')
