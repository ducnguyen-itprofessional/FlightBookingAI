USE FlightBookingAI;
GO

-- 1. Tìm và xóa ràng buộc CHECK cũ trên cột PaymentMethod của bảng dbo.Payments
DECLARE @ConstraintName NVARCHAR(200);
SELECT @ConstraintName = cc.name
FROM sys.check_constraints cc
INNER JOIN sys.columns c ON cc.parent_object_id = c.object_id AND cc.parent_column_id = c.column_id
WHERE cc.parent_object_id = OBJECT_ID('dbo.Payments')
  AND c.name = 'PaymentMethod';

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.Payments DROP CONSTRAINT [' + @ConstraintName + ']');
END
GO

-- 2. Tạo lại ràng buộc CHECK mới bao gồm cả 'ZaloPay'
ALTER TABLE dbo.Payments
ADD CONSTRAINT CK_Payments_PaymentMethod
CHECK (PaymentMethod IN ('VNPay', 'ZaloPay', 'MoMo', 'Simulation', 'Card', 'BankTransfer'));
GO