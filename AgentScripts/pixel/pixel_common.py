"""Hàm dùng chung cho pipeline pixel art của Linh Thú Ký.

Chỉ phụ thuộc Pillow và numpy. Không import UnityEngine; chạy ngoài Unity.
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image

PROJECT_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PALETTE = PROJECT_ROOT / "ArtDirection" / "Palette" / "linhthu-v2.gpl"
OUTLINE_HEX = "#263046"


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.strip().lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))  # type: ignore[return-value]


def rgb_to_hex(rgb) -> str:
    return "#{:02X}{:02X}{:02X}".format(*[int(c) for c in rgb[:3]])


def load_palette(path: str | Path = DEFAULT_PALETTE) -> np.ndarray:
    """Đọc .gpl (GIMP/Aseprite) hoặc .hex (Lospec). Trả về mảng (N, 3) uint8."""
    path = Path(path)
    colors: list[tuple[int, int, int]] = []
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if path.suffix.lower() == ".hex":
            colors.append(hex_to_rgb(line))
            continue
        if line.startswith(("GIMP Palette", "Name:", "Columns:")):
            continue
        parts = line.split()
        if len(parts) >= 3 and all(p.isdigit() for p in parts[:3]):
            colors.append((int(parts[0]), int(parts[1]), int(parts[2])))
    if not colors:
        raise ValueError(f"Palette rỗng hoặc sai định dạng: {path}")
    return np.array(colors, dtype=np.uint8)


# ---------- Màu cảm quan OKLab: chọn màu gần nhất theo mắt người ----------

def _srgb_to_linear(c: np.ndarray) -> np.ndarray:
    c = c / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def rgb_to_oklab(rgb: np.ndarray) -> np.ndarray:
    lin = _srgb_to_linear(rgb.astype(np.float64))
    m1 = np.array([[0.4122214708, 0.5363325363, 0.0514459929],
                   [0.2119034982, 0.6806995451, 0.1073969566],
                   [0.0883024619, 0.2817188376, 0.6299787005]])
    m2 = np.array([[0.2104542553, 0.7936177850, -0.0040720468],
                   [1.9779984951, -2.4285922050, 0.4505937099],
                   [0.0259040371, 0.7827717662, -0.8086757660]])
    lms = np.cbrt(lin @ m1.T)
    return lms @ m2.T


def snap_to_palette(rgb: np.ndarray, palette: np.ndarray) -> np.ndarray:
    """Map từng pixel (H, W, 3) về màu palette gần nhất trong OKLab, không dither."""
    flat = rgb.reshape(-1, 3)
    uniq, inverse = np.unique(flat, axis=0, return_inverse=True)
    d = ((rgb_to_oklab(uniq)[:, None, :] - rgb_to_oklab(palette)[None, :, :]) ** 2).sum(-1)
    return palette[d.argmin(1)][inverse.reshape(-1)].reshape(rgb.shape)


def palette_distance(rgb: np.ndarray, palette: np.ndarray) -> np.ndarray:
    """Khoảng cách OKLab từ mỗi màu (N, 3) tới màu palette gần nhất."""
    d = ((rgb_to_oklab(rgb)[:, None, :] - rgb_to_oklab(palette)[None, :, :]) ** 2).sum(-1)
    return np.sqrt(d.min(1))


def load_rgba(path: str | Path) -> np.ndarray:
    return np.array(Image.open(path).convert("RGBA"))


def save_rgba(arr: np.ndarray, path: str | Path) -> None:
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(arr.astype(np.uint8), "RGBA").save(path, optimize=True)


def opaque_bbox(alpha: np.ndarray):
    ys, xs = np.nonzero(alpha)
    if len(xs) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def edge_mask(alpha_bool: np.ndarray) -> np.ndarray:
    """Pixel đặc nằm cạnh pixel trong suốt hoặc mép ảnh (4 hướng)."""
    p = np.pad(alpha_bool, 1, constant_values=False)
    neighbors_clear = ~p[:-2, 1:-1] | ~p[2:, 1:-1] | ~p[1:-1, :-2] | ~p[1:-1, 2:]
    return alpha_bool & neighbors_clear


def iter_cells(arr: np.ndarray, cell_w: int | None, cell_h: int | None):
    """Chia sheet thành ô theo hàng; không truyền cell thì cả ảnh là một ô."""
    h, w = arr.shape[:2]
    if not cell_w or not cell_h:
        yield 0, 0, 0, arr
        return
    index = 0
    for y in range(0, h - cell_h + 1, cell_h):
        for x in range(0, w - cell_w + 1, cell_w):
            yield index, x, y, arr[y:y + cell_h, x:x + cell_w]
            index += 1
