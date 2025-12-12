using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace CaroServer
{
    public class Room
    {
        public string RoomID { get; set; }

        // Danh sách người chơi trong phòng (Tối đa 2)
        public List<TcpClient> Players { get; set; } = new List<TcpClient>();

        // Bàn cờ riêng của phòng này
        public int[,] Board { get; set; } = new int[15, 15];

        // Lịch sử đi (để Undo) riêng của phòng này
        public Stack<string> History { get; set; } = new Stack<string>();

        // Quản lý lượt đi và tỉ số
        public int Turn { get; set; } = 1; // 1 = X, 2 = O
        public int ScoreX { get; set; } = 0;
        public int ScoreO { get; set; } = 0;

        // Biến xử lý xin hòa/xin thua/reset
        public TcpClient RequestResetClient { get; set; } = null;
        public bool IsWaitingReset { get; set; } = false;

        public Room(string id)
        {
            this.RoomID = id;
        }

        // Hàm reset dữ liệu khi hết ván
        public void ResetGame()
        {
            Array.Clear(Board, 0, Board.Length);
            History.Clear();
            Turn = 1;
            IsWaitingReset = false;
            RequestResetClient = null;
        }
    }
}