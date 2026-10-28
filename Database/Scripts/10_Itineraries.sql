USE FlightBookingAI;
GO

-- BẢNG 10: ITINERARIES
CREATE TABLE Itineraries
(
    ItineraryID INT IDENTITY(1,1) PRIMARY KEY,

    UserID INT NOT NULL,

    DestinationID INT NOT NULL,

    ItineraryName NVARCHAR(200) NOT NULL,

    StartDate DATE NOT NULL,

    EndDate DATE NOT NULL,

    NumberOfDays INT NOT NULL,

    Budget DECIMAL(18,2) NOT NULL,

    EstimatedCost DECIMAL(18,2) NULL,

    AIResponse NVARCHAR(MAX) NULL,

    Status VARCHAR(20) NOT NULL DEFAULT 'Draft',

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    -- Khóa ngoại đến Users
    CONSTRAINT FK_Itineraries_Users
        FOREIGN KEY (UserID)
        REFERENCES Users(UserID),

    -- Khóa ngoại đến Destinations
    CONSTRAINT FK_Itineraries_Destinations
        FOREIGN KEY (DestinationID)
        REFERENCES Destinations(DestinationID),

    -- Ngày kết thúc không được trước ngày bắt đầu
    CONSTRAINT CK_Itineraries_Date
        CHECK (EndDate >= StartDate),

    -- Số ngày phải lớn hơn 0
    CONSTRAINT CK_Itineraries_NumberOfDays
        CHECK (NumberOfDays > 0),

    -- Kiểm tra ngân sách
    CONSTRAINT CK_Itineraries_Budget
        CHECK (Budget > 0),

    -- Kiểm tra chi phí dự kiến
    CONSTRAINT CK_Itineraries_EstimatedCost
        CHECK (
            EstimatedCost IS NULL
            OR EstimatedCost >= 0
        ),

    -- Kiểm tra trạng thái
    CONSTRAINT CK_Itineraries_Status
        CHECK (Status IN (
            'Draft',
            'Confirmed',
            'Cancelled'
        ))
);
GO

-- Thêm lịch trình du lịch mẫu
INSERT INTO Itineraries
(
    UserID,
    DestinationID,
    ItineraryName,
    StartDate,
    EndDate,
    NumberOfDays,
    Budget,
    EstimatedCost,
    AIResponse,
    Status
)
SELECT
    U.UserID,
    D.DestinationID,
    N'Du lịch Đà Nẵng 3 ngày 2 đêm',
    '2026-11-15',
    '2026-11-17',
    3,
    5000000,
    4500000,
    N'Ngày 1: Tham quan Cầu Rồng. Ngày 2: Khám phá Bà Nà Hills. Ngày 3: Tự do tham quan và trở về.',
    'Draft'
FROM Users AS U
CROSS JOIN Destinations AS D
WHERE U.Email = 'nguyenvana@example.com'
  AND D.DestinationName = N'Đà Nẵng';

-- Kiểm tra lịch trình và thông tin liên quan
SELECT
    I.ItineraryID,
    I.ItineraryName,
    U.FullName AS CustomerName,
    D.DestinationName,
    I.StartDate,
    I.EndDate,
    I.NumberOfDays,
    I.Budget,
    I.EstimatedCost,
    I.Status
FROM Itineraries AS I

INNER JOIN Users AS U
    ON I.UserID = U.UserID

INNER JOIN Destinations AS D
    ON I.DestinationID = D.DestinationID

ORDER BY I.ItineraryID;