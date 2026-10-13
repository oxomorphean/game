> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Bản thử thế giới mở V1 — cấu hình và bàn giao Logic

V1 chạy riêng với `AdventureHost → AdventureCampaign → CombatTrialSession/CombatWorld`. Domain vẫn C# thuần. `AdventureCampaign` ghép InventoryModule, EquipmentModule, CraftingModule và phần NPC Trade của JourneyModule bằng một GameState riêng chỉ phục vụ dữ liệu kho/nghề/kinh tế; không khởi tạo GameSession, không chạy logic thú cũ. Tọa độ trại legacy (3,3) được adapter dịch từ vị trí NPC V1 (0,0). UI chỉ nhận AdventureSnapshot và gửi AdventureCommand/CombatRequest; world view chỉ đọc actor/loot/event.

## Luật thử và nội dung

`AdventureConfig.asset` sở hữu AdventureRules, tham chiếu combat config M03. Definition được tạo và chụp Rules khi bắt đầu/load; ID công pháp, vật phẩm và loài ổn định. Rank/modifier nguồn `cultivation`, `equipment`, `fusion` được thay thế khi lựa chọn đổi; không cộng chỉ số trong Update. HP/linh lực runtime độc lập và luôn kẹp vào giới hạn. Các thông số dưới đây là thử V1 theo ủy quyền, không phải cân bằng cuối.

- Nhập môn → Luyện Khí: 30 tu vi, 3 quái, 2 linh thảo. Luyện Khí → Trúc Cơ: 80 tu vi, 3 quái Lôi Cốc, 2 lôi sa. Hấp thu 1 linh thảo tăng 10 tu vi, chỉ ở trại; không tự luyện theo thời gian. Mỗi bậc +40 HP, +12 công, +3 thủ; đột phá hồi đầy.
- Dưỡng Nguyên học 1 linh thảo, Khinh Vân 2 linh thảo, Lôi Ảnh 2 lôi sa ở Đạo sư trong trại. Hoành Tảo có sẵn. Chưa học/không đủ cost/đang thi triển không thay state. Chi phí/CD/bộ pháp theo M03; đổi khu giữ lựa chọn và cooldown, hủy hiệu ứng đang duy trì. Về trại hoặc load hồi đầy HP/linh lực theo luật checkpoint an toàn.
- Ba map mẫu là dữ liệu trong AdventureMapCatalog, có cổng riêng theo ID. Thanh Vân có NPC, cổng đông tới Trúc Lâm và tây tới Lôi Cốc. Hai vùng chiến đấu đều mở từ đầu; Lôi Cốc có boss tùy chọn. Tối đa 5 actor ở vùng mẫu. Local cache tối đa residentMaps world; vùng bị evict giữ ground và state quái để tiếp tục khi quay lại. Đây là adapter local; room online không theo vòng đời camera của một người.
- Yêu Quy đuổi/cắn; Mộc Linh khóa vị trí người chơi khi bắt đầu niệm, nổ sau .95s (rời vòng để né); Lôi Lang khóa hướng và khoảng lao lúc bắt đầu, tối đa 2.2 đơn vị sau .7s, dừng trước mục tiêu gần nửa tầm đánh để không lao vượt người đứng yên; có thể né ngang và va chạm chặn; boss đánh gần và đập đất vòng 2.65 sau 1.25s. Quái thường hồi sinh sau 20s; boss chết không hồi sinh. Gục hồi sinh ở cửa khu, không mất đồ V1.
- Mỗi đời quái phát thưởng một lần theo actor ID/generation. Drop table hỗ trợ core và nguyên liệu; dùng chung kho 20 ô. Add kho thành công mới xóa drop. Yêu Quy rơi Quy hạch + 3 linh thảo + 1 quặng; Mộc Linh 3 thảo + 2 quặng; Lôi Lang Lôi hạch + 2 lôi sa + 3 thảo; boss Ấn Cổ Giáp. Core rate hiện 100% để thử toàn tuyến, thay được trong config. Mỗi quái +8 tệ; boss +100 tệ/hoàn thành một lần.
- Một ô dung hợp: Quy giáp giảm thêm 50% sát thương trong lúc đang ra đòn, tốc -0.3; Lôi tức phát xung 1.3× công trong vòng 2 ở cuối Lôi Ảnh, hồi 5s. Lần đầu dùng 1 core; đổi/tháo tốn 20 tệ, không hoàn core. Đổi không xóa pulse cooldown. Hai thiên phú bổ sung công pháp.
- Kiếm nhập môn +2 công có sẵn. Rèn Linh thiết kiếm +12 công tốn 3 quặng; Giáp Quy +5 thủ tốn 3 quặng +1 Quy hạch, 8s/mẻ. Không có phí tiền rèn riêng. Một mẻ Smithing cùng lúc, sản phẩm giữ đến khi nhặt vào kho được. NPC bán thảo/quặng/Hồi xuân đan; mua tài nguyên. Buy/gear/craft dùng giao dịch kho nguyên tử trước khi cập nhật tiền/ô. Đan +50 HP, hồi 5s; đang đầy không tiêu đan.

