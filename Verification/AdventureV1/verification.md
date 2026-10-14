# Nghiệm thu V1 — 08/10/2026

V1 local/WebGL đã tích hợp và đạt phạm vi thử cơ chế, theo hướng MMORPG tự do khám phá. Boss là thử thách tùy chọn; ba vùng mở từ đầu; mở menu không dừng world. Đây chưa phải bản MMORPG có máy chủ hoặc phát hành public. Logic trực tiếp triển khai Domain/composition/persistence, tích hợp và sửa lỗi; Art và UI sở hữu phần trình bày chuyên môn.

## Cập nhật Art sau nghiệm thu Logic — 08/10/2026

Bản đang phục vụ tại 8791 đã được phiên Mỹ thuật cập nhật bóng bám chân, bỏ elip ở đá, nắng đông→tây và thanh thời gian 06:00–18:00 (240 giây). Logic đã đối chiếu hash các file Domain/Infrastructure/Adventure/AdventureUI có trong baseline: không thay đổi. Đã xem ảnh web trưa/chiều và đọc kết quả Art 23/23, compile 0 lỗi/0 cảnh báo, WebGL 0 lỗi/5 cảnh báo và browser không quan sát error/warn. Báo cáo mới tại ../DaylightGround; manifest bản đang chạy là ../DaylightGround/current-build-files.json.

Phần kết quả build và mẫu 104.34 FPS dưới đây thuộc bản Logic trước cập nhật bóng, không đại diện phép đo hiệu năng bản Art mới. Kết quả build mới đọc từ ../DaylightGround/build.json. Bản khôi phục trước sửa nằm ở Recovery/daylight-ground-before-20261008; các phép thử combat/save không chạy lại vì source gameplay giữ nguyên.

## Kết quả theo phạm vi

| Phần | Bằng chứng |
| --- | --- |
| Stats, công pháp, cost/CD, damage, AI, gục/hồi sinh, drop/pickup | `combat-regression.json`: 72/72; `domain-tests.json`: 47/47 V1/save/world. Hai ca mới kiểm đòn lao trúng gần/xa đúng một lần và endpoint cố định có thể né. |
| Học, tu luyện, hai lần đột phá, gear/craft/trade, hai thiên phú, boss và ấn | `input-route.json`: 22/22 assertion từ lượt input thật trong Editor, không sửa state. Kịch bản QA này không là tuyến bắt buộc của sản phẩm. |
| Save/Continue và dữ liệu cũ | Roundtrip, invalid/checksum, backup và full bag trong domain tests; nút lưu → reload → Continue trên browser giữ kho/28 tệ; học/chọn pháp rồi reload giữ lựa chọn. `preservation.json`: bốn save prototype không đổi. |
| Mỹ thuật và UI | `Art/hero-texture-art-checks.json`: 17/17, mipmap/Trilinear giảm alias hình nhỏ. `ModernUI/verification.json`: 37/37 ở mỗi viewport 1280×720, 960×720. Main kiểm phím/pointer/cuộn web thật tại `web-input.json` và ảnh kèm theo. |
| Mở rộng vùng/room | 600 map metadata (200× diện tích ba mẫu), cache bounded, state/ground khi evict, 300 actor tổng hợp trên 20 room, kho riêng/nhặt cạnh tranh/replay/cap trong AdventureTests. `map-authoring.json`: 2/2 prefab Resources thật, ID 901, kích thước/collision riêng. |
| Compile và build | `compile-final.json`: up_to_date, 0 lỗi/0 cảnh báo compile. `build.json`: Succeeded, 0 lỗi, 4 cảnh báo, 110016632 bytes, 71.74 giây. |

Lượt Editor đi qua New Game → khám phá/ba loài quái → nhặt → học Dưỡng Nguyên/Khinh Vân → Luyện Khí → rèn kiếm/mua bán/dung hợp Quy giáp → Lôi Cốc → học Lôi Ảnh/Trúc Cơ → đổi Lôi tức mất 20 tệ → boss/ấn → Save/Continue. Input đi qua Input System và pointer uGUI. Các kiểm tra fixture/domain không được dùng thay cho bằng chứng chơi tuyến này.

WebGL đã kiểm mới/tiếp tục, WASD, F chuyển vùng từ cấp đầu, K và nút Hoành Tảo (linh lực/cooldown thật), E nhặt, B/C/Esc, học và chọn công pháp bằng pointer, cuộn danh sách ở 960×720, F5 lưu không refresh, gục/hồi sinh và về trại. Toàn tuyến boss được kiểm trong Editor; không tuyên bố đã lặp lại cả tuyến trên web. Main phát hiện Lôi Lang lao vượt mục tiêu đứng gần trên bản web đầu, sửa quãng lao theo vị trí ngắm lúc windup và cập nhật telegraph, thêm regression, chạy lại domain/combat rồi build bản cuối.

## Bản build và hiệu năng

Bản chơi: http://127.0.0.1:8791/ ; thư mục `WebBuildV1`. Build GUID `6260690eda8d4334b6c7f16ef9be5eb7`. Chạy lại bằng `python3 serve_adventure.py` từ thư mục project. Build bằng `AdventureBuilder.BuildWeb()` qua Unity Pipeline/CLI; wrapper `AgentScripts/AdventureBuild.cs`. Server chỉ bind 127.0.0.1, không public.

