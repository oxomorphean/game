#!/usr/bin/env python3
"""Chuẩn hóa ảnh AI (hoặc sprite PixelLab) thành pixel art đúng chuẩn Linh Thú Ký.

Các bước: tách nền -> chia frame -> thu nhỏ bằng "màu trội" (không lấy trung bình,
nên không sinh màu nhòe) -> map về palette khóa (OKLab, không dither) -> alpha 0/255
-> dọn pixel lẻ -> chuẩn hóa viền -> xếp vào ô cố định, cùng điểm tiếp đất -> validate.

Ví dụ:
  # Sheet AI 3x2 frame -> sheet 32x32/ô
  python3 AgentScripts/pixel/pixelize.py ArtDirection/Sources/fox-run-v3.png \\
      --grid 3x2 --cell 32x32 -o out/fox-run.png --preview 8

  # Sprite PixelLab đã đúng kích thước: chỉ khóa palette + dọn alpha
  python3 AgentScripts/pixel/pixelize.py pixellab_fox.png --native -o out/fox.png

  # Prop đơn: cổng 128x128, giữ viền gốc
  python3 AgentScripts/pixel/pixelize.py gate-v2.png --cell 128x128 --outline keep -o out/gate.png

Kết quả chỉ là bản chuẩn hóa tự động; vẫn cần duyệt và chỉnh tay trong Aseprite.
"""
from __future__ import annotations

import argparse
import json
import math
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from pixel_common import (DEFAULT_PALETTE, OUTLINE_HEX, PROJECT_ROOT, edge_mask,  # noqa: E402
                          hex_to_rgb, load_palette, opaque_bbox, rgb_to_oklab,
                          save_rgba, snap_to_palette)


def parse_pair(text: str) -> tuple[int, int]:
    a, b = text.lower().split("x")
    return int(a), int(b)


