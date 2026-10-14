# Linh Thú Ký — hướng dẫn cho agent

## Phối hợp các phiên

- Trước mỗi task trong project, đọc [GAME_DESIGN.md](GAME_DESIGN.md), gồm mục Phối hợp bốn phiên; xác định vai trò theo yêu cầu của người dùng và giữ đúng phạm vi task.
- Chỉ coi quyết định được chủ dự án duyệt là luật chính thức; đề xuất và thông số thử nghiệm phải được ghi riêng.
- Thay đổi ảnh hưởng phiên khác cần ghi ngắn kết quả, file/asset hoặc interface thay đổi và phần cần bàn giao trong tài liệu của phần đó.
- Một phiên sửa một scene, prefab hoặc file chung tại một thời điểm; kiểm tra thay đổi có sẵn trước khi sửa và không ghi đè công việc của phiên khác.

## Làm nhanh và dứt khoát

- Làm theo nhịp phát triển game: khảo sát vừa đủ để xác định đúng luồng hiện hành, triển khai trọn một tính năng hoặc mốc chơi được, rồi kiểm tra và sửa lỗi. Tránh kéo dài phân tích các khả năng chưa có dấu hiệu xảy ra.
- Ưu tiên API Unity, pattern game phổ biến và cách tổ chức đang có. Với thao tác quen thuộc, áp dụng cách chuẩn ngay; không thử riêng từng bước để xác nhận lại hành vi đã rõ.
- Dùng lệnh tiêu chuẩn, builder và bộ kiểm tra có sẵn của project. Không tự tạo script kiểm tra, framework hoặc quy trình riêng khi công cụ hiện có đã đủ.
- Với code, bố cục UI, animation và thông số thử nghiệm dễ sửa lại, tự chọn phương án hợp lý trong phạm vi task và triển khai liền mạch. Ghi ngắn assumption cần thiết; chỉ hỏi khi lựa chọn ảnh hưởng đáng kể luật chưa chốt, dữ liệu, kiến trúc hoặc chi phí.
- Gom compile, test liên quan và chơi thử vào cuối mốc gameplay; không sửa vài dòng rồi build/test/chụp ảnh lặp lại. Chỉ kiểm tra chen giữa khi có lỗi thực tế hoặc một bước tích hợp chưa rõ cản phần tiếp theo; không chạy lại kiểm tra đã đạt khi không có thay đổi liên quan.
- Save/migration và thao tác có nguy cơ mất dữ liệu vẫn cần bản khôi phục và kiểm tra riêng trước khi áp dụng. Sửa lỗi thuộc phạm vi trước khi bàn giao; báo đúng phần đã và chưa kiểm chứng.

## Mỹ thuật

- Trước task tạo hoặc sửa nhân vật, linh thú, map, tile, đồ vật, hiệu ứng hay UI, đọc [ArtDirection/STYLE_GUIDE.md](ArtDirection/STYLE_GUIDE.md) và ảnh tham chiếu được liên kết trong đó.
- STYLE_GUIDE.md là chuẩn mỹ thuật hiện hành. Các ảnh, prompt và README của mẫu cũ là lịch sử thử nghiệm, không tự dùng làm hướng mặc định.
- Giữ thiết kế nhất quán qua asset và animation; đối chiếu đủ checklist trong style guide trước khi báo cáo đạt chuẩn.
- Ghi rõ asset mới chỉ là mẫu ảnh hay đã nhập/chạy trong Unity. Không suy ra sprite có thể dùng ngay chỉ vì ảnh sinh ra trông đẹp.
- Không tự đổi hướng mỹ thuật đã chọn. Yêu cầu trực tiếp mới của người dùng được ưu tiên.

## Phạm vi dự án

- Dự án nằm tại `/Users/nguyenhoangdacdat/dev/nguthu-unity-demo`.
- Gameplay hiện hành theo [GAME_DESIGN.md](GAME_DESIGN.md), kiến trúc theo [Architecture/CODE_ARCHITECTURE.md](Architecture/CODE_ARCHITECTURE.md); [game_architecture_plan.md](game_architecture_plan.md) là bản tham khảo khi không xung đột với tài liệu hiện hành. Style guide sở hữu quyết định mỹ thuật.
- Giữ scene và asset thử nghiệm hiện có trừ khi task yêu cầu thay thế. Dùng plugin Unity để thao tác scene/asset trong Editor; không sửa YAML của scene/prefab bằng tay.
- Task nhỏ làm trực tiếp, kiểm tra vừa đủ. Không tự thêm gameplay, dependency trả phí hoặc tải bộ asset ngoài phạm vi yêu cầu.

## Tài nguyên và dung lượng

- Ảnh/video nghiệm thu và frame xuất hàng loạt phải nằm ngoài `Assets`; dùng đường dẫn tuyệt đối tới `Verification/` khi chụp qua Unity. Giữ báo cáo ngắn trong Git; không thêm ảnh hàng loạt, thư mục `before/` hoặc archive backup vào source. Giữ master `.kra`/PSD và export game cần dùng; xem [quy tắc lưu trữ](Architecture/REPOSITORY_STORAGE.md) trước khi dọn hoặc nhập asset.
