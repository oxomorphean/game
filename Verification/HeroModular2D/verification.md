# Mẫu nhân vật tách lớp — kiểm chứng 09/10/2026

Native SpriteSkin: 20 kiểm tra đạt, 0 thất bại. Có vertex deformation của cơ thể gốc khi chạy, thay tóc/áo không reset pose, bỏ/mặc đồ giữ mesh cơ thể, chém windup/impact, độc lập theo actor và input/nút thật. Camera mức zoom thường nằm trong nền.

WebGL: build thành công, 0 lỗi, 4 cảnh báo; bằng chứng chi tiết trong build.json. Chơi thử browser tại 8795: chạy cơ thể gốc, mặc đồ/đổi tóc trong lúc chạy, zoom gần và kích thước chơi. Ảnh browser và console lưu cạnh báo cáo.

Protected hash: CultivationV1.unity, PlayerCombatTrial.unity, MeadowMainMap.prefab, MeadowMap.prefab khớp bản trước task. Editor stopped, trả về CultivationV1. Không sửa Domain/save/luật trang bị; không commit/push/deploy.

Giới hạn: mẫu góc nhìn trước, hai tóc/hai áo và một quần/một giày; chưa có đủ tám artwork, chưa nối vào player chính hoặc đo sức chứa nhiều người.
