using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaroShared; // Cần thiết cho Task.Run

namespace CaroServer
{
    internal class Program
    {
        // --- CÁC BIẾN TOÀN CỤC ---
        private static List<TcpClient> danhSachNguoiChoi = new List<TcpClient>();
        private static Dictionary<TcpClient, string> tenNguoiChoi = new Dictionary<TcpClient, string>();
        private static int[,] banCo = new int[15, 15];
        private static int luotDiHienTai = 1;
        private static int tiSoX = 0;
        private static int tiSoO = 0;
        // Biến xử lý Reset game
        private static TcpClient nguoiYeuCauReset = null;
        private static bool dangChoReset = false;

        private const int PORT = 8080;
        private static TcpListener listener;

        static void Main(string[] args)
        {
            try
            {
                // 1. Khởi tạo Server
                listener = new TcpListener(IPAddress.Any, PORT);
                listener.Start();

                Console.WriteLine($"Server dang chay tai cong {PORT}...");
                Console.WriteLine("Dang cho nguoi choi ket noi...");

                // 2. Vòng lặp nhận kết nối
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    danhSachNguoiChoi.Add(client);

                    Console.WriteLine(">> Co nguoi choi moi ket noi!");
                    Console.WriteLine($">> So luong nguoi choi: {danhSachNguoiChoi.Count}");

                    // 3. Tạo luồng xử lý riêng cho Client này
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.IsBackground = true;
                    clientThread.Start(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi Server: " + ex.Message);
            }
        }

        // Hàm gửi tin nhắn cho TẤT CẢ người chơi (trừ người gửi nếu cần)
        static void Broadcast(string message, TcpClient excludeClient = null)
        {
            foreach (TcpClient c in danhSachNguoiChoi)
            {
                if (excludeClient != null && c == excludeClient) continue;

                try
                {
                    if (c.Connected)
                    {
                        NetworkStream stream = c.GetStream();
                        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                        writer.WriteLine(message);
                    }
                }
                catch
                {
                    // Bỏ qua nếu Client đó bị lỗi kết nối
                }
            }
        }

        // --- XỬ LÝ CHÍNH CHO TỪNG NGƯỜI CHƠI ---
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
                    string message = reader.ReadLine();
                    if (message == null) break;

                    Console.WriteLine($"Nhan tu Client: {message}");

                    string[] parts = message.Split('|');
                    string command = parts[0];

                    // --- XỬ LÝ CÁC LỆNH ---

                    if (command == "CONNECT")
                    {
                        string name = parts[1];
                        if (tenNguoiChoi.ContainsKey(client))
                            tenNguoiChoi[client] = name;
                        else
                            tenNguoiChoi.Add(client, name);

                        Console.WriteLine($"=> Nguoi choi '{name}' da tham gia.");
                        Broadcast("MESSAGE|Người chơi " + name + " đã vào phòng!");
                        if (danhSachNguoiChoi.Count == 2)
                        {
                            Console.WriteLine("=> Da du 2 nguoi. Bat dau game!");

                            // Reset dữ liệu bàn cờ cho chắc chắn
                            ResetGameData();

                            // Gửi lệnh BẮT ĐẦU cho cả 2 máy
                            Broadcast("GAME_START");
                        }
                    }
                    else if (command == "MOVE")
                    {
                        int playerIndex = danhSachNguoiChoi.IndexOf(client);
                        int side = (playerIndex == 0) ? 1 : 2;

                        // Kiểm tra lượt đi
                        if (side != luotDiHienTai)
                        {
                            writer.WriteLine("MESSAGE|Chưa đến lượt của bạn!");
                            continue;
                        }

                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);

                        // Kiểm tra ô đã đánh chưa
                        if (banCo[x, y] != 0)
                        {
                            writer.WriteLine("MESSAGE|Ô này đã có người đánh rồi!");
                            continue;
                        }

                        // Cập nhật và thông báo
                        banCo[x, y] = side;
                        Broadcast($"MOVE|{x}|{y}|{side}");

                        // Kiểm tra thắng thua
                        if (CheckWin(x, y, side))
                        {
                            // Cập nhật tỉ số
                            if (side == 1) tiSoX++; else tiSoO++;

                            // Kiểm tra đã thắng chuỗi chưa (Ví dụ BO3: Ai thắng 2 trước là thắng)
                            int soVanCanThang = (GameConstant.SO_VAN_DAU / 2) + 1;

                            if (tiSoX >= soVanCanThang)
                            {
                                Broadcast("SERIES_WIN|1"); // X thắng chung cuộc
                                tiSoX = 0; tiSoO = 0; // Reset tỉ số
                            }
                            else if (tiSoO >= soVanCanThang)
                            {
                                Broadcast("SERIES_WIN|2"); // O thắng chung cuộc
                                tiSoX = 0; tiSoO = 0;
                            }
                            else
                            {
                                // Chưa thắng chung cuộc -> Chỉ báo thắng ván này và cập nhật tỉ số
                                Broadcast($"ROUND_WIN|{side}|{tiSoX}|{tiSoO}");
                                // Tự động Reset bàn cờ để đánh ván tiếp
                                ResetGameData();
                                Broadcast("NEW_GAME");
                            }
                        }
                        else
                        {
                            // Đổi lượt
                            luotDiHienTai = (luotDiHienTai == 1) ? 2 : 1;
                        }
                    }
                    else if (command == "NEW_GAME")
                    {
                        string name = tenNguoiChoi.ContainsKey(client) ? tenNguoiChoi[client] : "Đối thủ";

                        // Nếu chưa ai yêu cầu -> Đây là yêu cầu mới
                        if (!dangChoReset)
                        {
                            nguoiYeuCauReset = client;
                            dangChoReset = true;

                            writer.WriteLine("MESSAGE|Đã gửi yêu cầu. Chờ đối thủ 5s...");
                            Broadcast($"MESSAGE|{name} muốn ván mới. Bấm 'Ván mới' để đồng ý!", client);

                            // Đếm ngược 5s
                            Task.Run(async () =>
                            {
                                await Task.Delay(5000);
                                if (dangChoReset)
                                {
                                    dangChoReset = false;
                                    nguoiYeuCauReset = null;
                                    Broadcast("MESSAGE|Yêu cầu ván mới đã hết hạn!");
                                }
                            });
                        }
                        // Nếu đã có người yêu cầu -> Đây là sự đồng ý
                        else if (dangChoReset && client != nguoiYeuCauReset)
                        {
                            ResetGameData();
                            Broadcast("NEW_GAME");
                            Broadcast("MESSAGE|Đã tạo ván mới!");

                            dangChoReset = false;
                            nguoiYeuCauReset = null;
                        }
                    }
                    else if (command == "TIME_OUT")
                    {
                        int playerIndex = danhSachNguoiChoi.IndexOf(client);
                        int sideBiThua = (playerIndex == 0) ? 1 : 2;
                        int sideThang = (sideBiThua == 1) ? 2 : 1;

                        Broadcast($"GAMEOVER|{sideThang}");
                        ResetGameData();
                        Broadcast("NEW_GAME"); // Tự động dọn bàn cờ cho mọi người
                    }
                    else if (command == "CHAT")
                    {
                        string noiDung = parts[1];
                        string name = tenNguoiChoi.ContainsKey(client) ? tenNguoiChoi[client] : "Đối thủ";
                        Broadcast($"CHAT|{name}|{noiDung}", client);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi ket noi: " + ex.Message);
            }
            finally
            {
                // --- XỬ LÝ KHI NGƯỜI CHƠI THOÁT ---
                string name = "Người chơi";

                // 1. Lấy tên trước khi xóa
                if (tenNguoiChoi.ContainsKey(client))
                {
                    name = tenNguoiChoi[client];
                    tenNguoiChoi.Remove(client);
                }

                // 2. Xóa khỏi danh sách kết nối
                if (danhSachNguoiChoi.Contains(client))
                {
                    danhSachNguoiChoi.Remove(client);
                    client.Close();
                }

                Console.WriteLine($"=> {name} da ngat ket noi.");

                // 3. Nếu còn người ở lại -> Báo thắng cuộc và Reset
                if (danhSachNguoiChoi.Count > 0)
                {
                    Broadcast($"MESSAGE|{name} đã thoát! Bạn chiến thắng.");

                    ResetGameData();
                    Broadcast("NEW_GAME"); // Báo Client xóa bàn cờ

                    // Hủy trạng thái chờ reset nếu có
                    dangChoReset = false;
                    nguoiYeuCauReset = null;
                }

            }
        }

