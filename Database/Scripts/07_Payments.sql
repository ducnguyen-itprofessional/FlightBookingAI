USE FlightBookingAI;
GO

-- BẢNG 7: PAYMENTS
CREATE TABLE Payments
(
    PaymentID INT IDENTITY(1,1) PRIMARY KEY,

    BookingID INT NOT NULL,

    Amount DECIMAL(18,2) NOT NULL,

    PaymentMethod VARCHAR(30) NOT NULL,

    PaymentStatus VARCHAR(20) NOT NULL
        DEFAULT 'Pending',

    TransactionCode VARCHAR(100) NULL,

    PaymentDate DATETIME2 NULL,

    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    -- Khóa ngoại
    CONSTRAINT FK_Payments_Bookings
        FOREIGN KEY (BookingID)
        REFERENCES Bookings(BookingID),

    -- Kiểm tra số tiền
    CONSTRAINT CK_Payments_Amount
        CHECK (Amount > 0),

    -- Kiểm tra phương thức thanh toán
    CONSTRAINT CK_Payments_Method
        CHECK (PaymentMethod IN (
            'MoMo',
            'VNPay',
            'BankTransfer'
        )),

    -- Kiểm tra trạng thái thanh toán
    CONSTRAINT CK_Payments_Status
        CHECK (PaymentStatus IN (
            'Pending',
            'Success',
            'Failed',
            'Refunded'
        ))
);
GO

-- Kiểm tra bảng Payments
SELECT *
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'Payments';

SELECT
    BookingID,
    BookingCode,
    TotalAmount,
    Status
FROM Bookings
WHERE BookingCode = 'BK20260001';

-- Thêm giao dịch thanh toán mẫu
INSERT INTO Payments
(
    BookingID,
    Amount,
    PaymentMethod,
    PaymentStatus,
    TransactionCode,
    PaymentDate
)
SELECT
    BookingID,
    TotalAmount,
    'VNPay',
    'Pending',
    NULL,
    NULL
FROM Bookings
WHERE BookingCode = 'BK20260001';

SELECT *
FROM Payments;

-- Hiển thị thông tin thanh toán
SELECT
    P.PaymentID,
    B.BookingCode,
    U.FullName AS CustomerName,
    P.Amount,
    P.PaymentMethod,
    P.PaymentStatus,
    P.TransactionCode,
    P.PaymentDate,
    P.CreatedAt
FROM Payments AS P

INNER JOIN Bookings AS B
    ON P.BookingID = B.BookingID

INNER JOIN Users AS U
    ON B.UserID = U.UserID

WHERE B.BookingCode = 'BK20260001';

-- Ví dụ minh họa, KHÔNG chạy để xác nhận thanh toán thật
UPDATE Payments
SET
    PaymentStatus = 'Success',
    TransactionCode = 'DEMO_VNPAY_0001',
    PaymentDate = SYSDATETIME()
WHERE PaymentID = 1
  AND PaymentStatus = 'Pending';