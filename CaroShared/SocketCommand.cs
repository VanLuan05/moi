using System;
using System.Drawing; // Dòng này cực quan trọng để sửa lỗi Point

namespace CaroShared
{
    // 1. Định nghĩa các lệnh giao tiếp (Protocol)
    public enum SocketCommand
    {
        CONNECT,        // Kết nối
        SEND_POINT,     // Gửi nước đi
        NOTIFY,         // Thông báo
        NEW_GAME,       // Ván mới
        UNDO,           // Đi lại
        END_GAME,       // Kết thúc game
        QUIT            // Thoát
    }

    // 2. Gói tin dùng để gửi đi
    [Serializable] // Cho phép biến đổi object này thành dòng dữ liệu để truyền qua mạng
    public class SocketData
    {
        public SocketCommand Command { get; set; }

        // Point thuộc về thư viện System.Drawing
        public Point Point { get; set; }

        public string Message { get; set; }
    }
}