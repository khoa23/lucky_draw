using lucky_draw.Data;
using ExcelDataReader;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Globalization;
using System.Text;

namespace lucky_draw.Services
{
    // Lỗi cấu trúc file import (thiếu tiêu đề, thiếu cột bắt buộc...): thông báo được hiển thị nguyên văn cho người dùng
    public class ImportFormatException : Exception
    {
        public ImportFormatException(string message) : base(message) { }
    }

    public class ExcelImportService
    {
        // Tên cột chuẩn: trùng tên cột trong bảng temptable và là tiêu đề cột của file mẫu tải về
        public static readonly string[] ColumnNames =
            { "BranchCode", "BranchName", "CustomerId", "CustomerCode", "AccountNo", "FullName", "IdNumber", "Address", "ClientNo", "CardNo" };

        // Cột procedure InsertCustomerInfoFromTempTable cần (Id, CustomerCode, Name trong CustomerInfo không cho NULL)
        private static readonly string[] RequiredColumns = { "CustomerId", "CustomerCode", "FullName" };

        // Tiêu đề cũ vẫn được chấp nhận để các file đã chuẩn bị theo mẫu cũ import được
        private static readonly Dictionary<string, string> LegacyHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BRCD"] = "BranchCode",
            ["LCLBRNM"] = "BranchName",
            ["FTRSERIES"] = "CustomerId",
            ["LSTSERIES"] = "CustomerCode",
            ["IDXACNO"] = "AccountNo",
            ["NM"] = "FullName",
            ["IDNO"] = "IdNumber",
            ["DIACHI"] = "Address",
            ["CUSTSEQ"] = "ClientNo",
            ["CARDNO"] = "CardNo"
        };

        private const int BatchSize = 20000; // Số dòng mỗi đợt BulkCopy: nhỏ để giữ RAM thấp trên máy yếu

        private readonly LuckyDrawDbContext _context;
        private readonly IMemoryCache _cache;

        static ExcelImportService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public ExcelImportService(LuckyDrawDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // Hỗ trợ .xlsx/.xls (nhiều sheet, mỗi sheet Excel tối đa ~1 triệu dòng) và .csv (không giới hạn số dòng).
        // Cột được ghép theo TÊN tiêu đề (không phụ thuộc thứ tự, cột thừa bị bỏ qua).
        // Dữ liệu được đọc tuần tự và đẩy theo lô => bộ nhớ không tăng theo kích thước file.
        public async Task<long> ImportTemptableAsync(Stream fileStream, string fileName)
        {
            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Connection string not found.");
            }

            // Tăng timeout cho các procedure vì dữ liệu lớn
            _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));

            // Clear temptable first
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE temptable");

            using var reader = Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? ExcelReaderFactory.CreateCsvReader(fileStream, new ExcelReaderConfiguration { FallbackEncoding = Encoding.UTF8 })
                : ExcelReaderFactory.CreateReader(fileStream);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // TableLock: ghi hàng loạt nhanh hơn nhiều khi nạp hàng triệu dòng
            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, null)
            {
                DestinationTableName = "temptable",
                BatchSize = 10000,
                BulkCopyTimeout = 0 // Không giới hạn thời gian cho mỗi lô
            };
            foreach (var column in ColumnNames)
            {
                bulkCopy.ColumnMappings.Add(column, column);
            }

            var dt = new DataTable();
            foreach (var column in ColumnNames)
            {
                dt.Columns.Add(column);
            }

            long totalRows = 0;
            int[]? columnMap = null; // columnMap[k] = vị trí trong file của cột chuẩn ColumnNames[k] (-1: file không có)
            var sheetNo = 0;
            do
            {
                sheetNo++;
                var isFirstRow = true;
                while (reader.Read())
                {
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        var header = ReadHeader(reader);
                        if (header != null)
                        {
                            EnsureRequiredColumns(header, sheetNo);
                            columnMap = header;
                            continue;
                        }
                        // Sheet sau không có dòng tiêu đề: dùng cách ghép cột của sheet trước
                        if (columnMap == null)
                            throw new ImportFormatException(
                                "Dòng đầu tiên của file phải là dòng tiêu đề cột. Vui lòng dùng file mẫu tải từ trang Import.");
                    }

                    var values = new object?[ColumnNames.Length];
                    var hasValue = false;
                    for (int k = 0; k < values.Length; k++)
                    {
                        var source = columnMap![k];
                        if (source < 0 || source >= reader.FieldCount) continue;
                        var text = Convert.ToString(reader.GetValue(source), CultureInfo.InvariantCulture);
                        values[k] = text;
                        hasValue |= !string.IsNullOrWhiteSpace(text);
                    }
                    if (!hasValue) continue; // Bỏ qua dòng trống cuối sheet

                    dt.Rows.Add(values);
                    if (dt.Rows.Count >= BatchSize)
                    {
                        await bulkCopy.WriteToServerAsync(dt);
                        totalRows += dt.Rows.Count;
                        dt.Clear();
                    }
                }
            } while (reader.NextResult());

            if (dt.Rows.Count > 0)
            {
                await bulkCopy.WriteToServerAsync(dt);
                totalRows += dt.Rows.Count;
            }

            // Chạy procedure sau khi import
            await _context.Database.ExecuteSqlRawAsync("EXEC InsertCustomerInfoFromTempTable");
            await _context.Database.ExecuteSqlRawAsync("EXEC importdataFromCustomerInfo");

            // Danh sách khách hàng đã thay đổi => đếm lại tổng số khách tham gia
            ParticipantService.InvalidateTotal(_cache);

            return totalRows;
        }

        // Đọc dòng tiêu đề (chấp nhận tên mới và tên cũ, không phân biệt hoa thường).
        // Trả về vị trí trong file của từng cột chuẩn, hoặc null nếu dòng này không có tiêu đề nào nhận ra được.
        private static int[]? ReadHeader(IExcelDataReader reader)
        {
            var map = Enumerable.Repeat(-1, ColumnNames.Length).ToArray();
            var found = false;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var header = Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)?.Trim();
                if (string.IsNullOrEmpty(header)) continue;

                var name = LegacyHeaders.TryGetValue(header, out var newName) ? newName : header;
                var k = Array.FindIndex(ColumnNames, c => c.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (k >= 0 && map[k] < 0)
                {
                    map[k] = i;
                    found = true;
                }
            }
            return found ? map : null;
        }

        private static void EnsureRequiredColumns(int[] map, int sheetNo)
        {
            var missing = RequiredColumns.Where(c => map[Array.IndexOf(ColumnNames, c)] < 0).ToList();
            if (missing.Count > 0)
                throw new ImportFormatException(
                    $"Sheet {sheetNo} thiếu cột bắt buộc: {string.Join(", ", missing)}. " +
                    $"Các cột của file mẫu: {string.Join(", ", ColumnNames)}.");
        }
    }
}
