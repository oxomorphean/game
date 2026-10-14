# Kiểm chứng mẫu góc nhìn cao — 07/10/2026

Asset `Assets/_Project/Art/CartoonV2/` gồm hero.png 1322×1190 alpha, huyen-quy.png 1312×1199 alpha, field.png 1672×941 opaque. Các PNG được tạo bằng ImageGen tích hợp; nền được chỉnh một lần để giảm nét cỏ vụn. Prompt và đường dẫn nguồn được lưu tại `ArtDirection/CartoonV2/generation-prompts.json`.

`unity recompile --strict --focus` đạt 0 lỗi/0 cảnh báo. Import và lưu hai scene bằng `HighViewPrepare.Run` qua Editor API. Controller art cũ `CartoonPetRig` đã được thay bằng renderer `CartoonCharacterRig` và controller input/follow `CartoonTrialDirector`; không sửa domain hay save schema.

8 kiểm tra runtime đạt, 0 thất bại trong `high-view-runtime.json`: người/thú dùng hai sprite khác nhau, mỗi SpriteSkin có 6 bone và 475 vertex biến dạng, animation tiến thời gian, click di chuyển thú, người đi theo, phím Tab đổi điều khiển và R đưa cả hai về điểm xuất phát. Tab/R và pointer được thử qua UI Unity thật. Lần click đầu bị công cụ từ chối vì cửa sổ người dùng thay đổi; đã đọc lại UI rồi thử được. Ảnh GameView thực tế: `high-view-unity.png`.

Main logic tải phiên thành công, LoadError rỗng; Inspector binding của player là CartoonV2/hero.png, thú là CartoonV2/huyen-quy.png và nền là CartoonV2/field.png. Có ba CartoonUiRig cho player/thần hồn và thú trong phiên mẫu. Ảnh: `high-view-logic.png`. Main Canvas dùng mesh motion nhẹ, không phải SpriteSkin. Không chạy lại bộ 155 test domain/save cũ vì không sửa domain/save; kết quả lịch sử được builder giữ trong dashboard và không được coi là test mới.

Hai build web thành công, mỗi build 0 lỗi: mẫu art 66,56 giây (`high-view-build.json`), logic 6,11 giây (`high-view-logic-build.json`). Cảnh báo build từ Pipeline runtime tắt trong Player, pragma shader TMP cũ và phân tách file C++ TMP; builder logic có cảnh báo CS0649 của trường đọc bằng JSON. Console runtime của cảnh art và logic tại thời điểm kiểm tra đều 0 lỗi/0 cảnh báo; chi tiết cảnh báo lịch sử trong `high-view-console.json`.

Cổng 8788 và 8789 phản hồi HTTP 200; HTML không còn placeholder và tệp wasm/data/loader hiện diện. Chưa kiểm tra lại UI browser vì công cụ browser từ chối URL localhost theo chính sách URL, không thử đường vòng. Mẫu đã xem và thao tác trong Unity. Hai Python server đang chạy local, không mở public network.

Dung lượng filesystem: CartoonV2 3,32 MiB, bản nén code/scene trước thay đổi khoảng 32 KiB; mỗi web build khoảng 53 MiB. Gói 2D Animation và 2D Common vẫn là 54,6 + 3,8 MiB, không cài thêm package mới. CartoonV1 giữ nguyên để quay lại; pixel/Aseprite cũ vẫn đã gỡ theo yêu cầu trước.

Giới hạn: nền phẳng, không collision; sprite chỉ flip trái/phải, chưa có bộ nhìn lên/xuống hoặc animation tấn công riêng. Đây là mẫu cần duyệt mỹ thuật, chưa phải asset/map sản xuất hoặc benchmark FPS/online. Không commit/push/deploy. `git diff --check` đạt.

Kinh nghiệm của mẫu: tạo PNG theo cùng ảnh/góc nhìn rồi dùng skin giữ được thiết kế qua các frame; prompt cỏ đầu tiên vẫn sinh nhiều nét vụn nên cần một lần chỉnh có chủ đích. Quy tắc cỏ yên mắt và góc nhìn cao đã nằm trong STYLE_GUIDE v4; không nhân rộng khi chưa duyệt mẫu.
