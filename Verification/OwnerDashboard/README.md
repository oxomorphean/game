# Kiểm tra dashboard trong game — 13/10/2026

Scene: `ArtPreview`. Bản Web: `WebBuildV1`, phục vụ tại `http://127.0.0.1:8791/`.

## Kết quả

- Catalogue: 7/7 kiểm tra đạt, 141 asset gốc và 2 bố cục map; xem `catalog-checks.json`. Trong số đó, 28 asset được bản chơi tham chiếu. Map chính có 21 lượt đặt đồ vật. Các mẫu cũ được giữ trong tổng dự án; font, mã nguồn, `.meta`, ảnh QA và từng sprite con không được đếm.
- Unity Play: nút Quản trị, F2 và Esc hoạt động; cả ba trang hiển thị. Sau khi nhân vật đi khỏi điểm xuất phát, mở bảng rồi giữ W, nhấn R và nhập tìm kiếm: vị trí trước/sau cùng là (-0.523284435, -2), host chặn input. Xem hai bản ghi trong `interaction.jsonl` và `editor-2-open.png`.
- WebGL thực: mở/đóng bằng nút và F2/Esc, xem map hiện tại và map tham khảo, cuộn danh sách, lọc 28 asset trong bản chơi, tìm HeroC2 ra 57 kết quả, chuyển trang và tìm tên không tồn tại đều hoạt động. Thử viewport 1280×720 và 960×720; đã trả viewport về mặc định.
- Console trình duyệt thu được 0 warning/error trong phiên kiểm tra; xem `browser-console.json`.
- Build WebGL thành công, 0 lỗi, 5 warning; báo cáo đầy đủ ở `../ArtOnly/build.json`. Warning gồm Pipeline không cấu hình runtime trong Player, một pragma shader TMP đã cũ và ba thông báo biên dịch TMP/IL2CPP lớn.
- Review các file dashboard, cách tích hợp HUD/host, catalogue và kiểm tra dữ liệu; `git diff --check` đạt. Không chạy lại toàn bộ kiểm tra gameplay đã nghỉ dùng.

## Bằng chứng giao diện

- `web-launcher.jpg`: nút quản trị luôn hiện trên game.
- `web-overview.jpg`: tổng quan số liệu thật.
- `web-map.jpg`: bố cục và đồ vật trên map.
- `web-assets.jpg`: tìm kiếm và chi tiết nguồn asset.
- `web-empty.jpg`: tìm kiếm không có kết quả.
- `web-assets-960.jpg`: giao diện ở viewport hẹp.

Dashboard này chỉ xem và lọc thông tin. Chưa có chỉnh/xóa asset, lưu bố cục hoặc tài khoản/quyền quản trị online. C2 vẫn là mẫu nhân vật riêng; danh mục không biến mẫu đó thành nhân vật chính trong build.
