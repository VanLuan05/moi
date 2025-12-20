using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaroShared;

namespace CaroClient
{
    public partial class Form1 : Form
    {
        // --- 1. KHAI BÁO CÁC MÀN HÌNH (PANELS) ---
        private Panel pnlLogin;
        private Panel pnlRegister;
        private Panel pnlLobby;
        private Panel pnlGame;
        private Panel pnlAdmin;
        private Panel pnlBoardSize;
        // --- BIẾN CHO EMOTE (BIỂU CẢM) ---
        private Label lblEmoteP1, lblEmoteP2;   // Nhãn hiện biểu cảm đè lên Avatar
        private Panel pnlEmoteSelector;         // Bảng chọn biểu cảm
        private System.Windows.Forms.Timer tmHideEmote; // Đồng hồ đếm giờ để tự ẩn biểu cảm
        private Point winStart = new Point(-1, -1);
        private Point winEnd = new Point(-1, -1);
        private int[,] currBoard;
        // 0: Sáng (Mặc định), 1: Tối (Dark), 2: Gỗ (Wood)
        private int currentTheme = 0;
        // --- BIẾN HIỆU ỨNG ---
        private System.Windows.Forms.Timer tmAnimation; // Timer để tạo nhấp nháy
        private bool isGameOverEffect = false;          // Cờ báo hiệu game kết thúc chưa
        private string endResultText = "";              // Chữ hiển thị (THẮNG/THUA)
        private Color endResultColor = Color.Gold;      // Màu chữ
        private bool blinkToggle = false;               // Biến đảo trạng thái nhấp nháy
        // --- BIẾN CHO EMOTE NÂNG CAO (SLIDING) ---
        // 1. Danh sách Icon phong phú hơn
        // Danh sách Icon (Em có thể thêm bao nhiêu tùy thích)
        private string[] listEmojis = {
    "😂", "😡", "😭", "😎", "😍", "🤔", "😅", "👋", "👍", "👎",
    "💩", "👻", "👽", "🤖", "🔥", "💔", "❤️", "🎉", "zzz", "👀",
    "✨", "🎵", "🎲", "🎯", "🚀"
};

        // 2. Biến phục vụ việc Kéo (Drag)
        private bool isDraggingEmote = false;
        private int lastMouseX;
        private int dragThreshold = 5; // Độ nhạy, di chuyển quá 5px mới tính là kéo
        private bool isClickAction = true; // Để phân biệt giữa Click (chọn) và Drag (kéo)
        // --- BIẾN CHAT LOBBY ---
        private RichTextBox rtbLobbyChat;
        private TextBox txtLobbyMessage;
        private Panel pnlLobbyEmoteSelector; // Bảng chọn Emote ở sảnh
        private bool isMusicOn = true; // Trạng thái nhạc
        private System.Media.SoundPlayer bgmPlayer; // Máy phát nhạc
        private int currentLevel = 1; // Mặc định là Dễ                   // --- BIẾN ÂM THANH ---
        private WMPLib.WindowsMediaPlayer musicPlayer = new WMPLib.WindowsMediaPlayer();
        private Button btnToggleMusic; // Nút bật tắt nhạc trên màn hình game
        // --- CONTROL TOÀN CỤC ĐỂ QUẢN LÝ VỊ TRÍ ---
        private GroupBox gbGuest, gbLogin;
        private Label lblLoginTitle;
        private Button btnFindMatch, btnCreatePrivate, btnJoinPrivate, btnLeaderboard, btnHistory, btnLogout, btnOpenAdmin;
        private TextBox txtRoomIDJoin;
        private Label lblWelcome, lblStatus, lblJoinInstruction;
        private Panel pnlJoinGroup; // Panel nhóm chức năng vào phòng

        // --- CONTROL KHÁC ---
        private TextBox txtUserLogin, txtPassLogin, txtNickNameGuest;
        private TextBox txtServerIP;
        private Button btnLoginDB, btnGuestJoin, btnGoToRegister, btnBackToLogin, btnRegisterSubmit;
        private LinkLabel lnkForgotPassword;
        private TextBox txtRegUsername, txtRegPassword, txtRegConfirmPassword, txtRegDisplayName, txtRegEmail;
        private Label lblRegStatus;
        private CheckBox chkAcceptTerms;
        private RichTextBox rtbAdminData;
        private TextBox txtKickUser;
        private Button btnKick, btnBackFromAdmin;
        private Panel pnlChessBoard;
        private Label lblLuotDi, lblDongHo;
        private RichTextBox rtbChatLog;
        private TextBox txtMessage;
        private Button btnSend, btnNewGame, btnUndo, btnXinHoa, btnXinThua, btnLeaveGame;
        private ProgressBar prcbCoolDown;
        private PictureBox ptbAvatar1, ptbAvatar2;
        private Button btnBoard10x10, btnBoard15x15, btnBoard20x20, btnBackToLobby;
        private Label lblBoardTitle;
        private Label lblRegisterTitle;
        private Panel pnlRegisterBox;

        // --- BIẾN LOGIC ---
        private TcpClient client;
        private StreamWriter writer;
        private StreamReader reader;
        private int tongThoiGian = 180;
        private int thoiGianConLai;
        private Image imgX, imgO;
        private int mySide = 0; // 1 = X, 2 = O
        private System.Windows.Forms.Timer tmCoolDown;
        private bool isAdmin = false;
        private List<string> chatHistory = new List<string>();
        private int boardSize = 15;
        private int selectedGameMode = 0;
        private string tempRoomID = "";
        private bool isLoggedIn = false;
        private string currentUsername = "";
        private int playerMoveCount = 0;
        private Panel pnlDifficulty; // Panel chọn độ khó
        // Biến mới thêm
        public string CheDoChoi = "LAN";
        private bool isGuest = false;
        private CaroAI aiBot = new CaroAI();
        private int[,] banCoAo;
        private bool isAiThinking = false;

        public Form1()
        {
            this.Text = "Caro Pro - Ultimate Online";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 40);
            this.ForeColor = Color.White;

            this.FormClosing += Form1_FormClosing;
            this.Resize += Form1_Resize;

            InitializeScreens();
            InitializeGameLogic();
        }

        private void InitializeScreens()
        {
            // Tạo các Panel
            pnlLogin = CreateFullScreenPanel();
            pnlRegister = CreateFullScreenPanel();
            pnlLobby = CreateFullScreenPanel();
            pnlGame = CreateFullScreenPanel();
            pnlAdmin = CreateFullScreenPanel();
            pnlBoardSize = CreateFullScreenPanel();
            // --- [THÊM MỚI] ---
            pnlDifficulty = CreateFullScreenPanel();
            this.Controls.Add(pnlDifficulty);
            SetupDifficultyScreen();
            // Thêm vào Form
            this.Controls.Add(pnlLogin);
            this.Controls.Add(pnlRegister);
            this.Controls.Add(pnlLobby);
            this.Controls.Add(pnlGame);
            this.Controls.Add(pnlAdmin);
            this.Controls.Add(pnlBoardSize);

            // --- [QUAN TRỌNG: FIX LỖI CĂN GIỮA TẠI ĐÂY] ---
            // Tự động căn giữa khi Panel thay đổi kích thước hoặc vừa hiện lên

            // 1. Panel Đăng nhập (Guest + Login)
            pnlLogin.SizeChanged += (s, e) => { if (pnlLogin.Visible) CenterLoginControls(); };
            pnlLogin.VisibleChanged += (s, e) => { if (pnlLogin.Visible) CenterLoginControls(); };

            // 2. Panel Đăng ký
            pnlRegister.SizeChanged += (s, e) => { if (pnlRegister.Visible) CenterRegisterControls(); };
            pnlRegister.VisibleChanged += (s, e) => { if (pnlRegister.Visible) CenterRegisterControls(); };

            // 3. Panel Sảnh chờ (Lobby)
            pnlLobby.SizeChanged += (s, e) => { if (pnlLobby.Visible) CenterLobbyControls(); };
            pnlLobby.VisibleChanged += (s, e) => { if (pnlLobby.Visible) CenterLobbyControls(); };
            // ----------------------------------------------

            // Gọi các hàm Setup giao diện
            SetupLoginScreen();
            SetupRegisterScreen();
            SetupLobbyScreen();
            SetupGameScreen();
            SetupAdminScreen();
            SetupBoardSizeScreen();

            // Hiện màn hình đầu tiên
            ShowScreen(pnlLogin);
        }

