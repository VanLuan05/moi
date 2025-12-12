using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks; // Chỉ cần khai báo 1 lần
using System.Windows.Forms;
using CaroShared;
using System.Media;

namespace CaroClient
{
    public partial class Form1 : Form
    {
        // --- KHAI BÁO BIẾN ---
        private TcpClient client;
        private NetworkStream stream;
        private StreamWriter writer;
        private StreamReader reader;
        // --- CẤU HÌNH CỜ CHỚP ---
        // Tổng thời gian cho mỗi người (3 phút = 180 giây)
        private int tongThoiGian = 180;
        // Thời gian được cộng thêm sau mỗi nước đi (3 giây)
        private int thoiGianCongThem = 3;
        // Biến đếm ngược hiện tại
        private int thoiGianConLai;
        private int mySide = 0; // 1 = X, 2 = O
        private Image imgX;
        private Image imgO;
        // --- THAY THẾ TOÀN BỘ HÀM KHỞI TẠO (CONSTRUCTOR) ---
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            // --- 1. CÀI ĐẶT TRẠNG THÁI GIAO DIỆN MẶC ĐỊNH ---
            // Vì không dùng Login nên phải hiện các nút kết nối
            if (groupBox2 != null) groupBox2.Visible = true; // Khung nhập IP
            if (btnConnect != null) btnConnect.Visible = true;
            if (txtName != null) txtName.Visible = true;

            // Ẩn bàn cờ khi chưa kết nối
            if (pnlChessBoard != null) pnlChessBoard.Visible = false;

            // Đảm bảo nút chức năng (nếu có)
            if (btnNewGame != null) btnNewGame.Visible = true;
            if (btnUndo != null) btnUndo.Visible = true;
           

            // --- 2. LOAD HÌNH ẢNH QUÂN CỜ ---
            try
            {
                string pathX = Application.StartupPath + "\\x.png";
                string pathO = Application.StartupPath + "\\o.png";
                if (System.IO.File.Exists(pathX)) imgX = Image.FromFile(pathX);
                if (System.IO.File.Exists(pathO)) imgO = Image.FromFile(pathO);
            }
            catch { }

            // --- 3. ĐĂNG KÝ SỰ KIỆN RESIZE ---
            // Để bàn cờ luôn vẽ lại ô vuông khi kéo cửa sổ
            this.Resize += new EventHandler(Form1_Resize);

            // --- 4. CẤU HÌNH CO GIÃN GIAO DIỆN (RESPONSIVE) ---
            // Đây là phần sửa lỗi giao diện bị lệch khi phóng to/thu nhỏ

            // Bàn cờ: Co giãn 4 chiều để luôn lấp đầy khoảng trống
            if (pnlChessBoard != null)
                pnlChessBoard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Cột Trái (Chat): Dãn theo chiều dọc
            if (rtbChatLog != null)
                rtbChatLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

            // Nút Gửi & Ô nhập: Luôn dính đáy
            if (txtMessage != null) txtMessage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            if (btnSend != null) btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            // Cột Phải (Thông tin đối thủ): Luôn dính lề Phải
            if (ptbAvatar2 != null) ptbAvatar2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (panel2 != null) panel2.Anchor = AnchorStyles.Top | AnchorStyles.Right; // Panel màu đỏ (nếu có)

            // Đồng hồ & Thanh thời gian: Dính lề Phải
            if (lblDongHo != null) lblDongHo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (prcbCoolDown != null) prcbCoolDown.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Thiết lập kích thước tối thiểu để không bị vỡ giao diện
            this.MinimumSize = new Size(1000, 700);
        }
        // Thêm hàm xử lý sự kiện này ở bên dưới (cùng chỗ với các hàm click nút)
        private void Form1_Resize(object sender, EventArgs e)
        {
            pnlChessBoard.Invalidate(); // Lệnh này bắt buộc bàn cờ xóa đi vẽ lại ngay lập tức
        }

        // Hàm này giúp tính toán kích thước 1 ô cờ luôn vuông
        private float GetCellSize()
        {
            // Lấy cạnh nhỏ hơn của panel để làm chuẩn
            float minSide = Math.Min(pnlChessBoard.Width, pnlChessBoard.Height);
            return minSide / GameConstant.CHESS_BOARD_WIDTH;
        }
        // --- 1. KẾT NỐI SERVER ---
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                // Kiểm tra dữ liệu nhập
                string playerName = txtName.Text.Trim();
                string ipAddress = txtIP.Text.Trim();

