# Các lớp mặt — lượt thử 16/10/2026

Chủ dự án yêu cầu thử mắt, miệng và lông mày riêng, tiếp tục quy trình AI → Krita → Unity của lượt trang phục.

## Phạm vi và cách thực hiện

Trước lượt này, HeroBase có đầu/mũi cố định; slot eyes/mouth trống, palette amber/jade đã có trong dữ liệu. Bổ sung mắt mở hai màu, mí nhắm, miệng bình thường/hô chiêu, hai bên lông mày và lớp dấu mặt nhẹ. Lông mày phải có hai pivot riêng mới nhíu đối xứng được; xoay cả cặp chung một pivot không đạt hiệu ứng đó.

- Artwork: AI tạo toàn bộ nét mặt trên ảnh đầu tham chiếu; Krita tách, làm sạch da thừa và đưa về bind canvas. Không giả định AI giữ pixel chính xác hoặc màu da là mảng phẳng.
- Client: `OutfitConfigSO`, `FacialExpressionPresenter`, `ActorView`, prefab HeroBase và variant PlayerCultivator; tái sử dụng `ModularEquipmentRenderer` hiện có. Mắt chớp độc lập; miệng/lông mày phản hồi cast/hit đã commit. Vẫn dùng Y cho palette amber/jade; chưa thêm shop/UI chọn từng loại miệng/mày hoặc ID/save mới.
- Invariant: không đổi `Head.png`, mũi, tóc/đồ, controller/clip, pose cơ thể, Visual 0.625 hoặc luật Domain. Biểu cảm tạm thời không ghi vào save/config asset.
- Kiểm chứng: swap mắt/đồ giữa blink/cast giữ pose và clip; blink không đổi miệng; biểu cảm tự trả về rest; hidden basic không lộ lớp mặt; dữ liệu thiếu sprite hoặc lệch bind phải bị từ chối. Kiểm hình ở Game view với cả tóc và hướng flip.
- Ngoài phạm vi: nhiều góc artwork, lip sync, Spring Bone, thay đổi schema/save, tải phần mềm/model, commit/push.

Bản khôi phục ngoài checkout: `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261016-face-features/`, gồm 839 file trong `source-before-face.tar.gz`, manifest SHA-256 và trạng thái Play. SHA-256 archive: `ad749fb617313b0b1c65476e4d705415b34d87d7f1f2e2316046bf79367ce718`. Hoàn nguyên theo manifest riêng các file của lượt này; không reset toàn repo.

