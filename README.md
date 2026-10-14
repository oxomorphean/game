# Linh Thú Ký — Core Foundation

Dự án Unity `6000.6.4f1`. Bốn Phase đã được triển khai: repository hợp nhất, bảy trụ cột Domain, Configs/Client/DevTools và MainGame. Kết quả compile, NUnit và giới hạn nghiệm thu được ghi tại [tiến độ](Architecture/CORE_FOUNDATION_PROGRESS.md).

Mở `Assets/_Project/Content/Scenes/MainGame.unity`, bấm **Play**. Scene chứa MeadowMainMap, PlayerCultivator, Huyền Quy, mục tiêu chiến đấu, linh thảo, linh khoáng, linh điền, trứng và cổng. Build Settings chỉ có MainGame.

| Điều khiển | Hành động |
| --- | --- |
| WASD / phím mũi tên | Di chuyển, trượt quanh footprint |
| 1 / 2 / 3 | Linh Kiếm / Thanh Phong Bộ / Hộ Thể, ngắm theo chuột |
| Chuột trái | Linh Kiếm; không thi triển khi nhấn UI |
| Nút công pháp trên HUD | Ngắm mục tiêu thù địch gần nhất |
| E | Tương tác với đúng vật thể mang nhãn nổi `[E]`: thu hoạch, gieo hạt hoặc dùng cổng |
| R | Triệu hồi / thu hồi Huyền Quy |
| Tab | Đổi ba đạo bào đang có trong túi |
| F5 / F9 | Lưu / đọc Foundation theo yêu cầu người chơi |

Save mới nằm tại `Application.persistentDataPath/linhthu.foundation/save.json`; ghi thay thế nguyên tử, giữ `.bak` của bản trước. Không tự đọc hoặc ghi đè save cũ. Thông số SO là dữ liệu mẫu, chưa phải cân bằng sản phẩm. Hệ thống đột phá đã có trong Domain/config; cơ chế kiếm tu vi và UI tiến trình mở rộng chưa nằm trong mốc game loop này.

Chỉ một vật thể trong tầm và sẵn sàng được chọn; nhích lại gần sẽ đổi mục tiêu, thu hoạch xong tự chuyển sang vật thể kế tiếp. Tên trên nhãn nổi và nút E luôn đồng bộ. Mộc Nhân Luyện Công có tên, thanh máu và trạng thái thực tế trên đầu.

Asset chuẩn: `Art/Characters/HeroBase`, `Art/Environment/Meadow`, `Content/Configs`, `Content/Prefabs/{Actors,WorldNodes,Environment,UI}` dưới `Assets/_Project`. Trang phục thay Sprite trên cùng rig/AnimatorController. Map dùng tiling và mesh; prop riêng dùng Y-sort và footprint ở chân.

Kiểm logic bằng Unity Test Runner **EditMode** với hai assembly `LinhThu.Tests.EditMode`, `LinhThu.Tests.ClientEditMode`. Chủ dự án nghiệm thu hình ảnh trực tiếp trong Editor: idle/run, flip, đổi đồ giữa clip, điểm chân và bóng. Không dùng builder hoặc Web QA.

[Kiến trúc](Architecture/CODE_ARCHITECTURE.md) · [Kế hoạch 1–2](Architecture/CORE_FOUNDATION_PHASES_1_2.md) · [Kế hoạch 3–4](Architecture/CORE_FOUNDATION_PHASES_3_4.md) · [Thiết kế](GAME_DESIGN.md) · [Mỹ thuật](ArtDirection/STYLE_GUIDE.md) · [Khôi phục](ROLLBACK.md).
