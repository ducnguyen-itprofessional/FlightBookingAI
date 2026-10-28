USE FlightBookingAI;
GO 
--Tạo bảng Airlines 
CREATE TABLE Airlines 
(
	AirlineID INT IDENTITY(1,1) PRIMARY KEY,
	AirlineCode VARCHAR(5) NOT NULL UNIQUE,
	AirlineName NVARCHAR(150) NOT NULL,
	LogoURL NVARCHAR(500) NULL
);
GO 
--Thêm dữ liệu hãng hàng không 
INSERT INTO Airlines
	(AirlineCode, AirlineName, LogoURL)
VALUES 
	('VN', N'Vietnam Airlines', NULL),
	('VJ', N'Vietjet Air', NULL),
	('QH', N'Bamboo Airways', NULL);
GO
--Kiểm tra dữ liệu bảng Airlines 
SELECT*
FROM Airlines;