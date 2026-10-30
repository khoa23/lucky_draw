using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lucky_draw.Migrations
{
    // Migration mốc (gộp từ các migration trước): tạo toàn bộ bảng, index và stored procedure dùng khi import.
    // - Database mới: "dotnet ef database update" (hoặc chạy Database/migrations.sql).
    // - Database đã có đủ cấu trúc này: chỉ cần ghi nhận mốc bằng Database/mark-initial-create.sql.
    // Lưu ý: database không có khóa ngoại nên các khóa ngoại EF sinh ra đã được bỏ khỏi migration này
    // (model vẫn giữ quan hệ để dùng Include/navigation). Muốn thêm khóa ngoại thì viết migration riêng
    // bằng migrationBuilder.AddForeignKey, sau khi kiểm tra procedure import không vi phạm ràng buộc.
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CustomerCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ClientNo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BranchCode = table.Column<string>(type: "char(4)", unicode: false, fixedLength: true, maxLength: 4, nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AccountNo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IdNo = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    AddNo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TelNo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Amount = table.Column<decimal>(type: "money", nullable: true),
                    Time = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRewardHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: true),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    RewardId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ResultCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime", nullable: true),
                    NumberOfCustomer = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CustomerCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers_1", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateCreated = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    DateStart = table.Column<DateTime>(type: "datetime", nullable: true),
                    ProgramName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProgramFilePath = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    NumberSize = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardTypes",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "ntext", nullable: true),
                    RewardOption = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "temptable",
                columns: table => new
                {
                    BranchCode = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CustomerId = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    CustomerCode = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    AccountNo = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IdNumber = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ClientNo = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    CardNo = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    RewardName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumberOfReward = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((0))"),
                    RewardType = table.Column<byte>(type: "tinyint", nullable: true, defaultValueSql: "((0))"),
                    PrefixCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Idd = table.Column<byte>(type: "tinyint", nullable: true),
                    AllowPreviousResultCode = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    AllowMultiResultCode = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    NumberLength = table.Column<byte>(type: "tinyint", nullable: true),
                    RelationRewardId = table.Column<int>(type: "int", nullable: true),
                    MaxWheelTime = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReward",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    RewardId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ResultCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    NumberOfCustomer = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReward", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInfo_CustomerCode",
                table: "CustomerInfo",
                column: "CustomerCode")
                .Annotation("SqlServer:Include", new[] { "IdNo", "TelNo" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReward_CustomerId",
                table: "CustomerReward",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReward_ProgramId",
                table: "CustomerReward",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReward_RewardId",
                table: "CustomerReward",
                column: "RewardId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers",
                column: "CustomerCode");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_ProgramId",
                table: "Rewards",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_RewardType",
                table: "Rewards",
                column: "RewardType");

            // Stored procedure dùng khi import danh sách khách hàng (ExcelImportService)
            migrationBuilder.Sql(@"EXEC(N'
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
  FROM [dbo].[temptable]')");

            migrationBuilder.Sql(@"EXEC(N'
CREATE Proc [dbo].[importdataFromCustomerInfo]
as
begin

	insert into Customers
	SELECT a.Id, a.CustomerCode, a.[Name], a.[Status]
	FROM CustomerInfo a
	where a.CustomerCode not in (select CustomerCode from Customers)
end')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[importdataFromCustomerInfo]");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[InsertCustomerInfoFromTempTable]");

            migrationBuilder.DropTable(
                name: "CustomerInfo");

            migrationBuilder.DropTable(
                name: "CustomerReward");

            migrationBuilder.DropTable(
                name: "CustomerRewardHistory");

            migrationBuilder.DropTable(
                name: "temptable");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Rewards");

            migrationBuilder.DropTable(
                name: "Programs");

            migrationBuilder.DropTable(
                name: "RewardTypes");
        }
    }
}
