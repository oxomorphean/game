# Tăng độ phân giải nhân vật — 09/10/2026

Sửa nhỏ theo yêu cầu chủ dự án: ô frame 128→256 px, render 256→512 px, PPU 64→128; chiều cao thế giới 1.25 và pivot chân giữ nguyên. Importer chọn giới hạn đủ kích thước sheet theo manifest. Giữ compression HQ, mipmap/Trilinear, animation, hướng nhìn và runtime renderer. Sheet chạy lớn nhất 3072×2048; chưa đổi sang rig tách lớp hoặc bổ sung góc chéo.

416 frame xuất thành công, 0 frame rỗng hoặc cắt mép. Tổng PNG tăng từ 2.908.286 lên 10.014.051 byte; đây là chi phí tăng độ phân giải, không phải kết quả đo RAM hoặc FPS runtime. Asset definition/importer giữ sprite ID đã có; không ghi scene/prefab.

HeroDirectionChecks 47/47; HeroDirectionRuntime 31/31, dùng Input System/Update thật trong campaign RAM tạm, khôi phục campaign cũ và không Save. Ảnh front/back trong thư mục này. Các bài này kiểm tra hoạt động, không chứng minh animation đẹp hoặc tám góc vẽ khác nhau.

WebBuildMainMap cập nhật cổng 8794: Succeeded, 0 lỗi, 2 cảnh báo Pipeline Runtime config và TextMeshPro đã có, khoảng 19.29 giây. Bản web đã tải lại, tiếp tục phiên có sẵn, di chuyển xuống nhìn mặt trước ở viewport 1280×720, browser không lỗi/cảnh báo. Ảnh `web-desktop.jpg`; viewport đã trả mặc định. Console Unity 0 lỗi, compilationFailed=false. Hai scene và hai prefab map giữ nguyên SHA-256 (`protected-before.sha256`).

Khôi phục: `Recovery/HeroSharpness-20261009/before.tgz` lưu sheet/definition/baker/importer trước chỉnh. Đề xuất kiến trúc riêng, chưa duyệt: `ArtDirection/HeroPainted2D/MODULAR_CHARACTER_PROPOSAL.md`. Chưa chạy lại Domain/save/capacity suite vì lượt này không đổi các phần đó; chưa cập nhật build ở cổng 8791.
