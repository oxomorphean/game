"""Run in Krita Scripter to create and verify the C2 vector source and exports."""
from pathlib import Path
import json, hashlib
from krita import Krita, InfoObject

ROOT=Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/CharacterProduction/experiments/C2')
target=ROOT/'C2-vector.kra'
assert not target.exists(), 'Preserve edited KRA; explicitly choose a new version.'
manifest=json.loads((ROOT/'manifest.json').read_text())
app=Krita.instance()
doc=app.createDocument(1024,1024,'C2 — corps, vêtements et cheveux','RGBA','U8','sRGB-elle-V2-srgbtrc.icc',72)
doc.setBatchmode(True)
for node in doc.rootNode().childNodes(): node.remove()
nodes={}
report={'canvas':[1024,1024],'layers':{},'variants':{},'method':'authored Bezier parts imported into native Krita vector layers'}
(ROOT/'export').mkdir(exist_ok=True)
for layer in manifest['layers']:
    node=doc.createVectorLayer(layer['name'])
    doc.rootNode().addChildNode(node,None)
    shapes=node.addShapesFromSvg((ROOT/layer['source']).read_text())
    assert shapes,layer['name']
    nodes[layer['name']]=node
    report['layers'][layer['name']]={'type':node.type(),'shapes':len(shapes)}
    node.setVisible(False)

def visible(layer,outfit,hair,eyes='jade'):
    c=layer['category']
    if c=='outfit': return layer['variant']==outfit
    if c=='hair': return layer['variant']==hair
    if c=='eyes': return layer['variant']==eyes
    return outfit not in layer['hidden_by']

def select(outfit,hair,eyes='jade'):
    for layer in manifest['layers']:
        nodes[layer['name']].setVisible(visible(layer,outfit,hair,eyes))
    doc.refreshProjection();doc.waitForDone()

# Export each part alone at the full canvas size. No independent crop or scale.
for layer in manifest['layers']:
    node=nodes[layer['name']];node.setVisible(True)
    doc.refreshProjection();doc.waitForDone()
    assert doc.exportImage(str(ROOT/'export'/(layer['name']+'.png')),InfoObject())
    node.setVisible(False)

for outfit in ['classic','jade','basic']:
    for hair in ['silver','ink']:
        select(outfit,hair)
        name=outfit+'-'+hair
        assert doc.exportImage(str(ROOT/(name+'-krita.png')),InfoObject())
        report['variants'][name]=hashlib.sha256(bytes(doc.rootNode().projectionPixelData(0,0,1024,1024))).hexdigest()
select('basic','silver')
for layer in manifest['layers']:
    if layer['category']=='hair': nodes[layer['name']].setVisible(False)
doc.refreshProjection();doc.waitForDone()
assert doc.exportImage(str(ROOT/'body-basic-krita.png'),InfoObject())
select('classic','silver','amber')
assert doc.exportImage(str(ROOT/'eyes-amber-krita.png'),InfoObject())
select('classic','silver')
assert doc.saveAs(str(target))
app.activeWindow().addView(doc)
reopened=app.openDocument(str(target));reopened.setBatchmode(True)
reopened.refreshProjection();reopened.waitForDone()
assert doc.exportImage(str(ROOT/'C2-krita.png'),InfoObject())
assert reopened.exportImage(str(ROOT/'C2-reopened.png'),InfoObject())
report['reopened_vector_layers']=len([n for n in reopened.rootNode().childNodes() if n.type()=='vectorlayer'])
assert report['reopened_vector_layers']==len(manifest['layers'])
reopened.close()
# PSD handoff is deliberately raster-per-part; the editable vector master stays KRA.
raster=app.createDocument(1024,1024,'C2 — layered raster handoff','RGBA','U8','sRGB-elle-V2-srgbtrc.icc',72)
raster.setBatchmode(True)
for n in raster.rootNode().childNodes():n.remove()
for layer in manifest['layers']:
    src=app.openDocument(str(ROOT/'export'/(layer['name']+'.png')));src.waitForDone()
    n=raster.createNode(layer['name'],'paintlayer');raster.rootNode().addChildNode(n,None)
    n.setPixelData(src.rootNode().projectionPixelData(0,0,1024,1024),0,0,1024,1024)
    n.setVisible(layer['default_visible']);src.close()
raster.refreshProjection();raster.waitForDone()
assert raster.exportImage(str(ROOT/'C2-layered.psd'),InfoObject())
raster.close()
(ROOT/'krita-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('C2: 52 vector layers saved and reopened; 52 full-canvas PNG parts; 6 combinations and layered PSD exported.')
