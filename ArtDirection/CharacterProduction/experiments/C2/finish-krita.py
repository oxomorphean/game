"""Export the saved vector master to current full-canvas PNG parts and layered PSD."""
from pathlib import Path
import json, hashlib
from krita import Krita, InfoObject

ROOT=Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/CharacterProduction/experiments/C2')
manifest=json.loads((ROOT/'manifest.json').read_text())
source_hash=hashlib.sha256((ROOT/'C2-vector.kra').read_bytes()).hexdigest()
app=Krita.instance()
source=app.openDocument(str(ROOT/'C2-vector.kra'))
source.setBatchmode(True);source.waitForDone()
nodes=source.rootNode().childNodes()
assert len(nodes)==52 and all(n.type()=='vectorlayer' for n in nodes)
by_name={n.name():n for n in nodes}
assert set(by_name)=={layer['name'] for layer in manifest['layers']},'KRA layers must match the manifest.'
report={'canvas':[1024,1024],'reopened_vector_layers':len(nodes),
        'source_sha256':source_hash,
        'method':'Authored Bezier paths in native Krita vector layers; raster-per-part PSD handoff',
        'layers':{n.name():{'type':n.type(),'shapes':len(n.shapes())} for n in nodes}}
source.refreshProjection();source.waitForDone()
assert source.exportImage(str(ROOT/'C2-reopened.png'),InfoObject())
# Read the current saved KRA, including artist edits. Old PNGs are never the source.
(ROOT/'export').mkdir(exist_ok=True)
for node in nodes:node.setVisible(False)
for layer in manifest['layers']:
    node=by_name[layer['name']];node.setVisible(True)
    source.refreshProjection();source.waitForDone()
    assert source.exportImage(str(ROOT/'export'/(layer['name']+'.png')),InfoObject())
    node.setVisible(False)

def select(outfit,hair,eyes='jade'):
    for layer in manifest['layers']:
        category=layer['category']
        if category=='outfit':visible=layer['variant']==outfit
        elif category=='hair':visible=layer['variant']==hair
        elif category=='eyes':visible=layer['variant']==eyes
        else:visible=outfit not in layer['hidden_by']
        by_name[layer['name']].setVisible(visible)
    source.refreshProjection();source.waitForDone()

for outfit in ['classic','jade','basic']:
    for hair in ['silver','ink']:
        select(outfit,hair)
        assert source.exportImage(str(ROOT/(outfit+'-'+hair+'-krita.png')),InfoObject())
select('basic','silver')
for layer in manifest['layers']:
    if layer['category']=='hair':by_name[layer['name']].setVisible(False)
source.refreshProjection();source.waitForDone()
assert source.exportImage(str(ROOT/'body-basic-krita.png'),InfoObject())
select('classic','silver','amber')
assert source.exportImage(str(ROOT/'eyes-amber-krita.png'),InfoObject())
source.close()
assert source_hash==hashlib.sha256((ROOT/'C2-vector.kra').read_bytes()).hexdigest()
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
check=app.openDocument(str(ROOT/'C2-layered.psd'));check.setBatchmode(True);check.waitForDone()
report['reopened_psd_layers']=len(check.rootNode().childNodes())
assert report['reopened_psd_layers']==52
check.refreshProjection();check.waitForDone()
assert check.exportImage(str(ROOT/'C2-psd-reopened.png'),InfoObject())
check.close()
report['exports']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ROOT/'export').glob('*.png'))}
(ROOT/'krita-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('C2 exported from saved KRA: 52 current PNG parts; layered PSD reopened with 52 parts. KRA unchanged.')
