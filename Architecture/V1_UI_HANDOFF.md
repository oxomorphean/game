> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Bàn giao UI khám phá V1

Ngày 07/10/2026. Phần UI triển khai theo [V1_PLAN.md](V1_PLAN.md) và `Domain/Adventure/AdventureUi.cs`. Đây là giao diện người chơi; dashboard phát triển tại cổng 8789 tiếp tục riêng. UI đọc snapshot tách rời, gửi command và không sửa campaign, tài sản, save hoặc luật game.

## File và API

Các file mới thuộc `Assets/_Project/Scripts/Client/AdventureUI/`: `AdventureHud.cs` sở hữu API/cập nhật dữ liệu; `AdventureHudLayout.cs` dựng uGUI/TMP; `AdventureHudPanels.cs` dựng danh sách cuộn và các hành động; `AdventureHudDialogs.cs` chứa xác nhận và chiến thắng; `AdventureHudChecks.cs` chứa kiểm tra Editor với snapshot giả; `AdventureLineIcon.cs` vẽ các biểu tượng UI bằng mesh đơn giản, không cần ảnh/font icon tải ngoài. Assembly dùng `LinhThu.Prototype` hiện có; không thêm dependency, asset, scene, prefab hoặc cấu hình. Các `.meta` được Unity tạo khi import.

```csharp
using LinhThu.Prototype.AdventureUI;

var hud = hudObject.AddComponent<AdventureHud>();
hud.Build(font, HandleAdventureCommand, QueueCombatRequest);
hud.Refresh(snapshot);
if (hud.PanelsOpen) { /* Host chặn input, xóa intent đang chờ; world vẫn tick. */ }
hud.TogglePanel("inventory");
hud.ClosePanels();
```

`Build(TMP_FontAsset, Action<AdventureCommand>, Action<CombatRequest>)` được gọi một lần; font dùng `Resources/Fonts/NotoSans-TMP`. `Refresh(AdventureSnapshot)` nhận snapshot sau command và mỗi lần trạng thái thay đổi. UI giữ tham chiếu snapshot chỉ để trình bày và đọc các command, không viết vào nó. Host cập nhật snapshot sau mỗi lệnh để điều kiện và phản hồi được làm mới ngay.

`PanelsOpen` true ở màn bắt đầu, bảng chức năng hoặc hộp xác nhận/mốc boss. Host chặn input combat và bỏ intent đang chờ khi mở/đóng bảng; không xếp hàng đòn để chạy sau khi đóng. Theo hướng MMORPG được chủ dự án chốt, mở bảng không dừng world hoặc thời gian rèn; host sở hữu việc tick này. UI không thay `Time.timeScale` hoặc tự tick gameplay. Thông báo gục không mở panel. Lượt QA UI dùng host trước thay đổi world/craft; Logic kiểm chứng hành vi tick sau khi tích hợp nền thế giới mở.

## Điều hướng và dữ liệu

| Panel ID | Dữ liệu | Phím host |
| --- | --- | --- |
| `inventory` | `Inventory`, `InventorySummary`, `Equipment` | B |
| `techniques` | `Techniques` | C |
| `cultivation` | `Progression`, `Realm`, `Cultivation` | T |
| `workshop` | `Workshop` | F tại NPC hoặc nút |
| `market` | `Market`, `Coins` | F tại NPC hoặc nút |
| `fusion` | `Fusion`, `Talent` | F tại NPC hoặc nút |
| `travel` | `Travel`, `Location` | F tại cổng hoặc nút |
| `menu` | Lưu, tiếp tục, về màn bắt đầu, hành trình mới | Esc |

Host dùng Esc: đang mở bảng/hộp thoại thì `ClosePanels()`, nếu không thì `TogglePanel("menu")`. B/C/T toggle panel tương ứng; F đi qua `AdventureAction.Interact` để host/domain chọn đúng tương tác. F5 gửi Save. J/E và chuột trái giữ đường combat hiện hành; ba nút HUD gửi `Basic`, `Skill`, `Movement` vào cùng queue. UI không đăng ký keyboard hoặc bắt chuột của world; host phải raycast UI trước khi cho chuột trái đánh trong world. Navigation bằng submit/move/cancel của InputSystemUIInputModule cần được tắt ở EventSystem do host tạo, như HUD hiện hành; khi UI phải tạo EventSystem mới, nó tự tắt ba action này.

Mỗi lần mở bảng bắt đầu ở đầu danh sách; refresh giữ vị trí cuộn. Các row được tái sử dụng, cập nhật command ID/Quantity theo snapshot mới; chiều cao tính theo nội dung để lý do khóa dài không đè nút khác. Lý do khóa lấy từ `AdventureButton.Reason`, hoặc `PrimaryReason`/`SkillReason`/`MovementReason`; UI không suy điều kiện từ chi phí. Bấm chuyển vùng đóng bảng; domain vẫn quyết định thành công hay từ chối và HUD tiếp tục hiển thị feedback.

