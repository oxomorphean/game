# Linh Thú Ký — Core Foundation

Dự án Unity `6000.6.4f1`. Bốn Phase đã được triển khai: repository hợp nhất, bảy trụ cột Domain, Configs/Client/DevTools và MainGame. Kết quả compile, NUnit và giới hạn nghiệm thu được ghi tại [tiến độ](Architecture/CORE_FOUNDATION_PROGRESS.md).

Mở `Assets/_Project/Content/Scenes/MainGame.unity`, bấm **Play**. Scene chứa MeadowMainMap, PlayerCultivator, Huyền Quy, mục tiêu chiến đấu, linh thảo, linh khoáng, linh điền, trứng, cổng và bờ câu bên hồ. Bản đồ 48×48 mở sang Đồng cỏ Đông, Rừng Anh Đào và Bãi Đá Yêu Thú; ba bãi Huyền Quy ở (18,3), (7,18), (23,23). Build Settings chỉ có MainGame.

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
| R | Triệu hồi / thu hồi linh thú đang chọn; bấm từng dòng trong bảng I để chọn và gọi/thu hồi |
| T | Thuần Thú Quyết: thu phục yêu thú còn sống, HP ≤30%, trong tầm 2 và không bị vật cản che |
| B | Đột phá khi Viên Mãn và đủ Tu vi; nút sáng khi sẵn sàng |
| I | Ẩn/hiện bảng Túi đồ và danh sách Linh thú |
| Tab | Đổi ID classic/jade đang có trong túi; mẫu nam hiện dùng cùng cơ thể nền, áo bổ sung sau |
| H / Y | Đổi tóc đen/bạc và mắt hổ phách/xanh ngọc |
| F5 / F9 | Lưu / đọc Foundation theo yêu cầu người chơi |

Di chuyển, Dash, thi triển công pháp hợp lệ hoặc trúng đòn sẽ hủy thiền/câu cá. Khi túi đầy, cá đang cắn vẫn chờ để người chơi thu lại sau khi dọn túi. Chưa có hệ thống đặt Linh Điền.

Save schema 3 dùng `Application.persistentDataPath/linhthu.foundation/v3/save.json`, lưu các Players, actor/node nhiều chunk, ngoại hình, chuyển động, hoạt động, cast khóa mục tiêu, AI yêu thú, timer hồi sinh và phần thưởng chờ. Ghi nguyên tử và giữ `.bak`; restore validate toàn graph trước khi đổi world. File v1/v2 giữ nguyên, chưa có migration. Thông số SO là dữ liệu mẫu, chưa phải cân bằng sản phẩm.

Chỉ một vật thể trong tầm và sẵn sàng được chọn; nhích lại gần sẽ đổi mục tiêu, thu hoạch xong tự chuyển sang vật thể kế tiếp. Nhãn nổi giữ tên item, nằm hoàn toàn trên vật thể và đồng bộ với nút E. Hồ hiện “Thả câu” phía trên hồ; tầm câu đo tới mép hồ gần nhất, dùng được quanh toàn bộ bờ. Tên, trạng thái và số sát thương nằm trên đầu nhân vật liên quan, không che động tác.

Asset chuẩn: `Art/Characters/HeroBase`, `Art/Environment/Meadow`, `Content/Configs`, `Content/Prefabs/{Actors,WorldNodes,Environment,UI}` dưới `Assets/_Project`. [Mẫu nam 15/10](Architecture/HERO_MALE_REFRESH.md) có 16 phần tách trong Krita, giữ đầu trọc/mặt trống/quần lót trong basic; classic/jade có hai bộ đồ, tóc ink/silver và mắt amber/jade từ lượt thử 16/10. Mọi xương scale 1, Visual 0.625. ID outfit/tóc/mắt vẫn tương thích dữ liệu; Sprite Swap và AnimatorController được giữ. Các lớp mắt/mày/miệng tách riêng, chớp mắt độc lập và phản hồi cast/hit; xem [mẫu mặt](Architecture/FACE_FEATURES.md). Tint và hit flash dùng MaterialPropertyBlock với material chung. Run giữ hông gần Idle, chân trụ được hiệu chỉnh: `BaseStrideSpeed=0.9375`, clip chạy tỷ lệ trực tiếp với vận tốc tới ngưỡng 6, rồi nén mềm. Pose thiền/câu cá trộn sau Animator, cần trúc neo ở bàn tay. Map dùng texture vẽ tay tiling, ánh nắng nền/đường, cỏ/bụi cây phản ứng theo actor và cánh hoa theo gió. Bụi phát ở nhịp chân tiếp đất, phanh/đổi hướng/lướt; footprint không bị hiệu ứng thay đổi.

Yêu thú báo đòn quạt trong 0.65 giây, khóa hướng để Dash né; vượt leash 8 thì quay về và hồi máu. Tiêu diệt nhận 25 Tu vi và 2 Linh thảo, hồi sinh sau 15 giây. Thu phục đưa linh thú về trạng thái nghỉ trong roster. Túi đầy giữ loot chờ và tạm hoãn hồi sinh bãi đó đến khi nhận được; đây là chính sách thử nghiệm có giới hạn hàng đợi.

HUD hiện hành: thẻ chỉ số/Tu vi/đột phá phía trái trên, bảng I gồm túi và roster phía phải, năm ô kỹ năng ở giữa đáy và micro-bar phím phụ. Mộc Nhân dùng trụ gỗ; đi/lướt trượt mép vật cản và tự đẩy thoát nếu overlap.

Kiểm logic bằng Unity Test Runner **EditMode** với hai assembly `LinhThu.Tests.EditMode`, `LinhThu.Tests.ClientEditMode`. Chủ dự án nghiệm thu hình ảnh trực tiếp trong Editor: idle/run, flip, đổi đồ giữa clip, điểm chân và bóng. Không dùng builder hoặc Web QA.

[Kiến trúc](Architecture/CODE_ARCHITECTURE.md) · [Kế hoạch 1–2](Architecture/CORE_FOUNDATION_PHASES_1_2.md) · [Kế hoạch 3–4](Architecture/CORE_FOUNDATION_PHASES_3_4.md) · [Thiết kế](GAME_DESIGN.md) · [Mỹ thuật](ArtDirection/STYLE_GUIDE.md) · [Khôi phục](ROLLBACK.md).

Chi tiết và bằng chứng mốc bản đồ/thu phục: [Spatial World & Taming](Architecture/SPATIAL_WORLD_AND_TAMING.md).
