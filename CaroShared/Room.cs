using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace CaroServer
{
    public class Room
    {
        public string RoomID { get; set; }
        public string Password { get; set; } // Nếu có mật khẩu (tùy chọn)
        public bool IsPrivate { get; set; } = false; // True: Phòng riêng, False: Phòng ngẫu nhiên

        public List<TcpClient> Players { get; set; } = new List<TcpClient>();
        public int[,] Board { get; set; } = new int[15, 15];
        public Stack<string> History { get; set; } = new Stack<string>();
        public int Turn { get; set; } = 1;
        public object BoardSize { get; set; }
        public bool IsGameStarted { get; set; }

        public Room(string id, bool isPrivate = false)
        {
            this.RoomID = id;
            this.IsPrivate = isPrivate;
        }

        public void ResetGame()
        {
            Array.Clear(Board, 0, Board.Length);
            History.Clear();
            Turn = 1;
        }
    }
}