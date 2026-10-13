"""Run inside Krita Scripter. Creates guides, blank drawing layers and reference images.

This script does not draw the B character. Its visible strokes are authored with
Krita tools. A's original image is normalized with one transform, preserving alpha.
"""
from pathlib import Path
import json
from krita import Krita, InfoObject
from PyQt5.QtCore import QByteArray, Qt, QPointF
from PyQt5.QtGui import QImage, QPainter, QColor, QPen, QFont

ROOT = Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/CharacterProduction')
for target in ['character-template.kra','experiments/A/A.kra','experiments/B/B.kra']:
 assert not (ROOT/target).exists(), 'Source exists; create a new version instead of overwriting: '+target
app = Krita.instance()
S = 1024
slots = ['Back_Prop', 'Hair_Back', 'Arm_Back', 'Leg_Back', 'Leg_Front', 'Torso',
         'Coat', 'Sleeve_Back', 'Head', 'Eyes', 'Mouth', 'Hair_Front',
         'Hat', 'Weapon', 'Arm_Front', 'Sleeve_Front']
palette = {
 'Outline':'20283B','Hair':'EDF0EF','Hair shadow':'ABB9CD','Hair light':'FFFBEF',
 'Skin':'F3C5A2','Skin shadow':'CD927D','Skin light':'FFE2BE',
 'Ivory':'F2E8CD','Ivory shadow':'B4B8AA','Ivory light':'FFF5D8',
 'Navy':'334561','Navy shadow':'202D46','Navy light':'566B80',
 'Jade':'438F85','Jade shadow':'255960','Jade light':'9BD3B7',
 'Gold':'C9A365','Gold shadow':'866747','Gold light':'EDD5A0'}
with (ROOT/'character-v1.gpl').open('w') as f:
 f.write('GIMP Palette\nName: Nguthu Character v1 candidate\nColumns: 3\n#\n')
 for name,h in palette.items():
  c=QColor('#'+h); f.write(f'{c.red():3} {c.green():3} {c.blue():3}\t{name}\n')

def fresh(name):
 d=app.createDocument(S,S,name,'RGBA','U8','',72)
 d.setBatchmode(True)
 for n in d.rootNode().childNodes(): n.remove()
 return d

def group(d,parent,name,visible=True):
 n=d.createNode(name,'grouplayer'); parent.addChildNode(n,None); n.setVisible(visible); return n

def paint(d,parent,name,img=None,visible=True):
 n=d.createNode(name,'paintlayer'); parent.addChildNode(n,None); n.setVisible(visible)
 if img is not None:
  im=img.convertToFormat(QImage.Format_ARGB32); bits=im.constBits(); bits.setsize(im.sizeInBytes())
  assert n.setPixelData(QByteArray(bytes(bits)),0,0,S,S)
 return n

def blank():
 im=QImage(S,S,QImage.Format_ARGB32); im.fill(0); return im

def save(d,path):
 d.refreshProjection(); d.waitForDone(); assert d.saveAs(str(path))

original=QImage(str(ROOT/'experiments/A/generated-original.png')).convertToFormat(QImage.Format_ARGB32)
raw=original.constBits(); raw.setsize(original.sizeInBytes()); buf=bytes(raw)
xs=[];ys=[]
for y in range(original.height()):
 for x in range(original.width()):
  if buf[y*original.bytesPerLine()+x*4+3]>16: xs.append(x);ys.append(y)
bounds=[min(xs),min(ys),max(xs)+1,max(ys)+1]
scale=800/(bounds[3]-bounds[1]); tx=512-(bounds[0]+bounds[2])*.5*scale; ty=100-bounds[1]*scale
normalized=blank(); p=QPainter(normalized);p.setRenderHint(QPainter.SmoothPixmapTransform)
p.translate(tx,ty);p.scale(scale,scale);p.drawImage(0,0,original);p.end()
assert normalized.save(str(ROOT/'experiments/A/normalized-reference.png'))
(ROOT/'experiments/A/generation.json').write_text(json.dumps({'tool':'built-in image_gen','calls':1,'originalSize':[original.width(),original.height()],'sourceBounds':bounds,'uniformTransform':{'scale':scale,'x':tx,'y':ty},'targetHeight':800,'prompt':'prompt.txt'},indent=2)+'\n')

guides=blank();p=QPainter(guides);p.setRenderHint(QPainter.Antialiasing)
p.setPen(QPen(QColor('#4797a9'),2,Qt.DashLine));p.drawLine(512,70,512,930);p.drawLine(210,900,814,900);p.drawLine(210,100,814,100)
p.setFont(QFont('Sans Serif',13));p.setPen(QColor('#347084'));p.drawText(220,88,'CROWN 100');p.drawText(220,925,'GROUND 900 / shared canvas 1024')
p.end()

template=fresh('Nguthu character template v1 candidate')
char=group(template,template.rootNode(),'Character')
for slot in slots:
 g=group(template,char,slot)
 for layer in ['Flats','Shadow','Highlight','Ink','Corrections']:paint(template,g,layer)
group(template,template.rootNode(),'References',False)
guide=group(template,template.rootNode(),'Guides');paint(template,guide,'Canvas guides',guides)
save(template,ROOT/'character-template.kra')

a=fresh('A - ImageGen edited in Krita - candidate')
paint(a,a.rootNode(),'Paint_Source - to segment',normalized)
refs=group(a,a.rootNode(),'References',False);paint(a,refs,'Generated original normalized',normalized)
guide=group(a,a.rootNode(),'Guides',False);paint(a,guide,'Canvas guides',guides)
save(a,ROOT/'experiments/A/A.kra')

b=fresh('B - drawn with Krita tools - candidate')
refs=group(b,b.rootNode(),'References');paint(b,refs,'Common design underlay - hide for export',normalized)
char=group(b,b.rootNode(),'Character')
for slot in slots:
 g=group(b,char,slot)
 for layer in ['Flats','Shadow','Highlight','Ink','Corrections']:paint(b,g,layer)
guide=group(b,b.rootNode(),'Guides',False);paint(b,guide,'Canvas guides',guides)
save(b,ROOT/'experiments/B/B.kra')
app.activeWindow().addView(b)
b.setActiveNode(b.nodeByName('Leg_Back').childNodes()[0])
app.activeWindow().activeView().canvas().setZoomLevel(.6)
print('Created template and A/B sources; normalized transform',scale,tx,ty)
