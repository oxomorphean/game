# Đổi giữa các bản môi trường

1. Dừng Play Mode. Tại thư mục dự án, chạy `git status --short`; chỉ đổi nhánh khi không có output. Nếu có thay đổi chưa lưu/commit, dừng và giữ nguyên chúng. Không dùng `reset --hard` hoặc `clean`.
2. Chọn nhánh cần xem:
   - `git switch main` — v4, baseline `4247dcf` (`art-v4-before-unify`).
   - `git switch codex/art-style-sample` — v5, `d59a13e` (`art-v5-candidate`).
   - `git switch codex/huyen-giap-quy` — v6, nhánh tạo từ baseline v4.
3. Chờ Unity import xong, rồi mở lại scene `Assets/ArtSample/SpiritTurtleGarden.unity`. Nếu cần dựng lại scene và sprite từ nguồn, chạy `CreateScene` trước; sau đó build WebGL bằng Unity CLI như README:

```sh
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill sprite-editor --file AgentScripts/ArtSampleBuilder.cs --entry ArtSampleBuilder.CreateScene --format json
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --file AgentScripts/ArtSampleBuilder.cs --entry ArtSampleBuilder.BuildWeb --timeout_ms 180000 --timeout 240 --format json
```

4. Chạy `python3 serve.py` tại thư mục dự án nếu server chưa chạy, rồi reload `http://127.0.0.1:8788/`.

Builder trong baseline v4 lấy cổng từ `ArtDirection/Sources/gate-v2.png`, cây từ `tree-v2.png` và `tree-puffy-v3.png`, hồ từ `pond-v3.png`, đá từ `rock-v2.png`; các tile cỏ/gạch được tạo trong builder.
