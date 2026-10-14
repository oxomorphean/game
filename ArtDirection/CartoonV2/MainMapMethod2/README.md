# Mẫu map chính theo cách 2

Chủ dự án yêu cầu thử lại riêng map chính ngày 2026-10-08. Nền được dựng trong Unity từ texture cỏ lặp, đường đất bằng mesh cong và 17 viên đá lát riêng. Hai cây, hồ, các đồ vật, kích thước map và sáu footprint được giữ nguyên. Chưa dựng tám map mới hoặc thêm gameplay xây dựng.

## Chỉnh trong Unity

Mở `Assets/_Project/Content/Scenes/CultivationV1.unity`, hoặc mở `Assets/_Project/Content/Prefabs/Environment/MeadowMainMap.prefab` ở Prefab Mode. Chọn **Đường đất · kéo các điểm trong Scene**: kéo các điểm vị trí để đổi đường, kéo tay nắm vàng để đổi bề rộng. Inspector có mảng `knots` để thêm/bớt điểm. Thay đổi chỉ tác động hình ảnh, không tự đổi collider.

Trong nhóm **Đá lát riêng**, mỗi viên có sprite và Transform riêng; có thể di chuyển, xoay, thay tỷ lệ hoặc nhân bản. Texture cỏ/đất và hai material nằm ở `Assets/_Project/Art/CartoonV2/Terrain/`. Các texture dùng tọa độ thế giới, mipmap/Trilinear và Mirror để mép lặp không tạo đường cắt màu. Shader giữ ánh sáng của hệ ban ngày hiện có; đá lát không có bóng elip phụ.

## Thành phần

- `MeadowGroundSurface`: mặt nền cỏ theo kích thước map.
- `MeadowTerrainPath`: đường Catmull-Rom, độ rộng từng điểm, mesh tạo lại khi chỉnh hoặc chạy scene.
- `MeadowTerrainPathEditor`: tay nắm Scene có Undo và lưu override prefab.
- `MeadowTerrain.shader`: cỏ/đất lặp và mép đường biến thiên, nhận màu ánh sáng ban ngày.
- `AgentScripts/MainMapMethod2.cs`: nhập texture, cắt sáu sprite đá qua Sprite Editor API, dựng prefab/scene và build riêng `WebBuildMainMap`.

`MeadowMap.prefab` và `ground-road.png` được giữ nguyên làm mẫu trước đó. Bản scene trước thay đổi ở `Recovery/MainMapMethod2-20261008/`. Mẫu mới lưu vào prefab riêng và chỉ liên kết vào map nền chính của scene V1; không sửa Domain, input, save hoặc thông số combat.

## Kiểm chứng

`authoring.json` ghi kiểm tra hình đường thay đổi khi kéo điểm, nền PNG cũ đã tắt, 21 đồ vật và dữ liệu sáu footprint được giữ nguyên. `saved-map-check.json` kiểm tra scene mở lại, shader và bản prefab gốc. `build.json` ghi kết quả build WebGL. `editor-floor.png` là render Unity; `web-main-map-desktop.jpg` là bản chơi WebGL ở 1280×720 và `web-main-map.jpg` là khung hẹp. Nhân vật đã được thử di chuyển trên nền mới; console không có lỗi runtime, có một cảnh báo đồng bộ save WebGL dùng API cũ. Xem kết quả thực tế trong các file này; không suy ra gameplay xây dựng đã có từ khả năng chỉnh map trong Editor.

Nguồn ảnh và mô tả tạo texture được ghi trong `generation.json`. Đây là mẫu đầu tiên để chủ dự án đánh giá hình ảnh, chưa phải thư viện terrain hoàn chỉnh cho mọi khu vực.
