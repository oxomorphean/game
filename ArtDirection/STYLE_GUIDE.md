# Quy tắc mỹ thuật — Tu Tiên góc nhìn cao

Chỉ thị 14/10/2026 về tỷ lệ và động tác thay chuẩn chibi đầu lớn: HeroBase giữ góc 3/4, cụm đầu bằng 78% kích thước cũ, vai thu hẹp, tay thả gần thân, thân/chân kéo dài nhẹ. Tu sĩ khởi tạo mặc đạo bào classic; basic chỉ là lựa chọn thử thay đồ. Tỷ lệ toàn rig 0.625 giữ nguyên. Thiền/câu cá dùng chính rig, pose mượt và đạo cụ neo bàn tay; nghiệm thu thị giác trong Editor.

Phản hồi sau Play: chạy phải giữ dáng đứng, gối ở dưới hông; không hạ hông sâu hoặc cuộn chân vào thân để ép khớp stride. Kiểm cả silhouette của trọn chu kỳ và sai lệch chân trụ. Nhãn tương tác nằm hoàn toàn trên vật thể và giữ tên item; hồ hiện “Thả câu” phía trên cụm hồ khi người chơi tới gần từ bất kỳ phía nào. Chữ trạng thái/sát thương gắn nhân vật nằm trên đầu nhân vật tương ứng, không che thân, tay hoặc animation. HUD cố định có vùng riêng.

Bổ sung 13/10/2026: tài liệu sản xuất nhân vật trong Krita cùng mẫu gốc đã được chuyển vào backup ngoài repo theo Phase 1. Quy tắc slot, nét/màu và nghiệm thu hiện hành được giữ trong file này. Artwork mới vẫn là ứng viên chờ duyệt, không tự thay nhân vật đang chạy.

Trạng thái 14/10/2026: chuẩn nhân vật là HeroBase với một góc 3/4 + flip trái/phải; trang phục Sprite Swap theo cấu hình và giữ AnimatorController. MainGame đã tích hợp rig và ba outfit; nghiệm thu thị giác vẫn do chủ dự án thực hiện.

Yêu cầu ngày 07/10/2026: dùng Soul Strike (tham chiếu lịch sử; ảnh nguồn đã lưu trong backup ngoài repo) làm tham chiếu góc nhìn, tỷ lệ và cách phân cấp hình ảnh. Đây là mẫu để duyệt; thiết kế nhân vật và môi trường thuộc bối cảnh tu tiên của Linh Thú Ký.

