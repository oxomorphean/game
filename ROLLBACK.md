# Khôi phục Core Foundation

Backup trước Phase 1 nằm cố định ngoài repository:

`/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261014T014359Z/`

- `source-before-phase-1.tar.gz` chứa source, asset, meta, master artwork và file chưa tracked.
- `manifest-sha256.json` chứa checksum từng file; checksum archive: `de36f8aea624bcf54cfd70f275b5ce2ea09ef91c99eb046e348f8b08caf6c8f1`.
- `status.z`, `unstaged.patch`, `staged.patch`, `tracked-files.z` ghi baseline.

Để lấy lại file Krita/PSD/SVG: giải nén archive vào một thư mục riêng ngoài repo, tìm dưới ArtDirection hoặc nhánh HeroProduction2D rồi đối chiếu checksum. Không cần khôi phục cả game để vẽ thêm trang phục; chỉ export PNG game cần dùng vào Assets theo style guide.

Để rollback code/asset: dừng thay đổi/import, kiểm tra status mới nhất, đối chiếu manifest đã duyệt và chỉ phục hồi các path của task (kèm meta và Build Settings). Bảo toàn thay đổi các phiên khác. Import và compile lại bằng Unity. Không dùng reset --hard, clean, hoặc giải nén đè toàn checkout.

Backup không ở /private/tmp; thư mục tạm chỉ giữ log/kết quả kiểm tra. Recovery của gameplay cũ và save trên máy vẫn giữ nguyên. Các nhánh/tag mỹ thuật cũ là lịch sử, không dùng builder hay server Web cũ làm workflow hiện hành.

Snapshot trước khi chuẩn hóa Client/prefab tại Phase 3:

`/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/phase3-20261014T030912Z/source-before-phase-3.tar.gz`

490 file, có manifest SHA-256 riêng; archive SHA-256 `f4ff36ab72f51e6790b6d60ff4add1cc92a5c80ac0b5ed249db8a9459648d0cb`. Dùng snapshot này để so sánh hoặc phục hồi từng prefab/script sau Phase 2; không giải nén đè cả checkout.
