# Độ nét map — 07/10/2026

Đã bỏ giới hạn thu nhỏ texture môi trường: cây giữ 1357×1159; đá, cỏ, hoa, bụi cây và hồ giữ 1536×1024. Ground giữ 1672×941 như trước. Importer Bilinear, Uncompressed, max 2048; PNG nguồn giữ nguyên hash. Không sửa scene, prefab transform hoặc collider. Sprite bounds chênh tối đa 0.1% do bỏ làm tròn khi resize, không đổi tỷ lệ bố trí.

Preview combat render tối thiểu 2×, tối đa 3× theo mật độ màn hình. Đã reload bản build mới tại 8790 và kiểm tra canvas 2560×1440 cho viewport 1280×720, trang tải xong, console warn/error rỗng. Viewport kiểm tra đã trả về mặc định.

CombatBuild.Run thành công: 0 lỗi, 5 cảnh báo Pipeline/TMP; chưa chạy kiểm tra gameplay riêng vì thay đổi chỉ import/render. Logic đã nhận lại Editor. Báo cáo build, import trước/sau và ảnh preview-desktop.png nằm cùng thư mục. Cấu hình mới được giữ trong ModularMapBuilder.cs, Web/combat.html và tài liệu ArtDirection/CartoonV2/README.md.
