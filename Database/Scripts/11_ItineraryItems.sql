USE FlightBookingAI;
GO

-- BẢNG 11: ITINERARY ITEMS
CREATE TABLE ItineraryItems
(
    ItineraryItemID INT IDENTITY(1,1) PRIMARY KEY,

    ItineraryID INT NOT NULL,

    AttractionID INT NULL,

    DayNumber INT NOT NULL,

    StartTime TIME NULL,

    EndTime TIME NULL,

    ActivityName NVARCHAR(200) NOT NULL,

    Description NVARCHAR(MAX) NULL,

    EstimatedCost DECIMAL(18,2) NOT NULL DEFAULT 0,

    SortOrder INT NOT NULL DEFAULT 1,

    -- Khóa ngoại đến Itineraries
    CONSTRAINT FK_ItineraryItems_Itineraries
        FOREIGN KEY (ItineraryID)
        REFERENCES Itineraries(ItineraryID),

    -- Khóa ngoại đến Attractions
    CONSTRAINT FK_ItineraryItems_Attractions
        FOREIGN KEY (AttractionID)
        REFERENCES Attractions(AttractionID),

    -- Ngày thứ mấy phải lớn hơn 0
    CONSTRAINT CK_ItineraryItems_DayNumber
        CHECK (DayNumber > 0),

    -- Kiểm tra thời gian
    CONSTRAINT CK_ItineraryItems_Time
        CHECK (
            StartTime IS NULL
            OR EndTime IS NULL
            OR EndTime > StartTime
        ),

    -- Chi phí không được âm
    CONSTRAINT CK_ItineraryItems_Cost
        CHECK (EstimatedCost >= 0),

    -- Thứ tự hoạt động phải lớn hơn 0
    CONSTRAINT CK_ItineraryItems_SortOrder
        CHECK (SortOrder > 0)
);
GO

-- Thêm hoạt động ngày 1
INSERT INTO ItineraryItems
(
    ItineraryID,
    AttractionID,
    DayNumber,
    StartTime,
    EndTime,
    ActivityName,
    Description,
    EstimatedCost,
    SortOrder
)
SELECT
    I.ItineraryID,
    A.AttractionID,
    1,
    '18:00',
    '19:00',
    N'Tham quan Cầu Rồng',
    N'Tham quan Cầu Rồng và khu vực trung tâm Đà Nẵng.',
    0,
    1
FROM Itineraries AS I
CROSS JOIN Attractions AS A
WHERE I.ItineraryName = N'Du lịch Đà Nẵng 3 ngày 2 đêm'
  AND A.AttractionName = N'Cầu Rồng'
  AND A.DestinationID = I.DestinationID;


-- Thêm hoạt động ngày 2
INSERT INTO ItineraryItems
(
    ItineraryID,
    AttractionID,
    DayNumber,
    StartTime,
    EndTime,
    ActivityName,
    Description,
    EstimatedCost,
    SortOrder
)
SELECT
    I.ItineraryID,
    A.AttractionID,
    2,
    '08:00',
    '14:00',
    N'Khám phá Bà Nà Hills',
    N'Tham quan Bà Nà Hills, Cầu Vàng và khu vui chơi.',
    900000,
    1
FROM Itineraries AS I
CROSS JOIN Attractions AS A
WHERE I.ItineraryName = N'Du lịch Đà Nẵng 3 ngày 2 đêm'
  AND A.AttractionName = N'Bà Nà Hills'
  AND A.DestinationID = I.DestinationID;


-- Thêm hoạt động ngày 3
INSERT INTO ItineraryItems
(
    ItineraryID,
    AttractionID,
    DayNumber,
    StartTime,
    EndTime,
    ActivityName,
    Description,
    EstimatedCost,
    SortOrder
)
SELECT
    I.ItineraryID,
    NULL,
    3,
    '09:00',
    '11:00',
    N'Tham quan tự do',
    N'Tự do tham quan và chuẩn bị kết thúc chuyến đi.',
    0,
    1
FROM Itineraries AS I
WHERE I.ItineraryName = N'Du lịch Đà Nẵng 3 ngày 2 đêm';

-- Kiểm tra chi tiết lịch trình du lịch
SELECT
    I.ItineraryName,
    II.DayNumber,
    II.ActivityName,
    A.AttractionName,
    II.StartTime,
    II.EndTime,
    II.EstimatedCost
FROM ItineraryItems AS II

INNER JOIN Itineraries AS I
    ON II.ItineraryID = I.ItineraryID

LEFT JOIN Attractions AS A
    ON II.AttractionID = A.AttractionID

WHERE I.ItineraryName =
    N'Du lịch Đà Nẵng 3 ngày 2 đêm'

ORDER BY
    II.DayNumber,
    II.SortOrder,
    II.StartTime;