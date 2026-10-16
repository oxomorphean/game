# Spatial world và vòng lặp thu phục — 15/10/2026

## Phạm vi và kế hoạch

Chỉ thị: goal-objective.md của phiên 01a13e99-bf22-7a53-948d-199d04f63b63. Vai trò: Logic, tích hợp map và HUD theo yêu cầu A → B → C → D. Tiếp tục trên working tree có sẵn; giữ các sửa grounding, combat, save và HUD trước task.

Hiện tại: MainGame → GameBootstrapper → WorldCommand/WorldState → Domain events → views. Domain thuần C#, một Player, truy vấn toàn actor/node/obstacle, một MeadowMainMap. Combat đã có windup/cooldown, Dash, roster, EXP và breakthrough; HUD chưa có nút breakthrough. Save schema 2 chỉ chứa một player.

1. **A:** ChunkCoord 16 world units, SpatialChunkGrid (actor, node, footprint), cập nhật tức thì khi vị trí đổi; truy vấn broad phase theo ô rồi exact shape/LOS. Players theo EntityId, LocalPlayer/Player alias; mọi dịch vụ định tuyến theo owner. Save schema 3 lưu tất cả players và trạng thái world, rebuild index từ dữ liệu đã validate.
2. **B:** WorldChunkStreamer bật/tắt presentation theo vùng camera + ActiveChunkRadius=1; geometry và simulation độc lập visibility. Mở MainGame thành bốn vùng kề nhau bằng prefab/config hiện có, giữ footprint và mật độ nền.
3. **C:** WildBeastAiController với Patrol/Chase/WindupCast/ExecuteAttack/LeashReturn; hướng/điểm cast khóa từ lúc windup. Thu phục T khi HP ≤30%, còn sống, đúng tầm/LOS; chuyển vào roster một lần. Kill thưởng cho người chơi gây đòn kết liễu hoặc chủ companion; respawn theo clock Domain. Telegraph chỉ đọc state và DeliverySpec.
4. **D:** Giữ năm ô cooldown hiện có; thêm EXP/threshold, B breakthrough có trạng thái sẵn sàng; I bật/tắt túi và danh sách thú. Button/keyboard cùng gửi command.

## Invariant và rủi ro

- Domain không Unity/input/I/O. View không thay HP, loot, AI hay quyền sở hữu. Một command chỉ sửa đúng PlayerState đã xác định; boundary ExecuteFor kiểm actor thuộc người gửi. Đây là nền multi-player local, chưa có xác thực mạng/transport/server/DB.
- Grid là chỉ mục, không là source of truth. Footprint lớn đăng ký mọi ô giao nhau; fishing dùng bounds bờ hồ. Thứ tự kết quả xác định theo EntityId. Chi phí truy vấn phụ thuộc số ô và số candidate, không tuyên bố O(1) cho mọi mật độ/thế giới.
- Visibility không tháo collision hoặc dừng AI/respawn. Streamer không tạo lại world/roster hoặc nhân đôi view khi qua biên.
- Thu phục/kill không phát thưởng hai lần. Túi đầy giữ thưởng chờ trong Domain để nhận lại; không làm rơi mất đồ. Taming và death loại trừ nhau.
- Save dựng graph riêng rồi validate trước publish; schema mới/path v3 riêng, không migration/đọc/ghi đè save cũ. RNG/counter/owner/AI/respawn/pending reward đều phải nhất quán.
- Giữ HeroBase/rig/outfit/scale, meta/GUID cũ, style và scene MainGame. Không Dashboard, Spring Bone, netcode, package mới, WebGL, commit/push.

Tuning thử (chưa là cân bằng phát hành): 16 units/chunk; aggro 5, leash 8, tuần tra 2; windup 0.65s; tame range 2; respawn 15s; EXP/loot cấu hình trên SO. Yêu cầu mục C và test dùng 30% nên ưu tiên ngưỡng này thay mô tả mở đầu <25%. Thu phục thành công chắc chắn trong bản này; không áp luật Huyết Khế/xác suất của prototype lịch sử.

## Kiểm chứng và điều kiện hoàn thành

- SpatialChunkGridTests: tọa độ âm/biên, đăng ký/di chuyển/xóa, footprint xuyên biên, truy vấn so brute force, node hồ và collision không bỏ sót.
- Multi-player: lệnh owner A/B, wrong owner, inventory/equipment/roster/cultivation độc lập; save nhiều chunks/players, malformed save không sửa live dependencies.
- WildBeastAndTamingTests: FSM, hướng windup khóa, Dash ra khỏi shape tránh damage, leash/hồi máu, HP đúng ngưỡng/chết/xa/LOS, duplicate tame/kill, loot đầy túi, EXP đúng owner và respawn/save giữa timer.
- Client integration: bốn vùng/footprint, streaming bật/tắt rồi bật lại không mất state, telegraph khớp shape, HUD/keys đúng command và toggle không chặn click khi ẩn.
- Cuối mốc: Unity compile 0 lỗi, toàn bộ EditMode pass; kiểm diff/status/meta mồ côi/thư mục Assets rỗng. Ghi kết quả thực tế ở đây. Chủ dự án Play nghiệm thu mỹ thuật/cảm giác; compile không thay nghiệm thu thị giác hoặc benchmark FPS/CCU.

Rollback: bản khôi phục ngoài repo trong `~/dev/nguthu-backups/core-foundation-backup/*-spatial-taming/`, có archive và SHA-256 đối chiếu từng file, baseline diff/index. Chỉ khôi phục file thuộc task nếu cần; không reset toàn repo. Save v2 giữ nguyên.

