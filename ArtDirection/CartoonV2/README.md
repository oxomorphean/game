# Bộ mẫu chibi góc nhìn cao

Ngự thú sư và linh quy dùng `hero.png`, `huyen-quy.png`. Mẫu map chính trong `CultivationV1.unity` dùng `MeadowMainMap.prefab`: texture cỏ/đất lặp, đường mesh chỉnh bằng điểm uốn và đá lát riêng, theo lựa chọn cách 2 của chủ dự án. Hướng dẫn và bằng chứng ở [MainMapMethod2/README.md](MainMapMethod2/README.md). Mẫu dùng chung `MeadowMap.prefab` vẫn giữ nền PNG trước đó để so sánh. Đồ vật trong `Environment/` vẫn là anh đào, đá, bụi cỏ, hoa trắng, bụi cây và cụm hồ nước. Ảnh được tạo bằng ImageGen tích hợp; prompt và nguồn gốc nằm trong các JSON đi kèm. `field.png` là tham chiếu thử nghiệm cũ.

## Bố trí mẫu PNG dùng chung trong Unity

1. Tắt Play. Chọn menu **Demo → Môi trường → Mở bố cục map dùng chung** để mở `MeadowMap.prefab`.
2. Trong Hierarchy, mở nhóm **Cây · đá · cỏ · hoa** hoặc **Hồ nước trọn cụm**. Chọn đúng gốc đối tượng (ví dụ **Anh đào trái**), nhấn **W** rồi kéo. Đổi Scale hoặc nhân bản bằng **Cmd+D** nếu cần.
3. **Cmd+S** lưu bố cục. Prefab này được cảnh mẫu và bản logic dùng chung. Không cần tạo lại ảnh.

Có thể kéo đồ vật trực tiếp trong scene mẫu rồi lưu scene. Để chuyển các chỉnh sửa đó sang map dùng chung, chọn **Demo → Môi trường → Lưu bố cục cảnh mẫu vào map dùng chung**. Các chỉnh sửa trong Play chỉ là tạm thời và mất khi thoát Play.

Prefab đồ vật riêng nằm trong `Assets/_Project/Content/Prefabs/Environment/`. Kéo một prefab từ Project vào nhóm map để thêm bản sao. Hồ, đá bờ, cỏ bờ và lá sen là một PNG/prefab trọn cụm; kéo gốc **Hồ nước trọn cụm** di chuyển cả hồ. Đá ngoài hồ, cây, hoa và bụi cỏ khác vẫn là đối tượng riêng. Trong mẫu PNG dùng chung, đường và đá lát thuộc nền cố định; trong map chính mới, từng viên đá lát có Transform riêng. Bóng cây là sprite con, di chuyển theo cây. Phần mỹ thuật không sở hữu luật va chạm hoặc xây dựng.

## Cấu trúc và nhập ảnh

`ModularMapBuilder` chỉ tạo prefab mặc định khi chưa có, giữ các vị trí đã chỉnh. PNG gốc được giữ nguyên; importer tính pivot ở chân theo silhouette alpha. Bilinear, không mipmap, FullRect, PPU100 ở source (Unity điều chỉnh PPU theo texture resize); môi trường tối đa 2048 để giữ đầy đủ độ phân giải PNG gốc, không thu cây/hồ xuống 1024 hoặc đồ nhỏ xuống 512. Kích thước đồ vật tính theo phần ảnh nhìn thấy và bounds sau import, tránh thay đổi tỷ lệ do giới hạn texture. `ModularMapProp` xếp lớp theo chân; lớp nền và bóng nằm dưới nhân vật. `ModularMapLayout` lưu kích thước và các đối tượng. HUD Canvas dựng các ảnh riêng từ cùng prefab và sắp người/thú/cây/đá theo chiều sâu, không thay domain hoặc save. Preview combat WebGL render tối thiểu 2 pixel trên mỗi CSS pixel, tối đa 3 theo màn hình, giúp giữ nét khi phóng to; khung canvas tự khớp kích thước hiển thị.

