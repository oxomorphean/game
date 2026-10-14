"""Author the shared-canvas hero in Krita Scripter, then save native and PSD sources.

The existing approved head supplies identity. Body, hidden joint overlap and two
outfits are painted here with explicit paths on the same canvas, not cropped and
independently resized fragments. Paths remain in this file for repeatable edits.
"""
from pathlib import Path
import json
from krita import Krita, InfoObject
from PyQt5.QtCore import Qt, QByteArray, QRectF, QPointF
from PyQt5.QtGui import QImage, QPainter, QColor, QPen, QPainterPath, QTransform

ROOT = Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo')
SOURCE = ROOT / 'ArtDirection/HeroProduction2D'
ART = ROOT / 'Assets/_Project/Art/HeroProduction2D'
SIZE = 1024
OUTLINE = '#18202d'
app = Krita.instance()
doc = app.createDocument(SIZE, SIZE, 'Ngự thú sư · 3/4 · nguồn nhiều lớp', 'RGBA', 'U8', '', 72)
doc.setBatchmode(True)
for initial_node in doc.rootNode().childNodes():
    initial_node.remove()
images = {}

def shape(spec):
    p = QPainterPath()
    for command in spec:
        if command[0] == 'M': p.moveTo(*command[1:])
        elif command[0] == 'L': p.lineTo(*command[1:])
        elif command[0] == 'Q': p.quadTo(*command[1:])
        elif command[0] == 'C': p.cubicTo(*command[1:])
        elif command[0] == 'Z': p.closeSubpath()
    return p

def image():
    img = QImage(SIZE, SIZE, QImage.Format_ARGB32)
    img.fill(0)
    return img

def painter(img):
    p = QPainter(img)
    p.setRenderHint(QPainter.Antialiasing)
    p.setRenderHint(QPainter.SmoothPixmapTransform)
    return p

def draw(p, spec, fill=None, stroke=OUTLINE, width=7):
    p.setPen(QPen(QColor(stroke), width, Qt.SolidLine, Qt.RoundCap, Qt.RoundJoin) if stroke else Qt.NoPen)
    p.setBrush(QColor(fill) if fill else Qt.NoBrush)
    p.drawPath(shape(spec))

def ellipse(p, x, y, w, h, fill, stroke=None, width=7):
    p.setPen(QPen(QColor(stroke), width) if stroke else Qt.NoPen)
    p.setBrush(QColor(fill)); p.drawEllipse(QRectF(x, y, w, h))

def layer(name, img, parent=None, visible=True):
    node = doc.createNode(name, 'paintlayer')
    (parent or doc.rootNode()).addChildNode(node, None)
    bits = img.constBits(); bits.setsize(img.sizeInBytes())
    assert node.setPixelData(QByteArray(bytes(bits)), 0, 0, SIZE, SIZE)
    node.setVisible(visible)
    images[name] = img
    return node

def group(name, visible=True):
    node = doc.createNode(name, 'grouplayer')
    doc.rootNode().addChildNode(node, None); node.setVisible(visible)
    return node

# One anatomical template. Coordinates are also consumed by the Unity authoring
# script; the root is the foot point and all garment variants keep these joints.
joints = {
    'Root': [548, 948], 'Pelvis': [548, 724], 'Chest': [540, 575],
    'Head': [556, 503], 'Hair': [494, 216],
    'Arm_Back': [638, 551], 'Elbow_Back': [682, 637], 'Hand_Back': [709, 705],
    'Arm_Front': [437, 543], 'Elbow_Front': [409, 640], 'Hand_Front': [387, 714],
    'Leg_L': [497, 724], 'Knee_L': [470, 809], 'Foot_L': [457, 894],
    'Leg_R': [597, 724], 'Knee_R': [611, 804], 'Foot_R': [625, 878],
    'Coat_L': [493, 678], 'Coat_R': [599, 680], 'Eyes': [587, 433],
}
SOURCE.mkdir(parents=True, exist_ok=True); ART.mkdir(parents=True, exist_ok=True)