Tách attachment/slot là hướng hỗ trợ phối nhiều mảnh và dùng chung animation, không phải quy chuẩn bắt buộc mọi tựa game: [Spine skins](https://esotericsoftware.com/spine-skins#Mix-and-match). Chưa có bằng chứng về pipeline nội bộ của từng game được trích trong gợi ý AI.

## Trạng thái

Đã nhập và chạy trên MainGame; mẫu mặt và thông số biểu cảm là lượt thử để chủ dự án nhận xét, chưa phải phê duyệt mỹ thuật cuối.

- 8 PNG ở `Assets/_Project/Art/Characters/HeroBase/Face/`: Eyes_Jade, Eyes_Amber, Eyes_Closed, Eyebrow_Front, Eyebrow_Back, Mouth_Rest, Mouth_Call và Face_Mark. Tất cả nằm trong dependency của MainGame.
- Rig từ 25 lên 28 slot; ba slot thêm là hai lông mày và dấu mặt. Mắt/mày/miệng ở dưới lớp tóc. Basic vẫn giữ 16 phần cơ thể, không hiện lớp mặt.
- Hai outfit classic/jade dùng chung bộ nét mặt; **Y** đổi màu mắt, **H** đổi tóc, **Tab** đổi áo. Chớp mỗi 3.2 giây trong 0.14 giây; mỗi mày nhíu 12° theo hai chiều. Đây là tuning mẫu. Khi thiền/chết thì nhắm mắt; cast/hit dùng miệng gọi chiêu, hit nhắm mắt. Không có lip sync.
- Presenter nằm trên PlayerCultivator và đọc ActorView. Nó chạy trước ActorActionFeedback trong LateUpdate để tàn ảnh Dash lấy cả mặt. Bind/Unbind/disable trả biểu cảm về nghỉ. Thay đồ/màu mắt giữ pha blink và thời điểm clip.

## Nguồn chỉnh sửa

`/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261016-face-features/Source/`:

- `hero-face-master.kra`: đầu gốc khóa riêng; nhóm mặt có mắt jade/amber/nhắm, từng mày, miệng nghỉ/gọi chiêu, dấu mặt; tóc bạc là nhóm tham chiếu ẩn. Không lưu master trong Assets/Git.
- `neutral-generated.png`, `calling-generated.png`: ảnh nguồn do **imagegen tích hợp** sinh trên head-reference. Không dùng API trả phí riêng, không cài Krita AI Diffusion/ComfyUI/model.
- `face_in_krita.py`: thao tác tách và export bằng Scripter của Krita; `face-parts.json` chứa pivot và SHA-256. Canvas xuất 1087×1446, PPU 500. Mỗi mày có pivot riêng; các biến thể mắt/miệng dùng cùng bind coordinates.
- `face-expressions.png`: bảng ghép để so nét, không phải ảnh chụp runtime. Hình runtime đã kiểm trực tiếp trong Unity.

AI đã thay đổi kích thước ảnh nguồn và có tô lại da. Chỉ lấy nét mặt từ ảnh nguồn, căn lại và loại da thừa trong Krita; `Head.png`, mũi, cơ thể, tóc và đồ của lượt trước không bị sửa. Đây là lý do không coi inpaint là bảo đảm khớp 100%.

Tóm tắt bộ prompt dùng cho lượt thử:

1. Edit head-reference: giữ đầu trọc, tai, silhouette, shading và mũi; thêm cặp mắt anime xanh ngọc theo phối cảnh 3/4, mắt xa hẹp hơn, nhìn sang phải, lông mày riêng có khe với mí, miệng cười nhỏ khép, má hồng nhẹ; không tóc/thân/chữ, ngoài đầu trong suốt.
2. Cùng tham chiếu: giữ đầu/mũi; cặp mí nhắm riêng, mày tách biệt, miệng hô chiêu nhỏ với lòng đỏ nâu, một phần răng trên và lưỡi hồng dịu; không tóc/thân/chữ, ngoài đầu trong suốt. Mắt hổ phách được đổi màu iris từ bản xanh trong Krita, giữ nguyên nét và lòng trắng.

## Kiểm chứng ngày 16/10/2026

- Unity 6000.6.4f1: compile **0 lỗi, 0 warning**. EditMode **285/285 đạt**, 0 failed/skipped/inconclusive, 19.8 giây. Log tại `/private/tmp/nguthu-core-foundation/20261016-face/editmode-results.json`.
- Sáu case mặt mới: blink chỉ đổi mắt; cast/hit và mày hai pivot trả về rest; đổi palette/outfit giữa blink/cast giữ pose/controller/clip; rebind/basic không lưu lớp biểu cảm; thiếu sprite hoặc sai bind bị từ chối trước khi đổi rig. Bộ kiểm thử cũ về gameplay/wardrobe vẫn đạt.
- Game view native: xem classic/ink mắt xanh mở, jade/silver nhắm mắt giữ miệng nghỉ, jade/silver mắt amber/hô chiêu từ `GameBootstrapper.Cast` thành công với hướng trái; hit flash áp dụng toàn rig. Camera cận 1.35 và camera chơi 5.6 đã xem. Không build WebGL/headless. Hình cần chủ dự án nhận xét trực tiếp, đặc biệt nét mặt khi zoom xa.
- Khôi phục world trước lượt thử bằng snapshot: JSON sau restore bằng trước (`worldEqual=true`), save trên ổ đĩa không đổi (`diskUnchanged=true`), camera 5.6, Play tiếp tục không pause, scene không dirty. Bằng chứng tại `restoration-report.json` trong thư mục backup.
- Console hiện tại: 0 error/0 warning, compilationFailed=false. Lịch sử Pipeline có ghi các lỗi authoring đã xử lý (kích thước ảnh nguồn, tên thuộc tính trong test, vị trí ActorView trên variant); không dùng lịch sử này làm kết quả runtime hiện tại.
- Cuối lượt: quét Assets sau refresh không có orphan meta, missing meta hoặc thư mục rỗng; 8/8 sprite mặt có dependency từ MainGame. Index trước task được bảo toàn. Không commit/push.

## Ghi nhận cho lượt sau

Tách nét mặt sau khi AI nhìn toàn bộ đầu cho tỷ lệ dễ ghép; vẫn cần kiểm mép alpha và vị trí trên tóc trong game. Giả định ảnh AI giữ đúng kích thước là sai; đã chuẩn hóa và căn lại trước export. Chưa có menu chọn từng kiểu miệng/mày/dấu mặt, chưa có nhiều kiểu hình mắt ngoài hai màu. Có thể tiếp tục trên chính các slot và master này sau khi nhận xét mẫu.

## Chỉnh tỷ lệ mắt mở — phản hồi tiếp theo ngày 16/10

Chủ dự án thích chớp mắt nhưng thấy mắt mở quá lớn hoặc chưa đúng vị trí. Chỉ sửa `Eyes_Jade.png` và `Eyes_Amber.png` trong Krita rồi nhập lại vào đúng asset; không sửa presenter, timing chớp, mày, miệng, tóc hoặc rig.

- Mắt gần: ngang ×0.88, cao ×0.78, dịch phải 6 px và xuống 6 px quanh tâm (535,320). Mắt xa: ngang ×0.90, cao ×0.78, dịch trái 3 px và xuống 7 px quanh tâm (679,314). Tọa độ trên canvas 1087×1446, gốc phía trên trái. Đây là tuning để nhận xét, chưa phải phê duyệt mỹ thuật.
- Giữ canvas, PPU 500, pivot (550,991) theo Unity và GUID. Mí nhắm giữ nguyên byte; mắt mở mới có đáy gần vị trí mí nhắm cũ. Hai layer trong master KRA và script tái tạo nguồn đã đồng bộ.
- Bảng so sánh trước/mới/chớp mắt: `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261016-eye-proportions/Source/eye-before-after.png`. Đây là bản ghép artwork; đã kiểm riêng Game view với classic/ink mắt xanh, jade/silver chớp mắt, jade/silver mắt amber khi cast. Không đổi pose/animation để sửa lỗi hở eo trong lượt này; chẩn đoán và đề xuất nằm ở [WARDROBE_PILOT.md](WARDROBE_PILOT.md#hở-khớp-khi-tấn-công--chẩn-đoán-và-đề-xuất-1610).
- Unity: compile up to date, 0 lỗi/0 warning; EditMode **285/285 đạt**, 0 failed/skipped/inconclusive, 19.15 giây. Log: `/private/tmp/nguthu-core-foundation/20261016-eye-fit/editmode-results.json`.
- Snapshot ngoài checkout: `20261016-eye-proportions/source-before-eye-fit.tar.gz`, 831 file, SHA-256 `ed96889bcab48ec902cf691ad72773cbc7e6beef0d54a534efddfbd01cc64f86`. World sau restore bằng snapshot (`worldEqual=true`), save không đổi (`diskUnchanged=true`), camera 5.6, scene không dirty. Play tiếp tục, Animator và hai presenter được bật lại.

Lượt này tận dụng nét mắt hiện có và chỉnh tỷ lệ riêng từng mắt, không sinh lại cả mặt bằng AI. Nghiệm thu độ đẹp vẫn cần chủ dự án xem ở khoảng zoom thường dùng; kiểm thử tự động không đánh giá thẩm mỹ.
