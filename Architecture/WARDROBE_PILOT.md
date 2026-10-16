# Lượt thử tóc và trang phục — 16/10/2026

Chủ dự án yêu cầu thử quy trình AI → chỉnh/tách lớp → dùng trên rig hiện hành, chưa tải thêm phần mềm hoặc model. Phạm vi là hai bộ đồ và hai kiểu tóc để nhận xét; chưa phải phê duyệt mỹ thuật cuối.

**Cập nhật nối khớp 16/10:** đề xuất ở cuối tài liệu đã được chủ dự án cho phép triển khai. HeroBase hiện có 15 SpriteSkin và 41 sprite có mesh/trọng số tương thích cho cả ba outfit; xem [kết quả hiện hành](HERO_JOINT_SKINNING.md). Các mô tả cutout/0 SpriteSkin dưới đây ghi lại trạng thái của lượt thử trước khi sửa nối khớp.

## Kết quả và cách xem

- `classic`: áo xanh đen, viền ngà, đai ngọc; `jade`: áo xanh ngọc, quần teal và giày nâu.
- `ink`: tóc đen búi; `silver`: tóc bạc buộc ngắn. Hai bộ dùng chung hai kiểu tóc, có thể đổi chéo.
- MainGame dùng các asset này qua `OutfitConfigSO` và `ModularEquipmentRenderer`. Play rồi nhấn **Tab** đổi đạo bào, **H** đổi tóc. `basic` vẫn giữ phôi nam trống; khuôn mặt đã được bổ sung ở [lượt thử tiếp nối](FACE_FEATURES.md), Y đổi mắt.
- 32 PNG mới trong `Assets/_Project/Art/Characters/HeroBase/Wardrobe/`: mỗi outfit 13 mảnh, mỗi tóc 3 lớp. Cấu hình classic/jade và HeroBase prefab được cập nhật bằng Unity API; không sửa luật Domain.

## Quy trình đã làm

1. Dùng imagegen sẵn có tạo ảnh nguồn mặc trang phục trên tham chiếu phôi nam, rồi tạo tóc nền trong suốt. Không cài Krita AI Diffusion, ComfyUI, Stable Diffusion hoặc Photoshop.
2. Căn lại ảnh nguồn về khung phôi, tách áo/thân/tay/quần/giày và tóc trước/sau/phụ kiện trong Krita; bổ sung vùng chồng ở các khớp. Đầu và hai bàn tay lấy nguyên từ phôi đã duyệt. AI không tự bảo đảm khớp pixel 100%; lượt thử này vẫn cần căn và sửa lớp.
3. Xuất PNG alpha toàn canvas 1087×1446, PPU 500; kế thừa pivot theo từng sprite cơ thể, tóc theo pivot đầu. Cổ tay áo vẽ phía trên đầu khớp bàn tay.
4. Gắn sprite vào các slot của bộ xương Transform có sẵn. Giữ GUID của config/prefab, controller, clip, đường dẫn xương, scale xương 1 và Visual 0.625. Không rig lại, không tạo animation AI từng frame, không dùng SpriteSkin/mesh deformation.

## Nguồn chỉnh sửa và khôi phục

Thư mục ngoài checkout: `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261016-wardrobe-pilot/`.

- `Source/hero-wardrobe-master.kra`: master Krita có nhóm nguồn ẩn, phôi, hai outfit, hai tóc và guides pivot; mặc định bật classic/ink.
- `Source/Exports/`, `wardrobe-parts.json`: sprite và manifest SHA-256; `Source/wardrobe_in_krita.py` là script authoring chạy qua Scripter tích hợp của Krita. Đổi nhóm outfit/tóc để xem và sửa trong master.
- `source-before-wardrobe.tar.gz` cùng `manifest-sha256.json`: 773 file trước lượt thử, gồm thay đổi chưa commit. SHA-256 archive: `216d5554ba3894bd8e9aa0077b8cb3196362ddf2fa25a07d0aef3b6673aa512b`.
- `runtime-before.json`, `runtime-restored.json`, `restoration-report.json`: world đã khôi phục bằng DTO và so sánh toàn bộ bằng nhau; save trên đĩa không đổi. Camera trả 5.6, Play tiếp tục như trước, scene không dirty.
- Muốn hoàn nguyên riêng lượt thử: dùng manifest để lấy lại hai config outfit, HeroBase prefab, test và tài liệu bị đổi; gỡ reference rồi xóa Wardrobe bằng Unity API. Không reset cả working tree vì còn công việc có sẵn.

## Kiểm chứng

