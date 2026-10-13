"""Add labelled construction guides and the missing blank equipment slots."""
from pathlib import Path
import json
from krita import Krita
from PyQt5.QtCore import Qt, QByteArray, QPointF
from PyQt5.QtGui import QImage,QPainter,QPen,QColor,QFont

root=Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/CharacterProduction')
a=Krita.instance();path=root/'character-template.kra'
d=next((d for d in a.documents() if d.fileName()==str(path)),None) or a.openDocument(str(path))
d.setBatchmode(True)
char=d.nodeByName('Character')
for slot in ['Boot_Back','Boot_Front','Hair_Accessory']:
    if d.nodeByName(slot) is None:
        g=d.createNode(slot,'grouplayer');char.addChildNode(g,None)
        for name in ['Flats','Shadow','Highlight','Ink','Corrections']:
            n=d.createNode(name,'paintlayer');g.addChildNode(n,None)
# Stable back-to-front order; only blank nodes are reordered here.
order=['Back_Prop','Hair_Back','Boot_Back','Boot_Front','Leg_Back','Leg_Front','Arm_Back',
       'Torso','Coat','Sleeve_Back','Head','Eyes','Mouth','Hair_Front','Hair_Accessory',
       'Hat','Weapon','Arm_Front','Sleeve_Front']
for name in order:
    n=d.nodeByName(name)
    # Removing a Krita group detaches its children; keep a clone before removal.
    replacement=n.clone();n.remove();char.addChildNode(replacement,None)
    existing={child.name() for child in replacement.childNodes()}
    for layer_name in ['Flats','Shadow','Highlight','Ink','Corrections']:
        if layer_name not in existing:
            replacement.addChildNode(d.createNode(layer_name,'paintlayer'),None)

points=json.loads((root/'anchors.json').read_text())['anchors']
guide=QImage(1024,1024,QImage.Format_ARGB32);guide.fill(0)
p=QPainter(guide);p.setRenderHint(QPainter.Antialiasing)
p.setPen(QPen(QColor('#3195A8'),2,Qt.DashLine));p.drawLine(540,100,540,900);p.drawLine(260,100,770,100);p.drawLine(260,900,770,900)
p.setFont(QFont('Arial',12));p.setPen(QColor('#236775'));p.drawText(27,42,'NGUTHU / SHARED CANVAS 1024');p.drawText(27,65,'CANDIDATE ANCHORS — NOT RIGGED')
for i,(name,(x,y)) in enumerate(points.items(),1):
    p.setPen(QPen(QColor('#C56F3B'),2));p.drawEllipse(QPointF(x,y),5,5)
    p.drawLine(x-8,y,x+8,y);p.drawLine(x,y-8,x,y+8)
    p.setFont(QFont('Arial',10));p.setPen(QColor('#236775'));p.drawText(x+10,y-7,str(i))
    col=0 if i<=11 else 1;row=(i-1)%11;xx=25 if col==0 else 793
    p.drawText(xx,140+row*54,f'{i:02d}  {name}')
    p.setPen(QColor('#66818A'));p.drawText(xx+22,158+row*54,f'({x}, {y})')
p.setFont(QFont('Arial',12));p.setPen(QColor('#236775'));p.drawText(280,950,'Crown 100 · ground 900 · full-canvas export')
p.end();guide.save(str(root/'anchor-guide.png'))
guides=d.nodeByName('Guides')
for n in guides.childNodes():n.remove()
n=d.createNode('Candidate anchor guide','paintlayer');guides.addChildNode(n,None)
bits=guide.constBits();bits.setsize(guide.sizeInBytes());assert n.setPixelData(QByteArray(bytes(bits)),0,0,1024,1024)
n.setLocked(True);d.nodeByName('References').setVisible(False);guides.setVisible(True)
d.refreshProjection();d.waitForDone();assert d.save()
print('Template: 19 blank slot groups; locked, numbered construction guide saved')
