# Linh Thú Ký — đặc tả gameplay

Cập nhật 05/10/2026 sau hai vòng làm rõ với chủ sản phẩm. Các yêu cầu đã xác nhận nằm ở mục 2–6; mục 7 ghi những lựa chọn tạm do chủ sản phẩm giao tự quyết. Trạng thái triển khai/kiểm chứng nằm trong [bản logic 01](LOGIC_V1.md).

## 1. Thứ tự tham chiếu

Yêu cầu trực tiếp mới của chủ sản phẩm được ưu tiên. Tài liệu này sở hữu gameplay; [kiến trúc online](ONLINE_ARCHITECTURE.md) sở hữu đích kỹ thuật; [quy tắc mỹ thuật](../ArtDirection/STYLE_GUIDE.md) sở hữu art. [Kế hoạch v2](../game_architecture_plan.md) là bản nháp tham khảo, không tự áp dụng các luật chưa được xác nhận.

## 2. Trải nghiệm và điều khiển đã xác nhận

Người chơi gắn bó lâu dài với một linh thú yếu, khám phá lấy nguyên liệu để bồi dưỡng thành thần thú. Khó gỡ liên kết; sưu tầm loài hiếm không phải mục tiêu chính. Phần lớn loài có ưu, nhược điểm riêng, một số loài có thể mạnh hơn. Chủ sản phẩm sẽ quyết định chỉ số và kỹ năng từng loài sau.

Người chơi có kỹ năng tấn công và chủ yếu trực tiếp điều khiển nhân vật. Linh thú về lâu dài tự đánh/tự dùng kỹ năng; có thể chuyển qua điều khiển thú để chọn thời điểm dùng chiêu. Được phép hoãn AI tự đánh trong bản đầu. Người và thú cùng map; khi chuyển sang thú, người tự đi theo. Chiều ngược lại, thú đi theo người là lựa chọn tạm phù hợp vòng chơi.

Bắt đầu một khế ước, mở dần tới tối đa ba theo cảnh giới người chơi. Có thể liên kết cả ba và cùng đem đi theo; không có kho khế ước vô hạn trong bản đầu.

Nhịp chiến đấu vừa phải. Khám phá và kiếm nguyên liệu là chính, giao tranh thường ở cường độ nhẹ. Thế giới tồn tại lâu dài kết hợp bí cảnh theo từng chuyến khám phá. Tình huống thử: linh quy đen yếu ở hồ được nuôi bằng linh thảo rồi tiến tới thần thú có hoa văn vàng. Thiên kiếp, diễn biến cốt truyện và tiến hóa hình dáng chưa nằm trong bản logic đầu.

## 3. Thu phục đã xác nhận

- Sau khi bị hạ gục, một số thú không biến mất mà còn sống ở trạng thái bị thương, suy yếu. Đến gần và tương tác mở bảng thu phục; dùng Huyết Khế, có xác suất thành công.
- Có thể tìm trứng rồi ấp linh thú.
- Thả thức ăn: thú đến ăn có xác suất nhỏ xuất hiện hảo cảm, duy trì vài phút. Cho ăn tiếp tăng hảo cảm; sau đó có thể thử dùng Huyết Khế.

Mức tỷ lệ, chi phí, thời gian và chia quyền mục tiêu khi nhiều người tham gia chưa được chủ sản phẩm đặt số cụ thể. Bản local dùng cấu hình thử; không suy ra đã có quyền sở hữu mục tiêu online.

## 4. Bồi dưỡng và cảnh giới đã xác nhận

Cá thể cùng loài có cùng bộ kỹ năng. Khác biệt sức mạnh chủ yếu đến từ tu vi và bồi dưỡng. Nguyên liệu phù hợp, chất lượng tốt tăng tu vi, căn cốt, tấn công, phòng thủ tốt hơn đồ thường. Nuôi bằng đồ thường từ sớm có thể bù về sau, nhưng cần thêm công và tài nguyên; không làm hỏng vĩnh viễn linh thú đã liên kết.

Ăn rồi tiêu hóa dần, vẫn tiếp tục khám phá được. Căn cốt ảnh hưởng tốc độ hấp thu/tăng tu vi và hạn mức tích lũy trước đột phá. Ví dụ chủ sản phẩm đưa: có căn cốt tích lũy tới 200 mới đột phá, căn cốt thấp tới 100. Căn cốt cao cho nền tảng sâu hơn, không được tự diễn giải thành cần ít tu vi hơn.

Nhân vật tăng tu vi bằng tu luyện và đan dược trước. Luật đột phá chi tiết chưa do chủ sản phẩm chốt; cơ chế tạm nằm ở mục 7.

Khi linh thú hết máu, lập tức dịch chuyển vào Thần Đình nghỉ, hồi máu từ từ và khá lâu. Không chết vĩnh viễn trong bản đầu.

## 5. Thần Đình đã xác nhận

Mỗi Thần Đình là không gian rộng, bản thử dùng lưới 10×10. Mỗi ô chọn một nền sử dụng:

- Ô vật phẩm: giữ vật phẩm/stack; không trồng cây hay tưới nước.
- Ô đất: đặt một mảnh đất, có thể gieo và tưới cây; cho phép đặt vật phẩm như cây cảnh/ao trang trí lên trên.

Cây trồng và trang trí được giữ ở hai lớp riêng để không vô tình ghi đè nhau. Người chơi dùng thần hồn bước vào Thần Đình; linh thú cũng có thể ở bên trong. Quy tắc thân thể ngoài map và điều kiện ra/vào do bản thử tạm chọn, không phải yêu cầu bảo hộ cho PvP online.

