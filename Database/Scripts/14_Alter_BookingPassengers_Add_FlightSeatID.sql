USE FlightBookingAI;
GO

-- 1. Thêm cột FlightSeatID vào BookingPassengers
ALTER TABLE BookingPassengers
ADD FlightSeatID INT NULL;
GO

-- 2. Tạo khóa ngoại liên kết với FlightSeats
ALTER TABLE BookingPassengers
ADD CONSTRAINT FK_BookingPassengers_FlightSeats
FOREIGN KEY (FlightSeatID)
REFERENCES FlightSeats(FlightSeatID);
GO

-- 3. Ngăn hai hành khách cùng sử dụng một ghế
CREATE UNIQUE INDEX UX_BookingPassengers_FlightSeatID
ON BookingPassengers(FlightSeatID)
WHERE FlightSeatID IS NOT NULL;
GO
