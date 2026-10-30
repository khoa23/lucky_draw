IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE TABLE [CustomerInfo] (
        [Id] int NOT NULL,
        [CustomerCode] varchar(50) NOT NULL,
        [ClientNo] varchar(20) NULL,
        [Name] nvarchar(255) NOT NULL,
        [BranchCode] char(4) NULL,
        [BranchName] nvarchar(255) NULL,
        [AccountNo] varchar(50) NULL,
        [IdNo] varchar(255) NULL,
        [AddNo] nvarchar(255) NULL,
        [TelNo] nvarchar(255) NULL,
        [Kind] nvarchar(255) NULL,
        [Amount] money NULL,
        [Time] datetime NULL,
        [Status] bit NULL,
        CONSTRAINT [PK_CustomerInfo] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE TABLE [CustomerRewardHistory] (
        [Id] int NULL,
        [ProgramId] int NOT NULL,
        [RewardId] int NOT NULL,
        [CustomerId] int NULL,
        [ResultCode] nvarchar(50) NULL,
        [TimeStamp] datetime NULL,
        [NumberOfCustomer] int NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL,
        [CustomerCode] varchar(30) NULL,
        [Name] nvarchar(255) NOT NULL,
        [Status] tinyint NULL,
        CONSTRAINT [PK_Customers_1] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE TABLE [Programs] (
        [Id] int NOT NULL IDENTITY,
        [DateCreated] datetime NULL DEFAULT ((getdate())),
        [DateStart] datetime NULL,
        [ProgramName] nvarchar(255) NULL,
        [ProgramFilePath] varchar(255) NULL,
        [NumberSize] tinyint NULL,
        CONSTRAINT [PK_Programs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE TABLE [RewardTypes] (
        [Id] tinyint NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Description] ntext NULL,
        [RewardOption] nvarchar(255) NULL,
        CONSTRAINT [PK_RewardTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE TABLE [temptable] (
        [BranchCode] varchar(250) NULL,
        [BranchName] nvarchar(250) NULL,
        [CustomerId] varchar(250) NULL,
        [CustomerCode] varchar(250) NULL,
        [AccountNo] varchar(250) NULL,
        [FullName] nvarchar(250) NULL,
        [IdNumber] varchar(250) NULL,
        [Address] nvarchar(250) NULL,
        [ClientNo] varchar(250) NULL,
        [CardNo] varchar(250) NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE TABLE [Rewards] (
        [Id] int NOT NULL IDENTITY,
        [ProgramId] int NOT NULL,
        [RewardName] nvarchar(50) NOT NULL,
        [NumberOfReward] int NOT NULL DEFAULT (((0))),
        [RewardType] tinyint NULL DEFAULT (((0))),
        [PrefixCode] varchar(20) NULL,
        [Idd] tinyint NULL,
        [AllowPreviousResultCode] bit NULL DEFAULT (((0))),
        [AllowMultiResultCode] bit NULL DEFAULT (((0))),
        [NumberLength] tinyint NULL,
        [RelationRewardId] int NULL,
        [MaxWheelTime] tinyint NULL,
        CONSTRAINT [PK_Rewards] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE TABLE [CustomerReward] (
        [Id] int NOT NULL IDENTITY,
        [ProgramId] int NOT NULL,
        [RewardId] int NOT NULL,
        [CustomerId] int NULL,
        [ResultCode] nvarchar(50) NULL,
        [TimeStamp] datetime NULL DEFAULT ((getdate())),
        [NumberOfCustomer] int NULL,
        CONSTRAINT [PK_CustomerReward] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerInfo_CustomerCode] ON [CustomerInfo] ([CustomerCode]) INCLUDE ([IdNo], [TelNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerReward_CustomerId] ON [CustomerReward] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerReward_ProgramId] ON [CustomerReward] ([ProgramId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CustomerReward_RewardId] ON [CustomerReward] ([RewardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_CustomerCode] ON [Customers] ([CustomerCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Rewards_ProgramId] ON [Rewards] ([ProgramId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Rewards_RewardType] ON [Rewards] ([RewardType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    EXEC(N'
    CREATE PROC [dbo].[InsertCustomerInfoFromTempTable]
    AS
    insert into CustomerInfo
    (Id,
    CustomerCode,
    ClientNo,
    Name,
    BranchCode,
    BranchName,
    AccountNo,
    IdNo,
    AddNo
    )
    SELECT
    [CustomerId] AS Id
    ,[CustomerCode]
    ,[ClientNo]
    ,[FullName] AS Name
    ,[BranchCode]
    ,[BranchName]
    ,[AccountNo]
    ,[IdNumber] AS IdNo
    ,[Address] AS AddNo
      FROM [dbo].[temptable]')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    EXEC(N'
    CREATE Proc [dbo].[importdataFromCustomerInfo]
    as
    begin

    	insert into Customers
    	SELECT a.Id, a.CustomerCode, a.[Name], a.[Status]
    	FROM CustomerInfo a
    	where a.CustomerCode not in (select CustomerCode from Customers)
    end')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030015402_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030015402_InitialCreate', N'9.0.5');
END;

COMMIT;
GO

