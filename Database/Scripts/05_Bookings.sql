USE FlightBookingAI;
GO

-- Tạo bảng Bookings
CREATE TABLE Bookings
(
    BookingID INT IDENTITY(1,1) PRIMARY KEY,

    BookingCode VARCHAR(20) NOT NULL UNIQUE,

    UserID INT NOT NULL,

    FlightID INT NOT NULL,

    BookingDate DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    PassengerCount INT NOT NULL,

    TotalAmount DECIMAL(18,2) NOT NULL,

    Status VARCHAR(20) NOT NULL
        DEFAULT 'Pending',

    -- Khóa ngoại liên kết bảng Users
    CONSTRAINT FK_Bookings_Users
        FOREIGN KEY (UserID)
        REFERENCES Users(UserID),

    -- Khóa ngoại liên kết bảng Flights
    CONSTRAINT FK_Bookings_Flights
        FOREIGN KEY (FlightID)
        REFERENCES Flights(FlightID),

    -- Kiểm tra số lượng hành khách
    CONSTRAINT CK_Bookings_PassengerCount
        CHECK (PassengerCount > 0),

    -- Kiểm tra tổng tiền
    CONSTRAINT CK_Bookings_TotalAmount
        CHECK (TotalAmount >= 0),

    -- Kiểm tra trạng thái đơn đặt vé
    CONSTRAINT CK_Bookings_Status
        CHECK (Status IN (
            'Pending',
            'Confirmed',
            'Cancelled'
        ))
);
GO

-- Kiểm tra bảng Bookings
SELECT *
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'Bookings';

-- Kiểm tra dữ liệu người dùng
SELECT UserID, FullName, Email
FROM Users;

-- Kiểm tra dữ liệu chuyến bay
SELECT FlightID, FlightNumber, Price
FROM Flights;

-- Thêm người dùng mẫu để kiểm tra chức năng đặt vé
INSERT INTO Users
(
    FullName,
    Email,
    PasswordHash,
    Phone,
    Role
)
VALUES
(
    N'Nguyễn Văn A',
    'nguyenvana@example.com',
    'TEST_ACCOUNT_NO_LOGIN',
    '0901234567',
    'Customer'
);
SELECT UserID, FullName, Email
FROM Users;

-- Thêm đơn đặt vé đầu tiên
INSERT INTO Bookings
(
    BookingCode,
    UserID,
    FlightID,
    PassengerCount,
    TotalAmount,
    Status
)
VALUES
(
    'BK20260001',
    1,
    1,
    2,
    3000000,
    'Pending'
);
-- Kiểm tra dữ liệu đặt vé
SELECT *
FROM Bookings;

-- Hiển thị thông tin đơn đặt vé đầy đủ
SELECT
    B.BookingCode,
    U.FullName,
    F.FlightNumber,
    B.PassengerCount,
    B.TotalAmount,
    B.BookingDate,
    B.Status
FROM Bookings AS B

INNER JOIN Users AS U
    ON B.UserID = U.UserID

INNER JOIN Flights AS F
    ON B.FlightID = F.FlightID;