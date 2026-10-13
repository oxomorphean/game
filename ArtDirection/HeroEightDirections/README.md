# Chibi 8 hướng — mẫu Blender cho Unity 2D

Mẫu lịch sử: từ 09/10/2026, player đang dùng [minh họa 2D](../HeroPainted2D/README.md) theo yêu cầu chủ dự án. Source/sheet Blender được giữ để khôi phục; Blender và repo MCP đã gỡ khỏi máy. Các kết quả dưới đây thuộc mẫu cũ.

Mốc 08/10/2026: nhân vật tóc bạc, áo xanh đen/trắng ngà, ngọc và kiếm ngắn. Model gốc dựng bằng Blender 4.5.9 LTS, không tải model hoặc sinh riêng từng frame. Đây là mẫu chuyển động để chủ dự án duyệt, không phải asset sản phẩm cuối.

## Nội dung

- `hero.blend`: model, armature tám bone và bảy Action để chỉnh tiếp.
- `create_hero.py`: dựng lại model/rig và render tám hướng.
- `pack_sheets.py`: ghép render vào bảy sheet PNG có alpha gutter; chung một điểm đặt chân.
- `eight-directions.png`: ảnh so sánh các hướng; đây là ảnh render nguồn, không thay bằng chứng Unity.
- `Assets/_Project/Art/Hero8Directions/`: 416 sprite frame: idle 8, run 12, attack 8, sweep 10, dash 4, hurt 4, down 6 frame mỗi hướng. Thứ tự E, NE, N, NW, W, SW, S, SE.

Render nguồn 192 px; đóng gói vào ô 128 px, nội dung 112 px và gutter 8 px. Tạm PPU 58, camera Blender orthographic góc cao khoảng 50 độ để nhìn rõ mặt chibi; là thông số mẫu có thể chỉnh. Unity vẫn vẽ sprite trên mặt phẳng 2D, không chạy model hoặc xương Blender trên máy người chơi. Nhân vật đổi giữa tám góc, không xoay liên tục 360 độ.

## Tái tạo

Chạy từ thư mục gốc dự án, chỉ xuất render vào thư mục tạm để tránh Unity nhập từng frame:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtDirection/HeroEightDirections/create_hero.py -- --output /tmp/nguthu-hero-render
python3 ArtDirection/HeroEightDirections/pack_sheets.py /tmp/nguthu-hero-render Assets/_Project/Art/Hero8Directions
unity command run_script --file AgentScripts/HeroDirectionBuilder.cs --entry HeroDirectionBuilder.Run --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo
```

Packer cần Pillow. Builder dùng Sprite Editor API, giữ GUID sprite theo tên khi nhập lại, nạp definition sau khi mở từng scene và kiểm tra tham chiếu sau lưu/mở lại. Không sửa YAML thủ công. `.blend` nằm ngoài Assets nên Unity không cần Blender để import hoặc build game. Khi tái tạo model, chép `hero.blend` và ảnh tham chiếu từ thư mục render về đây.

## Runtime và bàn giao

`DirectionalHeroDefinition` là asset dùng chung; `DirectionalHeroView` giữ hướng, thời gian clip, generation và phản hồi trúng đòn của từng actor. `CombatWorldView` tạo presenter mới cho player khi có definition; các scene mẫu cũ và linh thú giữ rig cũ. `CultivationV1` và `PlayerCombatTrial` gắn definition mới bằng serialized field `directionalHero`.

- Di chuyển lấy `ActorState.Movement`; hướng chuột không ghi đè hướng đi. Đứng yên giữ góc cuối.
- Đánh theo `ActiveAttack`, `Aim`, `AttackElapsed` và `AttackId`; khóa hướng theo Domain. Đỉnh động tác theo thời điểm hết windup; không dùng animation event để gây sát thương.
- Lướt theo `TechniqueState.DashDirection`; gục theo `Alive`; generation mới đặt lại playback.
- Hurt từ hit flash đã resolve; hồi sinh xóa hurt/down. Mỗi sprite và bóng dùng cùng frame; sorting theo vị trí chân.
- Input, HUD, HP, cooldown, hitbox, rơi/nhặt và save không thay. UI tiếp tục đọc snapshot hiện có.

Bóng dùng material ban ngày hiện hành; phần map của Art không sửa. Chỉ hai scene thêm tham chiếu hero. Bản source trước mốc ở `Recovery/Hero8Directions-20261008/`; ảnh hero.png và rig cũ giữ nguyên. Muốn quay lại hình cũ, bỏ tham chiếu directionalHero trong Inspector của CombatWorldView; không cần khôi phục Domain hoặc save.

## Kiểm chứng

Bằng chứng cuối ở `Verification/Hero8Directions/verification.md`. `HeroDirectionChecks` kiểm tra hướng, trạng thái và sampling; `HeroDirectionRuntime` dùng Input System và Update thật trên campaign RAM tạm, không gọi Save. `CollisionRuntime` chấp nhận cả presenter mới và rig cũ để tiếp tục kiểm tra trượt vật cản. Không dùng kết quả mẫu này để cam kết số người chơi online.

## Thử Blender MCP

Ngày 09/10/2026 đã gọi MCP thật để xem rig, đổi Action và chụp các góc/chuỗi frame trên bản sao. Nguồn, vị trí cài, kết quả và giới hạn: [MCPTrial](MCPTrial/README.md).
