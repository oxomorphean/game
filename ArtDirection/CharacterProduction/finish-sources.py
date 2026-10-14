"""Run once inside Krita Scripter after the native B drawing session.

B operations only organize/export existing GUI-authored pixels. A uses explicit
pixel partition masks plus a provisional hidden-head repair; it is not hand drawn.
The paint standard sheet and guides are diagrams, not character artwork.
"""
from pathlib import Path
import json, datetime, hashlib
from krita import Krita, InfoObject
from PyQt5.QtCore import QByteArray, Qt, QRectF, QPointF
from PyQt5.QtGui import QImage, QPainter, QColor, QPen, QFont, QPainterPath, QPolygonF

ROOT = Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/CharacterProduction')
APP = Krita.instance()
S = 1024

def blank(w=S,h=S):
    im=QImage(w,h,QImage.Format_ARGB32); im.fill(0); return im

def raw(im):
    im=im.convertToFormat(QImage.Format_ARGB32)
    bits=im.constBits(); bits.setsize(im.sizeInBytes()); return bytes(bits)

def from_raw(data):
    return QImage(bytes(data),S,S,S*4,QImage.Format_ARGB32).copy()

def group(d,parent,name,visible=True):
    n=d.createNode(name,'grouplayer'); parent.addChildNode(n,None); n.setVisible(visible); return n

def paint(d,parent,name,img):
    n=d.createNode(name,'paintlayer'); parent.addChildNode(n,None)
    assert n.setPixelData(QByteArray(raw(img)),0,0,img.width(),img.height())
    return n

def refresh(d):
    d.refreshProjection(); d.waitForDone()

def export(d,path):
    refresh(d); info=InfoObject(); info.setProperty('alpha',True)
    assert d.exportImage(str(path),info), str(path)

def open_doc(path):
    d=next((d for d in APP.documents() if d.fileName()==str(path)),None)
    if d is None: d=APP.openDocument(str(path)); assert d
    d.setBatchmode(True); return d

def poly(p,points,color):
    p.setPen(Qt.NoPen); p.setBrush(QColor(color))
    p.drawPolygon(QPolygonF([QPointF(x,y) for x,y in points]))

# B: keep the drawing work layers as a hidden recovery copy; only clone pixels.
b=open_doc(ROOT/'experiments/B/B.kra')
assert b.nodeByName('Drawn_Character') is None, 'Already organized; do not rerun over edits'
work=b.nodeByName('Character'); work.setName('Work_Layers_Archive'); work.setVisible(False)
b.nodeByName('References').setVisible(False)
char=group(b,b.rootNode(),'Drawn_Character')
mapping={
 'Hair_Back':[(19,'Ribbon'),(13,'Bun_Paint'),(25,'Bun_Highlight')],
 'Boot_Back':[(0,'Paint')], 'Boot_Front':[(1,'Paint')],
 'Leg_Back':[(2,'Paint')], 'Leg_Front':[(3,'Paint')],
 'Arm_Back':[(11,'Hand_Paint')], 'Torso':[(4,'Paint'),(9,'Lapel')],
 'Sleeve_Back':[(5,'Paint')], 'Coat':[(6,'Front_Paint'),(7,'Back_Paint'),(17,'Belt')],
 'Arm_Front':[(10,'Hand_Paint')], 'Sleeve_Front':[(8,'Paint')],
 'Cloth_Details':[(24,'Shadows'),(26,'Gold_Trim')],
 'Head':[(12,'Paint')], 'Eyes':[(15,'Outline_White'),(16,'Iris'),(21,'Pupil_Highlight')],
 'Mouth':[(20,'Ink')], 'Hair_Front':[(14,'Paint'),(22,'Inner_Ink'),(23,'Shadows')],
 'Hair_Accessory':[(18,'Band')]
}
sources={i:b.nodeByName('Paint Layer '+str(i)) for i in range(1,27)}
sources[0]=next(n for n in b.nodeByName('Leg_Back').childNodes() if n.name()=='Flats')
for slot,items in mapping.items():
    g=group(b,char,slot)
    for i,name in items:
        assert sources[i], (i,name)
        n=sources[i].clone(); n.setName(name); g.addChildNode(n,None); n.setVisible(True)
refresh(b); assert b.save()
export(b,ROOT/'experiments/B/B.png')

# Candidate anchors measured on the shared A/B design canvas; not rigged yet.
anchors={'root':[540,900], 'crown':[486,100], 'neck':[568,539],
 'head_center':[555,431], 'eye_front':[534,491], 'eye_back':[633,466],
 'shoulder_front':[463,566], 'elbow_front':[430,653], 'wrist_front':[421,707],
 'shoulder_back':[596,566], 'elbow_back':[628,647], 'wrist_back':[679,692],
 'hip':[543,703], 'knee_front':[496,782], 'ankle_front':[467,834],
 'knee_back':[591,773], 'ankle_back':[601,814],
 'foot_front':[455,892], 'foot_back':[626,862], 'weapon_grip_front':[420,729],
 'hat':[538,266], 'back_prop':[539,588]}
