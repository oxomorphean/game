# Kiến trúc code hiện hành

Cập nhật 12/10/2026: theo yêu cầu chủ dự án, bản chạy chỉ phục vụ duyệt mỹ thuật, map và animation. Các hệ combat, chỉ số, công pháp, tu luyện, kho, nghề, rơi/nhặt, quái và save đã được đưa ra khỏi source đang biên dịch. Định hướng MMORPG dài hạn chưa phải tính năng hiện tại. [GAME_DESIGN.md](../GAME_DESIGN.md) ghi ưu tiên hiện hành.

## Luồng và trách nhiệm

`Input System → ArtPreviewHost → PreviewMovement → DirectionalHeroView`. `ArtPreviewHud` trình bày hướng dẫn và tạo `OwnerDashboard`: nút Quản trị luôn hiện trong game, mở Tổng quan/Bản đồ/Asset và chặn input đi lại khi bảng mở. Catalogue đọc metadata do `Editor/OwnerCatalogBuilder` sinh từ AssetDatabase khi Play/build; không tải toàn bộ artwork vào RAM để đếm. [Phạm vi và cách đếm](OWNER_DASHBOARD.md). Scene mặc định là `Assets/_Project/Content/Scenes/ArtPreview.unity`; không khởi tạo phiên gameplay hoặc truy cập save.

- `Domain/Navigation`: C# thuần, assembly `LinhThu.Domain` không tham chiếu Unity. `WorldGeometry` giữ footprint convex/circle/box, giới hạn map và trượt dọc vật cản; `PreviewMovement` giữ vị trí cùng độ dịch chuyển đã giải quyết va chạm. Bán kính chân và tốc độ đến từ controller đã cấu hình.
- `Client/Preview`: input, camera, bootstrap và HUD mẫu. `ObstacleAuthoring` chuyển contour của prefab thành dữ liệu hình học; vị trí và animation không được suy ra từ input chưa xử lý va chạm.
- `Client/Characters`: definition và renderer cho hình minh họa nhiều hướng; mẫu nhân vật tách lớp dùng `ModularHeroDefinition`, `ModularHeroRig` và `ModularHeroTrial`. Đổi tóc/áo/kiếm ở mẫu này chỉ là đổi hình, không có chỉ số, trang bị hoặc kho. Clip/pose mỹ thuật cũ được giữ; không có luật đánh, tính sát thương hoặc cooldown.
- Mẫu riêng `AuthoredHeroView`/`AuthoredHeroPreview` dùng nguồn Krita/PSD, SpriteSkin và clip Idle/Run đã lưu; `SetOutfit("Ivory" | "Navy")` chỉ đổi hình. Scene `HeroProductionTrial` chưa thay player chính. [Nguồn, cách chỉnh và bằng chứng](../ArtDirection/HeroProduction2D/README.md).
- Các component môi trường hiện có tiếp tục giữ mesh nền/đường, đồ vật độc lập, bóng đổ, ánh sáng và ambient. `Editor/ArtPreviewBuilder` tạo scene/build bằng Unity API. Các builder mỹ thuật còn lại tiếp tục phục vụ nhập và chỉnh artwork.

Không có lớp profile, campaign, event bus gameplay, spatial grid cho actor, persistence hoặc transport trong bản hiện tại. Chỉ thêm lại hệ game khi có mốc thiết kế cụ thể; không giữ implementation chưa dùng như bằng chứng rằng game đã sẵn sàng cho hàng trăm người chơi.

## Dữ liệu mỹ thuật

`MeadowMainMap.prefab` giữ map chính gồm nền texture lặp, đường mesh chỉnh điểm, đá lát, 21 đồ vật và contour chân. Cây, đá và cụm hồ vẫn là các đối tượng độc lập. Nguồn PNG, shader, texture, material, bố trí prefab và các định nghĩa trang phục được giữ.

`Content/Characters/` chứa definition của người minh họa và mẫu tách lớp; GUID giữ khi chuyển từ `Content/Combat`. Nhân vật chính sử dụng artwork minh họa hiện hành: có mặt trước/lưng/góc nghiêng, phản chiếu trái phải và ánh xạ tám hướng di chuyển; chưa phải tám góc artwork độc lập. Mẫu `HeroModularTrial` hiện chỉ có artwork mặt trước/phản chiếu, chưa thể thay nhân vật nhiều hướng.

Scene mỹ thuật còn lại: `ArtPreview`, `HeroModularTrial`, `CartoonArtTrial`. Build mặc định chỉ chứa `ArtPreview`; hai scene khác là công cụ duyệt art riêng. `HeroMotion` và các sprite clip lịch sử là dữ liệu animation, không sở hữu gameplay.

## Kiểm tra và chạy

Qua Unity CLI/Pipeline trên Editor đang mở đúng project:

```sh
unity recompile --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --json
unity command --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo eval 'return LinhThu.Editor.ArtPreviewBuilder.Prepare();' --json
unity command --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo eval 'return LinhThu.Editor.ArtPreviewChecks.Run();' --json
python3 serve_adventure.py
```

Build bằng menu Unity **Demo → Mỹ thuật → Build Web xem mẫu**. Gọi `Build()` trực tiếp qua `eval` có thể vượt timeout 5 giây của Pipeline dù build vẫn tiếp tục; timeout CLI không thay giới hạn đó. Lần nghiệm thu qua menu đạt 0 lỗi.

`Prepare` tạo lại riêng scene mẫu sau khi xác nhận scene hiện hành đã lưu; không dựng lại các prefab môi trường hoặc artwork. `Build` ghi `WebBuildV1`, cổng local giữ 8791 để dùng lại địa chỉ quen thuộc. WASD/mũi tên hoặc bấm sân để đi, R về điểm đầu. Kiểm tra focused nằm ở `ArtPreviewChecks`; kết quả mới được ghi trong `Verification/ArtOnly/`. Phải xem thêm bản chạy thật, đặc biệt đứng/chạy, đổi hướng, tiếp đất và đường ráp quần áo; compile không chứng minh thẩm mỹ.

## Khôi phục và bàn giao

`Recovery/ArtOnly-20261012/source-before.tgz` chứa 711 file source, meta, asset, scene, cấu hình và tài liệu trước thay đổi, gồm cả source chưa theo dõi bằng Git; SHA-256 đối chiếu trong `sha256.json`. `retired-source/` giữ nguyên những file được dời khỏi source đang dùng; `before-edit/` giữ một số bản trước sửa. Các file save cũ trên máy/trình duyệt không bị sửa hoặc xóa, nhưng bản art không tải chúng.

Để trở về gameplay cũ, dừng Editor, giải nén archive vào thư mục riêng rồi đối chiếu/copy theo manifest. Không dùng reset Git vì phần lớn source hiện đại chưa nằm trong commit hiện hành. Tài liệu mốc gameplay cũ dưới `Architecture/` được đánh dấu lịch sử. UI/UX và Mỹ thuật dùng các entry point mới ở trên; không nối lại `AdventureHost`, `CombatWorld` hoặc `ActorState`.
