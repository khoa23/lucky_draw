using lucky_draw.Data;
using lucky_draw.Models;
using ExcelDataReader;

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

            using var reader = ExcelReaderFactory.CreateReader(excelStream);
            var isFirstRow = true;
            while (reader.Read())
            {
                if (isFirstRow) { isFirstRow = false; continue; }
                var entity = new Temptable
                {
                    BRCD = reader.GetValue(0)?.ToString(),
                    LCLBRNM = reader.GetValue(1)?.ToString(),
                    FTRSERIES = reader.GetValue(2)?.ToString(),
                    LSTSERIES = reader.GetValue(3)?.ToString(),
                    IDXACNO = reader.GetValue(4)?.ToString(),
                    NM = reader.GetValue(5)?.ToString(),
                    IDNO = reader.GetValue(6)?.ToString(),
                    DIACHI = reader.GetValue(7)?.ToString(),
                    CUSTSEQ = reader.GetValue(8)?.ToString(),
                    CARDNO = reader.GetValue(9)?.ToString(),
                };
                _context.Temptable.Add(entity);
            }
            await _context.SaveChangesAsync();
        }
    }
}
