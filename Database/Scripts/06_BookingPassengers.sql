USE FlightBookingAI;
GO

-- Tạo bảng BookingPassengers
CREATE TABLE BookingPassengers
(
    BookingPassengerID INT IDENTITY(1,1) PRIMARY KEY,

    BookingID INT NOT NULL,

    FullName NVARCHAR(100) NOT NULL,

    DateOfBirth DATE NOT NULL,

    Gender NVARCHAR(10) NULL,

    IdentityNumber VARCHAR(30) NULL,

    PassengerType VARCHAR(10) NOT NULL,

    -- Khóa ngoại liên kết bảng Bookings
    CONSTRAINT FK_BookingPassengers_Bookings
        FOREIGN KEY (BookingID)
        REFERENCES Bookings(BookingID),

    -- Kiểm tra loại hành khách
    CONSTRAINT CK_BookingPassengers_Type
        CHECK (PassengerType IN (
            'Adult',
            'Child',
            'Infant'
        )),

    -- Kiểm tra giới tính
    CONSTRAINT CK_BookingPassengers_Gender
        CHECK (Gender IN (
            N'Nam',
            N'Nữ',
            N'Khác'
        ))
);
GO
-- Kiểm tra bảng BookingPassengers
SELECT *
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'BookingPassengers';

SELECT
    BookingID,
    BookingCode,
    PassengerCount
FROM Bookings;
-- Thêm 2 hành khách mẫu
INSERT INTO BookingPassengers
(
    BookingID,
    FullName,
    DateOfBirth,
    Gender,
    IdentityNumber,
    PassengerType
)
VALUES
(
    (SELECT BookingID FROM Bookings
     WHERE BookingCode = 'BK20260001'),

    N'Nguyễn Văn A',
    '2000-05-15',
    N'Nam',
    NULL,
    'Adult'
),
(
    (SELECT BookingID FROM Bookings
     WHERE BookingCode = 'BK20260001'),

    N'Trần Thị B',
    '2002-08-20',
    N'Nữ',
    NULL,
    'Adult'
);
SELECT *
FROM BookingPassengers;
-- Hiển thị hành khách theo đơn đặt vé

SELECT
    B.BookingCode,
    BP.FullName,
    BP.DateOfBirth,
    BP.Gender,
    BP.PassengerType
FROM BookingPassengers AS BP

INNER JOIN Bookings AS B
    ON BP.BookingID = B.BookingID

WHERE B.BookingCode = 'BK20260001';