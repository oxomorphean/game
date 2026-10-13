"""Run in Krita Scripter; test a saved layer edit on a disposable source copy."""
from pathlib import Path
import hashlib, json, shutil, tempfile
from krita import Krita

ROOT=Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/CharacterProduction/experiments/C2')
fixture=Path(tempfile.mkdtemp(prefix='nguthu-c2-handoff-'))
original_hash=hashlib.sha256((ROOT/'C2-vector.kra').read_bytes()).hexdigest()
for name in ['C2-vector.kra','manifest.json']:
    shutil.copy2(ROOT/name,fixture/name)
shutil.copytree(ROOT/'export',fixture/'export')
app=Krita.instance()
doc=app.openDocument(str(fixture/'C2-vector.kra'));doc.setBatchmode(True);doc.waitForDone()
eyes=next(n for n in doc.rootNode().childNodes() if n.name()=='Eyes__jade')
eyes.setOpacity(96)
doc.refreshProjection();doc.waitForDone()
assert doc.saveAs(str(fixture/'C2-vector.kra'))
doc.close()
edited_hash=hashlib.sha256((fixture/'C2-vector.kra').read_bytes()).hexdigest()
script=(ROOT/'finish-krita.py').read_text()
assert script.count(str(ROOT))==1
exec(compile(script.replace(str(ROOT),str(fixture)),'finish-krita-fixture.py','exec'),{})

def alpha_peak(path, isolate=None):
    opened=app.openDocument(str(path));opened.setBatchmode(True);opened.waitForDone()
    if isolate:
        for n in opened.rootNode().childNodes():n.setVisible(n.name()==isolate)
        opened.refreshProjection();opened.waitForDone()
    peak=max(bytes(opened.rootNode().projectionPixelData(0,0,1024,1024))[3::4])
    opened.close()
    return peak

png_peak=alpha_peak(fixture/'export/Eyes__jade.png')
psd_peak=alpha_peak(fixture/'C2-layered.psd','Eyes__jade')
report={'saved_edit_opacity':96,'exported_png_alpha_peak':png_peak,'exported_psd_alpha_peak':psd_peak,
        'master_unchanged':original_hash==hashlib.sha256((ROOT/'C2-vector.kra').read_bytes()).hexdigest(),
        'edited_source_unchanged_by_export':edited_hash==hashlib.sha256((fixture/'C2-vector.kra').read_bytes()).hexdigest(),
        'fixture':str(fixture),'passed':png_peak==96 and psd_peak==96}
(ROOT/'krita-handoff-check.json').write_text(json.dumps(report,indent=2)+'\n')
assert report['passed'] and report['master_unchanged'] and report['edited_source_unchanged_by_export'],report
print('C2 handoff check passed: a saved KRA layer edit reaches PNG and PSD; sources stay unchanged.')
