> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Tinh hạch — mốc 02

## Phạm vi và cấu hình thử

Scene `PlayerCombatTrial` bổ sung rơi và nhặt tinh hạch trong RAM. Không dung hợp, tẩy luyện, giá bán, đánh mất đồ khi gục hoặc migration save. Mốc này không sửa scene art/logic cũ, asset mỹ thuật hoặc module Inventory hiện có; thay đổi mỹ thuật từ phiên riêng được giữ lại.

Chỉnh `Assets/_Project/Content/Combat/CrystalLootConfig.asset` trong Inspector: danh mục loại tinh hạch (ID, tên, số lượng gộp mỗi ô), bảng rơi theo ID loài quái (loại tinh hạch, tỷ lệ phần trăm, số lượng), số ô và tầm nhặt. Giá trị ban đầu: Yêu Quy → `turtle-core`, **100% ×1**, kho **4 ô ×3 viên**, tầm **1.2** đơn vị. Tất cả là thông số thử, không phải giới hạn tinh hạch hay cân bằng đã duyệt. Đổi cấu hình áp dụng khi R/tạo lại phiên. Loại/ID tham chiếu sai, ID trùng và tỷ lệ ngoài 0–100 bị từ chối trước khi chơi.

Đồ dưới đất không tự hết hạn trong phiên; gục/hồi sinh không xóa kho hoặc đồ rơi. R hoặc tải lại trang tạo phiên trống. Không đọc/ghi save native, PlayerPrefs hay storage trình duyệt.

## Luồng và invariant

`CombatInput → CombatTrialSession.Step → CombatWorld.Step → CombatLoot.ResolveDeaths → CombatWorldView / CombatHud`.

`CombatWorld` tiếp tục sở hữu HP/đòn/AI; sự kiện `Died` chụp thêm `TargetGeneration`. `CombatLoot` dùng actor ID + generation để ghi nhận lần chết đã xét thưởng. Mỗi đời quái xét đúng một lần, kể cả kết quả không rơi; phát lại sự kiện không thử xác suất lại. Sau hồi sinh, generation mới được xét mới. Khi nhiều quái chết cùng một đòn, mỗi quái có đúng một lần xét. Không phát tinh hạch từ cái chết của người chơi.

`CrystalLootDefinition` bất biến giữ loại tinh hạch và bảng rơi. RNG dùng `IChanceSource`/`RandomChance` hiện có; test điều khiển nguồn xác suất. Tỷ lệ 0% không rơi, 100% luôn rơi; ở mức trung gian, rơi khi roll nhỏ hơn tỷ lệ cấu hình.

`CombatLoot` tái sử dụng nguyên trạng `InventoryModule.Add`, qua một `GameState.cells` và danh mục `GameContent` riêng chỉ trong RAM. Không khởi tạo `GameSession`, không dùng danh mục kinh tế mặc định, không thay nội dung save cũ. Kho lập phương án thêm đủ cả gói trước khi commit. Chỉ khi Add thành công mới xóa đúng ID đồ dưới đất. Kho đầy/thiếu chỗ giữ nguyên cả kho lẫn gói đồ, không nhặt một phần. Nhặt lặp ID đã xóa không cộng lần nữa. UI chỉ đọc slot/count và gửi lệnh, không sửa cells.

Nhặt kiểm tra người chơi còn sống, đã kết thúc đòn, đúng tầm và không có vật cản trên đường nối. Primary action J/chuột trái/nút HUD nhặt món gần nhất đủ tầm thay vì đánh; E chỉ yêu cầu nhặt. Khi kho đầy, primary vẫn báo lỗi nhặt và không kèm đòn đánh. K vẫn dùng kỹ năng để người chơi có thể chiến đấu khi đứng cạnh đồ. Cách chọn primary là đề xuất UX của bản thử.

## Thành phần và bàn giao cho các phiên

- Logic: thêm `Domain/Combat/{CrystalDefinitions,CombatLoot,CombatTrialSession}.cs`; `CombatEvent.TargetGeneration`; `CombatRequest.Pickup`. CombatWorld chỉ nhận None/Basic/Skill, Session chuyển Pickup thành lệnh tương tác trước khi gọi World.
- UI/UX: `CombatTrialHost.Session/Loot`, `Loot.Ground/Count/Slot/UsedSlots`, `Session.PickupFeedback` là nguồn đọc. `CombatHud.Refresh` nhận Session; nút chính đổi nhãn theo `NearestReachable`. Kho nhỏ bên phải và phản hồi dưới màn hình chỉ phục vụ mốc này, không thay dashboard quản trị.
- Mỹ thuật/map: biểu tượng tinh hạch tạm là diamond xanh ngọc/điểm sáng vàng ghép từ sprite `flat` có sẵn, có bóng chân và nhãn. Không sinh ảnh, không sửa art/prefab bố cục. Thay hình tại `CombatWorldView.RefreshLoot/CrystalFacet`; giữ ID/vị trí từ Loot, không xóa thưởng bằng animation.
- Authoring: `CombatTrialBuilder.Create()` gắn thêm `lootConfig` vào host qua Unity API; không ghi YAML bằng tay. Builder dùng scene hiện có, giữ bố cục. Build tiếp tục `WebBuildCombat/` ở cổng 8790; log mới vào `Verification/CombatM2/`.

## Kiểm tra tập trung và chơi

Chạy sau khi triển khai trọn mốc:

```sh
unity recompile --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --strict --format json
unity command eval --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo 'return LinhThu.Editor.CombatTrialBuilder.Create();' --caller plugin --skill unity-cli --format json
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --file AgentScripts/CombatTests.cs --entry CombatTests.Run --caller plugin --skill unity-cli --format json
# Khi scene đã Play và plugin nối lại sau reload:
python3 AgentScripts/combat_runtime_smoke.py --loot-only
# Dừng Play trước build:
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --file AgentScripts/CombatBuild.cs --entry CombatBuild.Run --timeout_ms 300000 --timeout 330 --caller plugin --skill unity-cli --format json
python3 serve_combat.py
```

Bộ test hiện có mở rộng cho drop trùng, xác suất, generation, nhiều quái, stack, kho đầy, giao dịch thiếu chỗ, pickup lặp, tầm/LOS, người gục và primary action. Smoke hiện có dùng input plugin thật để hạ quái, E nhặt và click HUD; ca kho đầy dùng config 1 ô ×1 chỉ trong Play Mode và khôi phục config sau kiểm tra. Xem kết quả chạy tại `Verification/CombatM2/`; build/HTTP không thay thế kiểm tra render và input trong trình duyệt.

Chơi ở http://127.0.0.1:8790/: WASD đi, chuột ngắm, J/chuột trái đánh hoặc nhặt gần, E nhặt, K kỹ năng, R tạo phiên mới. Sau khi build mới cần tải lại trang.

Kết quả đã chạy: **43/43 test domain, 15/15 ca input/HUD tinh hạch**; build WebGL thành công (0 lỗi, 5 cảnh báo công cụ/package). Báo cáo và giới hạn kiểm chứng: [VERIFICATION.md](../Verification/CombatM2/VERIFICATION.md).

## Khôi phục

`Recovery/crystal-m2-20261007-160329/source-before.tgz` giữ source/asset/config trước mốc (423 file gồm untracked); manifest có hash và Git status. Khôi phục bằng giải nén ra thư mục tạm và chọn file cần chép lại, không reset cây Git đang có thay đổi. Save không được migration; manifest có hash để đối chiếu dữ liệu native.
