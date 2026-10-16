# Ngắm chuột và tạm gỡ đá lát — 15/10/2026

Yêu cầu: kiểm bản Unity đang chạy, tạm gỡ đá lát, đạn đi đúng điểm click và không bị hồ/cây/đá chặn.

## Kết quả

- Editor đã mở đúng `nguthu-unity-demo/Assets/_Project/Content/Scenes/MainGame.unity`. Runtime dùng `HeroBase.controller` và `Run.anim` mới: curve ngực X tại đầu chu kỳ là 0.07732389, assembly không còn field `runLean` cũ. Không có bằng chứng đang xem nhầm project hoặc clip cũ. Dáng nghiêng thuộc Run, Idle vẫn giữ tư thế nghỉ.
- 17 GameObject đá lát trong `MeadowMainMap.prefab` đã inactive bằng PrefabUtility; cả hình, bóng và footprint biến mất. Giữ asset/vị trí vì đây là yêu cầu tạm gỡ. `WorldChunkStreamer` không đăng ký root đã inactive theo authoring, nên đi xa/quay về hoặc disable streamer không bật lại đá.
- Lỗi ngắm do VFX cộng 0.45 đơn vị Y nhưng hướng chuột đo từ chân, thêm việc snap vào tâm actor. `CastFromPointer` bù ngược cùng `ProjectileHeight` với VFX và bỏ snap cho Projectile; đường đạn nhìn thấy đi đúng điểm click. Click UI vẫn không cast; nút HUD và delivery khác giữ cách ngắm hiện hành.
- `WorldObstacle.BlocksProjectiles` và `ClearProjectileLine` tách vật cản đạn khỏi vật cản chân. Cảnh vật MainGame mặc định không chặn đạn; đi bộ/Dash, tương tác và delivery khác vẫn dùng footprint cũ. Vách chuyên chặn đạn có thể bật cờ. Targeted cast, preflight, thời điểm va chạm và bước bay dùng cùng chính sách. Không đổi tốc độ/tầm/sát thương đạn hoặc schema save.

## Kiểm chứng

- Test trước sửa tái hiện sai lệch đường VFX tới 0.445 đơn vị, snap tâm mục tiêu, cảnh vật tiêu hủy đạn và đá lát đang hiện.
- Compile: **0 errors, 0 warnings**, `compilationFailed=false`.
- NUnit EditMode: **277/277 passed**, 0 failed/skipped/inconclusive, 18.01 giây. Bao gồm ray qua con trỏ ở nhiều hướng/zoom, bắn trúng Mộc Nhân, cảnh vật chặn chân nhưng cho đạn qua, ba loại footprint có/không chặn đạn, restore giữ chính sách, và đá không được streaming bật lại. Test bố trí đá cũ được cập nhật theo trạng thái tạm ẩn; kiểm lối đi vẫn giữ.
- Native Unity Play: đã thấy đường đất không còn phiến đá. Runtime báo 0 đá lát active và 0 cảnh vật chặn projectile. Trạng thái world trước compile đã được khôi phục từ snapshot và so JSON bằng nhau, tick 1003; không ghi/đọc đè save trên đĩa.
- Kiểm repo: không đổi meta/GUID, 0 orphan/missing meta, 0 thư mục Assets rỗng; Git index có sẵn giữ nguyên. Diff kiểm theo baseline trước task, không ghi đè thay đổi trước đó. Không build WebGL, commit hoặc push.
- Evidence compile/NUnit/runtime: `/private/tmp/nguthu-core-foundation/20261015-pointer-and-path/`. Chủ dự án tiếp tục nghiệm thu cảm giác trực tiếp trong MainGame.

## Khôi phục

Backup bền vững: `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261015-pointer-and-path/`, archive 772 file nguồn gồm dữ liệu chưa commit; có manifest SHA-256, trạng thái/index ban đầu và snapshot runtime. SHA-256 archive: `9d06ccab93a2c817e847720ae130b3c1d4824bb031806d71e7e7617c9229e949`. Dùng manifest thay đổi để rollback từng file, không reset repo.

Điểm rút ra: test đường VFX cần tính cả offset render và camera; test streaming phải có prop bị tắt chủ động. Khi lưu prefab có ExecuteAlways, bảo toàn transform/màu đã author và loại mesh bóng HideAndDontSave khỏi dữ liệu cần đối chiếu.
