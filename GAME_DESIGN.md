# Hướng thiết kế game

## Ưu tiên hiện hành — xác nhận 14/10/2026

Chủ dự án đã duyệt Core Foundation: hợp nhất HeroBase/Meadow, xây bảy trụ cột Domain thuần C# và NUnit EditMode tests, sau đó Configs/Client và game loop. Kế hoạch chi tiết và chính sách kỹ thuật tại [Phase 1–2](Architecture/CORE_FOUNDATION_PHASES_1_2.md); kết quả thực tế tại [tiến độ](Architecture/CORE_FOUNDATION_PROGRESS.md). Phase 1–4 đã có implementation; MainGame là scene chơi duy nhất. Kết quả kiểm chứng xem tiến độ; cấu hình mẫu chưa phải cân bằng phát hành. Điều khiển hiện hành xem README.

Chỉ thị 15/10: mở bốn vùng liền mạch với grid/streaming, Players theo owner, yêu thú báo đòn–thu phục–thưởng/hồi sinh, HUD B/T/I. [Phạm vi, tuning thử và kiểm chứng](Architecture/SPATIAL_WORLD_AND_TAMING.md); ngưỡng thu phục bản này là HP ≤30% theo mục C và test của yêu cầu. Không phục hồi luật Huyết Khế/xác suất ở tài liệu prototype.

Chỉ thị mỹ thuật 15/10: thay mẫu nam bằng ảnh `a_clean_isolated_transparent_background_png_illust.png` gửi sau cùng; giữ cơ thể nền, tóc/đồ bổ sung sau. Mẫu này thay tỷ lệ đầu 78% và hình đạo bào mặc định của ngày 14/10 bên dưới; giữ Visual 0.625, gameplay và ID trang bị. [Bàn giao Krita/rig](Architecture/HERO_MALE_REFRESH.md). Ngày 16/10 chủ dự án cho thử hai bộ đồ và hai kiểu tóc: classic/jade thay hình trang phục, ink/silver thay tóc; basic vẫn là phôi trống. Đây là mẫu thử mỹ thuật, giữ nguyên luật và dữ liệu ngoại hình. [Bàn giao lượt thử](Architecture/WARDROBE_PILOT.md). Yêu cầu tiếp nối cùng ngày: thử mắt, hai lông mày, miệng và dấu mặt thành các lớp riêng; Y đổi mắt amber/jade, chớp mắt và biểu cảm chiến đấu thuộc presentation, chưa thêm ID miệng/mày hoặc shop. [Bàn giao mặt](Architecture/FACE_FEATURES.md).

Phản hồi điều khiển 15/10: dáng chạy nghiêng thân về phía trước, tay gập và vung đối nhịp. Giữ phím di chuyển khi dùng Space phải nối tiếp sang chạy mà không phanh về 0 ở cuối Dash; nhả phím vẫn dừng. Hướng Dash khóa đến hết thời lượng, sau đó dùng hướng di chuyển mới. [Thay đổi và kiểm chứng](Architecture/RUN_AND_DASH_POLISH.md).

Phản hồi map/ngắm bắn 15/10: tạm ẩn toàn bộ 17 phiến đá lát, gồm cả footprint. Đạn bắn bằng chuột đi đúng điểm click, không tự kéo về tâm mục tiêu; được bay qua cảnh vật hiện hành (hồ, cây, đá…). Những vật cản này vẫn chặn chân và Dash. Vách chuyên chặn đạn có cờ riêng; luật tương tác/thu phục và các delivery khác giữ nguyên. [Bàn giao](Architecture/POINTER_AND_PATH.md).

Chốt 22 StatId; năm Affinity dùng chung hệ số công/kháng nguyên tố. Công pháp ghép effect, ActorEntity dùng chung cho tu sĩ/thú/quái, modifier phân biệt từng nguồn, cộng hưởng chỉ từ thú xuất chiến. Số liệu fixture không phải cân bằng chính thức. Tinh hạch/tẩy luyện và multiplayer chưa thuộc phạm vi này.

