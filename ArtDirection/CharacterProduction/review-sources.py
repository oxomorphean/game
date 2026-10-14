"""Krita source checks and review sheets for the two static art experiments.

Source documents are restored after visibility/color tests. Comparison images
are layout diagrams, not runtime screenshots or additional generated artwork.
"""
from pathlib import Path
import json, hashlib, datetime
from krita import Krita, InfoObject
from PyQt5.QtCore import Qt, QByteArray, QPointF, QRectF
from PyQt5.QtGui import QImage, QPainter, QPen, QColor, QFont, QPainterPath

ROOT=Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/CharacterProduction')
PROJECT=ROOT.parent.parent
APP=Krita.instance(); S=1024

def blank(w=S,h=S,color=None):
    q=QImage(w,h,QImage.Format_ARGB32);q.fill(QColor(color) if color else 0);return q

def data(q):
    q=q.convertToFormat(QImage.Format_ARGB32);bits=q.constBits();bits.setsize(q.sizeInBytes());return bytes(bits)

def node_img(n):
    return QImage(bytes(n.projectionPixelData(0,0,S,S)),S,S,S*4,QImage.Format_ARGB32).copy()

def flush(d):d.refreshProjection();d.waitForDone()

def png(d,path):
    flush(d);info=InfoObject();info.setProperty('alpha',True);assert d.exportImage(str(path),info)

def bounds(q):
    b=data(q);xs=[];ys=[]
    for y in range(q.height()):
        for x in range(q.width()):
            if b[(y*q.width()+x)*4+3]>16:xs.append(x);ys.append(y)
    return [min(xs),min(ys),max(xs)+1,max(ys)+1] if xs else None

def count(q):return sum(1 for a in data(q)[3::4] if a)

def txt(p,x,y,text,size=16,bold=False,color='#263244'):
    f=QFont('Arial',size);f.setBold(bold);p.setFont(f);p.setPen(QColor(color));p.drawText(x,y,text)

def image_at(p,im,cx,ground,h,box=None):
    box=box or bounds(im); x,y,r,b=box;scale=h/(b-y)
    w=round(im.width()*scale); hh=round(im.height()*scale)
    # Pre-filter when reducing large art; canvas bilinear minification aliases details.
    reduced=im.scaled(w,hh,Qt.IgnoreAspectRatio,Qt.SmoothTransformation)
    p.drawImage(round(cx-(x+r)*.5*scale),round(ground-b*scale),reduced)

