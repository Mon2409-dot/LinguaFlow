using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LinguaFlow.Api.Data;
using LinguaFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Services;

public class ContentImporter(IHttpClientFactory httpFactory)
{
    private const int MaxBytes = 5 * 1024 * 1024;
    private const int MaxLessons = 500;

    private static readonly Regex ChapterRx = new(
        @"^\s*((CHAPTER|Chapter|KAPITEL|Kapitel|ГЛАВА|Глава|ЧАСТЬ|Часть)\b|第.{1,6}章).*$",
        RegexOptions.Multiline);

    // Cho phép đúng tên miền của nguồn (và tên miền con), bỏ qua "www."
    public static bool HostAllowed(Uri uri, string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var b)) return false;
        static string Norm(string h) => h.ToLowerInvariant().StartsWith("www.") ? h.ToLowerInvariant()[4..] : h.ToLowerInvariant();
        var host = Norm(uri.Host);
        var baseHost = Norm(b.Host);
        return host == baseHost || host.EndsWith("." + baseHost);
    }

    public async Task RunAsync(AppDbContext db, ImportJob job, CancellationToken ct)
    {
        var jobId = job.Id;
        var baseUrl = job.Source.BaseUrl;
        var log = new StringBuilder();
        void Log(string m) => log.AppendLine($"{DateTime.UtcNow:HH:mm:ss}  {m}");
        var status = "Failed";
        int? bookId = null;
        string? hash = null;

        job.Status = "Running";
        job.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            var uri = new Uri(job.Url);
            if (!HostAllowed(uri, baseUrl)) throw new InvalidOperationException("URL không thuộc tên miền của nguồn.");

            using var http = httpFactory.CreateClient("importer");

            Log("Kiểm tra robots.txt...");
            if (!await RobotsAllowsAsync(http, uri, ct))
                throw new InvalidOperationException("robots.txt của nguồn không cho phép tải đường dẫn này.");

            Log("Tải nội dung...");
            var text = await DownloadTextAsync(http, uri, baseUrl, ct);
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            Log($"Đã tải {text.Length:N0} ký tự.");

            hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            if (await db.ImportJobs.AnyAsync(x => x.Id != jobId && x.Status == "Completed"
                    && x.BookId != null && x.ContentHash == hash, ct))
                throw new InvalidOperationException("Nội dung này đã được nhập trước đó (trùng).");

            var title = job.Title ?? Match1(text, @"^Title:\s*(.+)$");
            var author = job.Author ?? Match1(text, @"^Author:\s*(.+)$");
            if (string.IsNullOrWhiteSpace(title))
                throw new InvalidOperationException("Không tự tìm được tiêu đề. Hãy điền 'title' khi tạo job.");

            var body = StripBoilerplate(text);
            var parts = SplitChapters(body);
            if (parts.Count >= 2) Log($"Tách được {parts.Count} chương.");
            else
            {
                parts = SplitByLength(body);
                Log($"Không thấy chương, chia theo độ dài thành {parts.Count} phần.");
            }
            if (parts.Count == 0) throw new InvalidOperationException("Không có nội dung để lưu.");
            if (parts.Count > MaxLessons) throw new InvalidOperationException($"Quá nhiều bài học ({parts.Count} > {MaxLessons}).");

            Log("Lưu vào cơ sở dữ liệu...");
            var book = new Book
            {
                SourceId = job.SourceId,
                LanguageId = job.LanguageId,
                Title = Truncate(title.Trim(), 300),
                Author = author is null ? null : Truncate(author.Trim(), 200),
                Level = job.Level,
                SourceUrl = job.Url,
                IsPublished = false
            };
            for (var i = 0; i < parts.Count; i++)
                book.Lessons.Add(new Lesson { OrderNo = i + 1, Title = parts[i].Title, Content = parts[i].Content });
            db.Books.Add(book);
            await db.SaveChangesAsync(ct);

            bookId = book.Id;
            status = "Completed";
            Log($"Hoàn tất: sách #{book.Id} với {parts.Count} bài (chưa xuất bản, chờ Admin duyệt).");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { Log("Lỗi: " + ex.Message); }

        // Làm sạch trạng thái EF rồi ghi kết quả job
        db.ChangeTracker.Clear();
        var j = await db.ImportJobs.FirstOrDefaultAsync(x => x.Id == jobId, CancellationToken.None);
        if (j is null) return;
        j.Status = status; j.BookId = bookId; j.ContentHash = hash;
        j.Log = log.ToString(); j.FinishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<bool> RobotsAllowsAsync(HttpClient http, Uri uri, CancellationToken ct)
    {
        using var resp = await http.GetAsync(new Uri(uri, "/robots.txt"), ct);
        if (!resp.IsSuccessStatusCode) return true;   // nguồn không có robots.txt

        var txt = await resp.Content.ReadAsStringAsync(ct);
        var disallows = new List<string>();
        bool star = false, lastWasAgent = false;
        foreach (var raw in txt.Split('\n'))
        {
            var line = raw.Split('#')[0].Trim();
            var idx = line.IndexOf(':');
            if (idx < 0) continue;
            var key = line[..idx].Trim().ToLowerInvariant();
            var val = line[(idx + 1)..].Trim();
            if (key == "user-agent")
            {
                if (!lastWasAgent) star = false;
                if (val == "*") star = true;
                lastWasAgent = true;
            }
            else
            {
                lastWasAgent = false;
                if (star && key == "disallow" && val.Length > 0) disallows.Add(val);
            }
        }

        var path = uri.AbsolutePath;
        foreach (var d in disallows)
        {
            if (d.Contains('*') || d.Contains('$'))
            {
                var rx = "^" + Regex.Escape(d).Replace("\\*", ".*").Replace("\\$", "$");
                if (Regex.IsMatch(path, rx)) return false;
            }
            else if (path.StartsWith(d, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static async Task<string> DownloadTextAsync(HttpClient http, Uri uri, string baseUrl, CancellationToken ct)
    {
        using var resp = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var finalUri = resp.RequestMessage?.RequestUri ?? uri;   // sau khi chuyển hướng
        if (!HostAllowed(finalUri, baseUrl))
            throw new InvalidOperationException("Link bị chuyển hướng sang tên miền không thuộc nguồn.");

        var type = resp.Content.Headers.ContentType?.MediaType ?? "";
        if (!type.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Chỉ hỗ trợ file văn bản thuần (text/plain), nhận được: {type}.");
        if (resp.Content.Headers.ContentLength > MaxBytes)
            throw new InvalidOperationException("File lớn hơn 5 MB.");

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            ms.Write(buffer, 0, read);
            if (ms.Length > MaxBytes) throw new InvalidOperationException("File lớn hơn 5 MB.");
        }
        return new UTF8Encoding(false).GetString(ms.ToArray()).TrimStart('\uFEFF');
    }

    private static string? Match1(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string StripBoilerplate(string text)
    {
        var start = Regex.Match(text, @"\*\*\*\s*START OF (THE|THIS) PROJECT GUTENBERG EBOOK.*?\*\*\*",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var end = Regex.Match(text, @"\*\*\*\s*END OF (THE|THIS) PROJECT GUTENBERG EBOOK", RegexOptions.IgnoreCase);
        var from = start.Success ? start.Index + start.Length : 0;
        var to = end.Success && end.Index > from ? end.Index : text.Length;
        return text[from..to].Trim();
    }

    private static List<(string Title, string Content)> SplitChapters(string body)
    {
        var matches = ChapterRx.Matches(body);
        var result = new List<(string, string)>();
        for (var i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            var start = m.Index + m.Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : body.Length;
            var content = body[start..end].Trim();
            if (content.Length < 200) continue;   // dòng trong mục lục, không có nội dung
            result.Add((Truncate(m.Value.Trim(), 200), content));
        }
        return result;
    }

    private static List<(string Title, string Content)> SplitByLength(string body, int target = 3000)
    {
        var result = new List<(string, string)>();
        var sb = new StringBuilder();
        foreach (var para in body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            if (sb.Length > 0 && sb.Length + para.Length > target)
            {
                result.Add(($"Phần {result.Count + 1}", sb.ToString().Trim()));
                sb.Clear();
            }
            sb.Append(para.Trim()).Append("\n\n");
        }
        if (sb.Length > 0) result.Add(($"Phần {result.Count + 1}", sb.ToString().Trim()));
        return result;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}