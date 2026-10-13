# Thử MCP for Blender — 09/10/2026

Nguồn: https://github.com/ahujasid/mcp-for-blender, package 2.1.9 tại commit 7a0373ec9199183cb460068c4f96aed9c579fb4f. Đây là dự án cộng đồng của ahujasid, không thuộc Blender Foundation. Tên PyPI trước đây là blender-mcp; tên hiện tại mcp-for-blender được đối chiếu với chính repository tác giả.

## Đã thử thật

MCP client Python SDK gọi server STDIO; server gọi addon trong Blender GUI 4.5.9 LTS, bind 127.0.0.1:9876. Không chỉ gửi Python trực tiếp qua socket rồi gọi đó là MCP. Handshake thành công; get_scene_info đọc 100 mesh, một armature tám bone; execute_blender_code đọc bảy Action rồi chuyển sang run/frame 4, hai chân nhận góc +0.65 và -0.65 rad; look trả ảnh bốn góc và dải sáu frame animation. JSON và PNG bên cạnh là kết quả tool thực tế.

Model thử là bản sao /tmp/nguthu-blender-mcp-trial/hero-mcp-trial.blend; thao tác pose chỉ nằm trong RAM, không ghi đè hero.blend, PNG hoặc scene Unity. SHA256 bản gốc và bản sao ban đầu ở trial.json. Không có thay đổi game cần build lại sau lần thử này.

## Cài đặt và giới hạn

Mã MCP và venv nằm tập trung ở /Users/nguyenhoangdacdat/dev/tools/mcp-for-blender. Dependency cài bằng uv sync --frozen --no-dev theo uv.lock. Không cài addon vào Preferences toàn máy, không sửa cấu hình Codex hoặc bật tự khởi động; addon chỉ đăng ký trong phiên Blender thử. Client sử dụng BLENDER_MCP_SAFE_MODE=1, BLENDER_MCP_DISABLE_TELEMETRY=1; Blender dùng BLENDERMCP_NO_UPDATE_CHECK=1. Không kết nối dịch vụ tạo 3D trả phí, tải asset ngoài hay chia sẻ screenshot qua telemetry. Safe mode giới hạn script trên đường MCP; nó không biến socket addon thành sandbox hoặc thêm xác thực.

Để gọi tiếp trong phiên hiện tại, dùng môi trường .venv của tool và MCP SDK STDIO với command .venv/bin/mcp-for-blender, args --host 127.0.0.1 --port 9876, cùng các biến môi trường trên. Sau khi đóng Blender, cần khởi động lại addon. Tích hợp trực tiếp thành tool của Codex chưa đăng ký; yêu cầu khởi động lại Codex thuộc bước cài tích hợp lâu dài, không phải bằng chứng của lần thử này.

## Nhận định cho dự án

Dùng MCP cho sửa model/rig, lấy góc nhìn và soi animation ngay trong Blender. Dùng create_hero.py + pack_sheets.py để tái tạo/export sprite có cùng camera, pivot và thông số. MCP không thay modeler hoặc tự bảo đảm nhân vật đẹp; mẫu hiện tại vẫn cần chủ dự án duyệt về tỷ lệ, tóc và trang phục. Các PNG thử không được nhập vào game.

Có thể gỡ toàn bộ bản thử bằng cách đóng Blender và xóa thư mục dev/tools/mcp-for-blender; venv/cache nằm bên trong. SDK có tạo metadata UUID cục bộ tại ~/Library/Application Support/BlenderMCP dù telemetry bị tắt; chưa thay quyền tài khoản nào.
