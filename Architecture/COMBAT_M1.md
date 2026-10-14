> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Combat người chơi — mốc 01

Tài liệu mốc combat nền. Scene hiện bổ sung [mốc tinh hạch 02](CRYSTAL_M2.md); output test/build hiện hành nằm trong `Verification/CombatM2/`.

Scene: `Assets/_Project/Content/Scenes/PlayerCombatTrial.unity`. Đây là phiên combat riêng dùng map/art CartoonV2, một người chơi và ba Yêu Quy cùng loài. Không có đồng hành, thu phục hoặc phần thưởng. Hướng sản phẩm và những giá trị chưa duyệt nằm trong [GAME_DESIGN.md](../GAME_DESIGN.md).

## Chơi thử

Trong Unity: **Demo → Combat → Mở bản combat người chơi**, rồi Play. Bản web ở `http://127.0.0.1:8790/` sau khi chạy `python3 serve_combat.py`.

WASD/phím mũi tên để di chuyển; chuột ngắm; J hoặc chuột trái đánh thường; K hoặc nút Hoành Tảo để dùng kỹ năng; R hoặc Thử lại để tạo lại phiên. Cây, đá và hồ chặn bước chân. Nét đỏ báo vùng quái chuẩn bị đánh: rời tầm trước lúc tác động để tránh đòn. HUD hiển thị HP, cooldown và Thiết Cốt đang giảm 20% sát thương nhận. Gục tự hồi sinh; không lưu tiến trình phiên thử.

## Luồng dữ liệu

`CombatInput → CombatTrialHost → CombatWorld (Domain) → CombatWorldView / CombatHud`. Host chạy bước 0.02s và giới hạn phần thời gian xử lý sau một frame chậm; không chạy bù hàng giây khi tab quay lại. Đây là bản local, chưa là server authority.

`CombatTrialConfig.asset` sở hữu các thông số thử trong Inspector. Nó tạo các `AttackDefinition`, `TalentDefinition`, `ActorDefinition`, `CombatRules` bất biến khi bắt đầu phiên. `ActorState` giữ riêng HP, cooldown, hướng khóa khi ra đòn, trạng thái đòn và hồi sinh. Chỉnh config áp dụng khi tạo lại phiên; không sửa ngầm definition của trận đang chạy.

`CombatWorld` sở hữu damage, cooldown, AI, death/respawn; không tham chiếu Unity, `GameSession` hoặc persistence. Một đòn resolve một lần sau windup; melee chọn một mục tiêu gần nhất trong vùng; Hoành Tảo áp dụng một lần cho từng mục tiêu hợp lệ. Tầm tính từ tâm chân, kiểm tra ở thời điểm impact cùng line-of-sight. Sát thương thụ động được tính từ raw damage và definition mỗi hit, không tăng/giảm chỉ số tích lũy theo frame. Gục hủy đòn còn chờ. Khi cùng tick có đòn chí mạng, đòn người chơi được xử lý trước đòn quái.

`CombatObstacleAuthoring` nằm trên instance đồ vật trong scene combat; collider chân là dữ liệu hình tròn/hộp, không sửa prefab map gốc hoặc suy từ alpha. Di chuyển chia bước ngắn và trượt theo cạnh. AI dùng truy đuổi/né cục bộ và quay về nơi sinh; không phải hệ tìm đường cho mê cung.

`CombatWorldView` dựng sprite, HP quái, nháy trúng đòn, tư thế gục và hiệu ứng từ state/event đã resolve. `CombatHud` chỉ trình bày thông tin và phát lệnh. `CartoonCharacterRig` được thêm pose combat tùy chọn; các scene cũ tiếp tục dùng pose mặc định. Không sinh asset mới, chưa có bộ sprite tấn công tám hướng.

## Kiểm tra và build

Thông qua plugin Unity/CLI đang nối Editor:

```sh
unity recompile --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --strict --format json
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --file AgentScripts/CombatTests.cs --entry CombatTests.Run --format json
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --file AgentScripts/CombatBuild.cs --entry CombatBuild.Run --timeout_ms 300000 --timeout 330 --format json
```

Sau khi Play scene mới, chạy `python3 AgentScripts/combat_runtime_smoke.py` để kiểm tra input/HP/HUD/chết/hồi sinh bằng plugin. Script tạo lại phiên RAM trước và sau kiểm tra. Tạo scene bằng `CombatTrialBuilder.Create()` qua Editor API. Build chỉ scene mới vào `WebBuildCombat/`; hai bản art/logic cũ dùng thư mục khác. Bằng chứng chạy thực tế nằm trong `Verification/CombatM1/`. Danh sách case được báo đúng theo lần chạy, không lấy kết quả domain cũ làm bằng chứng cho combat mới.

## Khôi phục

`Recovery/combat-m1-20261007-111859/` giữ archive source/asset/config gồm cả untracked và bốn file save native. Scene cũ, prefab map dùng chung và save không bị migration; phiên mới không có `PrototypeHost`, `LocalSaveStore` hoặc lời gọi PlayerPrefs. Chọn lại scene cũ để quay về prototype cũ. Đọc [kế hoạch](COMBAT_M1_PLAN.md) trước khi khôi phục file đè lên những chỉnh sửa tiếp theo.