`CartoonCharacterRig` dùng SpriteSkin sáu bone cho người và thú, `CartoonTrialDirector` quản lý input/follow riêng. Người đứng thở nhẹ với hai chân yên, bước chạy luân phiên nhấc chân và vung tay; chuyển idle/walk có độ chuyển tiếp. Trong combat, tay kiếm lấy đà rồi chém, thân có phản ứng ra đòn, đổi màu khi trúng đòn và nghiêng khi gục. Pose và animation chỉ tác động xương/renderer; vị trí chân, timing damage và trạng thái hồi sinh thuộc controller/domain. Đây là skin lưới từ một bitmap, chưa có bộ hướng lên/xuống riêng.

**Bóng đổ** của cây, đá, bụi cây, cỏ, hoa và nhân vật lấy alpha của chính sprite nguồn, giữ hình tán lá/thân/kiếm. Cây anh đào chỉ dùng bóng silhouette, không còn bóng elip dưới gốc. Các đồ vật thấp có bóng tiếp xúc nhỏ sát chân. Shader chiếu bóng từ chân xuống mặt đất theo hướng mặt trời; trưa bóng ngắn, sáng/chiều bóng dài và đổi hướng. Hồ là mặt đất/nước nên không đổ bóng toàn bộ mặt hồ. Bóng nằm dưới người/thú. `ArtPresentationBuilder.Apply()` chuẩn bị material và prefab qua Editor API; nhập lại môi trường cũng gọi bước này. `PrototypeMapArt` đưa cùng silhouette vào Canvas. PNG nguồn, vị trí/scale gốc của map và collider combat giữ nguyên.

`DaylightPresentation` bắt đầu ở 8h, lặp chu kỳ 6h–18h; mặc định `dayDurationSeconds = 240` cho một chu kỳ, chỉnh được trong Inspector. Mặt trời ẩn mọc bên trái (−X), lên cao vào trưa và lặn bên phải (+X); bóng ngả phải vào sáng, ngả trái vào chiều, ánh vàng tăng từ 14h. Chỉ thanh giờ xuất hiện trên màn hình. Bóng đá dùng phép chiếu rộng hơn cây để giữ khối khi mặt trời thấp, vẫn giữ điểm chân và footprint ngắn gần trưa; shader và Canvas dùng cùng tham số. `SetHour(6..18)` dùng để duyệt các thời điểm; `advanceTime` bật/tắt chuyển giờ. Thời gian này chỉ phục vụ hình ảnh, không phải đồng hồ gameplay, không đọc/ghi save. Phần Logic tiếp tục sở hữu vật cản, spawn, sát thương và mọi luật liên quan thời gian; interface animation `SetMotion`/`SetCombatPose` giữ nguyên. Hiện chỉ dựng ánh sáng 2D từ sprite, chưa có bóng từ mô hình 3D hoặc buổi tối.

## Hoa rơi, gốc cây và gợn hồ

`ModularMapLayout` gắn `GardenAmbientMesh` vào phần hình của anh đào và hồ khi scene chạy. `GardenAmbientGraphic` dựng cùng chi tiết trong map Canvas. Hai cây có cánh hoa hồng rơi chậm, nghiêng/lắc theo gió, rồi mờ đi gần mặt đất; một ít cánh hoa nằm yên và cỏ nhỏ che các mép rễ để gốc hòa vào nền. Không thêm lại bóng elip. Hồ có ba cụm gợn cung ngắn nở và tan dần, nằm trong nước, tránh đá bờ và lá sen.

Đây là mesh nhỏ vẽ từ code theo tọa độ của sprite, không có PNG mới, collider, random dùng chung với gameplay hoặc thay đổi vị trí vật cản. Hiệu ứng đi theo Transform hiện có, dùng cùng màu ánh nắng và không nhận input. Nhận xét gốc cây là đánh giá thị giác: rễ có viền kín trên cỏ phẳng, thiếu cỏ che mép; các chi tiết bổ sung thử khắc phục điều đó, không thay thiết kế thân/tán gốc.

## Khôi phục

`Verification/CartoonTrial/before-modular-props.tgz` giữ source và hai scene trước khi tách môi trường; các ảnh cũ vẫn được giữ. Bản nén nhỏ, không sao chép Library hoặc cả dự án.
