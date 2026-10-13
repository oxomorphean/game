> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Mốc 03 — chỉ số và công pháp

Phạm vi: mở rộng phiên `PlayerCombatTrial`, dùng mỹ thuật hiện có, C# Domain thuần và dữ liệu trong RAM. Không có save/migration, cảnh giới, luyện cấp, kinh tế hoặc dung hợp. Quyết định sản phẩm thuộc [GAME_DESIGN.md](../GAME_DESIGN.md); các số dưới đây chỉ là cấu hình thử nghiệm.

## Kiến trúc và luật

`CombatInput → CombatCommand → CombatTrialSession → CombatWorld/TechniqueModule → ActorState → CombatWorldView + CombatHud`. Session tiếp tục ghép loot/kho như mốc 02. `CharacterStats` tính từ `ActorDefinition.BaseStats` bất biến và bảng `StatModifier` theo source; thay source là thay thế, tháo source chỉ bỏ nguồn đó. Modifier có thời hạn được loại khi hết thời gian mô phỏng; chỉ tính lại lúc thêm/thay/tháo/hết hạn. HP và linh lực hiện tại nằm riêng trên ActorState và luôn được kẹp về giới hạn sau tính toán; tăng giới hạn không tự lấp tài nguyên.

`TechniqueCatalog/TechniqueDefinition` giữ ID, nhóm, chi phí, cooldown và hiệu lực. `TechniqueState` của từng actor giữ tập đã học, một nền và một bộ pháp đang chọn, ID đang thi triển và cooldown theo ID. `TechniqueModule` sở hữu học/chọn/thi triển; các mutation của ActorState và TechniqueState là internal. Chưa nối trang bị cũ vào combat mới; modifier đã có nguồn và thời hạn để mở rộng sau.

Mỗi tick 0.02s cập nhật timer/hồi linh lực/tiêu hao duy trì trước khi xử lý lệnh mới; một lệnh hợp lệ được trừ chi phí và đặt cooldown một lần. Lệnh thất bại không trừ tài nguyên hoặc tạo/reset cooldown, nhưng thời gian mô phỏng và hồi/tiêu hao sẵn có vẫn chạy. Cooldown không dừng khi đổi lựa chọn. Đổi nền không hồi HP/linh lực; đổi bộ pháp hủy hiệu ứng cũ. Học/chọn/tháo/thi triển bị khóa khi gục, đang ra đòn hoặc đang lướt. Khinh Vân đang chạy có thể tắt bằng chính nút bộ pháp; thao tác tắt không cần chờ cooldown.

Gục hủy lướt/tăng tốc đang chạy, giữ công pháp nền và lựa chọn. Hồi sinh đầy HP/linh lực theo chỉ số đang có, xóa cooldown công pháp/đòn và giữ đã học/đã chọn; tiếp tục bảo hộ 2 giây như mốc 01. R hoặc tải lại trang tạo phiên mới. Học miễn phí và một ô cho mỗi nhóm chỉ là cách thử hiện hành, chưa chốt giới hạn sản phẩm.

## Cấu hình thử

Nguồn: `Assets/_Project/Content/Combat/CombatTrialConfig.asset`, Inspector `CombatTrialConfig`. Definition được chụp khi khởi tạo phiên; sửa asset rồi R/Play lại để áp dụng. Không sửa YAML. Builder validate definition và serialize các field mới qua Unity API; scene hiện có không bị tạo lại.

| Thành phần | Giá trị thử |
| --- | --- |
| Chỉ số gốc người chơi | 100 HP, 60 linh lực, công 20, thủ 2, tốc 3.3, hồi linh lực 3/s |
| Yêu Quy | 54 HP, công 12, thủ 2, tốc 1.65; không dùng linh lực |
| Đánh thường | Hệ số công kích 1.0; không hao linh lực; cooldown 0.55s; hình học/windup/recovery giữ mốc 01 |
| Hoành Tảo `sweeping-sword` | Hệ số 1.7; 20 linh lực; cooldown 4s; đã học khi bắt đầu phiên |
| Dưỡng Nguyên Quyết `duong-nguyen` | Nền: +40 linh lực tối đa và +2 hồi linh lực/s; source `foundation:duong-nguyen` |
| Khinh Vân Bộ `khinh-van` | +2.7 tốc độ; 5 linh lực khi bật; tiêu hao 8/s; cooldown 1s; source `movement:khinh-van` |
| Lôi Ảnh Bộ `loi-anh` | 20 linh lực; lướt 2.4 đơn vị trong 0.18s; cooldown 3s; hướng di chuyển nếu có, nếu đứng yên dùng hướng ngắm |
| Thiết Cốt | Giảm 20% sát thương sau phòng ngự; gắn sẵn, chưa có dung hợp |

Sát thương = `min(HP còn lại, max(minimumDamage, RoundAwayFromZero((công kích × hệ số đòn − phòng ngự) × (1 − giảm từ thiên phú))))`. `minimumDamage` mặc định 1 trong config. Ví dụ đánh thường lên Yêu Quy là 18, Hoành Tảo là 32; đòn cắn vào người có Thiết Cốt là 8. Modifier công/thủ được dùng khi impact xảy ra; mỗi mục tiêu chỉ nhận một impact của đòn như trước.

