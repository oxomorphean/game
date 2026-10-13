> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Mốc combat 01 — phạm vi triển khai

## Khảo sát và quyết định

Combat cũ ở `GameSession`/`Expedition`/`WildMovement`, gắn với thu phục, thú đồng hành, vật phẩm và save. `CartoonArtTrial` chỉ có di chuyển hình mẫu. `GameplayHud` cũ vừa dựng world bằng Canvas vừa dựng HUD. Mốc mới dùng C# thuần cho mô phỏng trong assembly Domain hiện có, composition qua host Unity; không ECS, singleton hoặc event bus toàn cục.

Tái sử dụng `MeadowMap.prefab`, PNG CartoonV2, `CartoonCharacterRig`, `ModularMapProp`, font NotoSans, Input System, Unity 2D Animation và quy ước test runner qua plugin. Module combat mới không gọi `GameSession` hoặc persistence. Combat cũ vẫn phục vụ scene cũ; việc chuyển các cơ chế kinh tế/save cũ sang hướng mới nằm ngoài mốc này.

## Thành phần

- `Domain/Combat`: definition bất biến, trạng thái từng actor, hình học/va chạm, mô phỏng và sự kiện kết quả từng tick.
- `Client/Combat`: input → host → combat; world view đọc trạng thái/sự kiện để dựng sprite, HP quái, hiệu ứng; HUD chỉ chứa thông tin và nút điều khiển.
- `CombatTrialConfig`: ScriptableObject authoring chuyển thành definition khi tạo phiên.
- `Editor/CombatTrialBuilder`: tạo scene `PlayerCombatTrial` qua Unity API, đặt vật cản được author riêng trên bản instance của map, build vào `WebBuildCombat`.
- `AgentScripts/CombatTests.cs`, kiểm tra runtime và `Verification/CombatM1`: kiểm chứng phạm vi.

## Invariant và rủi ro

UI, animation và input không tự trừ máu hoặc thay cooldown. Definition không chứa trạng thái trận đấu. Một instance đòn có một lần resolve; mục tiêu phải còn sống, trong tầm/góc và không có vật cản chắn ở thời điểm impact. Gục hủy đòn và cấm lệnh; respawn không tái phát impact cũ. Mô phỏng cập nhật bước 0.02s, chặn dt/input không hữu hạn. Phiên này là authority local, chưa phải chống gian lận online.

Collision được author theo chân thân cây/đá và lòng hồ, không suy từ pixel alpha. AI chỉ là truy đuổi và tránh vật cản cục bộ, chưa phải tìm đường cho map phức tạp. Art hai hướng có sẵn được tái sử dụng; swing, nháy trúng đòn và gục được điều khiển bằng trạng thái thật, chưa có bộ attack sprite nhiều hướng mới.

## Kiểm tra và hoàn thành

Viết xong phạm vi rồi chạy các case tầm/góc/windup/cooldown/đơn-hit/aoe/LOS, passive và hồi sinh, collision/diagonal, AI và input không hợp lệ. Kiểm tra Unity compile, runtime scene/UI/animation, build WebGL và mở bản thực tế; kiểm tra hash scene/prefab/save cũ. Không chạy migration, commit, push hoặc deploy.

## Khôi phục

Mốc trước code: `Recovery/combat-m1-20261007-111859/`: `project-before.tgz` giữ 357 file source/asset/config (bao gồm untracked), `manifest.json` giữ hash và Git HEAD, `git-status-before.txt` giữ trạng thái Git, `native-save-before/` giữ bốn file save/backup native. Không có Library hoặc Web build trong archive. Khôi phục bằng cách giải nén vào thư mục tạm rồi chép lại đúng file cần phục hồi; không `git reset` worktree bẩn. Phiên web mới dùng đường dẫn build riêng và không dùng PlayerPrefs.
