"""Run in Krita Scripter. Import authored SVG groups as editable vector layers."""
from pathlib import Path
import json, xml.etree.ElementTree as ET
from krita import Krita, InfoObject

ET.register_namespace('', 'http://www.w3.org/2000/svg')

ROOT=Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/CharacterProduction/experiments/C')
target=ROOT/'C-vector.kra'
assert not target.exists(),'Keep the hand-edited source; save a new version instead of regenerating it.'
source=ET.fromstring((ROOT/'C-editable.svg').read_text())
ns='{http://www.w3.org/2000/svg}'
app=Krita.instance()
doc=app.createDocument(1024,1024,'Ngự Thú — C editable vector','RGBA','U8','sRGB-elle-V2-srgbtrc.icc',72)
doc.setBatchmode(True)
for n in doc.rootNode().childNodes():n.remove()
report={'source':'C-editable.svg','method':'one authored SVG group per Krita vector layer','canvas':[1024,1024],'layers':{},'tests':{}}
for group in source.findall(ns+'g'):
    name=group.attrib['id']
    layer=doc.createVectorLayer(name);doc.rootNode().addChildNode(layer,None)
    content=''.join(ET.tostring(child,encoding='unicode') for child in group)
    svg='<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024" stroke-linecap="round" stroke-linejoin="round">'+content+'</svg>'
    shapes=layer.addShapesFromSvg(svg)
    assert shapes,(name,'no imported shapes')
    report['layers'][name]={'type':layer.type(),'top_level_shapes':len(shapes)}
doc.refreshProjection();doc.waitForDone()
assert doc.saveAs(str(target))
assert doc.exportImage(str(ROOT/'C-krita.png'),InfoObject())
app.activeWindow().addView(doc)
back=app.openDocument(str(target));back.setBatchmode(True)
back.refreshProjection();back.waitForDone()
before=bytes(back.rootNode().projectionPixelData(0,0,1024,1024))
report['reopened_kra_exact_projection']=before==bytes(doc.rootNode().projectionPixelData(0,0,1024,1024))
assert back.exportImage(str(ROOT/'C-krita-reopened.png'),InfoObject())
for label,names in [('without-eyes',['Eyes']),('without-hair',['Hair_Back','Hair_Front','Hair_Accessory'])]:
    nodes=[back.nodeByName(name) for name in names]
    for n in nodes:n.setVisible(False)
    back.refreshProjection();back.waitForDone()
    after=bytes(back.rootNode().projectionPixelData(0,0,1024,1024))
    assert after!=before,label
    assert back.exportImage(str(ROOT/(label+'.png')),InfoObject())
    report['tests'][label]={'changed':True,'vector_nodes':names}
    for n in nodes:n.setVisible(True)
back.refreshProjection();back.waitForDone()
assert before==bytes(back.rootNode().projectionPixelData(0,0,1024,1024))
report['restored_after_visibility_checks']=True
back.close()
(ROOT/'krita-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('Imported 17 vector layers, reopened KRA, checked separate eyes/hair and exact source restoration.')
