# Kiểm chứng logic 02 — 06/10/2026

Scene `Assets/_Project/Content/Scenes/ThanDinhPrototype.unity`, Editor 6000.6.4f1. Bản chơi local tại `http://127.0.0.1:8789/#choi`; xưởng tại `#dashboard`. Dùng Unity plugin CLI để biên dịch, chạy và build. Không tạo asset mỹ thuật mới; font Noto hiện có được bổ sung glyph cho các nhãn mới.

## Kiểm tra đã chạy

| Phạm vi | Kết quả | Bằng chứng |
| --- | --- | --- |
| Hồi quy thu phục/bồi dưỡng/trồng trọt/nghề/đột phá/offline | 33 đạt, 0 lỗi | [domain v1](../LogicV1/domain-tests.json) |
| Theo sau/đổi hướng/đi-nghỉ/tự vệ/thú hoang/thả-nhặt/ăn mồi/giữ chiến lợi phẩm | 33 đạt, 0 lỗi | [domain v2](domain-tests.json) |
| Chuyển v1→v2, giữ tiến trình, từ chối save hỏng, không lặp offline | 16 đạt, 0 lỗi | [save](save-tests.json) |
| HUD nền: input, modal, dừng khi mở xưởng | 7 đạt, 0 lỗi | [HUD v1](../LogicV1/hud-tests.json) |
| Nút realm/Đóng/đi trong realm/thả số lượng/nhặt/máu/chữ không tràn | 19 đạt, 0 lỗi | [HUD v2](hud-tests.json) |
| Font nhúng và tiếng Việt | 237 ký tự, 3.792 mẫu raster; không thiếu glyph | [font](../LogicV1/font-check.json) |
| WebGL IL2CPP | Succeeded, 0 build errors | [build](build.json) |

Tổng 108 ca được nhúng vào `Assets/_Project/Resources/Project/logic-tests.json` và hiển thị trong xưởng. Builder từ chối báo cáo thiếu/lỗi trước khi nhúng. HUD v2 dùng phiên thử riêng qua nhiều frame, chặn lưu phiên giả và khôi phục phiên thật khi kết thúc. `Time.frameCount` được quan sát tăng; không chỉ chạy một hàm mô phỏng đứng yên.

## Thao tác quan sát trực tiếp

Trên WebGL: mở G thấy bảng Thần Đình và các ô của tiến trình cũ; chọn stack linh thảo, tăng số lượng lên hai, thả, bấm Đóng về map, thấy đồ cạnh thân thể và nút Nhặt, bấm E nhận lại hai linh thảo. Nhân vật và linh thú có thanh máu; thú hoang và thú đồng hành thay đổi vị trí qua các khung hình. Chỉnh chiều cao ô khế ước sau khi thấy dòng HP bị cắt; kiểm tra lại không tràn chữ trước build cuối.

Editor tải tiến trình thật với `stateVersion = 2`, `ValidateState = null`, không có LoadError, và file `.before-logic-v2` tồn tại. Bản web và Editor giữ hai tiến trình riêng. Kiểm tra Console hiện hành sau lượt HUD cuối: 0 errors, 0 warnings; nhật ký Pipeline còn giữ một lỗi từ phiên test HUD cũ đã được sửa bằng việc chờ frame UI trước thao tác kế tiếp.

Fixture legacy ban đầu dùng DTO động của run_script bị JsonUtility bỏ trường lồng nhau. Đã chuyển riêng việc tạo fixture sang Newtonsoft có sẵn trong dự án, xác nhận JSON chứa schema/state/rules v1 trước bài migration. Production codec vẫn dùng JsonUtility và không nới validation.

## Phạm vi và giới hạn

Chỉ kiểm chứng bản local hai vùng, một loài. Chưa triển khai hoặc đo tải server 100–300 người, đồng bộ mạng, tài khoản, giao dịch, chống gian lận hoặc va chạm với các sprite trang trí. Các tham số nhịp chơi là mặc định để chủ dự án thử, không phải cân bằng cuối.

Nguồn trước sửa ở `before/`; hướng khôi phục code/save nằm trong [hướng dẫn logic 02](../../Architecture/LOGIC_V2.md). Không commit/push/deploy trong lượt này.
