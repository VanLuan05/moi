using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography; // Để dùng MD5
using System.Text;
using System.Threading;
namespace CaroServer
{
    internal class Program
    {
        private static string connectionString = @"Data Source=LUAN\SQLEXPRESS;Initial Catalog=CaroDB;Integrated Security=True";
        private const int PORT = 8080;
        private static TcpListener listener;

        // --- CẤU TRÚC DỮ LIỆU ---
        private static List<Room> activeRooms = new List<Room>();
        private static Dictionary<TcpClient, PlayerInfo> onlinePlayers = new Dictionary<TcpClient, PlayerInfo>();
        // Lưu trữ những người chơi vừa bị rớt mạng (Key: UserID, Value: Thông tin phòng/trận đấu)
        private static Dictionary<int, PendingReconnect> pendingDisconnects = new Dictionary<int, PendingReconnect>();

        class PendingReconnect
        {
            public PlayerInfo Info { get; set; }
            public Room Room { get; set; }
            public DateTime DisconnectTime { get; set; }
        }
        private static readonly object _lock = new object();

        class PlayerInfo
        {
            public int UserID { get; set; }
            public string Username { get; set; }
            public string DisplayName { get; set; }
            public int AvatarID { get; set; } = 0;
            public bool IsAdmin { get; set; } = false;
            public bool IsInGame { get; set; } = false;
            public bool IsLoggedIn { get; set; } = false;
        }

        class Room
        {
            public string RoomID { get; set; }
            public int CurrentMatchID { get; set; } = -1;
            public bool IsPrivate { get; set; }
            public List<TcpClient> Players { get; set; }
            public int[,] Board { get; set; }
            public Stack<string> History { get; set; }
            public int Turn { get; set; } // 1 = X, 2 = O
            public bool IsGameStarted { get; set; }
            public int BoardSize { get; set; } // Kích thước bàn cờ

            public Room(string roomId, bool isPrivate, int boardSize)
            {
                RoomID = roomId;
                IsPrivate = isPrivate;
                Players = new List<TcpClient>();
                BoardSize = boardSize;
                Board = new int[boardSize, boardSize];
                History = new Stack<string>();
                Turn = 1;
                IsGameStarted = false;
            }

            public void ResetGame()
            {
                Board = new int[BoardSize, BoardSize];
                History.Clear();
                Turn = 1;
                IsGameStarted = true;
            }

            public int GetPlayerSide(TcpClient client)
            {
                int index = Players.IndexOf(client);
                return index == 0 ? 1 : (index == 1 ? 2 : 0);
            }
        }

