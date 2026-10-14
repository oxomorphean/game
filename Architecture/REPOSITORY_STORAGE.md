# Source và output của dự án

## Giữ ở đâu

| Loại | Vị trí | Git |
| --- | --- | --- |
| Asset dùng trong Unity, scene, prefab, definition, shader | `Assets/_Project/` | Giữ source và `.meta`; ảnh/PSD dùng LFS khi add. |
| Master artwork chỉnh sửa được | `ArtDirection/` | Giữ `.kra`, PSD, SVG, manifest, script tạo và export đầu vào builder. |
| Báo cáo kiểm tra ngắn | `Verification/` | Giữ JSON/Markdown có giá trị; số liệu cũ chỉ là lịch sử. |
| Capture, GIF, video, frame xuất hàng loạt | `Verification/`, `process-frames/`, `unity-frames/` | Output local, ignore. Chụp Unity dùng đường dẫn tuyệt đối. |
| Cache Unity và bản build | `Library/`, `WebBuild*/` | Ignore; không xóa cache trong lượt dọn source vì Unity phải nhập lại. |
| Khôi phục gameplay cũ | `Recovery/ArtOnly-20261012/` | Local, giữ nguyên cùng save cũ. |

`Assets/Verification` và `Assets/ArtDirection` không phải nơi đặt ảnh chứng minh. `.gitignore` chỉ ngăn Git thêm file mới, không ngăn Unity import nếu lại tạo ảnh ở đó. Không đưa backup `.tgz` hoặc source `before/` vào cây runtime.

## Các hướng nhân vật

Build mặc định chỉ có `ArtPreview`: map CartoonV2 và HeroPainted2D. `Hero8Directions.asset` là tên definition hiện hành, không có nghĩa đang chạy bộ Blender cũ. Bốn scene thử còn lại phục vụ duyệt hình/rig. `HeroModular2D` có material được mẫu HeroProduction2D tham chiếu; không xóa cả thư mục theo tên. Các master và export của A/B/C/C2 được giữ cho artist. Code dưới `Editor/` phục vụ nhập/dựng/kiểm tra, không biên dịch vào player; số dòng Editor không phải bằng chứng game nặng.

Hai bộ không còn tham chiếu đã gỡ: `Assets/_Project/Art/Hero8Directions` (Blender cũ), `Assets/_Project/Art/CartoonV1`. Không đổi GUID, bố cục, scene hoặc renderer đang dùng.

## Git LFS

Git LFS 3.8.0 được cài bằng Homebrew và cấu hình riêng repository (`git lfs install --local`). `.gitattributes` áp dụng cho PNG/JPG/GIF/KRA/PSD/PSB khi add file ở lần commit tới; không tự chuyển blob trong commit cũ. Lượt dọn này không stage, commit, push, upload LFS hoặc rewrite lịch sử. Sau khi clone cần `git lfs install --local` và `git lfs pull` để lấy binary đã được lưu bằng LFS.

Nén đối tượng Git bằng `git -c gc.reflogExpire=never -c gc.reflogExpireUnreachable=never gc --prune=never` giữ lịch sử và reflog. Việc chuyển lịch sử cũ sang LFS cần một task riêng nếu thực sự cần; không chạy `git lfs migrate` trong lượt dọn.

## Đợt dọn 13/10/2026

Theo yêu cầu chủ dự án, bản khôi phục ngoài checkout `nguthu-unity-demo-archive/cleanup-20261013/` đã bị xóa vĩnh viễn, giải phóng thêm khoảng 56 MiB. File lịch sử đã commit vẫn nằm trong Git; archive riêng chứa file chưa commit, manifest và script khôi phục không còn. Không rewrite lịch sử Git. Báo cáo kiểm tra đợt dọn vẫn ở `Verification/RepositoryCleanup/summary.json`.