Chỉ thị này thay chế độ chỉ xem mỹ thuật ngày 12/10 và pipeline nhiều hướng. Không nhập lại gameplay cũ từ `Recovery/`; giữ nguyên save cũ, schema foundation mới không tự đọc/ghi đè chúng. Nhân vật dùng một góc 3/4 + flip và chung bộ xương; môi trường dùng nền tiling/mesh cùng prefab độc lập.

### Tinh chỉnh tương tác và hiển thị — chỉ thị 14/10/2026

- Chỉ thị tiếp đất và mở rộng ngang: khóa dải nền/bóng/thực thể; toàn bộ người/thú/đá/cây/bụi/lều có bóng mềm bám chân. Ba gốc anh đào chặn đi và Dash; đi phía sau cây/lều làm vật thể mờ về 0.42, Y-sort vẫn theo chân. Năm ô kỹ năng chỉ hiện số giây trong lúc cooldown, badge vàng sáng và thanh giờ có chấm tròn. Dời hai đá lớn khỏi đường, hồ sang trái 1.35 đơn vị; ruộng có luống/mầm, trứng và trận pháp dùng sprite chống răng cưa. Phi kiếm, vung tay, tàn ảnh và rung Mộc Nhân là phản hồi presentation. WorldBounds/nền/camera hỗ trợ mở rộng ngang mà không kéo giãn cỏ; không mở rộng luật combat hoặc migration save.

- Chỉ thị bóng/tỷ lệ: người/thú dùng bóng elip mềm; bóng chiếu chỉ ở prop lớn, ngắn và mờ biên. Mọi xương thân/tay/chân scale 1; đầu giảm đồng đều 78%. Tab chỉ đổi classic/jade, basic là lớp lót; Mộc Nhân là trụ gỗ có đế đá và bia luyện kiếm theo chỉ thị sửa va chạm/HUD mới. Linh Điền đặt thẳng ở bãi cỏ cạnh lều, thanh phím tắt có nền mờ trên hotbar.

- Phản hồi nghiệm thu: bỏ dáng co chân khi chạy; giữ hông gần độ cao Idle và gối không cuộn lên thân. Nhãn tương tác giữ nguyên tên Linh dược, Trứng… và nằm trên từng vật thể. Hồ hiện “Thả câu” phía trên hồ khi người chơi ở gần; cho phép câu quanh toàn bộ bờ hồ có thể đứng được, không giới hạn một điểm. Chữ trạng thái/sát thương gắn nhân vật hiện tại/về sau nằm trên đầu để không che animation.

- Bổ sung chỉ thị tỷ lệ / stride / động tác đời sống: HeroBase đầu 78%, vai/tay thư thái và mặc đạo bào mặc định. X thiền, F hoặc E tại bờ hồ để câu cá; di chuyển/chiến đấu ngắt hoạt động. Thông số mẫu: tốc độ Tu sĩ 2.8, hồi Qi khi thiền ×3, 1 Tu vi/giây; cá cắn sau 4 giây, nhận 1 Linh Ngư khi giật cần. Chưa làm đặt Linh Điền. Các giá trị này là tuning triển khai, chưa là cân bằng sản phẩm.

- Chỉ một mục tiêu tương tác: ưu tiên khoảng cách, dùng hướng nhìn khi khoảng cách bằng nhau để nhích nhẹ vẫn chuyển sang vật gần hơn. Node hết hàng/đang mọc bị loại ngay; linh điền trống và cổng giữ hành động gieo/dịch chuyển. Nhãn nổi `[E]` và nút HUD cùng dùng mục tiêu Domain.
- Tên node cụ thể: Trứng Huyền Quy, Linh Thảo Thanh Tâm, Huyền Thiết Khoáng, Linh Điền, Truyền Tống Trận. HostileMonster hiện tên, HP và các status đang có, không tự thêm hiệu ứng Thiêu đốt vào công pháp mẫu.
- Chọn tỉ lệ hiển thị 62,5% cho tu sĩ/Mộc nhân trong khoảng 60–65% đã yêu cầu; bán kính chân và chiều cao tham chiếu bóng cùng tỉ lệ. Cỏ có vân vẽ tay và sắc ấm/mát; khóm cỏ/hoa ở rìa, lều và tài nguyên ở bãi thoáng. Trứng/quặng có footprint chân nhỏ.
- Kiểm logic bằng EditMode tests; chủ dự án mở MainGame bấm Play nghiệm thu hình ảnh.

