using System;
using System.Collections.Generic;
using System.Drawing;

namespace CaroClient
{
    public class CaroAI
    {
        // 1. BẢNG ĐIỂM (Cân chỉnh lại chút cho "Khôn" hơn)
        // Tấn công: Ưu tiên tạo thế 3, 4
        private long[] AttackScore = new long[] { 0, 9, 54, 162, 1458, 13122, 118098 };

        // Phòng thủ: Cần chặn gấp khi địch có 3 hoặc 4
        private long[] DefenseScore = new long[] { 0, 3, 27, 99, 729, 6561, 59049 };

        private int difficultyLevel = 1;

        public void SetDifficulty(int level)
        {
            difficultyLevel = level;
        }

        public Point Execute(int[,] board, int boardSize)
        {
            // --- XỬ LÝ ĐỘ KHÓ ---

            // MỨC 1 (DỄ): 30% Đánh bừa, 70% đánh tử tế
            if (difficultyLevel == 1)
            {
                Random rand = new Random();
                if (rand.Next(0, 100) < 30) // 30% cơ hội sai lầm
                {
                    return GetRandomMove(board, boardSize);
                }
            }

            // MỨC 2 & 3: Dùng thuật toán tính điểm Heuristic (Công/Thủ)
            // (Mức Khó có thể ưu tiên Tấn công hơn Phòng thủ một chút để dồn ép)

            Point bestMove = new Point(-1, -1);
            long maxScore = -1;

            // Duyệt tất cả ô trống
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board[i, j] == 0) // Nếu ô trống
                    {
                        // Tính điểm tấn công (Nếu mình đánh vào đây)
                        long attack = GetScore(board, i, j, boardSize, AttackScore, true);

                        // Tính điểm phòng thủ (Nếu địch đánh vào đây thì nguy hiểm thế nào)
                        long defense = GetScore(board, i, j, boardSize, DefenseScore, false);

                        long tempScore = 0;

                        // CHIẾN THUẬT THEO ĐỘ KHÓ
                        if (difficultyLevel == 3) // KHÓ: Tấn công mạnh mẽ
                        {
                            // Nếu nước này tạo ra chiến thắng (>= 5 con) -> Đi ngay
                            if (attack >= AttackScore[5]) tempScore = attack * 10;
                            // Nếu không thì lấy điểm cao nhất giữa Công và Thủ
                            else tempScore = attack > defense ? attack : defense;
                        }
                        else // TRUNG BÌNH & DỄ: Cân bằng (Ưu tiên thủ hơn xíu để an toàn)
                        {
                            // Cộng dồn để vừa công vừa thủ
                            tempScore = attack + defense;
                        }

                        // Cập nhật nước đi tốt nhất
                        if (tempScore > maxScore)
                        {
                            maxScore = tempScore;
                            bestMove = new Point(i, j);
                        }
                    }
                }
            }

            // Nếu không tìm được nước nào (Bàn cờ đầy hoặc lỗi), đánh bừa
            if (bestMove.X == -1) return GetRandomMove(board, boardSize);

            return bestMove;
        }

        // --- HÀM TÍNH ĐIỂM TỔNG HỢP (Dùng chung cho cả Attack và Defense) ---
        // isAttack = true (Tính điểm cho mình), false (Tính điểm chặn địch)
        private long GetScore(int[,] board, int x, int y, int size, long[] scoreTable, bool isAttack)
        {
            long totalScore = 0;
            // Quân của ai? 
            // Nếu Attack: Tính cho Máy (Quân 2)
            // Nếu Defense: Tính cho Người (Quân 1) để chặn
            int myPiece = isAttack ? 2 : 1;
            int enemyPiece = isAttack ? 1 : 2;

            // Duyệt 4 hướng: Ngang, Dọc, Chéo Chính, Chéo Phụ
            int[] dx = { 1, 0, 1, 1 };
            int[] dy = { 0, 1, 1, -1 };

            for (int dir = 0; dir < 4; dir++)
            {
                long score = 0;
                int countAlly = 0;
                int countEnemy = 0; // Đếm số đầu bị chặn

                // Duyệt xuôi
                for (int i = 1; i < 5; i++)
                {
                    int nx = x + i * dx[dir];
                    int ny = y + i * dy[dir];
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size) { countEnemy++; break; }

                    if (board[nx, ny] == myPiece) countAlly++;
                    else if (board[nx, ny] == enemyPiece) { countEnemy++; break; }
                    else break; // Gặp ô trống
                }

                // Duyệt ngược
                for (int i = 1; i < 5; i++)
                {
                    int nx = x - i * dx[dir];
                    int ny = y - i * dy[dir];
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size) { countEnemy++; break; }

                    if (board[nx, ny] == myPiece) countAlly++;
                    else if (board[nx, ny] == enemyPiece) { countEnemy++; break; }
                    else break;
                }

                // --- LOGIC CHẤM ĐIỂM ---

                // Nếu bị chặn 2 đầu và chưa đủ 5 quân -> Nước đi vô dụng -> 0 điểm
                if (countEnemy == 2 && countAlly < 4 && countAlly < 5)
                    score = 0;
                else
                    // Tra bảng điểm (Đảm bảo không vượt quá mảng)
                    score = scoreTable[Math.Min(countAlly, 6)];

                // Cộng dồn điểm của hướng này vào tổng
                totalScore += score;
            }

            return totalScore;
        }

        private Point GetRandomMove(int[,] board, int size)
        {
            List<Point> validMoves = new List<Point>();
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    if (board[i, j] == 0) validMoves.Add(new Point(i, j));

            if (validMoves.Count > 0)
                return validMoves[new Random().Next(validMoves.Count)];

            return new Point(size / 2, size / 2);
        }
    }
}