using UglyToad.PdfPig;
using System.Text;
using Xceed.Words.NET;
using ClosedXML.Excel;
using Tesseract;

public static class FileReaderService
{
    public static string ExtractText(string fileName, byte[] fileBytes)
    {
        var ext = Path.GetExtension(fileName).ToLower();

        return ext switch
        {
            ".pdf" => ExtractFromPdf(fileBytes),
            ".txt" => Encoding.UTF8.GetString(fileBytes),
            ".docx" => ExtractFromDocx(fileBytes),
            ".xlsx" => ExtractFromExcel(fileBytes),
            ".csv" => Encoding.UTF8.GetString(fileBytes),
            ".jpg" or ".jpeg" or ".png" => ExtractFromImage(fileBytes),
            _ => "[❌ Unsupported file type]"
        };

    }

    private static string ExtractFromPdf(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var doc = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages()) sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractFromDocx(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var doc = DocX.Load(ms);
        return doc.Text;
    }
    
    private static string ExtractFromExcel(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sb = new StringBuilder();
        foreach (var ws in workbook.Worksheets)
        {
            sb.AppendLine($"Sheet: {ws.Name}");
            foreach (var row in ws.RangeUsed().Rows())
            {
                sb.AppendLine(string.Join("\t", row.Cells().Select(c => c.GetValue<string>())));
            }
        }
        return sb.ToString();
    }

    private static string ExtractFromImage(byte[] imageBytes)
{
    using var engine = new TesseractEngine(@"./tessdata", "eng+tha", EngineMode.Default);
    using var img = Pix.LoadFromMemory(imageBytes);
    using var page = engine.Process(img);
    return page.GetText();
}
}