## Lịch sử thiết kế trước khi tạm gỡ gameplay


Ngày cập nhật: 07/10/2026. Tài liệu này sở hữu hướng gameplay mới; phần xung đột trong `game_architecture_plan.md` và `Architecture/GAMEPLAY_SPEC.md` là lịch sử của prototype ngự thú cũ. Mỹ thuật tiếp tục theo `ArtDirection/STYLE_GUIDE.md`.

## Phối hợp bốn phiên

Dự án có bốn phiên riêng dưới đây. Các phiên bàn giao qua tài liệu và file chung; không mặc định đã biết lịch sử chat của nhau. Chủ dự án duyệt quyết định gameplay; đề xuất và thông số thử nghiệm phải được ghi riêng.

| Phiên | Trách nhiệm |
| --- | --- |
| Thiết kế game | Bàn cơ chế, ưu tiên và cân bằng; ghi quyết định được chủ dự án duyệt và câu hỏi còn mở vào GAME_DESIGN.md. |
| Logic | Triển khai luật, mô phỏng, dữ liệu, lưu/tải và test theo thiết kế đã duyệt; cập nhật [kiến trúc code](Architecture/CODE_ARCHITECTURE.md) và tài liệu của mốc triển khai. |
| Mỹ thuật và map | Tạo asset, animation và bố trí môi trường theo [STYLE_GUIDE.md](ArtDirection/STYLE_GUIDE.md); phối hợp Logic về vật cản, điểm spawn và dữ liệu map. |
| UI/UX và dashboard | Thiết kế luồng thao tác, HUD, các bảng và công cụ chủ dự án theo [UX_SPEC.md](Architecture/UX_SPEC.md); giao diện gọi luật từ Logic. |

## Đã xác nhận

- Người chơi trực tiếp di chuyển, ngắm và chiến đấu; combat lấy người chơi làm trung tâm.
- Tu tiên truyền thống là nền của game: người chơi học và luyện công pháp, chọn bộ pháp và phát triển lối tu luyện của mình.
- Công pháp quyết định các hành động và cách vận dụng sức mạnh; bộ pháp là nhóm công pháp di chuyển. Các lựa chọn phải khác về cách chơi, không chỉ khác tên hoặc phần trăm chỉ số.
- Tinh hạch từ quái là lớp thiên phú bổ sung cho cơ thể và cách chơi công pháp. Dung hợp có ít ô và khó thay; số ô, chi phí và luật thay chưa chốt.
- Ưu tiên hoàn thiện chiến đấu, chỉ số nhân vật và công pháp/bộ pháp trước khi mở rộng dung hợp tinh hạch. Giữ phần rơi/nhặt đã triển khai ở mốc 02.
- Mốc combat đầu tiên dùng art hiện có, trong một scene có gameplay thật. Chỉ điều khiển người chơi; không có thú đồng hành trong phiên thử.
- Giữ scene cũ và save hiện có để khôi phục. Phiên combat này chạy trong RAM, không đọc hoặc ghi save cũ.

## Chưa chốt