report={'date':datetime.datetime.now().astimezone().isoformat(),'scope':'static art/source experiments; no Unity changes', 'samples':{}}
images={v:QImage(str(ROOT/'experiments'/v/(v+'.png'))) for v in ['A','B']}
for variant,root_name in ([] if globals().get('BOARDS_ONLY',False) else [('A','Character'),('B','Drawn_Character')]):
    folder=ROOT/'experiments'/variant
    # Re-open the saved native source, rather than trusting the live drawing view.
    d=APP.openDocument(str(folder/(variant+'.kra'))); assert d;d.setBatchmode(True)
    root=d.nodeByName(root_name); assert root
    flush(d)
    before=node_img(root); png(d,folder/(variant+'.png'));images[variant]=QImage(str(folder/(variant+'.png')))
    slots={n.name():{'nontransparent_pixels':count(node_img(n)),'bounds':bounds(node_img(n))} for n in root.childNodes()}
    entry={'reopened_kra':True,'canvas':[d.width(),d.height()],'slots':slots,'tests':{}}
    tests=[('without-eyes',['Eyes']),('without-hair',['Hair_Back','Hair_Front','Hair_Accessory'])]
    clothing=['Torso','Coat','Belt','Sleeve_Back','Sleeve_Front','Leg_Back','Leg_Front','Boot_Back','Boot_Front','Cloth_Details']
    tests.append(('without-clothes',clothing))
    for name,names in tests:
        targets=[n for n in root.childNodes() if n.name() in names]
        for n in targets:n.setVisible(False)
        png(d,folder/(name+'.png'))
        entry['tests'][name]={'changed':data(QImage(str(folder/(name+'.png'))))!=data(images[variant]),'hidden_slots':[n.name() for n in targets]}
        for n in targets:n.setVisible(True)
    # A color-edit trial on a duplicate pixel buffer; default source is restored.
    eye=next(n for n in root.childNodes() if n.name()=='Eyes')
    iris=eye.childNodes()[-1] if variant=='A' else next(n for n in eye.childNodes() if n.name()=='Iris')
    old=bytes(iris.pixelData(0,0,S,S));edited=bytearray(old);changed=0
    for j in range(0,len(edited),4):
        b,g,r,a=edited[j:j+4]
        if a>0 and g>r*1.15 and g>b*.85:
            # Preserve local luminance while changing jade to amber.
            lum=max(r,g,b);edited[j:j+3]=bytes([int(lum*.33),int(lum*.70),min(255,int(lum*1.15))]);changed+=1
    assert iris.setPixelData(QByteArray(bytes(edited)),0,0,S,S)
    png(d,folder/'eyes-amber.png');assert iris.setPixelData(QByteArray(old),0,0,S,S)
    flush(d)
    assert data(before)==data(node_img(root)), 'Source was not restored after tests'
    entry['tests']['eyes-amber']={'changed_pixels':changed,'source_restored_exactly':True,'method':'temporary color transform for editability check; not new drawing'}
    # Export a clean PSD with one raster layer per slot, then reopen it.
    delivery=APP.createDocument(S,S,variant+' flattened slots','RGBA','U8','',72);delivery.setBatchmode(True)
    for n in delivery.rootNode().childNodes():n.remove()
    for n in root.childNodes():
        layer=delivery.createNode(n.name(),'paintlayer');delivery.rootNode().addChildNode(layer,None)
        assert layer.setPixelData(n.projectionPixelData(0,0,S,S),0,0,S,S)
    flush(delivery);assert delivery.exportImage(str(folder/(variant+'.psd')),InfoObject())
    back=APP.openDocument(str(folder/(variant+'.psd')));assert back;back.setBatchmode(True);flush(back)
    p1=data(node_img(delivery.rootNode()));p2=data(node_img(back.rootNode()))
    maxdiff=max(abs(x-y) for x,y in zip(p1,p2));entry['psd_roundtrip_max_channel_difference']=maxdiff
    opaque_max=0;alpha_max=0;composite_max=0;visible_changed=0
    for j in range(0,len(p1),4):
        aa=p1[j+3];ab=p2[j+3];alpha_max=max(alpha_max,abs(aa-ab));pixel_max=0
        for c in range(3):
            if aa==255 and ab==255:opaque_max=max(opaque_max,abs(p1[j+c]-p2[j+c]))
            for bg in (0,225,255):
                ca=(p1[j+c]*aa+bg*(255-aa))/255
                cb=(p2[j+c]*ab+bg*(255-ab))/255
                pixel_max=max(pixel_max,abs(ca-cb))
        composite_max=max(composite_max,pixel_max)
        if pixel_max>2:visible_changed+=1
    entry['psd_roundtrip']={'opaque_rgb_max_difference':opaque_max,'alpha_max_difference':alpha_max,
      'over_black_gray_white_max_difference':round(composite_max,4),'pixels_over_2_levels':visible_changed,
      'status':'PASS' if composite_max<=2 else 'FAIL'}
    png(back,folder/'psd-roundtrip.png')
    back.close();delivery.close();d.close()
    report['samples'][variant]=entry

# Existing baseline is normalized once for comparison only.
base=QImage(str(PROJECT/'ArtDirection/HeroProduction2D/assembled-ivory.png'))
box=bounds(base);scale=800/(box[3]-box[1]);norm=blank();p=QPainter(norm);p.setRenderHint(QPainter.SmoothPixmapTransform)
p.translate(512-(box[0]+box[2])*.5*scale,100-box[1]*scale);p.scale(scale,scale);p.drawImage(0,0,base);p.end()
images['Baseline']=norm

