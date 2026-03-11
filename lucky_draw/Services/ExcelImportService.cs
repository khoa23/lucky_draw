using lucky_draw.Data;
using lucky_draw.Models;
using ExcelDataReader;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace lucky_draw.Services
{
    public class ExcelImportService
    {
        private readonly LuckyDrawDbContext _context;

        public ExcelImportService(LuckyDrawDbContext context)
        {
            _context = context;
        }

        public async Task ImportTemptableAsync(Stream excelStream)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // Clear temptable first
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE temptable");

            using var reader = ExcelReaderFactory.CreateReader(excelStream);
            
            // Tạo DataTable để chứa dữ liệu tạm thời trước khi BulkCopy
            var dt = new DataTable();
            dt.Columns.Add("BRCD");
            dt.Columns.Add("LCLBRNM");
            dt.Columns.Add("FTRSERIES");
            dt.Columns.Add("LSTSERIES");
            dt.Columns.Add("IDXACNO");
            dt.Columns.Add("NM");
            dt.Columns.Add("IDNO");
            dt.Columns.Add("DIACHI");
            dt.Columns.Add("CUSTSEQ");
            dt.Columns.Add("CARDNO");

            var batchSize = 100000; // Tăng lên 100.000 dòng mỗi đợt
            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Connection string not found.");
            }
            
            var isFirstRow = true;
            while (reader.Read())
            {
                if (isFirstRow) { isFirstRow = false; continue; }

                dt.Rows.Add(
                    reader.GetValue(0)?.ToString(),
                    reader.GetValue(1)?.ToString(),
                    reader.GetValue(2)?.ToString(),
                    reader.GetValue(3)?.ToString(),
                    reader.GetValue(4)?.ToString(),
                    reader.GetValue(5)?.ToString(),
                    reader.GetValue(6)?.ToString(),
                    reader.GetValue(7)?.ToString(),
                    reader.GetValue(8)?.ToString(),
                    reader.GetValue(9)?.ToString()
                );

                if (dt.Rows.Count >= batchSize)
                {
                    await PerformBulkCopy(dt, connectionString);
                    dt.Clear();
                }
            }

            if (dt.Rows.Count > 0)
            {
                await PerformBulkCopy(dt, connectionString);
            }

            // Tăng timeout cho các procedure vì dữ liệu lớn
            _context.Database.SetCommandTimeout(System.TimeSpan.FromMinutes(30));

            // Chạy procedure sau khi import
            await _context.Database.ExecuteSqlRawAsync("EXEC InsertCustomerInfoFromTempTable");
            await _context.Database.ExecuteSqlRawAsync("EXEC importdataFromCustomerInfo");
        }

        private async Task PerformBulkCopy(DataTable dt, string connectionString)
        {
            using (var bulkCopy = new SqlBulkCopy(connectionString))
            {
                bulkCopy.DestinationTableName = "Temptable"; // Tên bảng trong DB
                bulkCopy.BatchSize = 10000;
                bulkCopy.BulkCopyTimeout = 600; // 10 phút

                // Mapping các cột (nếu tên cột trong DT và DB khác nhau)
                bulkCopy.ColumnMappings.Add("BRCD", "BRCD");
                bulkCopy.ColumnMappings.Add("LCLBRNM", "LCLBRNM");
                bulkCopy.ColumnMappings.Add("FTRSERIES", "FTRSERIES");
                bulkCopy.ColumnMappings.Add("LSTSERIES", "LSTSERIES");
                bulkCopy.ColumnMappings.Add("IDXACNO", "IDXACNO");
                bulkCopy.ColumnMappings.Add("NM", "NM");
                bulkCopy.ColumnMappings.Add("IDNO", "IDNO");
                bulkCopy.ColumnMappings.Add("DIACHI", "DIACHI");
                bulkCopy.ColumnMappings.Add("CUSTSEQ", "CUSTSEQ");
                bulkCopy.ColumnMappings.Add("CARDNO", "CARDNO");

                await bulkCopy.WriteToServerAsync(dt);
            }
        }
    }
}