Giới hạn tinh hạch, chi phí và luật dung hợp, tẩy luyện, các nhóm thiên phú chi tiết, nguồn và chi phí học/luyện công pháp, cấp công pháp, cảnh giới, tiến trình tu luyện mới và cân bằng dài hạn chưa được duyệt. Mốc 02 bổ sung rơi và nhặt tinh hạch trong phiên thử theo yêu cầu chủ dự án; chưa triển khai dung hợp hoặc tẩy luyện. Nội dung, số ô trang bị và thông số của các mốc thử bên dưới không phải giới hạn sản phẩm chính thức.

## Thông số đề xuất chỉ cho phiên thử

Tất cả con số trong bảng là giá trị của prototype đã ngừng sử dụng, không phải luật sản phẩm đã duyệt hoặc cấu hình đang chạy. `CombatTrialConfig` và tài liệu M03 đã được loại khỏi codebase; có thể tra lịch sử Git khi cần. Chỉ số, công pháp và cấu hình hiện hành được mô tả trong [kiến trúc Core Foundation](Architecture/CODE_ARCHITECTURE.md) và [kế hoạch Phase 3–4](Architecture/CORE_FOUNDATION_PHASES_3_4.md).

| Thành phần | Giá trị thử |
| --- | --- |
| Người chơi | 100 HP, 60 linh lực, công 20, thủ 2, hồi linh lực 3/s; tốc độ 3.3; bán kính chân 0.28 |
| Đánh thường | Hệ số công kích 1.0 (18 sát thương lên Yêu Quy thử); tầm tính từ tâm chân 1.35; góc 100°; lấy một mục tiêu gần nhất trong vùng; windup 0.16s, recovery 0.20s; cooldown 0.55s |
| Hoành Tảo — chủ động | Hệ số công kích 1.7 (32 sát thương lên Yêu Quy thử); hao 20 linh lực; vòng tròn bán kính 2.10; mỗi mục tiêu trúng tối đa một lần; windup 0.28s, recovery 0.25s; cooldown 4s |
| Thiết Cốt — thụ động gắn sẵn | Giảm 20% sát thương nhận, tính tại mỗi lần trúng đòn từ definition; không cộng dồn chỉ số theo frame |
| Yêu Quy — một loại quái | Dùng sprite quy hiện có; 54 HP, công 12, thủ 2; tốc độ 1.65; cắn hệ số công 1.0, tầm 1.05, windup 0.38s, recovery 0.30s, cooldown 1.25s |
| AI | Phát hiện trong 4.5; đuổi, đánh; rời truy đuổi khi quá 6.5 từ người chơi hoặc 7 từ nơi sinh; về chỗ và hồi máu; ba cá thể cùng một definition |
| Gục/hồi sinh | Người chơi về điểm đầu sau 3s, quái sau 6s; HP/linh lực đầy, giữ đã học/đã chọn, hủy đòn/bộ pháp đang chạy, reset cooldown; bảo hộ người chơi 2s sau hồi sinh |

Vật thể trúng đòn khi **tâm chân** nằm trong tầm/góc ở thời điểm kết thúc windup và đường tới mục tiêu không bị vật cản chắn. Không đánh xuyên cây/đá/hồ. Hướng đòn thường được khóa khi bắt đầu ra đòn; người chơi và quái đứng lại trong windup/recovery. Kỹ năng dùng tâm chân người chơi tại thời điểm tác động. Quái đang windup có thể đánh hụt nếu người chơi tránh khỏi tầm. Hai loại đòn có cooldown riêng và cùng khóa thực hiện đòn; bấm nhanh không xếp hàng vô hạn.

## Điều khiển thử

WASD hoặc phím mũi tên di chuyển; chuột ngắm; J hoặc chuột trái đánh thường; K dùng Hoành Tảo; C mở catalogue công pháp; Space dùng/tắt bộ pháp đã chọn; R bắt đầu lại phiên thử. Nút HUD gọi cùng đường lệnh như bàn phím. Chuột trên HUD không gây thêm đòn vào thế giới. Thay đổi vị trí/HP/animation đều lấy từ mô phỏng combat, không từ hiệu ứng hình ảnh.

## Nghiệm thu mốc 01

