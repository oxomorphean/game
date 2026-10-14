# Nguồn mỹ thuật hiện hành

Chuẩn hình ảnh nằm trong [STYLE_GUIDE.md](STYLE_GUIDE.md). Scene chạy chính là `Assets/_Project/Content/Scenes/ArtPreview.unity`.

| Nội dung | Nguồn / nơi dùng |
| --- | --- |
| Map chính | `Assets/_Project/Art/CartoonV2/`, prefab `MeadowMainMap`; nền/đường mesh và prop riêng. |
| Nhân vật chính | `HeroPainted2D`, definition `Content/Characters/Hero8Directions.asset`; tên definition được giữ để ổn định tham chiếu. |
| Mẫu tách lớp cũ | `HeroModular2D`, scene `HeroModularTrial`; material còn được mẫu PSD sử dụng. |
| Mẫu Krita/PSD | [HeroProduction2D](HeroProduction2D/README.md), scene `HeroProductionTrial`; chưa thay nhân vật chính. |
| Thử tác giả A/B/C/C2 | [CharacterProduction](CharacterProduction/README.md); giữ master, layer, export, clip và scene C2 để duyệt. |

Bộ Blender `HeroEightDirections` và `CartoonV1` không còn được scene/asset nào tham chiếu, đã gỡ khỏi checkout. Không dùng prompt cũ làm chuẩn hiện hành. Nguồn khôi phục và quy tắc frame/ảnh kiểm chứng nằm trong [quản lý dung lượng](../Architecture/REPOSITORY_STORAGE.md).
