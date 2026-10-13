# Chuẩn sản xuất nhân vật — ứng viên v1

Ngày 13/10/2026, chủ dự án yêu cầu bổ sung năm phần và thử hai cách tạo nguồn: A dùng ImageGen rồi biên tập trong Krita, B dựng trực tiếp bằng công cụ Krita. Đây là chuẩn làm việc và thử nghiệm để duyệt; chưa có artwork mới được chủ dự án duyệt làm mẫu chính thức.

Đã có [bảng so sánh](comparison.png), [kiểm tra tách phần](editability.png) và [kết quả thử nghiệm](RESULTS.md). A là hướng tạo hình được đề xuất; B vẽ trực tiếp bằng công cụ chưa đạt thẩm mỹ. Cả hai còn thiếu vùng cơ thể bị che và chưa được rig.

Bổ sung thử nghiệm SVG theo yêu cầu tiếp theo: [mẫu C và kết quả](experiments/C/README.md), [đối chiếu A–C](experiments/C/comparison-A-C.png), [animation các bước vẽ](experiments/C/drawing-process.gif). C có nguồn SVG và 17 lớp vector thực trong Krita; vẫn là ứng viên hình đứng.

Mẫu tiếp theo [C2](experiments/C2/README.md) có thân nền, 52 lớp vector, hai bộ đồ cộng đồ cơ bản, hai kiểu tóc và hai màu mắt. Đã dựng scene Unity riêng với Idle/Run, kiểm tra 12 tổ hợp đổi ngoại hình trong Play mode. [Xem hình](experiments/C2/C2-overview.png) và [vòng chạy](experiments/C2/C2-run-preview.gif). Rig hiện là cutout; artwork và chất lượng khớp vẫn là ứng viên để duyệt.

| Phần | Nguồn sở hữu | Điều kiện nhận |
| --- | --- | --- |
| 1. Nhân vật mẫu | [MASTER.md](MASTER.md), ảnh ứng viên A/B | Duyệt hình toàn thân ở kích thước chơi trước khi khóa rig |
| 2. Template Krita | [TEMPLATE.md](TEMPLATE.md), [character-template.kra](character-template.kra) | Canvas, 19 nhóm slot trống, guide và cách xuất có thể sửa tiếp |
| 3. Quy tắc từng slot | [SLOTS.md](SLOTS.md), `anchors.json` | Mỗi món có vị trí gắn, thứ tự che và phạm vi tương thích |
| 4. Nét, màu, vật liệu | [PAINT.md](PAINT.md), [palette](character-v1.gpl), [bảng mẫu](paint-standard.png) | Có ví dụ nhìn thấy được và giá trị để tái lập |
| 5. Nghiệm thu | [ACCEPTANCE.md](ACCEPTANCE.md), [EXPERIMENT.md](EXPERIMENT.md) | Báo riêng mỹ thuật, nguồn chỉnh sửa và kiểm tra chuyển động |

Mẫu hiện tại tại `HeroProduction2D/` là baseline kỹ thuật. Giữ nguyên nguồn, scene và bản Web 8796. Artwork A/B giữ ngoài Assets; C2 được nhập vào `Assets/_Project/Art/HeroC2/` và scene thử riêng theo yêu cầu tiếp tục thử SVG. Không thay player, không bổ sung gameplay.

Quy trình: thiết kế toàn thân → duyệt hình → khóa mẫu cơ thể/góc nhìn → chuẩn hóa nguồn và slot → rig/weight → kiểm tra đổi đồ và chuyển động → đưa vào catalogue. Sửa tỷ lệ của mẫu cơ thể là phiên bản mới, không ép artwork mới theo rig thử cũ.

KRA là nguồn biên tập; PSD/PNG là bản giao. Không dùng script tái sinh đè lên nguồn artist đã sửa. Mọi bản xuất phải ghi mẫu cơ thể, góc nhìn, slot, phiên bản nguồn, ngày xuất và kết quả kiểm tra. Tệp xuất không được chứa nhóm Guides/References.

Nguồn thử: [A.kra](experiments/A/A.kra), [A.psd](experiments/A/A.psd), [B.kra](experiments/B/B.kra), [B.psd](experiments/B/B.psd). Bản PSD thử chỉ chứa raster của từng slot. Việc xuất thành công không chứng minh nguồn đã đủ điều kiện thay đồ hoặc chuyển động.

Tài liệu đối chiếu: [Krita group layers](https://docs.krita.org/en/reference_manual/layers_and_masks/group_layers.html), [Unity Sprite Swap](https://docs.unity3d.com/Packages/com.unity.2d.animation@16.0/manual/SpriteSwapIntro.html). Cách tổ chức chi tiết ở đây là quy ước của dự án, không phải tuyên bố có một chuẩn Krita duy nhất cho mọi studio.