- Unity compile: hoàn tất, `compilationFailed=false`, danh sách lỗi rỗng. Console hiện hành không có lỗi/warning.
- Unity Test Framework EditMode: **279/279 đạt**, 0 failed/skipped. Có kiểm tra 13 mảnh mỗi bộ khác phôi/khác nhau, chung kích thước/pivot/PPU, tóc dùng chung hai bộ, phôi basic vẫn đủ 16 mảnh; đổi mọi tổ hợp outfit/hair/eye không đổi pose, thời điểm clip hay material chung. Kết quả tại `/private/tmp/nguthu-core-foundation/20261016-wardrobe/editmode-results.json`.
- Đã xem trực tiếp trong Game view: classic/ink đứng, classic/silver chạy hướng trái, jade/ink ở nhịp chạy khác, jade/silver thiền và classic/ink câu cá; thử phím di chuyển, Tab, H và Space. Xem ở camera chuẩn và zoom tạm. Đây là kiểm tra các mẫu tư thế, không phải chứng minh mọi frame và mọi góc khớp đều hoàn hảo.
- Sau AssetDatabase refresh: 32/32 sprite mới có dependency từ MainGame; 0 meta mồ côi, 0 asset thiếu meta (không tính dotfile của hệ điều hành), 0 thư mục tài nguyên rỗng. 8 file có sẵn thay đổi trong phạm vi; không mất file của baseline và Git index giữ nguyên. Master KRA kiểm CRC đạt, lớp nền giấy ẩn, PNG khớp SHA-256 trong manifest.

## Giới hạn lượt thử

Tóc/vạt áo chưa có Spring Bone. Vùng nối hiện đã dùng mesh mềm theo bản cập nhật đầu tài liệu; vạt áo vẫn theo bộ xương cơ thể. Mẫu để chủ dự án nhận xét về nét, màu, độ dài tóc/áo và cảm giác chuyển động trước khi mở rộng số lượng.

## Hở khớp khi tấn công — chẩn đoán và đề xuất 16/10

**Chẩn đoán trước lượt triển khai nối khớp.** Chủ dự án thấy thân trên nghiêng còn thân dưới đứng yên, làm eo và đôi lúc các khớp khác bị tách.

Đã kiểm tra prefab đang chạy: HeroBase có **0 SpriteSkin**. Torso gắn theo chest, shorts/vạt áo theo pelvis; chest nằm ở local (-0.02,0.47,0) so với pelvis. `ActorActionFeedback.Present` xoay chest −12°, upper arm −42°, forearm +28° tại đỉnh cast; không có biến dạng nối giữa các mảnh. Slot belt riêng hiện trống, họa tiết đai nằm trong artwork thân/hông. Với cast thực tế thành công, một điểm trên thân tại cao độ eo bị kéo khoảng 0.061 đơn vị thế giới, tương đương khoảng 49 px trên canvas nguồn, trong khi pelvis không xoay. Game view cho thấy đường đai/thân lệch ở tư thế này. Đây là hạn chế của cutout/bind và vùng chồng ảnh, không phải collider hay vị trí Actor trong Domain.

Các kỹ thuật phù hợp với rig 2D modular:

1. **Chuẩn bị vùng chồng khớp và tâm xoay:** phần bị khuất được vẽ dài thêm, đầu khớp bo tròn, bỏ viền đen nội bộ; vùng chồng phải đủ ở góc cực hạn. Cổ áo/đai/tay áo có thể che đường nối. Chỉ che bằng đai không giải quyết được phôi trống hoặc mọi góc tay/chân.
2. **Mesh có trọng số qua nhiều xương:** phần thấp thân bám pelvis, phần trên bám chest, vùng giữa chuyển dần trọng số. Áo và thân cần cùng bind/weights tương ứng để đổi đồ không lại hở. Unity [SpriteSkin](https://docs.unity3d.com/Packages/com.unity.2d.animation@16.0/manual/SpriteSkin.html) hỗ trợ biến dạng từ xương và trọng số; [Spine weights](https://esotericsoftware.com/spine-weights) mô tả cùng nguyên lý và việc kiểm toàn bộ biên độ chuyển động. Không cần mua Spine hoặc cài model mới để thử trong dự án này.
3. **Sửa tư thế cực hạn:** chỉnh pose, giới hạn xoay không hợp hình, hoặc dùng artwork/mesh hiệu chỉnh riêng nếu cần. Auto weights chỉ là điểm bắt đầu, không chứng minh đã kín mọi khớp.

**Đề xuất cho lượt sửa:** xử lý eo trước bằng vùng chồng đúng và weighted mesh qua pelvis/chest; đánh giá tâm xoay chest khi author pose, tránh đổi bind tùy tiện. Sau khi đạt trên phôi basic và cả classic/jade, xử lý vai, khuỷu, hông và gối theo cùng nguyên tắc. Giữ đường dẫn rig/controller, scale xương 1, Visual 0.625, foot anchor và Domain. Nếu thay bind/pivot, phải đồng bộ sprite/outfit và kiểm lại animation đang dùng. Khi đưa SpriteSkin vào, cập nhật cách `ActorView.VisualBounds` lấy hình đã biến dạng để nhãn/canopy không dùng hình cứng cũ; giữ sprite swap và tàn ảnh Dash tương thích.

Điều kiện nghiệm thu đề xuất: không lộ nền/viền cắt hoặc đai gãy khi đứng/chạy/đánh/Dash/thiền/câu cá, ở cả hai hướng flip, mọi outfit và tại góc khớp lớn nhất; đổi đồ giữa animation không nhảy clip hoặc mất pose. Cần kiểm Game view; bộ test bind/swap hiện tại chưa phát hiện khe hở nhìn thấy giữa hai mảnh. Bằng chứng chẩn đoán: `20261016-eye-proportions/rig-inspection.json` trong thư mục backup ngoài checkout.