body = group('Body')
arm_specs = {
    'Arm_Back': [('M',623,527),('C',654,525,672,556,678,586),('L',718,686),('Q',730,716,709,732),('Q',690,741,677,716),('L',632,610),('Q',609,561,623,527),('Z',)],
    'Arm_Front': [('M',425,519),('C',452,520,461,552,449,582),('L',414,690),('Q',408,728,389,738),('Q',365,737,363,711),('L',398,586),('Q',399,535,425,519),('Z',)],
}
leg_specs = {
    'Leg_R': [('M',574,695),('C',606,682,635,704,642,734),('L',642,839),('Q',639,874,609,879),('Q',582,874,580,845),('L',561,735),('Q',558,707,574,695),('Z',)],
    'Leg_L': [('M',471,685),('C',505,680,529,704,528,738),('L',498,856),('Q',492,885,459,884),('Q',431,877,433,850),('L',447,735),('Q',444,698,471,685),('Z',)],
}
for name, spec in list(leg_specs.items()) + list(arm_specs.items()):
    img=image(); p=painter(img)
    draw(p,spec,'#e8b692')
    x,y=joints[name]; ellipse(p,x-27,y-27,54,54,'#e8b692')
    if name.startswith('Arm'):
        x,y=joints['Hand_Back' if name=='Arm_Back' else 'Hand_Front']
        ellipse(p,x-24,y-12,44,44,'#f3c9a5',OUTLINE,6)
        draw(p,[('M',x-9,y+11),('Q',x-10,y+22,x-1,y+23)],None,OUTLINE,3)
    p.end(); layer(name,img,body)

img=image();p=painter(img)
draw(p,[('M',458,512),('Q',509,480,572,506),('L',637,541),('L',623,686),('Q',555,728,465,679),('L',445,558),('Q',443,530,458,512),('Z',)],'#eee6d5')
p.end();layer('Body_Torso',img,body)