HUD có HP/linh lực, cảnh giới/chỉ số, thiên phú, linh tệ/vùng/mục tiêu, các nhãn/cooldown/lý do khóa và phản hồi lưu. Màn bắt đầu chỉ mở Continue khi `CanContinue`; NewGame yêu cầu xác nhận nếu có save có thể tiếp tục hoặc đang trong hành trình. ReturnToTitle xác nhận lưu trước khi về màn bắt đầu; host chỉ thoát nếu lưu thành công, nếu thất bại giữ hành trình và hiển thị lỗi để thử lại. `Completed` giữ tên để tương thích dữ liệu và mở thông báo đã hạ Cổ Giáp Vương một lần; đây là mốc thử thách, không phải kết thúc game. Save gửi về host, phản hồi lưu tiếp tục hiển thị trong thông báo; đóng/Esc cho khám phá tiếp. UI không coi việc bấm Save là đã lưu thành công.

Yêu cầu mới của chủ dự án ngày 07/10/2026 chọn giao diện hiện đại, gọn và thanh lịch. HUD dùng slate trong mờ, chữ trắng/xám, HP xanh mint và linh lực xanh lam; bảng chức năng/start/dialog dùng nền gần trắng và chữ charcoal, điểm nhấn teal tiết chế. Không dùng viền vàng/Outline. Khoảng cách 8–24, góc bo 10, Noto Sans với cỡ chữ/phân cấp và biểu tượng nét đơn giản; texture bo góc nhỏ được tạo trong RAM và hủy cùng HUD. Quickbar có đủ bảy tên với phím tắt tách riêng; gợi ý khám phá tối đa ba dòng; cooldown có số riêng bên cạnh biểu tượng. Các vùng HUD có raycast để bấm vào khoảng trống cũng không gây đòn vào world.

Canvas ScreenSpaceOverlay và CanvasScaler Expand vẫn dùng reference 1280×720; ở viewport 960×720, scale 0.75 và canvas có thêm chiều cao. Quickbar rộng 716 và cụm combat rộng 436 giữ khoảng cách 80 đơn vị; các bảng rộng 900, nhỏ hơn reference 1280. Panel chuyển sang điều hướng trái 164, cột thông tin 422 và cột thao tác 190; row đo chiều cao theo mô tả/lý do để không đè nhau. Mục tiêu là WebGL desktop; chưa có điều khiển touch/bố cục điện thoại.

## Kiểm chứng và bàn giao

Source hiện đại đã tích hợp dưới Assets. Báo cáo và ảnh thực ở `Verification/AdventureV1/ModernUI/`:

- `compile.json`: lần compile cuối completed, failed false, errors rỗng, compilationFailed false.
- `ui-checks-1280.json` và `ui-checks-960.json`: mỗi kích thước 37 passed, 0 failed. Canvas.pixelRect lần lượt 1280×720 và 960×720; không suy kích thước từ ảnh được resize. Các callback trong kiểm tra ghi vào danh sách cô lập, không gọi campaign hoặc save thật.
- `live-hud-1280.json`: không có text overflow; 10 icon có CanvasRenderer và depth render; raycast thực sau nhiều frame có hit trên Player status và Quick panels.
- `live-panel-960.json`: bảng Công pháp không có text overflow; danh sách dài 392 đơn vị so với viewport 346, có scrollbar. Kiểm tra cô lập xác minh kho dài và giữ scroll sau Refresh.
- `hud-1280.png`, `inventory-1280.png`, `hud-960.png`, `techniques-960.png`, `travel-960.png`: render screen thực trong Unity, đã xem ảnh. Bấm pointer native mở Túi đồ được; các lần input native tiếp theo bị báo người dùng thay đổi cửa sổ, nên bảng Công pháp/Chuyển vùng ở 960 được mở bằng UI API. Chưa dùng những ảnh này làm bằng chứng keyboard hoặc scroll thật trong browser.

Đã sửa lỗi quan sát từ render: thêm CanvasRenderer cho icon; tăng chiều cao cảnh giới và tiêu đề bảng theo Noto Sans; dùng Truncate để không yêu cầu glyph ellipsis còn thiếu trong font. Chế độ disabled giữ nền sáng và label xám để dễ đọc; chỉnh tint cuối đã compile, ảnh trên được chụp trước thay đổi màu nhỏ này. Binding checks chạy trước đổi tint cuối; thay đổi này không sửa callback hoặc trạng thái nút.

Copy phù hợp hướng thế giới mở: màn bắt đầu ghi BẢN THỬ LOCAL, tiếp tục khám phá/tạo nhân vật; HUD gọi objective là gợi ý khám phá; thông báo boss cho tiếp tục khám phá. UI không tuyên bố đã có server hoặc online. API, command, Domain, Host, scene, prefab và save không được UI sửa trong lượt này.

Console cuối (`console.json`, `console-errors.json`) có hai lỗi Invalid index từ lệnh QA gỡ size GameView bằng custom index; stack thuộc PipelineEval. Đã sửa lệnh dùng total index và xác minh còn 7 size mặc định. Hai lỗi này không phải exception từ UI/runtime; không dùng số lỗi Console cuối làm bằng chứng Console sạch. Logic lấy Console mới cho lượt tích hợp tiếp theo.

Editor đã Stop Play, xác nhận playing false trong CultivationV1; đã khôi phục Free Aspect và gỡ hai size QA tạm trước bàn giao Logic. UI không build. Logic sở hữu compile/test sau nền thế giới mở, kiểm input/craft khi bảng mở, save/load thật, WebGL keyboard/pointer/scroll, hiệu năng và Console của bản build. Dashboard quản lý cổng 8789 giữ phạm vi riêng.
