> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Kế hoạch V1 — nền thế giới mở và vòng chơi thử

Chủ dự án ủy quyền Logic điều phối hai phiên chuyên môn và chọn luật thử V1 ngày 07/10/2026. **Hướng cập nhật: MMORPG tự do khám phá; tuyến sau chỉ là kịch bản QA.** Mục tiêu: bắt đầu → khám phá/đánh/nhặt → học công pháp → tu luyện/đột phá → đổi lối chơi → boss → lưu/tiếp tục. Local/WebGL desktop, không multiplayer/backend/thanh toán/public release. Không commit/push. Hướng và phạm vi chung ở [GAME_DESIGN.md](../GAME_DESIGN.md).

## Các mốc hữu hạn

| Mốc | Nội dung / nghiệm thu | Chủ trì, file sở hữu | Trạng thái, phụ thuộc |
| --- | --- | --- | --- |
| A. Nền M03 | 6 stats, modifier theo source, 4 công pháp, chi phí/CD/va chạm; 72 domain tests; runtime và WebGL | Logic: Domain/Combat, Client/Combat trừ file giao Art, config/build/test M03 | Đạt; 72/72 domain, 27/27 runtime + 10/10 UI sau sửa màu; WebGL và trình duyệt thật đã chạy |
| B. Tiến trình | Học qua NPC/đồ rơi, hấp thu tài nguyên, 3 bậc với 2 lần đột phá, gợi ý khám phá | Logic: Domain/Adventure, Client/AdventureHost, AdventureConfig | Đã kiểm chứng tuyến input tới hai lần đột phá; A |
| C. Nội dung | Thanh Vân trại + Trúc Lâm + Lôi Cốc; rùa/hoa yêu/lang/boss có cách né khác; mục tiêu cuối | Logic sở hữu domain spawn/AI/portal; Art sở hữu Client/AdventureArt, CombatWorldView, asset riêng V1 | Đạt: ba khu/ba loài/boss bằng input; Art 17/17, mipmap/Trilinear sửa alias nhân vật; bản web đã xem |
| D. Hỗ trợ | Kho dùng chung, gear/craft/NPC trade từ module cũ; 1 ô tinh hạch, 2 talent, đổi tốn phí; không mất/nhân đồ | Logic: adapter Domain/Adventure, điểm nối CombatLoot; giữ module cũ | Domain tests và input craft/gear/trade/fusion đạt |
| E. Save/UI | Save V1 riêng và backup; start/continue; HUD/bag/stats/techniques/cultivation/NPC; dashboard riêng | Logic persistence/host; UI sở hữu Client/AdventureUI và Architecture/V1_UI_HANDOFF.md | Đạt: save/continue qua UI và reload WebGL; UI 37/37 mỗi kích thước 1280×720, 960×720; pointer, phím và cuộn web thật |
| F. Nghiệm thu | Chơi toàn tuyến bằng input, save roundtrip, crash/console, tests/build; đo cảnh nặng trên M4 16GB; handoff cả 2 phiên | Logic tích hợp cuối + chuyên môn sửa lỗi trong phần được giao | Đạt cho V1 local: tuyến Editor 22/22; domain 47/47 + combat 72/72; import map 2/2; WebGL 0 lỗi, browser/input/save và mẫu hiệu năng đã ghi |

Sáu mốc V1 local đã nghiệm thu ngày 08/10/2026. Toàn tuyến Editor đã đi qua input thật, gồm boss/ấn/lưu/tiếp tục, không sửa gameplay state. Bản WebGL cuối đã mở, kiểm input/HUD/combat/lưu/tiếp tục và đo frame time; bằng chứng và giới hạn ở [báo cáo nghiệm thu](../Verification/AdventureV1/verification.md). Nền nhiều người chỉ đã kiểm domain tổng hợp; backend, mạng và streaming thực thuộc giai đoạn sau. Logic nguồn ở [V1_LOGIC.md](V1_LOGIC.md).

## Luật thử V1 theo ủy quyền