board=blank(1440,1100,'#F2F0EB');p=QPainter(board);p.setRenderHint(QPainter.Antialiasing);p.setRenderHint(QPainter.SmoothPixmapTransform)
txt(p,36,39,'NGỰ THÚ  /  THỬ NGHIỆM NGUỒN NHÂN VẬT',23,True)
txt(p,36,69,'Cùng thiết kế và chiều cao hiển thị. Hình tĩnh để đánh giá; chưa phải animation hoặc bản Unity.',14)
grass=QImage(str(PROJECT/'Assets/_Project/Art/CartoonV2/Terrain/meadow-grass.png'))
columns=[('Baseline','Nguồn hiện tại',36),('A','A · ImageGen + tách lớp Krita',510),('B','B · Vẽ bằng công cụ Krita',984)]
for key,title,x in columns:
    txt(p,x,111,title,18,True)
    p.fillRect(x,130,420,444,QColor('#E2E4E5'))
    image_at(p,images[key],x+210,559,400)
    txt(p,x+14,154,'400 px',12,color='#647080')
    txt(p,x,605,'KÍCH THƯỚC CHƠI · nền xám',12,True)
    p.fillRect(x,618,420,145,QColor('#E2E4E5'))
    for i,h in enumerate([55,72,85]):
        image_at(p,images[key],x+70+i*140,734,h);txt(p,x+52+i*140,753,str(h)+' px',11)
    txt(p,x,795,'TRÊN TEXTURE CỎ · bố cục thử, chưa vào Unity',11,True)
    p.drawImage(QRectF(x,808,420,145),grass)
    for i,h in enumerate([55,72,85]): image_at(p,images[key],x+70+i*140,931,h)
    notes={
      'Baseline':['Rig thử đã có; thân dựng bằng script.','Giữ nguyên để đối chiếu.'],
      'A':['Tạo hình tốt hơn; nét/màu còn gộp trong raster.','Cần sửa vùng bị che và chuẩn hóa nét trước rig.'],
      'B':['27 lớp nét/mảng được vẽ bằng công cụ.','Hình còn cứng, mắt và mảng bóng chưa đạt.']}
    for j,line in enumerate(notes[key]):txt(p,x,985+j*24,line,13)
txt(p,36,1066,'Chưa mẫu nào được duyệt làm asset sản xuất. Nguồn và giới hạn: ArtDirection/CharacterProduction/RESULTS.md',13)
p.end();board.save(str(ROOT/'comparison.png'))

checks=blank(1500,840,'#F2F0EB');p=QPainter(checks);p.setRenderHint(QPainter.SmoothPixmapTransform)
txt(p,24,35,'KIỂM TRA NGUỒN · BẬT/TẮT SLOT VÀ ĐỔI MÀU MẮT',21,True)
variants=[('default','Đầy đủ'),('without-eyes','Ẩn mắt'),('without-hair','Ẩn tóc'),('eyes-amber','Mắt màu hổ phách'),('without-clothes','Ẩn đồ · thiếu thân gốc')]
for row,v in enumerate(['A','B']):
    y=95+row*360
    for col,(file,label) in enumerate(variants):
        x=20+col*296;p.fillRect(x,y,282,310,QColor('#E2E4E5'));txt(p,x,y-12,v+' · '+label,13,True)
        im=images[v] if file=='default' else QImage(str(ROOT/'experiments'/v/(file+'.png')))
        # Same full-canvas transform; hiding a slot must not rescale the rest.
        p.drawImage(QRectF(x-45,y-17,372,372),im)
txt(p,24,815,'Ẩn đồ cho thấy giới hạn thật: hai mẫu chưa có cơ thể đầy đủ phía dưới, chưa thể coi là bộ thay đồ sản xuất.',14)
p.end();checks.save(str(ROOT/'editability.png'))

# Paint rules: a visible reference sheet, deliberately labelled as diagrams.
sheet=blank(1440,1050,'#F5F2EB');p=QPainter(sheet);p.setRenderHint(QPainter.Antialiasing)
txt(p,36,43,'NGỰ THÚ  /  NÉT · MÀU · VẬT LIỆU',24,True)
txt(p,36,75,'Bảng hướng dẫn ứng viên v1. Các hình bên dưới là sơ đồ kỹ thuật, không phải artwork đã duyệt.',14)
txt(p,36,122,'NÉT  /  canvas 1024 · nhân vật cao khoảng 800 px',17,True)
for y,w,label in [(164,8,'Viền ngoài · 8 px'),(210,4,'Nét trong · 4 px'),(256,7,'Viền vàng · 7 px')]:
    p.setPen(QPen(QColor('#C9A365' if w==7 else '#20283B'),w,Qt.SolidLine,Qt.RoundCap));p.drawLine(50,y,315,y)
    txt(p,344,y+6,label,14)
