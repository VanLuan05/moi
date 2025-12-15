-- 2. TẠO DATABASE MỚI
CREATE DATABASE CaroDB;
GO
USE CaroDB;
GO

-- 3. TẠO BẢNG NGƯỜI CHƠI (Accounts)
-- Lưu thông tin đăng nhập và thành tích tổng quan
CREATE TABLE NguoiChoi (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    TaiKhoan NVARCHAR(50) UNIQUE NOT NULL,   -- Tên đăng nhập
    MatKhau VARCHAR(100) NOT NULL,          -- Mật khẩu
    TenHienThi NVARCHAR(50) NOT NULL,       -- Tên hiển thị (có dấu)
    Email VARCHAR(100) NULL,                -- Email (để phục hồi pass)
    
    -- Thống kê thành tích (Cập nhật sau mỗi ván)
    Diem INT DEFAULT 1000,                  -- Điểm Elo xếp hạng
    SoTranThang INT DEFAULT 0,
    SoTranThua INT DEFAULT 0,
    SoTranHoa INT DEFAULT 0,
    
    IsAdmin BIT DEFAULT 0,                  -- 1: Admin, 0: User thường
    NgayDangKy DATETIME DEFAULT GETDATE(),
    TrangThai BIT DEFAULT 1                 -- 1: Hoạt động, 0: Bị khóa
);
GO

-- 4. TẠO BẢNG TRẬN ĐẤU (Match History)
-- Lưu lại lịch sử ai đấu với ai, kết quả thế nào
CREATE TABLE TranDau (
    MatchID INT IDENTITY(1,1) PRIMARY KEY,
    
    Player1_ID INT NOT NULL,                -- ID người chơi 1
    Player2_ID INT NOT NULL,                -- ID người chơi 2
    Winner_ID INT NULL,                     -- ID người thắng (NULL nếu hòa)
    
    KichThuocBanCo INT DEFAULT 15,          -- Ví dụ: 15x15, 20x20
    ThoiGianBatDau DATETIME DEFAULT GETDATE(),
    ThoiGianKetThuc DATETIME,
    
    -- Tạo khóa ngoại liên kết với bảng NguoiChoi
    FOREIGN KEY (Player1_ID) REFERENCES NguoiChoi(ID),
    FOREIGN KEY (Player2_ID) REFERENCES NguoiChoi(ID),
    FOREIGN KEY (Winner_ID) REFERENCES NguoiChoi(ID)
);
GO

-- 5. TẠO BẢNG CHI TIẾT NƯỚC ĐI (Move Details - Nâng cao)
-- Lưu lại từng nước đi (X, Y) của ván đấu để làm tính năng Replay (nếu cần)
CREATE TABLE ChiTietTranDau (
    MoveID INT IDENTITY(1,1) PRIMARY KEY,
    MatchID INT NOT NULL,
    
    NuocDiSo INT,           -- Nước đi thứ mấy (1, 2, 3...)
    NguoiDi_ID INT,         -- Ai là người đi nước này
    ToaDoX INT,             -- Tọa độ dòng
    ToaDoY INT,             -- Tọa độ cột
    ThoiGianDanh DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (MatchID) REFERENCES TranDau(MatchID),
    FOREIGN KEY (NguoiDi_ID) REFERENCES NguoiChoi(ID)
);
GO

-- 6. THÊM DỮ LIỆU MẪU (SEED DATA)
INSERT INTO NguoiChoi (TaiKhoan, MatKhau, TenHienThi, IsAdmin, Diem) VALUES 
('admin', '123', N'Quản Trị Viên', 1, 9999),
('player1', '123', N'Cao Thủ Caro', 0, 1200),
('player2', '123', N'Gà Mờ', 0, 800),
('test', '123', N'Nick Test', 0, 1000);
GO

-- Kiểm tra lại dữ liệu
SELECT * FROM NguoiChoi;
SELECT * FROM TranDau;
SELECT * FROM ChiTietTranDau;

UPDATE NguoiChoi 
SET MatKhau = '202CB962AC59075B964B07152D234B70' 
WHERE MatKhau = '123';
GO


-- Thêm dữ liệu mẫu vào bảng NguoiChoi
-- Mật khẩu mặc định cho tất cả là '123' (Mã Hash MD5: 202CB962AC59075B964B07152D234B70)

INSERT INTO NguoiChoi (TaiKhoan, MatKhau, TenHienThi, Diem, SoTranThang, SoTranThua, SoTranHoa) VALUES 
(N'top1', '202CB962AC59075B964B07152D234B70', N'Rồng Thần', 3200, 200, 10, 5),
(N'top2', '202CB962AC59075B964B07152D234B70', N'Sát Thủ Caro', 2800, 150, 20, 10),
(N'top3', '202CB962AC59075B964B07152D234B70', N'Cao Thủ Ẩn Danh', 2500, 100, 5, 2),
(N'player4', '202CB962AC59075B964B07152D234B70', N'Học Sinh Giỏi', 1800, 50, 30, 20),
(N'player5', '202CB962AC59075B964B07152D234B70', N'Thánh Chơi Cờ', 1650, 40, 40, 5),
(N'player6', '202CB962AC59075B964B07152D234B70', N'Người Mới Tập', 1200, 10, 5, 0),
(N'player7', '202CB962AC59075B964B07152D234B70', N'Game Thủ Gà', 950, 5, 20, 2),
(N'player8', '202CB962AC59075B964B07152D234B70', N'Vui Là Chính', 800, 2, 50, 1),
(N'player9', '202CB962AC59075B964B07152D234B70', N'Thua Hoài Chán', 500, 0, 100, 0);
GO

SELECT COUNT(*) as SoLuongNguoiChoi FROM NguoiChoi;
SELECT * FROM NguoiChoi;