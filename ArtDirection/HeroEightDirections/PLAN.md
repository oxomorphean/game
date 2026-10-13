# Nhân vật 8 hướng — 08/10/2026

Chủ dự án yêu cầu phiên Logic tự triển khai phương án đã đề xuất: một chibi 3D tạo bằng Blender, render thành sprite 8 hướng dùng trong Unity 2D. Không giao subagent.

## Phạm vi và invariant

Tóc bạc, áo xanh đen/trắng ngà, điểm ngọc/vàng ít; góc nhìn cao theo STYLE_GUIDE. Có trước, sau, hai bên và bốn chéo. Idle/run/basic/sweep/dash/hurt/down cùng một model; nguồn .blend và script lưu ở thư mục này. Chuyển động, hitbox, sát thương và save do logic hiện có sở hữu. View chỉ đọc state; đứng yên giữ hướng cuối, tấn công theo aim đã khóa. Không thay asset/map/collider của Art.

## Các bước

1. Giữ nguồn trước sửa, kể cả file chưa được Git theo dõi; cài Blender ARM từ nguồn chính thức.
2. Tạo model/rig/toon vật liệu; xuất PNG animation vào Hero8Directions riêng, giữ hero.png cũ.
3. Thêm definition sprite animation, presenter và import builder chuẩn Unity; chỉ gắn cho player trong CultivationV1/PlayerCombatTrial.
4. Kiểm tra hướng/animation/thứ tự ưu tiên/cooldown và hồi quy combat, compile, Play Mode và WebGL; xem bản web thật.

## Kiểm chứng bắt buộc

8 hướng giữ hình dáng nhất quán; idle giữ hướng; con trỏ không ghi đè hướng đi; windup/recovery theo gameplay; gục dừng chuyển động, hồi sinh thoát pose; chân và pivot ổn định; bóng và sorting đi theo sprite hiện hành; bản web dùng input thật. Không tuyên bố sức chứa multiplayer từ kiểm tra này.

## Khôi phục và phối hợp

Recovery/Hero8Directions-20261008/source-before.tgz chứa source trước thay đổi; hero.png, rig cũ và các map/prefab gốc không xóa. Chỉ khôi phục các file do mốc này sửa, không bung đè toàn cây. Không commit/push. Giữ working tree hiện tại theo yêu cầu làm trực tiếp trong dự án đang mở. Kiểm tra gom cuối mốc theo AGENTS và yêu cầu chủ dự án.

## Tiến độ

- Đã khảo sát renderer/Domain và lưu mốc khôi phục.
- Đã tạo model/rig và 416 frame, nhập definition và presenter vào hai scene.
- Compile, 209 assertion, Play Mode và WebGL build đạt; bản web đã thử bằng phím thật. Chi tiết: ../../Verification/Hero8Directions/verification.md.
- Mỹ thuật vẫn là mẫu cần chủ dự án duyệt; không công bố benchmark multiplayer hoặc thiết bị yếu.