txt(p,36,315,'Basic-5 Size Opacity · opacity 100% · nét đặc · không texture',14)
txt(p,36,341,'Bezier cho contour; mỗi slot có cùng tọa độ và độ dày thị giác.',14)
txt(p,36,386,'PALETTE  /  nền — bóng — sáng',17,True)
palette=[('Tóc bạc',['EDF0EF','ABB9CD','FFFBEF']),('Da',['F3C5A2','CD927D','FFE2BE']),('Vải ngà',['F2E8CD','B4B8AA','FFF5D8']),('Vải xanh đen',['334561','202D46','566B80']),('Ngọc',['438F85','255960','9BD3B7']),('Kim loại vàng',['C9A365','866747','EDD5A0'])]
for i,(name,colors) in enumerate(palette):
    y=416+i*84;txt(p,36,y+30,name,14,True)
    for j,h in enumerate(colors):
        x=205+j*154;p.fillRect(x,y,128,44,QColor('#'+h));txt(p,x,y+65,'#'+h,12)
txt(p,775,122,'VÍ DỤ MẢNG VẬT LIỆU',17,True)
for i,(name,colors) in enumerate([palette[2],palette[0],palette[4],palette[5]]):
    x=790+(i%2)*294;y=165+(i//2)*302
    shape=QPainterPath();shape.moveTo(x+20,y+40);shape.cubicTo(x+60,y+10,x+150,y+10,x+208,y+45);shape.lineTo(x+218,y+189);shape.quadTo(x+118,y+221,x+12,y+182);shape.closeSubpath()
    p.setPen(QPen(QColor('#20283B'),8));p.setBrush(QColor('#'+colors[0]));p.drawPath(shape)
    p.save();p.setClipPath(shape)
    shadow=QPainterPath();shadow.moveTo(x+128,y+25);shadow.quadTo(x+84,y+135,x+32,y+199);shadow.lineTo(x+225,y+220);shadow.lineTo(x+240,y+90);shadow.closeSubpath()
    p.setPen(Qt.NoPen);p.setBrush(QColor('#'+colors[1]));p.drawPath(shadow)
    p.setPen(QPen(QColor('#'+colors[2]),10,Qt.SolidLine,Qt.RoundCap));p.drawLine(x+47,y+66,x+102,y+53)
    p.restore();txt(p,x+20,y+248,name,15,True)
txt(p,775,803,'2–3 sắc độ · bóng theo khối · điểm sáng ít',15,True)
txt(p,775,839,'Không vẽ viền vào phần khớp bị che.',14)
txt(p,775,868,'Không thêm họa tiết nếu mất nghĩa ở 72 px.',14)
txt(p,775,897,'Giữ mặt và tóc sáng hơn thân áo.',14)
txt(p,36,995,'Dùng palette character-v1.gpl. Mẫu nghệ thuật vẫn phải qua duyệt hình và kiểm tra chuyển động riêng.',14)
p.end();sheet.save(str(ROOT/'paint-standard.png'))

(ROOT/'brush-settings.json').write_text(json.dumps({'preset':'b) Basic-5 Size Opacity','engine':'Krita built-in pixel brush','opacity':1,'outer_px':8,'inner_px':4,'gold_px':7,'canvas_px':[1024,1024],'character_height_px':800,'input':'mouse/tool control, constant pressure','note':'Values used in B; not a claim that the B artwork meets the style bar.'},indent=2)+'\n')
if not globals().get('BOARDS_ONLY',False):
    (ROOT/'evidence/source-checks.json').write_text(json.dumps(report,indent=2)+'\n')
print('Review sheets saved.' if globals().get('BOARDS_ONLY',False) else 'Reopened both KRA; PSD round trips and source restoration checked; three review sheets saved.')
