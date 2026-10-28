USE FlightBookingAI;
GO

CREATE TABLE Airports
(
    AirportID INT IDENTITY(1,1) PRIMARY KEY,
    AirportCode CHAR(3) NOT NULL UNIQUE,
    AirportName NVARCHAR(150) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    Country NVARCHAR(100) NOT NULL
);
GO
--Thêm dữ liệu
INSERT INTO Airports
    (AirportCode, AirportName, City, Country)
VALUES
    ('SGN', N'Sân bay quốc tế Tân Sơn Nhất',
     N'TP. Hồ Chí Minh', N'Việt Nam'),

    ('HAN', N'Sân bay quốc tế Nội Bài',
     N'Hà Nội', N'Việt Nam'),

    ('DAD', N'Sân bay quốc tế Đà Nẵng',
     N'Đà Nẵng', N'Việt Nam'),

    ('CXR', N'Sân bay quốc tế Cam Ranh',
     N'Khánh Hòa', N'Việt Nam'),

    ('PQC', N'Sân bay quốc tế Phú Quốc',
     N'Phú Quốc', N'Việt Nam');

     --Kiểm tra dữ liệu
     SELECT*
     FROM Airports;