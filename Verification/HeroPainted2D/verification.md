# Kiểm chứng nhân vật minh họa 2D — 09/10/2026

## Kết quả đã kiểm tra

- `PaintedHeroBaker.Run`: compile script không có diagnostic; xuất 416 frame, 0 frame rỗng, 0 frame chạm mép 2 px. Tổng bảy PNG: 2.908.286 byte. Báo cáo `bake.json`.
- `HeroDirectionBuilder.Painted`: compile không diagnostic; nhập 416 sprite và cập nhật definition đang dùng. Không mở hoặc ghi scene/prefab.
- `HeroDirectionChecks.Run`: 47/47 assertion đạt. Kiểm tra hướng đi/aim, animation attack/sweep/dash/hurt/down, gục/hồi sinh, phase, sorting, pivot và shadow.
- `HeroDirectionRuntime.Run`: 31/31 assertion đạt bằng Input System/Update thật trong campaign RAM tạm; trả lại campaign trước kiểm tra, không gọi Save. Ảnh `editor-front.png`, `editor-back.png`.
- `AdventureBuild.Run`: WebGL Succeeded, 0 lỗi, 2 cảnh báo về RuntimePipelineConfig và shader TextMeshPro. Báo cáo `build.json`. Không dùng kết quả build cũ.
- Unity Console sau build: 0 lỗi, compilationFailed=false. Không có thay đổi assembly runtime trong task; builder/baker compile khi run_script và build xác nhận đường chạy hiện hành.
- Bản `http://127.0.0.1:8791/` đã tải lại và vào bằng “Tiếp tục khám phá”. Chơi ở viewport desktop 1280 × 720: đi xuống thấy mặt trước, đi lên thấy lưng, đi ngang dùng góc nghiêng; J ra đòn; K dùng Hoành Tảo có pose, vùng tác động, giảm linh lực và cooldown; Space Lôi Ảnh Bộ có thay đổi vị trí, pose và cooldown. Một lần K bấm trong recovery của J bị từ chối, lần sau thành công. Ảnh `web-front.png`, `web-back.png`, `web-attack.png`, `web-skill.png`, `web-dash.png`. `web-console.json` không có lỗi/cảnh báo trình duyệt trong lượt kiểm tra.
- SHA-256 của CultivationV1, PlayerCombatTrial, MeadowMap và MeadowMainMap vẫn trùng trước task: `protected-before.sha256`. Bóng và nền là phần đã có của phiên Mỹ thuật; không chỉnh trong task này.

## Giới hạn

Có ba ảnh nguồn và bốn hướng hình ảnh chính; trái là phản chiếu góc nghiêng phải, chéo dùng hình trước/lưng. Không tuyên bố tám góc vẽ độc lập. Mẫu dùng biến dạng mesh 2D để xuất frame, chưa có các mảnh cơ thể PNG riêng cho hệ thay trang phục. Mức đẹp cần chủ dự án duyệt trong bản chơi.

Không chạy lại toàn bộ Domain/capacity/save suite vì không đổi Domain, input, save hoặc map. Death/respawn đã kiểm tra qua HeroDirectionChecks; lượt WebGL này ở trại an toàn, không thử chết bởi quái hoặc chiến đấu nhiều người. Không ghi save mới, không commit/push/deploy. Viewport kiểm tra đã trả về mặc định sau chụp ảnh.

Nguồn và prompt: `ArtDirection/HeroPainted2D/`. Khôi phục liên kết trước task: `Recovery/HeroPainted2D-20261009/binding-before.tgz`.
