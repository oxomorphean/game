# C2 — nhân vật SVG có thân nền và ngoại hình riêng

Mẫu thử ngày 13/10/2026, phát triển từ C theo yêu cầu của chủ dự án. Nguồn có 275 đường vector được vẽ bằng tọa độ Bezier, không nhúng ảnh, không trace tự động và không dùng chỉ tiêu số phần tử. Đây là artwork ứng viên; chưa thay nhân vật chính.

[Bảng hình và kích thước chơi](C2-overview.png) · [Vòng chạy Unity](C2-run-preview.gif) · [Các tư thế kiểm tra](unity-pose-review.png)

## Nguồn và cách sửa

- [C2-vector.kra](C2-vector.kra): nguồn biên tập gồm 52 lớp vector thật, trên canvas 1024 × 1024. Da đầu, thân, tay, chân có phần bên dưới lớp trang phục. Đồ cơ bản là áo ngắn tay và quần soóc riêng.
- [C2-source.svg](C2-source.svg): nguồn vector trước khi nhập Krita; mỗi nhóm có ID lớp và điểm gắn. [parts](parts/) chứa cùng hình học theo từng lớp.
- [C2-layered.psd](C2-layered.psd): bản giao 52 lớp raster; [export](export/) chứa PNG alpha đầy canvas xuất từ Krita. PSD/PNG không thay thế nguồn vector.
- [manifest.json](manifest.json): mẫu `NGUTHU_C2_34_V1`, hướng 3/4 phải, vị trí 19 điểm gắn, thứ tự lớp và quy tắc ẩn phần cơ thể. Tọa độ này thuộc C2; không dùng lẫn với `anchors.json` của ứng viên trước.

Hai bộ đồ có silhouette khác nhau: áo ngà tay rộng và áo ngọc tay gọn. Tóc bạc có búi/dải buộc; tóc tối ngắn bỏ phần búi/dải buộc, giữ cùng dáng mái. Mắt ngọc và mắt hổ phách là hai lớp riêng. Cơ thể, bàn tay, giày và mặt được dùng chung.

Trong Krita, chọn lớp tương ứng rồi dùng công cụ chỉnh shape để sửa contour/fill/stroke. Khi đổi bộ đồ, bật các lớp thuộc bộ đó và tắt bộ trước theo `variant` trong manifest; các vùng cơ thể bị áo/quần che được ẩn theo `hidden_by`. Không co giãn riêng từng lớp để ráp cho vừa. Lưu phiên bản KRA mới trước khi thay tỷ lệ hoặc vị trí khớp. `build-source.py` và `import-krita.py` phục vụ tái lập bản thử; không chạy đè lên nguồn đã biên tập.

Sau khi lưu chỉnh sửa vào `C2-vector.kra`, chạy `finish-krita.py` trong **Tools → Scripts → Scripter**. Script đọc từng lớp từ KRA vừa lưu, cập nhật 52 PNG, hình phối đồ và PSD; không ghi lại KRA. Giữ tên lớp và canvas theo manifest. Chỉnh sửa chưa lưu trong cửa sổ Krita chưa được xuất.

## Thử trong Unity

Mở `Assets/_Project/Content/Characters/HeroC2/HeroC2Trial.unity`, hoặc menu **Demo → Character C2 → Open separate trial**, rồi Play. Các nút đổi áo, tóc, mắt, đứng/chạy và lật hướng chỉ điều khiển ngoại hình.

`HeroC2.prefab` dùng SpriteRenderer gắn vào các khớp chung và Animator với hai asset `Idle.anim` / `Run.anim`. Đây là rig cutout xoay các phần cứng; chưa có mesh/SpriteSkin để làm mềm chỗ gập vải. Root đứng yên; nhịp chân, tay và vạt áo nằm trong clip chỉnh được bằng Animation window. Điểm gắn tóc phụ được giữ trong manifest, nhưng tóc hiện đi cùng đầu.

Menu **Build and verify separate trial** nhập PNG từ Krita và dựng lại riêng asset C2. Dừng Play và lưu scene thử trước khi chạy. Nếu C2 đang mở cùng scene khác, builder từ chối trước khi thay asset; mở riêng C2 để dựng lại. Builder không thay build settings, scene chính hoặc nguồn baseline. Không chỉnh trực tiếp asset sinh tự động rồi chạy builder mà không lưu bản riêng.

## Bằng chứng

| Kiểm tra | Kết quả và phạm vi |
| --- | --- |
| Nguồn mở lại | PASS: KRA 52 lớp vector; PSD 52 lớp raster. [krita-checks.json](krita-checks.json) |
| Canvas / SVG | PASS: mọi phần 1024 × 1024, ID không trùng, không ảnh nhúng. [verification.json](verification.json) |
| Lưu KRA / màu | Sai khác mép khử răng cưa sau mở lại tối đa 4/255 trên nền xám, không phải pixel-identical. PSD mở lại khớp bản xuất raster. |
| Clip và ghép phần | 144 ảnh render: 6 tổ hợp áo–tóc × 24 phase Run; Idle và lật trái/phải đã xem trong Editor. [unity-checks.json](unity-checks.json) |
| Đổi khi chạy | PASS: 12 tổ hợp áo–tóc–mắt trong Play mode giữ phase, ma trận khớp và root; Animator tiếp tục tiến. Dùng `Animator.Update` với bước thời gian chủ động. [unity-play-mode-checks.json](unity-play-mode-checks.json) |
| Xuất chỉnh sửa Krita | PASS: trên bản sao KRA, opacity lớp mắt đổi từ 255 sang 96 xuất đúng vào cả PNG và PSD; KRA không bị script ghi lại. [krita-handoff-check.json](krita-handoff-check.json) |
| Dựng lại scene | PASS: builder từ chối khi có scene khác mở cùng C2, giữ scene đó và đối tượng chưa lưu. [unity-rebuild-check.json](unity-rebuild-check.json) |
| Baseline | PASS: SHA-256 nguồn KRA, PSD và scene HeroProductionTrial giữ nguyên. |

GIF là các frame do Camera Unity render từ clip Run, không phải bản quay chuột hay ảnh AI sinh riêng từng frame. Đã xem hình đứng, các phase chân/tay và việc đổi đồ trực tiếp trong Play mode. Sau khi thấy chân gập quá sâu làm ống quần chồng cục, đã giảm bước, độ nhấc chân và hạ hông. Hình chạy hiện đọc được, nhưng khớp gối/khuỷu vẫn có độ cứng của cutout; chưa đánh giá đạt chất lượng animation cuối.

## Giới hạn của mốc này

Chỉ một góc 3/4 và phản chiếu ngang, chưa đủ tám góc. Chưa thêm kiếm, mũ, biểu cảm/chớp mắt hoặc animation hành động. Chưa có thử chuyển động trên map, bóng tiếp xúc riêng hoặc đo trượt chân khi nhân vật di chuyển trong thế giới. Texture toàn canvas không nén phục vụ kiểm tra tọa độ; cần atlas và tối ưu vùng trong suốt trước khi dùng hàng loạt. Chưa build WebGL cho C2; scene Unity và GIF là kết quả kiểm chứng của mốc này.

Đánh giá mỹ thuật: nét và silhouette dễ kiểm soát hơn C, cơ thể có thể dùng lại, trang phục không đổi đầu. Khối tóc và nếp vải vẫn cứng hơn ứng viên A; chưa kết luận C2 đẹp hơn A. Chỉ mở rộng catalogue sau khi duyệt hình, làm mềm khớp cần thiết và kiểm tra lại những động tác được hỗ trợ.
