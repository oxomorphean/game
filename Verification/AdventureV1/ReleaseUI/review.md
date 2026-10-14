# Review UI WebGL release — 08/10/2026

URL `http://127.0.0.1:8791/`, tab riêng trong IAB. Không dùng Editor hoặc sửa source/Assets trong lượt review. Tiếp tục từ bản lưu đang có; không NewGame/reset/Save hoặc thao tác thay dữ liệu nhân vật.

## Kết quả

Không tìm thấy blocker UI trong phạm vi kiểm tra. Title, HUD, bảy bảng và menu nhìn vừa viewport 1280×720 và 960×720; nhãn/lý do khóa không đè nhau trong ảnh đã xem. Viewport CSS tương ứng canvas WebGL 2560×1440 và 1920×1440 theo cấu hình devicePixelRatio=2 của wrapper.

Pointer mở bảng/chuyển tab được. Phím B/C/T mở đúng bảng, Esc đóng bảng và mở menu. Wheel cuộn Công pháp đến dòng cuối ở cả hai kích thước; cuộn Thương nhân đến vật phẩm cuối ở 960. Các ảnh trước/sau cuộn và ảnh từng bảng nằm cùng thư mục. Console warn/error được tool quan sát trả về rỗng; lần lấy cuối sau reload title, không thay cho báo cáo Console/gameplay của Logic.

Metadata của cả 23 ảnh đã đọc bằng `sips`: 11 ảnh JPEG 1251×720 và 12 ảnh JPEG 960×720; các file vẫn mang đuôi `.png` do cách lưu screenshot. Các số viewport CSS/canvas ở trên lấy từ DOM riêng, không lấy từ kích thước ảnh.

## Theo dõi nhỏ

P3: Continue bị khóa chưa có màu phân biệt rõ với trạng thái bật. Logic đã quan sát nhánh không có save; source luôn dùng Ink/White và disabled tint gần trắng. Tab review hiện có save nên chỉ xác minh nhánh Continue bật. Patch `/private/tmp/v1-ui-continue-affordance/continue-disabled.patch` đổi màu riêng nút Continue theo CanContinue, giữ nguyên command/interactable. Patch chỉ staging, git apply --check đạt; chưa compile/render/test patch. Không cần ghép để hoàn tất bản release đang được nghiệm thu.

## Giới hạn

Đây là review UI desktop, không kết luận gameplay/save/performance. Chưa thử thông báo boss/gục trên browser và không kiểm touch/mobile. API/Domain/Host và modern style giữ nguyên. Logic sở hữu quyết định tích hợp patch, build và QA cuối.
