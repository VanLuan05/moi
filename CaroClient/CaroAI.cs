using System;
using System.Collections.Generic;
using System.Drawing;

namespace CaroClient
{
    public class CaroAI
    {
        // Bảng điểm cho Tấn Công (Ưu tiên thắng)
        private long[] AttackScore = new long[] { 0, 9, 54, 162, 1458, 13122, 118098 };

        // Bảng điểm cho Phòng Thủ (Ưu tiên chặn địch)
        // Điểm phòng thủ thường để cao hơn chút để AI "an toàn là bạn"
        private long[] DefenseScore = new long[] { 0, 3, 27, 99, 729, 6561, 59049 };

        public Point Execute(int[,] board, int boardSize)
        {
            Point bestMove = new Point();
            long maxScore = 0;

            // Duyệt qua tất cả các ô trống
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    // Nếu ô chưa có ai đánh
                    if (board[i, j] == 0)
                    {
                        long attack = GetAttackScore(board, i, j, boardSize);
                        long defense = GetDefenseScore(board, i, j, boardSize);

                        // Lấy điểm cao nhất giữa công và thủ
                        // (Hoặc cộng lại nếu muốn AI cân bằng: attack + defense)
                        long tempScore = attack > defense ? attack : defense;

                        if (tempScore > maxScore)
                        {
                            maxScore = tempScore;
                            bestMove = new Point(i, j);
                        }
                    }
                }
            }
            return bestMove;
        }

        // --- HÀM TÍNH ĐIỂM TẤN CÔNG (Duyệt 4 hướng) ---
        private long GetAttackScore(int[,] board, int x, int y, int size)
        {
            long totalScore = 0;
            // Duyệt Ngang, Dọc, Chéo Chính, Chéo Phụ
            totalScore += ScoreDirection(board, x, y, 0, 1, 2, size); // Ngang (Quân 2 là Máy)
            totalScore += ScoreDirection(board, x, y, 1, 0, 2, size); // Dọc
            totalScore += ScoreDirection(board, x, y, 1, 1, 2, size); // Chéo \
            totalScore += ScoreDirection(board, x, y, 1, -1, 2, size); // Chéo /
            return totalScore;
        }

        // --- HÀM TÍNH ĐIỂM PHÒNG THỦ (Duyệt 4 hướng) ---
        private long GetDefenseScore(int[,] board, int x, int y, int size)
        {
            long totalScore = 0;
            // Duyệt giống trên nhưng tính điểm cho quân Địch (Quân 1 là Người)
            totalScore += ScoreDirection(board, x, y, 0, 1, 1, size);
            totalScore += ScoreDirection(board, x, y, 1, 0, 1, size);
            totalScore += ScoreDirection(board, x, y, 1, 1, 1, size);
            totalScore += ScoreDirection(board, x, y, 1, -1, 1, size);
            return totalScore;
        }

        // Hàm cốt lõi: Đếm số quân liên tiếp và chấm điểm
        private long ScoreDirection(int[,] board, int x, int y, int dx, int dy, int player, int size)
        {
            long score = 0;
            int countAlly = 0; // Quân ta
            int countEnemy = 0; // Quân địch chặn

            // Duyệt về 1 phía (xuôi)
            for (int i = 1; i < 5; i++)
            {
                int nx = x + i * dx;
                int ny = y + i * dy;
                if (nx < 0 || nx >= size || ny < 0 || ny >= size) { countEnemy++; break; }
                if (board[nx, ny] == player) countAlly++;
                else if (board[nx, ny] == 0) break;
                else { countEnemy++; break; }
            }

            // Duyệt về phía ngược lại (ngược)
            for (int i = 1; i < 5; i++)
            {
                int nx = x - i * dx;
                int ny = y - i * dy;
                if (nx < 0 || nx >= size || ny < 0 || ny >= size) { countEnemy++; break; }
                if (board[nx, ny] == player) countAlly++;
                else if (board[nx, ny] == 0) break;
                else { countEnemy++; break; }
            }

            // Bị chặn 2 đầu thì điểm thấp (trừ khi đã đủ 5 con)
            if (countEnemy == 2 && countAlly < 4) return 0;

            // Tra bảng điểm
            return AttackScore[countAlly];
            // Lưu ý: Đây là bảng điểm đơn giản, bạn có thể dùng DefenseScore cho hàm Defense nếu muốn tinh chỉnh
        }
    }
}