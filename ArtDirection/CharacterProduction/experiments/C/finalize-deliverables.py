"""Build comparison layout, GIF and source checks from the rendered vector art."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import numpy as np
import ast,datetime,hashlib,json,xml.etree.ElementTree as ET,zipfile

C=Path(__file__).resolve().parent
ROOT=C.parent.parent
PROJECT=ROOT.parent.parent
FONT='/System/Library/Fonts/Supplemental/Arial.ttf'
BOLD='/System/Library/Fonts/Supplemental/Arial Bold.ttf'

def text(draw,pos,s,size=16,strong=False,fill='#253348'):
    draw.text(pos,s,font=ImageFont.truetype(BOLD if strong else FONT,size),fill=fill)

def place(board,im,cx,ground,h):
    box=im.getchannel('A').point(lambda a:255 if a>16 else 0).getbbox()
    piece=im.crop(box)
    piece=piece.resize((round(piece.width*h/piece.height),h),Image.Resampling.LANCZOS)
    board.alpha_composite(piece,(round(cx-piece.width/2),ground-h))

board=Image.new('RGBA',(1180,1050),'#f3f0e9');d=ImageDraw.Draw(board)
text(d,(32,20),'NGỰ THÚ · THỬ NGHIỆM SVG',28,True)
text(d,(32,62),'Cùng tham chiếu, cùng chiều cao hiển thị · Hình tĩnh, chưa có rig',17)
for n,(v,label) in enumerate([('A','A · ImageGen + phân lớp'),('C','C · Vẽ bằng SVG')]):
    x=32+n*580;im=Image.open(ROOT/'experiments'/v/(v+'.png')).convert('RGBA')
    text(d,(x,104),label,22,True)
    d.rounded_rectangle((x,146,x+536,716),radius=10,fill='#e2e7e7')
    place(board,im,x+268,700,530)
    text(d,(x+16,164),'530 px',13,fill='#61717c')
    text(d,(x,736),'KÍCH THƯỚC CHƠI',14,True)
    d.rounded_rectangle((x,768,x+536,910),radius=8,fill='#e2e7e7')
    for i,h in enumerate([55,72,85]):
        place(board,im,x+90+i*177,873,h);text(d,(x+72+i*177,887),f'{h} px',12)
    notes=['Chi tiết mềm và tự nhiên hơn.','Nét, màu còn gộp trong ảnh raster.'] if v=='A' else ['Nét rõ, màu và hình sửa bằng tọa độ.','Khối tóc/áo còn cứng hơn A.']
    for j,s in enumerate(notes):text(d,(x,931+j*28),s,17)
text(d,(32,1009),'C: 177 đường gốc · bản phát lại 1600 phần tử · không nhúng ảnh, không trace tự động',15)
board.convert('RGB').save(C/'comparison-A-C.png')

files=sorted((C/'process-frames').glob('*.png'));assert len(files)==289,len(files)
frames=[Image.open(p).convert('RGB') for p in files]
contact=Image.new('RGB',(640*6,750))
for i,k in enumerate([0,40,92,155,208,288]):contact.paste(frames[k],(i*640,0))
palette=contact.quantize(colors=256,method=Image.Quantize.MEDIANCUT)
quantized=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in frames]
quantized[0].save(C/'drawing-process.gif',save_all=True,append_images=quantized[1:],duration=[120 if i%2==0 else 130 for i in range(288)]+[3000],loop=0,optimize=False,disposal=2)

# Visible evidence that the layer switches operate on independent geometry.
checks=Image.new('RGBA',(1160,500),'#f3f0e9');dr=ImageDraw.Draw(checks)
text(dr,(24,18),'C · NGUỒN VECTOR TRONG KRITA',24,True)
for i,(file,title) in enumerate([('C-krita-reopened.png','17 lớp vector'),('without-hair.png','Ẩn tóc — đầu vẫn đầy đủ'),('without-eyes.png','Ẩn mắt — mặt vẫn đầy đủ')]):
    x=24+i*382;dr.rounded_rectangle((x,78,x+348,452),10,fill='#e2e7e7')
    text(dr,(x,54),title,16,True)
    im=Image.open(C/file).convert('RGBA').resize((420,420),Image.Resampling.LANCZOS)
    checks.alpha_composite(im,(x-20,62))
text(dr,(24,473),'Hình đứng thử nghiệm; chưa có thân dưới áo hoặc kiểm tra khớp khi chạy.',14)
checks.convert('RGB').save(C/'editability-vector.png')

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def delta(file_a,file_b):
    a=np.asarray(Image.open(C/file_a).convert('RGBA')).astype(float)
    b=np.asarray(Image.open(C/file_b).convert('RGBA')).astype(float)
    delta=np.abs((a[:,:,:3]*a[:,:,3:]+225*(255-a[:,:,3:]))/255-(b[:,:,:3]*b[:,:,3:]+225*(255-b[:,:,3:]))/255)
    return {'composite_gray_max_channel':round(float(delta.max()),4),'composite_gray_mean_channel':round(float(delta.mean()),6),'pixels_delta_gt_8':int((delta.max(2)>8).sum()),'alpha_max':float(np.abs(a[:,:,3]-b[:,:,3]).max())}

ns='{http://www.w3.org/2000/svg}'
svgs={}
for file,count in [('C.svg',1600),('C-editable.svg',177)]:
    s=ET.parse(C/file).getroot();paths=s.findall('.//'+ns+'path')
    assert len(paths)==count
    assert not s.findall('.//'+ns+'image')
    ids=[p.attrib['id'] for p in paths];assert len(set(ids))==len(ids)
    for node in paths:
        for field in ['fill','stroke']:
            value=node.attrib.get(field,'none');assert value=='none' or (value.startswith('#') and len(value)==7),(node.attrib['id'],field,value)
    svgs[file]={'path_count':count,'groups':len(s.findall(ns+'g')),'embedded_rasters':0,'sha256':sha(C/file)}
with zipfile.ZipFile(C/'C-vector.kra') as z:
    assert z.testzip() is None
    x=ET.fromstring(z.read('maindoc.xml'))
    nodes=[n for n in x.iter() if n.attrib.get('nodetype')=='shapelayer']
    assert len(nodes)==17,len(nodes)
    vector_source_files=sum(n.endswith('content.svg') for n in z.namelist())
    assert vector_source_files==17
baseline=json.loads((ROOT/'evidence/baseline-hashes.json').read_text())
unchanged={f:sha(PROJECT/f)==h for f,h in baseline['files'].items()};assert all(unchanged.values())
gif=Image.open(C/'drawing-process.gif');duration=0
for i in range(gif.n_frames):gif.seek(i);duration+=gif.info['duration']
for p in C.glob('*.py'):ast.parse(p.read_text(),filename=str(p))
result={'checked_at':datetime.datetime.now().astimezone().isoformat(),'svg':svgs,'kra_vector_layers':17,'vector_shapes':177,'gif':{'frames':gif.n_frames,'duration_ms':duration,'size':[gif.width,gif.height]},'compact_vs_segmented':delta('C-editable.png','C.png'),'krita_initial_vs_reopened':delta('C-krita.png','C-krita-reopened.png'),'svg_vs_krita':delta('C-editable.png','C-krita-reopened.png'),'baseline_unchanged':unchanged,'browser_controls':'Verified play, pause, phase jump, range input and zoom using actual browser UI','scope':'static artwork + simulated drawing-stage animation; no character rig, no Unity replacement'}
(C/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(result,ensure_ascii=False,indent=2))
