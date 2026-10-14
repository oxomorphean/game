# Mẫu nhân vật 3/4 có nguồn nhiều lớp

Mẫu riêng ngày 12/10/2026: hai trang phục dùng chung cơ thể, skeleton và clip Idle/Run. Đây là mẫu xác minh quy trình chỉnh sửa và đổi đồ; chưa được chủ dự án duyệt thay player chính hoặc duyệt chất lượng mỹ thuật cuối.

## Nguồn và cách chỉnh

- `hero-three-quarter.kra`: nguồn Krita 1024×1024, 26 lớp production, nhóm Body/Identity/Ivory/Navy và nhóm tham chiếu ẩn. KRA đã được mở lại bằng Krita từ bản sao byte giống hệt để tránh hộp thoại autosave; bằng chứng ở `Verification/HeroProduction2D/krita-reopen.json`.
- `Assets/_Project/Art/HeroProduction2D/hero-three-quarter.psd`: bản giao Unity, chỉ chứa lớp production. Krita 5.3.4.1 xuất raster PSD; Unity PSD Importer 15.0.1 nhập theo tên lớp có phân biệt hoa thường, giữ lớp ẩn và bố cục chung.
- `joints.json`: các khớp trong canvas gốc, tọa độ Y hướng xuống; root chân `(548,843)`. Cơ thể và cả hai bộ đồ đã nhận cùng phép biến đổi tỷ lệ, không scale mỗi mảnh theo crop riêng.
- `krita-source.py`: công cụ dựng nguồn ban đầu chạy **bên trong Krita Scripter**, dùng QPainter để vẽ path trên các paint layer. Đầu/tóc/mặt tái sử dụng artwork hero đã có; thân và trang phục được dựng mới bằng các path. Đây không phải toàn bộ nhân vật được artist vẽ lại bằng cọ. `master-front.png` chỉ là tham chiếu cũ, không phải nguồn rig.

Khi chỉnh bằng Krita, giữ canvas, vị trí khớp và tên lớp. Vẽ trên lớp tương ứng của cùng khuôn, vẽ đủ vùng bị che và phần chồng tại khớp. Nếu đổi silhouette hoặc vẽ vượt vùng cắt của sprite hiện có, phải cập nhật SpriteRect, mesh và weights trong Sprite Editor/Skinning Editor; giữ canvas và tên lớp chưa đủ để phần vẽ mới hiện đúng. Lưu KRA; xuất một bản PSD production sau khi loại nhóm References khỏi bản xuất. Không bật cả hai nhóm trang phục khi xem assembled. `assembled-ivory.png` và `assembled-navy.png` chỉ là ảnh kiểm tra nguồn, không dùng làm một sprite nhân vật nguyên khối.

**Không chạy lại `krita-source.py` sau khi artist đã sửa tay mà chưa lưu bản sao**, vì script dựng lại và ghi nguồn. Tương tự, `HeroProductionBuilder.Prepare()` là lệnh tạo lại mesh/weights/clip/scene; không dùng nó như lệnh reimport thường ngày. Kiểm tra reimport hiện dùng cùng PSD, xác nhận giữ ID/rig/mesh/weights; chưa xác minh thay đổi silhouette. Importer giữ SpriteRect đã lưu (`resliceFromLayer=0`). Sau khi sửa animation bằng Animation Window hoặc weight trong Skinning Editor, giữ các asset đó làm nguồn; chỉ chạy builder khi chủ động muốn dựng lại cấu hình mẫu.

## Unity và giao diện bàn giao

Scene: `Assets/_Project/Content/Scenes/HeroProductionTrial.unity`. Prefab và các asset dưới `Assets/_Project/Content/Characters/HeroProduction2D/`: `HeroThreeQuarter.prefab`, `HeroOutfits.asset`, `Hero.controller`, `Idle.anim`, `Run.anim`.

20 xương, 17 SpriteRenderer nhìn thấy và 16 lớp bóng dùng SpriteSkin chung. Bones/mesh/weights nằm trong dữ liệu nhập PSD; Animator và AnimationClip lưu thành asset. Runtime không dựng lại rig, không tính pose bằng công thức sin. Bóng tiếp tục dùng shader daylight hiện có và nhận root chân theo world space.

`AuthoredHeroView.SetOutfit("Ivory" | "Navy")` đổi chín slot áo/quần/giày qua SpriteLibrary/SpriteResolver mà không gọi lại Animator.Play. `SetMoving(bool)` chọn Idle/Run, `SetFacing(-1 | 1)` phản chiếu góc 3/4. Các ID này chỉ là mẫu mỹ thuật, chưa là schema trang bị/save. Mắt và tóc có lớp/xương riêng nhưng bản này chưa cung cấp thêm kiểu tóc hoặc bảng đổi mắt trong UI.

`AuthoredHeroPreview` dùng `PreviewMovement` và footprint map hiện có. Animation đi lại lấy displacement sau va chạm; nút **Xem chạy** là chế độ duyệt clip tại chỗ có nhãn rõ. WASD/mũi tên di chuyển; 1/2 đổi đồ; Z đổi zoom; R về điểm đầu. Clip Idle dài 2 giây; Run dài 0.5 giây, gồm các pose tiếp đất/thu chân/nhấc chân cùng chuyển động tay/tóc/vạt áo. Tốc độ mẫu 1.2 unit/giây.

