# Gốc và bóng anh đào — 13/10/2026

Phạm vi: chặn chân người ở thân/rễ cây anh đào và bỏ mép thẳng ngang dưới bóng gần trưa. Không đổi vị trí cây, tán, bán kính chân, thuật toán di chuyển hoặc artwork. [Ảnh scene và build tích hợp hiện hành](../CanopyAndGrass/README.md).

## Nguyên nhân đã kiểm tra

Footprint cũ của cây có mép thấp ở `y=.053`, rộng từ `x=-.356` đến `.508` đơn vị tại scale 1. Các rễ vươn ngang và vùng sát gốc nằm ngoài contour này; chân bán kính `.28` vẫn có thể đứng ở cạnh trước của rễ trái/phải.

Render GPU isolated bằng `SunShadowMesh` và đúng sprite cây tái hiện mép ngang ở 11:30 và trưa. Bỏ `baseCutoff=.06` cho diện tích và ảnh giống baseline, nên cutoff không gây ra mép trong ảnh này. Tăng riêng contact band lên `.25` vẫn để lại mép ngang. [Probe và ảnh baseline](probe.json) ghi run ngang dưới silhouette dài 59 px lúc 11:30 và 54 px lúc trưa, ở khung 768 px.

Phép chiếu cũ cho thân thấp dùng cùng chiều sâu/bề rộng với tán: sau contact band `.10`, lưới đi từ điểm chân xuống vùng âm sâu rồi quay lên theo chiều cao. Hàng lưới ở chỗ gập này tạo mép ngoài gần như phẳng. `width=1.3` của cây lớn hơn bề rộng sprite, nên clamp `sqrt` ở cạnh không phải nguyên nhân trong mẫu này.

## Thay đổi

- `ArtPresentationBuilder.ApplyTreeGrounding()` ghi contour convex 8 điểm bao thân và rễ bằng Unity API, vào prefab cây cùng hai instance trong `MeadowMainMap`. Vùng bên tán vẫn đi được; không sửa cảnh bằng tay.
- Cây giữ contact band `.10`, `baseCutoff=-1`, dùng profile tán `depth=.64`, `center=.49`, `canopyBlueRatio=.65`. Shader chọn hoa hồng khỏi gỗ nâu và viền rễ tối trong palette nguồn, giữ contour tán bằng phép chiếu affine; khối thân hẹp nối tán về chân trong cùng mesh/pass. Rễ sát đất không còn tham gia silhouette tán. Ngưỡng màu phụ thuộc artwork, cần chỉnh lại khi đổi palette.
- `ArtPreviewChecks.CheckTreeGrounding()` kiểm tra root, hai cạnh trước rễ, vùng đi dưới tán và tiếp cận từ phía dưới. `CheckUnifiedShadows()` có thêm sprite cây thật ở 08:00, 11:30, 12:00, 16:00; kiểm tra nối chân và độ dài mép ngang dưới contour, đồng thời lưu ảnh.

## Kiểm chứng

- Compile C# cuối: 0 lỗi, 0 cảnh báo.
- `ApplyTreeGrounding()` đã ghi dữ liệu bằng Unity API. [14 kiểm tra footprint đạt](collision-checks.json): rễ trái/phải chặn chân, gốc nằm trong contour, đi dưới tán được và tiếp cận phía dưới dừng trước rễ. Khoảng cách hai điểm từng lọt tới contour mới là `.218`/`.140`, nhỏ hơn bán kính chân `.28`; hai điểm dưới tán cách khoảng `.70`, vẫn mở.
- `CheckUnifiedShadows()`: 69 kiểm tra đạt, gồm 49 mốc giờ, lớp bóng chồng nhau, lật hướng, bốn ảnh cây thật và ba kiểm tra riêng về loại pixel gỗ/viền rễ khỏi tán và giữ bóng thân. [Dữ liệu cây](shadow-checks.json) ghi mép ngang dài 6 px tại cả bốn mốc, trên bề rộng 289–306 px. Vùng bóng chính chứa hơn 99% pixel, cách điểm chân tối đa `.018` đơn vị; gần trưa khoảng cách bằng 0. Số đo mép đơn thuần trước đó không bắt hết lỗi rễ trong ảnh scene; các kiểm tra pixel và nghiệm thu scene được dùng cùng nhau.
- Đã xem [11:30](tree-11.5.png) và [trưa](tree-12.0.png), giữ contour cụm hoa; [sáng](tree-8.0.png) và [chiều](tree-16.0.png) lưu cùng thư mục. Kiểm tra scene chính ở sáng, 11:30 và chiều nằm trong [lượt tích hợp tán/cỏ](../CanopyAndGrass/README.md). Capture và các probe nằm ngoài `Assets`.
- `git diff --check` đạt trên host. Sandbox chặn temp của Git LFS sau đợt dọn repo; retry nguyên lệnh đã thành công.

Lệnh tích hợp:

```sh
unity recompile --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --json
unity command eval 'LinhThu.Editor.ArtPresentationBuilder.ApplyTreeGrounding();' --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --json
unity command eval 'LinhThu.Editor.ArtPreviewChecks.CheckTreeGrounding();' --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --json
unity command eval 'LinhThu.Editor.ArtPreviewChecks.CheckUnifiedShadows();' --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --json
```

Đã tải lại scene chính và bấm cho nhân vật đi vào gốc cây ở 11:30. Chân dừng ngoài footprint: [tọa độ và va chạm](scene-walk.json), [ảnh scene](scene-1130.png). Đã xem cây/đá/nhân vật và viền bóng trong ảnh scene cùng bốn ảnh GPU; không dùng compile thay nghiệm thu hình ảnh.

[Build trước lượt chỉnh tán/cỏ](build.json) đạt `Succeeded`, 0 lỗi, 2 cảnh báo có sẵn của Pipeline/TextMeshPro, 105,474,584 byte. Build và ảnh trình duyệt mới nằm trong [CanopyAndGrass](../CanopyAndGrass/README.md). [Kiểm tra thao tác thanh giờ](../TimeScrub/README.md).

Lượt trước đã giữ 11:30 và bấm vào gốc trong trình duyệt: [ảnh web trước chỉnh tán/cỏ](web-final.jpg). [Console và thao tác thanh giờ](../TimeScrub/browser-check.json) không ghi nhận lỗi/cảnh báo.
