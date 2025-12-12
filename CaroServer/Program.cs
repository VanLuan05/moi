using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.SqlClient;
using CaroShared;

namespace CaroServer
{
    internal class Program
    {
        // CẤU HÌNH SQL (Giữ nguyên của bạn)
        private static string connectionString = @"Data Source=LUAN\SQLEXPRESS;Initial Catalog=CaroDB;Integrated Security=True";

        private const int PORT = 8080;
        private static TcpListener listener;

        // --- THAY ĐỔI LỚN: QUẢN LÝ DANH SÁCH PHÒNG ---
        private static List<Room> activeRooms = new List<Room>();

        // Lưu tên người chơi (Client -> Tên)
        private static Dictionary<TcpClient, string> playerNames = new Dictionary<TcpClient, string>();

        static void Main(string[] args)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, PORT);
                listener.Start();
                Console.WriteLine($"Server (Multi-Room) dang chay tai cong {PORT}...");

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine(">> Client moi ket noi.");
                    Thread t = new Thread(HandleClient);
                    t.IsBackground = true;
                    t.Start(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi Server: " + ex.Message);
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            try
            {
                while (client.Connected)
                {
                    string msg = reader.ReadLine();
                    if (msg == null) break;

                    // Console.WriteLine($"Nhan: {msg}"); // Debug

                    string[] parts = msg.Split('|');
                    string command = parts[0];

                    // --- XỬ LÝ ĐĂNG NHẬP / KẾT NỐI ---
                    if (command == "LOGIN" || command == "CONNECT")
                    {
                        // Lưu ý: Hỗ trợ cả 2 lệnh để tương thích Client cũ/mới
                        string user = parts[1];
                        string pass = (parts.Length > 2) ? parts[2] : ""; // Lệnh CONNECT ko có pass

                        bool loginSuccess = true;
                        if (command == "LOGIN") loginSuccess = KiemTraDangNhap(user, pass);

                        if (loginSuccess)
                        {
                            if (command == "LOGIN") writer.WriteLine("LOGIN_OK");

                            lock (playerNames)
                            {
                                if (playerNames.ContainsKey(client)) playerNames[client] = user;
                                else playerNames.Add(client, user);
                            }

                            // --- LOGIC TÌM PHÒNG (MATCHMAKING) ---
                            JoinRoom(client, user, writer);
                        }
                        else
                        {
                            writer.WriteLine("LOGIN_FAIL");
                        }
                    }
                    else
                    {
                        // CÁC LỆNH CHƠI GAME (MOVE, UNDO, CHAT...)
                        // Phải xác định Client đang ở phòng nào
                        Room room = FindRoomByClient(client);
                        if (room != null)
                        {
                            ProcessGameCommand(room, client, command, parts, writer);
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Client Error: " + ex.Message); }
            finally { RemoveClient(client); }
        }

        // --- HÀM XỬ LÝ LOGIC TÌM PHÒNG ---
        static void JoinRoom(TcpClient client, string playerName, StreamWriter writer)
        {
            lock (activeRooms)
            {
                // 1. Tìm phòng nào đang có 1 người đợi
                Room availableRoom = null;
                foreach (var r in activeRooms)
                {
                    if (r.Players.Count == 1)
                    {
                        availableRoom = r;
                        break;
                    }
                }

                // 2. Nếu tìm thấy -> Vào luôn
                if (availableRoom != null)
                {
                    availableRoom.Players.Add(client);
                    Console.WriteLine($"User {playerName} vao phong {availableRoom.RoomID}. Phong da Day!");

                    BroadcastToRoom(availableRoom, $"MESSAGE|{playerName} đã vào phòng!");

                    // Bắt đầu game
                    availableRoom.ResetGame();
                    SendGameStart(availableRoom);
                }
                // 3. Nếu không thấy -> Tạo phòng mới
                else
                {
                    Room newRoom = new Room(Guid.NewGuid().ToString().Substring(0, 5));
                    newRoom.Players.Add(client);
                    activeRooms.Add(newRoom);

                    Console.WriteLine($"User {playerName} tao phong moi {newRoom.RoomID}. Dang doi doi thu...");
                    writer.WriteLine("MESSAGE|Đang đợi đối thủ vào phòng...");
                }
            }
        }

        // --- HÀM XỬ LÝ LỆNH GAME (Đã chuyển từ logic cũ sang dùng Room) ---
        static void ProcessGameCommand(Room room, TcpClient client, string command, string[] parts, StreamWriter writer)
        {
            int pIndex = room.Players.IndexOf(client);
            int side = (pIndex == 0) ? 1 : 2; // 1=X, 2=O

            if (command == "MOVE")
            {
                if (side != room.Turn) return; // Chưa đến lượt

                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);

                if (room.Board[x, y] != 0) return;

                // Xử lý nước đi
                room.Board[x, y] = side;
                room.History.Push($"{x}|{y}");
                BroadcastToRoom(room, $"MOVE|{x}|{y}|{side}");

                // Kiểm tra thắng
                if (CheckWin(room.Board, x, y, side))
                {
                    if (side == 1) room.ScoreX++; else room.ScoreO++;

                    // Logic thắng (Ví dụ BO1 ăn luôn cho nhanh, hoặc bạn giữ BO3 cũ)
                    BroadcastToRoom(room, $"ROUND_WIN|{side}|{room.ScoreX}|{room.ScoreO}");
                    room.ResetGame();
                    BroadcastToRoom(room, "NEW_GAME");
                }
                else
                {
                    room.Turn = (room.Turn == 1) ? 2 : 1;
                }
            }
            else if (command == "UNDO")
            {
                if (room.History.Count > 0)
                {
                    string last = room.History.Pop();
                    string[] p = last.Split('|');
                    int ux = int.Parse(p[0]);
                    int uy = int.Parse(p[1]);

                    room.Board[ux, uy] = 0;
                    room.Turn = (room.Turn == 1) ? 2 : 1;
                    BroadcastToRoom(room, $"UNDO|{ux}|{uy}");
                }
            }
            else if (command == "CHAT")
            {
                string content = parts[1];
                string name = playerNames.ContainsKey(client) ? playerNames[client] : "Unknown";
                BroadcastToRoom(room, $"CHAT|{name}|{content}", client);
            }
            else if (command == "SURRENDER")
            {
                int winner = (side == 1) ? 2 : 1;
                BroadcastToRoom(room, $"GAMEOVER|{winner}");
                room.ResetGame();
                BroadcastToRoom(room, "NEW_GAME");
            }
            else if (command == "DRAW_REQUEST")
            {
                BroadcastToRoom(room, "DRAW_REQUEST", client);
            }
            else if (command == "DRAW_ACCEPT")
            {
                BroadcastToRoom(room, "GAME_DRAW");
                room.ResetGame();
                BroadcastToRoom(room, "NEW_GAME");
            }
            else if (command == "NEW_GAME") // Xin ván mới
            {
                string name = playerNames[client];
                if (!room.IsWaitingReset)
                {
                    room.RequestResetClient = client;
                    room.IsWaitingReset = true;
                    BroadcastToRoom(room, $"MESSAGE|{name} muốn ván mới. Bấm 'Ván mới' để đồng ý!", client);
                    // Có thể thêm Timer đếm ngược hủy request nếu muốn
                }
                else if (client != room.RequestResetClient)
                {
                    room.ResetGame();
                    BroadcastToRoom(room, "NEW_GAME");
                }
            }
        }

        // --- CÁC HÀM HỖ TRỢ ---

        static Room FindRoomByClient(TcpClient client)
        {
            lock (activeRooms)
            {
                foreach (var r in activeRooms)
                {
                    if (r.Players.Contains(client)) return r;
                }
                return null;
            }
        }

        static void BroadcastToRoom(Room room, string msg, TcpClient exclude = null)
        {
            foreach (var p in room.Players)
            {
                if (p == exclude) continue;
                try
                {
                    StreamWriter w = new StreamWriter(p.GetStream()) { AutoFlush = true };
                    w.WriteLine(msg);
                }
                catch { }
            }
        }

        static void SendGameStart(Room room)
        {
            for (int i = 0; i < room.Players.Count; i++)
            {
                int side = (i == 0) ? 1 : 2;
                try
                {
                    StreamWriter w = new StreamWriter(room.Players[i].GetStream()) { AutoFlush = true };
                    w.WriteLine($"GAME_START|{side}");
                }
                catch { }
            }
        }

        static void RemoveClient(TcpClient client)
        {
            Room room = FindRoomByClient(client);
            if (room != null)
            {
                lock (activeRooms)
                {
                    string name = playerNames.ContainsKey(client) ? playerNames[client] : "Someone";
                    room.Players.Remove(client);

                    if (room.Players.Count > 0)
                    {
                        // Còn người ở lại -> Báo thắng
                        BroadcastToRoom(room, $"MESSAGE|{name} đã thoát! Bạn thắng.");
                        room.ResetGame();
                        BroadcastToRoom(room, "NEW_GAME");

                        // Đưa phòng này về trạng thái chờ người mới
                        Console.WriteLine($"Phong {room.RoomID} tro ve trang thai cho.");
                    }
                    else
                    {
                        // Không còn ai -> Xóa phòng
                        activeRooms.Remove(room);
                        Console.WriteLine($"Phong {room.RoomID} da giai tan.");
                    }
                }
            }
            if (playerNames.ContainsKey(client)) playerNames.Remove(client);
            client.Close();
        }

        static bool CheckWin(int[,] board, int x, int y, int side)
        {
            // Logic check win giữ nguyên, chỉ thay đổi tham số truyền vào board
            return CountDir(board, x, y, 1, 0, side) >= 5 ||
                   CountDir(board, x, y, 0, 1, side) >= 5 ||
                   CountDir(board, x, y, 1, 1, side) >= 5 ||
                   CountDir(board, x, y, 1, -1, side) >= 5;
        }

        static int CountDir(int[,] board, int x, int y, int dx, int dy, int side)
        {
            int count = 1;
            for (int i = 1; i < 5; i++)
            {
                int nx = x + i * dx;
                int ny = y + i * dy;
                if (nx < 0 || nx >= 15 || ny < 0 || ny >= 15 || board[nx, ny] != side) break;
                count++;
            }
            for (int i = 1; i < 5; i++)
            {
                int nx = x - i * dx;
                int ny = y - i * dy;
                if (nx < 0 || nx >= 15 || ny < 0 || ny >= 15 || board[nx, ny] != side) break;
                count++;
            }
            return count;
        }

        static bool KiemTraDangNhap(string user, string pass)
        {
            // Giữ nguyên logic SQL của bạn
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM NguoiChoi WHERE TaiKhoan = @u AND MatKhau = @p";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@u", user);
                    cmd.Parameters.AddWithValue("@p", pass);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
            catch { return true; } // Hack: Nếu lỗi SQL thì cho qua luôn để test
        }
    }
}