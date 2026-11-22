
USE FlightBookingAI;
GO

CREATE UNIQUE NONCLUSTERED INDEX
    UX_Payments_BookingID_Completed
ON dbo.Payments (BookingID)
WHERE PaymentStatus = 'Completed';
GO
