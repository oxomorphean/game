# Dashboard chủ dự án

OwnerDashboard/OwnerCatalog ở `Scripts/DevTools/Dashboard`, assembly `LinhThu.DevTools`. Công cụ nội bộ tách khỏi HUD chơi; Domain/Client không tham chiếu DevTools. Dashboard nhận ModularMapLayout, TMP font và TextAsset catalogue bằng serialized reference; giữ IsOpen và phát OpenChanged để host kiểm soát input.

Catalogue ở `Content/DevTools/catalog.json` là snapshot được lọc đường dẫn còn tồn tại sau hợp nhất, không tự cập nhật khi Play/build. `inBuild` không được dùng làm bằng chứng build thực tế. Khi cập nhật catalogue, lấy dependency từ Build Settings bằng Unity API, không phục hồi OwnerCatalogBuilder.

MainGame không chứa dashboard. Khi gắn công cụ này vào workflow chủ dự án, cấp đủ map/font/catalogue và xử lý OpenChanged trong host; kiểm tra F2/Escape, tìm/lọc/cuộn và chặn input. Công cụ không sửa/xóa asset hoặc ghi save.
