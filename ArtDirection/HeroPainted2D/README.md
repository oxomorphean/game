# Nhân vật minh họa 2D — 09/10/2026

Theo yêu cầu chủ dự án: ưu tiên nét đẹp của nhân vật cũ, bổ sung mặt trước/sau và giữ animation chơi được; không dùng Blender. Mẫu dùng tóc bạc, áo xanh đen–trắng ngà, đai ngọc và kiếm ngắn, đối chiếu `Art/CartoonV2/hero.png`. Đây là mẫu đã triển khai để chủ dự án duyệt thẩm mỹ, chưa phải art cuối.

## Nguồn và chuyển động

`turnaround.png` là một atlas PNG alpha do ImageGen tích hợp tạo với ảnh hero cũ làm tham chiếu. Ba hình gốc: mặt trước, góc nghiêng phải và lưng; hướng trái phản chiếu góc nghiêng. Đường chéo hiện dùng hình trước hoặc lưng theo hướng dọc, chưa có tám góc vẽ độc lập. [Prompt](prompt.txt) ghi yêu cầu tạo ảnh. Không sinh từng frame riêng.

`AgentScripts/PaintedHeroBaker.cs` dùng mesh 2D và sáu vùng biến dạng (thân, đầu, hai chân, hai tay) để dựng pose trong preview scene riêng của Unity. Đây chưa phải bộ các mảnh cơ thể PNG tách rời; các khớp chuyển động từ cùng một ảnh để giữ nét minh họa. Preview scene đóng sau khi xuất; không ghi scene/prefab của game. Kiếm chuyển cùng vùng tay cầm kiếm. Đứng, chạy, chém thường, Hoành Tảo, lướt, trúng đòn và gục được xuất thành 416 frame trong bảy sheet.

Nguồn 1981 × 794 px; render 512 px, đóng gói ô 256 px. PPU 128, pivot chân `(0.5, 0.23)`, chiều cao mẫu 1.25 đơn vị. Điều chỉnh độ nét ngày 09/10 tăng gấp đôi độ phân giải so với ô 128/PPU 64 trước đó, giữ kích thước trong game. Sheet có alpha, mipmap và Trilinear; giới hạn importer đủ cho kích thước sheet, không ép sheet chạy 3072 px xuống 2048 px. Kiểm tra không cắt mép hoặc có frame rỗng trước khi nhập. Frame PNG dùng straight alpha sau khi đọc RenderTexture. Bằng chứng lần tăng độ nét nằm ở `Verification/HeroSharpness20261009/`; hướng tách lớp để thay đồ là [đề xuất chưa duyệt](MODULAR_CHARACTER_PROPOSAL.md).

## Tái tạo bằng Unity

Chạy ở thư mục gốc dự án, khi Editor dừng Play:

```sh
unity command run_script --file AgentScripts/PaintedHeroBaker.cs --entry PaintedHeroBaker.Run --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo
unity command run_script --file AgentScripts/HeroDirectionBuilder.cs --entry HeroDirectionBuilder.Painted --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo
```

Baker chỉ xuất `Assets/_Project/Art/HeroPainted2D/`. Importer thay frame trong definition `Content/Combat/Hero8Directions.asset`, giữ GUID definition và tham chiếu của hai scene hiện hành. Source PNG nằm ngoài Assets nên không đưa toàn bộ ảnh nguồn vào WebGL. Cần sửa vùng crop/điểm chân trong baker nếu thay bố cục atlas nguồn.

## Runtime và bàn giao

Tái sử dụng `DirectionalHeroView`/`DirectionalHeroDefinition` hiện hành: hướng đi, hướng đòn đã khóa, nhịp impact, cooldown, hurt, death/respawn và bóng đều đọc trạng thái thật như trước. Không sửa Domain, input, HUD, save, hitbox hoặc code bóng của phiên Mỹ thuật. Bốn góc hình ảnh không hạn chế tám hướng di chuyển/aim trong Domain.

Animation mesh chỉ chạy khi xuất ở Editor; bản game phát sheet sprite, không mang model Blender hoặc phép dựng hình vào runtime. Các thông số hình/nhịp là giá trị thử dễ chỉnh, không thay luật combat.

## Khôi phục và bằng chứng

`Recovery/HeroPainted2D-20261009/binding-before.tgz` giữ definition/importer/style guide trước thay đổi; các sheet và nguồn Blender cũ vẫn ở project để khôi phục, không cần cài lại Blender để phát chúng. Khôi phục riêng definition từ bản nén qua filesystem rồi `AssetDatabase.Refresh` sẽ trả lại hình cũ mà không động scene/map/save. Không khôi phục cả style guide nếu phiên khác đã sửa tiếp.

Bằng chứng và giới hạn kiểm tra nằm ở `Verification/HeroPainted2D/verification.md`.