        private Panel CreateFullScreenPanel() { return new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 40), Visible = false }; }
        // Tìm hàm này và sửa lại:
        private void ShowScreen(Panel p)
        {
            // 1. Ẩn tất cả các màn hình cũ
            if (pnlLogin != null) pnlLogin.Visible = false;
            if (pnlRegister != null) pnlRegister.Visible = false;
            if (pnlLobby != null) pnlLobby.Visible = false;
            if (pnlGame != null) pnlGame.Visible = false;
            if (pnlAdmin != null) pnlAdmin.Visible = false;
            if (pnlBoardSize != null) pnlBoardSize.Visible = false;

            // --- [THÊM DÒNG NÀY VÀO] ---
            // Phải ẩn cả màn hình chọn độ khó đi nữa!
            if (pnlDifficulty != null) pnlDifficulty.Visible = false;
            // ---------------------------

            // 2. Hiện màn hình mong muốn
            if (p != null)
            {
                p.Visible = true;
                p.BringToFront(); // Đảm bảo nó nổi lên trên cùng
            }
        }

        // --- SETUP LOGIN (Đã chỉnh sửa vị trí & thêm nút Offline) ---
        private void SetupLoginScreen()
        {
            // 1. Tiêu đề
            lblLoginTitle = new Label { Text = "CARO ONLINE", Font = new Font("Segoe UI", 30, FontStyle.Bold), ForeColor = Color.Cyan, AutoSize = true };
            pnlLogin.Controls.Add(lblLoginTitle);

            // 2. GroupBox Khách (Guest)
            gbGuest = CreateGroupBox("Chơi Nhanh (Guest)", 400, 250);
            Label lblIP = new Label
            {
                Text = "IP Máy Chủ:",
                Location = new Point(20, 35),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            txtServerIP = CreateInput("127.0.0.1", 20, 55, 360);
            txtNickNameGuest = CreateInput("Nhập biệt danh...", 20, 95, 360);

            btnGuestJoin = CreateButton("CHƠI ONLINE (LAN)", 20, 135, 360, Color.SeaGreen);
            btnGuestJoin.Click += (s, e) => {
                string inputName = txtNickNameGuest.Text.Trim();
                if (string.IsNullOrWhiteSpace(inputName) || inputName == "Nhập biệt danh...") { MessageBox.Show("Vui lòng nhập biệt danh!", "Thông báo"); txtNickNameGuest.Focus(); return; }
                isGuest = true; ConnectAndLogin("GUEST");
            };

            Button btnGuestPlayAI = CreateButton("🤖 ĐẤU VỚI MÁY (OFFLINE)", 20, 185, 360, Color.OrangeRed);
            btnGuestPlayAI.Click += (s, e) => ShowScreen(pnlDifficulty);

            gbGuest.Controls.Add(lblIP); gbGuest.Controls.Add(txtServerIP); gbGuest.Controls.Add(txtNickNameGuest); gbGuest.Controls.Add(btnGuestJoin); gbGuest.Controls.Add(btnGuestPlayAI);
            pnlLogin.Controls.Add(gbGuest);

            // 3. GroupBox Đăng nhập (Login)
            gbLogin = CreateGroupBox("Đăng Nhập Tài Khoản", 400, 300);
            txtUserLogin = CreateInput("Tài khoản...", 20, 40, 360);
            txtPassLogin = CreateInput("Mật khẩu...", 20, 80, 360);
            txtPassLogin.UseSystemPasswordChar = true;

            btnLoginDB = CreateButton("ĐĂNG NHẬP", 20, 120, 360, Color.DodgerBlue);
            btnLoginDB.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtUserLogin.Text) || string.IsNullOrWhiteSpace(txtPassLogin.Text)) { MessageBox.Show("Nhập đủ thông tin!", "Thông báo"); return; }
                isGuest = false; ConnectAndLogin("DB");
            };

            btnGoToRegister = CreateButton("ĐĂNG KÝ TÀI KHOẢN MỚI", 20, 170, 360, Color.Purple);
            btnGoToRegister.Click += (s, e) => ShowScreen(pnlRegister); // Thêm sự kiện chuyển màn hình đăng ký

            // --- [SỬA LẠI ĐOẠN LINK LABEL TẠI ĐÂY] ---

            // BƯỚC A: Khởi tạo LinkLabel trước!
            lnkForgotPassword = new LinkLabel
            {
                Text = "Quên mật khẩu?",
                AutoSize = true,
                LinkColor = Color.LightSkyBlue,
                VisitedLinkColor = Color.LightSkyBlue
            };

            // BƯỚC B: Gán sự kiện Click (Logic Reset Pass)
            lnkForgotPassword.Click += (s, e) => {
                // 1. Kết nối đến Server trước (nếu chưa kết nối)
                if (!IsConnected())
                {
                    try
                    {
                        client = new TcpClient();
                        // Lấy IP từ textbox, nếu trống thì mặc định localhost
                        client.Connect(string.IsNullOrWhiteSpace(txtServerIP.Text) ? "127.0.0.1" : txtServerIP.Text, 8080);
                        NetworkStream st = client.GetStream();
                        writer = new StreamWriter(st) { AutoFlush = true };
                        reader = new StreamReader(st);
                        new Thread(ReceiveMessage) { IsBackground = true }.Start();
                    }
                    catch
                    {
                        MessageBox.Show("Không thể kết nối đến Server!", "Lỗi");
                        return;
                    }
                }

                // 2. Hiện hộp thoại nhập Email
                string email = ShowInputDialog("Nhập Email đã đăng ký:", "Quên mật khẩu");
                if (!string.IsNullOrWhiteSpace(email))
                {
                    SendCommand($"RESET_PASS|{email}");
                }
            };

            // BƯỚC C: Thêm vào GroupBox trước để tính toán kích thước
            gbLogin.Controls.Add(lnkForgotPassword);

            // BƯỚC D: Căn giữa
            int xCenter = (gbLogin.Width - lnkForgotPassword.Width) / 2;
            lnkForgotPassword.Location = new Point(xCenter, 230);

            // --- [KẾT THÚC SỬA] ---

            gbLogin.Controls.Add(txtUserLogin);
            gbLogin.Controls.Add(txtPassLogin);
            gbLogin.Controls.Add(btnLoginDB);
            gbLogin.Controls.Add(btnGoToRegister);
            // Lưu ý: lnkForgotPassword đã được Add ở bước C rồi, không Add lại nữa

            pnlLogin.Controls.Add(gbLogin);

            CenterLoginControls();
        }

        // --- SETUP LOBBY (Đã chỉnh sửa xếp dọc & Guest Logic) ---
        private void SetupLobbyScreen()
        {
            // 1. Reset Form
            pnlLobby.Controls.Clear();
            pnlLobby.BackColor = Color.FromArgb(30, 30, 40);

            // =================================================================================
            // A. CỘT TRÁI: CHAT THẾ GIỚI (CỐ ĐỊNH 260px)
            // =================================================================================
            Panel pnlLeftChat = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260, // Kích thước vừa đủ, không quá to
                BackColor = Color.FromArgb(25, 25, 35),
                Padding = new Padding(5)
            };

            // Tiêu đề Chat
            Label lblChatTitle = new Label
            {
                Text = "💬 THẾ GIỚI",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.Cyan,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Khu vực nhập chat (Dưới cùng)
            Panel pnlChatInput = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.Transparent };

            // Khung hiển thị chat
            RichTextBox rtbChatContent = new ExRichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10)
            };
            rtbLobbyChat = rtbChatContent; // Gán vào biến toàn cục

            // Input controls
            txtLobbyMessage = CreateInput("", 5, 5, 180);
            txtLobbyMessage.Width = 250; // Full width
            txtLobbyMessage.Height = 30;
            txtLobbyMessage.KeyPress += (s, e) => { if (e.KeyChar == 13) { SendLobbyMessage(); e.Handled = true; } };

            Button btnEmo = CreateButton("😎", 5, 40, 40, Color.Pink);
            btnEmo.Height = 30;
            btnEmo.Click += (s, e) => { if (pnlLobbyEmoteSelector.Visible) pnlLobbyEmoteSelector.Hide(); else { pnlLobbyEmoteSelector.Show(); pnlLobbyEmoteSelector.BringToFront(); } };

            Button btnSendChat = CreateButton("Gửi", 50, 40, 205, Color.DodgerBlue);
            btnSendChat.Height = 30;
            btnSendChat.Click += (s, e) => SendLobbyMessage();

            pnlChatInput.Controls.Add(txtLobbyMessage);
            pnlChatInput.Controls.Add(btnEmo);
            pnlChatInput.Controls.Add(btnSendChat);

            // Emote Popup
            //pnlLobbyEmoteSelector = new Panel { Size = new Size(200, 60), Location = new Point(30, 400), BackColor = Color.White, Visible = false, BorderStyle = BorderStyle.FixedSingle };
            //string[] emojis = { "😂", "😡", "😭", "😎", "😀", "😃", "😄", "😁", 
            //    "😆", "😅", " 😂", "🤣", "😴", "🤤", "😪", " 😲", "😯", "😦", "😧", "😨", "😰", "😥", "😢", "😭", "😱", "😖", "😣", "😞", "😓", "😩", "😫"
            //    ,"😎", "😼"
            //};


            
            pnlLobbyEmoteSelector = CreatePagedEmotePanel(
                new Point(15, 400), 
                (symbol) => {
                    SendCommand($"LOBBY_CHAT|{symbol}");
                    pnlLobbyEmoteSelector.Visible = false; // Chọn xong thì ẩn bảng đi
                }
            );
            // ------------------------

            pnlLeftChat.Controls.Add(pnlLobbyEmoteSelector);
            pnlLobbyEmoteSelector.BringToFront();
            // --------------------------------------------------

            pnlLeftChat.Controls.Add(pnlLobbyEmoteSelector);

            pnlLeftChat.Controls.Add(pnlLobbyEmoteSelector);
            pnlLeftChat.Controls.Add(rtbChatContent);
            pnlLeftChat.Controls.Add(pnlChatInput);
            pnlLeftChat.Controls.Add(lblChatTitle);


            // =================================================================================
            // B. PHẦN CHÍNH (BÊN PHẢI): DÙNG TABLE LAYOUT ĐỂ KHÔNG BỊ VỠ
            // =================================================================================
            Panel pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // 1. Nút Cài đặt & Đăng xuất (Góc phải trên cùng)
            // Neo (Anchor) vào góc phải để khi kéo form nó chạy theo
            Button btnSet = CreateButton("⚙ Cài đặt", 0, 20, 110, Color.ForestGreen);
            Button btnOut = CreateButton("Đăng xuất", 0, 70, 110, Color.FromArgb(80, 80, 80));

            // Đặt vị trí ban đầu (Sẽ được cập nhật lại ở sự kiện Resize nhưng cứ đặt trước cho chắc)
            btnSet.Location = new Point(pnlMain.Width - 130, 20);
            btnOut.Location = new Point(pnlMain.Width - 130, 70);

            btnSet.Click += (s, e) => ShowSettingsDialog();
            btnOut.Click += (s, e) => PerformLogout();

            pnlMain.Controls.Add(btnSet);
            pnlMain.Controls.Add(btnOut);

            // 2. MENU CHÍNH (DÙNG TABLE LAYOUT PANEL)
            // Đây là "khung lưới" giúp các nút tự xếp hàng, không bao giờ đè lên nhau
            TableLayoutPanel tblMenu = new TableLayoutPanel();
            tblMenu.Size = new Size(450, 600); // Kích thước tổng thể menu
            tblMenu.RowCount = 8;
            tblMenu.ColumnCount = 1;
            tblMenu.BackColor = Color.Transparent;
            // Căn giữa các nút trong lưới
            for (int i = 0; i < 8; i++) tblMenu.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F)); // Mỗi dòng cao 70px

            // --- Tạo nội dung Menu ---

            // Dòng 1: Xin chào
            lblWelcome = new Label { Text = "Xin chào!", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.Yellow, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            tblMenu.Controls.Add(lblWelcome, 0, 0);

            // Dòng 2: Trạng thái
            lblStatus = new Label { Text = "Online", Font = new Font("Segoe UI", 12), ForeColor = Color.LightGreen, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            this.Load += (s, e) => { lblStatus.Text = isLoggedIn ? $"Biệt danh: {currentUsername}" : "Khách"; };
            tblMenu.Controls.Add(lblStatus, 0, 1);

            // Helper tạo nút cho TableLayout
            Func<string, Color, Button> AddGridBtn = (txt, col) => {
                Button b = new Button
                {
                    Text = txt,
                    BackColor = col,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 13, FontStyle.Bold),
                    Dock = DockStyle.Fill, // Tự lấp đầy ô lưới
                    Margin = new Padding(10, 5, 10, 5) // Cách lề một chút
                };
                b.FlatAppearance.BorderSize = 0;
                return b;
            };

            // Dòng 3: Tìm trận
            btnFindMatch = AddGridBtn("🔥 TÌM TRẬN NGẪU NHIÊN", Color.OrangeRed);
            btnFindMatch.Click += (s, e) => { selectedGameMode = 1; ShowScreen(pnlBoardSize); };
            tblMenu.Controls.Add(btnFindMatch, 0, 2);

            // Dòng 4: Tạo phòng
            btnCreatePrivate = AddGridBtn("🏠 TẠO PHÒNG RIÊNG", Color.Teal);
            btnCreatePrivate.Click += (s, e) => { selectedGameMode = 2; ShowScreen(pnlBoardSize); };
            tblMenu.Controls.Add(btnCreatePrivate, 0, 3);

            // Dòng 5: Xếp hạng
            btnLeaderboard = AddGridBtn("🏆 BẢNG XẾP HẠNG", Color.Gold);
            btnLeaderboard.ForeColor = Color.Black;
            btnLeaderboard.Click += (s, e) => SendCommand("GET_LEADERBOARD");
            tblMenu.Controls.Add(btnLeaderboard, 0, 4);

            // Dòng 6: Lịch sử
            btnHistory = AddGridBtn("📜 LỊCH SỬ ĐẤU", Color.SlateGray);
            btnHistory.Click += (s, e) => { if (!isLoggedIn || isGuest) MessageBox.Show("Khách không có lịch sử.", "Thông báo"); else SendCommand("GET_HISTORY"); };
            tblMenu.Controls.Add(btnHistory, 0, 5);

            // Dòng 7: Panel nhập ID
            Panel pnlJoinGrid = new Panel { Dock = DockStyle.Fill, Margin = new Padding(10, 5, 10, 5) };
            txtRoomIDJoin = CreateInput("", 0, 10, 280); txtRoomIDJoin.Height = 35; txtRoomIDJoin.Font = new Font("Segoe UI", 12);
            // Tính toán lại chiều rộng TextBox cho vừa khung
            txtRoomIDJoin.Dock = DockStyle.Left;
            txtRoomIDJoin.Width = 250;

            Button btnJoinG = new Button { Text = "Vào", BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Width = 80, Dock = DockStyle.Right };
            btnJoinG.Click += (s, e) => { if (!string.IsNullOrWhiteSpace(txtRoomIDJoin.Text)) { selectedGameMode = 3; tempRoomID = txtRoomIDJoin.Text; ShowScreen(pnlBoardSize); } };

            Button btnWatchG = new Button { Text = "Xem", BackColor = Color.DarkSlateBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Width = 80, Dock = DockStyle.Right };
            btnWatchG.Click += (s, e) => { if (!string.IsNullOrWhiteSpace(txtRoomIDJoin.Text)) SendCommand($"WATCH_ROOM|{txtRoomIDJoin.Text}"); };

            // Add vào panel con (Lưu ý thứ tự Dock: Right add trước sẽ nằm ngoài cùng bên phải)
            pnlJoinGrid.Controls.Add(txtRoomIDJoin);
            pnlJoinGrid.Controls.Add(btnWatchG); // Nút Xem nằm giữa
            pnlJoinGrid.Controls.Add(btnJoinG);  // Nút Vào nằm sát phải

            // Groupbox bọc ngoài cho đẹp
            GroupBox gbJ = new GroupBox { Text = "Nhập ID phòng:", ForeColor = Color.Gray, Dock = DockStyle.Fill };
            gbJ.Controls.Add(pnlJoinGrid);

            tblMenu.Controls.Add(gbJ, 0, 6);


            // --- KẾT THÚC CẤU HÌNH ---
            pnlMain.Controls.Add(tblMenu);
            pnlLobby.Controls.Add(pnlMain);
            pnlLobby.Controls.Add(pnlLeftChat);

            // Nút Admin (Ẩn)
            btnOpenAdmin = CreateButton("ADMIN", 0, 0, 100, Color.Red); btnOpenAdmin.Visible = false;
            btnOpenAdmin.Click += (s, e) => { ShowScreen(pnlAdmin); SendCommand("ADMIN_LIST"); };
            pnlMain.Controls.Add(btnOpenAdmin);

            // SỰ KIỆN CĂN GIỮA & RESPONSIVE (QUAN TRỌNG NHẤT)
            pnlLobby.SizeChanged += (s, e) => {
                if (pnlMain != null && tblMenu != null)
                {
                    // 1. Căn giữa Menu
                    // Nếu form nhỏ quá thì menu tự thu nhỏ lại để không vỡ
                    int targetWidth = Math.Min(450, pnlMain.Width - 40);
                    tblMenu.Size = new Size(targetWidth, 600);

                    tblMenu.Location = new Point(
                        (pnlMain.Width - tblMenu.Width) / 2,
                        Math.Max(0, (pnlMain.Height - tblMenu.Height) / 2)
                    );

                    // 2. Neo nút Cài đặt/Đăng xuất góc phải
                    btnSet.Location = new Point(pnlMain.Width - 140, 20);
                    btnOut.Location = new Point(pnlMain.Width - 140, 70);
                    btnOpenAdmin.Location = new Point(pnlMain.Width - 140, 120);
                }
            };
            pnlLobby.SizeChanged += pnlLobby_Resize;
            // Kích hoạt sự kiện resize một lần ngay khi chạy để sắp xếp
            pnlLobby_Resize(null, null);
        }

        
        // Hàm phụ để xử lý Responsive (Co giãn giao diện)
        private void pnlLobby_Resize(object sender, EventArgs e)
        {
            // Kiểm tra an toàn
            if (pnlLobby == null || pnlLobby.Controls.Count == 0) return;

            // 1. Tìm pnlMain (Là Panel chiếm toàn bộ không gian - Dock.Fill)
            Panel pnlMain = null;
            foreach (Control c in pnlLobby.Controls)
            {
                if (c is Panel && c.Dock == DockStyle.Fill)
                {
                    pnlMain = (Panel)c;
                    break;
                }
            }

            if (pnlMain == null) return; // Không tìm thấy thì thoát

            // 2. Tìm tblMenu (Là TableLayoutPanel chứa các nút chức năng)
            TableLayoutPanel tblMenu = null;
            foreach (Control c in pnlMain.Controls)
            {
                if (c is TableLayoutPanel)
                {
                    tblMenu = (TableLayoutPanel)c;
                    break;
                }
            }

            // 3. Thực hiện tính toán và căn giữa Menu
            if (tblMenu != null)
            {
                // Tính chiều rộng mục tiêu (giới hạn max 450px)
                int targetWidth = Math.Min(450, pnlMain.Width - 40);

                // Cập nhật kích thước
                tblMenu.Size = new Size(targetWidth, 600);

                // Căn giữa tblMenu trong pnlMain
                tblMenu.Location = new Point(
                    (pnlMain.Width - tblMenu.Width) / 2,
                    Math.Max(0, (pnlMain.Height - tblMenu.Height) / 2)
                );
            }

            // 4. Neo các nút góc phải (Cài đặt, Đăng xuất, Admin)
            // Vì các nút này có thể là biến cục bộ, ta tìm theo Text hoặc tên biến toàn cục
            foreach (Control c in pnlMain.Controls)
            {
                if (c is Button b)
                {
                    if (b.Text.Contains("Cài đặt"))
                        b.Location = new Point(pnlMain.Width - 140, 20);

                    else if (b.Text.Contains("Đăng xuất"))
                        b.Location = new Point(pnlMain.Width - 140, 70);

                    else if (b.Text.Contains("ADMIN"))
                        b.Location = new Point(pnlMain.Width - 140, 120);
                }
            }
        }

        // Hàm gửi tin nhắn Lobby
        private void SendLobbyMessage()
        {
            if (!string.IsNullOrWhiteSpace(txtLobbyMessage.Text))
            {
                SendCommand($"LOBBY_CHAT|{txtLobbyMessage.Text}");
                txtLobbyMessage.Clear();
            }
        }

        private void CenterLoginControls()
        {
            if (pnlLogin != null && pnlLogin.Visible)
            {
                int cx = pnlLogin.Width / 2;
                if (lblLoginTitle != null) lblLoginTitle.Location = new Point(cx - (lblLoginTitle.Width / 2), 30);
                if (gbGuest != null) gbGuest.Location = new Point(cx - (gbGuest.Width / 2), 120);
                if (gbLogin != null) gbLogin.Location = new Point(cx - (gbLogin.Width / 2), (gbGuest?.Bottom ?? 120) + 30);
            }
        }

        // Hàm căn giữa
        private void CenterLobbyControls()
        {
            if (pnlLobby != null && pnlLobby.Visible)
            {
                int cx = pnlLobby.Width / 2;
                int y = 50; int gap = 15;

                // Căn giữa các nhãn tiêu đề
                if (lblWelcome != null) { lblWelcome.Location = new Point(cx - (lblWelcome.Width / 2), y); y += lblWelcome.Height + 5; }
                if (lblStatus != null) { lblStatus.Location = new Point(cx - (lblStatus.Width / 2), y); y += lblStatus.Height + 30; }

                // --- [SỬA LỖI QUAN TRỌNG TẠI ĐÂY] ---
                if (btnOpenAdmin != null)
                {
                    // 1. Dòng này giúp nút hiện lại khi quay về Lobby (Code của em đang thiếu dòng này!)
                    btnOpenAdmin.Visible = isAdmin;

                    if (btnOpenAdmin.Parent != null)
                    {
                        // Dùng btnOpenAdmin.Parent.Width thay vì pnlLobby.Width
                        btnOpenAdmin.Location = new Point(btnOpenAdmin.Parent.Width - 140, 120);
                    }
                }
                // -------------------------------------

                // Căn giữa các nút chức năng
                if (btnLeaderboard != null) { btnLeaderboard.Location = new Point(cx - 150, y); y += btnLeaderboard.Height + gap; }
                if (btnHistory != null) { btnHistory.Location = new Point(cx - 150, y); y += btnHistory.Height + gap; }
                if (btnFindMatch != null) { btnFindMatch.Location = new Point(cx - 150, y); y += btnFindMatch.Height + gap; }
                if (btnCreatePrivate != null) { btnCreatePrivate.Location = new Point(cx - 150, y); y += btnCreatePrivate.Height + gap + 10; }
                if (pnlJoinGroup != null) { pnlJoinGroup.Location = new Point(cx - 150, y); y += pnlJoinGroup.Height + gap + 20; }
                if (btnLogout != null) { btnLogout.Location = new Point(cx - 150, y); }
            }
        }

        // --- CÁC MÀN HÌNH KHÁC (Giữ nguyên logic cũ nhưng gọn hơn) ---
        private void SetupRegisterScreen()
        {
            // 1. Tiêu đề lớn
            lblRegisterTitle = new Label
            {
                Text = "ĐĂNG KÝ TÀI KHOẢN",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.Cyan,
                AutoSize = true
                // Location tính sau
            };
            pnlRegister.Controls.Add(lblRegisterTitle);

            // 2. Khung chứa thông tin (Panel)
            pnlRegisterBox = new Panel
            {
                Size = new Size(500, 580), // Tăng chiều cao để chứa hết nút
                BackColor = Color.FromArgb(45, 45, 55), // Màu nền sáng hơn nền chính xíu
                BorderStyle = BorderStyle.FixedSingle
            };

            int y = 30; // Tọa độ Y bắt đầu
            int spacing = 75; // Khoảng cách giữa các mục (tăng lên cho thoáng)

            // -- Username --
            pnlRegisterBox.Controls.Add(new Label { Text = "Tên đăng nhập:", Location = new Point(40, y), ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10) });
            txtRegUsername = CreateInput("Tối thiểu 4 ký tự", 40, y + 25, 420);
            pnlRegisterBox.Controls.Add(txtRegUsername);
            y += spacing;

            // -- Password --
            pnlRegisterBox.Controls.Add(new Label { Text = "Mật khẩu:", Location = new Point(40, y), ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10) });
            txtRegPassword = CreateInput("Tối thiểu 6 ký tự", 40, y + 25, 420);
            txtRegPassword.UseSystemPasswordChar = true;
            pnlRegisterBox.Controls.Add(txtRegPassword);
            y += spacing;

            // -- Confirm Pass --
            pnlRegisterBox.Controls.Add(new Label { Text = "Nhập lại mật khẩu:", Location = new Point(40, y), ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10) });
            txtRegConfirmPassword = CreateInput("Phải khớp mật khẩu trên", 40, y + 25, 420);
            txtRegConfirmPassword.UseSystemPasswordChar = true;
            pnlRegisterBox.Controls.Add(txtRegConfirmPassword);
            y += spacing;

            // -- Display Name --
            pnlRegisterBox.Controls.Add(new Label { Text = "Tên hiển thị (Ingame):", Location = new Point(40, y), ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10) });
            txtRegDisplayName = CreateInput("Tên sẽ hiện trong game", 40, y + 25, 420);
            pnlRegisterBox.Controls.Add(txtRegDisplayName);
            y += spacing;

            // -- Email --
            pnlRegisterBox.Controls.Add(new Label { Text = "Email (Tùy chọn):", Location = new Point(40, y), ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10) });
            txtRegEmail = CreateInput("example@email.com", 40, y + 25, 420);
            pnlRegisterBox.Controls.Add(txtRegEmail);
            y += spacing - 15; // Rút ngắn khoảng cách đoạn cuối

            // -- Checkbox --
            chkAcceptTerms = new CheckBox
            {
                Text = "Tôi đồng ý với điều khoản sử dụng",
                Location = new Point(40, y),
                ForeColor = Color.LightGreen,
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            pnlRegisterBox.Controls.Add(chkAcceptTerms);
            y += 40;

            // -- Nút Đăng Ký --
            btnRegisterSubmit = CreateButton("ĐĂNG KÝ NGAY", 150, y, 200, Color.MediumPurple);
            btnRegisterSubmit.Click += (sender, e) => RegisterAccount();
            pnlRegisterBox.Controls.Add(btnRegisterSubmit);

            // -- Nút Quay lại --
            btnBackToLogin = CreateButton("⬅ Quay lại", 40, 520, 100, Color.Gray);
            btnBackToLogin.Height = 30; // Nút nhỏ thôi
            btnBackToLogin.Click += (sender, e) => ShowScreen(pnlLogin);
            pnlRegisterBox.Controls.Add(btnBackToLogin);

            // -- Label Trạng thái (Lỗi/Thành công) --
            lblRegStatus = new Label
            {
                Text = "",
                Location = new Point(150, 520),
                Size = new Size(300, 30),
                TextAlign = ContentAlignment.MiddleRight, // Căn phải cho gọn
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };
            pnlRegisterBox.Controls.Add(lblRegStatus);

            pnlRegister.Controls.Add(pnlRegisterBox);

            // Căn giữa ngay lần đầu
            CenterRegisterControls();
        }
        private void CenterRegisterControls()
        {
            if (pnlRegister != null && pnlRegister.Visible)
            {
                int centerX = pnlRegister.Width / 2;
                int centerY = pnlRegister.Height / 2;

                if (pnlRegisterBox != null)
                {
                    // Đặt khung đăng ký chính giữa màn hình
                    pnlRegisterBox.Location = new Point(centerX - (pnlRegisterBox.Width / 2), centerY - (pnlRegisterBox.Height / 2) + 20);
                }

                if (lblRegisterTitle != null && pnlRegisterBox != null)
                {
                    // Đặt tiêu đề nằm ngay trên khung đăng ký
                    lblRegisterTitle.Location = new Point(centerX - (lblRegisterTitle.Width / 2), pnlRegisterBox.Top - 60);
                }
            }
        }
        private void SetupAdminScreen()
        {
            pnlAdmin.Controls.Add(new Label { Text = "QUẢN TRỊ VIÊN", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.Red, Location = new Point(20, 20), AutoSize = true });
            rtbAdminData = new RichTextBox { Location = new Point(20, 70), Size = new Size(800, 500), Font = new Font("Consolas", 10), ReadOnly = true }; pnlAdmin.Controls.Add(rtbAdminData);
            txtKickUser = CreateInput("User to Kick...", 850, 70, 200); btnKick = CreateButton("KICK", 850, 110, 200, Color.DarkRed); btnKick.Click += (s, e) => { if (!string.IsNullOrWhiteSpace(txtKickUser.Text)) SendCommand($"ADMIN_KICK|{txtKickUser.Text}"); };
            btnBackFromAdmin = CreateButton("BACK", 850, 200, 200, Color.Gray); btnBackFromAdmin.Click += (s, e) => ShowScreen(pnlLobby);
            pnlAdmin.Controls.Add(txtKickUser); pnlAdmin.Controls.Add(btnKick); pnlAdmin.Controls.Add(btnBackFromAdmin);
        }

        private void SetupBoardSizeScreen()
        {
            lblBoardTitle = new Label { Text = "CHỌN KÍCH THƯỚC", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.Cyan, AutoSize = true, Location = new Point(350, 50) }; pnlBoardSize.Controls.Add(lblBoardTitle);
            btnBoard10x10 = CreateBoardSizeButton("10x10", "Nhanh", 350, 150, Color.FromArgb(100, 150, 200)); btnBoard10x10.Click += (s, e) => { boardSize = 10; ProcessSelectedGameMode(); }; pnlBoardSize.Controls.Add(btnBoard10x10);
            btnBoard15x15 = CreateBoardSizeButton("15x15", "Tiêu chuẩn", 350, 270, Color.FromArgb(70, 130, 180)); btnBoard15x15.Click += (s, e) => { boardSize = 15; ProcessSelectedGameMode(); }; pnlBoardSize.Controls.Add(btnBoard15x15);
            btnBoard20x20 = CreateBoardSizeButton("20x20", "Chiến thuật", 350, 390, Color.FromArgb(50, 110, 160)); btnBoard20x20.Click += (s, e) => { boardSize = 20; ProcessSelectedGameMode(); }; pnlBoardSize.Controls.Add(btnBoard20x20);
            btnBackToLobby = CreateButton("⬅ Quay lại", 350, 510, 400, Color.Gray); btnBackToLobby.Click += (s, e) => ShowScreen(pnlLobby); pnlBoardSize.Controls.Add(btnBackToLobby);
        }
        private void SetupDifficultyScreen()
        {
            // 1. Tiêu đề
            Label lblTitle = new Label
            {
                Text = "CHỌN ĐỘ KHÓ",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.Cyan,
                AutoSize = true,
                Location = new Point(420, 50) // Căn giữa tươm tất
            };
            pnlDifficulty.Controls.Add(lblTitle);

            // 2. Các nút chọn độ khó
            // DỄ (Màu xanh lá)
            Button btnEasy = CreateButton("🐣 DỄ (GÀ)", 350, 150, 400, Color.LimeGreen);
            btnEasy.Click += (s, e) => StartPvEGame(1); // Truyền mức 1
            pnlDifficulty.Controls.Add(btnEasy);

            // TRUNG BÌNH (Màu vàng)
            Button btnMedium = CreateButton("🐯 TRUNG BÌNH", 350, 250, 400, Color.Orange);
            btnMedium.Click += (s, e) => StartPvEGame(2); // Truyền mức 2
            pnlDifficulty.Controls.Add(btnMedium);

            // KHÓ (Màu đỏ)
            Button btnHard = CreateButton("🤖 KHÓ (SIÊU CẤP)", 350, 350, 400, Color.Red);
            btnHard.Click += (s, e) => StartPvEGame(3); // Truyền mức 3
            pnlDifficulty.Controls.Add(btnHard);

            // 3. Nút quay lại
            Button btnBack = CreateButton("⬅ QUAY LẠI", 350, 480, 400, Color.Gray);
            btnBack.Click += (s, e) => ShowScreen(pnlLogin);
            pnlDifficulty.Controls.Add(btnBack);
        }
        private void SetupGameScreen()
        {
            // --- 1. TẠO PANEL CHÍNH ---
            // Tăng độ rộng cột 2 bên lên 280 để thoáng hơn
            Panel left = new Panel { Dock = DockStyle.Left, Width = 280, BackColor = Color.FromArgb(40, 40, 50) };
            pnlGame.Controls.Add(left);

            Panel right = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Color.FromArgb(40, 40, 50) };
            pnlGame.Controls.Add(right);

            pnlChessBoard = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
            pnlChessBoard.Paint += PnlChessBoard_Paint;
            pnlChessBoard.MouseClick += PnlChessBoard_MouseClick;
            pnlGame.Controls.Add(pnlChessBoard);

            // =================================================================================
            // CỘT TRÁI: CHUYÊN DÙNG ĐỂ CHAT (Giao diện giống Messenger)
            // =================================================================================

            // Tiêu đề khung chat
            Label lblChatTitle = new Label { Text = "💬 TRÒ CHUYỆN", ForeColor = Color.Gray, Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            left.Controls.Add(lblChatTitle);

            // Khung hiển thị tin nhắn (Kéo dài gần hết chiều cao)
            rtbChatLog = new ExRichTextBox
            {
                Location = new Point(10, 35),
                Width = 260,
                Height = 560, // Tận dụng tối đa chiều cao
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10)
            };

            // Nút Emote (😎)
            Button btnEmote = CreateButton("😎", 10, 610, 40, Color.Pink);
            btnEmote.Height = 30;
            btnEmote.Click += (s, e) =>
            {
                if (pnlEmoteSelector.Visible) pnlEmoteSelector.Hide();
                else { pnlEmoteSelector.Show(); pnlEmoteSelector.BringToFront(); }
            };

            // Ô nhập tin nhắn
            txtMessage = CreateInput("", 60, 610, 150);
            txtMessage.Height = 30; // Cao hơn xíu cho dễ nhập
            txtMessage.KeyPress += (s, e) => { if (e.KeyChar == 13) { SendChatMessage(); e.Handled = true; } };

            // Nút Gửi
            btnSend = CreateButton("➤", 220, 610, 50, Color.DodgerBlue);
            btnSend.Height = 30;
            btnSend.Click += (s, e) => SendChatMessage();

            // Bảng chọn Emote (Popup) - Đặt vị trí để hiện lên trên nút
            pnlEmoteSelector = new Panel
            {
                Size = new Size(200, 60),
                Location = new Point(10, 540),
                BackColor = Color.White,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            // --- Thay thế đoạn cũ ---
            pnlEmoteSelector = CreatePagedEmotePanel(
                new Point(15, 540), // Vị trí nằm trên khung chat
                (symbol) => {
                    SendCommand($"EMOTE|{symbol}");
                    pnlEmoteSelector.Visible = false;
                }
            );
            // ------------------------

            left.Controls.Add(pnlEmoteSelector); // Add vào Panel bên trái (chứa khung chat)
            pnlEmoteSelector.BringToFront();
            // --------------------------------------------------

            left.Controls.Add(rtbChatLog);
            left.Controls.Add(btnEmote);
            left.Controls.Add(txtMessage);
            left.Controls.Add(btnSend);
            left.Controls.Add(pnlEmoteSelector);
            pnlEmoteSelector.BringToFront();

            left.Controls.Add(rtbChatLog);
            left.Controls.Add(btnEmote);
            left.Controls.Add(txtMessage);
            left.Controls.Add(btnSend);
            left.Controls.Add(pnlEmoteSelector);
            pnlEmoteSelector.BringToFront();


            // =================================================================================
            // CỘT PHẢI: BẢNG ĐIỀU KHIỂN & THÔNG TIN (Sắp xếp lại logic)
            // =================================================================================

            int centerX = 140; // Trục giữa của cột phải (280/2)

            // 1. Đồng hồ (To nhất, trên cùng)
            lblDongHo = new Label
            {
                Text = "03:00",
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = Color.Cyan,
                AutoSize = true,
                // Căn giữa thủ công sau khi tạo xong hoặc dùng logic này:
                Location = new Point(70, 20)
            };

            // Nút nhạc nhỏ gọn góc trên cùng
            btnToggleMusic = CreateButton(isMusicOn ? "🔊" : "🔇", 230, 10, 40, Color.DarkSlateGray);
            btnToggleMusic.Font = new Font("Segoe UI Emoji", 10);
            btnToggleMusic.Height = 30;
            btnToggleMusic.Click += (s, e) =>
            {
                isMusicOn = !isMusicOn;
                btnToggleMusic.Text = isMusicOn ? "🔊" : "🔇";
                if (isMusicOn) PlayGameMusic(); else StopGameMusic();
            };

            // Thanh CoolDown
            prcbCoolDown = new ProgressBar { Location = new Point(40, 90), Width = 200, Height = 10, Maximum = tongThoiGian, Value = tongThoiGian };

            // 2. Khu vực Avatar (Đưa lên giữa để dễ nhìn) - QUAN TRỌNG
            // Avatar 1 (Bên Trái)
            ptbAvatar1 = new PictureBox { Size = new Size(80, 80), Location = new Point(30, 120), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.Black };
            // Label Emote P1
            lblEmoteP1 = new Label { Size = new Size(80, 80), Location = new Point(30, 120), BackColor = Color.Transparent, Font = new Font("Segoe UI Emoji", 40), TextAlign = ContentAlignment.MiddleCenter, Visible = false };

            // Chữ VS ở giữa
            Label lblVS = new Label { Text = "VS", Font = new Font("Segoe UI", 16, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.Red, AutoSize = true, Location = new Point(125, 145) };

            // Avatar 2 (Bên Phải)
            ptbAvatar2 = new PictureBox { Size = new Size(80, 80), Location = new Point(170, 120), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.Black };
            // Label Emote P2
            lblEmoteP2 = new Label { Size = new Size(80, 80), Location = new Point(170, 120), BackColor = Color.Transparent, Font = new Font("Segoe UI Emoji", 40), TextAlign = ContentAlignment.MiddleCenter, Visible = false };

            // 3. Trạng thái lượt đi (Dưới Avatar)
            lblLuotDi = new Label { Text = "Đang chờ...", Location = new Point(40, 220), AutoSize = false, Size = new Size(200, 30), ForeColor = Color.Yellow, Font = new Font("Segoe UI", 11, FontStyle.Italic), TextAlign = ContentAlignment.MiddleCenter };

            // 4. Các nút hành động (Gom nhóm lại)
            int btnY = 270;
            int btnH = 45;
            int btnW = 200;
            int btnX = 40;

            btnNewGame = CreateButton("VÁN MỚI", btnX, btnY, btnW, Color.Teal);
            btnNewGame.Click += (s, e) =>
            {
                // Nếu chơi với máy thì reset luôn không cần hỏi
                if (CheDoChoi == "VS_MAY")
                {
                    StartPvEGame(currentLevel); 
                }
                // Nếu chơi Online thì phải xin phép
                else
                {
                    // Kiểm tra xem có đang chơi không, hoặc hết ván mới được xin (tùy em)
                    SendCommand("NEW_GAME_REQUEST");

                    // Disable nút đi để tránh spam
                    btnNewGame.Enabled = false;
                    btnNewGame.Text = "Đang chờ...";
                }
            };

            btnUndo = CreateButton("XIN ĐI LẠI", btnX, btnY + 60, btnW, Color.Goldenrod);
            btnUndo.Click += (s, e) => { if (CheDoChoi != "VS_MAY") SendCommand("UNDO_REQUEST"); };

            btnXinHoa = CreateButton("CẦU HÒA", btnX, btnY + 120, btnW, Color.Gray);
            btnXinHoa.Click += (s, e) =>
            {
                // 1. Nếu chơi với Máy: Máy không biết hòa (hoặc em có thể cho máy random đồng ý)
                if (CheDoChoi == "VS_MAY")
                {
                    MessageBox.Show("Máy tính bảo: 'Đã đánh là phải phân thắng bại!'", "Thông báo");
                    return;
                }

                // 2. Nếu chơi Online
                DialogResult confirm = MessageBox.Show("Bạn có chắc muốn xin hòa không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    SendCommand("DRAW_REQUEST");

                    // Khóa nút lại để chờ phản hồi
                    btnXinHoa.Enabled = false;
                    btnXinHoa.Text = "Đang xin...";
                }
            };

            btnXinThua = CreateButton("ĐẦU HÀNG", btnX, btnY + 180, btnW, Color.Maroon);
            btnXinThua.Click += (s, e) => { if (MessageBox.Show("Chắc chắn đầu hàng?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes) SendCommand("SURRENDER"); };

            // 5. Nút Rời phòng (Đưa xuống cuối cùng, màu đỏ đậm)
            btnLeaveGame = CreateButton("⬅ RỜI PHÒNG", btnX, 580, btnW, Color.FromArgb(60, 60, 60)); // Màu xám đậm
            btnLeaveGame.Click += (s, e) =>
            {
                SendCommand("LEAVE_GAME");
                ShowScreen(pnlLobby);
                StopGameMusic();
                mySide = 0;
                tmCoolDown.Stop();
            };

            // --- ADD CONTROLS VÀO PANEL PHẢI ---
            right.Controls.Add(lblDongHo);
            right.Controls.Add(btnToggleMusic);
            right.Controls.Add(prcbCoolDown);
            Button btnTheme = CreateButton("🎨 Đổi màu", 40, 520, 200, Color.Purple); // Vị trí nằm trên nút Rời phòng
            btnTheme.Click += (s, e) => {
                // Đổi vòng tròn: 0 -> 1 -> 2 -> 0
                currentTheme++;
                if (currentTheme > 2) currentTheme = 0;

                // Vẽ lại bàn cờ ngay lập tức
                pnlChessBoard.Invalidate();

                // Thông báo nhỏ (tuỳ chọn)
                string themeName = currentTheme == 0 ? "Sáng" : (currentTheme == 1 ? "Tối" : "Gỗ");
                // MessageBox.Show($"Đã đổi sang giao diện: {themeName}"); 
            };
            right.Controls.Add(btnTheme);
            // Add Avatar & Emote (Emote phải Add sau hoặc BringToFront)
            right.Controls.Add(ptbAvatar1);
            right.Controls.Add(ptbAvatar2);
            right.Controls.Add(lblEmoteP1);
            right.Controls.Add(lblEmoteP2);
            right.Controls.Add(lblVS);
            lblEmoteP1.BringToFront();
            lblEmoteP2.BringToFront();

            right.Controls.Add(lblLuotDi);
            right.Controls.Add(btnNewGame);
            right.Controls.Add(btnUndo);
            right.Controls.Add(btnXinHoa);
            right.Controls.Add(btnXinThua);
            right.Controls.Add(btnLeaveGame);
            SetDoubleBuffered(pnlChessBoard); // Bật chống giật cho bàn cờ
            // Timer ẩn Emote
            tmHideEmote = new System.Windows.Forms.Timer { Interval = 3000 };
            tmHideEmote.Tick += (s, e) => {
                lblEmoteP1.Visible = false;
                lblEmoteP2.Visible = false;
                tmHideEmote.Stop();
            };
        }

        // --- HÀM LOGIC GAME ---
        // Thêm tham số 'level' vào hàm
        private void StartPvEGame(int level)
        {
            currentLevel = level;
            CheDoChoi = "VS_MAY";
            boardSize = 15;
            mySide = 1;
            banCoAo = new int[boardSize, boardSize];
            playerMoveCount = 0;

            // --- [QUAN TRỌNG: CÀI ĐẶT ĐỘ KHÓ CHO AI] ---
            aiBot.SetDifficulty(level);
            // -------------------------------------------

            currentUsername = (string.IsNullOrWhiteSpace(txtNickNameGuest.Text) || txtNickNameGuest.Text == "Nhập biệt danh...") ? "Người Chơi" : txtNickNameGuest.Text;

            ShowScreen(pnlGame);
            pnlChessBoard.Invalidate();

            // Cập nhật text hiển thị độ khó cho ngầu
            string strLevel = level == 1 ? "Dễ" : (level == 2 ? "Trung Bình" : "Khó");
            lblLuotDi.Text = $"Bạn đi trước (Mức: {strLevel})";

            lblWelcome.Text = $"Xin chào, {currentUsername}!";
            btnXinThua.Visible = false;
            btnXinHoa.Visible = false;
            btnUndo.Enabled = true;
            ResetTimer();
        }

        // Win vẽ đường thắng
        private void InitializeGameLogic()
        {
            CheckForIllegalCrossThreadCalls = false;
            tmCoolDown = new System.Windows.Forms.Timer { Interval = 1000 };

            // --- [SỬA ĐOẠN NÀY] ---
            tmCoolDown.Tick += (s, e) => {
                if (thoiGianConLai > 0)
                {
                    thoiGianConLai--;
                    lblDongHo.Text = TimeSpan.FromSeconds(thoiGianConLai).ToString(@"mm\:ss");
                    prcbCoolDown.Value = Math.Min(thoiGianConLai, prcbCoolDown.Maximum);
                    blinkToggle = !blinkToggle; // Đảo trạng thái nhấp nháy
                    pnlChessBoard.Invalidate(); // Vẽ lại bàn cờ
                    // --- [CHÈN CODE ÂM THANH HỒI HỘP VÀO ĐÂY] ---
                    if (thoiGianConLai <= 10) // Nếu còn dưới 10 giây
                    {
                        lblDongHo.ForeColor = Color.Red; // Đổi màu chữ sang đỏ

                        // Phát tiếng "Bụp" (Click) mỗi giây để giả lập tiếng kim đồng hồ
                        PlaySound(Properties.Resources.Click);
                    }
                    else
                    {
                        lblDongHo.ForeColor = Color.Cyan; // Trả lại màu xanh bình thường
                    }
                    // ----------------------------------------------
                }
                else
                {
                    tmCoolDown.Stop();
                    if (CheDoChoi == "LAN") SendCommand("TIME_OUT");
                    else MessageBox.Show("Hết giờ! Bạn thua.");
                }
            };
            // ----------------------

            try
            {
                imgX = Properties.Resources.x;
                imgO = Properties.Resources.o;
            }
            catch
            {
                MessageBox.Show("Lỗi tải hình ảnh từ Resources!");
            }
        }

        private void PnlChessBoard_Paint(object sender, PaintEventArgs e)
        {
            // 1. Luôn xóa nền trước (Tránh màn hình đen/trong suốt)
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color gridColor = Color.Gray; // Màu đường kẻ mặc định
            if (currentTheme == 0) // Sáng
            {
                g.Clear(Color.WhiteSmoke);
                gridColor = Color.Gray;
            }
            else if (currentTheme == 1) // Tối (Dark Mode)
            {
                g.Clear(Color.FromArgb(40, 40, 40));
                gridColor = Color.DimGray;
            }
            else if (currentTheme == 2) // Gỗ (Wood)
            {
                g.Clear(Color.BurlyWood);
                gridColor = Color.SaddleBrown;
            }
            g.Clear(Color.WhiteSmoke); // Tô màu nền bàn cờ

            // 2. Kiểm tra boardSize hợp lệ
            if (boardSize <= 0) return;

            // 3. Tính toán kích thước ô cờ (cs) và tọa độ bắt đầu (ox, oy)
            GetBoardMetrics(out float cs, out float ox, out float oy);

            // Nếu kích thước ô quá nhỏ hoặc bị lỗi -> Không vẽ
            if (cs <= 0 || float.IsNaN(cs) || float.IsInfinity(cs)) return;

            // Dịch chuyển gốc tọa độ để vẽ từ lề đã tính toán
            g.TranslateTransform(ox, oy);

            // 4. Vẽ lưới bàn cờ
            using (Pen pen = new Pen(gridColor, 1))
            {
                for (int i = 0; i <= boardSize; i++)
                {
                    // Vẽ đường dọc
                    g.DrawLine(pen, i * cs, 0, i * cs, cs * boardSize);
                    // Vẽ đường ngang
                    g.DrawLine(pen, 0, i * cs, cs * boardSize, i * cs);
                }
            }

            // 5. Vẽ lại các quân cờ từ bộ nhớ (banCoAo)
            if (banCoAo != null && banCoAo.GetLength(0) == boardSize)
            {
                for (int i = 0; i < boardSize; i++)
                {
                    for (int j = 0; j < boardSize; j++)
                    {
                        if (banCoAo[i, j] == 1) // Quân X
                            DrawChess(g, imgX, "X", Brushes.Red, i, j, cs);
                        else if (banCoAo[i, j] == 2) // Quân O
                            DrawChess(g, imgO, "O", Brushes.Blue, i, j, cs);
                    }
                }
            }

            // 6. [QUAN TRỌNG] Vẽ đường gạch ngang chiến thắng
            if (winStart.X != -1 && winEnd.X != -1)
            {
                // Thay vì dùng cứng số 30, ta dùng biến 'cs' đã tính ở trên
                // Để đảm bảo chính xác khi thay đổi kích thước cửa sổ
                float offset = cs / 2;

                // Tính tọa độ thực tế (Float để chính xác hơn)
                float x1 = winStart.X * cs + offset;
                float y1 = winStart.Y * cs + offset;
                float x2 = winEnd.X * cs + offset;
                float y2 = winEnd.Y * cs + offset;

                // Dùng bút màu Đỏ, độ dày 5
                using (Pen winPen = new Pen(Color.Red, 5))
                {
                    // Vẽ đường thẳng nối tâm 2 điểm đầu cuối
                    g.DrawLine(winPen, x1, y1, x2, y2);
                }
            }
            // --- [THAY THẾ ĐOẠN IF CŨ BẰNG ĐOẠN NÀY] ---
            if (isGameOverEffect)
            {
                e.Graphics.ResetTransform();
                // 1. Vẽ màn đen mờ (Overlay)
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(180, 0, 0, 0))) // Tăng độ tối lên 180 cho chữ nổi hơn
                {
                    e.Graphics.FillRectangle(brush, 0, 0, pnlChessBoard.Width, pnlChessBoard.Height);
                }

                // 2. Vẽ chữ (Chỉ vẽ khi nhấp nháy)
                if (blinkToggle)
                {
                    // Bật chế độ vẽ chữ đẹp (Khử răng cưa)
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    // Cấu hình Font và Căn giữa
                    using (Font f = new Font("Segoe UI", 48, FontStyle.Bold)) // Em có thể giảm 48 xuống 36 nếu chữ to quá
                    using (SolidBrush textBrush = new SolidBrush(endResultColor))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;     // Căn giữa ngang
                        sf.LineAlignment = StringAlignment.Center; // Căn giữa dọc

                        // Vẽ chữ vào chính giữa Panel
                        e.Graphics.DrawString(endResultText, f, textBrush,
                            new RectangleF(0, 0, pnlChessBoard.Width, pnlChessBoard.Height),
                            sf);
                    }
                }
            }
            // -------------------------------------------
        }

        private void PnlChessBoard_MouseClick(object sender, MouseEventArgs e)
        {
            GetBoardMetrics(out float cs, out float ox, out float oy);
            float cx = e.X - ox; float cy = e.Y - oy;
            if (cx < 0 || cy < 0) return;
            int x = (int)(cx / cs); int y = (int)(cy / cs);
            if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return;
            PlaySound(Properties.Resources.Click); // Tiếng Bụp
            if (CheDoChoi == "SPECTATOR") return; // Khán giả không được đánh
            if (CheDoChoi == "VS_MAY")
            {
                if (isAiThinking || banCoAo[x, y] != 0) return;
                Graphics g = pnlChessBoard.CreateGraphics(); g.SmoothingMode = SmoothingMode.AntiAlias; g.TranslateTransform(ox, oy);
                DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                banCoAo[x, y] = 1;
                PlaySound(Properties.Resources.Click);
                playerMoveCount++;
                if (CheckWinClient(banCoAo, x, y, 1)) { MessageBox.Show("Thắng rồi!"); return; }
                isAiThinking = true; lblLuotDi.Text = "Máy đang nghĩ...";
                Task.Run(() => {
                    Thread.Sleep(800); Point move = aiBot.Execute(banCoAo, boardSize);
                    this.Invoke(new Action(() => {
                        Graphics g2 = pnlChessBoard.CreateGraphics(); g2.SmoothingMode = SmoothingMode.AntiAlias; g2.TranslateTransform(ox, oy);
                        DrawChess(g2, imgO, "O", Brushes.Blue, move.X, move.Y, cs); banCoAo[move.X, move.Y] = 2; PlaySound(Properties.Resources.Click);
                        if (CheckWinClient(banCoAo, move.X, move.Y, 2))
                        {
                            string thongBao = "";

                            // Dưới 10 nước mà thua -> Gà
                            if (playerMoveCount < 10)
                                thongBao = "Máy đã thắng! Gà quá đi :v";

                            // Từ 10 đến 20 nước -> Có chút thực lực
                            else if (playerMoveCount < 20)
                                thongBao = "Máy đã thắng! Bạn cũng có chút thực lực đấy.";

                            // Từ 20 đến 40 nước -> Có cố gắng
                            else if (playerMoveCount < 40)
                                thongBao = "Máy đã thắng! Bạn cũng có cố gắng, trận đấu khá dài.";

                            // Trên 40 nước -> Tốt
                            else
                                thongBao = "Máy đã thắng! Kỹ năng tốt đó, suýt nữa thì bạn thắng.";

                            MessageBox.Show(thongBao, "Kết quả");
                        }
                        isAiThinking = false; lblLuotDi.Text = "Đến lượt bạn";
                    }));
                });
            }

            else
            {
                if (!IsConnected()) return;
                string l = lblLuotDi.Text; if ((mySide == 1 && !l.Contains("X") && !l.Contains("bạn")) || (mySide == 2 && !l.Contains("O") && !l.Contains("bạn"))) return;
                SendCommand($"MOVE|{x}|{y}"); thoiGianConLai += 3;
                lblDongHo.Text = TimeSpan.FromSeconds(thoiGianConLai).ToString(@"mm\:ss");
            }
        }

        private bool CheckWinClient(int[,] board, int x, int y, int side)
        {
            int size = boardSize; // Lấy kích thước bàn cờ hiện tại
            int[] dx = { 1, 0, 1, 1 };
            int[] dy = { 0, 1, 1, -1 };

            // Xác định quân đối thủ để kiểm tra chặn
            int opponent = (side == 1) ? 2 : 1;

            for (int dir = 0; dir < 4; dir++)
            {
                int count = 1;

                // 1. Quét chiều dương
                int i = 1;
                while (true)
                {
                    int nx = x + i * dx[dir];
                    int ny = y + i * dy[dir];
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size || board[nx, ny] != side) break;
                    count++; i++;
                }
                // Vị trí chặn đầu dương
                int nx_end = x + i * dx[dir];
                int ny_end = y + i * dy[dir];

                // 2. Quét chiều âm
                int j = 1;
                while (true)
                {
                    int nx = x - j * dx[dir];
                    int ny = y - j * dy[dir];
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size || board[nx, ny] != side) break;
                    count++; j++;
                }
                // Vị trí chặn đầu âm
                int nx_start = x - j * dx[dir];
                int ny_start = y - j * dy[dir];

                if (count >= 5)
                {
                    // --- LOGIC CHẶN 2 ĐẦU (Client) ---
                    if (count == 5)
                    {
                        bool isBlockedStart = false;
                        bool isBlockedEnd = false;

                        // Kiểm tra đầu Start (Ngoài biên hoặc bị chặn bởi địch)
                        if (nx_start < 0 || nx_start >= size || ny_start < 0 || ny_start >= size || board[nx_start, ny_start] == opponent)
                            isBlockedStart = true;

                        // Kiểm tra đầu End
                        if (nx_end < 0 || nx_end >= size || ny_end < 0 || ny_end >= size || board[nx_end, ny_end] == opponent)
                            isBlockedEnd = true;

                        // Nếu bị chặn cả 2 đầu thì KHÔNG tính thắng
                        if (isBlockedStart && isBlockedEnd) continue;
                    }

                    // Nếu thoát được điều kiện trên thì là thắng
                    return true;
                }
            }
            return false;
        }

        private void ReceiveMessage()
        {
            try
            {
                while (client.Connected)
                {
                    string msg = reader.ReadLine();
                    if (msg == null) break;

                    string[] parts = msg.Split('|');
                    string cmd = parts[0];

                    if (cmd == "LOGIN_SUCCESS")
                    {
                        string n = parts[1];
                        string r = parts[2];
                        isAdmin = (r == "1");
                        isLoggedIn = true;
                        currentUsername = n;
                        this.Invoke(new Action(() => {
                            lblWelcome.Text = $"Xin chào, {n}!";
                            btnOpenAdmin.Visible = isAdmin;
                            ShowScreen(pnlLobby);
                            txtUserLogin.Clear();
                            txtPassLogin.Clear();
                            txtNickNameGuest.Clear();
                        }));
                    }
                    else if (cmd == "LOGIN_FAIL" || cmd == "REGISTER_FAIL")
                    {
                        string r = parts[1];
                        this.Invoke(new Action(() => MessageBox.Show(r, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                    else if (cmd == "REGISTER_SUCCESS")
                    {
                        this.Invoke(new Action(() => {
                            MessageBox.Show("Đăng ký thành công!", "Thông báo");
                            ShowScreen(pnlLogin);
                        }));
                    }
                    else if (cmd == "RESET_SUCCESS")
                    {
                        string content = parts[1];
                        this.Invoke(new Action(() => MessageBox.Show(content, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                    }
                    else if (cmd == "RESET_FAIL")
                    {
                        string content = parts[1];
                        this.Invoke(new Action(() => MessageBox.Show(content, "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                    // ... (Sau các lệnh RESET_FAIL, trước XỬ LÝ TRONG GAME)
                    else if (cmd == "FORBIDDEN")
                    {
                        string reason = parts[1];
                        this.Invoke(new Action(() => {
                            MessageBox.Show($"Nước đi không hợp lệ: {reason}", "Luật Cấm", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            // [QUAN TRỌNG] Tự động xóa quân vừa đánh (Logic giống UNDO)
                            // Lấy tọa độ nước đi cuối cùng của mình
                            // Do Server đã gửi lại lệnh UNDO cho đối thủ, ta chỉ cần tự undo cho mình
                            if (banCoAo != null)
                            {
                                // Tìm nước vừa đánh (là nước cuối cùng của mình)
                                // Do Server đã hủy nước đi, ta chỉ cần xóa nó trên giao diện
                                // Do Client chưa có lịch sử nước đi, ta phải dùng logic xóa quân cờ:

                                // Xóa quân vừa đánh trên giao diện
                                int lastX = -1, lastY = -1;
                                // Tạm thời reset timer và cho phép đánh lại
                                ResetTimer();

                                // Cần một cách an toàn để lấy tọa độ quân vừa đánh.
                                // Vì Server đã trả lại lượt cho mình, ta chỉ cần ép vẽ lại bàn cờ
                                // (Giả sử Client đã có logic lưu nước đi)

                                pnlChessBoard.Invalidate(); // Vẽ lại để xóa quân cờ sai

                                // Cập nhật lại lượt đi trên UI
                                string myTurnText = (mySide == 1) ? "Đến lượt BẠN (X)" : "Đến lượt BẠN (O)";
                                lblLuotDi.Text = myTurnText;

                            }
                        }));
                    }
                    // ...
                    // --- XỬ LÝ TRONG GAME ---
                    else if (cmd == "MOVE")
                    {
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        int s = int.Parse(parts[3]);

                        this.Invoke(new Action(() => {
                            // 1. Luôn phát tiếng Bụp khi có quân cờ đặt xuống
                            PlaySound(Properties.Resources.Click);

                            // 2. Nếu là đối thủ đánh -> Kêu Ding
                            if (s != mySide)
                            {
                                Task.Delay(200).ContinueWith(t => PlaySound(Properties.Resources.ding));
                            }

                            // 3. Cập nhật dữ liệu bàn cờ
                            if (banCoAo != null && x >= 0 && x < boardSize && y >= 0 && y < boardSize)
                            {
                                banCoAo[x, y] = s;
                            }

                            // 4. Vẽ quân cờ (Có ResetTransform để chống lệch)
                            Graphics g = pnlChessBoard.CreateGraphics();
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            GetBoardMetrics(out float cs, out float ox, out float oy);

                            g.ResetTransform(); // <--- Rất quan trọng để fix lỗi lệch tâm
                            g.TranslateTransform(ox, oy);

                            if (s == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                            else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);

                            lblLuotDi.Text = (s == 1) ? "Đến lượt O" : "Đến lượt X";
                            ResetTimer();
                            UpdateButtonStates();
                        }));
                    }
                    else if (cmd == "GAMEOVER")
                    {
                        int winnerSide = int.Parse(parts[1]);

                        // Nhận tọa độ và vẽ đường kẻ
                        if (parts.Length > 2)
                        {
                            int sx = int.Parse(parts[2]);
                            int sy = int.Parse(parts[3]);
                            int ex = int.Parse(parts[4]);
                            int ey = int.Parse(parts[5]);

                            winStart = new Point(sx, sy);
                            winEnd = new Point(ex, ey);
                        }

                        this.Invoke(new Action(() => {
                            tmCoolDown.Stop();

                            isGameOverEffect = true; // Bật cờ hiệu ứng

                            if (winnerSide == mySide)
                            {
                                endResultText = "🏆 CHIẾN THẮNG 🏆";
                                endResultColor = Color.Gold;
                                PlaySound(Properties.Resources.tada); // Âm thanh thắng (nếu có)
                            }
                            else
                            {
                                endResultText = "💀 THẤT BẠI 💀";
                                endResultColor = Color.Red;
                                // PlaySound(Properties.Resources.sad);
                            }

                            // Kiểm tra kỹ trước khi Start
                            if (tmAnimation == null)
                            {
                                // Nếu chưa có thì tạo mới luôn cho chắc ăn
                                tmAnimation = new System.Windows.Forms.Timer { Interval = 500 };
                                tmAnimation.Tick += (sender, args) => {
                                    blinkToggle = !blinkToggle;
                                    pnlChessBoard.Invalidate();
                                };
                            }
                            tmAnimation.Start();
                            pnlChessBoard.Invalidate(); // Vẽ ngay lập tức
                                                        // -------------------------
                        }));
                    }
                    else if (cmd == "NEW_GAME_ASK")
                    {
                        this.Invoke(new Action(() => {
                            // Hiện bảng hỏi
                            DialogResult dr = MessageBox.Show(
                                "Đối thủ muốn bắt đầu ván mới. Bạn có đồng ý không?",
                                "Yêu cầu Ván mới",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question
                            );

                            if (dr == DialogResult.Yes)
                            {
                                SendCommand("NEW_GAME_ACCEPT");
                            }
                            else
                            {
                                SendCommand("NEW_GAME_REJECT");
                            }
                        }));
                    }
                    else if (cmd == "NEW_GAME_REJECT")
                    {
                        this.Invoke(new Action(() => {
                            MessageBox.Show("Đối thủ đã từ chối yêu cầu ván mới!", "Thông báo");

                            // Mở lại nút để bấm tiếp
                            btnNewGame.Enabled = true;
                            btnNewGame.Text = "VÁN MỚI";
                        }));
                    }
                    else if (cmd == "NEW_GAME")
                    {
                        this.Invoke(new Action(() => {
                            // 1. [QUAN TRỌNG] Tắt hiệu ứng Chiến thắng/Thua cuộc (Nếu có)
                            isGameOverEffect = false;       // Tắt cờ hiệu ứng
                            if (tmAnimation != null) tmAnimation.Stop(); // Dừng nhấp nháy

                            // 2. Reset tọa độ đường gạch đỏ
                            winStart = new Point(-1, -1);
                            winEnd = new Point(-1, -1);

                            // 3. Xóa dữ liệu bàn cờ (Reset về 0)
                            if (banCoAo != null)
                            {
                                // Nếu kích thước bàn cờ vẫn đúng -> Chỉ cần xóa trắng dữ liệu (Nhanh hơn)
                                if (banCoAo.GetLength(0) == boardSize)
                                {
                                    Array.Clear(banCoAo, 0, banCoAo.Length);
                                }
                                else
                                {
                                    // Nếu kích thước thay đổi (ít gặp) -> Tạo mới
                                    banCoAo = new int[boardSize, boardSize];
                                }
                            }
                            else
                            {
                                // Nếu chưa có thì tạo mới
                                banCoAo = new int[boardSize, boardSize];
                            }

                            // 4. Vẽ lại giao diện sạch sẽ
                            pnlChessBoard.Invalidate();

                            // 5. Reset Đồng hồ & Thông báo
                            ResetTimer();

                            if (CheDoChoi == "SPECTATOR")
                            {
                                lblLuotDi.Text = "Hai người chơi đang bắt đầu ván mới...";
                            }
                            else
                            {
                                // Mặc định ván mới thì X (Side 1) luôn đi trước
                                if (mySide == 1)
                                {
                                    lblLuotDi.Text = "Đến lượt BẠN (X)"; // Có chữ "BẠN" và "X" -> Client cho phép đánh
                                    PlaySound(Properties.Resources.ding); // Ting ting nhắc nhở
                                }
                                else
                                {
                                    lblLuotDi.Text = "Đến lượt Đối thủ (X)";
                                }
                            }

                            // (Tùy chọn) Phát nhạc nền lại nếu cần
                            PlayGameMusic();
                            if (btnNewGame != null)
                            {
                                btnNewGame.Enabled = true;
                                btnNewGame.Text = "VÁN MỚI";
                            }
                        }));
                    }
                    // --- XỬ LÝ HÒA ---
                    else if (cmd == "DRAW_ASK")
                    {
                        this.Invoke(new Action(() => {
                            // Hiện bảng hỏi Ý kiến
                            DialogResult dr = MessageBox.Show(
                                "Đối thủ nhận thấy ván cờ bế tắc và muốn xin HÒA.\nBạn có đồng ý không?",
                                "Lời mời Hòa",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question
                            );

                            if (dr == DialogResult.Yes) SendCommand("DRAW_ACCEPT");
                            else SendCommand("DRAW_REJECT");
                        }));
                    }
                    else if (cmd == "DRAW_REFUSED")
                    {
                        this.Invoke(new Action(() => {
                            MessageBox.Show("Đối thủ không đồng ý hòa! Hãy chiến đấu tiếp.", "Từ chối");
                            // Mở lại nút
                            btnXinHoa.Enabled = true;
                            btnXinHoa.Text = "Xin hòa";
                        }));
                    }
                    else if (cmd == "GAME_DRAW")
                    {
                        this.Invoke(new Action(() => {
                            // 1. Dừng đồng hồ
                            tmCoolDown.Stop();
                            if (tmAnimation != null)
                            {
                                tmAnimation.Stop();
                            } // Nếu đang có hiệu ứng gì đó

                            // 2. Hiện hiệu ứng HÒA (Tận dụng cái hiệu ứng Victory xịn xò lúc nãy)
                            isGameOverEffect = true;
                            endResultText = "🤝 HÒA NHAU 🤝";
                            endResultColor = Color.LightSkyBlue; // Màu xanh nhẹ nhàng tình cảm

                            // Kiểm tra kỹ trước khi Start
                            if (tmAnimation == null)
                            {
                                // Nếu chưa có thì tạo mới luôn cho chắc ăn
                                tmAnimation = new System.Windows.Forms.Timer { Interval = 500 };
                                tmAnimation.Tick += (sender, args) => {
                                    blinkToggle = !blinkToggle;
                                    pnlChessBoard.Invalidate();
                                };
                            }
                            tmAnimation.Start();
                            pnlChessBoard.Invalidate();

                            // 3. Thông báo text
                            lblLuotDi.Text = "Kết quả: Bất phân thắng bại!";

                            // 4. Mở nút Ván mới để chơi lại
                            if (btnNewGame != null) btnNewGame.Enabled = true;

                            // 5. Khóa nút Xin hòa (Hòa rồi thì xin gì nữa)
                            btnXinHoa.Enabled = false;
                        }));
                    }
                    // --- XỬ LÝ HÒA (BỔ SUNG) ---
                    else if (cmd == "DRAW_REQUEST")
                    {
                        this.Invoke(new Action(() => {
                            if (MessageBox.Show("Đối thủ cầu hòa. Bạn đồng ý không?", "Cầu hòa", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                SendCommand("DRAW_ACCEPT");
                        }));
                    }
                    else if (cmd == "GAME_DRAW")
                    {
                        this.Invoke(new Action(() => {
                            tmCoolDown.Stop();
                            MessageBox.Show("Ván đấu kết thúc với tỉ số HÒA! 🤝");
                        }));
                    }
                    // --- XỬ LÝ UNDO (ĐI LẠI) ---
                    else if (cmd == "UNDO_ASK")
                    {
                        string question = parts[1];
                        this.Invoke(new Action(() => {
                            if (MessageBox.Show(question, "Xin đi lại", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                SendCommand("UNDO_ACCEPT");
                            else
                                SendCommand("UNDO_REJECT");
                        }));
                    }
                    else if (cmd == "UNDO")
                    {
                        int ux = int.Parse(parts[1]);
                        int uy = int.Parse(parts[2]);
                        this.Invoke(new Action(() => {
                            if (banCoAo != null) banCoAo[ux, uy] = 0;
                            pnlChessBoard.Invalidate(); // Vẽ lại toàn bộ cho sạch
                            ResetTimer();
                            PlaySound(Properties.Resources.Click);
                        }));
                    }
                    // --- XỬ LÝ CHAT & EMOTE ---
                    else if (cmd == "CHAT")
                    {
                        string senderName = parts[1];
                        string content = parts[2];
                        this.Invoke(new Action(() => {
                            PlaySound(Properties.Resources.ding);

                            // 1. Tên người gửi
                            rtbChatLog.SelectionColor = Color.Cyan; // Màu xanh cho tên nổi bật
                            rtbChatLog.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
                            rtbChatLog.AppendText($"[{senderName}]: ");

                            // 2. Nội dung (Dùng font Emoji)
                            rtbChatLog.SelectionColor = Color.White;
                            rtbChatLog.SelectionFont = new Font("Segoe UI Emoji", 12); // <-- QUAN TRỌNG
                            rtbChatLog.AppendText($"{content}\n");

                            rtbChatLog.ScrollToCaret();
                        }));
                    }
                    else if (cmd == "EMOTE")
                    {
                        int side = int.Parse(parts[1]);
                        string symbol = parts[2];
                        this.Invoke(new Action(() => {
                            Label targetLabel = (side == 1) ? ((mySide == 1) ? lblEmoteP1 : lblEmoteP2)
                                                            : ((mySide == 2) ? lblEmoteP1 : lblEmoteP2);
                            if (targetLabel != null)
                            {
                                // Sét Font Emoji kích thước lớn (40)
                                targetLabel.Font = new Font("Segoe UI Emoji", 40); // <-- QUAN TRỌNG
                                targetLabel.Text = symbol;
                                targetLabel.Visible = true;
                                targetLabel.BringToFront();
                            }
                            PlaySound(Properties.Resources.ding);
                            tmHideEmote.Stop();
                            tmHideEmote.Start();
                        }));
                    }
                    else if (cmd == "LOBBY_CHAT")
                    {
                        string sender = parts[1];
                        string content = parts[2];
                        this.Invoke(new Action(() => {
                            // 1. Tên người gửi: Dùng Font thường, màu Vàng
                            rtbLobbyChat.SelectionColor = Color.Yellow;
                            rtbLobbyChat.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
                            rtbLobbyChat.AppendText($"[{sender}]: ");

                            // 2. Nội dung tin nhắn: Dùng Font "Segoe UI Emoji" để hiện màu
                            rtbLobbyChat.SelectionColor = Color.White;
                            rtbLobbyChat.SelectionFont = new Font("Segoe UI Emoji", 12); // <-- QUAN TRỌNG
                            rtbLobbyChat.AppendText($"{content}\n");

                            rtbLobbyChat.ScrollToCaret();
                            if (pnlLobby.Visible) PlaySound(Properties.Resources.ding);
                        }));
                    }
                    // --- CÁC LOGIC KHÁC ---
                    else if (cmd == "GAME_START")
                    {
                        // 1. Parse dữ liệu từ Server gửi về
                        mySide = int.Parse(parts[1]);
                        boardSize = int.Parse(parts[2]);
                        string opName = (parts.Length > 3) ? parts[3] : "Đối thủ";
                        int opAvatarID = (parts.Length > 4) ? int.Parse(parts[4]) : 0;

                        this.Invoke(new Action(() => {
                            // --- [BẮT ĐẦU ĐOẠN SỬA LỖI (Dọn dẹp hiệu ứng cũ)] ---
                            // 1. Tắt cờ hiệu ứng và dừng đèn nhấp nháy
                            isGameOverEffect = false;
                            if (tmAnimation != null) tmAnimation.Stop();

                            // 2. Reset các nút chức năng về trạng thái ban đầu
                            // (Tránh trường hợp ván trước xin hòa/ván mới xong nút bị disable)
                            if (btnXinHoa != null)
                            {
                                btnXinHoa.Enabled = true;
                                btnXinHoa.Text = "Xin hòa";
                            }
                            if (btnNewGame != null)
                            {
                                btnNewGame.Enabled = true;
                                btnNewGame.Text = "VÁN MỚI";
                            }

                            // 3. Reset toạ độ đường gạch đỏ (nếu có)
                            winStart = new Point(-1, -1);
                            winEnd = new Point(-1, -1);
                            // ----------------------------------------------------

                            // --- [CODE CŨ CỦA EM GIỮ NGUYÊN BÊN DƯỚI] ---
                            ShowScreen(pnlGame);
                            ResetTimer();
                            PlayGameMusic();

                            // Kiểm tra null để tránh lỗi nếu chưa tạo nút ToggleMusic
                            if (btnToggleMusic != null) btnToggleMusic.Text = isMusicOn ? "🔊" : "🔇";

                            banCoAo = new int[boardSize, boardSize];
                            pnlChessBoard.Invalidate(); // Vẽ lại bàn cờ mới tinh
                            UpdateButtonStates();

                            // Setup Avatar và Label hiển thị
                            if (mySide == 1)
                            {
                                // Mình là X (P1)
                                // Lưu ý: Avatar của mình (GetUserAvatar) vs Avatar đối thủ
                                // Nếu em dùng hàm GetAvatarByID(0) cho mình thì OK, giữ nguyên logic của em
                                if (ptbAvatar1 != null) ptbAvatar1.Image = GetAvatarByID(0);
                                if (ptbAvatar2 != null) ptbAvatar2.Image = GetAvatarByID(opAvatarID);
                                if (lblLuotDi != null) lblLuotDi.Text = $"Bạn (X) vs {opName} (O)";
                            }
                            else
                            {
                                // Mình là O (P2)
                                if (ptbAvatar1 != null) ptbAvatar1.Image = GetAvatarByID(opAvatarID);
                                if (ptbAvatar2 != null) ptbAvatar2.Image = GetAvatarByID(0);
                                if (lblLuotDi != null) lblLuotDi.Text = $"Bạn (O) vs {opName} (X)";
                            }

                            if (ptbAvatar1 != null) ptbAvatar1.SizeMode = PictureBoxSizeMode.StretchImage;
                            if (ptbAvatar2 != null) ptbAvatar2.SizeMode = PictureBoxSizeMode.StretchImage;
                        }));
                    }
                    else if (cmd == "RECONNECT_GAME")
                    {
                        try
                        {
                            // 1. Lấy dữ liệu từ gói tin
                            mySide = int.Parse(parts[1]);
                            boardSize = int.Parse(parts[2]);
                            string opName = parts[3];
                            int opAvatar = int.Parse(parts[4]);
                            string historyData = (parts.Length > 5) ? parts[5] : "";

                            this.Invoke(new Action(() => {
                                // --- [QUAN TRỌNG] Đặt lại chế độ chơi là Online ---
                                CheDoChoi = "LAN";
                                ShowScreen(pnlGame);

                                lblWelcome.Text = $"Xin chào, {currentUsername}!";

                                // 2. Setup thông tin đối thủ và Avatar
                                if (mySide == 1) // Mình là X (Host)
                                {
                                    ptbAvatar1.Image = GetAvatarByID(0); // Ảnh mình
                                    ptbAvatar2.Image = GetAvatarByID(opAvatar); // Ảnh đối thủ
                                }
                                else // Mình là O (Guest)
                                {
                                    ptbAvatar1.Image = GetAvatarByID(opAvatar); // Ảnh đối thủ
                                    ptbAvatar2.Image = GetAvatarByID(0); // Ảnh mình
                                }
                                ptbAvatar1.SizeMode = PictureBoxSizeMode.StretchImage;
                                ptbAvatar2.SizeMode = PictureBoxSizeMode.StretchImage;

                                // 3. VẼ LẠI BÀN CỜ
                                if (banCoAo == null || banCoAo.GetLength(0) != boardSize)
                                    banCoAo = new int[boardSize, boardSize];
                                else
                                    Array.Clear(banCoAo, 0, banCoAo.Length);

                                pnlChessBoard.Refresh();
                                Application.DoEvents(); // Đợi 1 chút cho UI xóa xong hẳn rồi mới vẽ đè lên

                                // Biến đếm lượt để xác định lượt tiếp theo: 1 là X, 2 là O
                                int turnCounter = 1;

                                if (!string.IsNullOrEmpty(historyData))
                                {
                                    string[] moves = historyData.Split(';');

                                    Graphics g = pnlChessBoard.CreateGraphics();
                                    g.SmoothingMode = SmoothingMode.AntiAlias;
                                    GetBoardMetrics(out float cs, out float ox, out float oy);

                                    if (cs > 0 && !float.IsNaN(cs))
                                    {
                                        g.TranslateTransform(ox, oy);

                                        foreach (string move in moves)
                                        {
                                            if (string.IsNullOrWhiteSpace(move)) continue;
                                            string[] coord = move.Split('|');
                                            if (coord.Length < 2) continue;

                                            int x = int.Parse(coord[0]);
                                            int y = int.Parse(coord[1]);

                                            if (x >= 0 && x < boardSize && y >= 0 && y < boardSize)
                                            {
                                                // Cập nhật mảng logic
                                                banCoAo[x, y] = turnCounter;

                                                // Vẽ lại quân cờ
                                                if (turnCounter == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                                                else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);
                                            }

                                            // Đổi lượt cho nước tiếp theo
                                            turnCounter = (turnCounter == 1) ? 2 : 1;
                                        }
                                    }
                                }

                                // --- [XÁC ĐỊNH LƯỢT ĐI TIẾP THEO] ---
                                // So sánh lượt hiện tại (turnCounter) với phe của mình (mySide)
                                if (turnCounter == mySide)
                                {
                                    // Nếu trùng nhau => Đến lượt mình đánh
                                    lblLuotDi.Text = $"Đến lượt BẠN ({(mySide == 1 ? "X" : "O")})";
                                    PlaySound(Properties.Resources.ding); // Kêu ding để báo hiệu
                                }
                                else
                                {
                                    // Nếu khác nhau => Đến lượt đối thủ
                                    lblLuotDi.Text = $"Đến lượt {opName} ({(mySide == 1 ? "O" : "X")})";
                                }
                                // ----------------------------------------------

                                ResetTimer();
                                UpdateButtonStates(); // Cập nhật trạng thái nút Undo/Xin hòa
                                MessageBox.Show("Đã kết nối lại trận đấu! Mời bạn tiếp tục.", "Thông báo");
                            }));
                        }
                        catch (Exception ex)
                        {
                            this.Invoke(new Action(() => MessageBox.Show($"Lỗi Reconnect: {ex.Message}")));
                        }
                    }
                    else if (cmd == "WATCH_SUCCESS")
                    {
                        try
                        {
                            // Format: WATCH_SUCCESS | BoardSize | P1Name | P2Name | HistoryString
                            int serverBoardSize = int.Parse(parts[1]);

                            // Cập nhật lại kích thước bàn cờ nếu cần
                            boardSize = (serverBoardSize > 0) ? serverBoardSize : 15;

                            string p1 = parts[2];
                            string p2 = parts[3];

                            // --- [FIX LỖI TẠI ĐÂY] ---
                            // Vì HistoryString chứa ký tự '|' nên bị hàm Split cắt vụn ra.
                            // Ta cần nối chúng lại từ phần tử thứ 4 trở đi.
                            string history = "";
                            if (parts.Length > 4)
                            {
                                // Nối lại tất cả các phần còn lại bằng dấu '|'
                                history = string.Join("|", parts, 4, parts.Length - 4);
                            }
                            // -------------------------

                            this.Invoke(new Action(() => {
                                CheDoChoi = "SPECTATOR";
                                ShowScreen(pnlGame);

                                lblWelcome.Text = "CHẾ ĐỘ KHÁN GIẢ";
                                lblLuotDi.Text = $"Đang xem: {p1} (X) vs {p2} (O)";

                                // Ẩn nút chức năng của người chơi
                                btnNewGame.Visible = false;
                                btnUndo.Visible = false;
                                btnXinHoa.Visible = false;
                                btnXinThua.Visible = false;
                                // Hiện nút rời phòng
                                btnLeaveGame.Visible = true;

                                // Khởi tạo bàn cờ ảo
                                if (banCoAo == null || banCoAo.GetLength(0) != boardSize)
                                    banCoAo = new int[boardSize, boardSize];
                                else
                                    Array.Clear(banCoAo, 0, banCoAo.Length);

                                // Vẽ lại ngay lập tức
                                pnlChessBoard.Invalidate();
                                pnlChessBoard.Update();

                                // Diễn lại lịch sử nước đi
                                if (!string.IsNullOrEmpty(history))
                                {
                                    string[] moves = history.Split(';');
                                    int turn = 1; // 1=X, 2=O

                                    Graphics g = pnlChessBoard.CreateGraphics();
                                    g.SmoothingMode = SmoothingMode.AntiAlias;
                                    GetBoardMetrics(out float cs, out float ox, out float oy);

                                    if (cs > 0 && !float.IsNaN(cs))
                                    {
                                        g.TranslateTransform(ox, oy);
                                        foreach (string move in moves)
                                        {
                                            if (string.IsNullOrWhiteSpace(move)) continue;
                                            string[] coord = move.Split('|');
                                            if (coord.Length < 2) continue;

                                            if (int.TryParse(coord[0], out int x) && int.TryParse(coord[1], out int y))
                                            {
                                                if (x >= 0 && x < boardSize && y >= 0 && y < boardSize)
                                                {
                                                    banCoAo[x, y] = turn;
                                                    if (turn == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                                                    else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);
                                                }
                                                turn = (turn == 1) ? 2 : 1;
                                            }
                                        }
                                    }
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            this.Invoke(new Action(() => MessageBox.Show("Lỗi hiển thị phòng xem: " + ex.Message)));
                        }
                    }
                    else if (cmd == "REPLAY_DATA")
                    {
                        string data = parts[1];
                        this.Invoke(new Action(async () => { // Dùng async để có thể delay
                            CheDoChoi = "REPLAY";
                            ShowScreen(pnlGame);
                            lblWelcome.Text = "ĐANG XEM LẠI...";

                            // Ẩn các nút chức năng khi replay
                            btnNewGame.Visible = false;
                            btnUndo.Visible = false;
                            btnXinHoa.Visible = false;
                            btnXinThua.Visible = false;
                            btnLeaveGame.Visible = true;

                            // Reset bàn cờ
                            if (banCoAo == null || banCoAo.GetLength(0) != boardSize)
                                banCoAo = new int[boardSize, boardSize];
                            else
                                Array.Clear(banCoAo, 0, banCoAo.Length);

                            pnlChessBoard.Refresh();

                            string[] moves = data.Split(';');
                            Graphics g = pnlChessBoard.CreateGraphics();
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            GetBoardMetrics(out float cs, out float ox, out float oy);

                            if (cs > 0 && !float.IsNaN(cs))
                            {
                                g.TranslateTransform(ox, oy);

                                foreach (string move in moves)
                                {
                                    if (string.IsNullOrWhiteSpace(move)) continue;
                                    string[] info = move.Split(','); // Format: x,y,side

                                    if (info.Length < 3) continue;

                                    int x = int.Parse(info[0]);
                                    int y = int.Parse(info[1]);
                                    int side = int.Parse(info[2]);

                                    if (x >= 0 && x < boardSize && y >= 0 && y < boardSize)
                                    {
                                        // Cập nhật và vẽ
                                        banCoAo[x, y] = side;
                                        if (side == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                                        else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);
                                        PlaySound(Properties.Resources.Click);
                                    }

                                    // Nghỉ 500ms để tạo hiệu ứng như đang đánh thật
                                    await Task.Delay(500);
                                }
                            }
                            MessageBox.Show("Đã kết thúc Replay!");
                        }));
                    }
                    else if (cmd == "HISTORY_DATA")
                    {
                        string data = string.Join("|", parts, 1, parts.Length - 1);
                        this.Invoke(new Action(() => ShowHistoryDialog(data)));
                    }
                    
                    else if (cmd == "LEADERBOARD_DATA")
                    {
                        // Lấy toàn bộ chuỗi dữ liệu (từ phần tử thứ 1 trở đi)
                        string data = string.Join("|", parts, 1, parts.Length - 1);
                        this.Invoke(new Action(() => ShowLeaderboardDialog(data)));
                    }
                    else if (cmd == "ADMIN_DATA")
                    {
                        // Server gửi về dạng: ADMIN_DATA|Dòng 1|Dòng 2|Dòng 3...
                        // Ta cần bỏ chữ "ADMIN_DATA" đi, và nối các phần còn lại bằng xuống dòng
                        string content = "";
                        if (parts.Length > 1)
                        {
                            // Thay thế dấu gạch đứng | bằng xuống dòng để hiển thị danh sách đẹp như ảnh
                            content = string.Join("\n", parts, 1, parts.Length - 1);
                        }

                        this.Invoke(new Action(() => {
                            if (rtbAdminData != null)
                            {
                                rtbAdminData.Text = content; // Hiển thị lên bảng đen
                            }
                        }));
                    }
                    else if (cmd == "FORCE_DISCONNECT")
                    {
                        this.Invoke(new Action(() => {
                            // 1. Đóng kết nối mạng ngay lập tức
                            if (client != null) client.Close();

                            // 2. Reset các trạng thái đăng nhập
                            isLoggedIn = false;
                            isAdmin = false;
                            currentUsername = "";

                            // 3. Dừng các timer game (nếu đang chạy)
                            if (tmCoolDown != null) tmCoolDown.Stop();

                            // 4. ĐÁ VĂNG VỀ MÀN HÌNH ĐĂNG NHẬP
                            ShowScreen(pnlLogin);

                            // 5. Xóa trắng các ô nhập liệu để người dùng phải nhập lại từ đầu
                            txtUserLogin.Clear();
                            txtPassLogin.Clear();

                            // (Tùy chọn) Hiện thông báo nếu chưa có thông báo trước đó
                            // MessageBox.Show("Kết nối đã bị ngắt!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }));
                    }
                    // ...
                    else if (cmd == "MESSAGE")
                    {
                        string m = string.Join("|", parts, 1, parts.Length - 1);
                        this.Invoke(new Action(() => {
                            PlaySound(Properties.Resources.ding);
                            if (pnlLobby.Visible) MessageBox.Show(m);
                            else { rtbChatLog.AppendText($"[Hệ thống] {m}\n"); rtbChatLog.ScrollToCaret(); }
                        }));
                    }
                    // --- BỔ SUNG: XỬ LÝ KHI ĐỐI THỦ THOÁT ---
                    else if (cmd == "OPPONENT_LEFT")
                    {
                        this.Invoke(new Action(() => {
                            MessageBox.Show("Đối thủ đã thoát trận!", "Thông báo");
                            tmCoolDown.Stop();
                            // Tùy chọn: Tự động out ra lobby hoặc cho user bấm nút
                        }));
                    }
                    else if (cmd == "WAITING_MATCH")
                    {
                        this.Invoke(new Action(() => MessageBox.Show("Đang tìm đối thủ...", "Thông báo")));
                    }
                    else if (cmd == "ROOM_CREATED")
                    {
                        string id = parts[1];
                        this.Invoke(new Action(() => MessageBox.Show($"Mã phòng: {id}", "Tạo phòng")));
                    }
                }
            }
            catch
            {
                this.Invoke(new Action(() => {
                    if (!IsConnected()) { MessageBox.Show("Mất kết nối server!"); ShowScreen(pnlLogin); }
                }));
            }
        }

        // --- CÁC HÀM HỖ TRỢ CHUNG ---
        private void ConnectAndLogin(string type)
        {
            try
            {
                client = new TcpClient(); client.Connect(txtServerIP.Text.Trim() == "" ? "127.0.0.1" : txtServerIP.Text, 8080); NetworkStream st = client.GetStream(); writer = new StreamWriter(st) { AutoFlush = true }; reader = new StreamReader(st); new Thread(ReceiveMessage) { IsBackground = true }.Start();
                if (type == "GUEST") SendCommand($"QUICK_CONNECT|{txtNickNameGuest.Text}"); else SendCommand($"LOGIN|{txtUserLogin.Text}|{CalculateMD5Hash(txtPassLogin.Text)}");
            }
            catch (Exception ex) { MessageBox.Show("Lỗi kết nối: " + ex.Message); }
        }
        // Hàm kích hoạt chế độ chống giật cho Panel
        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                return;
            System.Reflection.PropertyInfo aProp = typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            aProp.SetValue(c, true, null);
        }
        private void RegisterAccount() { /* Logic đăng ký cũ */ if (txtRegPassword.Text == txtRegConfirmPassword.Text) { try { client = new TcpClient(); client.Connect("127.0.0.1", 8080); NetworkStream st = client.GetStream(); writer = new StreamWriter(st) { AutoFlush = true }; reader = new StreamReader(st); new Thread(ReceiveMessage) { IsBackground = true }.Start(); SendCommand($"REGISTER|{txtRegUsername.Text}|{CalculateMD5Hash(txtRegPassword.Text)}|{txtRegDisplayName.Text}|{txtRegEmail.Text}"); } catch { MessageBox.Show("Lỗi kết nối Server"); } } else MessageBox.Show("Mật khẩu không khớp"); }
        private void SendCommand(string c) { try { if (IsConnected()) writer.WriteLine(c); } catch { } }
        private bool IsConnected() { return client != null && client.Connected; }
        private void PerformLogout() { if (MessageBox.Show("Đăng xuất?", "Hỏi", MessageBoxButtons.YesNo) == DialogResult.Yes) { isLoggedIn = false; if (IsConnected()) client.Close(); ShowScreen(pnlLogin); } }
        private void GetBoardMetrics(out float cs, out float ox, out float oy) { float w = pnlChessBoard.Width; float h = pnlChessBoard.Height; float min = Math.Min(w, h); cs = (min - 20) / boardSize; float size = cs * boardSize; ox = (w - size) / 2; oy = (h - size) / 2; }
        private void DrawChess(Graphics g, Image i, string t, Brush b, int x, int y, float s) { float sc = boardSize == 10 ? 0.8f : 0.75f; float sz = s * sc; float off = (s - sz) / 2; if (i != null) g.DrawImage(i, x * s + off, y * s + off, sz, sz); else g.DrawString(t, new Font("Arial", s * 0.5f, FontStyle.Bold), b, x * s + s * 0.2f, y * s + s * 0.1f); }
        private void SendChatMessage() { if (!string.IsNullOrWhiteSpace(txtMessage.Text)) { SendCommand($"CHAT|{txtMessage.Text}"); txtMessage.Clear(); } }
        private void ResetTimer() { thoiGianConLai = tongThoiGian; lblDongHo.Text = "03:00"; prcbCoolDown.Value = tongThoiGian; tmCoolDown.Start(); }

        // Hàm tạo bảng chọn Emote trượt (Sliding)
        // parentControl: Panel cha để chứa bảng emote
        // location: Vị trí hiển thị
        // onEmoteClick: Hàm xử lý khi chọn icon (gửi lệnh gì)
        // Hàm tạo bảng Emote có nút Next/Prev
        private Panel CreatePagedEmotePanel(Point location, Action<string> onEmoteClick)
        {
            int iconSize = 45;      // Kích thước 1 icon
            int margin = 5;         // Khoảng cách giữa các icon
            int visibleCount = 3;   // Số icon hiển thị cùng lúc (Giảm xuống 4 để chừa chỗ cho nút mũi tên)
            int buttonWidth = 30;   // Kích thước nút mũi tên

            // 1. Tính toán kích thước
            int viewWidth = (iconSize + margin) * visibleCount; // Chiều rộng vùng hiển thị icon
            int totalWidth = (iconSize + margin) * listEmojis.Length; // Chiều rộng thực tế chứa hết icon
            int panelHeight = iconSize + 15;
            int totalPanelWidth = viewWidth + (buttonWidth * 2) + 10; // Tổng chiều rộng cả bảng (bao gồm nút)

            // 2. Panel Chính (Chứa tất cả)
            Panel pnlMain = new Panel
            {
                Size = new Size(totalPanelWidth, panelHeight),
                Location = location,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false // Mặc định ẩn
            };

            // 3. Nút Previous (<)
            Button btnPrev = new Button
            {
                Text = "◀",
                Size = new Size(buttonWidth, iconSize),
                Location = new Point(0, 5),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false // Ban đầu ở trang 1 nên không quay lại được
            };
            btnPrev.FlatAppearance.BorderSize = 0;

            // 4. Nút Next (>)
            Button btnNext = new Button
            {
                Text = "▶",
                Size = new Size(buttonWidth, iconSize),
                Location = new Point(buttonWidth + viewWidth + 5, 5), // Nằm bên phải cùng
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;

            // 5. Panel Mask (Khung nhìn - Cắt bớt phần thừa)
            Panel pnlMask = new Panel
            {
                Size = new Size(viewWidth, panelHeight),
                Location = new Point(buttonWidth + 5, 0), // Nằm giữa 2 nút
                BackColor = Color.Transparent
            };

            // 6. Panel Content (Chứa icon - Trượt bên trong Mask)
            Panel pnlContent = new Panel
            {
                Size = new Size(totalWidth, panelHeight),
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            };

            // --- LOGIC THÊM ICON VÀO PANEL CONTENT ---
            for (int i = 0; i < listEmojis.Length; i++)
            {
                string symbol = listEmojis[i];
                Button btn = new Button
                {
                    Text = symbol,
                    Size = new Size(iconSize, iconSize),
                    Location = new Point(margin + i * (iconSize + margin), 5),
                    Font = new Font("Segoe UI Emoji", 15),
                    FlatStyle = FlatStyle.Flat,
                    Tag = symbol,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => onEmoteClick(symbol); // Sự kiện chọn icon
                pnlContent.Controls.Add(btn);
            }

            // --- LOGIC XỬ LÝ SỰ KIỆN NÚT BẤM (QUAN TRỌNG) ---
            int scrollStep = (iconSize + margin) * visibleCount; // Mỗi lần bấm trượt 1 trang (4 icon)

            btnNext.Click += (s, e) =>
            {
                // Trượt sang trái (giá trị Left giảm)
                if (pnlContent.Left - scrollStep > -(totalWidth))
                {
                    pnlContent.Left -= scrollStep;
                }
                else // Nếu trượt quá thì về cuối hẳn
                {
                    pnlContent.Left = -(totalWidth - viewWidth);
                }

                // Cập nhật trạng thái nút
                btnPrev.Enabled = true;
                if (pnlContent.Left <= -(totalWidth - viewWidth)) btnNext.Enabled = false;
            };

            btnPrev.Click += (s, e) =>
            {
                // Trượt sang phải (giá trị Left tăng)
                if (pnlContent.Left + scrollStep < 0)
                {
                    pnlContent.Left += scrollStep;
                }
                else // Về đầu
                {
                    pnlContent.Left = 0;
                }

                // Cập nhật trạng thái nút
                btnNext.Enabled = true;
                if (pnlContent.Left == 0) btnPrev.Enabled = false;
            };

            // Ghép các thành phần vào nhau
            pnlMask.Controls.Add(pnlContent);
            pnlMain.Controls.Add(btnPrev);
            pnlMain.Controls.Add(pnlMask);
            pnlMain.Controls.Add(btnNext);

            return pnlMain;
        }

        // --- CÁC HÀM XỬ LÝ SỰ KIỆN CHUỘT (DRAG LOGIC) ---

        private void Emote_MouseDown(object sender, MouseEventArgs e)
        {
            isDraggingEmote = true;
            isClickAction = true; // Giả định là click, nếu di chuyển nhiều sẽ thành false
            lastMouseX = Cursor.Position.X; // Dùng toạ độ màn hình để chính xác nhất
        }

        private void Emote_MouseMove(object sender, MouseEventArgs e, Panel pnlInner, int outerWidth)
        {
            if (isDraggingEmote)
            {
                int currentX = Cursor.Position.X;
                int deltaX = currentX - lastMouseX;

                // Nếu di chuyển quá ngưỡng dragThreshold -> Đây là hành động Kéo, không phải Click
                if (Math.Abs(deltaX) > dragThreshold || !isClickAction)
                {
                    isClickAction = false; // Hủy click

                    // Di chuyển Panel con
                    int newLeft = pnlInner.Left + deltaX;

                    // Giới hạn biên (Không cho kéo quá trái hoặc quá phải)
                    int minLeft = outerWidth - pnlInner.Width; // Điểm giới hạn bên trái
                    if (newLeft > 0) newLeft = 0; // Không được kéo quá mép phải
                    if (newLeft < minLeft) newLeft = minLeft; // Không được kéo quá mép trái

                    pnlInner.Left = newLeft;
                    lastMouseX = currentX;
                }
            }
        }

        private void Emote_MouseUp(object sender, MouseEventArgs e, Action<string> onClickAction)
        {
            isDraggingEmote = false;

            // Nếu sau khi nhả chuột mà vẫn được tính là Click -> Thực hiện chọn Icon
            if (isClickAction)
            {
                Button btn = sender as Button;
                if (btn != null)
                {
                    string symbol = btn.Tag.ToString();
                    onClickAction(symbol); // Gọi hàm gửi icon
                }
            }
        }
        // Thêm tham số 'sound' kiểu Stream để nhận file từ Resources
        private void PlaySound(System.IO.Stream sound)
        {
            if (!isMusicOn) return; // Nếu tắt tiếng thì không kêu
            try
            {
                if (sound != null)
                {
                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(sound);
                    player.Play();
                }
            }
            catch { }
        }
        private void UpdateButtonStates() { /* Cập nhật trạng thái nút Undo/NewGame */ }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) { try { if (IsConnected()) { SendCommand("DISCONNECT"); client.Close(); } } catch { } }
        private void Form1_Resize(object sender, EventArgs e) { if (pnlChessBoard != null && pnlChessBoard.Visible) pnlChessBoard.Invalidate(); CenterLoginControls(); CenterLobbyControls(); CenterRegisterControls(); }
        private void ProcessSelectedGameMode() { pnlBoardSize.Visible = false; switch (selectedGameMode) { case 1: SendCommand($"FIND_MATCH|{boardSize}"); break; case 2: SendCommand($"CREATE_PRIVATE|{boardSize}"); break; case 3: SendCommand($"JOIN_PRIVATE|{tempRoomID}|{boardSize}"); break; } }
        private string CalculateMD5Hash(string input) { using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) { byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input); byte[] hashBytes = md5.ComputeHash(inputBytes); System.Text.StringBuilder sb = new System.Text.StringBuilder(); for (int i = 0; i < hashBytes.Length; i++) { sb.Append(hashBytes[i].ToString("X2")); } return sb.ToString(); } }

        // --- HÀM TẠO CONTROL (UI HELPER) ---
        private TextBox CreateInput(string p, int x, int y, int w) { TextBox t = new TextBox { Text = p, Location = new Point(x, y), Width = w, Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(70, 70, 80), ForeColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle }; t.Enter += (s, e) => { if (t.Text == p) { t.Text = ""; t.ForeColor = Color.White; } }; t.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(t.Text)) { t.Text = p; t.ForeColor = Color.Gray; } }; return t; }
        private Button CreateButton(string t, int x, int y, int w, Color c) { Button b = new Button { Text = t, Location = new Point(x, y), Width = w, Height = 40, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand }; return b; }
        private Button CreateBoardSizeButton(string t, string d, int x, int y, Color c) { return CreateButton($"{t}\n{d}", x, y, 400, c); }
        private GroupBox CreateGroupBox(string t, int w, int h) { return new GroupBox { Text = t, Size = new Size(w, h), ForeColor = Color.Gold, Font = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Point) }; }
        
        // Hàm hỗ trợ hiển thị hộp thoại nhập Email (Copy cái này dán vào cuối class Form1)
        private string ShowInputDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, Width = 350 };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340 };
            Button confirmation = new Button() { Text = "Gửi yêu cầu", Left = 230, Width = 130, Top = 90, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
        private Image GetAvatarByID(int id)
        {
            // Ánh xạ ID sang ảnh trong Resources
            // Bạn cần đảm bảo đã thêm ảnh avatar0, avatar1... vào Resources
            switch (id)
            {
                case 0: return Properties.Resources.avatar0; // Thay bằng tên file ảnh của bạn
                case 1: return Properties.Resources.avatar1;
                case 2: return Properties.Resources.avatar2;
                case 3: return Properties.Resources.avatar3;
                default: return Properties.Resources.avatar0; // Mặc định
            }
        }
        // Hàm hiển thị danh sách lịch sử đấu để chọn xem lại
        private void ShowHistoryDialog(string dataString)
        {
            Form historyForm = new Form()
            {
                Text = "Lịch sử đấu (Click đúp để xem lại)",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent
            };

            ListBox lstHistory = new ListBox() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12) };

            // Tách chuỗi dữ liệu server gửi về (ngăn cách bằng $)
            string[] matches = dataString.Split('$');
            foreach (var match in matches)
            {
                if (string.IsNullOrWhiteSpace(match)) continue;
                // Format từ server: MatchID | Thời gian | Đối thủ | Kết quả
                string[] info = match.Split('|');
                if (info.Length >= 4)
                {
                    // Hiển thị đẹp mắt hơn trên ListBox
                    // Lưu MatchID vào object item (hoặc parse từ chuỗi khi click)
                    lstHistory.Items.Add($"[{info[1]}] vs {info[2]} -> {info[3]}  (ID: {info[0]})");
                }
            }

            // Xử lý sự kiện chọn trận để xem lại
            lstHistory.DoubleClick += (s, e) => {
                if (lstHistory.SelectedItem != null)
                {
                    string selectedText = lstHistory.SelectedItem.ToString();
                    // Lấy ID từ chuỗi hiển thị "... (ID: 123)"
                    int startIndex = selectedText.LastIndexOf("ID: ") + 4;
                    int endIndex = selectedText.LastIndexOf(")");
                    string matchId = selectedText.Substring(startIndex, endIndex - startIndex);

                    // Gửi yêu cầu xem lại
                    SendCommand($"GET_REPLAY|{matchId}");
                    historyForm.Close();
                }
            };

            historyForm.Controls.Add(lstHistory);
            historyForm.ShowDialog();
        }
        private void ShowSettingsDialog()
        {
            Form f = new Form { Text = "Cài đặt", Size = new Size(300, 200), StartPosition = FormStartPosition.CenterParent };

            // Checkbox Bật/Tắt âm thanh
            CheckBox chkSound = new CheckBox { Text = "Bật Âm Thanh (SFX)", Location = new Point(50, 50), Checked = isMusicOn, AutoSize = true };

            // Nút Lưu
            Button btnSave = new Button { Text = "Lưu", Location = new Point(100, 100), DialogResult = DialogResult.OK };

            btnSave.Click += (s, e) => {
                isMusicOn = chkSound.Checked;
                if (isMusicOn) PlayMusic(); else StopMusic();
                f.Close();
            };

            f.Controls.Add(chkSound);
            f.Controls.Add(btnSave);
            f.ShowDialog();
        }

        // Hàm quản lý nhạc (Đơn giản)
        private void PlayMusic()
        {
            // Nếu bạn đã thêm file bgm.wav vào Resources:
            if (bgmPlayer == null) bgmPlayer = new System.Media.SoundPlayer(Properties.Resources.ding);
            bgmPlayer.PlayLooping();
        }

        private void StopMusic()
        {
            if (bgmPlayer != null) bgmPlayer.Stop();
        }
        private void PlayGameMusic()
        {
            try
            {
                // Chỉ phát nếu người dùng cho phép (trong cài đặt)
                if (!isMusicOn) return;

                // Đường dẫn file nhạc (nằm cùng thư mục với file exe)
                string musicPath = System.IO.Path.Combine(Application.StartupPath, "bgm.mp3");

                if (bgmPlayer == null)
                {
                    // Properties.Resources.bgm chính là file look_up_mastered.wav em đã add
                    bgmPlayer = new System.Media.SoundPlayer(Properties.Resources.bgm);
                }

                bgmPlayer.PlayLooping(); // Phát lặp lại liên tục
            }
            catch
            {
                // Nếu lỗi thì thôi, không làm phiền người chơi
            }
        }

        private void StopGameMusic()
        {
            musicPlayer.controls.stop();
        }
        private void ShowLeaderboardDialog(string dataString)
        {
            // 1. Setup Form
            Form lbForm = new Form()
            {
                Text = "🏆 BẢNG XẾP HẠNG (Top 10)",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                // Dùng màu nền tối cho đồng bộ
                BackColor = Color.FromArgb(30, 30, 40)
            };

            // 2. Setup ListView (Bảng hiển thị)
            ListView lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details, // Bắt buộc phải là Details để hiện cột
                GridLines = true,
                FullRowSelect = true,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White
            };

            // 3. Add Columns
            lv.Columns.Add("Hạng", 50, HorizontalAlignment.Center);
            lv.Columns.Add("Tên Hiển Thị", 180);
            lv.Columns.Add("Điểm Elo", 80, HorizontalAlignment.Center);
            lv.Columns.Add("Thắng", 70, HorizontalAlignment.Center);
            lv.Columns.Add("Thua", 70, HorizontalAlignment.Center);
            lv.Columns.Add("Hòa", 70, HorizontalAlignment.Center);

            // 4. Populate Data
            if (dataString != "Chưa có dữ liệu xếp hạng.")
            {
                string[] players = dataString.Split('$');
                foreach (var player in players)
                {
                    if (string.IsNullOrWhiteSpace(player)) continue;
                    // Format: Rank | DisplayName | Diem | Thang | Thua | Hoa
                    string[] info = player.Split('|');
                    if (info.Length == 6)
                    {
                        ListViewItem item = new ListViewItem(info[0]); // Rank
                        item.SubItems.Add(info[1]); // DisplayName
                        item.SubItems.Add(info[2]); // Diem
                        item.SubItems.Add(info[3]); // Thang
                        item.SubItems.Add(info[4]); // Thua
                        item.SubItems.Add(info[5]); // Hoa

                        // Highlight Top 3 cho đẹp
                        if (int.TryParse(info[0], out int rank) && rank <= 3)
                        {
                            item.BackColor = Color.Gold;
                            item.ForeColor = Color.Black;
                        }

                        lv.Items.Add(item);
                    }
                }
                lbForm.Controls.Add(lv);
            }
            else
            {
                Label lblNoData = new Label { Text = dataString, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Red, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
                lbForm.Controls.Add(lblNoData);
            }

            lbForm.ShowDialog();
        }

        // Lớp RichTextBox tùy chỉnh để hỗ trợ Emoji màu
        public class ExRichTextBox : RichTextBox
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            static extern IntPtr LoadLibrary(string lpFileName);

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams createParams = base.CreateParams;
                    try
                    {
                        // Nạp thư viện MsftEdit.dll (Chứa RichEdit 5.0 hỗ trợ Emoji màu)
                        LoadLibrary("MsftEdit.dll");
                        createParams.ClassName = "RichEdit50W";
                    }
                    catch { } // Nếu lỗi thì kệ, dùng mặc định
                    return createParams;
                }
            }
        }
    }
}