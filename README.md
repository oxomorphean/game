# Linh Thú Ký — bản thử mỹ thuật

Theo yêu cầu 12/10/2026, bản hiện tại chỉ có map, nhân vật, di chuyển/va chạm, animation và ánh sáng. Combat, lên cấp/tu luyện, kỹ năng, vật phẩm, nghề và save không còn trong source đang biên dịch.

Mở scene `Assets/_Project/Content/Scenes/ArtPreview.unity` rồi bấm Play. WASD/mũi tên hoặc bấm sân để đi; R về điểm xuất phát. Bản WebGL dùng [localhost:8791](http://127.0.0.1:8791/) sau khi build và chạy `python3 serve_adventure.py`.

Nút **Quản trị** trên màn chơi (hoặc F2) mở dashboard chủ dự án: Tổng quan, Bản đồ và Asset. Có số lượng thực, tìm kiếm, bộ lọc và chi tiết nguồn; Escape đóng để đi lại tiếp. [Quy tắc catalogue](Architecture/OWNER_DASHBOARD.md).

Build qua menu **Demo → Mỹ thuật → Build Web xem mẫu**. [Kiến trúc hiện hành và workflow](Architecture/CODE_ARCHITECTURE.md) · [Thiết kế và phạm vi hiện hành](GAME_DESIGN.md) · [Chuẩn mỹ thuật](ArtDirection/STYLE_GUIDE.md).

Map chính giữ texture cỏ/đất lặp, đường mesh chỉnh điểm và các prefab cây/đá/hồ riêng. Nhân vật minh họa có hướng trước/lưng/nghiêng và animation. `HeroModularTrial` là mẫu tách lớp để xem trang phục, hiện chỉ có hình mặt trước/phản chiếu; `CartoonArtTrial` giữ mẫu người/quy cũ để so sánh mỹ thuật.

Bản khôi phục trước dọn nằm ở `Recovery/ArtOnly-20261012/`; có archive source/asset và nguyên file gameplay được chuyển ra khỏi `Assets`. Save cũ không bị xóa. Không suy ra khả năng multiplayer hoặc chất lượng thẩm mỹ từ các báo cáo test cũ.

[Quản lý dung lượng và khôi phục file đã dọn](Architecture/REPOSITORY_STORAGE.md). Checkout giữ source/art đang dùng; ảnh nghiệm thu và frame trung gian không thuộc asset game.
