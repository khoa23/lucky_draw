-- =====================================================================================
-- Dữ liệu mẫu để chạy thử (database mới tạo bằng migration, ví dụ QS_Test).
-- Chạy:  sqlcmd -S .\SQLEXPRESS -d QS_Test -f 65001 -i Database\seed-sample-data.sql
-- KHÔNG chạy trên database thật: script xóa sạch dữ liệu quay số trước khi nạp.
-- Khách hàng được nạp qua đúng quy trình import (bảng tạm -> 2 procedure).
-- =====================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DELETE FROM CustomerReward;
DELETE FROM CustomerRewardHistory;
DELETE FROM Customers;
DELETE FROM CustomerInfo;
DELETE FROM Rewards;
DELETE FROM Programs;
DELETE FROM RewardTypes;
TRUNCATE TABLE temptable;

-- Loại giải
SET IDENTITY_INSERT RewardTypes ON;
INSERT RewardTypes (Id, Name, Description) VALUES
 (1, N'Lồng cầu', N'Quay số lồng cầu'),
 (2, N'Chọn ngẫu nhiên tuần tự', N'Quay số tự động từng giải chọn ngẫu nhiên một mã số'),
 (3, N'Chọn ngẫu nhiên liên tục', N'Quay số tự động tất cả giải thưởng chọn ngẫu nhiên các mã số'),
 (4, N'Lồng cầu các số cuối', N'Quay số lồng cầu các số cuối để chọn các mã số cùng trúng giải'),
 (5, N'Chọn ngẫu nhiên các số cuối', N'Quay số tự động ngẫu nhiên các số cuối để chọn các mã số cùng trúng giải');
SET IDENTITY_INSERT RewardTypes OFF;

-- Chương trình và giải thưởng
SET IDENTITY_INSERT Programs ON;
INSERT Programs (Id, ProgramName, DateStart) VALUES (1, N'Quay số cuối năm', CAST(GETDATE() AS date));
SET IDENTITY_INSERT Programs OFF;

INSERT Rewards (ProgramId, RewardName, NumberOfReward, RewardType, Idd, NumberLength, PrefixCode) VALUES
 (1, N'Giải khuyến khích', 5, 3, 1, 10, ''),
 (1, N'Giải ba',           3, 2, 2, 10, ''),
 (1, N'Giải nhì',          2, 2, 3, 10, ''),
 (1, N'Giải nhất',         1, 2, 4, 10, ''),
 (1, N'Giải đặc biệt',     1, 2, 5, 10, '');

-- 200 khách hàng mẫu (họ x tên đệm x tên), nạp vào bảng tạm như khi import file
;WITH ho AS (SELECT * FROM (VALUES (1,N'Nguyễn'),(2,N'Trần'),(3,N'Lê'),(4,N'Phạm'),(5,N'Hoàng')) v(i, x)),
      dem AS (SELECT * FROM (VALUES (1,N'Văn'),(2,N'Thị'),(3,N'Minh'),(4,N'Ngọc'),(5,N'Đức')) v(i, x)),
      ten AS (SELECT * FROM (VALUES (1,N'An'),(2,N'Bình'),(3,N'Cường'),(4,N'Dũng'),(5,N'Giang'),(6,N'Hà'),(7,N'Khánh'),(8,N'Lan')) v(i, x)),
      chiNhanh AS (SELECT * FROM (VALUES (0,'0101',N'Chi nhánh Hà Nội'),(1,'0202',N'Chi nhánh Đà Nẵng'),(2,'0303',N'Chi nhánh TP. Hồ Chí Minh'),(3,'0404',N'Chi nhánh Cần Thơ')) v(i, code, name)),
      kh AS (SELECT ROW_NUMBER() OVER (ORDER BY ho.i, dem.i, ten.i) AS n, ho.x + N' ' + dem.x + N' ' + ten.x AS FullName
             FROM ho CROSS JOIN dem CROSS JOIN ten)
INSERT temptable (CustomerId, CustomerCode, FullName, BranchCode, BranchName, AccountNo, IdNumber, Address, ClientNo, CardNo)
SELECT CAST(kh.n AS varchar(10)),
       RIGHT('0000000000' + CAST(1000000 + kh.n * 37 AS varchar(10)), 10),
       kh.FullName,
       cn.code, cn.name,
       'TK' + RIGHT('00000000' + CAST(kh.n AS varchar(10)), 8),
       '0012' + RIGHT('00000000' + CAST(kh.n AS varchar(10)), 8),
       N'Số ' + CAST(kh.n AS nvarchar(10)) + N' đường Lê Lợi',
       'C' + CAST(kh.n AS varchar(10)),
       '9704' + RIGHT('000000000000' + CAST(kh.n AS varchar(12)), 12)
FROM kh JOIN chiNhanh cn ON cn.i = kh.n % 4;

EXEC InsertCustomerInfoFromTempTable;
EXEC importdataFromCustomerInfo;

COMMIT TRANSACTION;

SELECT 'RewardTypes' AS Bang, COUNT(*) AS SoDong FROM RewardTypes
UNION ALL SELECT 'Programs', COUNT(*) FROM Programs
UNION ALL SELECT 'Rewards', COUNT(*) FROM Rewards
UNION ALL SELECT 'CustomerInfo', COUNT(*) FROM CustomerInfo
UNION ALL SELECT 'Customers', COUNT(*) FROM Customers;
