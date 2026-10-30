-- =====================================================================================
-- Ghi nhận migration mốc "InitialCreate" cho database ĐÃ CÓ SẴN đủ cấu trúc hiện tại
-- (database đã chạy hết các migration cũ trước khi gộp).
--
-- - Chỉ sửa bảng __EFMigrationsHistory: xóa tên các migration cũ đã gộp, ghi tên migration mốc.
-- - Không đổi bảng/dữ liệu. An toàn khi chạy lại nhiều lần.
-- - Tự kiểm tra cấu trúc trước: database chưa đủ cấu trúc mới thì dừng, không ghi gì.
-- - Database MỚI thì không dùng file này: chạy "dotnet ef database update" hoặc Database/migrations.sql.
-- =====================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH('dbo.CustomerInfo', 'ClientNo') IS NULL
   OR COL_LENGTH('dbo.temptable', 'CustomerId') IS NULL
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customers_CustomerCode' AND object_id = OBJECT_ID(N'dbo.Customers'))
   OR OBJECT_ID(N'dbo.InsertCustomerInfoFromTempTable', N'P') IS NULL
   OR OBJECT_ID(N'dbo.importdataFromCustomerInfo', N'P') IS NULL
BEGIN
    RAISERROR(N'Database chưa có đủ cấu trúc hiện tại (tên cột mới, index, procedure). Không ghi nhận migration.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );

-- Các migration cũ đã được gộp vào InitialCreate
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] IN (
    N'20261029125555_InitialBaseline',
    N'20261029132324_FixTemptableUnicode',
    N'20261029171402_AddCustomerCodeIndex',
    N'20261030014728_RenameColumnsToEnglish');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261030015402_InitialCreate')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030015402_InitialCreate', N'9.0.5');

COMMIT TRANSACTION;
PRINT N'Đã ghi nhận migration mốc InitialCreate.';
