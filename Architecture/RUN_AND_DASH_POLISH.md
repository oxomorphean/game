# Chạy nghiêng thân và nối Dash — 15/10/2026

Yêu cầu: chạy hơi nghiêng người về trước; giữ di chuyển + Space không bị khựng ở cuối lướt. Phạm vi gồm clip Run, presentation nghiêng người và chuyển tiếp vận tốc trong Domain; không đổi input, cooldown, thời lượng, collision, schema save hoặc artwork.

## Thay đổi

- `Run.anim`: ngực xoay −12° ±1.2° quanh hông, đầu bù nhẹ; tay gập và vung đối nhịp. Giữ nguyên đường chân IK, chiều cao hông, độ dài clip 0.8s, stride 0.6 rig unit/0.4s, `baseStrideSpeed=0.9375`, scale và GUID/controller. Sửa bằng `AnimationUtility`.
- `ActorAnimator`: bỏ `runLean` nghiêng toàn Visual khi chạy thường; chỉ giữ lean Dash. `HeroBase.prefab` bỏ field cũ qua `PrefabUtility`, giữ giá trị bóng trước task.
- `DashState`/`WorldState`: trước đây đường ease-out giảm tốc về 0 dù còn giữ input, rồi Walk tăng tốc lại. Nay tích phân `max(peak × remainingRatio², tốc độ chạy hiệu dụng × độ lớn input)`, giới hạn sàn bởi peak. Nhả input vẫn về 0; đổi hướng được dùng sau khi Dash kết thúc. Motor vẫn sở hữu kết quả trượt/chặn vật cản.
- Với tuning hiện tại (MoveSpeed 2.8, peak ×4, duration 0.2s), Dash đứng yên vẫn đi khoảng 0.747 đơn vị; Dash giữ chạy đi khoảng 0.933 đơn vị vì không phanh xuống 0 ở đoạn cuối. Đây là tuning của lượt sửa cảm giác điều khiển, chưa là cân bằng phát hành.

## Kiểm chứng

- Đã chạy test trước sửa: lỗi vận tốc cuối Dash bằng 0 và thân chưa nghiêng về trước được tái hiện. Một assertion mới yêu cầu dừng toàn bộ vận tốc trước tường đã được sửa để phản ánh luật trượt dọc tường hiện hành: cấm vận tốc xuyên tường, vẫn cho phép tiếp tuyến.
- Unity compile sau sửa: **0 errors, 0 warnings**, `compilationFailed=false`.
- EditMode: **265/265 passed**, 0 failed/skipped/inconclusive, 15.04 giây. Bao gồm chạy xuyên Dash ở 30/60/144 Hz, hướng mới sau Dash, nhả input, vật cản, save giữa Dash có/không giữ input; clip hai hướng giữ vai trước hông, bàn chân ngang, stride và gối đúng giới hạn.
- Đã xem pose chạy trong native MainGame Play ở camera phóng lớn. Đây là kiểm tra pose đã quan sát; cảm giác trọn chu kỳ/chơi liên tục vẫn do chủ dự án nghiệm thu trực tiếp. Không dùng compile để kết luận mỹ thuật đạt chuẩn.
- Kiểm cuối: `compiling=false`, `compilationFailed=false`; Console hiện tại 0 errors/0 warnings (buffer Pipeline giữ 21 lỗi lịch sử đã có trước task). MainGame đang Play, không pause, scene không dirty, camera 5.6. Quét Assets sau refresh: 0 orphan meta, 0 missing meta, 0 thư mục rỗng; không đổi meta/GUID. Diff so với backup chỉ có 11 file thuộc task; Git index trước task giữ nguyên. `git diff --check` đạt khi bỏ blank-at-eol của YAML Unity và tắt clean filter LFS trong lệnh đọc.
- Evidence compile/test: `/private/tmp/nguthu-core-foundation/20261015-run-dash-polish/`. Không build WebGL, commit hoặc push.

## Khôi phục và bàn giao

Backup ngoài checkout: `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261015-run-dash-polish/`. Archive `source-before-run-dash.tar.gz` chứa 785 file source, gồm thay đổi chưa commit, cùng manifest SHA-256 và index trước task. SHA-256 archive: `7234383a5cf79c4a027eace7b531ad7bb51e8d2c99a477d735d30b7e9a5ef327`.

Rollback chọn file từ manifest thay đổi của task, không reset toàn repo. Runtime trước khi dừng Play để compile có snapshot trong backup; không ghi đè save trên đĩa. Khi kiểm hình, Editor đã chuyển sang một lượt Play mới, nên giữ nguyên lượt chơi mới đó thay vì nạp snapshot cũ đè lên.

Điểm rút ra: kiểm vận tốc xuyên ranh giới Dash→Walk bắt được nhịp khựng mà kiểm từng trạng thái riêng không thấy; độ nghiêng thân cần kiểm cùng chân trụ. Không thêm framework hoặc script QA vào repo.