meta={'body_id':'NGUTHU_CHIBI_34_V1_CANDIDATE','view_id':'three_quarter_right',
      'canvas':[S,S],'coordinates':'x right, y down, pixels; shared full canvas',
      'status':'candidate measured on A; adjust after master approval; not rig pivots yet',
      'anchors':anchors}
(ROOT/'anchors.json').write_text(json.dumps(meta,indent=2)+'\n')

guide=blank();p=QPainter(guide);p.setRenderHint(QPainter.Antialiasing)
p.setPen(QPen(QColor('#2C8FA2'),2,Qt.DashLine))
p.drawLine(540,100,540,900);p.drawLine(230,900,800,900);p.drawLine(230,100,800,100)
p.setFont(QFont('Sans Serif',11))
for name,(x,y) in anchors.items():
    p.setPen(QPen(QColor('#D26A35'),2));p.drawEllipse(QPointF(x,y),5,5)
    p.drawLine(x-9,y,x+9,y);p.drawLine(x,y-9,x,y+9)
    p.setPen(QColor('#236775'));p.drawText(x+11,y-5,name)
p.end(); guide.save(str(ROOT/'anchor-guide.png'))
template=open_doc(ROOT/'character-template.kra')
for n in template.nodeByName('Guides').childNodes(): n.remove()
paint(template,template.nodeByName('Guides'),'Candidate anchors - see anchors.json',guide)
ref=paint(template,template.nodeByName('References'),'A candidate proportions - not approved',QImage(str(ROOT/'experiments/A/normalized-reference.png')))
ref.setLocked(True)
template.nodeByName('References').setVisible(False)
refresh(template); assert template.save()

# A: one uniform canvas, no rescaling/recentering per part.
a=open_doc(ROOT/'experiments/A/A.kra')
assert a.nodeByName('Character') is None, 'Already segmented; do not rerun over edits'
src=QImage(str(ROOT/'experiments/A/normalized-reference.png')).convertToFormat(QImage.Format_ARGB32)
original=raw(src)
labels=blank(); labels.fill(QColor(0,0,0,255)); p=QPainter(labels)
names=['Torso','Hair_Back','Hair_Front','Head','Eyes','Mouth','Sleeve_Back','Arm_Back',
       'Leg_Back','Boot_Back','Leg_Front','Boot_Front','Coat','Sleeve_Front','Arm_Front','Belt','Hair_Accessory']
def region(name,points):
    i=names.index(name); poly(p,points,QColor(i,0,0,255))
region('Hair_Front',[(250,70),(800,70),(800,556),(640,556),(600,539),(500,536),(380,552),(250,430)])
region('Hair_Back',[(250,70),(587,70),(590,230),(547,244),(468,263),(414,301),(370,300),(340,403),(250,403)])
region('Head',[(448,491),(482,476),(503,471),(537,467),(568,479),(583,483),(590,453),(603,412),(613,383),(627,378),(640,392),(656,420),(662,446),(659,479),(654,501),(637,518),(598,535),(556,538),(515,530),(485,514),(474,507),(452,511),(431,508),(416,499),(409,486),(414,471),(426,462),(444,461),(440,476),(451,471),(434,492)])
region('Eyes',[(486,471),(502,460),(533,460),(562,474),(571,484),(560,507),(532,514),(504,505),(491,492)])
region('Eyes',[(609,450),(627,430),(651,416),(663,435),(656,470),(646,489),(628,495),(614,481)])
region('Mouth',[(572,511),(601,506),(608,514),(599,524),(577,525)])
region('Sleeve_Back',[(590,537),(631,540),(655,598),(660,619),(690,634),(716,669),(688,693),(655,688),(620,654),(610,613)])
region('Arm_Back',[(681,688),(704,676),(719,697),(714,730),(689,737),(679,718)])
region('Leg_Back',[(542,682),(644,706),(663,798),(629,824),(574,822),(556,785),(534,742)])
region('Boot_Back',[(571,813),(633,813),(672,841),(690,857),(674,877),(564,877),(558,848)])
region('Leg_Front',[(480,674),(556,673),(570,746),(537,815),(511,836),(441,835),(423,799),(444,742)])
region('Boot_Front',[(439,829),(497,828),(505,884),(494,910),(414,910),(408,883)])
region('Coat',[(486,665),(557,674),(530,718),(510,740),(478,781),(453,769),(422,735),(447,701)])
region('Coat',[(602,669),(637,682),(667,716),(689,737),(633,779),(619,752),(609,718)])
region('Sleeve_Front',[(445,538),(476,547),(487,584),(474,614),(449,660),(470,685),(453,704),(424,711),(390,727),(363,727),(336,714),(318,675),(352,642),(391,614),(400,589),(410,553)])
region('Arm_Front',[(392,681),(437,681),(457,714),(453,743),(438,753),(412,762),(387,743),(381,717)])
region('Belt',[(488,640),(573,643),(616,638),(637,652),(628,677),(595,681),(544,679),(485,672)])
region('Hair_Accessory',[(363,263),(416,248),(444,217),(482,189),(504,174),(528,182),(547,202),(542,225),(508,240),(474,240),(435,263),(396,281),(370,287)])
p.end()
labelbytes=raw(labels)
parts=[bytearray(S*S*4) for n in names]
counts={n:0 for n in names}
for j in range(0,len(original),4):
    if original[j+3]:
        k=labelbytes[j+2]; parts[k][j:j+4]=original[j:j+4]; counts[names[k]]+=1

