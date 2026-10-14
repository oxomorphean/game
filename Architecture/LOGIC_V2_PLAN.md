> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Logic V2 — hoàn thiện vòng chơi local

Yêu cầu ngày 06/10/2026: thú đồng hành theo sau có độ trễ, tự đi quanh gần người; thú hoang có nhịp đi/nghỉ/đuổi/quay về; đóng Thần Đình trở lại map; người và thú có máu/chỉ số dễ xem; chọn stack và chủ động thả cạnh thân thể từ Thần Đình. Thức ăn trên đất dụ thú tới ăn, đồ chưa bị ăn nhặt lại được.

Hiện trạng: follower đi tới offset cố định mỗi frame; thú hoang chỉ đuổi khi gần; nút đóng chỉ ẩn bảng; Lure tiêu thức ăn và tính hảo cảm ngay cạnh thú; chưa có item trên đất. Máu/công/thủ đã có trong domain nhưng UI chỉ hiện một phần.

Thay đổi trong Domain (movement, expedition, drops, trạng thái và validation), Client (luồng realm, hiển thị chỉ số/thả đồ, save), AgentScripts (test) và tài liệu/dashboard. Domain C# tiếp tục sở hữu luật; input/HUD chỉ gửi command. Không làm asset hoặc map mới, multiplayer, VPS hay hệ nghề/loài mới.

Bất biến: mỗi vật phẩm chỉ ở một nơi; thả–nhặt giữ dấu chiến lợi phẩm; không mất đồ nếu kho đầy; khế ước tối đa ba; thú hồi phục không bị kéo ra; thú hoang không vượt biên/vùng sống; đổi điều khiển không dịch chuyển chớp nhoáng; đóng và G/Escape thoát realm nhất quán; bản v1 đọc được và có backup trước nâng schema.

Kiểm chứng: test pure-domain cho delay/leash/idle/control, roam/respawn, thả–nhặt/ăn/invalid/đầy kho/trip/save migration; kiểm tra nhiều frame Unity cho HUD đóng realm/máu/drop; chạy hồi quy logic hiện có, build WebGL và thao tác bản web thật. Review phạm vi diff, không sửa mỹ thuật. Hoàn tất khi các vòng chơi nêu trên chạy và có bằng chứng; lỗi trong vòng chơi liên quan được sửa trong cùng lượt.

Snapshot nguồn trước sửa ở Verification/LogicV2/before. Mọi bản save v1 được bảo tồn một lần trước lần lưu schema mới; không reset save để chữa lỗi. Việc online và cân bằng cuối không thuộc bản hoàn thiện local này.