        static void Main(string[] args)
        {
            // --- ĐOẠN TEST KẾT NỐI (Thêm vào đây) ---
            Console.WriteLine("Dang kiem tra ket noi SQL...");
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open(); // Cố gắng mở cửa vào DB
                    Console.WriteLine("✅ KẾT NỐI SQL THÀNH CÔNG!");
                    Console.WriteLine($"   Server Version: {conn.ServerVersion}");
                    Console.WriteLine($"   Database: {conn.Database}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ LỖI KẾT NỐI SQL:");
                Console.WriteLine($"   Lý do: {ex.Message}");
                Console.WriteLine("   --> Hãy kiểm tra lại 'connectionString'!");
                Console.ReadKey(); // Dừng màn hình để đọc lỗi
                return; // Dừng chương trình luôn
            }
            Console.WriteLine("----------------------------------");
            // ----------------------------------------
            try
            {
                listener = new TcpListener(IPAddress.Any, PORT);
                listener.Start();
                Console.WriteLine("=========================================");
                Console.WriteLine("    CARO SERVER PRO - VERSION 2.0");
                Console.WriteLine("=========================================");
                Console.WriteLine($"Địa chỉ: {IPAddress.Any}:{PORT}");
                Console.WriteLine($"Database: {connectionString}");
                Console.WriteLine("Chức năng hỗ trợ:");
                Console.WriteLine("1. Đăng ký tài khoản mới");
                Console.WriteLine("2. Đăng nhập tài khoản");
                Console.WriteLine("3. Chơi ngay (Guest)");
                Console.WriteLine("4. Tìm trận ngẫu nhiên");
                Console.WriteLine("5. Tạo phòng riêng (chỉ User)");
                Console.WriteLine("6. Vào phòng riêng (chỉ User)");
                Console.WriteLine("7. Bàn cờ 10x10, 15x15, 20x20");
                Console.WriteLine("=========================================");
                Console.WriteLine(">> SERVER ĐANG CHẠY...");
                // Chạy luồng dọn dẹp các kết nối chờ (ngay trước vòng lặp while)
                new Thread(CleanupPendingDisconnects).Start();
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread t = new Thread(HandleClient);
                    t.IsBackground = true;
                    t.Start(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi Server: {ex.Message}");
                Console.ReadKey();
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            Console.WriteLine($">> Client kết nối từ: {clientIP}");

            try
            {
                while (client.Connected)
                {
                    string msg = reader.ReadLine();
                    if (msg == null) break;

                    string[] parts = msg.Split('|');
                    string command = parts[0];

                    Console.WriteLine($">> [{clientIP}] Lệnh: {command}");

                    // --- XỬ LÝ ĐĂNG KÝ TÀI KHOẢN ---
                    if (command == "REGISTER")
                    {
                        HandleRegister(client, parts, writer);
                    }
                    else if (command == "RESET_PASS")
                    {
                        string email = parts[1];
                        HandleResetPassword(client, email, writer);
                    }
                    // --- XỬ LÝ ĐĂNG NHẬP GUEST ---
                    else if (command == "QUICK_CONNECT")
                    {
                        string name = parts[1];
                        if (string.IsNullOrWhiteSpace(name))
                            name = "Khach_" + new Random().Next(100, 999);

                        RegisterPlayer(client, name, name, false, false);
                        writer.WriteLine($"LOGIN_SUCCESS|{name}|0");
                        Console.WriteLine($">> Guest: {name} đã kết nối");
                    }
                    // --- XỬ LÝ ĐĂNG NHẬP TÀI KHOẢN ---
                    else if (command == "LOGIN")
                    {
                        string user = parts[1];
                        string pass = parts[2];

                        // Khai báo biến hứng giá trị avatar
                        int uId, uAvatar;
                        string dName;
                        bool adm;

                        // Gọi hàm mới sửa
                        if (CheckLoginDB(user, pass, out uId, out dName, out adm, out uAvatar))
                        {
                            lock (_lock)
                            {
                                // --- LOGIC MỚI: KIỂM TRA TÁI KẾT NỐI ---
                                if (pendingDisconnects.ContainsKey(uId))
                                {
                                    var pending = pendingDisconnects[uId];

                                    // 1. Khôi phục thông tin vào onlinePlayers với Socket mới (client hiện tại)
                                    RegisterPlayer(client, user, dName, adm, true, uId, uAvatar);

                                    // 2. Cập nhật lại Socket trong Room (Thay cái socket cũ chết bằng socket mới)
                                    Room room = pending.Room;
                                    for (int i = 0; i < room.Players.Count; i++)
                                    {
                                        // Tìm socket cũ trong Room (Socket không nằm trong onlinePlayers nữa là socket cũ)
                                        if (!onlinePlayers.ContainsKey(room.Players[i]))
                                        {
                                            room.Players[i] = client; // Thay thế
                                            break;
                                        }
                                    }

                                    // 3. Xóa khỏi danh sách chờ
                                    pendingDisconnects.Remove(uId);

                                    writer.WriteLine($"LOGIN_SUCCESS|{dName}|{(adm ? "1" : "0")}");
                                    Console.WriteLine($">> {user} đã tái kết nối thành công!");

                                    // 4. GỬI LẠI BÀN CỜ ĐỂ CLIENT VẼ LẠI
                                    // Format: RECONNECT_GAME | Side | BoardSize | OpponentName | OpponentAvatar | HistoryString

                                    int myIndex = room.Players.IndexOf(client);
                                    int side = (myIndex == 0) ? 1 : 2;
                                    TcpClient op = (room.Players.Count > 1) ? room.Players[(side == 1) ? 1 : 0] : null;
                                    string opName = (op != null && onlinePlayers.ContainsKey(op)) ? onlinePlayers[op].DisplayName : "Unknown";
                                    int opAvatar = (op != null && onlinePlayers.ContainsKey(op)) ? onlinePlayers[op].AvatarID : 0;

                                    // Gom lịch sử nước đi thành chuỗi: "x1,y1;x2,y2;..."
                                    // Lưu ý: History là Stack, cần Reverse để lấy đúng thứ tự từ đầu
                                    string historyStr = string.Join(";", room.History.Reverse().ToArray());

                                    SendToClient(client, $"RECONNECT_GAME|{side}|{room.BoardSize}|{opName}|{opAvatar}|{historyStr}");

                                    // Báo cho đối thủ biết
                                    if (op != null) SendToClient(op, "MESSAGE|Đối thủ đã quay lại!");
                                    return; // Kết thúc luôn, không chạy logic Login mới
                                }
                                // ----------------------------------------
                            }

                            KickIfAccountLoggedIn(user, client);
                            RegisterPlayer(client, user, dName, adm, true, uId, uAvatar);
                            writer.WriteLine($"LOGIN_SUCCESS|{dName}|{(adm ? "1" : "0")}");
                        }
                        else
                        {
                            writer.WriteLine("LOGIN_FAIL|Sai tài khoản hoặc mật khẩu!");
                        }
                    }
                    else if (command == "GET_LEADERBOARD")
                    {
                        SendLeaderboard(writer);
                    }
                    else if (command == "GET_HISTORY")
                    {
                        if (onlinePlayers.ContainsKey(client) && onlinePlayers[client].IsLoggedIn)
                        {
                            SendHistory(client, writer);
                        }
                        else writer.WriteLine("MESSAGE|Bạn cần đăng nhập để xem lịch sử!");
                    }

                    // --- XỬ LÝ SẢNH CHỜ ---
                    else if (command == "FIND_MATCH")
                    {
                        int boardSize = parts.Length > 1 ? int.Parse(parts[1]) : 15;
                        JoinRandomMatch(client, writer, boardSize);
                    }
                    else if (command == "CREATE_PRIVATE")
                    {
                        int boardSize = parts.Length > 1 ? int.Parse(parts[1]) : 15;
                        CreatePrivateRoom(client, writer, boardSize);
                    }
                    else if (command == "JOIN_PRIVATE")
                    {
                        string roomId = parts[1];
                        int boardSize = parts.Length > 2 ? int.Parse(parts[2]) : 15;
                        JoinPrivateRoom(client, roomId, writer, boardSize);
                    }
                    // --- XỬ LÝ ADMIN ---
                    else if (command == "ADMIN_LIST")
                    {
                        if (IsAdmin(client)) SendAdminData(writer);
                    }
                    else if (command == "ADMIN_KICK")
                    {
                        if (IsAdmin(client)) KickUser(parts[1]);
                    }
                    // --- XỬ LÝ TRONG GAME ---
                    else
                    {
                        Room room = FindRoomByClient(client);
                        if (room != null)
                        {
                            ProcessGameCommand(room, client, command, parts, writer);
                        }
                        else
                        {
                            writer.WriteLine("ERROR|Bạn không ở trong phòng nào!");
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($">> Lỗi xử lý client {clientIP}: {ex.Message}");
            }
            finally
            {
                RemoveClient(client);
                Console.WriteLine($">> Client ngắt kết nối: {clientIP}");
            }
        }
        // --- HÀM KIỂM TRA VÀ KICK TÀI KHOẢN ĐANG ONLINE ---
        static void KickIfAccountLoggedIn(string username, TcpClient currentClient)
        {
            TcpClient oldClient = null;

            lock (_lock)
            {
                // Duyệt danh sách để tìm xem có ai đang dùng Username này không
                foreach (var item in onlinePlayers)
                {
                    // Kiểm tra: Cùng Username, Đã đăng nhập, và KHÔNG PHẢI là client hiện tại (tránh tự kick mình)
                    if (item.Value.IsLoggedIn &&
                        item.Value.Username == username &&
                        item.Key != currentClient)
                    {
                        oldClient = item.Key;
                        break;
                    }
                }
            }

            // Nếu tìm thấy thiết bị cũ
            if (oldClient != null)
            {
                Console.WriteLine($">> PHÁT HIỆN ĐĂNG NHẬP TRÙNG: {username}. Đang kick thiết bị cũ...");

                // 1. Gửi thông báo cho thiết bị cũ
                SendToClient(oldClient, "MESSAGE|Tài khoản của bạn đã được đăng nhập ở thiết bị khác!");

                // 2. Gửi lệnh bắt buộc thoát
                SendToClient(oldClient, "FORCE_DISCONNECT");

                // 3. Đợi xíu cho tin nhắn đi rồi ngắt kết nối Server-side
                Thread.Sleep(200);
                RemoveClient(oldClient);
            }
        }
        // --- HÀM ĐĂNG KÝ VÀO DATABASE ---
        static bool RegisterPlayerToDB(string username, string password, string displayName, string email)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Câu lệnh INSERT vào bảng NguoiChoi
                    // Lưu ý: Đảm bảo bảng NguoiChoi trong SQL của bạn đã có các cột này
                    string query = @"INSERT INTO NguoiChoi (TaiKhoan, MatKhau, TenHienThi, Email, NgayDangKy, Diem, SoTranThang, SoTranThua, SoTranHoa, IsAdmin) 
                     VALUES (@u, @p, @d, @e, GETDATE(), 1000, 0, 0, 0, 0)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);
                    cmd.Parameters.AddWithValue("@d", displayName);
                    // Xử lý trường hợp email rỗng thì lưu NULL
                    cmd.Parameters.AddWithValue("@e", string.IsNullOrEmpty(email) ? DBNull.Value : (object)email);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đăng ký DB: {ex.Message}");
                return false;
            }
        }
        // =================================================================================
        // XỬ LÝ ĐĂNG KÝ
        // =================================================================================
        static void HandleRegister(TcpClient client, string[] parts, StreamWriter writer)
        {
            if (parts.Length < 4)
            {
                writer.WriteLine("REGISTER_FAIL|Thiếu thông tin đăng ký!");
                return;
            }

            string username = parts[1];
            string password = parts[2];
            string displayName = parts[3];
            string email = parts.Length > 4 ? parts[4] : "";

            // Kiểm tra username
            if (username.Length < 4)
            {
                writer.WriteLine("REGISTER_FAIL|Tên đăng nhập phải có ít nhất 4 ký tự!");
                return;
            }

            // Kiểm tra password
            if (password.Length < 6)
            {
                writer.WriteLine("REGISTER_FAIL|Mật khẩu phải có ít nhất 6 ký tự!");
                return;
            }

            // Kiểm tra displayName
            if (string.IsNullOrWhiteSpace(displayName))
            {
                writer.WriteLine("REGISTER_FAIL|Tên hiển thị không được để trống!");
                return;
            }

            // Kiểm tra username đã tồn tại
            if (CheckUsernameExists(username))
            {
                writer.WriteLine("REGISTER_FAIL|Tên đăng nhập đã tồn tại!");
                return;
            }

            // Kiểm tra displayName đã tồn tại
            if (CheckDisplayNameExists(displayName))
            {
                writer.WriteLine("REGISTER_FAIL|Tên hiển thị đã tồn tại!");
                return;
            }

            // Kiểm tra email nếu có
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!IsValidEmail(email))
                {
                    writer.WriteLine("REGISTER_FAIL|Email không hợp lệ!");
                    return;
                }
            }

