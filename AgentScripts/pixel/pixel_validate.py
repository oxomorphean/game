#!/usr/bin/env python3
"""Kiểm tra sprite có đúng chuẩn pixel art của Linh Thú Ký không.

Ví dụ:
  python3 AgentScripts/pixel/pixel_validate.py Assets/ArtSample/Sprites/*.png
  python3 AgentScripts/pixel/pixel_validate.py fox-run.png --cell 32x32 --frames 6
  python3 AgentScripts/pixel/pixel_validate.py grass.png --kind tile --size 16x16
  python3 AgentScripts/pixel/pixel_validate.py out/*.png --json report.json

Mã thoát 0 = không có FAIL, 1 = có FAIL (WARN không làm fail, trừ khi --strict).
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from pixel_common import (DEFAULT_PALETTE, OUTLINE_HEX, edge_mask, hex_to_rgb,  # noqa: E402
                          iter_cells, load_palette, load_rgba, opaque_bbox,
                          palette_distance, rgb_to_hex)


def parse_size(text: str | None):
    if not text:
        return None
    w, h = text.lower().split("x")
    return int(w), int(h)


def validate_file(path: Path, palette: np.ndarray, args) -> dict:
    arr = load_rgba(path)
    h, w = arr.shape[:2]
    alpha = arr[..., 3]
    issues: list[dict] = []

    def add(level: str, code: str, msg: str):
        issues.append({"level": level, "code": code, "message": msg})

    # 1. Alpha phải nhị phân: 0 hoặc 255
    semi = int(((alpha > 0) & (alpha < 255)).sum())
    if semi:
        add("FAIL", "semi_alpha", f"{semi} pixel bán trong suốt (chỉ cho phép alpha 0/255)")

    opaque = alpha > 0  # bán trong suốt đã báo ở trên; vẫn tính vào màu/hình dạng
    rgb = arr[..., :3]
    kind = args.kind
    if kind == "auto":
        kind = "tile" if (alpha == 255).all() else "sprite"

    # 2. Mọi màu phải thuộc palette khóa
    used = np.unique(rgb[opaque].reshape(-1, 3), axis=0) if opaque.any() else np.zeros((0, 3), np.uint8)
    pal_set = {tuple(c) for c in palette.tolist()}
    off = np.array([c for c in used.tolist() if tuple(c) not in pal_set], dtype=np.uint8).reshape(-1, 3)
    if len(off):
        dist = palette_distance(off, palette)
        worst = ", ".join(rgb_to_hex(c) for c in off[np.argsort(-dist)][:6])
        add("FAIL", "off_palette", f"{len(off)} màu ngoài palette (ví dụ: {worst})")

    # 3. Kích thước
    cell = parse_size(args.cell)
    expected = parse_size(args.size)
    if cell and (w % cell[0] or h % cell[1]):
        add("FAIL", "cell_mismatch", f"ảnh {w}x{h} không chia hết cho ô {cell[0]}x{cell[1]}")
    if expected and not cell and (w, h) != expected:
        add("FAIL", "size", f"kích thước {w}x{h}, cần {expected[0]}x{expected[1]}")
    if expected and cell and cell != expected:
        add("WARN", "size", f"ô {cell[0]}x{cell[1]} khác kích thước kỳ vọng {expected[0]}x{expected[1]}")
    unit = cell or (w, h)
    if unit[0] % args.grid or unit[1] % args.grid:
        add("WARN", "grid", f"ô {unit[0]}x{unit[1]} không là bội số của {args.grid}")

    # 4. Kiểm tra từng ô / frame
    outline = np.array(hex_to_rgb(OUTLINE_HEX))
    dark_cut = args.outline_luma
    frames = []
    for index, x0, y0, sub in iter_cells(arr, *(cell or (None, None))):
        if args.frames and index >= args.frames:
            break
        a = sub[..., 3] > 0
        bbox = opaque_bbox(a)
        tag = f"frame {index}" if cell else "ảnh"
        if bbox is None:
            add("FAIL", "empty", f"{tag}: rỗng")
            frames.append(None)
            continue
        colors = len(np.unique(sub[..., :3][a].reshape(-1, 3), axis=0))
        if colors > args.max_colors:
            add("FAIL", "too_many_colors", f"{tag}: {colors} màu (tối đa {args.max_colors})")

        if kind == "tile":
            if not a.all() and not args.allow_transparent_tile:
                add("WARN", "tile_hole", f"{tag}: tile có pixel trong suốt")
        else:
            bx0, by0, bx1, by1 = bbox
            sh, sw = a.shape
            if bx0 == 0 or by0 == 0 or bx1 == sw or by1 == sh:
                add("WARN", "no_padding", f"{tag}: chạm mép ô, nên chừa ít nhất 1px padding")
            edges = edge_mask(a)
            edge_rgb = sub[..., :3][edges].astype(np.float64)
            luma = edge_rgb @ np.array([0.299, 0.587, 0.114])
            dark_ratio = float((luma <= dark_cut).mean()) if len(luma) else 1.0
            if dark_ratio < args.min_outline:
                add("WARN", "weak_outline",
                    f"{tag}: chỉ {dark_ratio:.0%} pixel viền là màu tối (nên ≥ {args.min_outline:.0%}, ví dụ {OUTLINE_HEX})")
            # Pixel mồ côi: đặc nhưng 8 ô xung quanh đều trong suốt
            p = np.pad(a, 1, constant_values=False)
            neigh = sum(np.roll(np.roll(p, dy, 0), dx, 1)
                        for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx)[1:-1, 1:-1]
            orphans = int((a & (neigh == 0)).sum())
            if orphans:
                add("WARN", "orphan_pixels", f"{tag}: {orphans} pixel lẻ đứng một mình")
        frames.append({"index": index, "bbox": bbox, "colors": colors})

    # 5. Độ ổn định giữa các frame (điểm tiếp đất, chiều cao)
    valid = [f for f in frames if f]
    if cell and len(valid) > 1 and kind != "tile":
        bottoms = [f["bbox"][3] for f in valid]
        if max(bottoms) - min(bottoms) > args.anchor_tolerance:
            add("WARN", "anchor_drift", f"điểm tiếp đất lệch {max(bottoms) - min(bottoms)}px giữa các frame {bottoms}")
        heights = [f["bbox"][3] - f["bbox"][1] for f in valid]
        if min(heights) and max(heights) / min(heights) > 1.25:
            add("WARN", "size_drift", f"chiều cao sprite dao động {min(heights)}–{max(heights)}px giữa các frame")

    total_colors = len(used)
    status = "FAIL" if any(i["level"] == "FAIL" for i in issues) else ("WARN" if issues else "PASS")
    return {"file": str(path), "kind": kind, "size": [w, h], "colors": total_colors,
            "frames": len(valid), "status": status, "issues": issues}


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("files", nargs="+", type=Path)
    ap.add_argument("--palette", default=str(DEFAULT_PALETTE))
    ap.add_argument("--kind", choices=["auto", "sprite", "tile"], default="auto",
                    help="auto: ảnh đặc hoàn toàn coi là tile, còn lại là sprite")
    ap.add_argument("--cell", help="kích thước ô của sheet, vd 32x32")
    ap.add_argument("--frames", type=int, help="chỉ kiểm tra N ô đầu (sheet có ô trống cuối)")
    ap.add_argument("--size", help="kích thước kỳ vọng, vd 32x48")
    ap.add_argument("--grid", type=int, default=8, help="kích thước nên là bội số của số này (mặc định 8)")
    ap.add_argument("--max-colors", type=int, default=16, help="số màu tối đa mỗi frame (mặc định 16)")
    ap.add_argument("--min-outline", type=float, default=0.6, help="tỉ lệ pixel viền tối tối thiểu")
    ap.add_argument("--outline-luma", type=float, default=110, help="ngưỡng độ sáng coi là màu viền tối")
    ap.add_argument("--anchor-tolerance", type=int, default=1)
    ap.add_argument("--allow-transparent-tile", action="store_true")
    ap.add_argument("--strict", action="store_true", help="WARN cũng làm fail")
    ap.add_argument("--json", type=Path, help="ghi báo cáo JSON ra file")
    args = ap.parse_args()

    palette = load_palette(args.palette)
    results = [validate_file(f, palette, args) for f in args.files]

    icon = {"PASS": "✅", "WARN": "⚠️ ", "FAIL": "❌"}
    for r in results:
        print(f"{icon[r['status']]} {r['status']}  {r['file']}  ({r['size'][0]}x{r['size'][1]}, {r['colors']} màu)")
        for i in r["issues"]:
            print(f"     [{i['level']}] {i['code']}: {i['message']}")
    counts = {s: sum(r["status"] == s for r in results) for s in ("PASS", "WARN", "FAIL")}
    print(f"\nTổng: {counts['PASS']} PASS · {counts['WARN']} WARN · {counts['FAIL']} FAIL")

    if args.json:
        args.json.write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
    failed = counts["FAIL"] or (args.strict and counts["WARN"])
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