## Bàn giao implementation

- **A:** `ChunkCoord`, `SpatialChunkGrid`, index được cập nhật từ Transform2D; Players/ExecuteFor định tuyến owner ở command và dịch vụ. Node bờ hồ lớn/footprint nhiều ô không bị bỏ sót. Realm/equipment/roster/bond/life activities của từng player được kiểm độc lập.
- **B:** MeadowMainMap giữ 75 prop ban đầu, thêm 31 prop từ prefab hiện có (106 tổng cộng). Bốn vùng nội dung tương ứng (0,0), (1,0), (0,1), (1,1); biên gameplay [-16,32] hai trục, nền có đệm trình bày như trước. Camera hỗ trợ tâm map khác 0 và giữ 1% lề actor ở màn hình rộng. Streamer chỉ bật/tắt prop theo camera + một vòng chunk; disable/re-enable phục hồi đúng active set.
- **C:** Ba bãi Huyền Quy tại (18,3), (7,18), (23,23), cấu hình mới `wild-huyen-quy`, `wild-huyen-quy-actor`, `ho-son-tram`, prefab `WildHuyenQuy`. Art dùng lại Huyền Quy, thêm overhead HP/Suy yếu. AI và telegraph cone đã nối vào MainGame; presenter còn hỗ trợ circle/line theo DeliverySpec. T thu phục, thưởng kết liễu 25 EXP + 2 Linh thảo; hồi sinh 15s. Loot đầy túi giữ tại spawn site và hoãn hồi sinh đến khi nhận được, giới hạn một phần thưởng chờ/site.
- **D:** EXP hiển thị CurrentExp/NextThreshold, nút B đọc CanBreakthrough; I ẩn cả inventory và roster, không để hit target UI khi ẩn. Dòng linh thú chọn và gọi/thu hồi qua Domain; R thao tác linh thú đang chọn. Giữ năm ô cooldown và các phím cũ. T/B/I có nút trên màn hình.
- Save v3/path riêng giữ Players, bounds, AI/cast/respawn/pending reward; v1/v2 không migration. Bootstrap spawn node/actor trước bãi quái để kiểm footprint đầy đủ và giữ thứ tự entity khởi tạo cũ. View sinh động nằm dưới bootstrap của đúng scene, không rò sang scene đang mở khi chạy preview tests.

Bàn giao cho phiên mỹ thuật: scene MainGame và MeadowMainMap có bố cục mở rộng, camera size 5.6, bóng chân của các prop mới đã hiệu chỉnh theo tỷ lệ; rig/scale/clip HeroBase và mọi meta cũ giữ nguyên. Chủ dự án bấm Play để nghiệm thu mật độ, cảnh báo đòn, HUD và cảm giác né. Chưa có benchmark FPS/CCU hoặc nghiệm thu mỹ thuật bằng mắt trong mốc này.

Retrospective: NUnit toàn bộ phát hiện offset bóng khi scale prefab và giả định tâm map bằng 0 trong fixture cũ. Các test motor lặp hàng nghìn tick nay bỏ spawn chiến đấu trong fixture để không chết giữa phép đo; test săn thú riêng giữ AI thật. Preview scene không dispatch lifecycle thường như Play, nên test disable gọi đúng callback bằng reflection. Không thêm framework/Builder/QA script hoặc quy tắc AGENTS.

## Gate cuối và bằng chứng

- Unity `6000.6.4f1`: recompile_status completed, failed=false, compilationFailed=false, errors=[] (**0 compile errors**).
- Full EditMode: **254/254 passed**, **162 Domain + 92 Client**, **0 failed/skipped/inconclusive**, 73.31 giây ở run cuối. 26 ca mới so với 228 ca trước task; không xóa test cũ. SpatialChunkGridTests (10), WildBeastAndTamingTests (9), SpatialWorldPresentationTests (7) bổ sung cả ca biên/negative/round-trip.
- A: randomized grid so global scan, node/bờ hồ xuyên ô, collision và depleted mask; lệnh owner và modifier/life/roster độc lập; save nhiều người/chunk.
- B: bốn vùng thực sự có prop, player/companion qua biên không tạo world mới, hibernate vẫn giữ collision, disable/re-enable và quay lại phục hồi prop.
- C: FSM, Dash thoát cone/line/circle khóa aim, thu phục đúng 30%, sai owner/xa/chết/LOS/duplicate; kill/respawn, túi đầy và save giữa timer; periodic damage vẫn thưởng chủ companion đã thu hồi. MainGame có kiểm chứng cảnh báo và thu phục rồi triệu hồi qua nút HUD.
- D: EXP/ngưỡng, B sẵn sàng và breakthrough thật, I ẩn raycast targets; giữ các regression cooldown, HUD, camera 4:3/16:9/21:9, shadow, movement, fishing và inventory.
- Hygiene: **0 orphan meta, 0 missing meta, 0 thư mục Assets rỗng**; toàn bộ **416 meta trước task không đổi/không mất**. Diff source/docs không có whitespace error. Working tree/index được rà, giữ thay đổi có sẵn; không commit/push.
- Bằng chứng ở `/private/tmp/nguthu-core-foundation/20261015-spatial-taming/`: `compile-final.json`, `tests-final.json`, `scene-final.json`, `hygiene-final.json`. Backup bền vững: `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261015T081117Z-spatial-taming/`.

Nghiệm thu hình ảnh/cảm giác bằng Play và benchmark hiệu năng chưa thực hiện; kết quả trên chứng minh compile, logic, authoring và integration được assertion kiểm tra, không chứng nhận FPS hay vận hành MMO production.
