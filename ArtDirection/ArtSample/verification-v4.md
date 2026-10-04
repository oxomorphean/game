# Kiểm chứng mẫu linh quy v4

Scene hiện hành: `Assets/ArtSample/SpiritTurtleGarden.unity`; dựng bằng plugin Unity CLI trong Editor 6000.6.4f1, không sửa YAML scene/metadata.

- Main agent tích hợp Unity/web; một subagent cùng model tạo asset qua Aseprite MCP stdio thật. Nguồn 5 layer/10 frame ở `ArtDirection/Handmade/SpiritTurtle/spirit-turtle.aseprite`, kèm Lua/Python tái tạo và bằng chứng các tool call.
- `pixel_validate.py --kind sprite --cell 64x64 --strict`: cả ba PNG PASS, 0 WARN/FAIL. Palette dự án, alpha 0/255, tối đa13 màu/frame; 4 idle và6 walk frame khác nhau.
- Import Unity: 1 sprite đứng, 4 idle và6 walk frame, cùng64×64/PPU16/pivot(32,12), Point, không mipmap/nén. Camera Built-in Pixel Perfect orthographic, tham chiếu384×256; kiểm tra live: `import-verification-v4.json`.
- Runtime probe trong Play Mode: quan sát đủ4 sprite idle và6 sprite walk, di chuyển thực và reset về spawn đều PASS. Bằng chứng: `animation-verification-v4.json`; script `AgentScripts/TurtleRuntimeProbe.cs`. Probe đặt điểm đến trong runtime qua reflection, khác với kiểm tra thao tác người chơi trên web bên dưới.
- Linh hồ cũ đã được xóa khỏi Assets bằng Unity API: scene SpiritFoxGarden, SpiritFoxController, fox.png, fox-run.png và hai ảnh preview cũ. Sân, cổng, đá, hai cây anh đào, bóng cây và hồ nước được giữ nguyên. Tài liệu/ảnh nguồn lịch sử ngoài Assets được giữ để tham khảo.
- Unity WebGL build Succeeded, 0 lỗi,26.397154 giây. Kết quả gốc: `build-result.json`; trang phiên bản0.4.
- Trình duyệt thật tại `http://127.0.0.1:8788/`: tải scene linh quy, bấm sân thấy di chuyển, A quay trái, R reset đúng chỗ cũ. Log trình duyệt không có warn/error ở kiểm tra cuối. Ảnh: `web-preview-v4.png`; Game View: `unity-preview-v4.png`.

Hướng trái dùng flip, lên/xuống dùng cùng bộ bước chân; chưa có sprite riêng cho bốn hướng. Chưa thêm hệ thống chiến đấu, tiến hóa hoặc nuôi thú.
