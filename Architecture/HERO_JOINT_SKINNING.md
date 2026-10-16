# Nối khớp HeroBase — 16/10/2026

Chủ dự án yêu cầu triển khai thử mesh mềm kết hợp phần chồng ảnh sau khi thấy eo tách khi đánh; rà vai, khuỷu, hông, gối cho cả phôi và hai bộ đồ.

## Kế hoạch và ranh giới

- Hiện tại: 16 mảnh cơ thể xoay cứng theo Transform; torso/chest nghiêng riêng so với shorts/pelvis. Artwork đã có vùng chồng nhưng hai bên đường nối không biến dạng cùng nhau.
- Thay đổi: import mesh và trọng số bằng Sprite Editor API; thêm SpriteSkin trên các phần thân/tay/chân của HeroBase, giữ xương và clip. Eo chuyển trọng số giữa pelvis/chest; các mảnh cùng tay/chân dùng trọng số nhất quán qua khớp. Tận dụng vùng ảnh chồng hiện có và chỉnh trong Krita nếu kiểm pose cho thấy thiếu.
- Client: kiểm binding khi đổi đồ; VisualBounds và tàn ảnh phải đọc hình mesh ở pose hiện tại. Không thêm input hoặc luật vào view.
- Invariant: giữ GUID, sprite ID, canvas/pivot/PPU, tên và đường dẫn xương, controller/thời điểm animation; scale xương 1 và Player Visual 0.625; không sửa Domain/save/mặt/tóc hay thêm Spring Bone.
- Kiểm chứng: mesh có trọng số hợp lệ, bind pose khớp gốc; điểm chung ở eo/khớp không tách khi xoay; đổi basic/classic/jade giữa pose giữ xương/clip và SpriteSkin hợp lệ; bounds và tàn ảnh theo mesh biến dạng. Kiểm Game view idle/run/cast/Dash/thiền/câu cá, hai hướng flip, cả ba outfit. Compile 0 lỗi và bộ EditMode đạt. Hình ảnh vẫn là lượt thử để chủ dự án nghiệm thu.
- Rủi ro: trọng số làm bóp méo họa tiết tại góc gập lớn, sprite swap bị sai xương, CPU deformation chậm một frame so với nhãn/tàn ảnh; kiểm riêng các trường hợp này. Nếu asset thiếu mesh/bind không tương thích, từ chối trước khi đổi bất kỳ slot nào.
- Ngoài phạm vi: làm lại toàn bộ artwork, thêm động tác/luật chiến đấu, thay camera hay giao diện, cài phần mềm, commit/push.

## Khôi phục

Backup ngoài checkout tại `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261016-hero-joint-skinning/`: 859 file trong `source-before-joint-skinning.tar.gz`, SHA-256 `1768cc992d3515adc4ffeb7723a59907866336a9b671266e4cab9c8dddb2a153`; manifest từng file, Git index/status và snapshot world/camera/save trước khi thoát Play. Hoàn nguyên riêng file của lượt này theo manifest, không reset checkout hoặc ghi đè save.

## Kết quả

- HeroBase dùng 15 SpriteSkin (mọi phần cơ thể trừ đầu). 41 sprite phôi/classic/jade có mesh, bind pose và weight trong importer; tổng 4.916 đỉnh, nhiều nhất 234 đỉnh/sprite. Không sửa PNG: tái sử dụng phần chồng và đầu khớp đã vẽ trong Krita.
- Lưới toàn canvas bước 20 px, chỉ giữ ô có alpha và mép đệm. Mọi sprite dùng cùng hệ tọa độ và hàm trọng số theo vị trí; các điểm trùng ở đường nối có cùng chuyển động. Eo chuyển chest → pelvis từ y=625 đến 730 trên canvas nhìn từ trên xuống; phần thấp giữ theo hông khi ngực nghiêng. Vai/khuỷu/cổ tay/hông/gối/cổ chân chuyển trọng số dọc chuỗi xương, tối đa bốn ảnh hưởng/đỉnh.
- Đầu, tóc, mắt/mày/miệng vẫn đi theo head như trước. Toàn bộ thứ tự xương, controller, clip Idle/Run, pivot/canvas/PPU, GUID/subasset ID và Visual 0.625 được giữ. Không thêm xương runtime hay cài package.
- `ModularEquipmentRenderer` kiểm đủ mesh, trọng số, thứ tự/quan hệ xương và bind pose trước khi thay bất kỳ sprite nào. Đồ không có skin data bị từ chối nguyên tử. `SpritePoseGeometry` tính hình từ pose xương hiện tại cho bounds, bụi chân và tàn ảnh Dash, tránh dùng buffer SpriteSkin cập nhật muộn hơn các presenter; render vẫn do SpriteSkin của Unity thực hiện.
- Authoring một lần được lưu ngoài checkout ở thư mục backup, `Authoring/import-joint-mesh.cs.txt`; chỉnh tiếp mesh/weight bằng Skinning Editor hoặc chạy lại authoring sau khi backup và kiểm lại các invariant. Không thêm builder/check script vào project.

