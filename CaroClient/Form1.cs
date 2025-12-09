using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks; // Chỉ cần khai báo 1 lần
using System.Windows.Forms;
using CaroShared;

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

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false; // Tránh lỗi thread cơ bản
        }

        // --- 1. KẾT NỐI SERVER ---
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                // Kiểm tra tên
                string playerName = txtName.Text.Trim();
                if (string.IsNullOrEmpty(playerName))
                {
                    MessageBox.Show("Vui lòng nhập tên!");
                    return;
                }

                // Kiểm tra IP
                string ipAddress = txtIP.Text.Trim();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    MessageBox.Show("Vui lòng nhập IP Server!");
                    return;
                }

                // Kết nối
                client = new TcpClient();
                client.Connect(ipAddress, 8080);

                stream = client.GetStream();
                writer = new StreamWriter(stream) { AutoFlush = true };
                reader = new StreamReader(stream);

                // Gửi tên đăng nhập
                writer.WriteLine("CONNECT|" + playerName);

                // Bắt đầu nhận tin
                Thread listenThread = new Thread(ReceiveMessage);
                listenThread.IsBackground = true;
                listenThread.Start();

                HienThongBaoTamThoi("Kết nối thành công!");

                // Cập nhật giao diện
                btnConnect.Enabled = false;       // Khóa nút Connect
                btnDisconnect.Enabled = true;     // Mở nút Đăng xuất
                txtName.ReadOnly = true;          // Khóa tên
                txtIP.ReadOnly = true;            // Khóa IP
                pnlChessBoard.Visible = true;     // Hiện bàn cờ

                // Lưu ý: Không ResetTimer ở đây nữa, đợi GAME_START mới chạy
                lblLuotDi.Text = "Đang đợi đối thủ vào phòng...";
            }
            catch (Exception ex)
            {
                HienThongBaoTamThoi("Lỗi kết nối: " + ex.Message);
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

            float cellWidth = (float)pnlChessBoard.Width / GameConstant.CHESS_BOARD_WIDTH;
            float cellHeight = (float)pnlChessBoard.Height / GameConstant.CHESS_BOARD_HEIGHT;

            int x = (int)(e.X / cellWidth);
            int y = (int)(e.Y / cellHeight);

            if (x >= GameConstant.CHESS_BOARD_WIDTH || y >= GameConstant.CHESS_BOARD_HEIGHT) return;

            try
            {
                string tinNhanGuiDi = $"MOVE|{x}|{y}";
                writer.WriteLine(tinNhanGuiDi);

                // --- [MỚI] LOGIC CỘNG GIỜ (INCREMENT) ---
                // Sau khi đánh xong, mình được cộng thêm thời gian
                thoiGianConLai += thoiGianCongThem;

                // Cập nhật lại giao diện ngay lập tức để nhìn thấy mình được hồi máu
                lblDongHo.Text = TimeSpan.FromSeconds(thoiGianConLai).ToString(@"mm\:ss");

                // Cập nhật ProgressBar (không được vượt quá Max)
                if (thoiGianConLai > prcbCoolDown.Maximum)
                    prcbCoolDown.Maximum = thoiGianConLai; // Tự nới rộng thanh nếu thời gian cộng dồn quá nhiều
                prcbCoolDown.Value = thoiGianConLai;
                // ----------------------------------------
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

                            float cellWidth = (float)pnlChessBoard.Width / GameConstant.CHESS_BOARD_WIDTH;
                            float cellHeight = (float)pnlChessBoard.Height / GameConstant.CHESS_BOARD_HEIGHT;
                            float fontSize = cellHeight * 0.6f;
                            Font dynamicFont = new Font("Arial", fontSize, FontStyle.Bold);

                            float xPos = x * cellWidth + (cellWidth * 0.2f);
                            float yPos = y * cellHeight + (cellHeight * 0.1f);

                            if (side == 1) g.DrawString("X", dynamicFont, Brushes.Red, xPos, yPos);
                            else g.DrawString("O", dynamicFont, Brushes.Blue, xPos, yPos);

                            ResetTimer();
                        }));
                    }
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
                            pnlChessBoard.Invalidate();
                            lblLuotDi.Text = "Ván mới! Lượt của X";
                            ResetTimer();
                            HienThongBaoTamThoi("Đã tạo ván mới!");
                        }));
                    }
                    else if (command == "GAME_START")
                    {
                        this.Invoke(new Action(() => {
                            pnlChessBoard.Invalidate();
                            lblLuotDi.Text = "Đủ người! Bắt đầu. Lượt của X";
                            HienThongBaoTamThoi("Game bắt đầu!");
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

        // --- CÁC HÀM HỖ TRỢ ---

        private void btnNewGame_Click(object sender, EventArgs e)
        {
            if (client != null && client.Connected) writer.WriteLine("NEW_GAME");
        }

        private void pnlChessBoard_Paint_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Black);
            float cellWidth = (float)pnlChessBoard.Width / GameConstant.CHESS_BOARD_WIDTH;
            float cellHeight = (float)pnlChessBoard.Height / GameConstant.CHESS_BOARD_HEIGHT;

            for (int i = 0; i <= GameConstant.CHESS_BOARD_WIDTH; i++)
                g.DrawLine(pen, i * cellWidth, 0, i * cellWidth, pnlChessBoard.Height);
            for (int i = 0; i <= GameConstant.CHESS_BOARD_HEIGHT; i++)
                g.DrawLine(pen, 0, i * cellHeight, pnlChessBoard.Width, i * cellHeight);
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

        private void Form1_Load(object sender, EventArgs e)
        {
            // Biến 2 khung ảnh thành hình tròn
            MakeCircular(ptbAvatar1);
            MakeCircular(ptbAvatar2);

            // Bo tròn các nút bấm (Bo góc 20px)
            MakeRounded(btnConnect, 20);
            MakeRounded(btnSend, 15);
            MakeRounded(btnNewGame, 20);

            // Bo tròn cả cái bàn cờ nếu thích
            // MakeRounded(pnlChessBoard, 10);
        }

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