> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Nền mở rộng thế giới và mô phỏng nhiều người

Hướng đã xác nhận: MMORPG tự do khám phá, tổng 100–300 người trên nhiều vùng có cap riêng. Tạm hiểu 200× là tổng diện tích. Mốc này chuẩn bị code; không tuyên bố đã có server hay chịu được 300 kết nối.

## Nội dung và vòng đời map

`AdventureMapDefinition` chứa ID persistent không phụ thuộc thứ tự, key/tên, arena riêng, spawn, NPC, cổng có destination/arrival, danh sách enemy spawn và presentation preset. `AdventureMapCatalog` kiểm ID/đích cổng/spawn. `AdventureMapAsset` là authoring Unity qua Create > LinhThu > World > Map; mỗi vùng lưu đường dẫn Resources của layout prefab, size và collision data riêng; catalogue không tham chiếu prefab/texture trực tiếp nên đọc metadata không nạp mọi artwork. Tạo collision data khớp authoring, kiểm riêng trước khi đưa map mới vào bản chơi. AdventureConfig.maps rỗng dùng ba mẫu hiện có; prefab nằm dưới Resources và path không có đuôi; điền catalogue thì homeMapId phải trỏ vùng an toàn. Khi thêm map, cấu hình ID/prefab/cổng; không thêm if/switch trong Campaign. Số 0/1/2 còn ở presentation preset mẫu, không giới hạn số ID map. Checkpoint boss schema 1 chỉ áp dụng Cổ Giáp Vương (loi-coc:guardian); world boss của map thêm sau phải dùng room persistence, không dùng flag completed của profile làm luật chung.

Bản local giữ tối đa `AdventureRules.residentMaps` (mặc định 3) world. Chỉ map hiện tại tick; map evicted giữ ground và state quái dạng dữ liệu trong RAM (HP, đời, cooldown, hồi sinh), tiếp tục khi quay lại; không hồi đầy hoặc rút ngắn hồi sinh do cache. Không giữ World/session/render của vùng bị evict. Mở UI không dừng world/crafting. Camera theo người trong vùng lớn, không thu nhỏ toàn bản đồ vào một khung. Client resolve Resources prefab khi đổi vùng, hủy layout cũ rồi UnloadUnusedAssets, chỉ một layout mới active; không load hàng trăm prefab cùng lúc. Resources loading hiện đồng bộ lúc chuyển vùng, không phải seamless streaming; AssetBundle/Addressables streaming qua mạng và chunk trong vùng chưa triển khai. Khi mở rộng thật, chia nội dung thành vùng có ngân sách; không dùng một prefab/texture 200×.

## Mô phỏng room dùng chung

`CombatWorld` chứa actor registry với ID không tái sử dụng trong instance, một đồng hồ, TechniqueModule từng player. Overload Step nhận dictionary command theo ActorId; validate toàn batch trước tick. AI chọn người còn sống gần nhất cùng room; PvE chưa có friendly fire/PvP. Adapter Step một command và Player vẫn giữ để mẫu local dùng lại.

`CombatRoomSession` có instance Guid, cap configurable (thử 24), Join/Leave, inventory binding authority, Submit(instance, boundActorId, sequence, command) và một Step/tick. Caller phải xác thực connection→actor trước Submit; API không tự thực hiện login. Tối đa một pending command/người/tick, sequence cũ/instance cũ bị từ chối, không phát lại input khi không nhận mới. Transport tương lai quản lý tick/ack/reliable action, prediction/reconciliation và rate limit; không ghép nhiều Step mỗi người.

Quái và ground loot thuộc room; người nhặt có InventoryModule riêng. Hai lệnh cùng nhặt một drop được xử lý tuần tự trong một room authority: chỉ một commit kho thành công, kho đầy giữ đồ. Ground ID được scope bằng room instance; không dùng ID drop từ vùng khác. CopyVisibleActors lọc room + bán kính vào buffer caller, là seam AOI cho transport, chưa có serialize/delta hoặc spatial grid.

`AdventureCampaign` là adapter local cho tiến trình/kho/save đang chơi. Nó không phải server world manager. Server phải ghép profile của nhiều người vào một CombatRoomSession, chia dữ liệu profile và room persistence; tuyệt đối không restore ground trong save của từng người vào room chung. Save local schema 1 giữ tương thích và không được tin làm tài sản online.

## Ngân sách đồ rơi

Bản local giới hạn 4096 ground records chung các vùng, đồng thời dành chỗ cho phần thưởng tiềm năng của quái còn sống. Khi hết chỗ, quái chờ hồi sinh tới khi người chơi nhặt bớt; không xóa drop/ấn và không tạo save vượt ngưỡng do farm lâu. Room dùng chung mặc định 256 record với cùng nguyên tắc dành chỗ, số này truyền qua constructor. Đây là giới hạn thử có thể đổi, không phải quy tắc economy cuối. Chưa có expiry/mail tự động vì luật giữ/mất đồ chưa chốt.

## Kiểm chứng và giới hạn

AgentScripts/AdventureTests phủ catalogue 600 map (200× tổng diện tích ba mẫu), ID không liên tiếp/arena riêng, cache và ground qua eviction/save; hai player/cooldown/AI target, nhặt cạnh tranh/full bag, instance/sequence replay và 300 actor qua 20 room tổng hợp. Đây là test domain, không là benchmark CCU, latency, GPU hoặc tải VPS. Lượt kiểm tra hiện tại đạt 47/47 AdventureTests và 72/72 hồi quy combat; evidence trong Verification/AdventureV1. RunMapAuthoring đạt 2/2 bằng prefab Resources tạm được tạo qua Editor API: ID 901, size/arena 40×22.5, collision riêng và instantiate đúng layout. Fixture được dọn ngay, không sửa scene/save. Chưa có bộ art của 600 vùng hoặc đo streaming đường truyền; test này không thay việc nghiệm thu từng map được thêm sau.

Trước khi công bố online phải hoàn thành các cổng ONLINE_ARCHITECTURE: server authoritative qua protocol thật, admission toàn thế giới, room handoff và quyền sở hữu, profile/room persistence bền vững/idempotency, reconnect, asset streaming, prediction/interpolation, AOI delta và bài tải 100→300 có RTT/jitter/loss. M4 local không chứng minh hiệu năng của VPS hoặc máy người chơi.