- Đúng tầm, góc, cooldown; mỗi lần ra đòn chỉ tác động một lần theo definition.
- Vật cản chặn chuyển động và đòn đánh; tốc độ chéo được chuẩn hóa.
- Thiên phú giảm đúng sát thương và không tích lũy theo tick hoặc hồi sinh.
- AI đuổi/đánh/chết/hồi sinh; người chơi nhận sát thương/gục/hồi sinh, không hành động khi gục.
- HUD và animation đọc trạng thái/impact thật; scene mới mở chơi được; compile, test và WebGL build đạt.
- Scene cũ, prefab map gốc và save cũ được giữ. Bản khôi phục bao gồm cả source chưa được Git theo dõi.

## Mốc 02 — rơi và nhặt tinh hạch

Đã xác nhận cho phiên thử: loại tinh hạch và tỷ lệ rơi dùng dữ liệu cấu hình; mỗi đời quái chết chỉ xét thưởng một lần; chỉ xóa đồ dưới đất sau khi kho nhận đủ; kho đầy giữ nguyên vật phẩm. Tái sử dụng module hành trang khi phù hợp. Không mở luật dung hợp/tẩy luyện trong mốc này.

Đề xuất thử nghiệm: Yêu Quy rơi 1 tinh hạch với tỷ lệ 100% để dễ nghiệm thu; kho tạm 4 ô, mỗi ô gộp 3, tầm nhặt 1.2. E nhặt; J/chuột trái/nút chính ưu tiên nhặt món gần, K vẫn đánh kỹ năng. Đồ và kho chỉ tồn tại trong phiên, giữ qua gục/hồi sinh, R hoặc tải lại trang tạo lại phiên. Đây không phải giới hạn sở hữu tinh hạch chính thức. Cấu hình và bàn giao mốc này chỉ còn trong lịch sử Git; tinh hạch chưa thuộc phạm vi Core Foundation hiện hành.

## Mốc 03 — chỉ số nhân vật và công pháp

### Phạm vi đã duyệt

Mốc M03 trước đây làm nền chỉ số nhân vật cùng hệ công pháp trong phiên combat thử. `ActorDefinition.BaseStats` giữ chỉ số gốc; `ActorState` giữ HP/linh lực/cooldown/trạng thái. `CharacterStats` tính modifier theo nguồn và `TechniqueModule` vận hành công pháp. Các interface này đã được thay bởi `ActorEntity`, `StatSheet` và hệ Combat trong [Core Foundation Phase 1–2](Architecture/CORE_FOUNDATION_PHASES_1_2.md); cấu hình thử M03 không phải luật sản phẩm.

- Bộ chỉ số tối thiểu: HP tối đa, linh lực tối đa, công kích, phòng ngự, tốc độ di chuyển và hồi linh lực. HP/linh lực hiện tại là tài nguyên runtime riêng; không trộn với chỉ số tối đa.
- Tách chỉ số gốc, modifier từ công pháp/hiệu ứng và chỉ số sau tính toán. Cùng một nguồn không cộng lại mỗi frame hoặc mỗi lần đổi trang bị; tháo/hết hiệu ứng bỏ đúng modifier của nguồn đó.
- Công pháp dùng definition có ID ổn định, dữ liệu chi phí/cooldown/hiệu lực riêng với trạng thái học, trang bị và cooldown của từng người chơi. Domain C# sở hữu điều kiện và tác động; UI/input chỉ gửi lệnh và đọc kết quả.
- Đánh thường và Hoành Tảo phải dùng công kích để tính sát thương; phòng ngự và Thiết Cốt ảnh hưởng sát thương nhận theo công thức thống nhất. Giữ chức năng của Thiết Cốt như thiên phú thử gắn sẵn, chưa triển khai dung hợp.
- Bản đầu giữ đánh thường, Hoành Tảo và thêm hai bộ pháp khác cách vận hành để lựa chọn. Dùng art hiện có, bổ sung HUD thử cho linh lực, chỉ số và công pháp; bàn giao interface cho UI/UX.
- Giữ AI, va chạm, chết/hồi sinh, rơi/nhặt và kho mốc 02. Phiên vẫn chạy trong RAM; không sửa save/migration hoặc mở kinh tế, cảnh giới, đột phá và dung hợp trong mốc này.