# Both outfits use the same painted sleeve/leg boundaries. Hidden round caps
# extend into the torso/waist; no outline is added at their concealed cuts.
palettes = {
    'Ivory': {'coat':'#f1eddf','shade':'#c9c9bf','light':'#fff9eb','inner':'#31405c','innerShade':'#22304a','trim':'#d2ae5d','belt':'#378e86'},
    'Navy': {'coat':'#304765','shade':'#20314d','light':'#4f6884','inner':'#ebe5d6','innerShade':'#b5bcbb','trim':'#ddd0a4','belt':'#327f79'},
}
outfits=[]
for outfit, colors in palettes.items():
    clothes=group(outfit,outfit=='Ivory');outfits.append(clothes)
    for name,spec in leg_specs.items():
        img=image();p=painter(img)
        draw(p,spec,colors['inner'])
        p.save();p.setClipPath(shape(spec))
        draw(p,[('M',430,735),('L',474,779),('L',521,734),('L',645,713),('L',666,900),('L',418,900),('Z',)],colors['innerShade'],None)
        p.restore();x,y=joints[name];ellipse(p,x-27,y-27,54,54,colors['inner'])
        draw(p,[('M',x-16,y+34),('Q',x+10,y+48,x+7,y+78)],None,colors['innerShade'],4)
        p.end();layer(outfit+'__'+name,img,clothes)
    boots={
        'Foot_L':[('M',434,854),('Q',459,858,488,850),('L',488,902),('Q',492,931,474,945),('Q',449,964,412,947),('Q',406,935,422,907),('Z',)],
        'Foot_R':[('M',592,841),('Q',615,851,639,839),('L',653,886),('Q',680,912,662,926),('Q',632,941,601,918),('Q',583,906,592,882),('Z',)],
    }
    for name,spec in boots.items():
        img=image();p=painter(img);draw(p,spec,'#243852')
        draw(p,[('M',417 if name=='Foot_L' else 599,939 if name=='Foot_L' else 921),('Q',450 if name=='Foot_L' else 633,955 if name=='Foot_L' else 937,478 if name=='Foot_L' else 664,938 if name=='Foot_L' else 920)],None,colors['trim'],5)
        draw(p,[('M',436 if name=='Foot_L' else 598,874 if name=='Foot_L' else 863),('L',479 if name=='Foot_L' else 637,870 if name=='Foot_L' else 860)],None,colors['trim'],6)
        p.end();layer(outfit+'__'+name,img,clothes)
    for name,spec in arm_specs.items():
        img=image();p=painter(img)
        # Sleeve ends stop before the skin hand, with cuffs drawn as an intentional edge.
        p.setClipRect(QRectF(0,0,SIZE,698 if name=='Arm_Back' else 702))
        draw(p,spec,colors['coat'])
        p.save();p.setClipPath(shape(spec))
        draw(p,[('M',340,589),('Q',416,625,467,592),('L',659,573),('Q',693,620,734,660),('L',734,725),('L',340,750),('Z',)],colors['shade'],None)
        p.restore();x,y=joints[name];ellipse(p,x-29,y-29,58,58,colors['coat'])
        shoulder = ([('M',406,545),('Q',417,521,448,530),('Q',462,546,451,577),('L',434,602),('Q',409,591,397,575),('Z',)]
            if name=='Arm_Front' else [('M',624,530),('Q',654,529,668,552),('L',688,588),('Q',672,607,649,606),('L',629,575),('Z',)])
        draw(p,shoulder,colors['coat'],OUTLINE,5)
        shoulder_trim=([('M',400,573),('Q',417,590,437,594),('L',451,575)] if name=='Arm_Front'
            else [('M',647,597),('Q',671,600,682,586)])
        draw(p,shoulder_trim,None,colors['trim'],6)
        cuff=[('M',365,682),('Q',387,699,415,694)] if name=='Arm_Front' else [('M',679,682),('Q',697,700,718,694)]
        draw(p,cuff,None,colors['trim'],8)
        draw(p,[('M',x-6,y+39),('Q',x+8,y+61,x-3,y+85)],None,colors['light'],4)
        p.end();layer(outfit+'__Sleeve_'+('Back' if name=='Arm_Back' else 'Front'),img,clothes)
    img=image();p=painter(img)
    torso=[('M',458,512),('Q',509,480,572,506),('L',637,541),('L',623,686),('Q',555,728,465,679),('L',445,558),('Q',443,530,458,512),('Z',)]
    draw(p,torso,colors['coat'])
    draw(p,[('M',490,504),('L',553,496),('L',590,524),('L',615,661),('L',490,675),('L',483,549),('Z',)],colors['inner'],None)
    draw(p,[('M',496,505),('L',537,559),('L',590,515)],None,colors['trim'],11)
    draw(p,[('M',584,521),('Q',575,559,539,591),('L',505,644)],None,colors['coat'],14)
    draw(p,[('M',450,540),('Q',449,576,461,625),('L',478,633),('Q',463,581,466,547),('Z',)],colors['shade'],None)
    draw(p,[('M',616,544),('L',635,548),('L',625,645),('L',604,650),('Q',602,597,616,544),('Z',)],colors['shade'],None)
    draw(p,[('M',507,611),('Q',545,620,594,595),('L',605,643),('Q',548,660,492,641),('Z',)],colors['innerShade'],None)
    # Jade sash belongs to the torso, not the legs; both silhouettes are identical.
    draw(p,[('M',460,640),('Q',540,670,624,640),('L',627,676),('Q',543,704,465,676),('Z',)],colors['belt'])
    draw(p,[('M',470,652),('Q',540,679,613,652)],None,'#6bb5a7',5)
    ellipse(p,574,650,36,35,'#65b6a7',OUTLINE,5)
    ellipse(p,584,660,14,14,colors['belt'])
    p.end();layer(outfit+'__Body_Torso',img,clothes)
    flaps={
        'Coat_L':[('M',466,674),('Q',492,683,524,685),('L',495,755),('L',454,795),('Q',426,778,414,756),('Z',)],
        'Coat_R':[('M',587,683),('L',622,675),('L',675,748),('Q',656,782,623,792),('L',600,744),('Z',)],
    }
    for name,spec in flaps.items():
        img=image();p=painter(img);draw(p,spec,colors['coat'])
        p.save();p.setClipPath(shape(spec))
        draw(p,[('M',430,742),('Q',570,770,674,715),('L',700,820),('L',400,820),('Z',)],colors['shade'],None)
        p.restore()
        edge=[('M',424,754),('Q',435,774,454,785),('L',490,750)] if name=='Coat_L' else [('M',664,750),('Q',649,775,626,782)]
        draw(p,edge,None,colors['trim'],7)
        p.end();layer(outfit+'__'+name,img,clothes)