Menu Unity: **Demo → Mỹ thuật → Nhân vật nhiều lớp → Mở bản thử / Tạo rig và bản thử / Build Web mẫu**. Build riêng vào `WebBuildHeroProduction`; build mặc định của dự án vẫn chỉ có ArtPreview.

Bản Web duyệt mẫu: [localhost:8796](http://127.0.0.1:8796/). Server chỉ bind `127.0.0.1`; có thể mở lại từ project bằng `python3 -m http.server 8796 --bind 127.0.0.1 --directory WebBuildHeroProduction`. Dùng cửa sổ ngang để xem đúng bố cục và kích thước mục tiêu. Đây là bản mẫu riêng, không thay nội dung ở cổng 8794.

## Kiểm chứng và giới hạn

`HeroProductionChecks.Assets()` kiểm tra đăng ký hai bộ đồ, dữ liệu xương/weight, clip và reimport; kết quả 40 assertion qua. `HeroProductionChecks.BeginRuntime()` chạy khi scene mẫu đang Play, kiểm tra mesh thực sự biến dạng, root cố định, đổi đồ không reset phase, lật hướng không đổi hướng nắng, bóng bám root và trở lại Run sau khi bật lại nhân vật; kết quả 37 assertion qua, ảnh ở tám phase cho mỗi bộ đồ. Các con số này là kiểm tra kỹ thuật, không phải điểm chấm mỹ thuật.

Đã xem bản chạy trong Unity, ảnh hai bộ đồ, pose nhấc chân, phản chiếu và cận cảnh. Lỗi MaterialPropertyBlock khởi tạo ngoài Awake đã được sửa; các kiểm tra runtime sau sửa không có lỗi mới. Source KRA/PSD mở và nhập được. `protected-after.json` xác nhận ArtPreview, HeroModularTrial, MeadowMainMap và EditorBuildSettings giữ nguyên; thay đổi package trong task chỉ thêm PSD Importer cùng dependency Tilemap.

Review độc lập phát hiện công cụ kiểm tra chưa phục hồi scene khi lỗi và hướng dẫn reimport chưa nói rõ giới hạn SpriteRect; cả hai đã sửa. Kiểm tra trực tiếp còn tái hiện Animator mất tham số Moving khi tắt/bật hero; `SetMoving` hiện đồng bộ lại tham số kể cả giá trị không đổi. `review-regressions-before-fix.json` và `review-regressions.json` ghi nhận trước/sau. Trường hợp cố ý làm mất tên lớp chân trong Play cũng xác nhận camera, tốc độ Animator, trang phục và controller được phục hồi sau exception.

Build cuối ngày 13/10 thành công, 0 lỗi và 1 cảnh báo Pipeline không bật remote control trong Player; báo cáo ở `build.json`. Lần gọi build qua kết nối trước đó đã hết thời gian chờ của Pipeline; bản cuối chạy từ menu Unity không gặp lỗi này. Bản Web đã được tải lại và thử ở 1280×720: di chuyển, đổi đồ khi đang Run, zoom và dừng/tiếp tục animation; console không có lỗi hoặc cảnh báo. `web-review.json` ghi rõ những thao tác đã thử trước/sau build; `web-final-*.jpg` là ảnh trình duyệt của bản cuối. Đây chưa phải kiểm thử trên nhiều trình duyệt hoặc thiết bị.

Tự đánh giá: phương pháp native Unity phù hợp với nhu cầu thay quần áo và để artist sửa tiếp. Tuy nhiên đầu minh họa chi tiết hơn thân dựng bằng path, thân vẫn hơi phẳng, hai bộ đồ hiện cùng silhouette và động tác còn là vòng chạy mẫu. Chưa nên sản xuất hàng loạt trang phục dựa trên chất lượng hình này. Mốc tiếp theo về mỹ thuật nên chỉnh đồng bộ toàn thân trong KRA rồi duyệt một bộ đồ và trọn vòng chạy ở 55–85 px trước khi mở rộng nội dung. Một góc 3/4 và phản chiếu không thay cho các artwork nhìn từ lưng hoặc tám góc độc lập. Chưa đo hiệu năng đám đông/mobile.

## Khôi phục

Giữ mẫu cũ và file nguồn cũ. Cấu hình trước khi thêm package nằm trong `Recovery/HeroProduction2D-20261012/config-before.tgz`; nguồn mẫu đã kiểm chứng nằm trong `verified-source-20261013.tgz` cùng thư mục, danh sách/hash ở `Verification/HeroProduction2D/source-manifest.json`. Archive là bản sao phần mẫu và cấu hình, vẫn cần project cùng các material/map dùng chung. Không reset Git vì repository có nhiều thay đổi và source chưa tracked từ trước. Muốn gỡ mẫu, bỏ các file riêng của HeroProduction2D và bốn file C# AuthoredHeroView, AuthoredHeroPreview, HeroProductionBuilder, HeroProductionChecks sau khi sao lưu; không gỡ 2D Animation vì mẫu cũ đã dùng package này. Krita được cài từ bản ký chính thức của KDE tại `/Applications/Krita.app`, khoảng 960 MiB.

Tài liệu Unity đối chiếu: [Sprite Swap và yêu cầu skeleton giống nhau](https://docs.unity3d.com/Packages/com.unity.2d.animation@16.0/manual/SpriteSwapIntro.html).