### Nội dung và luật thử nghiệm

Đề xuất nội dung cho Logic triển khai trong bản thử: một công pháp nền **Dưỡng Nguyên Quyết** bổ sung linh lực/hồi linh lực, **Hoành Tảo** là công pháp công kích dùng linh lực, **Khinh Vân Bộ** tăng tốc duy trì với tiêu hao linh lực thấp và **Lôi Ảnh Bộ** lướt ngắn có chi phí/cooldown. Lướt bị vật cản chặn; chưa có bất tử khi lướt. Các tên và thông số có thể điều chỉnh, không phải danh sách nội dung cuối.

Catalogue cho học miễn phí trong phiên thử và chọn công pháp nền/bộ pháp đang dùng; tạm dùng một ô cho mỗi nhóm này. Không làm sách rơi, sư phụ, chi phí học hoặc tiến trình luyện cấp. Logic tự chọn công thức, hệ số sát thương, thời gian và số liệu cân bằng hợp lý, ghi rõ là cấu hình thử trong tài liệu của mốc. Học/chọn miễn phí và số ô thử không phải luật sản phẩm.

Chỉ nhận lệnh khi người chơi còn sống, công pháp đã học/được chọn, đủ linh lực, hết cooldown và không bị khóa hành động. Lệnh bị từ chối không trừ tài nguyên hoặc tạo cooldown; lệnh hợp lệ trừ chi phí và bắt đầu cooldown đúng một lần. Cooldown gắn theo ID công pháp; đổi bộ pháp không xóa cooldown, hồi HP/linh lực hay giữ buff cũ. Khinh Vân Bộ dừng khi không còn đủ linh lực; không tạo tài nguyên âm. Gục hủy hiệu ứng đang thi triển; hồi sinh giữ công pháp đã học/đã chọn, phục hồi HP/linh lực và reset cooldown theo luật thử hiện hành. R/tạo phiên mới khởi tạo lại toàn bộ phiên.

### Nghiệm thu

- Linh lực thực sự tiêu hao/hồi phục; công kích và phòng ngự thay đổi kết quả combat. HP/linh lực luôn nằm trong giới hạn sau tính toán.
- Học/chọn/đổi công pháp và hai bộ pháp chơi được; không dùng công pháp chưa học hoặc chưa chọn. Đổi liên tục không nhân buff hay né cooldown.
- Lôi Ảnh Bộ không xuyên cây/đá/hồ hoặc ra ngoài map; Khinh Vân Bộ hết hiệu lực đúng lúc. Gục/hồi sinh không giữ hiệu ứng rác.
- HUD lấy dữ liệu thật; các lệnh bàn phím/nút HUD dùng chung đường xử lý. Chức năng combat và rơi/nhặt mốc 02 không hồi quy.
- Compile, test liên quan, chơi thử và WebGL build được kiểm tra tập trung cuối mốc. Logic cập nhật kiến trúc, tài liệu mốc và các interface bàn giao; không ghi đè phần đổ bóng/asset/scene mà phiên Mỹ thuật đang sửa.

## V1 — mở rộng theo ủy quyền ngày 07/10/2026

Chủ dự án yêu cầu bản local/WebGL desktop chơi được từ bắt đầu đến boss rồi lưu/tiếp tục; cho phép Logic chọn luật thử và điều phối Mỹ thuật/UI. Giới hạn RAM và chưa dung hợp của M01–M03 chỉ áp dụng cho các phiên thử đó. V1 được phép có tiến trình, dung hợp và save riêng, giữ nguyên prototype cũ.

