using LinguaFlow.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Services;

public class ImportWorker(IServiceScopeFactory scopes, ILogger<ImportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await RecoverStuckJobsAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var importer = scope.ServiceProvider.GetRequiredService<ContentImporter>();

                var job = await db.ImportJobs.Include(j => j.Source)
                    .Where(j => j.Status == "Pending").OrderBy(j => j.Id)
                    .FirstOrDefaultAsync(ct);

                if (job is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                    continue;
                }

                logger.LogInformation("Bắt đầu job nhập nội dung #{Id}", job.Id);
                await importer.RunAsync(db, job, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi xử lý job nhập nội dung");
                try { await Task.Delay(TimeSpan.FromSeconds(10), ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    // Job đang "Running" mà ứng dụng bị tắt giữa chừng thì đánh dấu lỗi để thử lại được
    private async Task RecoverStuckJobsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stuck = await db.ImportJobs.Where(j => j.Status == "Running").ToListAsync(ct);
            foreach (var j in stuck)
            {
                j.Status = "Failed";
                j.Log = (j.Log ?? "") + "Job bị gián đoạn do ứng dụng dừng giữa chừng. Hãy thử lại.\n";
                j.FinishedAt = DateTime.UtcNow;
            }
            if (stuck.Count > 0) await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không kiểm tra được job đang dở (bảng ImportJobs có thể chưa được tạo).");
        }
    }
}