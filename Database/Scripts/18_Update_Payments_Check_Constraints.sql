
USE FlightBookingAI;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Cập nhật phương thức thanh toán
    ALTER TABLE dbo.Payments
    DROP CONSTRAINT CK_Payments_Method;

    ALTER TABLE dbo.Payments
    WITH CHECK ADD CONSTRAINT CK_Payments_Method
    CHECK (
        PaymentMethod IN (
            'BankTransfer',
            'VNPay',
            'MoMo',
            'Simulation'
        )
    );

    -- 2. Cập nhật trạng thái thanh toán
    ALTER TABLE dbo.Payments
    DROP CONSTRAINT CK_Payments_Status;

    ALTER TABLE dbo.Payments
    WITH CHECK ADD CONSTRAINT CK_Payments_Status
    CHECK (
        PaymentStatus IN (
            'Refunded',
            'Failed',
            'Success',
            'Pending',
            'Completed'
        )
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
