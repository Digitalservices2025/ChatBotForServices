using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Text;

namespace ChatBotForServices.Data;

public static class GoogleSheetFAQService
{
    private static readonly string SheetId = "1N8Ens_0AqH0y3ZirEYX3DqdovHJdnq2UmSFb_EWhYZE";
    private static readonly string Range = "Sheet1!A:B";
    private static Dictionary<string, string>? _faqCache;

    // Loads FAQs from Google Sheet and caches them
    public static async Task<Dictionary<string, string>> GetFAQsAsync()
    {
        if (_faqCache != null) return _faqCache;

        var credential = GoogleCredential.FromFile("D:/Work/ChatBotForServices/google-credentials.json")
            .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

        var service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Sheetforchatbot",
        });

        var request = service.Spreadsheets.Values.Get(SheetId, Range);
        var response = await request.ExecuteAsync();
        var values = response.Values;

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (values != null)
        {
            for (int i = 1; i < values.Count; i++)
            {
                var row = values[i];
                if (row.Count >= 2 && row[0] != null && row[1] != null)
                    dict[row[0]!.ToString()] = row[1]!.ToString();
            }
        }

        _faqCache = dict;
        return dict;
    }

    public static async Task<string?> TryMatchAsync(string input)
    {
        var faq = await GetFAQsAsync();
        var normalizedInput = input.Trim();

        // ✅ Exact match
        if (faq.TryGetValue(normalizedInput, out var exactAnswer))
            return exactAnswer;

        // ✅ Find all keyword matches
        var matches = faq
            .Where(kvp => kvp.Key.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                          normalizedInput.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        if (matches.Count == 1)
            return matches[0].Value;

        if (matches.Count > 1)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🟡 <strong>พบคำถามที่เกี่ยวข้องหลายข้อ:</strong><br><br>");

            foreach (var match in matches)
            {
                // 👇 This makes it clickable by setting the input value
                sb.AppendLine($"- <a href=\"#\" onclick=\"document.getElementById('user-input').value='{match.Key.Replace("'", "\\'")}';\">{match.Key}</a><br>");
            }

            sb.AppendLine("<br>📌 พิมพ์คำถามเต็มเพื่อรับคำตอบนะครับ");
            return sb.ToString();
        }

        return null;
    }

}
