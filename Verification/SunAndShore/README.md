# Nắng, bóng gốc cây và bờ hồ — 13/10/2026

Yêu cầu: bỏ vệt bóng rễ tách dưới cây, cho chân nhân vật đến sát bờ hồ, làm nước lấp lánh và cảnh vật phản ứng với nắng chiều. Scene kiểm tra là `ArtPreview`; không đổi bố cục map hoặc hệ gameplay.

## Thay đổi

- `DaylightShadow` loại phần rễ sát nền khỏi phép chiếu; vùng gần trưa chỉ lấy tán, không còn mảng thân đậm. Sáng/chiều vẫn giữ bóng thân phía trên nối với bóng tán. `DaylightCanvasEffect` dùng cùng quy tắc cho mẫu Canvas còn lại.
- `DaylightPresentation` cung cấp hướng mặt trời, độ cao và mức nắng chiều chung. Shader sprite đổi sắc và độ sáng rộng theo bên nhận nắng; cây/cỏ/hoa/đá, nền và nhân vật cùng nhận màu nắng. Bóng chiều có sắc lạnh nhẹ; mặt trời vẫn ẩn, chỉ có thanh giờ.
- `DaylightSprite` cô lập vùng nước xanh ngọc để tạo gợn phản quang chuyển động; `GardenAmbientArt` bổ sung điểm lóe thưa, đổi sang vàng và dịch theo mặt trời. Cỏ bờ, đá và lá sen giữ đứng yên.
- `ArtPresentationBuilder.ApplyPondShore` lưu contour convex 12 điểm qua Unity API, vào prefab hồ và `MeadowMainMap`. Phần bờ phía trên thấp hơn khoảng 0,3 đơn vị; contour không ôm đỉnh đá/cỏ nhô cao. Bán kính chân người và thuật toán va chạm không đổi. `MeadowMap` nền ảnh cũ giữ nguyên.

## Kiểm chứng

- Compile C#: 0 lỗi, 0 cảnh báo.
- `ArtPreviewChecks.CheckSunAndShore`: 10/10 đạt; kiểm tra tới gần bờ, nước vẫn cản, hướng/độ dài bóng theo giờ, sắc nắng và ba shader. `checks.json` ghi điểm dừng phía bắc cách gốc hồ 0,85 đơn vị.
- `ArtPreviewChecks.Run`: 18/18 kiểm tra đi lại, giới hạn map, tám hướng và scene đạt; báo cáo ở `../ArtOnly/checks.json`.
- Trong Unity Play, đưa nhân vật bằng `PreviewMovement` tới phía bắc rồi giữ phím S thật 900 ms: nhân vật trượt dọc bờ, không vào vùng nước; `shore-input.json` ghi vị trí cuối. Camera zoom chỉ đổi trong Play và đã bỏ khi Stop.
- Đã xem ảnh Game View tại 08:00, 11:30, 12:00 và 17:00. Ảnh do Unity Pipeline lưu ở `Assets/Verification/SunAndShore/`: `tree-morning`, `tree-1130`, `tree-sunset`, `pond-noon`, `pond-sunset`, `shore-player`.
- WebGL build thành công trong 54,38 giây, 0 lỗi, 5 cảnh báo Pipeline/TMP đã có; xem `build.json`. Bản 8791 đã tải và thử WASD tới mép hồ trong trình duyệt. Cây, màu nắng và phản quang nước hiển thị; console 0 lỗi/0 cảnh báo (`browser-console.json`).
- Review diff xác nhận hai prefab chỉ thay contour hồ; scene và vị trí đồ vật không bị ghi lại. `git diff --check` đạt. Không commit hoặc phát hành lên dịch vụ bên ngoài.

Đây là ánh sáng stylized trên sprite 2D: màu và phản quang thay đổi theo mặt trời, không phải mô phỏng chiếu sáng thể tích/3D. Nét sáng tối đã vẽ trong PNG vẫn là một phần của artwork.