Cây tiếp tục lớn khi người chơi nghỉ, thời gian tiến triển tối đa 12 giờ cho mỗi lần vắng mặt. Chưa yêu cầu tự thu hoạch hoặc nhiều vụ tự lặp.

## 6. Thất bại và nghề đã xác nhận

Khi người chơi gục, chỉ mất một phần nguyên liệu kiếm trong chuyến đi; không lấy đồ cũ đã tích lũy để phạt. Mức tỷ lệ và cách kết thúc chuyến đi do bản thử đặt tạm.

Ai cũng có thể học các nghề nếu đầu tư thời gian. Hướng sản phẩm có trồng linh dược, binh khí sư/rèn vũ khí, luyện đan, luyện phù và buôn bán tài nguyên cho người khác. Giao dịch nhiều người cần đặc tả và authority online riêng.

## 7. Lựa chọn tạm do chủ sản phẩm giao tự quyết

| Phần | Luật của bản logic đầu |
| --- | --- |
| Cảnh giới/khế ước | Ba cảnh giới mở 1, 2, 3 khế ước; cả ba có thể đi theo. Không có nút gỡ khế ước |
| Tu vi | Lưu tổng tu vi đã kiếm và phần đã dùng cho đột phá riêng; giữ chỉ số nền tích lũy sau đột phá |
| Căn cốt | 0–100, tốc độ hấp thu ×(1 + căn cốt/100), sức chứa mặc định 100 × cấp cảnh giới ×(1 + căn cốt/100) |
| Đột phá | Trong Thần Đình, đủ sức chứa tu vi và 3 linh khoáng thì chắc chắn thành công; chưa có thiên kiếp |
| Thức ăn | Một bữa tại một thời điểm; tiêu hóa nền 30s, căn cốt rút ngắn. Bồi dưỡng công/thủ có trần theo cảnh giới, có thể bù dần bằng thức ăn |
| Cạn máu | Tự thu hồi, mất quyền điều khiển thú, hồi đầy trong khoảng 10 phút theo cấu hình; chủ động gọi ra sau khi hồi đầy |
| Thu phục | Mục tiêu bị thương hoặc có hảo cảm; mặc định 85% + phần hảo cảm, mỗi lần tốn một Huyết Khế; thất bại được thử lại |
| Hảo cảm/trứng | Mỗi lần ăn có 20% khởi phát hảo cảm, tăng 20, giữ 180s; ấp một trứng trong 60s, giữ sẵn một ô khế ước |
| Thần Đình | Một không gian 100 ô trước, mỗi stack tối đa 99; một lớp cây trồng và một trang trí mỗi ô đất; quản lý ô qua bảng Thần Đình của người chơi; dashboard chủ dự án có công cụ kiểm tra riêng |
| Ra/vào | Sau giao tranh 5s mới nhập; thân thể được bảo hộ khi thần hồn bên trong bản local; trở về đúng vị trí |
| Cây | Cần nước; ngọc linh thảo chín sau 180s tăng trưởng, một lần tưới đủ 12h, thu thủ công được ba nguyên liệu |
| Offline | Cây đủ nước và thú đang nghỉ tiến triển tối đa 12h; tiêu hóa, tu luyện và ấp trứng chỉ khi mở game |
| Chuyến đi | Tài nguyên nhặt ngoài map mang dấu chuyến đi. Chuyển ô hoặc cất Thần Đình không bỏ dấu; về trại đầu và hoàn tất mới bảo toàn |
| Gục ngã | Mất 50% phần còn mang dấu chuyến đi, làm tròn lên; hồi sinh ở trại, đồ đã bảo toàn giữ nguyên |
| Nghề | Ba công thức khởi đầu; học được tất cả, mỗi lần chế tạo +5 kinh nghiệm. Sản phẩm dùng nguyên liệu mới giữ dấu chuyến đi |
| Dashboard | Bản dành chủ dự án chạy local; cấp đồ, tua thời gian và chỉnh dữ liệu có nhãn rõ; áp dụng cấu hình rồi lưu |

Thông số nhịp chơi, thức ăn và loài có thể chỉnh trong dashboard. Công việc đã khởi tạo như cây/bữa ăn giữ thời gian và phần thưởng ban đầu khi cấu hình thay đổi. Các cơ chế sâu hơn như thương tích người chơi, nhiều Thần Đình, bí cảnh và kỹ năng riêng từng loài được mở rộng sau khi kiểm tra vòng chơi đầu.

## 8. Phạm vi kỹ thuật

Chưa thuê VPS; mã logic C# độc lập Unity nằm trong assembly Domain, bản local chạy chung với input/UI Unity. Giữ mẫu mỹ thuật cũ; nhân vật và vật thể ở scene mới là khối màu. Networking, persistence server, tài khoản, giao dịch online và sức chứa chưa được triển khai/đo ở bản này.

Tài liệu v2 không được dùng để tự khôi phục kho thú vô hạn, tiềm năng ngẫu nhiên, hệ khắc chế chưa chốt, nhân vật cáo, mức mất đồ áp dụng cả kho hoặc quy tắc thú ngất chỉ hồi ở điểm an toàn. Những luật này đã khác yêu cầu hiện hành.

## Tách giao diện ngày 05/10/2026

Dashboard dành cho chủ dự án theo dõi và điều chỉnh hệ thống, tách khỏi HUD người chơi. Nút chính trong game kiêm đánh/nhặt/tương tác theo ngữ cảnh; nút kỹ năng và Thần Đình ở cạnh. Quy tắc chi tiết nằm trong [UX_SPEC.md](UX_SPEC.md).
