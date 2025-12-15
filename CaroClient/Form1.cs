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
        private void ShowScreen(Panel p) { pnlLogin.Visible = false; pnlRegister.Visible = false; pnlLobby.Visible = false; pnlGame.Visible = false; pnlAdmin.Visible = false; pnlBoardSize.Visible = false; p.Visible = true; }

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
            btnGuestPlayAI.Click += (s, e) => StartPvEGame();

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
            lblWelcome = new Label { Text = "Xin chào!", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.Yellow, AutoSize = true };
            pnlLobby.Controls.Add(lblWelcome);

            lblStatus = new Label { Text = "Chế độ: Guest", Font = new Font("Segoe UI", 11), ForeColor = Color.LightGreen, AutoSize = true };
            this.Load += (s, e) => { lblStatus.Text = isLoggedIn ? $"User: {currentUsername}" : "Guest"; CenterLobbyControls(); };
            pnlLobby.Controls.Add(lblStatus);

            btnOpenAdmin = CreateButton("QUẢN LÝ ADMIN", 0, 0, 150, Color.Red);
            btnOpenAdmin.Visible = false;
            btnOpenAdmin.Click += (s, e) => { ShowScreen(pnlAdmin); SendCommand("ADMIN_LIST"); };
            pnlLobby.Controls.Add(btnOpenAdmin);

            btnLeaderboard = CreateButton("🏆 BẢNG XẾP HẠNG", 0, 0, 300, Color.Gold);
            btnLeaderboard.ForeColor = Color.Black;
            btnLeaderboard.Click += (s, e) => SendCommand("GET_LEADERBOARD");
            pnlLobby.Controls.Add(btnLeaderboard);

            btnHistory = CreateButton("📜 LỊCH SỬ ĐẤU", 0, 0, 300, Color.LightSlateGray);
            btnHistory.Click += (s, e) => {
                if (!isLoggedIn || isGuest) MessageBox.Show("Khách không có lịch sử đấu.", "Thông báo");
                else SendCommand("GET_HISTORY");
            };
            pnlLobby.Controls.Add(btnHistory);

            btnFindMatch = CreateButton("🔍 TÌM TRẬN NGẪU NHIÊN", 0, 0, 300, Color.Orange);
            btnFindMatch.Height = 60;
            btnFindMatch.Click += (s, e) => { selectedGameMode = 1; ShowScreen(pnlBoardSize); };
            pnlLobby.Controls.Add(btnFindMatch);

            btnCreatePrivate = CreateButton("🏠 TẠO PHÒNG RIÊNG", 0, 0, 300, Color.Teal);
            btnCreatePrivate.Click += (s, e) => { selectedGameMode = 2; ShowScreen(pnlBoardSize); };
            pnlLobby.Controls.Add(btnCreatePrivate);

            pnlJoinGroup = new Panel { Size = new Size(300, 120), BackColor = Color.Transparent };
            lblJoinInstruction = new Label { Text = "Nhập ID phòng:", Location = new Point(0, 0), ForeColor = Color.White, AutoSize = true };
            txtRoomIDJoin = CreateInput("", 0, 25, 180);
            btnJoinPrivate = CreateButton("VÀO NGAY", 190, 23, 110, Color.SteelBlue);
            Button btnWatch = CreateButton("XEM (👁️)", 190, 60, 110, Color.DarkSlateBlue);
            btnWatch.Height = 29;
            btnWatch.Click += (s, e) => {
                if (!string.IsNullOrWhiteSpace(txtRoomIDJoin.Text))
                    SendCommand($"WATCH_ROOM|{txtRoomIDJoin.Text}");
            };
            pnlJoinGroup.Controls.Add(btnWatch);
            btnJoinPrivate.Height = 29;
           
           
            // Nhớ tăng chiều cao pnlJoinGroup lên nếu bị che: pnlJoinGroup.Size = new Size(300, 100);
            btnJoinPrivate.Click += (s, e) => { if (!string.IsNullOrWhiteSpace(txtRoomIDJoin.Text)) { selectedGameMode = 3; tempRoomID = txtRoomIDJoin.Text; ShowScreen(pnlBoardSize); } };
            pnlJoinGroup.Controls.Add(lblJoinInstruction); pnlJoinGroup.Controls.Add(txtRoomIDJoin); pnlJoinGroup.Controls.Add(btnJoinPrivate);
            pnlLobby.Controls.Add(pnlJoinGroup);

            btnLogout = CreateButton("ĐĂNG XUẤT", 0, 0, 300, Color.DarkGray);
            btnLogout.Click += (s, e) => PerformLogout();
            pnlLobby.Controls.Add(btnLogout);

            CenterLobbyControls();
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

        private void CenterLobbyControls()
        {
            if (pnlLobby != null && pnlLobby.Visible)
            {
                int cx = pnlLobby.Width / 2;
                int y = 50; int gap = 15;
                if (lblWelcome != null) { lblWelcome.Location = new Point(cx - (lblWelcome.Width / 2), y); y += lblWelcome.Height + 5; }
                if (lblStatus != null) { lblStatus.Location = new Point(cx - (lblStatus.Width / 2), y); y += lblStatus.Height + 30; }
                if (btnOpenAdmin != null) btnOpenAdmin.Location = new Point(pnlLobby.Width - 170, 20);

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

        private void SetupGameScreen()
        {
            Panel left = new Panel { Dock = DockStyle.Left, Width = 250, BackColor = Color.FromArgb(40, 40, 50) }; pnlGame.Controls.Add(left);
            Panel right = new Panel { Dock = DockStyle.Right, Width = 250, BackColor = Color.FromArgb(40, 40, 50) }; pnlGame.Controls.Add(right);
            pnlChessBoard = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
            pnlChessBoard.Paint += PnlChessBoard_Paint; pnlChessBoard.MouseClick += PnlChessBoard_MouseClick; pnlGame.Controls.Add(pnlChessBoard);

            rtbChatLog = new RichTextBox { Location = new Point(10, 10), Width = 230, Height = 500, BackColor = Color.FromArgb(60, 60, 70), ForeColor = Color.White, BorderStyle = BorderStyle.None, ReadOnly = true };
            txtMessage = CreateInput("", 10, 520, 160); txtMessage.KeyPress += (s, e) => { if (e.KeyChar == 13) { SendChatMessage(); e.Handled = true; } };
            btnSend = CreateButton("Gửi", 180, 518, 60, Color.DodgerBlue); btnSend.Height = 28; btnSend.Click += (s, e) => SendChatMessage();
            btnLeaveGame = CreateButton("⬅ Rời Phòng", 10, 600, 230, Color.Gray); btnLeaveGame.Click += (s, e) => { SendCommand("LEAVE_GAME"); ShowScreen(pnlLobby); mySide = 0; tmCoolDown.Stop(); };
            left.Controls.Add(rtbChatLog); left.Controls.Add(txtMessage); left.Controls.Add(btnSend); left.Controls.Add(btnLeaveGame);

            lblDongHo = new Label { Text = "03:00", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.Cyan, Size = new Size(250, 50), Location = new Point(0, 20), TextAlign = ContentAlignment.MiddleCenter };
            prcbCoolDown = new ProgressBar { Location = new Point(25, 80), Width = 200, Height = 10, Maximum = tongThoiGian, Value = tongThoiGian };
            lblLuotDi = new Label { Text = "Lượt đấu...", Location = new Point(25, 100), AutoSize = true, ForeColor = Color.Yellow, Font = new Font("Segoe UI", 12) };
            btnNewGame = CreateButton("VÁN MỚI", 25, 150, 200, Color.Teal); btnNewGame.Click += (s, e) => SendCommand("NEW_GAME");
            btnUndo = CreateButton("XIN ĐI LẠI", 25, 200, 200, Color.Goldenrod); btnUndo.Click += (s, e) => {
                // Chỉ cho xin đi lại khi đang chơi Online (Lan/Internet)
                if (CheDoChoi == "VS_MAY")
                {
                    // Nếu đấu với máy thì cho Undo luôn (logic cũ của bạn)
                    // (Bạn có thể giữ logic Undo cũ cho máy ở đây nếu muốn)
                    return;
                }

                SendCommand("UNDO_REQUEST"); // Gửi yêu cầu xin phép
            };
            btnXinHoa = CreateButton("CẦU HÒA", 25, 250, 200, Color.Gray); btnXinHoa.Click += (s, e) => SendCommand("DRAW_REQUEST");
            btnXinThua = CreateButton("ĐẦU HÀNG", 25, 300, 200, Color.Maroon); btnXinThua.Click += (s, e) => { if (MessageBox.Show("Đầu hàng?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes) SendCommand("SURRENDER"); };
            ptbAvatar1 = new PictureBox { Size = new Size(60, 60), Location = new Point(25, 350), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.StretchImage };
            ptbAvatar2 = new PictureBox { Size = new Size(60, 60), Location = new Point(165, 350), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.StretchImage };
            right.Controls.Add(lblDongHo); right.Controls.Add(prcbCoolDown); right.Controls.Add(lblLuotDi); right.Controls.Add(btnNewGame); right.Controls.Add(btnUndo); right.Controls.Add(btnXinHoa); right.Controls.Add(btnXinThua); right.Controls.Add(ptbAvatar1); right.Controls.Add(ptbAvatar2);
        }

        // --- HÀM LOGIC GAME ---
        private void StartPvEGame()
        {
            CheDoChoi = "VS_MAY"; boardSize = 15; mySide = 1; banCoAo = new int[boardSize, boardSize]; playerMoveCount = 0;
            currentUsername = (string.IsNullOrWhiteSpace(txtNickNameGuest.Text) || txtNickNameGuest.Text == "Nhập biệt danh...") ? "Người Chơi" : txtNickNameGuest.Text;
            ShowScreen(pnlGame); pnlChessBoard.Invalidate(); lblLuotDi.Text = "Bạn đi trước (X)"; lblWelcome.Text = $"Xin chào, {currentUsername}!";
            btnXinThua.Visible = false; btnXinHoa.Visible = false; btnUndo.Enabled = true; ResetTimer();
        }

        private void InitializeGameLogic()
        {
            CheckForIllegalCrossThreadCalls = false;
            tmCoolDown = new System.Windows.Forms.Timer { Interval = 1000 };
            tmCoolDown.Tick += (s, e) => {
                if (thoiGianConLai > 0) { thoiGianConLai--; lblDongHo.Text = TimeSpan.FromSeconds(thoiGianConLai).ToString(@"mm\:ss"); prcbCoolDown.Value = Math.Min(thoiGianConLai, prcbCoolDown.Maximum); }
                else { tmCoolDown.Stop(); if (CheDoChoi == "LAN") SendCommand("TIME_OUT"); else MessageBox.Show("Hết giờ! Bạn thua."); }
            };
           
            try
            {
                // Lấy ảnh trực tiếp từ Resources đã nhúng
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
            // 1. Vẽ nền và kẻ lưới (Giữ nguyên code cũ)
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.WhiteSmoke);
            GetBoardMetrics(out float cs, out float ox, out float oy);
            g.TranslateTransform(ox, oy);

            using (Pen pen = new Pen(Color.Gray, 1))
            {
                for (int i = 0; i <= boardSize; i++)
                {
                    g.DrawLine(pen, i * cs, 0, i * cs, cs * boardSize);
                    g.DrawLine(pen, 0, i * cs, cs * boardSize, i * cs);
                }
            }

            // 2. [SỬA LẠI] VẼ LẠI CÁC QUÂN CỜ (Áp dụng cho cả Online và Offline)
            // Chỉ cần kiểm tra banCoAo khác null là vẽ
            if (banCoAo != null)
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
        }

        private void PnlChessBoard_MouseClick(object sender, MouseEventArgs e)
        {
            GetBoardMetrics(out float cs, out float ox, out float oy);
            float cx = e.X - ox; float cy = e.Y - oy;
            if (cx < 0 || cy < 0) return;
            int x = (int)(cx / cs); int y = (int)(cy / cs);
            if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return;
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
            int winLen = (boardSize < 10) ? 3 : 5; int[] dx = { 1, 0, 1, 1 }; int[] dy = { 0, 1, 1, -1 };
            for (int d = 0; d < 4; d++)
            {
                int c = 1;
                for (int i = 1; i <= winLen; i++) { int nx = x + dx[d] * i; int ny = y + dy[d] * i; if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize || board[nx, ny] != side) break; c++; }
                for (int i = 1; i <= winLen; i++) { int nx = x - dx[d] * i; int ny = y - dy[d] * i; if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize || board[nx, ny] != side) break; c++; }
                if (c >= winLen) return true;
            }
            return false;
        }

        private void ReceiveMessage()
        {
            try
            {
                while (client.Connected)
                {
                    string msg = reader.ReadLine(); if (msg == null) break;
                    string[] parts = msg.Split('|'); string cmd = parts[0];
                    if (cmd == "LOGIN_SUCCESS") { string n = parts[1]; string r = parts[2]; isAdmin = (r == "1"); isLoggedIn = true; currentUsername = n; this.Invoke(new Action(() => { lblWelcome.Text = $"Xin chào, {n}!"; btnOpenAdmin.Visible = isAdmin; ShowScreen(pnlLobby); txtUserLogin.Clear(); txtPassLogin.Clear(); txtNickNameGuest.Clear(); })); }
                    else if (cmd == "LOGIN_FAIL" || cmd == "REGISTER_FAIL") { string r = parts[1]; this.Invoke(new Action(() => MessageBox.Show(r, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error))); }
                    else if (cmd == "REGISTER_SUCCESS") { this.Invoke(new Action(() => { MessageBox.Show("Đăng ký thành công!", "Thông báo"); ShowScreen(pnlLogin); })); }
                    else if (cmd == "RESET_SUCCESS")
                    {
                        string content = parts[1]; // Đã đổi tên biến
                        this.Invoke(new Action(() => MessageBox.Show(content, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                    }
                    else if (cmd == "RESET_FAIL")
                    {
                        string content = parts[1]; // Đã đổi tên biến
                        this.Invoke(new Action(() => MessageBox.Show(content, "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                    // ... (Các lệnh cũ)

                    else if (cmd == "HISTORY_DATA")
                    {
                        // Nhận dữ liệu lịch sử và hiện bảng chọn
                        string data = (parts.Length > 1) ? parts[1] : "";
                        this.Invoke(new Action(() => ShowHistoryDialog(data)));
                    }
                    else if (cmd == "REPLAY_DATA")
                    {
                        // Nhận dữ liệu các nước đi để chiếu lại (Replay)
                        string data = parts[1];
                        this.Invoke(new Action(async () => {
                            CheDoChoi = "REPLAY"; // Chế độ xem lại
                            ShowScreen(pnlGame);
                            lblWelcome.Text = "ĐANG XEM LẠI TRẬN ĐẤU...";
                            lblLuotDi.Text = "Replay Mode";

                            // Ẩn nút chức năng
                            btnUndo.Visible = false;
                            btnNewGame.Visible = false;
                            btnXinHoa.Visible = false;
                            btnXinThua.Visible = false;

                            // Reset bàn cờ
                            if (banCoAo == null || banCoAo.GetLength(0) != boardSize)
                                banCoAo = new int[boardSize, boardSize];
                            else
                                Array.Clear(banCoAo, 0, banCoAo.Length);

                            pnlChessBoard.Refresh();

                            // Bắt đầu diễn lại từng nước đi
                            string[] moves = data.Split(';');
                            Graphics g = pnlChessBoard.CreateGraphics();
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            GetBoardMetrics(out float cs, out float ox, out float oy);
                            g.TranslateTransform(ox, oy);

                            foreach (string move in moves)
                            {
                                if (string.IsNullOrWhiteSpace(move)) continue;
                                string[] info = move.Split(','); // Format: x,y,side
                                if (info.Length < 3) continue;

                                int x = int.Parse(info[0]);
                                int y = int.Parse(info[1]);
                                int side = int.Parse(info[2]);

                                // Cập nhật và vẽ
                                banCoAo[x, y] = side;
                                if (side == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                                else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);

                                PlaySound(Properties.Resources.Click);

                                // Delay 800ms để tạo cảm giác như đang đánh thật
                                await Task.Delay(800);
                            }
                            MessageBox.Show("Đã kết thúc Replay!");
                            // Hiện lại các nút khi xong (nếu cần) hoặc user tự bấm Rời phòng
                        }));
                    }
                    // ... (Các lệnh khác)
                    // ... Trong vòng lặp while của ReceiveMessage ...
                    else if (cmd == "WATCH_SUCCESS")
                    {
                        // Format: WATCH_SUCCESS | BoardSize | P1Name | P2Name | History
                        boardSize = int.Parse(parts[1]);
                        string p1 = parts[2];
                        string p2 = parts[3];
                        string history = (parts.Length > 4) ? parts[4] : "";

                        this.Invoke(new Action(() => {
                            CheDoChoi = "SPECTATOR"; // Chế độ khán giả
                            ShowScreen(pnlGame);

                            lblLuotDi.Text = $"Đang xem: {p1} vs {p2}";
                            lblWelcome.Text = "CHẾ ĐỘ KHÁN GIẢ";

                            // Ẩn các nút điều khiển
                            btnUndo.Visible = false;
                            btnXinHoa.Visible = false;
                            btnXinThua.Visible = false;
                            btnNewGame.Visible = false;

                            // Vẽ lại bàn cờ (Copy logic vẽ từ Reconnect qua)
                            if (banCoAo == null || banCoAo.GetLength(0) != boardSize)
                                banCoAo = new int[boardSize, boardSize];
                            else
                                Array.Clear(banCoAo, 0, banCoAo.Length);

                            pnlChessBoard.Refresh();
                            Application.DoEvents();

                            // Vẽ các nước đã đánh
                            if (!string.IsNullOrEmpty(history))
                            {
                                string[] moves = history.Split(';');
                                int turn = 1;
                                Graphics g = pnlChessBoard.CreateGraphics();
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                GetBoardMetrics(out float cs, out float ox, out float oy);
                                g.TranslateTransform(ox, oy);

                                foreach (string move in moves)
                                {
                                    if (string.IsNullOrWhiteSpace(move)) continue;
                                    string[] coord = move.Split('|');
                                    int x = int.Parse(coord[0]);
                                    int y = int.Parse(coord[1]);
                                    banCoAo[x, y] = turn;
                                    if (turn == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                                    else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);
                                    turn = (turn == 1) ? 2 : 1;
                                }
                            }
                        }));
                    }
                    else if (cmd == "REPLAY_DATA")
                    {
                        string data = parts[1];
                        this.Invoke(new Action(async () => { // Dùng async để có thể delay
                            CheDoChoi = "REPLAY";
                            ShowScreen(pnlGame);
                            lblWelcome.Text = "ĐANG XEM LẠI...";

                            // Reset bàn cờ
                            if (banCoAo == null) banCoAo = new int[boardSize, boardSize];
                            Array.Clear(banCoAo, 0, banCoAo.Length);
                            pnlChessBoard.Refresh();

                            string[] moves = data.Split(';');
                            Graphics g = pnlChessBoard.CreateGraphics();
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            GetBoardMetrics(out float cs, out float ox, out float oy);
                            g.TranslateTransform(ox, oy);

                            foreach (string move in moves)
                            {
                                if (string.IsNullOrWhiteSpace(move)) continue;
                                string[] info = move.Split(','); // x,y,side
                                int x = int.Parse(info[0]);
                                int y = int.Parse(info[1]);
                                int side = int.Parse(info[2]);

                                // Cập nhật và vẽ
                                banCoAo[x, y] = side;
                                if (side == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                                else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);
                                PlaySound(Properties.Resources.Click);

                                // Nghỉ 500ms để tạo hiệu ứng đánh cờ
                                await Task.Delay(500);
                            }
                            MessageBox.Show("Đã kết thúc Replay!");
                        }));
                    }
                    else if (cmd == "UNDO_ASK")
                    {
                        // Đây là Client B nhận được câu hỏi
                        string question = parts[1];
                        DialogResult dr = MessageBox.Show(question, "Yêu cầu đi lại", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (dr == DialogResult.Yes)
                        {
                            SendCommand("UNDO_ACCEPT"); // Đồng ý
                        }
                        else
                        {
                            SendCommand("UNDO_REJECT"); // Từ chối
                        }
                    }

                    else if (cmd == "UNDO")
                    {
                        // Server bảo xóa quân cờ tại vị trí X, Y (Logic vẽ lại bàn cờ)
                        int ux = int.Parse(parts[1]);
                        int uy = int.Parse(parts[2]);

                        this.Invoke(new Action(() => {
                            // 1. Xóa quân cờ trên bàn cờ ảo (nếu có dùng mảng client)
                            if (banCoAo != null) banCoAo[ux, uy] = 0;

                            // 2. Vẽ lại ô đó thành ô trống (màu nền)
                            Graphics g = pnlChessBoard.CreateGraphics();
                            GetBoardMetrics(out float cs, out float ox, out float oy);
                            g.TranslateTransform(ox, oy);

                            // Vẽ đè một hình chữ nhật màu nền lên ô đó để xóa quân cờ
                            // Lưu ý: Màu nền phải trùng với màu pnlChessBoard (WhiteSmoke)
                            using (Brush eraser = new SolidBrush(Color.WhiteSmoke))
                            {
                                // Tính toán vị trí chính xác để xóa (cộng trừ 1px để không xóa mất đường kẻ)
                                g.FillRectangle(eraser, ux * cs + 1, uy * cs + 1, cs - 2, cs - 2);
                            }

                            // 3. (Tùy chọn) Vẽ lại đường kẻ lưới cho ô đó nếu bị xóa mất
                            using (Pen pen = new Pen(Color.Gray, 1))
                            {
                                g.DrawRectangle(pen, ux * cs, uy * cs, cs, cs);
                            }

                            // 4. Cập nhật lượt đi và Reset timer
                            // Nếu vừa xóa nước của X -> Giờ đến lượt X đi lại
                            string nguoiVuaBiXoa = (lblLuotDi.Text.Contains("X")) ? "O" : "X";
                            // Logic hiển thị text này có thể cần chỉnh tùy theo game của bạn đang hiển thị gì
                            // Tạm thời chỉ cần reset timer
                            ResetTimer();
                            PlaySound(Properties.Resources.Click); // Âm thanh báo hiệu
                        }));
                    }
                    else if (cmd == "WAITING_MATCH") { this.Invoke(new Action(() => MessageBox.Show("Đang tìm đối thủ...", "Thông báo"))); }
                    else if (cmd == "ROOM_CREATED") { string id = parts[1]; this.Invoke(new Action(() => MessageBox.Show($"Mã phòng: {id}", "Tạo phòng"))); }
                    else if (cmd == "GAME_START")
                    {
                        // Format: GAME_START | Side | BoardSize | OpponentName | OpponentAvatar
                        mySide = int.Parse(parts[1]);
                        boardSize = int.Parse(parts[2]);
                        string opName = (parts.Length > 3) ? parts[3] : "Đối thủ";
                        int opAvatarID = (parts.Length > 4) ? int.Parse(parts[4]) : 0;

                        // Xác định avatar của BẢN THÂN (Logic tạm: Lấy random hoặc mặc định vì Client chưa có chỗ chọn)
                        // Để đẹp, sau này bạn nên lưu AvatarID của mình khi Login thành công.
                        // Tạm thời mình lấy mặc định là 0 cho mình.
                        int myAvatarID = 0;

                        this.Invoke(new Action(() => {
                            ShowScreen(pnlGame);
                            ResetTimer();
                            // Khởi tạo mảng lưu bàn cờ cho chế độ Online
                            banCoAo = new int[boardSize, boardSize];
                            pnlChessBoard.Invalidate();
                            UpdateButtonStates();

                            // Cập nhật Avatar và Tên
                            if (mySide == 1) // Mình là Host (Trái)
                            {
                                ptbAvatar1.Image = GetAvatarByID(myAvatarID); // Mình
                                ptbAvatar2.Image = GetAvatarByID(opAvatarID); // Đối thủ
                                lblLuotDi.Text = $"Bạn (X) vs {opName} (O)";
                            }
                            else // Mình là Guest (Phải)
                            {
                                ptbAvatar1.Image = GetAvatarByID(opAvatarID); // Đối thủ (Host)
                                ptbAvatar2.Image = GetAvatarByID(myAvatarID); // Mình
                                lblLuotDi.Text = $"Bạn (O) vs {opName} (X)";
                            }

                            // Căn chỉnh ảnh cho đẹp (Stretch)
                            ptbAvatar1.SizeMode = PictureBoxSizeMode.StretchImage;
                            ptbAvatar2.SizeMode = PictureBoxSizeMode.StretchImage;
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
                                // Biến đếm lượt: 1 là X, 2 là O
                                int turnCounter = 1;

                                if (!string.IsNullOrEmpty(historyData))
                                {
                                    string[] moves = historyData.Split(';');

                                    Graphics g = pnlChessBoard.CreateGraphics();
                                    g.SmoothingMode = SmoothingMode.AntiAlias;
                                    GetBoardMetrics(out float cs, out float ox, out float oy);
                                    g.TranslateTransform(ox, oy);

                                    foreach (string move in moves)
                                    {
                                        if (string.IsNullOrWhiteSpace(move)) continue;
                                        string[] coord = move.Split('|');
                                        if (coord.Length < 2) continue;

                                        int x = int.Parse(coord[0]);
                                        int y = int.Parse(coord[1]);

                                        // Cập nhật mảng logic
                                        banCoAo[x, y] = turnCounter;

                                        // Vẽ lại quân cờ
                                        if (turnCounter == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                                        else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);

                                        // Đổi lượt cho nước tiếp theo
                                        turnCounter = (turnCounter == 1) ? 2 : 1;
                                    }
                                }

                                // --- [ĐÂY LÀ PHẦN SỬA LỖI QUAN TRỌNG NHẤT] ---
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
                    else if (cmd == "MOVE")
                    {
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        int s = int.Parse(parts[3]);

                        this.Invoke(new Action(() => {
                            // 1. Luôn kêu Click khi có người đặt quân cờ
                            PlaySound(Properties.Resources.Click);

                            // 2. Nếu người vừa đánh (s) KHÔNG PHẢI LÀ MÌNH -> Tức là đến lượt mình -> Kêu Ding
                            if (s != mySide)
                            {
                                // Dùng Thread để kêu đè lên tiếng click (hoặc kêu sau 1 chút)
                                Task.Delay(200).ContinueWith(t => PlaySound(Properties.Resources.ding));
                            }
                            // Lưu nước đi vào mảng client để Paint có thể vẽ lại khi cần
                            if (banCoAo != null && x >= 0 && x < boardSize && y >= 0 && y < boardSize)
                            {
                                banCoAo[x, y] = s;
                            }
                            // ... (Giữ nguyên đoạn vẽ bàn cờ phía sau) ...
                            Graphics g = pnlChessBoard.CreateGraphics();
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            GetBoardMetrics(out float cs, out float ox, out float oy);
                            g.TranslateTransform(ox, oy);
                            if (s == 1) DrawChess(g, imgX, "X", Brushes.Red, x, y, cs);
                            else DrawChess(g, imgO, "O", Brushes.Blue, x, y, cs);
                            lblLuotDi.Text = (s == 1) ? "Đến lượt O" : "Đến lượt X";
                            ResetTimer();
                            UpdateButtonStates();
                        }));
                    }
                    // --- Thay thế dòng 'else if (cmd == "MESSAGE")' cũ bằng đoạn này ---
                    else if (cmd == "MESSAGE")
                    {
                        string m = string.Join("|", parts, 1, parts.Length - 1);
                        this.Invoke(new Action(() => {
                            PlaySound(Properties.Resources.ding); // <--- Kêu Ding khi có thông báo
                            if (pnlLobby.Visible) MessageBox.Show(m);
                            else { rtbChatLog.AppendText($"[Hệ thống] {m}\n"); rtbChatLog.ScrollToCaret(); }
                        }));
                    }
                    else if (cmd == "CHAT") // <--- Bổ sung thêm phần Chat
                    {
                        string senderName = parts[1];
                        string content = parts[2];
                        this.Invoke(new Action(() => {
                            PlaySound(Properties.Resources.ding); // <--- Kêu Ding khi có ai đó chat
                            rtbChatLog.AppendText($"[{senderName}]: {content}\n");
                            rtbChatLog.ScrollToCaret();
                        }));
                    }
                    else if (cmd == "GAMEOVER")
                    {
                        int winnerSide = int.Parse(parts[1]);
                        this.Invoke(new Action(() => {
                            PlaySound(Properties.Resources.tada); // <--- Kêu Ding báo kết quả

                            if (winnerSide == mySide) MessageBox.Show("CHÚC MỪNG! BẠN ĐÃ CHIẾN THẮNG!");
                            else MessageBox.Show("Đáng tiếc, bạn đã thua!");

                            // Reset game hoặc hiện nút chơi lại
                        }));
                    }
                }
            }
            catch { this.Invoke(new Action(() => { if (!IsConnected()) { MessageBox.Show("Mất kết nối!"); ShowScreen(pnlLogin); } })); }
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
        private void RegisterAccount() { /* Logic đăng ký cũ */ if (txtRegPassword.Text == txtRegConfirmPassword.Text) { try { client = new TcpClient(); client.Connect("127.0.0.1", 8080); NetworkStream st = client.GetStream(); writer = new StreamWriter(st) { AutoFlush = true }; reader = new StreamReader(st); new Thread(ReceiveMessage) { IsBackground = true }.Start(); SendCommand($"REGISTER|{txtRegUsername.Text}|{CalculateMD5Hash(txtRegPassword.Text)}|{txtRegDisplayName.Text}|{txtRegEmail.Text}"); } catch { MessageBox.Show("Lỗi kết nối Server"); } } else MessageBox.Show("Mật khẩu không khớp"); }
        private void SendCommand(string c) { try { if (IsConnected()) writer.WriteLine(c); } catch { } }
        private bool IsConnected() { return client != null && client.Connected; }
        private void PerformLogout() { if (MessageBox.Show("Đăng xuất?", "Hỏi", MessageBoxButtons.YesNo) == DialogResult.Yes) { isLoggedIn = false; if (IsConnected()) client.Close(); ShowScreen(pnlLogin); } }
        private void GetBoardMetrics(out float cs, out float ox, out float oy) { float w = pnlChessBoard.Width; float h = pnlChessBoard.Height; float min = Math.Min(w, h); cs = (min - 20) / boardSize; float size = cs * boardSize; ox = (w - size) / 2; oy = (h - size) / 2; }
        private void DrawChess(Graphics g, Image i, string t, Brush b, int x, int y, float s) { float sc = boardSize == 10 ? 0.8f : 0.75f; float sz = s * sc; float off = (s - sz) / 2; if (i != null) g.DrawImage(i, x * s + off, y * s + off, sz, sz); else g.DrawString(t, new Font("Arial", s * 0.5f, FontStyle.Bold), b, x * s + s * 0.2f, y * s + s * 0.1f); }
        private void SendChatMessage() { if (!string.IsNullOrWhiteSpace(txtMessage.Text)) { SendCommand($"CHAT|{txtMessage.Text}"); txtMessage.Clear(); } }
        private void ResetTimer() { thoiGianConLai = tongThoiGian; lblDongHo.Text = "03:00"; prcbCoolDown.Value = tongThoiGian; tmCoolDown.Start(); }
        // Hàm này không cần tham số nữa vì ta chỉ dùng 1 âm thanh click
        // Thêm tham số 'sound' kiểu Stream để nhận file từ Resources
        private void PlaySound(System.IO.Stream sound)
        {
            try
            {
                // Nếu âm thanh tồn tại thì mới chơi
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
    }
}