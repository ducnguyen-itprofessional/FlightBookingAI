USE FlightBookingAI;
GO

-- Tạo bảng Flights
CREATE TABLE Flights
(
    FlightID INT IDENTITY(1,1) PRIMARY KEY,
    FlightNumber VARCHAR(20) NOT NULL,
    AirlineID INT NOT NULL,
    DepartureAirportID INT NOT NULL,
    ArrivalAirportID INT NOT NULL,
    DepartureTime DATETIME2 NOT NULL,
    ArrivalTime DATETIME2 NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    TotalSeats INT NOT NULL,
    AvailableSeats INT NOT NULL,

    Status VARCHAR(20) NOT NULL
        DEFAULT 'Scheduled',

    -- Khóa ngoại đến bảng Airlines
    CONSTRAINT FK_Flights_Airlines
        FOREIGN KEY (AirlineID)
        REFERENCES Airlines(AirlineID),

    -- Khóa ngoại sân bay khởi hành
    CONSTRAINT FK_Flights_DepartureAirport
        FOREIGN KEY (DepartureAirportID)
        REFERENCES Airports(AirportID),

    -- Khóa ngoại sân bay đến
    CONSTRAINT FK_Flights_ArrivalAirport
        FOREIGN KEY (ArrivalAirportID)
        REFERENCES Airports(AirportID),

    -- Các ràng buộc kiểm tra dữ liệu
    CONSTRAINT CK_Flights_DifferentAirports
        CHECK (DepartureAirportID <> ArrivalAirportID),

    CONSTRAINT CK_Flights_ValidTime
        CHECK (ArrivalTime > DepartureTime),

    CONSTRAINT CK_Flights_Price
        CHECK (Price >= 0),

    CONSTRAINT CK_Flights_Seats
        CHECK (
            TotalSeats > 0
            AND AvailableSeats >= 0
            AND AvailableSeats <= TotalSeats
        ),

    CONSTRAINT CK_Flights_Status
        CHECK (Status IN (
            'Scheduled',
            'Delayed',
            'Cancelled',
            'Completed'
        ))
);
GO

-- Kiểm tra bảng đã được tạo hay chưa
SELECT *
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'Flights';

SELECT AirlineID, AirlineCode, AirlineName
FROM Airlines;

SELECT AirportID, AirportCode, AirportName
FROM Airports;

INSERT INTO Flights
(
    FlightNumber,
    AirlineID,
    DepartureAirportID,
    ArrivalAirportID,
    DepartureTime,
    ArrivalTime,
    Price,
    TotalSeats,
    AvailableSeats,
    Status
)
VALUES
(
    'VN220',

    (SELECT AirlineID FROM Airlines
     WHERE AirlineCode = 'VN'),

    (SELECT AirportID FROM Airports
     WHERE AirportCode = 'SGN'),

    (SELECT AirportID FROM Airports
     WHERE AirportCode = 'HAN'),

    '2026-11-15T08:00:00',
    '2026-11-15T10:10:00',

    1500000,
    180,
    180,
    'Scheduled'
);
SELECT* 
FROM Flights;

-- Hiển thị thông tin chuyến bay đầy đủ
SELECT
    F.FlightID,
    F.FlightNumber,
    AL.AirlineName,
    AP1.AirportName AS DepartureAirport,
    AP2.AirportName AS ArrivalAirport,
    F.DepartureTime,
    F.ArrivalTime,
    F.Price,
    F.AvailableSeats,
    F.Status
FROM Flights AS F

INNER JOIN Airlines AS AL
    ON F.AirlineID = AL.AirlineID

INNER JOIN Airports AS AP1
    ON F.DepartureAirportID = AP1.AirportID

INNER JOIN Airports AS AP2
    ON F.ArrivalAirportID = AP2.AirportID;