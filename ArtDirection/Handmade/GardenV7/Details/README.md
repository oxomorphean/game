# Linh Thú Ký — garden detail v7

Native pixel clusters authored in Aseprite through `garden-details.lua`. Each PNG has a matching editable `.aseprite`; `preview-1x.png` and `preview-4x.png` are native contact sheets. Sources and outputs stay in this temporary directory. The Unity project, existing v4 gate/cherry trees, and v6 Huyền Giáp Quy were not edited.

All sprites use PPU 16, the locked `linhthu-v1.hex` palette, and binary alpha. Light comes from the upper left. Natural props use green shade clusters without a dark character-style contour; stone lantern uses cream stone and a small warm chamber, with no glow overlay.

| PNG | Canvas | Colors | Unity normalized pivot | Placement |
| --- | --- | --- | --- | --- |
| grass-tuft-a.png | 16×16 | 3 | (0.5, 0.125) | Low grass, sparse groups |
| grass-tuft-b.png | 16×16 | 3 | (0.5, 0.125) | Alternate grass silhouette |
| flower-cluster.png | 16×16 | 8 | (0.5, 0.125) | Pink/cream accents near edges |
| fern.png | 32×32 | 4 | (0.5, 0.0625) | Map borders; avoid central path |
| lantern.png | 32×48 | 9 | (0.5, 0.0625) | Ground anchor at base of stone pedestal |
| petal.png | 8×8 | 3 | (0.5, 0.5) | Airborne particle; visible footprint 3×4 px |
| water-ripple.png | 64×16 | 4 total | (0.5, 0.5) per cell | Slice 4 horizontal 16×16 frames, left to right |

`metadata.json` records pixel anchors, footprint rectangles, color limits, and opaque bounds. Pixel coordinates use a top-left origin; Unity pivots use bottom-left. Water/petal use a centered effect pivot rather than a ground foot pivot. Suggested water loop duration is 0.6–0.8 seconds for all four frames; actual runtime timing remains a scene decision.

## Verification

Viewed the actual PNG contact sheets at native 1× and nearest-neighbor 4×. Project validator reports 7 PASS, 0 WARN, 0 FAIL in strict mode. Props use `--max-colors 20`; observed small-prop counts also satisfy the tighter 16-color cap. Water uses `--cell 16x16 --frames 4 --max-colors 6`.

Both validator runs use `--min-outline 0`: the default 60% dark outline rule is inappropriate for the explicitly requested soft natural props and thin light water effect. Strict alpha, palette, color count, padding, orphan-pixel, and animation-anchor checks remain enabled. JSON reports are `pixel-validation-props.json` and `pixel-validation-water.json`.

These sprites have not been imported or tested in Unity. Compare the assembled map in Game View at PPU 16 with the unchanged v4/v6 assets before asserting Unity acceptance.