            // Đăng ký tài khoản
            if (RegisterPlayerToDB(username, password, displayName, email))
            {
                writer.WriteLine("REGISTER_SUCCESS|Đăng ký thành công!");
                Console.WriteLine($">> Đăng ký thành công: {username} ({displayName})");
            }
            else
            {
                writer.WriteLine("REGISTER_FAIL|Lỗi đăng ký. Vui lòng thử lại!");
            }
        }
        // --- [NEW] XỬ LÝ QUÊN MẬT KHẨU ---
        static void HandleResetPassword(TcpClient client, string email, StreamWriter writer)
        {
            Console.WriteLine($">> Yêu cầu reset mật khẩu cho email: {email}");

            // 1. Kiểm tra Email có tồn tại trong DB không
            if (!CheckEmailExists(email))
            {
                writer.WriteLine("RESET_FAIL|Email này chưa được đăng ký!");
                return;
            }

            // 2. Tạo mật khẩu mới ngẫu nhiên (6 ký tự)
            string newPass = GenerateRandomPassword(6);

            // 3. Cập nhật mật khẩu mới vào Database (Lưu dạng MD5)
            string hashedPass = CalculateMD5(newPass); // Server cần hàm MD5 để lưu
            if (UpdatePasswordInDB(email, hashedPass))
            {
                // 4. Gửi email cho người dùng
                bool sendResult = SendEmail(email, "Cấp lại mật khẩu Caro Game", $"Mật khẩu mới của bạn là: {newPass}\nVui lòng đăng nhập và đổi lại mật khẩu ngay.");

                if (sendResult)
                    writer.WriteLine("RESET_SUCCESS|Mật khẩu mới đã được gửi vào Email của bạn!");
                else
                    writer.WriteLine("RESET_FAIL|Lỗi khi gửi Email. Vui lòng thử lại sau.");
            }
            else
            {
                writer.WriteLine("RESET_FAIL|Lỗi Database. Không thể cập nhật mật khẩu.");
            }
        }