- Góc nhìn từ cao xuống: thấy đỉnh đầu, đỉnh mai và mặt trên vật thể; thân/chân bị rút ngắn. Camera Unity 2D vẫn nhìn thẳng mặt phẳng sprite, góc cao nằm trong thiết kế ảnh. Không coi góc ước lượng từ ảnh Soul Strike là thông số camera đã xác minh; tiêu chí duyệt là hình người và môi trường có cùng phối cảnh.
- Nhân vật Tu Tiên thanh thoát, đầu nhỏ hơn mẫu chibi cũ, vai và tay thư thái; tóc thành vài khối rõ. Ngự thú sư tóc bạc, áo xanh đen–trắng ngà, đai xanh ngọc, điểm vàng ít. Hình phải đọc được khi hiển thị cao khoảng 55–85 px. Quy tắc nhân vật bổ sung ngày 12/10 bên dưới sở hữu góc trình mẫu, nét, lớp trang phục và tỷ lệ vũ khí.
- Huyền Giáp Quy giữ mai than đen, ít đường hoa văn vàng, da xanh ngọc; mai thấy từ trên nhiều hơn thành bên. Không phủ nhiều trang sức hoặc chấm da vụn.
- Viền ngoài nhân vật đậm hơn nét bên trong; độ dày thị giác nhất quán giữa các nhân vật. Tô 2–3 mảng sáng tối. Mặt trời ẩn mọc từ bên trái (đông, −X), qua đỉnh đầu rồi lặn về bên phải (tây, +X): bóng sáng ngả phải, trưa nằm quanh gốc theo hình tán, chiều ngả trái và dài hơn. Bóng lấy silhouette vật thể, nối chân cây/đá; bóng đá giữ bề rộng theo khối đá, cây và đá không dùng bóng elip phụ. Không vẽ mặt trời trên màn hình; thanh thời gian hiển thị giờ ban ngày. Hoàng hôn có ánh vàng; tạm chưa làm buổi tối. Nét nhân vật rõ hơn nền. Không pixel, không mô hình bóng nhựa, không lông/tóc tả thực.
- Nền là mảng cỏ xanh rộng yên mắt, chỉ thỉnh thoảng có mảng xanh đậm/nhạt nhẹ; không rắc nét vụn, texture nham nhở hoặc mảng màu phản chiếu/lặp rõ. Cỏ tại mép đường phải khớp nền. Đá, cây, bụi cỏ tập trung ít cụm; phần giữa để trống cho nhân vật và chiến đấu.
- Gần trưa, bóng đá bám đáy silhouette, không có dải bóng rời dưới đá. Tán trên cao giữ dáng các cụm hoa/lá; thân nối về chân bằng khối hẹp, không chiếu rễ nằm trên đất thành vệt ngang. Bóng thân hòa vào tán, không đậm hơn vì lưới tự chồng lớp. Xem ảnh scene ở 11:30 cùng sáng/trưa/chiều; số đo diện tích và đường biên không thay cho nghiệm thu hình ảnh.
- Nhân vật và môi trường dùng chung phép chiếu `SunShadow.cginc`. Thông số hình khối/điểm tiếp đất nằm trên prefab hoặc definition; không thêm công thức runtime theo tên asset. Các lớp tóc, áo, người và vũ khí dùng cùng điểm chân, không cộng dồn độ đậm khi bóng chồng nhau.
- Nắng dùng chung cho nền, cây/cỏ/hoa, đá, nhân vật và mặt nước; sắc ấm chiều tà vẫn giữ màu riêng của từng chất liệu. Hồ có gợn phản quang và điểm lấp lánh thưa, không chạy hiệu ứng lên đá bờ/lá sen. Rễ sát đất không được chiếu thành một bóng rễ rời; contour hồ bám chân bờ thấp, không ôm phần cỏ hay đỉnh đá nhô cao. Kiểm tra bằng chân nhân vật tại bờ và ảnh sáng–11:30–trưa–chiều.
- Góc nhìn và tỷ lệ phải đồng nhất giữa người, thú, đá và cây. Map rộng nhìn thoáng; tránh cổng lớn hoặc tán cây chắn cả trung tâm.
- PNG alpha cho người và thú. Ảnh người và quy dùng mipmap/Trilinear khi hiển thị nhỏ trong bản chơi để giảm răng cưa và lốm đốm; sprite môi trường giữ Bilinear hiện có. Texture cỏ/đất lặp của mẫu map chính dùng mipmap/Trilinear. Nền giữ tỷ lệ, không kéo méo nền hoặc nhân vật.
- Animation dùng cùng nguồn nhân vật, xương và bind coordinates; không sinh ảnh AI độc lập cho từng frame. Chỉ một góc 3/4 chuẩn và FlipX, không làm tám hướng. Đổi Sprite theo OutfitConfigSO tại runtime, giữ nguyên AnimatorController.
- Cơ thể gốc và trang phục dùng xương chung; mắt, tóc, áo/tay áo, quần, giày và kiếm độc lập. Tay/chân có đủ vùng bị đồ che; đổi trang phục không đổi đầu hoặc reset chuyển động.
- Môi trường chia thành prefab độc lập: cây, bụi, đá, cụm hồ, thảo dược, quặng, linh điền, trại và cổng. Mặt đất tiling/đường spline-mesh, không ghép ảnh nền cố định. Pivot tại chân, Y-sort theo Y, collider WorldGeometry chỉ ở gốc. Hồ giữ cả nước/đá/cỏ/lá sen trong cùng cụm. Nền, bóng và nước vẽ dưới nhân vật.

## Nhân vật nhiều lớp — bổ sung theo yêu cầu 12/10/2026

Phần này quy định mẫu tiếp theo; asset đang chạy chưa được đánh giá đạt các tiêu chí mới. Ảnh Soul Strike là tham chiếu thị giác, không phải bằng chứng về phần mềm, số góc artwork hoặc cách tách file nguồn của nhà phát triển.