# Compress the entire body and both outfits with one shared transform. This
# corrects chibi proportions without changing the registration of any garment.
body_mapping=QTransform(1,0,0,.75,0,132)
for container in [body] + outfits:
    for node in container.childNodes():
        refined=image();p=painter(refined);p.setTransform(body_mapping);p.drawImage(0,0,images[node.name()]);p.end()
        bits=refined.constBits();bits.setsize(refined.sizeInBytes())
        node.setPixelData(QByteArray(bytes(bits)),0,0,SIZE,SIZE)
        images[node.name()]=refined
for name in joints:
    if name not in ('Hair','Eyes'):
        point=body_mapping.map(QPointF(*joints[name]));joints[name]=[point.x(),point.y()]
(SOURCE / 'joints.json').write_text(json.dumps({'canvas': SIZE, 'foot': joints['Root'], 'joints': joints}, indent=2) + '\n')

# Preserve the approved three-quarter head using one global mapping. Hair and
# face masks share that mapping; the chin and side locks receive independent layers.
original=QImage(str(ROOT/'Assets/_Project/Art/CartoonV2/hero.png'))
assert not original.isNull()
mapping=QTransform(.8,0,0,.8,16,-2)
whole_head=mapping.map(shape([('M',325,80),('L',945,80),('L',945,575),('L',821,590),('Q',801,637,743,659),('Q',674,677,604,651),('L',584,676),('L',565,648),('L',543,625),('Q',496,637,476,608),('L',474,580),('L',400,612),('L',325,570),('Z',)]))
mapped=image();p=painter(mapped);p.setClipPath(whole_head)
p.setTransform(mapping);p.drawImage(0,0,original);p.end()
head_shape=mapping.map(shape([('M',490,555),('Q',557,542,609,554),('L',712,479),('Q',756,451,804,480),('L',818,550),('L',805,608),('Q',782,644,727,657),('Q',657,676,602,650),('Q',557,630,548,620),('Q',491,632,490,592),('Z',)]))
head=image();p=painter(head);p.setClipPath(head_shape);p.drawImage(0,0,mapped);p.end()
hair=image();p=painter(hair);p.drawImage(0,0,mapped)
p.setCompositionMode(QPainter.CompositionMode_DestinationOut);p.fillPath(head_shape,QColor('#ffffff'));p.end()
features=mapping.map(shape([('M',584,574),('Q',640,560,695,587),('L',695,633),('L',589,640),('Z',)]))
features.addPath(mapping.map(shape([('M',745,548),('L',802,532),('L',802,605),('L',746,614),('Z',)])))
features.addPath(mapping.map(shape([('M',696,627),('L',744,627),('L',744,652),('L',696,652),('Z',)])))
eyes=image();p=painter(eyes);p.setClipPath(features);p.drawImage(0,0,mapped);p.end()
p=painter(head);p.setClipPath(features);p.fillPath(head_shape,QColor('#f3c9a5'));p.end()
identity=group('Identity')
layer('Head',head,identity);layer('Eyes_Mouth',eyes,identity);layer('Hair_Front',hair,identity)

# Retain before/side/back reference artwork outside the PSD runtime source.
references=group('References',False)
ref=image();p=painter(ref);p.drawImage(QRectF(0,0,SIZE,SIZE),original);p.end()
layer('Reference_approved_hero',ref,references,False)
doc.refreshProjection();doc.waitForDone()
assert doc.saveAs(str(SOURCE/'hero-three-quarter.kra'))

# PSD export contains only production layers. The Krita master retains the hidden
# reference and both grouped outfits for the artist to edit.
references.remove()
assert doc.exportImage(str(ART/'hero-three-quarter.psd'),InfoObject())
assert doc.exportImage(str(SOURCE/'assembled-ivory.png'),InfoObject())
outfits[0].setVisible(False);outfits[1].setVisible(True)
doc.refreshProjection();doc.waitForDone()
assert doc.exportImage(str(SOURCE/'assembled-navy.png'),InfoObject())
outfits[0].setVisible(True);outfits[1].setVisible(False)
doc.refreshProjection();doc.waitForDone()
app.activeWindow().addView(doc)
print('Authored',len(images)-1,'production layers; shared canvas 1024, two outfits; KRA and PSD saved.')