char=group(a,a.rootNode(),'Character')
# The face repair is deliberately isolated and labelled provisional.
g=group(a,char,'Head_Underpaint_CANDIDATE')
repair=blank();p=QPainter(repair);p.setRenderHint(QPainter.Antialiasing)
p.setPen(QPen(QColor('#20283B'),5));p.setBrush(QColor('#F3C5A2'))
p.drawEllipse(QRectF(435,319,233,214));p.end()
paint(a,g,'Scalp repair - scripted, needs artist review',repair)
order=['Hair_Back','Boot_Back','Boot_Front','Leg_Back','Leg_Front','Arm_Back','Torso','Sleeve_Back','Coat','Sleeve_Front','Arm_Front','Belt','Head','Eyes','Mouth','Hair_Front','Hair_Accessory']
for name in order:
    g=group(a,char,name)
    if name=='Head':
        patch=blank();p=QPainter(patch)
        p.setPen(Qt.NoPen);p.setBrush(QColor('#FFD6B2'))
        p.drawPolygon(QPolygonF([QPointF(x,y) for x,y in [(480,455),(574,453),(576,514),(499,522),(477,490)]]))
        p.drawPolygon(QPolygonF([QPointF(x,y) for x,y in [(608,448),(634,413),(666,409),(666,491),(636,506),(612,489)]]))
        p.drawRect(570,505,42,22);p.end()
        # Restrict repair to exactly the removed eye/mouth pixels.
        rb=bytearray(raw(patch))
        for j in range(0,len(rb),4):
            if labelbytes[j+2] not in (4,5): rb[j:j+4]=b'\0\0\0\0'
        paint(a,g,'Face beneath eyes and mouth - provisional paint',from_raw(rb))
    paint(a,g,'Paint_Source',from_raw(parts[names.index(name)]))
a.nodeByName('Paint_Source - to segment').setName('Generated composite archive');a.nodeByName('Generated composite archive').setVisible(False)
a.nodeByName('References').setVisible(False);a.nodeByName('Guides').setVisible(False)
paint(a,a.nodeByName('Guides'),'Candidate anchors',guide)
refresh(a); assert a.save()
export(a,ROOT/'experiments/A/A.png')
(ROOT/'experiments/A/source-method.json').write_text(json.dumps({
 'method':'ImageGen raster partitioned using scripted masks through Krita API',
 'paint_source':'Ink, flats, shading remain fused within each AI raster part',
 'repairs':'Scripted scalp and face-under-eyes on explicitly labelled layers; provisional',
 'nonempty_source_pixels':counts,
 'limits':['Static source trial only','No complete hidden torso/arm anatomy','No joint overlaps or rig yet','Mask edges require artist cleanup before motion','Not approved production art']},indent=2)+'\n')
(ROOT/'experiments/B/source-method.json').write_text(json.dumps({
 'method':'Native Krita Bezier, brush, color picker and fill; controlled through desktop UI',
 'source_design':'A normalized reference used as underlay, hidden in export',
 'scripts':'Document/layer setup, palette, organizing GUI-authored pixels and exporting only; no B path synthesis',
 'brush':'b) Basic-5 Size Opacity; outer 8 px, inner 4 px, gold trim 7 px; opacity 100%',
 'limits':['Ink and fill fused in most Paint layers','Cloth_Details spans several slots','No complete hidden torso/arms','No rig or animation proof','Visual quality below A']},indent=2)+'\n')
print('A partitioned, B organized, anchors/template saved. Both are static source candidates.')