Linh lực vẫn hồi khi Khinh Vân hoạt động: tiêu hao ròng 5/s khi chưa có nền và 3/s với Dưỡng Nguyên. Không đủ trả lượt tiêu hao kế tiếp thì linh lực về 0 và dừng tăng tốc; một hành động tiêu hết linh lực cũng dừng hiệu ứng duy trì ngay. Không hồi máu tự động trong combat. Giới hạn kỹ thuật sau tính: HP 1–100000, linh lực 0–100000, công/thủ/hồi linh lực 0–10000, tốc độ 0–20. Đây là kiểm soát dữ liệu số, không phải giới hạn tiến trình nhân vật.

Lướt chạy nhiều bước nhỏ qua `CombatArena.Move`, giữ va chạm và giới hạn map; không dịch chuyển thẳng qua vật cản và không có bất tử. Lướt bị khóa điều khiển hướng trong 0.18s, không bẻ hướng giữa lượt. Animation/độ dịch chuyển đọc ActorState như đi bộ; chưa có asset lướt riêng.

## Bàn giao UI/UX và mỹ thuật

| Ý định / hiển thị | Interface |
| --- | --- |
| Di chuyển/ngắm/đánh/nhặt | `CombatCommand.Move/Aim/Request`; giữ Basic, Skill, Pickup như mốc 02 |
| Học, chọn, tháo | Request `Learn/Equip/Unequip` cùng `TechniqueId`; catalogue `World.Techniques.Catalog.All` |
| Dùng/tắt bộ pháp | Request `Movement`; Input `RequestMovement()`; Space và nút HUD cùng đường lệnh |
| Quyền thi triển | `World.Techniques.CanCast(id)` trả ActionResult; Domain kiểm tra lại khi xử lý lệnh |
| Chỉ số/tài nguyên | `World.Player.Definition.BaseStats`, `Player.Modifiers`, `Player.Stats`, `Player.Health/Spirit` |
| Học/chọn/hiệu lực/CD | `Player.Techniques.Knows(id)`, `Foundation/Movement/ActiveMovement`, `Dashing`, `Cooldown(id)` |
| Kết quả lệnh | `Session.Feedback/LastActionSucceeded`; kết quả loot riêng tiếp tục qua `LastPickup/PickupFeedback` |
| HUD | `CombatHud` giữ HP/talent/kho; partial `CombatTechniqueHud` giữ catalogue/nút bộ pháp và linh lực/chỉ số; không vẽ world |

C mở/đóng catalogue; nút mỗi dòng học miễn phí, sau đó chọn hoặc tháo. Bảng không pause mô phỏng. HUD nền trái trên: HP/linh lực/công/thủ/tốc/hồi; phải trên giữ Thiết Cốt và kho; phải dưới thêm bộ pháp cạnh Hoành Tảo/đánh thường. Nút bị khóa lấy từ trạng thái thật; lệnh bàn phím bị từ chối có phản hồi chữ. Chuột trên UI không bắn thêm đòn xuống thế giới. Font TMP hiện có, palette xanh đen/ngà hiện hành. Canvas dùng Expand theo khung 1280×720 để các panel không chồng lên nhau khi đổi tỷ lệ cửa sổ; trên tab quá hẹp chữ sẽ nhỏ, nên chơi bằng cửa sổ ngang. Đây là UI thử chức năng, chưa phải bố cục cuối hoặc giao diện mobile riêng.

Mỹ thuật giữ `CartoonCharacterRig`, shader, cây/đá/nước, `ModularMapLayout/MeadowMap` và scene/prefab bố cục. M03 chỉ đổi hai chỗ đọc HP tối đa trong CombatWorldView sang chỉ số đã tính. Editor dùng lần lượt theo bàn giao giữa hai phiên; không build trong khi phiên khác đang tác động Editor.

## Kiểm chứng và khôi phục

Dùng workflow sẵn có, chạy khi triển khai xong:

```sh
unity recompile --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --strict --format json
unity command eval 'return LinhThu.Editor.CombatTrialBuilder.Create();' --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --format json
unity command run_script --file AgentScripts/CombatTests.cs --entry CombatTests.Run --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --format json
# Sau khi scene Play và plugin nối lại:
python3 AgentScripts/combat_runtime_smoke.py
# Dừng Play trước build:
unity command run_script --file AgentScripts/CombatBuild.cs --entry CombatBuild.Run --timeout_ms 300000 --timeout 330 --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --format json
```

Domain tests mở rộng bộ mốc 01/02, kiểm tra độc lập chi phí/CD/đổi nguồn/giới hạn tài nguyên/thi triển/va chạm/gục/hồi sinh và hồi quy loot. Smoke dùng phím và pointer thật của Input System; `--techniques-only` chạy riêng UI/input M03 khi cần sửa phần đó. Không coi build thành công là bằng chứng tương tác web. Kết quả thực tế được ghi riêng trong `Verification/CombatM3/VERIFICATION.md` sau lần chạy.

Source trước mốc nằm trong `Recovery/techniques-m3-20261007-165046/source-before.tgz` (440 file, gồm source chưa Git theo dõi); manifest chứa hash và trạng thái Git/save trước thay đổi. Khôi phục chọn file cần từ archive, không reset cây Git đang có công việc phiên khác. Không đưa Library hoặc bản sao build vào checkpoint.
