"""Run in Krita Scripter: verify layered PSD export before authoring the hero."""
from krita import Krita, InfoObject
from PyQt5.QtGui import QImage, QPainter, QColor
from PyQt5.QtCore import QByteArray, QRectF
from pathlib import Path

root = Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo')
app = Krita.instance()
doc = app.createDocument(256, 256, 'Layer import probe', 'RGBA', 'U8', '', 72)
doc.setBatchmode(True)
for initial_node in doc.rootNode().childNodes():
    initial_node.remove()
for name, color, rect in [
    ('Back', '#26364d', QRectF(40, 90, 90, 110)),
    ('Torso', '#f0eadb', QRectF(70, 60, 110, 120)),
    ('Face', '#efba95', QRectF(110, 35, 65, 65)),
]:
    img = QImage(256, 256, QImage.Format_ARGB32)
    img.fill(0)
    painter = QPainter(img)
    painter.fillRect(rect, QColor(color))
    painter.end()
    node = doc.createNode(name, 'paintlayer')
    doc.rootNode().addChildNode(node, None)
    bits = img.constBits()
    bits.setsize(img.sizeInBytes())
    assert node.setPixelData(QByteArray(bytes(bits)), 0, 0, 256, 256)
doc.refreshProjection()
doc.waitForDone()
source = root / 'ArtDirection/HeroProduction2D'
target = root / 'Assets/_Project/Art/HeroProduction2D'
target.mkdir(parents=True, exist_ok=True)
assert doc.saveAs(str(source / 'import-probe.kra'))
assert doc.exportImage(str(target / 'import-probe.psd'), InfoObject())
app.activeWindow().addView(doc)
print('PSD probe exported: three raster layers on one 256px canvas.')
