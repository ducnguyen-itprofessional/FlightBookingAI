USE FlightBookingAI;
GO

-- BẢNG 8: DESTINATIONS
CREATE TABLE Destinations
(
    DestinationID INT IDENTITY(1,1) PRIMARY KEY,

    DestinationName NVARCHAR(150) NOT NULL,

    City NVARCHAR(100) NOT NULL,

    Country NVARCHAR(100) NOT NULL
        DEFAULT N'Việt Nam',

    Description NVARCHAR(MAX) NULL,

    ImageURL NVARCHAR(500) NULL,

    AverageDailyBudget DECIMAL(18,2) NULL,

    IsActive BIT NOT NULL DEFAULT 1,

    -- Kiểm tra ngân sách
    CONSTRAINT CK_Destinations_Budget
        CHECK (
            AverageDailyBudget IS NULL
            OR AverageDailyBudget >= 0
        )
);
GO
-- Kiểm tra bảng Destinations
SELECT *
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'Destinations';
-- Thêm dữ liệu mẫu cho Destinations
INSERT INTO Destinations
(
    DestinationName,
    City,
    Country,
    Description,
    ImageURL,
    AverageDailyBudget,
    IsActive
)
VALUES
(
    N'Đà Nẵng',
    N'Đà Nẵng',
    N'Việt Nam',
    N'Thành phố biển nổi tiếng với Cầu Rồng, Bà Nà Hills và Ngũ Hành Sơn.',
    NULL,
    800000,
    1
),
(
    N'Đà Lạt',
    N'Đà Lạt',
    N'Việt Nam',
    N'Thành phố cao nguyên với khí hậu mát mẻ, hồ nước và rừng thông.',
    NULL,
    700000,
    1
),
(
    N'Phú Quốc',
    N'Phú Quốc',
    N'Việt Nam',
    N'Đảo du lịch nổi tiếng với các bãi biển và khu nghỉ dưỡng.',
    NULL,
    1200000,
    1
),
(
    N'Hà Nội',
    N'Hà Nội',
    N'Việt Nam',
    N'Thủ đô Việt Nam với nhiều di tích lịch sử và văn hóa ẩm thực.',
    NULL,
    850000,
    1
),
(
    N'Nha Trang',
    N'Nha Trang',
    N'Việt Nam',
    N'Thành phố biển với nhiều đảo đẹp và hoạt động nghỉ dưỡng.',
    NULL,
    900000,
    1
);
SELECT *
FROM Destinations;
-- Tìm điểm đến theo ngân sách
SELECT
    DestinationID,
    DestinationName,
    City,
    AverageDailyBudget
FROM Destinations
WHERE AverageDailyBudget <= 850000
  AND IsActive = 1
ORDER BY AverageDailyBudget ASC;
-- Tìm kiếm điểm đến theo tên
SELECT
    DestinationID,
    DestinationName,
    City,
    Description
FROM Destinations
WHERE DestinationName LIKE N'%Đà%'
  AND IsActive = 1;
-- Đếm tổng số điểm đến đang hoạt động
SELECT COUNT(*) AS TotalDestinations
FROM Destinations
WHERE IsActive = 1;