- **Góc nhân vật:** một góc 3/4 nghiêng nhẹ, thấy mặt/đỉnh tóc và chiều sâu tay trước/sau; lật trái/phải bằng FlipX. Tư thế nghỉ tự nhiên. Không tạo nhánh tám hướng hoặc scene mẫu thay pipeline chính.
- **Nét và màu:** ưu tiên silhouette tóc, áo và vũ khí rõ ở 55–85 px. Viền ngoài than tối/xanh đen; nét nếp tóc, áo và mắt mảnh hơn, không làm thân thành một mảng đen. Mỗi chất liệu dùng màu nền, mảng bóng có biên rõ và điểm sáng tiết chế; đầu/tóc sáng hơn thân. Bỏ chi tiết nhỏ không đọc được ở kích thước chơi; không dùng chuyển sắc làm hình trông như nhựa 3D.
- **Nguồn và slot:** dùng cùng canvas, kích thước, vị trí chân và tỷ lệ cho cả cơ thể và trang phục. Giữ tên lớp ổn định cho `Back_Prop`, `Hair_Back`, `Arm_Back`, `Weapon`, `Leg_L`, `Leg_R`, `Body_Torso`, `Coat_Bottom`, `Head`, `Eyes_Mouth`, `Hair_Front`, `Hat`, `Arm_Front`, `Sleeve_Front`; chỉ tạo slot khi có phần tương ứng. Tay áo phía sau tách riêng nếu trang phục cần. Áo/quần được vẽ trên khuôn cơ thể chung; không crop rồi co giãn độc lập để ráp. Tách vạt áo hoặc tóc phụ khi cần chuyển động riêng, không bắt buộc chia mọi lọn tóc.
- **Nối khớp:** vẽ bù phần bị che, đầu vai/hông/khuỷu/gối có vùng chồng bo tròn nằm dưới lớp che. Pivot nằm tại khớp, mesh và weight vùng nối biến dạng tương thích trong cùng bind coordinates. Đầu tròn giúp che khe nhưng không bảo đảm hết hở ở mọi góc xoay; phải xem các pose lớn và trọn vòng chạy. Không vẽ thêm viền đậm vào đường cắt bị che.
- **Vũ khí và vệt chém:** vũ khí có silhouette lớn, bản đủ rộng để đọc được khi nhân vật nhỏ; vị trí cầm theo tay và không che mặt. Vệt chém là sprite/mesh hiệu ứng riêng theo nhịp clip, không chỉ quay tay rồi xem là động tác hoàn chỉnh. Đây là chuẩn hình cho mốc hiệu ứng sau; mốc Idle/Run hiện tại không thêm input tấn công hoặc luật combat.
- **Animation và nghiệm thu:** rig/mesh/weight lưu thành asset; clip có keyframe cho tiếp đất, đẩy người, nhấc chân, vung tay và chuyển động phụ. Root tại chân không bị clip dịch chuyển. Xem hai bộ đồ ở idle, trọn vòng chạy, đổi đồ giữa clip và lật trái/phải, cả ở kích thước chơi và zoom. Không hở khớp, trượt chân rõ, đổi pivot/tỷ lệ hay reset clip khi đổi đồ; bóng bám chân. Kết quả hình ảnh được ghi riêng với compile/build.

Thông số thử để duyệt, không phải số đo Soul Strike: góc xoay người khoảng 25–35 độ; phối cảnh từ cao xuống khoảng 35–40 độ để so với ảnh tham chiếu và nền hiện có; viền ngoài khoảng 1.5–2 lần nét trong, màu khởi điểm `#141218`; vũ khí lớn có chiều dài gần chiều cao nhân vật. Chỉnh các giá trị này theo hình thực tế và giữ đồng nhất với môi trường, không xoay camera Unity 2D theo các số đó.

## Tài nguyên và nghiệm thu

Nguồn game chuẩn ở `Assets/_Project/Art/Characters/HeroBase/` và `Art/Environment/Meadow/`; prefab rig ở `Content/Prefabs/Actors/HeroBase.prefab`, map ở `Content/Prefabs/Environment/MeadowMainMap.prefab`. Giữ xương, pivot, clip Idle/Run, nền tiling và đường mesh hiện có. Linh thú dùng cùng ActorEntity, hình có thể nhiều mảnh hoặc sprite đơn với chuyển động phụ và SunShadowMesh.

Master KRA/PSD/SVG và ảnh tham chiếu lịch sử nằm trong backup bền vững ngoài repo; xem [khôi phục](../ROLLBACK.md). ArtDirection chỉ giữ file này. Không đưa master hoặc script export vào Assets.

Chủ dự án mở Unity Editor để xem idle/run, thay ba bộ đồ giữa clip, lật trái/phải, tiếp đất và bóng sáng/trưa/chiều. Compile và EditMode tests xác minh cấu trúc/logic; không chứng minh mỹ thuật đã đạt. Không chụp WebGL/headless tự nghiệm thu.

## UI — yêu cầu chủ dự án 07/10/2026

Giao diện game cần hiện đại, gọn gàng và thanh lịch. Dùng phân cấp chữ, khoảng cách và màu nhấn tiết chế; giảm khung trang trí, viền vàng và những bảng đặc che sân chơi. HUD ưu tiên HP/linh lực, kỹ năng/cooldown và mục tiêu ngắn; chỉ số chi tiết nằm trong bảng chức năng. Bảng hành trang/công pháp cần dễ đọc, đủ dấu tiếng Việt, có lý do khi lệnh bị từ chối. Dashboard phát triển dành cho chủ dự án vẫn tách khỏi UI chơi. Sprite nhân vật và map tiếp tục theo chuẩn mỹ thuật hiện hành ở trên.