## Save V1 riêng

`AdventureSaveStore` sử dụng `cultivation-v1.json` trong persistentDataPath, backup `.previous`; WebGL key `linhthu.cultivation-v1` và `.previous` qua PlayerPrefs/IndexedDB. Schema 1 lưu tiến trình, coins, inventory, gear, craft job, core, công pháp/lựa chọn/CD, đan cooldown và tất cả đồ dưới đất. Không chứa save prototype, tài khoản hoặc dữ liệu mạng. Bản lưu là checkpoint local: load về trại an toàn, không tiếp tục giữa boss; quái thường được tạo lại thành đời mới. Boss đã chết không tái tạo; Ấn chưa nhặt được lưu dưới đất.

JSON có marker/checksum SHA-256, bắt buộc trường, kiểm giới hạn/ID/danh mục trước Restore. Native ghi temp rồi File.Replace/Move; chỉ bản primary hợp lệ mới đẩy thành backup. Primary hỏng sẽ thử previous và báo rõ. Checksum phát hiện file hỏng, không chống sửa save bởi người chơi. Không offline farming. Lưu thủ công F5/menu; tự lưu khi học/đột phá/dung hợp/nhận mẻ/thắng boss và khi rời ứng dụng nếu runtime phát lifecycle event. WebGL đã kiểm lưu bằng nút/F5 rồi reload và Continue: kho, tiền và công pháp khôi phục; không dựa duy nhất vào OnApplicationQuit. Xem Verification/AdventureV1/verification.md cho bằng chứng.

## Input, scene và build

Scene `CultivationV1.unity` tạo bằng AdventureBuilder qua Editor API; giữ PlayerCombatTrial/ThanDinhPrototype. WASD/arrow di chuyển, chuột ngắm, J/chuột đánh hoặc nhặt, E nhặt, K Hoành Tảo, Space bộ pháp, F NPC/cổng, B kho, C công pháp, T tu luyện, Esc menu, F5 lưu. Menu chỉ khóa input, combat/craft vẫn chạy, thông báo chết không pause hồi sinh. UI contract ở `Domain/Adventure/AdventureUi.cs`; row Buttons mang availability/reason từ Domain.

`AdventureBuilder.BuildWeb()` xuất `WebBuildV1`; `python3 serve_adventure.py` mở local 8791. Combat M03 ở 8790 được giữ riêng. F8 bắt đầu đo 20 giây gameplay bằng Time.unscaledDeltaTime, loại thời gian mở menu; kết quả `V1_PERF` vào console gồm device/renderer/resolution/build/actor/fps/p50/p95/max. Không suy kết quả máy khác từ phép đo Mac M4 này.

## Kiểm chứng

AgentScripts/AdventureTests.cs: kiểm domain và save bằng fixture cô lập; không phải bằng chứng đã chơi toàn tuyến. AgentScripts/CombatTests.cs: hồi quy M01–M03. UI tự kiểm bằng AdventureHudChecks chỉ chứng minh binding/layout state, không chứng minh pointer. Bằng chứng compile, Play input, WebGL, toàn tuyến và hiệu năng được lưu ở Verification/AdventureV1 và cập nhật khi thực sự chạy.

Nền mở rộng bản đồ và nhiều người: [WORLD_FOUNDATION.md](WORLD_FOUNDATION.md). Tên AdventureCampaign/completed trong save schema 1 giữ tương thích; completed chỉ đánh dấu boss đã hạ trong local, không kết thúc thế giới.