        // --- [NEW] HÀM KIỂM TRA EMAIL TỒN TẠI ---
        static bool CheckEmailExists(string email)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM NguoiChoi WHERE Email = @e";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@e", email);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch { return false; }
        }

        // --- [NEW] HÀM CẬP NHẬT MẬT KHẨU ---
        static bool UpdatePasswordInDB(string email, string newPassHash)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE NguoiChoi SET MatKhau = @p WHERE Email = @e";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@p", newPassHash);
                    cmd.Parameters.AddWithValue("@e", email);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch { return false; }
        }

        // --- [NEW] HÀM GỬI EMAIL (QUAN TRỌNG: CẦN CẤU HÌNH) ---
        static bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                // CẤU HÌNH GMAIL CỦA BẠN Ở ĐÂY
                string fromEmail = "luannguyenqn.00@gmail.com";
                string password = "mcro eoly edqp qfru"; // KHÔNG PHẢI PASS GMAIL, LÀ APP PASSWORD

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(fromEmail, password);

                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(fromEmail, "Caro Game Server");
                msg.To.Add(new MailAddress(toEmail));
                msg.Subject = subject;
                msg.Body = body;

                smtp.Send(msg);
                Console.WriteLine($">> Đã gửi mail thành công cho {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($">> Lỗi gửi mail: {ex.Message}");
                return false;
            }
        }

        // --- [NEW] HÀM SINH PASS NGẪU NHIÊN ---
        static string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // --- [NEW] HÀM MD5 CHO SERVER (Vì Server cũng cần hash để lưu vào DB) ---
        static string CalculateMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        // --- HÀM ĐĂNG KÝ VÀO DATABASE (Cần thêm vào Program.cs) ---
        // Sửa hàm RegisterPlayer
        static void RegisterPlayer(TcpClient client, string username, string displayName, bool isAdmin, bool isLoggedIn, int userId = -1, int avatarId = 0)
        {
            lock (_lock)
            {
                if (!onlinePlayers.ContainsKey(client))
                {
                    onlinePlayers.Add(client, new PlayerInfo
                    {
                        UserID = userId,
                        Username = username,
                        DisplayName = displayName,
                        AvatarID = avatarId, // <--- Lưu
                        IsAdmin = isAdmin,
                        IsLoggedIn = isLoggedIn
                    });
                }
                else
                {
                    // Cập nhật
                    var info = onlinePlayers[client];
                    info.UserID = userId;
                    info.Username = username;
                    info.DisplayName = displayName;
                    info.AvatarID = avatarId; // <--- Lưu
                    info.IsAdmin = isAdmin;
                    info.IsLoggedIn = isLoggedIn;
                }
            }
        }
        static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        static bool CheckUsernameExists(string username)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM NguoiChoi WHERE TaiKhoan = @username";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kiểm tra username: {ex.Message}");
                return false; // Ngăn đăng ký nếu có lỗi
            }
        }

        static bool CheckDisplayNameExists(string displayName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM NguoiChoi WHERE TenHienThi = @displayName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@displayName", displayName);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kiểm tra display name: {ex.Message}");
                return false;
            }
        }

        // Cập nhật hàm RegisterPlayer để nhận thêm userId
        static void RegisterPlayer(TcpClient client, string username, string displayName, bool isAdmin, bool isLoggedIn, int userId = -1)
        {
            lock (_lock)
            {
                if (!onlinePlayers.ContainsKey(client))
                {
                    onlinePlayers.Add(client, new PlayerInfo
                    {
                        UserID = userId, // Lưu UserID vào
                        Username = username,
                        DisplayName = displayName,
                        IsAdmin = isAdmin,
                        IsLoggedIn = isLoggedIn
                    });
                }
                else
                {
                    // Cập nhật thông tin nếu đã tồn tại
                    var info = onlinePlayers[client];
                    info.UserID = userId; // Lưu UserID vào
                    info.Username = username;
                    info.DisplayName = displayName;
                    info.IsAdmin = isAdmin;
                    info.IsLoggedIn = isLoggedIn;
                }
            }
        }

        // =================================================================================
        // XỬ LÝ ĐĂNG NHẬP
        // =================================================================================

        static bool CheckLoginDB(string username, string password, out int userId, out string displayName, out bool isAdmin, out int avatarId) // <--- Thêm out avatarId
        {
            userId = -1;
            displayName = username;
            isAdmin = false;
            avatarId = 0; // <--- Mặc định

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Lấy thêm cột AvatarID
                    string query = "SELECT ID, TenHienThi, IsAdmin, AvatarID FROM NguoiChoi WHERE TaiKhoan = @u AND MatKhau = @p";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            userId = (int)r["ID"];
                            displayName = r["TenHienThi"].ToString();
                            isAdmin = Convert.ToBoolean(r["IsAdmin"]);

                            // Đọc AvatarID từ DB (Kiểm tra null cho chắc)
                            if (r["AvatarID"] != DBNull.Value)
                                avatarId = (int)r["AvatarID"];

                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        static void RegisterPlayer(TcpClient client, string username, string displayName, bool isAdmin, bool isLoggedIn)
        {
            lock (_lock)
            {
                if (!onlinePlayers.ContainsKey(client))
                {
                    onlinePlayers.Add(client, new PlayerInfo
                    {
                        Username = username,
                        DisplayName = displayName,
                        IsAdmin = isAdmin,
                        IsLoggedIn = isLoggedIn
                    });
                }
                else
                {
                    onlinePlayers[client] = new PlayerInfo
                    {
                        Username = username,
                        DisplayName = displayName,
                        IsAdmin = isAdmin,
                        IsLoggedIn = isLoggedIn
                    };
                }
            }
        }

        // =================================================================================
        // XỬ LÝ SẢNH CHỜ
        // =================================================================================
        static void JoinRandomMatch(TcpClient client, StreamWriter writer, int boardSize)
        {
            lock (_lock)
            {
                // --- [FIX 1] KIỂM TRA XEM ĐANG CÓ PHÒNG CHƯA ---
                // Nếu đã ở trong phòng nào đó rồi thì chặn ngay, không cho tìm tiếp
                if (FindRoomByClient(client) != null)
                {
                    writer.WriteLine("ERROR|Bạn đang ở trong phòng rồi (hoặc đang tìm trận)!");
                    return;
                }
                // ------------------------------------------------

                // Tìm phòng public có 1 người và cùng kích thước bàn cờ
                var room = activeRooms.FirstOrDefault(r =>
                    !r.IsPrivate &&
                    r.Players.Count == 1 &&
                    r.BoardSize == boardSize &&
                    !r.IsGameStarted &&
                    !r.Players.Contains(client)); // --- [FIX 2] CHẮC CHẮN KHÔNG PHẢI LÀ CHÍNH MÌNH ---

                if (room != null)
                {
                    // Ghép đôi thành công
                    room.Players.Add(client);
                    Console.WriteLine($">> Ghép đôi thành công: {GetPlayerName(client)} vào phòng {room.RoomID}");
                    SendGameStart(room);
                }
                else
                {
                    // Tạo phòng mới
                    string roomId = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
                    Room newRoom = new Room(roomId, false, boardSize);
                    newRoom.Players.Add(client);
                    activeRooms.Add(newRoom);

                    writer.WriteLine("WAITING_MATCH|Đang tìm đối thủ...");
                    Console.WriteLine($">> Tạo phòng chờ: {roomId} (Bàn: {boardSize}x{boardSize})");
                }
            }
        }

        static void CreatePrivateRoom(TcpClient client, StreamWriter writer, int boardSize)
        {
            lock (_lock)
            {
                string roomId = new Random().Next(1000, 9999).ToString();
                Room newRoom = new Room(roomId, true, boardSize);
                newRoom.Players.Add(client);
                activeRooms.Add(newRoom);

                writer.WriteLine($"ROOM_CREATED|{roomId}");
                Console.WriteLine($">> Tạo phòng riêng: {roomId} (Bàn: {boardSize}x{boardSize}) bởi {GetPlayerName(client)}");
            }
        }

        static void JoinPrivateRoom(TcpClient client, string roomId, StreamWriter writer, int boardSize)
        {
            lock (_lock)
            {
                var room = activeRooms.FirstOrDefault(r => r.RoomID == roomId && r.IsPrivate);
                if (room != null)
                {
                    if (room.Players.Count >= 2)
                    {
                        writer.WriteLine("ERROR|Phòng đã đầy!");
                        return;
                    }

                    if (room.BoardSize != boardSize)
                    {
                        writer.WriteLine("BOARD_SIZE_MISMATCH");
                        return;
                    }

                    // Kiểm tra không cho tự vào phòng của mình
                    if (room.Players.Contains(client))
                    {
                        writer.WriteLine("ERROR|Bạn đã ở trong phòng này!");
                        return;
                    }

                    room.Players.Add(client);
                    Console.WriteLine($">> {GetPlayerName(client)} vào phòng riêng: {roomId}");
                    SendGameStart(room);
                }
                else
                {
                    writer.WriteLine("ERROR|Phòng không tồn tại!");
                }
            }
        }

        // =================================================================================
        // XỬ LÝ TRONG GAME
        // =================================================================================
        static void ProcessGameCommand(Room room, TcpClient client, string command, string[] parts, StreamWriter writer)
        {
            int pIndex = room.Players.IndexOf(client);
            if (pIndex == -1) return;

            int side = (pIndex == 0) ? 1 : 2;
            TcpClient opponent = (room.Players.Count > 1) ? room.Players[(pIndex == 0) ? 1 : 0] : null;

            switch (command)
            {
                case "MOVE":
                    if (side != room.Turn) return;

                    int x = int.Parse(parts[1]);
                    int y = int.Parse(parts[2]);

                    // Kiểm tra tọa độ hợp lệ
                    if (x < 0 || x >= room.BoardSize || y < 0 || y >= room.BoardSize)
                        return;

                    // Kiểm tra ô trống
                    if (room.Board[x, y] != 0)
                        return;

                    // Thực hiện nước đi
                    room.Board[x, y] = side;
                    room.History.Push($"{x}|{y}");
                    room.Turn = (side == 1) ? 2 : 1;
                    // ---  Lưu nước đi ---
                    if (room.CurrentMatchID != -1 && onlinePlayers.ContainsKey(client))
                    {
                        int playerID = onlinePlayers[client].UserID;
                        int moveNum = room.History.Count; // Nước đi thứ mấy

                        // Chạy Thread riêng để không làm lag game
                        new Thread(() => DB_LuuNuocDi(room.CurrentMatchID, moveNum, playerID, x, y)).Start();
                    }
                    // Gửi nước đi cho cả phòng
                    BroadcastToRoom(room, $"MOVE|{x}|{y}|{side}");

                    // Kiểm tra thắng thua
                    if (CheckWin(room.Board, x, y, side, room.BoardSize))
                    {
                        BroadcastToRoom(room, $"GAMEOVER|{side}");
                        Console.WriteLine($">> Phòng {room.RoomID}: {GetPlayerName(client)} thắng!");
                        if (room.CurrentMatchID != -1)
                        {
                            int winnerID = onlinePlayers[client].UserID; // ID người vừa đánh thắng
                            DB_KetThucTran(room.CurrentMatchID, winnerID);
                        }
                        // Cập nhật thống kê nếu là user đã đăng nhập
                        if (onlinePlayers.ContainsKey(client) && onlinePlayers[client].IsLoggedIn)
                        {
                            UpdatePlayerStats(client, true, false, false);
                        }
                        if (opponent != null && onlinePlayers.ContainsKey(opponent) && onlinePlayers[opponent].IsLoggedIn)
                        {
                            UpdatePlayerStats(opponent, false, true, false);
                        }
                    }
                    break;

                // --- TÌM CASE "UNDO" CŨ VÀ XÓA ĐI, THAY BẰNG ĐOẠN NÀY ---

                case "UNDO_REQUEST":
                    // Người chơi A muốn xin đi lại -> Gửi thông báo hỏi ý kiến người chơi B
                    if (opponent != null)
                    {
                        SendToClient(opponent, "UNDO_ASK|Đối thủ muốn xin đi lại một nước. Bạn có đồng ý không?");
                    }
                    else
                    {
                        writer.WriteLine("MESSAGE|Đối thủ đã thoát, không thể xin đi lại!");
                    }
                    break;

                case "UNDO_ACCEPT":
                    // Người chơi B đồng ý -> Thực hiện Logic Undo cũ tại đây
                    if (room.History.Count > 0)
                    {
                        string lastMove = room.History.Pop();
                        string[] pos = lastMove.Split('|');
                        int ux = int.Parse(pos[0]);
                        int uy = int.Parse(pos[1]);

                        room.Board[ux, uy] = 0;
                        room.Turn = (room.Turn == 1) ? 2 : 1; // Đổi lại lượt

                        // Gửi lệnh cập nhật bàn cờ cho CẢ HAI người (để xóa quân cờ trên UI)
                        BroadcastToRoom(room, $"UNDO|{ux}|{uy}");

                        // Thông báo cho người xin biết là được đồng ý
                        if (opponent != null) SendToClient(opponent, "MESSAGE|Đối thủ đã CHẤP NHẬN cho bạn đi lại!");
                    }
                    break;

                case "UNDO_REJECT":
                    // Người chơi B từ chối -> Báo lại cho A biết
                    if (opponent != null)
                    {
                        SendToClient(opponent, "MESSAGE|Đối thủ ĐÃ TỪ CHỐI yêu cầu đi lại của bạn!");
                    }
                    break;

                // --------------------------------------------------------

                case "SURRENDER":
                    int winner = (side == 1) ? 2 : 1;
                    BroadcastToRoom(room, $"GAMEOVER|{winner}");
                    Console.WriteLine($">> Phòng {room.RoomID}: {GetPlayerName(client)} đầu hàng!");

                    if (opponent != null && onlinePlayers.ContainsKey(opponent) && onlinePlayers[opponent].IsLoggedIn)
                    {
                        UpdatePlayerStats(opponent, true, false, false);
                    }
                    break;

                case "DRAW_REQUEST":
                    if (opponent != null)
                        SendToClient(opponent, "DRAW_REQUEST");
                    break;

                case "DRAW_ACCEPT":
                    BroadcastToRoom(room, "GAME_DRAW");
                    room.ResetGame();
                    Console.WriteLine($">> Phòng {room.RoomID}: Hòa!");

                    // Cập nhật thống kê cho cả 2
                    if (onlinePlayers.ContainsKey(client) && onlinePlayers[client].IsLoggedIn)
                    {
                        UpdatePlayerStats(client, false, false, true);
                    }
                    if (opponent != null && onlinePlayers.ContainsKey(opponent) && onlinePlayers[opponent].IsLoggedIn)
                    {
                        UpdatePlayerStats(opponent, false, false, true);
                    }
                    break;

                case "NEW_GAME":
                    room.ResetGame();
                    BroadcastToRoom(room, "NEW_GAME");
                    SendGameStart(room);
                    Console.WriteLine($">> Phòng {room.RoomID}: Ván mới!");
                    break;

                case "CHAT":
                    if (parts.Length > 1)
                    {
                        string playerName = onlinePlayers.ContainsKey(client) ? onlinePlayers[client].DisplayName : "Unknown";
                        string chatMessage = parts[1];
                        BroadcastToRoom(room, $"CHAT|{playerName}|{chatMessage}");
                    }
                    break;

                case "TIME_OUT":
                    int timeoutWinner = (side == 1) ? 2 : 1;
                    BroadcastToRoom(room, $"GAMEOVER|{timeoutWinner}");
                    Console.WriteLine($">> Phòng {room.RoomID}: {GetPlayerName(client)} hết giờ!");

                    if (opponent != null && onlinePlayers.ContainsKey(opponent) && onlinePlayers[opponent].IsLoggedIn)
                    {
                        UpdatePlayerStats(opponent, true, false, false);
                    }
                    break;

                case "LEAVE_GAME":
                    // Thông báo cho đối thủ
                    if (opponent != null)
                    {
                        SendToClient(opponent, "MESSAGE|Đối thủ đã rời phòng!");
                        SendToClient(opponent, "OPPONENT_LEFT");
                    }

                    // Xóa client khỏi phòng
                    room.Players.Remove(client);
                    writer.WriteLine("LEAVE_SUCCESS");

                    // Nếu phòng trống thì xóa phòng
                    if (room.Players.Count == 0)
                    {
                        lock (_lock)
                        {
                            activeRooms.Remove(room);
                        }
                    }

                    Console.WriteLine($">> {GetPlayerName(client)} rời phòng {room.RoomID}");
                    break;

                case "DISCONNECT":
                    // Client đang disconnect
                    break;
            }
        }

        // =================================================================================
        // KIỂM TRA THẮNG THUA (CARO LOGIC)
        // =================================================================================
        static bool CheckWin(int[,] board, int x, int y, int side, int boardSize)
        {
            // Kiểm tra 4 hướng: ngang, dọc, chéo chính, chéo phụ
            int[] dx = { 1, 0, 1, 1 };
            int[] dy = { 0, 1, 1, -1 };

            for (int dir = 0; dir < 4; dir++)
            {
                int count = 1;

                // Kiểm tra một chiều
                for (int i = 1; i <= 4; i++)
                {
                    int nx = x + dx[dir] * i;
                    int ny = y + dy[dir] * i;

                    if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize || board[nx, ny] != side)
                        break;
                    count++;
                }

                // Kiểm tra chiều ngược lại
                for (int i = 1; i <= 4; i++)
                {
                    int nx = x - dx[dir] * i;
                    int ny = y - dy[dir] * i;

                    if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize || board[nx, ny] != side)
                        break;
                    count++;
                }

                if (count >= 5) return true;
            }

            return false;
        }

        // =================================================================================
        // HÀM HỖ TRỢ
        // =================================================================================
        static void SendGameStart(Room room)
        {
            room.IsGameStarted = true;

            // Lấy thông tin 2 người chơi
            TcpClient p1 = room.Players[0];
            TcpClient p2 = (room.Players.Count > 1) ? room.Players[1] : null;

            int p1Avatar = onlinePlayers.ContainsKey(p1) ? onlinePlayers[p1].AvatarID : 0;
            int p2Avatar = (p2 != null && onlinePlayers.ContainsKey(p2)) ? onlinePlayers[p2].AvatarID : 0;

            string p1Name = onlinePlayers.ContainsKey(p1) ? onlinePlayers[p1].DisplayName : "Unknown";
            string p2Name = (p2 != null && onlinePlayers.ContainsKey(p2)) ? onlinePlayers[p2].DisplayName : "Máy";

            // Gửi cho Player 1 (Bạn là X, Đối thủ là O) -> Gửi Avatar O cho P1
            // Format: GAME_START | Side | BoardSize | OpponentName | OpponentAvatar
            SendToClient(p1, $"GAME_START|1|{room.BoardSize}|{p2Name}|{p2Avatar}");

            // Gửi cho Player 2 (Bạn là O, Đối thủ là X) -> Gửi Avatar X cho P2
            if (p2 != null)
            {
                SendToClient(p2, $"GAME_START|2|{room.BoardSize}|{p1Name}|{p1Avatar}");
            }

            // ... (Đoạn code lưu vào DB giữ nguyên)
        }

        static void SendAdminData(StreamWriter writer)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ADMIN_DATA");

            lock (_lock)
            {
                foreach (var room in activeRooms)
                {
                    string playerNames = "";
                    foreach (var player in room.Players)
                    {
                        if (onlinePlayers.ContainsKey(player))
                            playerNames += onlinePlayers[player].DisplayName + (onlinePlayers[player].IsLoggedIn ? "" : "(Guest)") + ",";
                    }

                    sb.Append($"|Phòng {room.RoomID} ({room.Players.Count}/2)");
                    sb.Append($" - Bàn: {room.BoardSize}x{room.BoardSize}");
                    sb.Append($" - {(room.IsPrivate ? "Riêng" : "Công khai")}");
                    sb.Append($" - Players: {playerNames.TrimEnd(',')}");
                }

                // Thêm thông tin online players
                sb.Append($"|Tổng người chơi: {onlinePlayers.Count}");
                sb.Append($"|Tổng phòng: {activeRooms.Count}");
            }

            writer.WriteLine(sb.ToString());
        }

        static void KickUser(string username)
        {
            try
            {
                TcpClient targetClient = null;

                lock (_lock)
                {
                    foreach (var player in onlinePlayers)
                    {
                        if (player.Value.DisplayName.Equals(username, StringComparison.OrdinalIgnoreCase))
                        {
                            targetClient = player.Key;
                            break;
                        }
                    }
                }

                if (targetClient != null)
                {
                    SendToClient(targetClient, "MESSAGE|Bạn đã bị kick bởi admin!");
                    SendToClient(targetClient, "FORCE_DISCONNECT");

                    // Đóng kết nối
                    Thread.Sleep(100);
                    RemoveClient(targetClient);

                    Console.WriteLine($">> Admin kick: {username}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi kick user: {ex.Message}");
            }
        }

        static void UpdatePlayerStats(TcpClient client, bool win, bool lose, bool draw)
        {
            try
            {
                if (!onlinePlayers.ContainsKey(client) || !onlinePlayers[client].IsLoggedIn)
                    return;

                string username = onlinePlayers[client].Username;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "UPDATE NguoiChoi SET ";

                    if (win)
                    {
                        query += "Diem = Diem + 20, SoTranThang = SoTranThang + 1 ";
                    }
                    else if (lose)
                    {
                        query += "Diem = Diem - 10, SoTranThua = SoTranThua + 1 ";
                    }
                    else if (draw)
                    {
                        query += "Diem = Diem + 5, SoTranHoa = SoTranHoa + 1 ";
                    }

                    query += "WHERE TaiKhoan = @username";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi cập nhật thống kê: {ex.Message}");
            }
        }

        static Room FindRoomByClient(TcpClient client)
        {
            lock (_lock)
            {
                return activeRooms.FirstOrDefault(r => r.Players.Contains(client));
            }
        }

        static bool IsAdmin(TcpClient client)
        {
            lock (_lock)
            {
                return onlinePlayers.ContainsKey(client) && onlinePlayers[client].IsAdmin;
            }
        }

        static string GetPlayerName(TcpClient client)
        {
            lock (_lock)
            {
                if (onlinePlayers.ContainsKey(client))
                    return onlinePlayers[client].DisplayName;
                return "Unknown";
            }
        }

        static void BroadcastToRoom(Room room, string message, TcpClient exclude = null)
        {
            foreach (var player in room.Players)
            {
                if (player != exclude)
                {
                    SendToClient(player, message);
                }
            }
        }

        static void SendToClient(TcpClient client, string message)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                    writer.WriteLine(message);
                }
            }
            catch { }
        }

        static void RemoveClient(TcpClient client)
        {
            try
            {
                lock (_lock)
                {
                    if (onlinePlayers.ContainsKey(client))
                    {
                        PlayerInfo info = onlinePlayers[client];
                        Room room = FindRoomByClient(client);

                        // --- LOGIC MỚI: NẾU ĐANG CHƠI THÌ KHÔNG XÓA NGAY ---
                        if (info.IsInGame && room != null && info.UserID > 0) // Chỉ áp dụng cho User đã đăng nhập
                        {
                            Console.WriteLine($">> {info.Username} bị mất kết nối. Đang chờ quay lại...");

                            // 1. Lưu vào danh sách chờ
                            if (!pendingDisconnects.ContainsKey(info.UserID))
                            {
                                pendingDisconnects.Add(info.UserID, new PendingReconnect
                                {
                                    Info = info,
                                    Room = room,
                                    DisconnectTime = DateTime.Now
                                });
                            }

                            // 2. Thông báo cho đối thủ biết
                            TcpClient opponent = room.Players.FirstOrDefault(p => p != client);
                            if (opponent != null)
                            {
                                SendToClient(opponent, "MESSAGE|Đối thủ bị mất kết nối! Đang chờ 60s...");
                            }

                            // 3. Xóa socket cũ khỏi danh sách online (vì nó chết rồi)
                            onlinePlayers.Remove(client);

                            // Lưu ý: KHÔNG xóa phòng, KHÔNG xóa player khỏi Room.Players vội
                        }
                        else
                        {
                            // --- LOGIC CŨ: XÓA BÌNH THƯỜNG ---
                            if (room != null)
                            {
                                room.Players.Remove(client);
                                foreach (var other in room.Players)
                                {
                                    SendToClient(other, "MESSAGE|Đối thủ đã rời phòng!");
                                    SendToClient(other, "OPPONENT_LEFT");
                                }
                                if (room.Players.Count == 0) activeRooms.Remove(room);
                            }
                            onlinePlayers.Remove(client);
                        }
                    }

                    if (client != null) client.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi RemoveClient: {ex.Message}");
            }
        }
        // 1. Tạo bản ghi trận đấu mới khi Game bắt đầu (Trả về MatchID)
        static int DB_TaoTranDau(int p1_ID, int p2_ID, int kichThuoc)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Insert vào bảng TranDau, lấy về ID vừa tạo (SCOPE_IDENTITY)
                    string q = @"INSERT INTO TranDau (Player1_ID, Player2_ID, KichThuocBanCo, ThoiGianBatDau) 
                         VALUES (@p1, @p2, @sz, GETDATE()); 
                         SELECT CAST(SCOPE_IDENTITY() as int)";
                    SqlCommand cmd = new SqlCommand(q, conn);
                    cmd.Parameters.AddWithValue("@p1", p1_ID);
                    cmd.Parameters.AddWithValue("@p2", p2_ID);
                    cmd.Parameters.AddWithValue("@sz", kichThuoc);
                    return (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex) { Console.WriteLine("Lỗi DB TaoTran: " + ex.Message); return -1; }
        }

        // 2. Cập nhật kết quả khi Game kết thúc
        static void DB_KetThucTran(int matchID, int winnerID)
        {
            if (matchID == -1) return;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string q = "UPDATE TranDau SET Winner_ID = @w, ThoiGianKetThuc = GETDATE() WHERE MatchID = @m";
                    SqlCommand cmd = new SqlCommand(q, conn);
                    // Nếu winnerID = -1 (Hòa) thì để NULL trong DB
                    if (winnerID == -1) cmd.Parameters.AddWithValue("@w", DBNull.Value);
                    else cmd.Parameters.AddWithValue("@w", winnerID);

                    cmd.Parameters.AddWithValue("@m", matchID);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e) { Console.WriteLine("Lỗi DB KetThuc: " + e.Message); }
        }

        // 3. (Nâng cao) Lưu từng nước đi
        static void DB_LuuNuocDi(int matchID, int nuocDiSo, int nguoiDiID, int x, int y)
        {
            if (matchID == -1) return;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string q = @"INSERT INTO ChiTietTranDau (MatchID, NuocDiSo, NguoiDi_ID, ToaDoX, ToaDoY) 
                         VALUES (@m, @n, @p, @x, @y)";
                    SqlCommand cmd = new SqlCommand(q, conn);
                    cmd.Parameters.AddWithValue("@m", matchID);
                    cmd.Parameters.AddWithValue("@n", nuocDiSo);
                    cmd.Parameters.AddWithValue("@p", nguoiDiID);
                    cmd.Parameters.AddWithValue("@x", x);
                    cmd.Parameters.AddWithValue("@y", y);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { } // Bỏ qua lỗi để không lag game
        }
        static void SendLeaderboard(StreamWriter writer)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Lấy Top 5 người điểm cao nhất
                    string query = "SELECT TOP 5 TenHienThi, Diem FROM NguoiChoi ORDER BY Diem DESC";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader r = cmd.ExecuteReader();

                    StringBuilder sb = new StringBuilder();
                    int rank = 1;
                    while (r.Read())
                    {
                        sb.AppendLine($"#{rank} - {r["TenHienThi"]} : {r["Diem"]} Elo");
                        rank++;
                    }
                    if (sb.Length == 0) sb.Append("Chưa có dữ liệu xếp hạng.");

                    writer.WriteLine($"LEADERBOARD_DATA|{sb.ToString()}");
                }
            }
            catch { writer.WriteLine("MESSAGE|Lỗi lấy BXH!"); }
        }

        static void SendHistory(TcpClient client, StreamWriter writer)
        {
            try
            {
                int myID = onlinePlayers[client].UserID;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Lấy 5 trận gần nhất
                    // Cần JOIN bảng NguoiChoi 2 lần để lấy tên đối thủ
                    string query = @"SELECT TOP 5 T.ThoiGianKetThuc, 
                                    P1.TenHienThi as P1Name, 
                                    P2.TenHienThi as P2Name,
                                    T.Winner_ID
                             FROM TranDau T
                             JOIN NguoiChoi P1 ON T.Player1_ID = P1.ID
                             JOIN NguoiChoi P2 ON T.Player2_ID = P2.ID
                             WHERE (T.Player1_ID = @uid OR T.Player2_ID = @uid) 
                               AND T.ThoiGianKetThuc IS NOT NULL
                             ORDER BY T.ThoiGianKetThuc DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@uid", myID);
                    SqlDataReader r = cmd.ExecuteReader();

                    StringBuilder sb = new StringBuilder();
                    while (r.Read())
                    {
                        string p1 = r["P1Name"].ToString();
                        string p2 = r["P2Name"].ToString();
                        int winner = r["Winner_ID"] == DBNull.Value ? -1 : (int)r["Winner_ID"];
                        DateTime time = (DateTime)r["ThoiGianKetThuc"];

                        string opponent = (p1 == onlinePlayers[client].DisplayName) ? p2 : p1;
                        string result = (winner == -1) ? "HÒA" : (winner == myID ? "THẮNG" : "THUA");

                        sb.AppendLine($"[{time:dd/MM HH:mm}] vs {opponent} -> {result}");
                    }
                    writer.WriteLine($"HISTORY_DATA|{sb.ToString()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                writer.WriteLine("MESSAGE|Lỗi lấy lịch sử!");
            }
        }
        // Thêm dòng này vào Main(): new Thread(CleanupPendingDisconnects).Start();

        static void CleanupPendingDisconnects()
        {
            while (true)
            {
                Thread.Sleep(5000); // Kiểm tra mỗi 5s
                lock (_lock)
                {
                    List<int> toRemove = new List<int>();
                    foreach (var item in pendingDisconnects)
                    {
                        // Nếu quá 60 giây
                        if ((DateTime.Now - item.Value.DisconnectTime).TotalSeconds > 60)
                        {
                            var pending = item.Value;
                            Console.WriteLine($">> Hết thời gian chờ: {pending.Info.Username} -> Xử thua.");

                            // Xử lý game over (Xử thua)
                            Room room = pending.Room;
                            if (room != null && activeRooms.Contains(room))
                            {
                                // Tìm người còn lại để báo thắng
                                TcpClient opponent = room.Players.FirstOrDefault(p => onlinePlayers.ContainsKey(p) && onlinePlayers[p].UserID != pending.Info.UserID);
                                if (opponent != null)
                                {
                                    SendToClient(opponent, "MESSAGE|Đối thủ không quay lại. Bạn thắng!");
                                    SendToClient(opponent, "GAMEOVER|WIN_BY_DISCONNECT"); // Bạn cần xử lý cái này ở Client nếu muốn
                                }
                                activeRooms.Remove(room);
                            }
                            toRemove.Add(item.Key);
                        }
                    }
                    foreach (int id in toRemove) pendingDisconnects.Remove(id);
                }
            }
        }
    }
}