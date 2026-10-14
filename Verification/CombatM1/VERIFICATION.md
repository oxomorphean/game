# Combat người chơi — nghiệm thu mốc 01

Ngày kiểm tra: 07/10/2026. Project `/Users/nguyenhoangdacdat/dev/nguthu-unity-demo`; Unity 6000.6.4f1 trên Mac Apple Silicon. Phạm vi là phiên local trong scene `PlayerCombatTrial`, không có đồng hành hoặc ghi save.

## Kết quả đã kiểm chứng

| Yêu cầu | Bằng chứng |
| --- | --- |
| Tầm/góc/line-of-sight, windup, cooldown, một lần damage, skill radial | `domain-tests.json`: 25 đạt, 0 lỗi; gồm ranh giới trong/đúng/ngoài tầm, mục tiêu tránh đòn và vật cản |
| Thụ động không cộng dồn | So sánh damage 10 → 8, chạy nhiều tick qua hồi sinh; definition và HP từng actor độc lập |
| Di chuyển/vật cản | Domain kiểm tra tốc độ chéo, thân cây tròn, mép hồ mỏng, trượt cạnh và giới hạn map; input D thật trong Play Mode làm tọa độ thay đổi |
| Gameplay tích hợp | `runtime-input.json`: 10 đạt; AI truy đuổi, J trừ 18 HP, K trừ 32 HP, quái chết/hồi sinh, người chơi gục/hồi sinh |
| HUD phản ánh gameplay | HP/cooldown/thụ động khớp state; nút kỹ năng không gây thêm đòn vào world; feedback tăng khi có damage thật |
| Input và animation | `input-edge-and-pose.json`: 2 đạt; giữ J quá cooldown chỉ phát một đòn, chuột trái phát đòn; xương tay kiếm thay góc khi windup; pose gục và trở lại được kiểm tra trong runtime |
| Scene cũ | `legacy-rig.json`: hai SpriteSkin vẫn đủ sáu xương, màu/pose mặc định và di chuyển; không có host save |
| Compile/runtime | Recompile strict: 0 lỗi, 0 cảnh báo. `console.json`: 0 lỗi và 0 cảnh báo sau lần Play cuối |
| WebGL build | `build.json`: Succeeded, 0 lỗi, 1 cảnh báo. `build-messages.json`: Pipeline điều khiển Editor không được bật trong Player build |
| Phục vụ đúng bản build | `http.json`: index và bốn file loader/data/framework/wasm đều HTTP 200, hash khớp file trên đĩa, MIME wasm đúng |
| Bảo toàn dữ liệu | `preservation.json`: scene cũ, art/prefab, logic/save cũ khớp checkpoint; bốn save native khớp SHA-256; Git HEAD không đổi |

Ảnh `playable.png` lấy từ Game View của scene thực. Chuyển động, damage và hồi sinh được kiểm tra qua nhiều frame; ảnh đơn không được dùng làm bằng chứng game đang chạy.

## Cách chơi

Mở **http://127.0.0.1:8790/**. Server hiện chạy bằng `python3 serve_combat.py`; sau khi khởi động lại máy cần chạy lại lệnh này trong project. Bản web được mở bằng trình duyệt mặc định; yêu cầu mở tab Codex cũng đã được gửi và có thể chờ khi chat đang ẩn.

- WASD/phím mũi tên: di chuyển; chuột: hướng ngắm.
- J hoặc chuột trái: đánh thường; K hoặc nút Hoành Tảo: kỹ năng vòng tròn.
- R hoặc Thử lại: bắt đầu lại phiên. Gục tự hồi sinh sau 3 giây.
- Unity: `Demo → Combat → Mở bản combat người chơi`, rồi Play.

## Giới hạn bằng chứng

Kiểm tra tương tác và hình ảnh thực hiện trong Unity Editor. Bản WebGL đã build, phục vụ và mở, nhưng chưa tự động kiểm tra tương tác/render trong trình duyệt; HTTP 200 không chứng minh Wasm chạy thành công. Chưa đo FPS, bộ nhớ hoặc độ tương thích trên các trình duyệt/máy khác. Chưa có multiplayer hoặc benchmark sức chứa người chơi.

Animation dùng rig hai hướng hiện có, chưa phải bộ sprite tấn công nhiều hướng. AI tránh vật cản cục bộ, chưa có tìm đường cho mê cung. Các chỉ số chỉ là đề xuất cho phiên thử trong `GAME_DESIGN.md` và `CombatTrialConfig.asset`.

## Phạm vi và khôi phục

Combat mới nằm trong Domain C# thuần, tách definition khỏi state; host ghép input, world view và HUD. Không refactor toàn bộ combat/save của `GameSession` cũ. Không thêm dependency, sinh art, triển khai tinh hạch/dung hợp/tẩy luyện, commit, push hoặc deploy.

Checkpoint trước code: `Recovery/combat-m1-20261007-111859/`, gồm `project-before.tgz` (357 file, cả untracked), manifest/hash/Git status và bốn bản sao save native. Muốn quay lại chơi mẫu cũ chỉ cần mở scene cũ. Muốn phục hồi source, giải nén archive vào thư mục tạm rồi chép đúng file cần khôi phục; không reset worktree bẩn.

## Ghi nhận sau triển khai

Scene riêng và Domain thuần giúp kiểm tra combat mà không đụng save. Hai vấn đề tích hợp được phát hiện và sửa: reference config bị mất khi chuyển scene (nạp lại asset sau chuyển scene), và cạnh bấm phím bị mất khi công cụ mô phỏng cập nhật Input System sớm (giữ event vào hàng đợi lệnh). Lệnh build đầu dùng eval timeout 5 giây; build tiếp tục nhưng ghi lỗi điều khiển. Lần xác minh cuối dùng `CombatBuild.Run` với timeout phù hợp và đạt 0 lỗi. Probe rig cũ được sửa để đọc renderer của SpriteSkin thay vì renderer bóng chân. Các script và báo cáo trong project giúp tái lập các kiểm tra này.