- Ba bậc: Nhập môn → Luyện Khí → Trúc Cơ. Hấp thu linh thảo kiếm từ khám phá/đánh quái để tăng tu vi; không tự tăng khi đứng chờ. Ngưỡng thử 30/80; mỗi linh thảo +10. Đột phá 1 cần 3 lần hạ quái và 2 linh thảo, đột phá 2 cần 3 lần hạ quái ở Lôi Cốc và 2 lôi sa. Cảnh giới tăng HP/công/thủ; cả Trúc Lâm và Lôi Cốc đều mở từ đầu, không khóa cổng hoặc boss theo cảnh giới.
- Bắt đầu với kiếm thường và Hoành Tảo; nền Dưỡng Nguyên học ở NPC trong trại sau khi có 1 linh thảo. Khinh Vân học bằng 2 linh thảo; Lôi Ảnh bằng 2 lôi sa. Giá và điều kiện là config; không khóa vĩnh viễn vào tutorial.
- Trúc Lâm: Yêu Quy đuổi/cắn và Mộc Linh đứng xa niệm vùng nổ có báo trước ở chân người chơi (rời vòng để né). Lôi Cốc: Lôi Lang lao nhanh theo đường báo trước, Cổ Giáp Vương phối hợp chém gần và đập đất vùng rộng. Tối đa 4 quái thường/boss cùng người chơi trong khu hiện hành (5 actor); quái thường hồi sinh tại spawn cố định.
- Ba khu dùng bố cục/asset hiện có; cổng hiện rõ. Chuyển khu qua tương tác gần cổng; vào trại hồi đầy và có NPC. Thua hồi sinh tại cửa khu, không mất vật phẩm V1. Đồ dưới đất và kho tách nhau; nhặt thành công mới xóa.
- Một ô dung hợp. Quy giáp: giảm sát thương khi đang thi triển kỹ năng (có đánh đổi tốc di chuyển nhỏ). Lôi tức: lần lướt tiếp theo sau cooldown để lại xung điện gây sát thương gần khi kết thúc lướt. Không thay vai trò công pháp. Lần đầu tốn 1 core, đổi/tháo tốn 20 linh tệ; không hoàn core; số/chi phí configurable.
- Kho 20 ô gộp đồ; gear kiếm/giáp. Một nghề rèn: kiếm thép (quặng), giáp quy (quặng + core); một loại dược hồi HP mua từ NPC. Module Inventory/Equipment/Crafting/Journey Trade cũ được tái sử dụng qua adapter state riêng.
- NPC trại bán linh thảo/quặng/dược, mua tài nguyên/core; không có giao thương giữa người chơi. Mọi thao tác chi/nhận đồ nguyên tử trước khi đổi tiến trình.
- Save `cultivation-v1` riêng, schema 1; native JSON ghi temp→replace + bản previous; WebGL lưu qua PlayerPrefs/IndexedDB Unity và checksum/backup hai slot. Chỉ lưu state V1; không load hoặc sửa save prototype. Save tại menu/nút và khi đạt mốc; load về trại an toàn, giữ tiến trình/kho/gear/công pháp/CD, không mang hiệu ứng đang chạy. Không cho load vào boss lưng chừng.
- Menu chức năng chỉ khóa input; combat và chế tạo vẫn chạy. Thao tác phát triển tại NPC cần đúng vị trí/điều kiện. Gợi ý khám phá không ép tuyến; hạ boss và nhận ấn là thử thách tùy chọn, thế giới tiếp tục hoạt động.

## Sở hữu file và lượt Editor

Logic quản lý GAME_DESIGN.md, V1_PLAN.md, Domain, persistence, host, scene V1/builder/tests. Art không sửa domain/host/UI/config chung; UI không sửa Domain/scene/prefab/CombatHud. Art được sửa CombatWorldView chỉ về trình bày, giữ Bind/Refresh/loot và phần HP theo Stats của M03. Mỗi phiên ghi interface/thay đổi/checks trong tài liệu handoff riêng. Không ai tự Play/Stop/build khi chưa nhận lượt. Scene `PlayerCombatTrial` và save cũ giữ nguyên; V1 tạo scene riêng bằng Editor API, giữ prefab map gốc.

## Cổng nghiệm thu

Domain kiểm tra cost/learn/level/breakthrough, kho đầy, gear/fusion spam, thưởng generation, craft/trade atomicity, boss reward một lần, save invalid/roundtrip/backup. Client kiểm tra start/continue, panel/input capture, buttons gọi domain, feedback. Chơi tuyến thật bằng thao tác người chơi; không dùng chỉnh state hoặc dashboard để chứng minh hoàn thành. Đo fps/frame-time cảnh tối đa actor trong build desktop WebGL và ghi thiết bị/resolution/build hash. Không kết luận hiệu năng cho thiết bị khác từ M4.

Giữ checkpoint source gồm untracked trước mỗi mốc save lớn. Không reset Git đang dirty; khôi phục chọn file từ archive. Có lỗi hoặc thiếu bằng chứng thì mốc vẫn chưa đạt.

## Điều chỉnh theo yêu cầu MMORPG thế giới mở

Yêu cầu trực tiếp mới nhất thay “đi hết tuyến/đích cuối” thành vòng chơi tự do. Boss và chuỗi Editor đã nghiệm thu là kịch bản kiểm thử, không là luật mở map. Tiếp tục sáu mốc hữu hạn trên, cùng mốc nền mở rộng có giới hạn: catalogue/map data + nhiều player trong combat room + inventory theo người + test bản đồ/bộ nhớ/nhặt đồng thời. Chưa thuê VPS, triển khai transport/backend hoặc công bố tải online.

Nghiệm thu bổ sung: thêm map có ID không liên tiếp/arena/cổng riêng không sửa Campaign; catalogue 600 vùng có tổng diện tích 200× ba vùng mẫu, chỉ giữ số world cấu hình; 300 actor tổng qua 20 room trong test C# với input/technique/AI/pickup độc lập; mở bảng không dừng world. Đây là test logic tổng hợp, không thay benchmark 300 kết nối. Giữ WebGL/build/runtime/UI và performance V1 là cổng nghiệm thu bắt buộc.
