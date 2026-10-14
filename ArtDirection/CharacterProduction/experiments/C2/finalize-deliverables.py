"""Assemble review boards and a Run preview from actual Unity-rendered frames."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import datetime, hashlib, json, xml.etree.ElementTree as ET, zipfile
import numpy as np

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[3]
FONT='/System/Library/Fonts/Supplemental/Arial.ttf'
BOLD='/System/Library/Fonts/Supplemental/Arial Bold.ttf'
COMBOS=['classic-silver','jade-silver','classic-ink','jade-ink']
LABELS=['Áo ngà · tóc bạc','Áo ngọc · tóc bạc','Áo ngà · tóc ngắn','Áo ngọc · tóc ngắn']

def text(d,xy,s,size=17,bold=False,color='#263b48'):
    d.text(xy,s,font=ImageFont.truetype(BOLD if bold else FONT,size),fill=color)

def full_canvas(board,file,cx,ground,size=560):
    im=Image.open(ROOT/file).convert('RGBA').resize((size,size),Image.Resampling.LANCZOS)
    board.alpha_composite(im,(round(cx-548*size/1024),round(ground-898*size/1024)))

board=Image.new('RGBA',(1320,920),'#f5f2ea');d=ImageDraw.Draw(board)
text(d,(28,20),'C2 · THÂN NỀN + NGOẠI HÌNH TÁCH RIÊNG',28,True)
text(d,(28,62),'Cùng canvas, cùng điểm khớp · Nguồn vector Krita · Mẫu một góc 3/4',18)
for i,(file,label) in enumerate(zip(['body-basic-krita.png','classic-silver-krita.png','jade-silver-krita.png','jade-ink-krita.png'],['Thân + đồ cơ bản',LABELS[0],LABELS[1],LABELS[3]])):
    x=24+i*324
    text(d,(x+8,108),label,20,True)
    d.rounded_rectangle((x,148,x+300,656),14,fill='#e0e8e6')
    full_canvas(board,file,x+150,626)
    text(d,(x+8,678),'55 px          72 px          85 px',15)
    im=Image.open(ROOT/file).convert('RGBA');box=im.getchannel('A').getbbox();im=im.crop(box)
    for j,h in enumerate([55,72,85]):
        piece=im.resize((round(im.width*h/im.height),h),Image.Resampling.LANCZOS)
        board.alpha_composite(piece,(x+47+j*99-piece.width//2,802-h))
text(d,(28,836),'52 lớp độc lập · 2 bộ áo + đồ cơ bản · 2 kiểu tóc · 2 màu mắt',20,True)
text(d,(28,875),'Đã dựng Idle / Run trong scene C2 riêng. Chưa thay nhân vật chính hoặc mở thêm góc nhìn.',17)
board.convert('RGB').save(ROOT/'C2-overview.png')

# A single crop is shared across every style and every frame; the motion never recenters.
frames={c:[Image.open(ROOT/'unity-frames'/f'{c}-{i:02}.png').convert('RGB') for i in range(24)] for c in COMBOS}
boxes=[]
for seq in frames.values():
    for im in seq:
        a=np.asarray(im).astype(int);mask=np.max(np.abs(a-a[0,0]),axis=2)>12
        ys,xs=np.where(mask);boxes.append((xs.min(),ys.min(),xs.max()+1,ys.max()+1))
crop=(int(min(b[0] for b in boxes))-16,int(min(b[1] for b in boxes))-14,
      int(max(b[2] for b in boxes))+16,int(max(b[3] for b in boxes))+14)
scale=330/(crop[3]-crop[1]);size=(round((crop[2]-crop[0])*scale),330)
animation=[]
for n in range(24):
    sheet=Image.new('RGB',(1120,464),'#e0e8e6');dr=ImageDraw.Draw(sheet)
    text(dr,(24,16),'C2 · VÒNG CHẠY TRONG UNITY',23,True)
    for i,c in enumerate(COMBOS):
        text(dr,(i*280+18,60),LABELS[i],18,True)
        piece=frames[c][n].crop(crop).resize(size,Image.Resampling.LANCZOS)
        sheet.paste(piece,(i*280+(280-piece.width)//2,93))
    text(dr,(24,438),'24 khung hình từ clip Run · 0,8 giây/vòng · Cùng rig và nhịp chuyển động',15)
    animation.append(sheet)
palette=animation[0].quantize(colors=256,method=Image.Quantize.MEDIANCUT)
encoded=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in animation]
encoded[0].save(ROOT/'C2-run-preview.gif',save_all=True,append_images=encoded[1:],duration=[30,30,40]*8,loop=0,disposal=2,optimize=False)

poses=Image.new('RGB',(1440,1080),'#e0e8e6');dr=ImageDraw.Draw(poses)
for row,c in enumerate(['classic-silver','jade-ink','basic-ink']):
    for col,phase in enumerate([0,6,15,20]):
        im=Image.open(ROOT/'unity-frames'/f'{c}-{phase:02}.png').convert('RGB').crop(crop)
        im.thumbnail((300,310),Image.Resampling.LANCZOS)
        poses.paste(im,(col*360+(360-im.width)//2,row*360+35))
        text(dr,(col*360+16,row*360+8),f'{c} · {phase}/24',16)
poses.save(ROOT/'unity-pose-review.png')

manifest=json.loads((ROOT/'manifest.json').read_text())
svg=ET.parse(ROOT/'C2-source.svg').getroot()
ids=[n.get('id') for n in svg.iter() if n.get('id')]
assert len(ids)==len(set(ids))
assert not any(n.tag.endswith('image') for n in svg.iter())
assert all(Image.open(ROOT/'export'/(l['name']+'.png')).size==(1024,1024) for l in manifest['layers'])
with zipfile.ZipFile(ROOT/'C2-vector.kra') as z:
    layers=[n for n in ET.fromstring(z.read('maindoc.xml')).iter() if n.tag.endswith('layer')]
    assert len(layers)==52 and all(n.get('nodetype')=='shapelayer' for n in layers)
baseline=json.loads((ROOT.parent.parent/'evidence/baseline-hashes.json').read_text())['files']
unchanged={p:hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()==h for p,h in baseline.items()}
assert all(unchanged.values())
original=np.asarray(Image.open(ROOT/'C2-krita.png').convert('RGBA')).astype(float)
reopened=np.asarray(Image.open(ROOT/'C2-reopened.png').convert('RGBA')).astype(float)
def over_gray(a):return (a[:,:,:3]*a[:,:,3:]+225*(255-a[:,:,3:]))/255
diff=np.abs(over_gray(original)-over_gray(reopened))
report={'checked_at':datetime.datetime.now().astimezone().isoformat(),
        'svg_unique_ids':len(ids),'svg_paths':sum(n.tag.endswith('path') for n in svg.iter()),
        'embedded_raster_images':0,'source_canvas':[1024,1024],'vector_layers':len(layers),
        'kra_reopen_difference':{'max_composited_channel':float(diff.max()),'mean_composited_channel':float(diff.mean()),'pixels_over_8':int((diff.max(2)>8).sum())},
        'baseline_unchanged':unchanged,
        'gif':{'frames':24,'duration_ms':800,'method':'Unity Camera renders of evenly sampled native Run clip, encoded as GIF'},
        'runtime_checks':'unity-play-mode-checks.json',
        'limits':['One three-quarter view plus horizontal flip','Rigid cutout joints, no mesh deformation','Full-canvas uncompressed trial textures, not an optimized runtime atlas','No new weapons or hats in this milestone']}
(ROOT/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'vector_layers':len(layers),'paths':report['svg_paths'],'baseline_unchanged':all(unchanged.values()),'reopen_pixels_over_8':report['kra_reopen_difference']['pixels_over_8'],'gif_frames':24}))