`web-performance.json` và raw `v1-web-performance-final.json`: Mac mini M4/16GB, Chromium 154 trong Codex, Unity 6000.6.4f1, viewport 1280×720/canvas 2560×1440. Mẫu 20.002 giây tại Lôi Cốc, người + ba lang + boss, có di chuyển/thi triển/nhận sát thương/hồi sinh: 2087 frame, 104.34 FPS trung bình, median 10 ms, p95 12 ms, max 26 ms. Unity Editor cũng mở. Đây là mẫu ngắn của cảnh nặng nhất V1 hiện tại, không chứng minh thiết bị yếu hoặc 100–300 kết nối. `SystemInfo.memoryMB` do WebGL trả không phải dung lượng RAM vật lý. Mẫu Editor cũ 55.8 FPS/p95 29.69 ms được giữ riêng trong `editor-performance.json`.

Console browser bản cuối không quan sát error; có một cảnh báo Unity về `JS_FileSystem_Sync()` bị deprecate. Lưu/khôi phục đã kiểm thực tế; chưa đổi cơ chế persistence chỉ để loại cảnh báo. Bốn warning build gồm Pipeline không bật trong Player (công cụ chỉ dùng Editor) và ba thông báo TextMeshPro/IL2CPP tách phương thức lớn thành file C++; không phải lỗi gameplay. Raw ở `v1-web-console-final.json`, `build.json`.

## Bảo toàn và bàn giao

Checkpoint source có cả file chưa theo dõi Git: `Recovery/v1-before-campaign-20261007`, `Recovery/openworld-before-20261007`; save V1 trước đổi hướng nằm ở checkpoint thứ hai. Riêng sửa import art và đòn lao có checkpoint `Recovery/hero-rig-before-20261007`, `Recovery/wolf-rush-before-20261008`. Khôi phục chọn file, không reset toàn bộ checkout vì các phiên dùng chung. Chưa commit/push/deploy.

`preservation.json` đối chiếu 115 source được bảo vệ: scene/prefab, PNG và legacy gameplay giữ nguyên; tám metadata import art thay đổi có chủ đích (sáu Environment tăng max texture 512→2048; hero/huyen-quy bật mipmap/Trilinear). Bốn save prototype giữ hash cũ. `source-sha256.json` ghi nguồn tích hợp; `git diff --check` đạt. Mỹ thuật bàn giao tại Architecture/V1_ART_HANDOFF.md; review ảnh bản cuối xác nhận hình/HP, còn endpoint telegraph được đối chiếu source vì ảnh chụp sau impact. UI bàn giao tại Architecture/V1_UI_HANDOFF.md. Main đã tự kiểm browser cuối; UI sau đó hoàn tất review độc lập ở 1280×720 và 960×720: bảy bảng/menu, Continue, B/C/T/Esc và wheel, không thấy blocker trong phạm vi này. Báo cáo bổ sung ở ReleaseUI/review.md. Màu Continue disabled là P3, patch mới ở staging và chưa compile/render nên chưa tích hợp.

## Giới hạn có chủ đích

- Chưa có transport, server/auth, world admission, room handoff, profile/room persistence bền vững hoặc chống sửa client. Domain 300 actor không là benchmark CCU. Trước online phải hoàn thành các cổng ở Architecture/ONLINE_ARCHITECTURE.md.
- Map metadata và tải layout riêng đã có; chưa author 200× artwork. Resources chuyển vùng đồng bộ, chưa streaming nội dung qua mạng hoặc seamless chunk trong một vùng. Không phóng một prefab/texture lên 200×. Bài import fixture không chứng minh chuyển hàng trăm map mỹ thuật thật trong WebGL.
- Ba vùng V1 dùng bố cục nền hiện có với decor/tint riêng; quái mesh là art mẫu. Chưa kiểm thiết bị yếu/mobile/touch hoặc phiên chạy dài. Thư mục build thực có 18 file, tổng 66442568 bytes (~66.4 MB); BuildReport báo 110016632 bytes theo cách tổng hợp nội bộ. Build phục vụ local chưa cấu hình nén/CDN. Manifest file nằm ở build-files.json.
- Cân bằng/cảnh giới/ô tinh hạch/phí là luật thử theo ủy quyền. Save local schema 1 không là nguồn tài sản online; checkpoint boss chỉ áp dụng boss mẫu của V1.

## Cách chơi

WASD di chuyển; chuột ngắm; J/chuột trái đánh, E nhặt; K Hoành Tảo; Space bộ pháp; F gần NPC/cổng; B hành trang; C công pháp; T tu luyện; Esc đóng bảng/menu; F5 lưu. Nhân vật kiểm tra web hiện ở trại, đã học các pháp bằng tài nguyên thu được; chọn “Tạo nhân vật mới” nếu muốn bắt đầu từ đầu (có xác nhận thay save V1). Khoảng thời gian thế giới vẫn chạy khi mở bảng; về trại trước khi quản lý đồ nếu cần an toàn.

## Ghi nhận sau mốc

Nền C# thuần và adapter module cũ cho phép giữ cùng luật ở test, Editor và WebGL. Input thật phát hiện lỗi đòn lao mà fixture cũ chỉ kiểm va chạm chưa bắt được; nay đã có ca gần/xa và né windup. Hiện tượng hình nhỏ lốm đốm do minification được sửa bằng import, không thay rig. Tiếp tục giữ test theo hành vi, phân biệt benchmark domain với mạng thật và bàn giao một chủ sửa file/Editor tại một thời điểm.
