"""Inspect exported assets without drawing or rewriting pixels."""
from collections import Counter
from pathlib import Path
import hashlib
import json
from PIL import Image

root = Path(__file__).resolve().parent
project = Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo')
palette_text = (project / 'ArtDirection/Palette/linhthu-v1.hex').read_text()
palette = {tuple(bytes.fromhex(line.strip())) for line in palette_text.splitlines() if line.strip()}
names = ['grass-a', 'grass-b', 'grass-c', 'grass-d', 'paving-a', 'paving-b', 'paving-moss', 'paving-edge', 'earth-path']
atlas = Image.open(root / 'garden-ground-atlas.png').convert('RGBA')
assets = {}
images = {}
for index, name in enumerate(names):
    path = root / f'{name}.png'
    im = Image.open(path).convert('RGBA')
    pixels = list(im.get_flattened_data())
    colors = Counter(pixels)
    alpha = sorted({p[3] for p in pixels})
    assert im.size == (16, 16), name
    assert alpha == [255], name
    assert len(colors) <= 16, name
    assert all(p[:3] in palette for p in pixels), name
    x, y = index % 3 * 16, index // 3 * 16
    assert list(atlas.crop((x, y, x + 16, y + 16)).get_flattened_data()) == pixels, name
    assets[name] = {'size': list(im.size), 'alpha_values': alpha, 'opaque_pixels': 256,
                    'colour_count': len(colors), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                    'colours': {f'#{r:02X}{g:02X}{b:02X}': n for (r, g, b, a), n in colors.most_common()},
                    'pivot_pixels': [8, 8], 'pivot_normalized': [0.5, 0.5], 'ppu': 16}
    images[name] = im

def borders(im):
    return [im.getpixel((x, y)) for x, y in [(x, 0) for x in range(16)] + [(x, 15) for x in range(16)] + [(0, y) for y in range(1, 15)] + [(15, y) for y in range(1, 15)]]

grass_base = (0x83, 0xB8, 0x65, 255)
cream_base = (0xE8, 0xDD, 0xB6, 255)
assert all(set(borders(images[n])) == {grass_base} for n in names[:4])
assert all(set(borders(images[n])) == {cream_base} for n in names[4:7])
edge = images['paving-edge']
assert [edge.getpixel((x, 0)) for x in range(16)] == [edge.getpixel((x, 15)) for x in range(16)]
assert all(edge.getpixel((0, y)) == grass_base for y in range(16))
assert all(edge.getpixel((15, y)) == cream_base for y in range(16))
assert set(borders(images['earth-path'])) == {(0xF2, 0xC9, 0xA8, 255)}

existing = {}
for n in ['grass.png', 'stone.png', 'stone-moss.png', 'stone-grass.png', 'tree.png', 'tree-puffy.png']:
    im = Image.open(project / 'Assets/ArtSample/Sprites' / n).convert('RGBA')
    colors = Counter(p for p in im.get_flattened_data() if p[3] == 255)
    existing[n] = {'size': list(im.size), 'dominant_opaque_colours': [{
        'rgba': list(color), 'pixels': count} for color, count in colors.most_common(8)]}
report = {'palette_colours': len(palette), 'used_colours_total': len({p[:3] for im in images.values() for p in im.get_flattened_data()}),
          'all_tiles_opaque': True, 'atlas_exports_match_source_pixels': True,
          'seams': {'grass_all_four_edges_same_base': True, 'paving_a_b_moss_all_edges_same_cream': True,
                    'paving_edge_left_grass_right_cream_top_bottom_match': True, 'earth_all_edges_same_warm_base': True},
          'assets': assets, 'existing_assets_read_only_comparison': existing}
(root / 'statistics.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
(root / 'linhthu-v1.hex').write_text(palette_text)
print(f'PASS: {len(names)} opaque 16x16 tiles; {report["used_colours_total"]} colours total; palette {len(palette)}; exact atlas crop match; all seam assertions pass.')
