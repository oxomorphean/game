# Linh Thú Ký — Core Foundation

Dự án Unity `6000.6.4f1`. Bốn Phase đã được triển khai: repository hợp nhất, bảy trụ cột Domain, Configs/Client/DevTools và MainGame. Kết quả compile, NUnit và giới hạn nghiệm thu được ghi tại [tiến độ](Architecture/CORE_FOUNDATION_PROGRESS.md).

Mở `Assets/_Project/Content/Scenes/MainGame.unity`, bấm **Play**. Scene chứa MeadowMainMap, PlayerCultivator, Huyền Quy, mục tiêu chiến đấu, linh thảo, linh khoáng, linh điền, trứng, cổng và bờ câu bên hồ. Build Settings chỉ có MainGame.

| Điều khiển | Hành động |
| --- | --- |
| WASD / phím mũi tên | Di chuyển có gia tốc/phanh, trượt quanh footprint |
| Space / LeftShift | Lướt khóa hướng 0.2 giây, hồi riêng 1 giây |
| 1 / 2 / 3 | Linh Kiếm / Thanh Phong Bộ / Hộ Thể, ngắm theo chuột |
| Chuột trái | Linh Kiếm; không thi triển khi nhấn UI |
| Nút công pháp trên HUD | Ngắm mục tiêu thù địch gần nhất |
| E | Tương tác với mục tiêu ghi trên nhãn `[E]` phía trên vật thể: thu hoạch, gieo hạt, dùng cổng hoặc câu cá |
| X | Ngồi thiền / đứng dậy; hồi Linh lực gấp 3 và +1 Tu vi/giây theo cấu hình mẫu |
| F | Gần bất kỳ bờ nào của hồ: thả câu; sau 4 giây cá cắn, nhấn lại để nhận Linh Ngư. Thu sớm sẽ cất cần |
| R | Triệu hồi / thu hồi Huyền Quy |
| Tab | Đổi ba đạo bào đang có trong túi |
| H / Y | Đổi tóc ink/silver và mắt amber/jade |
| F5 / F9 | Lưu / đọc Foundation theo yêu cầu người chơi |

Di chuyển, Dash, thi triển công pháp hợp lệ hoặc trúng đòn sẽ hủy thiền/câu cá. Khi túi đầy, cá đang cắn vẫn chờ để người chơi thu lại sau khi dọn túi. Chưa có hệ thống đặt Linh Điền.

Save schema 2 nằm tại `Application.persistentDataPath/linhthu.foundation/v2/save.json`; lưu đủ ngoại hình, vận tốc, lướt và tiến trình thiền/câu cá, ghi thay thế nguyên tử, giữ `.bak` của bản trước. Dữ liệu hoạt động mới là trường bổ sung; save schema 2 cũ mặc định không có hoạt động. Bản lưu thiếu điểm câu trong bố cục MainGame mới bị từ chối, không thay live world hoặc ghi đè file. File schema 1 và save cũ được giữ nguyên, chưa có migration. Thông số SO là dữ liệu mẫu, chưa phải cân bằng sản phẩm.

Chỉ một vật thể trong tầm và sẵn sàng được chọn; nhích lại gần sẽ đổi mục tiêu, thu hoạch xong tự chuyển sang vật thể kế tiếp. Nhãn nổi giữ tên item, nằm hoàn toàn trên vật thể và đồng bộ với nút E. Hồ hiện “Thả câu” phía trên hồ; tầm câu đo tới mép hồ gần nhất, dùng được quanh toàn bộ bờ. Tên, trạng thái và số sát thương nằm trên đầu nhân vật liên quan, không che động tác.

Asset chuẩn: `Art/Characters/HeroBase`, `Art/Environment/Meadow`, `Content/Configs`, `Content/Prefabs/{Actors,WorldNodes,Environment,UI}` dưới `Assets/_Project`. Cụm đầu 78%, vai thu gọn, tay thư thái; mặc định áo classic. Áo/tóc/mắt thay Sprite trên cùng rig/AnimatorController; tint và hit flash dùng MaterialPropertyBlock với material chung. Run giữ hông gần Idle, chân trụ được hiệu chỉnh: `BaseStrideSpeed=0.9375`, clip chạy tỷ lệ trực tiếp với vận tốc tới ngưỡng 6, rồi nén mềm. Pose thiền/câu cá trộn sau Animator, cần trúc neo ở bàn tay. Map dùng texture vẽ tay tiling, ánh nắng nền/đường, cỏ/bụi cây phản ứng theo actor và cánh hoa theo gió. Bụi phát ở nhịp chân tiếp đất, phanh/đổi hướng/lướt; footprint không bị hiệu ứng thay đổi.

Kiểm logic bằng Unity Test Runner **EditMode** với hai assembly `LinhThu.Tests.EditMode`, `LinhThu.Tests.ClientEditMode`. Chủ dự án nghiệm thu hình ảnh trực tiếp trong Editor: idle/run, flip, đổi đồ giữa clip, điểm chân và bóng. Không dùng builder hoặc Web QA.

[Kiến trúc](Architecture/CODE_ARCHITECTURE.md) · [Kế hoạch 1–2](Architecture/CORE_FOUNDATION_PHASES_1_2.md) · [Kế hoạch 3–4](Architecture/CORE_FOUNDATION_PHASES_3_4.md) · [Thiết kế](GAME_DESIGN.md) · [Mỹ thuật](ArtDirection/STYLE_GUIDE.md) · [Khôi phục](ROLLBACK.md).
