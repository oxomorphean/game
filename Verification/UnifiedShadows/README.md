# Bóng mặt trời dùng chung — 13/10/2026

Cây, đá, bụi/cỏ/hoa và nhân vật dùng cùng `SunShadow.cginc` và `DaylightShadow.mat`. `DaylightPresentation` điều khiển ánh sáng toàn cảnh: mặt trời mọc trái, lặn phải; bóng sáng ngả phải, trưa thu về chân, chiều ngả trái. Các lớp nhân vật và vùng bóng chồng nhau chỉ tô một lần bằng stencil bit 64.

## Dữ liệu artist chỉnh

`SunShadowSettings` nằm trên `ModularMapProp`, definition nhân vật hoặc `AuthoredHeroView`. Chiều cao tham chiếu lấy từ kích thước sprite prop hoặc từ definition nhân vật; mọi lớp của một nhân vật dùng cùng chiều cao và điểm chân. Các giá trị còn lại là tỷ lệ so với chiều cao đó.

| Loại | Height scale | Width | Depth | Center | Contact height | Base cutoff | Canopy blue ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Cây anh đào | 1 | 1.3 | .64 | .49 | .10 | -1 | .65 |
| Đá | 1 | 1.4 | .65 | .28 | .10 | -1 | 0 |
| Bụi cây | 1 | 1.4 | .50 | .40 | .10 | -1 | 0 |
| Cỏ, hoa, nhân vật | 1 | .75 | .40 | .52 | .12 | -1 | 0 |

`Height scale` điều khiển độ cao suy ra; `Width` và `Depth` tạo thể tích vùng bóng trưa; `Center` đặt tâm phần khối trên cao; `Contact height` giữ vùng thấp bám đất; `Base cutoff` loại phần rễ thấp không cần chiếu. Nhân vật chính dùng chiều cao tham chiếu 1.35, mẫu modular 1.2, mẫu production 1.3 đơn vị trước scale.

`Canopy blue ratio` khác 0 chọn tán hồng có kênh xanh lam đủ cao khỏi thân/rễ nâu và viền rễ tối trong palette anh đào hiện hành. Tán dùng phép chiếu affine để giữ dáng cụm hoa; khối thân hẹp nối về chân trong cùng pass. Ngưỡng phụ thuộc màu nguồn, không phải mask dùng được cho mọi loại cây. Profile bằng 0 giữ phép chiếu silhouette đầy đủ cho đá, bụi và nhân vật.

Menu `Demo → Mỹ thuật → Thống nhất bóng mặt trời` ghi thông số vào asset bằng Unity API. Việc nhận diện tên asset chỉ có trong bước authoring này; runtime đọc dữ liệu đã lưu. Các ellipse phụ của prop đứng đã được bỏ. Khi nhập asset mới, đặt root tại chân, dùng material chung và chỉnh thông số theo khối; không thêm nhánh shader riêng.

## Kiểm chứng

- `ArtPreviewChecks.CheckUnifiedShadows()`: [69 kiểm tra đạt](checks.json), gồm shader, cấu hình asset và render GPU qua 49 mốc từ 06:00 đến 18:00. Kiểm tra bóng còn diện tích, nối điểm chân, đảo phía theo mặt trời; lớp trùng nhau không tăng độ đậm và lật sprite không đảo hướng nắng. Ba kiểm tra riêng xác nhận pixel gỗ nâu/viền rễ tối không vào bóng tán và khối thân vẫn đổ bóng. Sprite cây thật được kiểm tra thêm ở 08:00, 11:30, 12:00 và 16:00. [Bản sửa gốc/bóng cây](../TreeGrounding/README.md); [ảnh scene và build tích hợp hiện hành](../CanopyAndGrass/README.md).
- `ArtPreviewChecks.CheckSunAndShore()`: 10 kiểm tra đạt về nắng và mép hồ. `ArtPreviewChecks.Run()`: 18 kiểm tra đạt về di chuyển, ánh xạ hướng, scene và map.
- `HeroProductionChecks.BeginRuntime()`: [37 kiểm tra đạt](../HeroProduction2D/runtime-checks.json), hai bộ đồ × tám pose chạy, đổi đồ giữ phase animation, lật hướng và điểm neo bóng. Đã xem ảnh `ivory-run-3.png` và `mirrored-run.png` trong thư mục đó.
- Đã thao tác WASD trong scene chính. Ảnh thực tế đã xem: [sáng](morning.png), [11:30](hero-1130.png), [trưa](noon.png), [chiều](afternoon.png). Capture local được giữ ngoài `Assets` theo quy tắc lưu trữ.
- Biên dịch C# đạt 0 lỗi/0 cảnh báo. Sau sửa callback `OnValidate`, mở và chạy scene chính không phát sinh lại cảnh báo thay đổi bounds trong lúc Unity kiểm tra asset.
- [Build WebGL](build.json) lúc 20:01 ngày 13/10 đạt `Succeeded`, 0 lỗi, 5 cảnh báo có sẵn của Pipeline/TextMeshPro, 105,533,313 byte. Pipeline runtime không được đóng vào player; các cảnh báo TextMeshPro liên quan pragma cũ và cách chia file C++ khi biên dịch.
- [Bản local](http://127.0.0.1:8791/) đã tải lại đúng asset build mới, thử đi và đổi hướng bằng W/D. [Console trình duyệt](browser-check.json) ghi nhận 0 lỗi/0 cảnh báo; [ảnh sau di chuyển lúc 13:00](web-main.jpg) cho thấy bóng theo chân và đổi phía khi mặt trời qua trưa.

## Giới hạn

Phép chiếu 2.5D suy ra khối từ silhouette; không tái tạo chiều sâu 3D thật. Asset khác tỷ lệ cần artist chỉnh dữ liệu và xem trong scene. Bộ render GPU dùng silhouette kiểm tra đơn giản; đánh giá thẩm mỹ dựa riêng vào ảnh scene. Mẫu production đã được kiểm tra nhưng chưa thay nhân vật chính trong build. Scene Cartoon cũ chỉ được kiểm tra compile và thiếu script, chưa nghiệm thu hình ảnh riêng trong đợt này.
