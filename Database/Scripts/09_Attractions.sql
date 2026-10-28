USE FlightBookingAI;
GO

-- BẢNG 9: ATTRACTIONS
CREATE TABLE Attractions
(
    AttractionID INT IDENTITY(1,1) PRIMARY KEY,

    DestinationID INT NOT NULL,

    AttractionName NVARCHAR(150) NOT NULL,

    Description NVARCHAR(MAX) NULL,

    Address NVARCHAR(255) NULL,

    TicketPrice DECIMAL(18,2) NOT NULL DEFAULT 0,

    EstimatedDuration INT NULL,

    ImageURL NVARCHAR(500) NULL,

    IsActive BIT NOT NULL DEFAULT 1,

    -- Khóa ngoại liên kết Destinations
    CONSTRAINT FK_Attractions_Destinations
        FOREIGN KEY (DestinationID)
        REFERENCES Destinations(DestinationID),

    -- Kiểm tra giá vé
    CONSTRAINT CK_Attractions_TicketPrice
        CHECK (TicketPrice >= 0),

    -- Kiểm tra thời gian tham quan
    CONSTRAINT CK_Attractions_Duration
        CHECK (
            EstimatedDuration IS NULL
            OR EstimatedDuration > 0
        )
);
GO
-- Thêm 10 địa điểm tham quan mẫu
INSERT INTO Attractions
(
    DestinationID,
    AttractionName,
    Description,
    Address,
    TicketPrice,
    EstimatedDuration,
    ImageURL,
    IsActive
)
VALUES
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Đà Nẵng'),
    N'Bà Nà Hills',
    N'Khu du lịch nổi tiếng với Cầu Vàng và nhiều hoạt động tham quan.',
    N'Hòa Vang, Đà Nẵng',
    900000,
    360,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Đà Nẵng'),
    N'Cầu Rồng',
    N'Cây cầu biểu tượng của thành phố Đà Nẵng.',
    N'Trung tâm Đà Nẵng',
    0,
    60,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Đà Lạt'),
    N'Hồ Xuân Hương',
    N'Hồ nước nổi tiếng ở trung tâm thành phố Đà Lạt.',
    N'Trung tâm Đà Lạt',
    0,
    90,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Đà Lạt'),
    N'Thung lũng Tình Yêu',
    N'Khu tham quan với cảnh quan thiên nhiên và nhiều hoạt động.',
    N'Đà Lạt, Lâm Đồng',
    250000,
    180,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Phú Quốc'),
    N'Bãi Sao',
    N'Bãi biển nổi tiếng với cát trắng và nước biển trong xanh.',
    N'Phú Quốc',
    0,
    180,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Phú Quốc'),
    N'Grand World Phú Quốc',
    N'Khu vui chơi và tham quan với nhiều hoạt động giải trí.',
    N'Phú Quốc',
    0,
    180,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Hà Nội'),
    N'Hồ Hoàn Kiếm',
    N'Địa điểm tham quan nổi tiếng tại trung tâm Hà Nội.',
    N'Hoàn Kiếm, Hà Nội',
    0,
    90,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Hà Nội'),
    N'Văn Miếu - Quốc Tử Giám',
    N'Di tích lịch sử và văn hóa nổi tiếng của Hà Nội.',
    N'Đống Đa, Hà Nội',
    70000,
    120,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Nha Trang'),
    N'Tháp Bà Ponagar',
    N'Di tích kiến trúc và văn hóa Chăm.',
    N'Nha Trang, Khánh Hòa',
    30000,
    90,
    NULL,
    1
),
(
    (SELECT DestinationID FROM Destinations
     WHERE DestinationName = N'Nha Trang'),
    N'Hòn Chồng',
    N'Địa điểm tham quan ven biển với cảnh quan đá tự nhiên.',
    N'Nha Trang, Khánh Hòa',
    30000,
    90,
    NULL,
    1
);
-- Hiển thị địa điểm tham quan và điểm đến
SELECT
    A.AttractionID,
    A.AttractionName,
    D.DestinationName,
    D.City,
    A.TicketPrice,
    A.EstimatedDuration,
    A.IsActive
FROM Attractions AS A

INNER JOIN Destinations AS D
    ON A.DestinationID = D.DestinationID

ORDER BY
    D.DestinationName,
    A.AttractionName;

    -- Tìm địa điểm tham quan theo ngân sách
SELECT
    A.AttractionName,
    D.DestinationName,
    A.TicketPrice,
    A.EstimatedDuration
FROM Attractions AS A

INNER JOIN Destinations AS D
    ON A.DestinationID = D.DestinationID

WHERE D.DestinationName = N'Đà Nẵng'
  AND A.TicketPrice <= 300000
  AND A.IsActive = 1

ORDER BY A.TicketPrice ASC;