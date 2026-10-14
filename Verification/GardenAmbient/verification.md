# Hoa anh đào rơi và gợn hồ — 07/10/2026

Hiệu ứng nằm trong `GardenAmbientArt`, `GardenAmbientMesh`, `GardenAmbientGraphic`, được nối từ `ModularMapLayout` và `PrototypeMapArt`. World và Canvas dùng cùng tọa độ theo sprite; không sửa PNG, prefab, scene bố cục, collision, combat hoặc save. Hai cây có mỗi cây 20 cánh hoa nhỏ rơi lệch nhịp. Hồ có ba cụm cung gợn nở/tan; tâm và bán kính được giới hạn trong phần nước, tránh đá và lá sen.

Nhận xét thị giác về gốc cây: PNG có viền đậm kín quanh các rễ xòe, trên nền cỏ không có chi tiết che chân rễ, nên dễ có cảm giác ảnh dán. Thử bổ sung sáu cụm cỏ rất nhỏ che vài mép rễ và ít cánh hoa nằm trên mặt đất. Không thêm bóng elip; hiệu quả thẩm mỹ cần đối chiếu bằng hình trong game.

Checkpoint nguồn trước thay đổi nằm trong `checkpoint.txt`. Unity compile trả success, 0 lỗi/0 warning sau khi dùng API `GetEntityId` của Editor 6.6. Lượt Play Mode có 3 mesh: hai cây 246 vertex mỗi cây, hồ 480 vertex; thứ tự vẽ nằm ngay trên sprite chủ. Đối chiếu hai frame cho thấy 140 vertex cánh hoa mỗi cây và cả 480 vertex gợn hồ đã đổi vị trí. Map Canvas cũng dựng 3 hiệu ứng và tất cả đều không bắt raycast. Có 21 prop gốc và 0 bóng elip dưới cây; runtime console 0 lỗi/0 warning. `runtime.json` ghi bằng chứng. 28 PNG nguồn và 4 file save native giữ nguyên checksum, ghi trong `preservation.json`.

Các thay đổi mốc 03 của phiên Logic không thuộc kiểm chứng domain trong báo cáo này. Chỉ build lại preview combat hiện hành; không chạy lại hoặc sửa test gameplay.

WebGL combat build thành công, 0 lỗi/5 warning Pipeline/TextMeshPro, 73.13 giây; chi tiết trong `build.json`. Editor đã Stop tại `PlayerCombatTrial`, không thay đổi scene; lượt kiểm tra/build được trả lại phiên Logic. Preview dùng `http://127.0.0.1:8790/?art-motion=daylight`.

Browser đã tải lại bản build mới, console không có warning/error. Năm file HTML/loader/data/framework/wasm đều trả HTTP 200 và checksum khớp build; `web-assets.json` ghi kết quả. `web-preview.png` là ảnh browser sau tải lại, với viewport hiện có của ứng dụng.

![Preview web](/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/Verification/GardenAmbient/web-preview.png)

![Hiệu ứng chạy trong Unity](/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/Verification/GardenAmbient/preview.png)
