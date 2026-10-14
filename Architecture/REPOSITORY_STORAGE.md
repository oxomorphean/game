# Source và output của dự án

Áp dụng chỉ thị ngày 14/10/2026.

| Loại | Vị trí | Quy tắc |
| --- | --- | --- |
| Asset Unity, prefab, cấu hình, shader, source | `Assets/_Project/` | Giữ file + meta/GUID; không chứa KRA/PSD/SVG/Python. |
| Quy chuẩn mỹ thuật | `ArtDirection/STYLE_GUIDE.md` | Đây là file duy nhất trong ArtDirection. |
| Master artwork và backup | `~/dev/nguthu-backups/core-foundation-backup/` | Ngoài checkout, bền vững qua reboot; archive + manifest SHA-256. |
| Báo cáo ngắn | `Architecture/` | Ghi bằng chứng thật, không đưa output hàng loạt vào Git. |
| Compile log / NUnit XML | `/private/tmp/nguthu-core-foundation/<run-id>/` | Output có thể mất sau reboot; tuyệt đối không dùng làm backup. |
| Cache / build lịch sử | `Library/`, `Temp/`, `WebBuild*/` | Ignore, không thuộc phạm vi xóa Phase 1. |
| Khôi phục gameplay cũ | `Recovery/ArtOnly-20261012/` | Giữ nguyên, không tự phục hồi vào Assets. |

Backup Phase 1: `~/dev/nguthu-backups/core-foundation-backup/20261014T014359Z/source-before-phase-1.tar.gz`, với `manifest-sha256.json`, diff và status baseline cùng thư mục. Bao gồm master art và source chưa tracked. [Cách khôi phục](../ROLLBACK.md).

Không tạo lại AgentScripts, Verification hoặc Web QA. `.gitignore` là hàng rào Git, không phải cơ chế ngăn Unity import. Asset di chuyển bằng git mv kèm meta; file untracked chuyển cả cặp và đối chiếu GUID. Không di trú LFS hoặc rewrite history nếu chưa được chủ dự án yêu cầu. `.gitattributes` hiện có được giữ.