        // Hàm phụ: Reset dữ liệu bàn cờ trên Server
        static void ResetGameData()
        {
            Array.Clear(banCo, 0, banCo.Length);
            luotDiHienTai = 1;
        }

        // Hàm kiểm tra thắng thua
        static bool CheckWin(int x, int y, int side)
        {
            return CountDir(x, y, 1, 0, side) >= 5 || // Ngang
                   CountDir(x, y, 0, 1, side) >= 5 || // Dọc
                   CountDir(x, y, 1, 1, side) >= 5 || // Chéo chính
                   CountDir(x, y, 1, -1, side) >= 5;  // Chéo phụ
        }

        static int CountDir(int x, int y, int dx, int dy, int side)
        {
            int count = 1;
            for (int i = 1; i < 5; i++)
            {
                int nx = x + i * dx;
                int ny = y + i * dy;
                if (nx < 0 || nx >= 15 || ny < 0 || ny >= 15 || banCo[nx, ny] != side) break;
                count++;
            }
            for (int i = 1; i < 5; i++)
            {
                int nx = x - i * dx;
                int ny = y - i * dy;
                if (nx < 0 || nx >= 15 || ny < 0 || ny >= 15 || banCo[nx, ny] != side) break;
                count++;
            }
            return count;
        }
    }
}