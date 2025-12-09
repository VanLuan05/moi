namespace CaroClient
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.pnlChessBoard = new System.Windows.Forms.Panel();
            this.lblThongBao = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.lblLuotDi = new System.Windows.Forms.Label();
            this.btnNewGame = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.prcbCoolDown = new System.Windows.Forms.ProgressBar();
            this.rtbChatLog = new System.Windows.Forms.RichTextBox();
            this.tmCoolDown = new System.Windows.Forms.Timer(this.components);
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtIP = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.ptbAvatar1 = new System.Windows.Forms.PictureBox();
            this.ptbAvatar2 = new System.Windows.Forms.PictureBox();
            this.btnAvatar = new System.Windows.Forms.Button();
            this.lblDongHo = new System.Windows.Forms.Label();
            this.pnlChessBoard.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ptbAvatar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ptbAvatar2)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(12, 499);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 0;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtMessage
            // 
            this.txtMessage.Location = new System.Drawing.Point(12, 473);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(200, 20);
            this.txtMessage.TabIndex = 1;
            // 
            // pnlChessBoard
            // 
            this.pnlChessBoard.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlChessBoard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlChessBoard.Controls.Add(this.lblThongBao);
            this.pnlChessBoard.Location = new System.Drawing.Point(452, 134);
            this.pnlChessBoard.Name = "pnlChessBoard";
            this.pnlChessBoard.Size = new System.Drawing.Size(775, 670);
            this.pnlChessBoard.TabIndex = 2;
            this.pnlChessBoard.Visible = false;
            this.pnlChessBoard.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlChessBoard_Paint_Paint);
            this.pnlChessBoard.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pnlChessBoard_Paint_MouseClick);
            // 
            // lblThongBao
            // 
            this.lblThongBao.AutoSize = true;
            this.lblThongBao.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThongBao.ForeColor = System.Drawing.Color.Red;
            this.lblThongBao.Location = new System.Drawing.Point(146, 155);
            this.lblThongBao.Name = "lblThongBao";
            this.lblThongBao.Size = new System.Drawing.Size(0, 33);
            this.lblThongBao.TabIndex = 4;
            this.lblThongBao.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(6, 42);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // lblLuotDi
            // 
            this.lblLuotDi.AutoSize = true;
            this.lblLuotDi.Location = new System.Drawing.Point(449, 118);
            this.lblLuotDi.Name = "lblLuotDi";
            this.lblLuotDi.Size = new System.Drawing.Size(86, 13);
            this.lblLuotDi.TabIndex = 5;
            this.lblLuotDi.Text = "Đang đợi kết nối";
            // 
            // btnNewGame
            // 
            this.btnNewGame.Location = new System.Drawing.Point(6, 71);
            this.btnNewGame.Name = "btnNewGame";
            this.btnNewGame.Size = new System.Drawing.Size(75, 23);
            this.btnNewGame.TabIndex = 6;
            this.btnNewGame.Text = "Ván Mới";
            this.btnNewGame.UseVisualStyleBackColor = true;
            this.btnNewGame.Click += new System.EventHandler(this.btnNewGame_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnDisconnect);
            this.groupBox1.Controls.Add(this.txtName);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.btnNewGame);
            this.groupBox1.Controls.Add(this.btnConnect);
            this.groupBox1.Location = new System.Drawing.Point(12, 83);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 100);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(89, 42);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnDisconnect.TabIndex = 12;
            this.btnDisconnect.Text = "Sign Out";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(89, 16);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(100, 20);
            this.txtName.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Nhập Tên Bạn";
            // 
            // prcbCoolDown
            // 
            this.prcbCoolDown.Location = new System.Drawing.Point(721, 105);
            this.prcbCoolDown.Maximum = 30000;
            this.prcbCoolDown.Name = "prcbCoolDown";
            this.prcbCoolDown.Size = new System.Drawing.Size(248, 23);
            this.prcbCoolDown.Step = 1;
            this.prcbCoolDown.TabIndex = 9;
            // 
            // rtbChatLog
            // 
            this.rtbChatLog.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.rtbChatLog.Location = new System.Drawing.Point(12, 189);
            this.rtbChatLog.Name = "rtbChatLog";
            this.rtbChatLog.ReadOnly = true;
            this.rtbChatLog.Size = new System.Drawing.Size(200, 278);
            this.rtbChatLog.TabIndex = 10;
            this.rtbChatLog.Text = "";
            // 
            // tmCoolDown
            // 
            this.tmCoolDown.Interval = 1000;
            this.tmCoolDown.Tick += new System.EventHandler(this.tmCoolDown_Tick_1);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtIP);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(200, 65);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "groupBox2";
            // 
            // txtIP
            // 
            this.txtIP.Location = new System.Drawing.Point(6, 32);
            this.txtIP.Name = "txtIP";
            this.txtIP.Size = new System.Drawing.Size(100, 20);
            this.txtIP.TabIndex = 13;
            this.txtIP.Text = "127.0.0.1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "IP Server";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.RoyalBlue;
            this.panel1.Location = new System.Drawing.Point(228, 19);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 100);
            this.panel1.TabIndex = 12;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Firebrick;
            this.panel2.Location = new System.Drawing.Point(1331, 64);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(200, 100);
            this.panel2.TabIndex = 0;
            // 
            // ptbAvatar1
            // 
            this.ptbAvatar1.Location = new System.Drawing.Point(248, 125);
            this.ptbAvatar1.Name = "ptbAvatar1";
            this.ptbAvatar1.Size = new System.Drawing.Size(160, 160);
            this.ptbAvatar1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ptbAvatar1.TabIndex = 13;
            this.ptbAvatar1.TabStop = false;
            // 
            // ptbAvatar2
            // 
            this.ptbAvatar2.Location = new System.Drawing.Point(1358, 170);
            this.ptbAvatar2.Name = "ptbAvatar2";
            this.ptbAvatar2.Size = new System.Drawing.Size(160, 160);
            this.ptbAvatar2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ptbAvatar2.TabIndex = 14;
            this.ptbAvatar2.TabStop = false;
            // 
            // btnAvatar
            // 
            this.btnAvatar.Location = new System.Drawing.Point(248, 290);
            this.btnAvatar.Name = "btnAvatar";
            this.btnAvatar.Size = new System.Drawing.Size(75, 23);
            this.btnAvatar.TabIndex = 15;
            this.btnAvatar.Text = "Up Avatar";
            this.btnAvatar.UseVisualStyleBackColor = true;
            this.btnAvatar.Click += new System.EventHandler(this.btnAvatar_Click);
            // 
            // lblDongHo
            // 
            this.lblDongHo.AutoSize = true;
            this.lblDongHo.BackColor = System.Drawing.Color.AliceBlue;
            this.lblDongHo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDongHo.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDongHo.Location = new System.Drawing.Point(800, 47);
            this.lblDongHo.Name = "lblDongHo";
            this.lblDongHo.Size = new System.Drawing.Size(84, 33);
            this.lblDongHo.TabIndex = 5;
            this.lblDongHo.Text = "03:00";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightSteelBlue;
            this.ClientSize = new System.Drawing.Size(1617, 756);
            this.Controls.Add(this.lblDongHo);
            this.Controls.Add(this.btnAvatar);
            this.Controls.Add(this.ptbAvatar2);
            this.Controls.Add(this.ptbAvatar1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.rtbChatLog);
            this.Controls.Add(this.prcbCoolDown);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblLuotDi);
            this.Controls.Add(this.pnlChessBoard);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.btnSend);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlChessBoard.ResumeLayout(false);
            this.pnlChessBoard.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ptbAvatar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ptbAvatar2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Panel pnlChessBoard;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label lblThongBao;
        private System.Windows.Forms.Label lblLuotDi;
        private System.Windows.Forms.Button btnNewGame;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar prcbCoolDown;
        private System.Windows.Forms.RichTextBox rtbChatLog;
        private System.Windows.Forms.Timer tmCoolDown;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.PictureBox ptbAvatar1;
        private System.Windows.Forms.PictureBox ptbAvatar2;
        private System.Windows.Forms.Button btnAvatar;
        private System.Windows.Forms.Label lblDongHo;
    }
}

