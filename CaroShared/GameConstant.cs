using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaroShared
{
    public class GameConstant
    {
        // Bỏ chữ 'const' hoặc 'readonly' đi
        public static int CHESS_BOARD_WIDTH = 15;
        public static int CHESS_BOARD_HEIGHT = 15;

        // Thêm cấu hình chuỗi trận
        public static int MUC_TIEU_CHIEN_THANG = 5; // Số quân liên tiếp để thắng (3x3 thì chỉ cần 3)
        public static int SO_VAN_DAU = 1; // 1 (đánh đơn), 3 (BO3), 5 (BO5)
    }
}
