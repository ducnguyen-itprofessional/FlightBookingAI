
USE FlightBookingAI;
GO

-- BOOK-01C: Bo sung ma yeu cau dat ve
-- Chi them cot neu chua ton tai
IF COL_LENGTH('dbo.Bookings', 'BookingRequestId') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings
    ADD BookingRequestId UNIQUEIDENTIFIER NULL;
END;
GO

-- Tao Unique Index de ngan trung BookingRequestId
-- Cho phep cac Booking cu co gia tri NULL
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_Bookings_BookingRequestId'
      AND object_id = OBJECT_ID('dbo.Bookings')
)
BEGIN
    CREATE UNIQUE INDEX UX_Bookings_BookingRequestId
    ON dbo.Bookings(BookingRequestId)
    WHERE BookingRequestId IS NOT NULL;
END;
GO