## Kiểm chứng 16/10/2026

- Compile Unity: không lỗi, `compilationFailed=false`. Full EditMode **292/292 đạt**, 0 failed/skipped, 20,91 giây. JSON/XML ở `/private/tmp/nguthu-core-foundation/20261016-joint-skinning/`.
- Bảy test mới: mọi outfit có trọng số chuẩn hóa/bind pose khớp ảnh ban đầu; 14 cặp đường nối không tách khi xoay cực hạn ở hai hướng; eo thấp đứng cùng hông khi chest nghiêng −12°; đổi đồ giữ clip time/controller/pose; đồ thiếu skin bị từ chối trước mutation; tàn ảnh lấy pose ngay và giữ nguyên sau khi xương di chuyển.
- Kiểm tra live 15 SpriteSkin ở Play: số đỉnh thực tế khớp geometry dùng cho bounds/ghost; sai số vị trí lớn nhất dưới `0.000001` đơn vị thế giới. Báo cáo trong `live-skin-comparison.json` ở thư mục backup.
- Xem Game view phóng camera tạm: cast classic và basic, pose thiền jade hướng trái, pose câu cá classic hướng phải, mẫu nhịp chạy jade. Chưa thấy khe hở trong các mẫu đã xem; vùng eo uốn thay vì tách. Pose thiền/câu cá lấy đúng cấu hình xương hiện hành, không thay luật hoạt động. Đây là kiểm tra mẫu hình ảnh, không khẳng định mọi frame/góc cực đoan hoặc mọi bộ đồ tương lai đều đạt mỹ thuật.
- Lúc chẩn đoán bằng eval, Editor có thể repaint trước khi tiến frame, làm mesh đổi sprite hiển thị tạm buffer cũ. Đã đối chiếu sau khi Play tiến frame thật và kiểm số đỉnh render. Một lần reload code giữa Play cũng làm buffer ambience mất trạng thái; lượt Play mới không dùng trạng thái QA đó. Không sửa code môi trường ngoài phạm vi.
- Đã khôi phục toàn bộ world từ snapshot và so sánh DTO bằng nhau; save trên đĩa không đổi, camera size 5.6, scene không dirty. Trả HUD/Animator/action pose và Game view về chế độ chơi.
- Quét Assets: không meta mồ côi, không thiếu meta, không thư mục tài nguyên rỗng. Không mất file baseline; Git index giữ nguyên, diff whitespace đạt. PNG, animation clip, config, MainGame và PlayerCultivator không bị sửa trong lượt nối khớp.

## Giới hạn và bàn giao

Mesh giữ đường nối nhưng có thể làm nén họa tiết ở góc gập lớn; cần chủ dự án nhận xét cảm giác chuyển động trực tiếp. Tóc/vạt áo chưa có Spring Bone. Đồ mới phải cùng bind và trọng số tương thích, không chỉ trùng pivot. Không đổi chiều dài chi hoặc thiết kế pose trong lượt này.

Điểm rút ra: test hình học phát hiện tách khớp tốt, nhưng chưa thay thế việc xem texture trong Game view. Khi kiểm sprite swap phải để SpriteSkin chạy hết frame; khi sửa code giữa Play cần khởi động lại trước kết luận về lỗi runtime.

API tham khảo: [Unity SpriteSkin 16](https://docs.unity3d.com/Packages/com.unity.2d.animation@16.0/manual/SpriteSkin.html), [Spine — testing weights](https://esotericsoftware.com/spine-weights#Testing-weights).
