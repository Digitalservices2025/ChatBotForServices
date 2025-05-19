using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Text;

namespace ChatBotForServices.Data;

public static class GoogleSheetFAQService
{
    private static readonly string SheetId = "1PI38alcbujHwzUFTlfCMVyxm_mBaJkQCFfz1uya_v2k";
    private static readonly string Range = "Data!A:B";

    private static Dictionary<string, string>? _faqCache;

    private static string Normalize(string text)
    {
        return string.Concat(text
            .Trim()
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormC)
            .Where(c => !char.IsControl(c)))
            .Replace("\u00A0", " ")  // non-breaking space
            .Replace("\u200B", "")   // zero-width space
            .Replace("\u200C", "")   // zero-width non-joiner
            .Replace("\u200D", "")   // zero-width joiner
            .Replace("​", "");       // literal zero-width space (invisible)
    }


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

        var dict = new Dictionary<string, string>();
        if (values != null)
        {
            for (int i = 1; i < values.Count; i++)
            {
                var row = values[i];
                if (row.Count >= 2 && row[0] != null && row[1] != null)
                {
                    var keyRaw = row[0]?.ToString();
                    var valRaw = row[1]?.ToString();
                    var altKeywords = row.Count > 2 ? row[2]?.ToString()?.Split(',') : Array.Empty<string>();

                    if (!string.IsNullOrWhiteSpace(keyRaw) && valRaw != null)
                    {
                        var key = Normalize(keyRaw);
                        dict[key] = valRaw;

                        // Add aliases
                        if (altKeywords != null)
                        {
                            foreach (var alt in altKeywords)
                            {
                                var altKey = Normalize(alt);
                                if (!dict.ContainsKey(altKey))
                                    dict[altKey] = valRaw;
                            }
                        }
                    }
                }
            }
        }
        _faqCache = dict;
        return _faqCache;
    }

    public static async Task<string?> TryMatchAsync(string input)
    {
        var faq = await GetFAQsAsync();
        var normalizedInput = Normalize(input);

        if (faq.TryGetValue(normalizedInput, out var exactAnswer))
        {
            var rows = exactAnswer.Split('\n');
            var sb = new StringBuilder();
            sb.AppendLine("<table class='faq-table'>");
            foreach (var line in rows)
            {
                sb.AppendLine("<tr><td>" + System.Net.WebUtility.HtmlEncode(line.Trim()) + "</td></tr>");
            }
            sb.AppendLine("</table>");
            Console.WriteLine($"🧪 User input normalized: {normalizedInput}");
            foreach (var key in faq.Keys)
            {
                Console.WriteLine($"📌 Key: [{key}]");
            }

            return sb.ToString();
        }


        var matches = faq
            .Where(kvp =>
                kvp.Key.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                normalizedInput.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        if (matches.Count == 1)
        {
            var rows = matches[0].Value.Split('\n');
            var sb = new StringBuilder();
            sb.AppendLine("<table class='faq-table'>");
            foreach (var line in rows)
            {
                sb.AppendLine("<tr><td>" + System.Net.WebUtility.HtmlEncode(line.Trim()) + "</td></tr>");
            }
            sb.AppendLine("</table>");
            return sb.ToString();
        }

        if (matches.Count > 1)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<strong>🟡 พบคำถามที่ใกล้เคียงหลายข้อ:</strong><br><br>");
            foreach (var match in matches)
            {
                sb.AppendLine($"• <a href=\"#\" onclick=\"document.getElementById('user-input').value='{match.Key.Replace("'", "\\'")}';\">{match.Key}</a><br>");
            }
            sb.AppendLine("<br>📌 พิมพ์คำถามเต็มเพื่อรับคำตอบครับ");
            return sb.ToString();
        }

        return null;
    }


}
