# Cập nhật bản chơi 8791 — 09/10/2026

Dùng `AdventureBuild.Run` hiện có để cập nhật WebBuildV1 với asset nhân vật độ nét mới do phiên Mỹ thuật bàn giao. Không sửa source, scene, prefab, Domain, HUD hoặc save. Bộ 47 + 31 kiểm tra của lần bàn giao không chạy lại vì lần này chỉ build.

Builder compile không có diagnostic. WebGL build Succeeded, 0 lỗi, 1 cảnh báo Pipeline không có RuntimePipelineConfig; build 130.640.657 byte, 2.31 giây nhờ cache. Chi tiết ở `build-8791.json`.

Đã tải lại http://127.0.0.1:8791/, chọn Tiếp tục khám phá từ save có sẵn, di chuyển bằng S/D/W và thấy nhân vật mới trong map. Kiểm tra desktop 1280×720; viewport đã trả mặc định. Browser không ghi lỗi/cảnh báo; ảnh `web-8791.png`. Không bấm F5 lưu hoặc tạo nhân vật mới. Lượt này không kiểm tra lại combat, save/migration hoặc hiệu năng nhiều người chơi.