Phạm vi V1: hoàn thiện M03; nguồn học công pháp trong gameplay; ba bậc tu luyện với hai đột phá; trại an toàn + hai khu chiến đấu; ba loại quái thường và một boss; một ô dung hợp với hai thiên phú khác cách chơi; kho/trang bị/rèn/NPC mua bán; UI start/continue và save V1 riêng. Multiplayer, backend, thanh toán, public release và nghề/nội dung khác để sau.

Các thông số và luật V1 trước đây là **quyết định thử V1 theo ủy quyền**, không tuyên bố chủ dự án đã duyệt riêng từng con số; kế hoạch cũ chỉ còn trong lịch sử Git. Phạm vi và kiểm chứng đang áp dụng nằm trong [Core Foundation Phase 3–4](Architecture/CORE_FOUNDATION_PHASES_3_4.md) và [báo cáo tiến độ](Architecture/CORE_FOUNDATION_PROGRESS.md).

Yêu cầu UI trực tiếp ngày 07/10/2026: hiện đại, gọn gàng, đẹp và thanh lịch; thay cách trình bày hoài cổ nhiều khung. Không đổi luật gameplay khi chỉnh UI. Quy tắc thị giác ở [STYLE_GUIDE.md](ArtDirection/STYLE_GUIDE.md#ui-v1--yêu-cầu-chủ-dự-án-07102026); HUD và interface Client hiện hành ở [kiến trúc code](Architecture/CODE_ARCHITECTURE.md#configs-composition-và-presentation).

## Hướng MMORPG thế giới mở — xác nhận mới nhất 07/10/2026

Game đích là MMORPG cho người chơi tự do khám phá; không phải game offline có tuyến cốt truyện bắt buộc. Chủ dự án sẽ thêm map sau. V1 local/WebGL là môi trường kiểm thử cơ chế, boss là thử thách tùy chọn, không phải điểm kết thúc thế giới. Yêu cầu này thay phần mô tả tuyến bắt buộc và cổng khóa theo cảnh giới ở các mục V1 phía trên. Giữ hệ công pháp, tu luyện, tài nguyên, nghề và chiến đấu; đột phá vẫn tăng sức mạnh, không mở quyền đi map.

Mục tiêu quy mô giữ theo xác nhận trước: 100–300 người đồng thời **tổng toàn thế giới**, mỗi vùng có giới hạn riêng. “Map gấp 200 lần” tạm hiểu là tổng diện tích thế giới, chia thành các vùng/chunk có dữ liệu độc lập; không phóng to một texture hoặc tải toàn thế giới vào RAM. Sức chứa chưa được công bố trước bài đo multiplayer qua mạng thật.

Mốc nền hiện tại: map catalogue theo ID ổn định, cổng có đích/điểm đến; room sở hữu quái/drop, nhiều actor người chơi có lệnh/cooldown riêng; tách kho người nhặt khỏi ground loot. Menu không dừng đồng hồ thế giới. Bản local vẫn có save riêng để thử; save của một người không được dùng làm dữ liệu room chung khi triển khai server. Chi tiết và giới hạn: [WORLD_FOUNDATION.md](Architecture/WORLD_FOUNDATION.md).

### Cảm giác di chuyển quanh địa hình — đã xác nhận 2026-10-08

Giữ hướng di chuyển khi gặp đá, gốc cây hoặc bờ hồ thì người chơi men theo mép vật cản để tiếp tục đi. Hitbox theo phần tiếp đất của asset; tán lá và bóng đổ không tạo tường vô hình. Đường đi kiểm tra cả bán kính chân nhân vật. Walk và Dash đều trượt theo tiếp tuyến khi chạm vật cản; hướng dash vẫn khóa và không có tìm đường. Actor overlap được đẩy về vùng đứng được mỗi bước mô phỏng, kể cả đang đứng yên/thiền. Thông số hỗ trợ trượt 45% và contour mẫu hiện tại là giá trị thử nghiệm có thể chỉnh.
