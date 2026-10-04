# Linh Thú Ký — hướng dẫn cho agent

## Mỹ thuật

- Trước task tạo hoặc sửa nhân vật, linh thú, map, tile, đồ vật, hiệu ứng hay UI, đọc [ArtDirection/STYLE_GUIDE.md](ArtDirection/STYLE_GUIDE.md) và ảnh tham chiếu được liên kết trong đó.
- STYLE_GUIDE.md là chuẩn mỹ thuật hiện hành. Các ảnh, prompt và README của mẫu cũ là lịch sử thử nghiệm, không tự dùng làm hướng mặc định.
- Giữ thiết kế nhất quán qua asset và animation; đối chiếu đủ checklist trong style guide trước khi báo cáo đạt chuẩn.
- Ghi rõ asset mới chỉ là mẫu ảnh hay đã nhập/chạy trong Unity. Không suy ra sprite có thể dùng ngay chỉ vì ảnh sinh ra trông đẹp.
- Không tự đổi hướng mỹ thuật đã chọn. Yêu cầu trực tiếp mới của người dùng được ưu tiên.

## Phạm vi dự án

- Dự án nằm tại `/Users/nguyenhoangdacdat/dev/nguthu-unity-demo`.
- Đọc phần liên quan trong [game_architecture_plan.md](game_architecture_plan.md) khi làm gameplay hoặc kiến trúc; style guide sở hữu quyết định mỹ thuật.
- Giữ scene và asset thử nghiệm hiện có trừ khi task yêu cầu thay thế. Dùng plugin Unity để thao tác scene/asset trong Editor; không sửa YAML của scene/prefab bằng tay.
- Task nhỏ làm trực tiếp, kiểm tra vừa đủ. Không tự thêm gameplay, dependency trả phí hoặc tải bộ asset ngoài phạm vi yêu cầu.
