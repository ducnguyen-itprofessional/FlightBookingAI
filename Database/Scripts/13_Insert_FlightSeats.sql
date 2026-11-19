
USE FlightBookingAI;
GO

DECLARE @FlightID INT;
DECLARE @Row INT = 1;
DECLARE @LetterIndex INT;
DECLARE @SeatNumber VARCHAR(5);

-- Lấy mã chuyến bay VN220
SELECT @FlightID = FlightID
FROM Flights
WHERE FlightNumber = 'VN220';

IF @FlightID IS NULL
BEGIN
    PRINT N'Không tìm thấy chuyến bay VN220.';
END
ELSE
BEGIN
    -- Tạo 30 hàng ghế
    WHILE @Row <= 30
    BEGIN
        SET @LetterIndex = 1;

        -- Mỗi hàng gồm 6 ghế A-F
        WHILE @LetterIndex <= 6
        BEGIN
            SET @SeatNumber =
                CAST(@Row AS VARCHAR(3))
                + CHAR(64 + @LetterIndex);

            -- Tránh thêm ghế trùng
            IF NOT EXISTS
            (
                SELECT 1
                FROM FlightSeats
                WHERE FlightID = @FlightID
                  AND SeatNumber = @SeatNumber
            )
            BEGIN
                INSERT INTO FlightSeats
                    (FlightID, SeatNumber, Status)
                VALUES
                    (@FlightID, @SeatNumber, 'Available');
            END

            SET @LetterIndex = @LetterIndex + 1;
        END

        SET @Row = @Row + 1;
    END

    PRINT N'Đã tạo xong danh sách ghế VN220.';
END
GO

-- Kiểm tra số ghế vừa tạo
SELECT COUNT(*) AS TotalSeats
FROM FlightSeats
WHERE FlightID = (
    SELECT FlightID
    FROM Flights
    WHERE FlightNumber = 'VN220'
);
