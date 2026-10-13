# Hướng thiết kế game

## Ưu tiên hiện hành — xác nhận 12/10/2026

Chủ dự án yêu cầu tạm thời không có gameplay; gỡ logic đánh/tấn công, lên cấp và các hệ gameplay khác để tập trung kiểm tra kiến trúc, hình ảnh và animation. Bản hiện tại chỉ giữ map, đi lại/va chạm, nhân vật, hiệu ứng môi trường và công cụ xem mỹ thuật. Không khởi tạo combat, quái, chỉ số, công pháp, tu luyện, kho, nghề, rơi/nhặt hoặc save. Đổi trang phục trong mẫu art chỉ đổi hình.

Source gameplay cũ được chuyển ra khỏi thư mục đang biên dịch vào `Recovery/ArtOnly-20261012/retired-source/`; archive và SHA-256 giữ cả source chưa có trong Git. Asset art/bố cục map và save cũ được giữ. Hướng MMORPG dài hạn chưa bị thay bằng luật mới, nhưng các mốc gameplay bên dưới hiện chỉ là lịch sử; không tự triển khai hoặc nối lại chúng. [Kiến trúc và entry point hiện hành](Architecture/CODE_ARCHITECTURE.md).

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

Tất cả con số trong bảng là giá trị thử nghiệm, không phải luật sản phẩm đã duyệt. Nguồn chạy là asset `CombatTrialConfig` trong `Assets/_Project/Content/Combat/`; definition được chụp thành dữ liệu bất biến khi bắt đầu phiên, còn HP/linh lực/cooldown/trạng thái nằm riêng trên từng nhân vật. Công thức và số liệu M03 hiện hành ở [TECHNIQUES_M3.md](Architecture/TECHNIQUES_M3.md).

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

Đề xuất thử nghiệm: Yêu Quy rơi 1 tinh hạch với tỷ lệ 100% để dễ nghiệm thu; kho tạm 4 ô, mỗi ô gộp 3, tầm nhặt 1.2. E nhặt; J/chuột trái/nút chính ưu tiên nhặt món gần, K vẫn đánh kỹ năng. Đồ và kho chỉ tồn tại trong phiên, giữ qua gục/hồi sinh, R hoặc tải lại trang tạo lại phiên. Đây không phải giới hạn sở hữu tinh hạch chính thức. Cấu hình, interface bàn giao và cách kiểm tra nằm ở [mốc tinh hạch](Architecture/CRYSTAL_M2.md).

## Mốc 03 — chỉ số nhân vật và công pháp

### Phạm vi đã duyệt

Làm nền chỉ số nhân vật cùng hệ công pháp trong phiên combat hiện hành. Không chỉ thêm tên kỹ năng vào nhân vật đang có. `ActorDefinition.BaseStats` giữ chỉ số gốc; `ActorState` giữ HP/linh lực/cooldown/trạng thái. `CharacterStats` tính modifier theo nguồn và `TechniqueModule` vận hành công pháp. [Tài liệu M03](Architecture/TECHNIQUES_M3.md) sở hữu cấu hình thử, công thức và interface bàn giao; con số chưa trở thành luật sản phẩm.

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

Các thông số và luật cụ thể trong [V1_PLAN.md](Architecture/V1_PLAN.md#luật-thử-v1-theo-ủy-quyền) là **quyết định thử V1 theo ủy quyền**, có thể chỉnh bằng config, không tuyên bố chủ dự án đã duyệt riêng từng con số. Logic quản lý kế hoạch/tích hợp; Art và UI bàn giao phần độc lập theo quyền điều phối trực tiếp. Hoàn thành chỉ khi toàn tuyến bằng thao tác người chơi, save/load, build/console và đo hiệu năng có bằng chứng.

Yêu cầu UI trực tiếp ngày 07/10/2026: hiện đại, gọn gàng, đẹp và thanh lịch; thay cách trình bày hoài cổ nhiều khung. Không đổi luật gameplay khi chỉnh UI. Quy tắc thị giác ở [STYLE_GUIDE.md](ArtDirection/STYLE_GUIDE.md#ui-v1--yêu-cầu-chủ-dự-án-07102026); giao diện V1 và API bàn giao ở [V1_UI_HANDOFF.md](Architecture/V1_UI_HANDOFF.md).

## Hướng MMORPG thế giới mở — xác nhận mới nhất 07/10/2026

Game đích là MMORPG cho người chơi tự do khám phá; không phải game offline có tuyến cốt truyện bắt buộc. Chủ dự án sẽ thêm map sau. V1 local/WebGL là môi trường kiểm thử cơ chế, boss là thử thách tùy chọn, không phải điểm kết thúc thế giới. Yêu cầu này thay phần mô tả tuyến bắt buộc và cổng khóa theo cảnh giới ở các mục V1 phía trên. Giữ hệ công pháp, tu luyện, tài nguyên, nghề và chiến đấu; đột phá vẫn tăng sức mạnh, không mở quyền đi map.

Mục tiêu quy mô giữ theo xác nhận trước: 100–300 người đồng thời **tổng toàn thế giới**, mỗi vùng có giới hạn riêng. “Map gấp 200 lần” tạm hiểu là tổng diện tích thế giới, chia thành các vùng/chunk có dữ liệu độc lập; không phóng to một texture hoặc tải toàn thế giới vào RAM. Sức chứa chưa được công bố trước bài đo multiplayer qua mạng thật.

Mốc nền hiện tại: map catalogue theo ID ổn định, cổng có đích/điểm đến; room sở hữu quái/drop, nhiều actor người chơi có lệnh/cooldown riêng; tách kho người nhặt khỏi ground loot. Menu không dừng đồng hồ thế giới. Bản local vẫn có save riêng để thử; save của một người không được dùng làm dữ liệu room chung khi triển khai server. Chi tiết và giới hạn: [WORLD_FOUNDATION.md](Architecture/WORLD_FOUNDATION.md).

### Cảm giác di chuyển quanh địa hình — đã xác nhận 2026-10-08

Giữ hướng di chuyển khi gặp đá, gốc cây hoặc bờ hồ thì người chơi men theo mép vật cản để tiếp tục đi. Hitbox theo phần tiếp đất của asset; tán lá và bóng đổ không tạo tường vô hình. Đường đi kiểm tra cả bán kính chân nhân vật. Hành vi này áp dụng cho bước đi, không biến lướt/đòn lao thành tự tìm đường. Thông số hỗ trợ trượt 45% và contour mẫu hiện tại là giá trị thử nghiệm có thể chỉnh.
