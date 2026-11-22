-- SQL Server: accounts, transactions and a monthly statement report
CREATE TABLE dbo.Account (
    AccountId     INT IDENTITY PRIMARY KEY,
    AccountNo     VARCHAR(20)  NOT NULL UNIQUE,
    CustomerName  NVARCHAR(100) NOT NULL,
    OpenedOn      DATE NOT NULL
);
CREATE TABLE dbo.Txn (
    TxnId      BIGINT IDENTITY PRIMARY KEY,
    AccountId  INT NOT NULL REFERENCES dbo.Account(AccountId),
    TxnDate    DATETIME2 NOT NULL,
    Amount     DECIMAL(18,2) NOT NULL,
    TxnType    CHAR(1) NOT NULL CHECK (TxnType IN ('D','C'))
);
-- Covering index for "transactions of one account in a date range"
CREATE NONCLUSTERED INDEX IX_Txn_Account_Date ON dbo.Txn (AccountId, TxnDate) INCLUDE (Amount, TxnType);
GO

-- Running balance + rank with window functions
CREATE OR ALTER PROCEDURE dbo.usp_GetStatement
    @AccountNo VARCHAR(20), @From DATE, @To DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT t.TxnDate, t.TxnType, t.Amount,
           SUM(CASE t.TxnType WHEN 'C' THEN t.Amount ELSE -t.Amount END)
               OVER (ORDER BY t.TxnDate, t.TxnId ROWS UNBOUNDED PRECEDING) AS RunningBalance
    FROM dbo.Txn t
    JOIN dbo.Account a ON a.AccountId = t.AccountId
    WHERE a.AccountNo = @AccountNo AND t.TxnDate >= @From AND t.TxnDate < DATEADD(DAY, 1, @To)  -- SARGable range
    ORDER BY t.TxnDate, t.TxnId;
END
GO

-- Top 3 largest debits per account (CTE + ROW_NUMBER)
WITH ranked AS (
    SELECT AccountId, TxnId, Amount,
           ROW_NUMBER() OVER (PARTITION BY AccountId ORDER BY Amount DESC) AS rn
    FROM dbo.Txn WHERE TxnType = 'D'
)
SELECT * FROM ranked WHERE rn <= 3;

-- Safe transfer: transaction + error handling
CREATE OR ALTER PROCEDURE dbo.usp_Transfer @FromId INT, @ToId INT, @Amount DECIMAL(18,2)
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        INSERT dbo.Txn (AccountId, TxnDate, Amount, TxnType) VALUES (@FromId, SYSUTCDATETIME(), @Amount, 'D');
        INSERT dbo.Txn (AccountId, TxnDate, Amount, TxnType) VALUES (@ToId,   SYSUTCDATETIME(), @Amount, 'C');
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
