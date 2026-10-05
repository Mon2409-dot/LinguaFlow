using LinguaFlow.Api.Data;
using LinguaFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Services;

public class ActivityService(AppDbContext db)
{
    // "Hôm nay" tính theo giờ Việt Nam (UTC+7).
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    // Ghi nhận hôm nay có học. Người gọi tự gọi SaveChangesAsync sau đó.
    public async Task RecordAsync(string userId)
    {
        var day = Today();
        var row = await db.StudyActivities.FirstOrDefaultAsync(a => a.UserId == userId && a.Day == day);
        if (row is null) db.StudyActivities.Add(new StudyActivity { UserId = userId, Day = day, Count = 1 });
        else row.Count++;
    }

    // Số ngày học liên tiếp. Vẫn tính nếu hôm qua có học mà hôm nay chưa học.
    public async Task<int> CurrentStreakAsync(string userId)
    {
        var days = await db.StudyActivities.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Day).Select(a => a.Day).Take(400).ToListAsync();

        var today = Today();
        if (days.Count == 0 || (days[0] != today && days[0] != today.AddDays(-1))) return 0;

        var streak = 0;
        var expected = days[0];
        foreach (var d in days)
        {
            if (d != expected) break;
            streak++;
            expected = expected.AddDays(-1);
        }
        return streak;
    }
}