def remove_background(img: Image.Image, tolerance: int) -> Image.Image:
    """Ảnh không có alpha: flood-fill nền từ viền ảnh thành trong suốt."""
    rgba = img.convert("RGBA")
    if np.array(rgba)[..., 3].min() < 255:
        return rgba  # đã có nền trong suốt
    marker = (255, 0, 255)
    rgb = rgba.convert("RGB")
    w, h = rgb.size
    seeds = [(x, y) for x in range(0, w, max(1, w // 16)) for y in (0, h - 1)]
    seeds += [(x, y) for y in range(0, h, max(1, h // 16)) for x in (0, w - 1)]
    for s in seeds:
        if rgb.getpixel(s) != marker:
            ImageDraw.floodfill(rgb, s, marker, thresh=tolerance)
    bg = np.all(np.array(rgb) == marker, axis=-1)
    arr = np.array(rgba)
    arr[bg, 3] = 0
    return Image.fromarray(arr, "RGBA")


def split_frames(arr: np.ndarray, grid: tuple[int, int] | None):
    if not grid:
        return [arr]
    cols, rows = grid
    h, w = arr.shape[:2]
    fw, fh = w // cols, h // rows
    return [arr[r * fh:(r + 1) * fh, c * fw:(c + 1) * fw] for r in range(rows) for c in range(cols)]


def dominant_downsample(src_rgb_snapped: np.ndarray, mask: np.ndarray, palette: np.ndarray,
                        out_w: int, out_h: int, coverage: float, dark_boost: float) -> np.ndarray:
    """Mỗi pixel đích = màu palette xuất hiện nhiều nhất trong khối nguồn tương ứng.

    Màu tối (viền, mắt) được nhân trọng số để nét mảnh không biến mất khi thu nhỏ.
    """
    h, w = mask.shape
    xs = np.linspace(0, w, out_w + 1).round().astype(int)
    ys = np.linspace(0, h, out_h + 1).round().astype(int)
    pal_idx = np.full(mask.shape, -1, dtype=np.int32)
    lookup = {tuple(c): i for i, c in enumerate(palette.tolist())}
    flat = src_rgb_snapped.reshape(-1, 3)
    uniq, inv = np.unique(flat, axis=0, return_inverse=True)
    uniq_idx = np.array([lookup[tuple(c)] for c in uniq.tolist()])
    pal_idx[:] = uniq_idx[inv.reshape(-1)].reshape(mask.shape)
    pal_idx[~mask] = -1

    luma = palette.astype(np.float64) @ np.array([0.299, 0.587, 0.114])
    weights = np.where(luma < 90, 1.0 + dark_boost, 1.0)

    out = np.zeros((out_h, out_w, 4), dtype=np.uint8)
    for oy in range(out_h):
        for ox in range(out_w):
            block = pal_idx[ys[oy]:max(ys[oy + 1], ys[oy] + 1), xs[ox]:max(xs[ox + 1], xs[ox] + 1)]
            solid = block[block >= 0]
            if solid.size == 0 or solid.size / block.size < coverage:
                continue
            counts = np.bincount(solid, minlength=len(palette)) * weights
            out[oy, ox, :3] = palette[int(counts.argmax())]
            out[oy, ox, 3] = 255
    return out


def clean(sprite: np.ndarray) -> np.ndarray:
    """Xóa pixel đặc đứng một mình; lấp lỗ trong suốt 1px bị bao 4 phía."""
    a = sprite[..., 3] == 255
    p = np.pad(a, 1, constant_values=False)
    n8 = sum(np.roll(np.roll(p, dy, 0), dx, 1) for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx)[1:-1, 1:-1]
    sprite[a & (n8 == 0), 3] = 0
    a = sprite[..., 3] == 255
    p = np.pad(a, 1, constant_values=False)
    n4 = p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]
    holes = ~a & n4
    if holes.any():
        rgb = np.pad(sprite[..., :3], ((1, 1), (1, 1), (0, 0)))
        for y, x in zip(*np.nonzero(holes)):
            sprite[y, x, :3] = rgb[y, x + 1]  # lấy màu pixel bên trái
            sprite[y, x, 3] = 255
    return sprite


def _luma(rgb) -> float:
    return float(np.dot(np.asarray(rgb, dtype=np.float64), [0.299, 0.587, 0.114]))


def despeckle(sprite: np.ndarray, threshold: float) -> np.ndarray:
    """Pixel lẻ có 4 láng giềng cùng một màu khác và gần màu nó (OKLab) -> đổi theo láng giềng.

    Không đổi pixel tối (nét, mắt) và không đổi khi láng giềng là màu tối (giữ highlight mắt).
    """
    if threshold <= 0:
        return sprite
    h, w = sprite.shape[:2]
    src = sprite.copy()
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            if src[y, x, 3] != 255:
                continue
            n = [src[y - 1, x], src[y + 1, x], src[y, x - 1], src[y, x + 1]]
            if any(p[3] != 255 for p in n):
                continue
            first = tuple(n[0][:3])
            if any(tuple(p[:3]) != first for p in n[1:]) or first == tuple(src[y, x, :3]):
                continue
            if _luma(src[y, x, :3]) < 90 or _luma(first) < 90:
                continue
            lab = rgb_to_oklab(np.array([src[y, x, :3], first]))
            if np.linalg.norm(lab[0] - lab[1]) < threshold:
                sprite[y, x, :3] = first
    return sprite


def reduce_colors(sprite: np.ndarray, max_colors: int, keep: tuple[int, int, int] | None) -> np.ndarray:
    """Gộp dần màu ít dùng nhất vào màu còn lại gần nhất cho tới khi ≤ max_colors."""
    a = sprite[..., 3] == 255
    while True:
        cols, counts = np.unique(sprite[a][:, :3], axis=0, return_counts=True)
        if len(cols) <= max_colors:
            return sprite
        order = np.argsort(counts)
        victim = None
        for i in order:
            if keep is None or tuple(cols[i]) != keep:
                victim = i
                break
        rest = np.delete(cols, victim, axis=0)
        lab = rgb_to_oklab(np.vstack([cols[victim:victim + 1], rest]))
        target = rest[int(((lab[1:] - lab[0]) ** 2).sum(1).argmin())]
        hit = a & np.all(sprite[..., :3] == cols[victim], axis=-1)
        sprite[hit, :3] = target


def apply_outline(sprite: np.ndarray, mode: str, color: tuple[int, int, int]) -> np.ndarray:
    if mode == "keep":
        return sprite
    a = sprite[..., 3] == 255
    if mode == "recolor":
        sprite[edge_mask(a), :3] = color
    elif mode == "add":
        p = np.pad(a, 1, constant_values=False)
        grown = p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:]
        ring = grown & ~a
        sprite[ring, :3] = color
        sprite[ring, 3] = 255
    return sprite


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("input", type=Path)
    ap.add_argument("-o", "--output", type=Path, required=True)
    ap.add_argument("--palette", default=str(DEFAULT_PALETTE))
    ap.add_argument("--grid", type=parse_pair, help="số frame trong ảnh nguồn, dạng CỘTxHÀNG (vd 3x2)")
    ap.add_argument("--cell", type=parse_pair, help="kích thước ô đầu ra, vd 32x32 (bỏ qua khi --native)")
    ap.add_argument("--native", action="store_true", help="ảnh đã đúng độ phân giải (PixelLab): không resize")
    ap.add_argument("--subject-height", type=int, help="chiều cao chủ thể mong muốn (px); mặc định vừa ô")
    ap.add_argument("--pad", type=int, default=1, help="padding tối thiểu quanh sprite trong ô")
    ap.add_argument("--baseline", type=int, help="khoảng cách từ đáy ô tới chân (mặc định = pad)")
    ap.add_argument("--align", choices=["bottom", "union"], default="bottom",
                    help="bottom: mọi frame cùng đường chân; union: giữ chuyển động lên xuống gốc")
    ap.add_argument("--outline", choices=["recolor", "add", "keep"], default="recolor",
                    help=f"recolor: mép ngoài thành {OUTLINE_HEX}; add: thêm 1px viền; keep: giữ nguyên")
    ap.add_argument("--outline-color", default=OUTLINE_HEX)
    ap.add_argument("--alpha-threshold", type=int, default=128)
    ap.add_argument("--coverage", type=float, default=0.45, help="tỉ lệ phủ tối thiểu để pixel đích là đặc")
    ap.add_argument("--dark-boost", type=float, default=1.0, help="trọng số thêm cho màu tối khi thu nhỏ")
    ap.add_argument("--bg-tolerance", type=int, default=24)
    ap.add_argument("--no-clean", action="store_true")
    ap.add_argument("--max-colors", type=int, default=16, help="số màu tối đa mỗi frame (mặc định 16)")
    ap.add_argument("--despeckle", type=float, default=0.16,
                    help="ngưỡng OKLab để khử đốm 1px; 0 = tắt")
    ap.add_argument("--out-cols", type=int, help="số cột của sheet đầu ra (mặc định = cột nguồn)")
    ap.add_argument("--preview", type=int, default=0, help="xuất thêm ảnh phóng N lần trên nền cỏ")
    ap.add_argument("--no-validate", action="store_true")
    args = ap.parse_args()

    palette = load_palette(args.palette)
    outline_rgb = hex_to_rgb(args.outline_color)
    src = np.array(remove_background(Image.open(args.input), args.bg_tolerance))
    frames = split_frames(src, args.grid)

    # Ảnh native: ô = kích thước frame nguồn, tỉ lệ 1:1
    if args.native:
        fh, fw = frames[0].shape[:2]
        cell_w, cell_h = fw, fh
    elif args.cell:
        cell_w, cell_h = args.cell
    else:
        ap.error("cần --cell WxH hoặc --native")

    masks = [f[..., 3] >= args.alpha_threshold for f in frames]
    boxes = [opaque_bbox(m) for m in masks]
    if any(b is None for b in boxes):
        ap.error("có frame rỗng sau khi tách nền; kiểm tra --grid hoặc --alpha-threshold")

    pad = args.pad
    baseline = args.baseline if args.baseline is not None else pad
    if args.native:
        scale = 1.0
    else:
        ux0 = min(b[0] for b in boxes); ux1 = max(b[2] for b in boxes)
        uy0 = min(b[1] for b in boxes); uy1 = max(b[3] for b in boxes)
        max_h = max(b[3] - b[1] for b in boxes)
        span_w = ux1 - ux0
        span_h = (uy1 - uy0) if args.align == "union" else max_h
        if args.subject_height:
            scale = max_h / args.subject_height
        else:
            scale = max(span_w / (cell_w - 2 * pad), span_h / (cell_h - pad - baseline))
        scale = max(scale, 1.0)

    out_frames, placements = [], []
    for frame, mask, (bx0, by0, bx1, by1) in zip(frames, masks, boxes):
        if args.native:
            sprite = np.zeros((cell_h, cell_w, 4), np.uint8)
            sprite[..., :3] = snap_to_palette(frame[..., :3], palette)
            sprite[..., 3] = np.where(mask, 255, 0)
        else:
            crop_rgb = frame[by0:by1, bx0:bx1, :3]
            crop_mask = mask[by0:by1, bx0:bx1]
            snapped = snap_to_palette(crop_rgb, palette)
            ow = max(1, round((bx1 - bx0) / scale))
            oh = max(1, round((by1 - by0) / scale))
            small = dominant_downsample(snapped, crop_mask, palette, ow, oh, args.coverage, args.dark_boost)
            sprite = np.zeros((cell_h, cell_w, 4), np.uint8)
            x = pad + round((bx0 - ux0) / scale) + max(0, (cell_w - 2 * pad - round(span_w / scale)) // 2)
            if args.align == "union":
                y = cell_h - baseline - round((uy1 - by0) / scale)
            else:
                y = cell_h - baseline - oh
            x = int(np.clip(x, 0, cell_w - ow)); y = int(np.clip(y, 0, cell_h - oh))
            region = sprite[y:y + oh, x:x + ow]
            region[:] = small[:region.shape[0], :region.shape[1]]
            placements.append([x, y, ow, oh])
        if not args.no_clean:
            sprite = clean(sprite)
        sprite = despeckle(sprite, args.despeckle)
        sprite = apply_outline(sprite, args.outline, outline_rgb)
        sprite = reduce_colors(sprite, args.max_colors, outline_rgb if args.outline != "keep" else None)
        out_frames.append(sprite)

    n = len(out_frames)
    cols = args.out_cols or (args.grid[0] if args.grid else 1)
    rows = math.ceil(n / cols)
    sheet = np.zeros((rows * cell_h, cols * cell_w, 4), np.uint8)
    for i, f in enumerate(out_frames):
        r, c = divmod(i, cols)
        sheet[r * cell_h:(r + 1) * cell_h, c * cell_w:(c + 1) * cell_w] = f
    save_rgba(sheet, args.output)

    def rel(p: Path) -> str:
        try:
            return str(p.resolve().relative_to(PROJECT_ROOT))
        except ValueError:
            return str(p)

    meta = {
        "source": rel(args.input), "palette": rel(Path(args.palette)),
        "cell": [cell_w, cell_h], "frames": n, "columns": cols,
        "source_px_per_output_px": round(scale, 3),
        "pivot_px": [cell_w / 2, baseline], "pivot_normalized": [0.5, round(baseline / cell_h, 4)],
        "align": args.align, "outline": args.outline, "native": args.native,
        "note": "Chuẩn hóa tự động — cần duyệt và chỉnh tay trước khi coi là asset sản xuất.",
    }
    meta_path = args.output.with_suffix(".json")
    meta_path.write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Đã ghi {args.output} ({sheet.shape[1]}x{sheet.shape[0]}, {n} frame {cell_w}x{cell_h}, "
          f"tỉ lệ {scale:.2f} px nguồn/px) và {meta_path.name}")

    if args.preview:
        s = args.preview
        bg = Image.new("RGBA", (sheet.shape[1] * s, sheet.shape[0] * s), (131, 184, 101, 255))
        big = Image.fromarray(sheet, "RGBA").resize(bg.size, Image.NEAREST)
        bg.alpha_composite(big)
        prev = args.output.with_name(args.output.stem + f"_preview_x{s}.png")
        bg.save(prev)
        print(f"Preview: {prev}")

    if not args.no_validate:
        cmd = [sys.executable, str(Path(__file__).with_name("pixel_validate.py")), str(args.output),
               "--palette", args.palette, "--cell", f"{cell_w}x{cell_h}", "--frames", str(n),
               "--max-colors", str(args.max_colors)]
        return subprocess.call(cmd)
    return 0


if __name__ == "__main__":
    sys.exit(main())