                if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(ipAddress))
                {
                    MessageBox.Show("Vui lòng nhập Tên và IP!");
                    return;
                }

                // 1. Tạo kết nối MỚI
                client = new TcpClient();
                client.Connect(ipAddress, 8080); // Kết nối tới Server

                // 2. Tạo luồng đọc/ghi (Quan trọng: Phải tạo ở đây)
                stream = client.GetStream();
                writer = new StreamWriter(stream) { AutoFlush = true };
                reader = new StreamReader(stream);

                // 3. Gửi lệnh CONNECT (Dùng lại lệnh cũ vì không dùng LOGIN nữa)
                // Lưu ý: Server phải hỗ trợ lệnh CONNECT này (như code Server ban đầu)
                writer.WriteLine("CONNECT|" + playerName);

                // 4. Bắt đầu luồng nhận tin nhắn
                Thread listenThread = new Thread(ReceiveMessage);
                listenThread.IsBackground = true;
                listenThread.Start();

                // 5. Cập nhật giao diện sau khi kết nối
                HienThongBaoTamThoi("Kết nối thành công!");
                btnConnect.Enabled = false;
                txtName.ReadOnly = true;
                txtIP.ReadOnly = true;
                pnlChessBoard.Visible = true; // Hiện bàn cờ
                lblLuotDi.Text = "Đang đợi đối thủ...";

                // Cập nhật tiêu đề Form
                this.Text = "Game Caro - " + playerName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        // --- 2. ĐĂNG XUẤT / NGẮT KẾT NỐI ---
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (client != null)
                {
                    client.Close();
                    client = null;
                }

                // Reset giao diện
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                txtName.ReadOnly = false;
                txtIP.ReadOnly = false;
                pnlChessBoard.Visible = false;

                rtbChatLog.Clear();
                lblLuotDi.Text = "";
                tmCoolDown.Stop(); // Dừng đồng hồ nếu đang chạy

                HienThongBaoTamThoi("Đã đăng xuất!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đăng xuất: " + ex.Message);
            }
        }

        // --- 3. GỬI TIN NHẮN CHAT ---
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (client != null && client.Connected)
            {
                string msg = txtMessage.Text;
                if (!string.IsNullOrEmpty(msg))
                {
                    writer.WriteLine("CHAT|" + msg);

                    // Hiện tin mình lên
                    rtbChatLog.AppendText("Tôi: " + msg + Environment.NewLine);
                    rtbChatLog.ScrollToCaret();

                    txtMessage.Clear();
                }
            }
            else
            {
                MessageBox.Show("Bạn chưa kết nối Server!");
            }
        }

        // --- 4. XỬ LÝ CLICK BÀN CỜ ---
        private void pnlChessBoard_Paint_MouseClick(object sender, MouseEventArgs e)
        {
            if (client == null || !client.Connected) return;

            // --- SỬA ĐỔI: Dùng hàm GetCellSize mới ---
            float cellSize = GetCellSize();

            int x = (int)(e.X / cellSize);
            int y = (int)(e.Y / cellSize);
            // ----------------------------------------

            // Kiểm tra xem click có ra ngoài bàn cờ không (vì khi co kéo, bàn cờ có thể nhỏ hơn khung chứa)
            if (x >= GameConstant.CHESS_BOARD_WIDTH || y >= GameConstant.CHESS_BOARD_HEIGHT) return;

            try
            {
                string tinNhanGuiDi = $"MOVE|{x}|{y}";
                writer.WriteLine(tinNhanGuiDi);

                // Logic cộng giờ (giữ nguyên như cũ)
                thoiGianConLai += thoiGianCongThem;
                lblDongHo.Text = TimeSpan.FromSeconds(thoiGianConLai).ToString(@"mm\:ss");
                if (thoiGianConLai > prcbCoolDown.Maximum)
                    prcbCoolDown.Maximum = thoiGianConLai;
                prcbCoolDown.Value = thoiGianConLai;
            }
            catch { }
        }

        // --- 5. NHẬN TIN NHẮN TỪ SERVER ---
        private void ReceiveMessage()
        {
            try
            {
                while (client.Connected)
                {
                    string msg = reader.ReadLine();
                    if (msg == null) break;

                    string[] parts = msg.Split('|');
                    string command = parts[0];

                    if (command == "MOVE")
                    {
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        int side = int.Parse(parts[3]);
                        string luotTiepTheo = (side == 1) ? "O" : "X";

                        this.Invoke(new Action(() => {
                            lblLuotDi.Text = $"Đến lượt người chơi {luotTiepTheo}";
                            Graphics g = pnlChessBoard.CreateGraphics();

                            // --- SỬA ĐỔI: Dùng GetCellSize ---
                            float cellSize = GetCellSize();
                            float xPos = x * cellSize;
                            float yPos = y * cellSize;
                            // ---------------------------------
                            PlaySound("click.wav");
                            if (side == 1)
                            {
                                if (imgX != null)
                                    g.DrawImage(imgX, xPos + 2, yPos + 2, cellSize - 4, cellSize - 4);
                                else
                                    g.DrawString("X", new Font("Arial", cellSize * 0.6f, FontStyle.Bold), Brushes.Red, xPos + (cellSize * 0.2f), yPos + (cellSize * 0.1f));
                            }
                            else
                            {
                                if (imgO != null)
                                    g.DrawImage(imgO, xPos + 2, yPos + 2, cellSize - 4, cellSize - 4);
                                else
                                    g.DrawString("O", new Font("Arial", cellSize * 0.6f, FontStyle.Bold), Brushes.Blue, xPos + (cellSize * 0.2f), yPos + (cellSize * 0.1f));
                            }

                            ResetTimer();
                        }));
                    }
                    // ... (Các lệnh MOVE, UNDO phía trên giữ nguyên)

                    else if (command == "ROUND_WIN")
                    {
                        int winnerSide = int.Parse(parts[1]);

                        this.Invoke(new Action(() => {
                            tmCoolDown.Stop();

                            if (mySide == winnerSide)
                                PlaySound("win.wav");  // <--- Mình thắng
                            else
                                PlaySound("lose.wav"); // <--- Mình thua

                            string thongBao = "";
                            // Logic xác định thắng thua
                            if (mySide == winnerSide)
                            {
                                // Nếu mình thắng
                                string doiThu = (mySide == 1) ? "O" : "X";
                                thongBao = $"Bạn đã thắng thằng {doiThu}!";
                            }
                            else
                            {
                                // Nếu mình thua
                                string keThang = (winnerSide == 1) ? "X" : "O";
                                thongBao = $"Bạn đã để thua thằng {keThang}!";
                            }

                            MessageBox.Show(thongBao, "Kết quả ván đấu");
                        }));
                    }
                    else if (command == "SERIES_WIN")
                    {
                        // Server gửi: SERIES_WIN|SideThang (Thắng chung cuộc)
                        int winnerSide = int.Parse(parts[1]);
                        this.Invoke(new Action(() => {
                            tmCoolDown.Stop();
                            string thongBao = (winnerSide == 1) ? "CHÚC MỪNG! X ĐÃ VÔ ĐỊCH!" : "CHÚC MỪNG! O ĐÃ VÔ ĐỊCH!";
                            MessageBox.Show(thongBao, "Kết quả chung cuộc");
                        }));
                    }

                    // ... (Các lệnh NEW_GAME, GAMEOVER giữ nguyên)
                    else if (command == "UNDO")
                    {
                        int uX = int.Parse(parts[1]);
                        int uY = int.Parse(parts[2]);

                        this.Invoke(new Action(() => {
                            Graphics g = pnlChessBoard.CreateGraphics();

                            // --- SỬA ĐỔI: Dùng GetCellSize ---
                            float cellSize = GetCellSize();
                            // ---------------------------------

                            SolidBrush eraserBrush = new SolidBrush(pnlChessBoard.BackColor);

                            // Xóa ô (vẽ đè màu nền)
                            g.FillRectangle(eraserBrush, uX * cellSize + 1, uY * cellSize + 1, cellSize - 2, cellSize - 2);

                            // Vẽ lại đường viền ô
                            Pen pen = new Pen(Color.Black);
                            g.DrawRectangle(pen, uX * cellSize, uY * cellSize, cellSize, cellSize);

                            if (lblLuotDi.Text.Contains("X"))
                                lblLuotDi.Text = "Đã đi lại. Đến lượt người chơi O";
                            else
                                lblLuotDi.Text = "Đã đi lại. Đến lượt người chơi X";

                            ResetTimer();
                        }));
                    }
                    // ... (Các lệnh GAMEOVER, NEW_GAME, MESSAGE, CHAT giữ nguyên logic cũ) ...
                    else if (command == "GAMEOVER")
                    {
                        this.Invoke(new Action(() => {
                            tmCoolDown.Stop();
                            int winnerSide = int.Parse(parts[1]);
                            string thongBao = (winnerSide == 1) ? "QUÂN X CHIẾN THẮNG!" : "QUÂN O CHIẾN THẮNG!";
                            MessageBox.Show(thongBao);
                        }));
                    }
                    else if (command == "NEW_GAME")
                    {
                        this.Invoke(new Action(() => {
                            PlaySound("start.wav");
                            pnlChessBoard.Invalidate();
                            lblLuotDi.Text = "Ván mới! Lượt của X";
                            ResetTimer();
                            HienThongBaoTamThoi("Đã tạo ván mới!");
                        }));
                    }
                    else if (command == "GAME_START")
                    {
                        // Server gửi: GAME_START|Side (ví dụ: GAME_START|1)
                        if (parts.Length > 1)
                            mySide = int.Parse(parts[1]);

                        this.Invoke(new Action(() => {
                            pnlChessBoard.Invalidate();
                            lblLuotDi.Text = "Đủ người! Bắt đầu. Lượt của X";

                            // Thông báo cho người chơi biết họ là quân gì
                            string quanTa = (mySide == 1) ? "X" : "O";
                            HienThongBaoTamThoi($"Game bắt đầu! Bạn là quân {quanTa}");

                            ResetTimer();
                        }));
                    }
                    else if (command == "MESSAGE")
                    {
                        string noiDung = parts[1];
                        this.Invoke(new Action(() => {
                            if (noiDung.Contains("thoát"))
                                MessageBox.Show(noiDung, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            else
                                HienThongBaoTamThoi(noiDung);
                        }));
                    }
                    else if (command == "CHAT")
                    {
                        string tenNguoiGui = parts[1];
                        string noiDung = parts[2];
                        this.Invoke(new Action(() => {
                            rtbChatLog.AppendText($"{tenNguoiGui}: {noiDung}{Environment.NewLine}");
                            rtbChatLog.ScrollToCaret();
                        }));
                    }
                }
            }
            catch { }
        }

        // Hàm phát âm thanh an toàn (không gây lỗi nếu thiếu file)
        private void PlaySound(string fileName)
        {
            try
            {
                // Đường dẫn file nằm cùng thư mục với file exe
                string path = Application.StartupPath + "\\" + fileName;

                // Kiểm tra file có tồn tại không mới phát
                if (System.IO.File.Exists(path))
                {
                    SoundPlayer player = new SoundPlayer(path);
                    player.Play(); // Play() phát xong tự tắt, không lặp
                }
            }
            catch
            {
                // Nếu lỗi âm thanh thì bỏ qua, không làm crash game
            }
        }
        // --- 6. YÊU CẦU UNDO ---
        private void btnUndo_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Đã bấm nút!");
            // Kiểm tra nếu đang kết nối thì mới gửi lệnh
            if (client != null && client.Connected)
            {
                writer.WriteLine("UNDO");
            }
        }

        // --- CÁC HÀM HỖ TRỢ ---

        private void btnNewGame_Click(object sender, EventArgs e)
        {
            if (client != null && client.Connected) writer.WriteLine("NEW_GAME");
        }

        private void pnlChessBoard_Paint_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            float cellSize = GetCellSize(); // Dùng hàm chung

            float boardWidth = cellSize * GameConstant.CHESS_BOARD_WIDTH;
            float boardHeight = cellSize * GameConstant.CHESS_BOARD_HEIGHT;

            Pen pen = new Pen(Color.Black);

            for (int i = 0; i <= GameConstant.CHESS_BOARD_HEIGHT; i++)
                g.DrawLine(pen, 0, i * cellSize, boardWidth, i * cellSize);

            for (int i = 0; i <= GameConstant.CHESS_BOARD_WIDTH; i++)
                g.DrawLine(pen, i * cellSize, 0, i * cellSize, boardHeight);
        }

        private void tmCoolDown_Tick_1(object sender, EventArgs e)
        {
            if (thoiGianConLai > 0)
            {
                thoiGianConLai--; // Trừ đi 1 giây

                // Cập nhật Label đồng hồ (02:59...)
                lblDongHo.Text = TimeSpan.FromSeconds(thoiGianConLai).ToString(@"mm\:ss");

                // Cập nhật thanh ProgressBar (tụt dần)
                if (prcbCoolDown.Value > 0) prcbCoolDown.Value = thoiGianConLai;

                // Hiệu ứng: Dưới 10 giây thì chữ chuyển màu đỏ báo động
                if (thoiGianConLai <= 10) lblDongHo.ForeColor = Color.Red;
                else lblDongHo.ForeColor = Color.Black;
            }
            else
            {
                // HẾT GIỜ!
                tmCoolDown.Stop();
                if (client != null && client.Connected)
                {
                    writer.WriteLine("TIME_OUT"); // Gửi lệnh thua cuộc
                }
            }
        }

        private void ResetTimer()
        {
            // Cài đặt lại thời gian về ban đầu (180 giây)
            thoiGianConLai = tongThoiGian;

            // Cập nhật giao diện
            lblDongHo.Text = TimeSpan.FromSeconds(thoiGianConLai).ToString(@"mm\:ss");

            // Cài đặt thanh ProgressBar (để trang trí cho đẹp)
            prcbCoolDown.Maximum = tongThoiGian;
            prcbCoolDown.Value = tongThoiGian;

            tmCoolDown.Start();
        }

        private async void HienThongBaoTamThoi(string noiDung)
        {
            lblThongBao.ForeColor = (noiDung.Contains("Lỗi") || noiDung.Contains("Chưa")) ? Color.Red : Color.Green;
            lblThongBao.Text = noiDung;
            lblThongBao.Visible = true;
            await Task.Delay(2000);
            lblThongBao.Text = "";
        }

        private void txtMessage_TextChanged(object sender, EventArgs e) { }
        // --- CODE TIỆN ÍCH ĐỒ HỌA ---

        // 1. Hàm biến hình thành Hình Tròn (Dùng cho Avatar)
        private void MakeCircular(Control control)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, control.Width, control.Height);
            control.Region = new Region(path);
        }

        // 2. Hàm bo tròn góc (Dùng cho Button, Panel)
        private void MakeRounded(Control control, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2; // Đường kính góc bo

            // Vẽ 4 góc bo
            path.AddArc(0, 0, d, d, 180, 90); // Góc trên trái
            path.AddArc(control.Width - d, 0, d, d, 270, 90); // Góc trên phải
            path.AddArc(control.Width - d, control.Height - d, d, d, 0, 90); // Góc dưới phải
            path.AddArc(0, control.Height - d, d, d, 90, 90); // Góc dưới trái
            path.CloseFigure();

            control.Region = new Region(path);
        }

        //private void Form1_Load(object sender, EventArgs e)
        //{
        //    // Biến 2 khung ảnh thành hình tròn
        //    MakeCircular(ptbAvatar1);
        //    MakeCircular(ptbAvatar2);

        //    // Bo tròn các nút bấm (Bo góc 20px)
        //    MakeRounded(btnConnect, 20);
        //    MakeRounded(btnSend, 15);
        //    MakeRounded(btnNewGame, 20);

        //    // Bo tròn cả cái bàn cờ nếu thích
        //    // MakeRounded(pnlChessBoard, 10);
        //}

        private void btnAvatar_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            // Chỉ lọc lấy file ảnh
            open.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (open.ShowDialog() == DialogResult.OK)
            {
                // Tải ảnh lên PictureBox
                ptbAvatar1.Image = new Bitmap(open.FileName);

                // (Nâng cao) Nếu muốn gửi ảnh này cho đối thủ thấy:
                // Bạn cần chuyển ảnh thành chuỗi Base64 rồi gửi qua mạng (khá phức tạp cho người mới).
                // Tạm thời chỉ hiện trên máy mình cho đẹp trước đã.
            }
        }


    }

}