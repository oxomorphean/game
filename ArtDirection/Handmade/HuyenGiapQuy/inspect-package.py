"""Measure exported frames and create display-only contact previews."""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent
frames = []
rows = []
for tag, count, start in [('idle', 4, 1), ('walk', 6, 5)]:
    sheet = Image.open(root / f'turtle-{tag}.png').convert('RGBA')
    assert sheet.size == (64 * count, 64), sheet.size
    row = Image.new('RGBA', (64 * 6, 64), '#f7f0d8')
    for i in range(count):
        im = sheet.crop((i * 64, 0, (i + 1) * 64, 64))
        bbox = im.getbbox()
        colours = set(im.getdata())
        opaque = {c[:3] for c in colours if c[3] == 255}
        alpha = sorted({c[3] for c in colours})
        assert alpha == [0, 255], alpha
        assert len(opaque) <= 16
        assert bbox[3] == 53, bbox
        assert bbox[2] - bbox[0] <= 52, bbox
        assert bbox[3] - bbox[1] <= 52, bbox
        frames.append({
            'frame': start + i, 'tag': tag, 'cell': i,
            'bbox_xyxy_exclusive': bbox,
            'footprint_wh': [bbox[2] - bbox[0], bbox[3] - bbox[1]],
            'lowest_opaque_y': bbox[3] - 1,
            'opaque_colors': len(opaque), 'alpha_values': alpha,
            'opaque_pixels': sum(p[3] == 255 for p in im.getdata()),
            'pixel_sha256': hashlib.sha256(im.tobytes()).hexdigest(),
        })
        row.alpha_composite(im, (64 * i, 0))
    rows.append(row)
contact = Image.new('RGBA', (384, 128), '#f7f0d8')
for i, row in enumerate(rows):
    contact.alpha_composite(row, (0, i * 64))
contact.resize((1536, 512), Image.Resampling.NEAREST).save(root / 'contact-preview4x.png')
one = Image.open(root / 'turtle.png').convert('RGBA')
back = Image.new('RGBA', one.size, '#f7f0d8')
back.alpha_composite(one)
back.resize((256, 256), Image.Resampling.NEAREST).save(root / 'preview-on-cream4x.png')
metadata = {
    'asset': 'Huyen Giap Quy', 'revision': 'v6', 'workflow': 'Hand-authored Lua pixel clusters executed through Aseprite MCP; native layered source and native PNG/GIF exports',
    'source': 'huyen-giap-quy.aseprite', 'recreation': 'create-huyen-giap-quy.lua',
    'palette': 'linhthu-v1', 'palette_entries': 33, 'opaque_colors_per_frame': 13,
    'canvas_px': [64, 64], 'ppu': 16, 'unity_pivot_px_bottom_left': [32, 12],
    'ground_y_top_left': 52, 'facing': 'right',
    'layer_count': 9, 'cel_count': 90, 'frames': frames,
    'tags': {'idle': [1, 4], 'walk': [5, 10]},
    'durations_ms': [380, 380, 140, 380, 140, 140, 140, 140, 140, 140],
    'rigid_shell_and_pattern': {'checked_frames': list(range(1, 11)), 'mismatched_pixels_after_bob_compensation': 0},
    'strict_pixel_validation': {'pass': 3, 'warn': 0, 'fail': 0, 'report': 'pixel-validation.json'},
    'unity_status': 'Not imported by this art agent; root agent owns Unity verification beside v4 environment',
    'controller_changes': False,
}
(root / 'source-metadata.json').write_text(json.dumps(metadata, indent=2) + '\n')
for frame in frames:
    print(f"frame {frame['frame']} {frame['tag']}: bbox={frame['bbox_xyxy_exclusive']}, footprint={frame['footprint_wh']}, ground_y={frame['lowest_opaque_y']}, colors={frame['opaque_colors']}, alpha={frame['alpha_values']}")
print('Created source-metadata.json and nearest-neighbour display previews.')
