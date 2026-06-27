using System;

namespace WC26Pool.API.Helpers;

public static class BrasiliaTime
{
    private static readonly TimeZoneInfo Zone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateOnly GetDisplayDay(DateTimeOffset matchDate)
    {
        var matchTimeInBrasilia = TimeZoneInfo.ConvertTimeFromUtc(matchDate.UtcDateTime, Zone);

        return matchTimeInBrasilia.TimeOfDay < TimeSpan.FromHours(3)
            ? DateOnly.FromDateTime(matchTimeInBrasilia.AddDays(-1))
            : DateOnly.FromDateTime(matchTimeInBrasilia);
    }

    public static DateOnly GetTodayDisplayDay()
    {
        var nowBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
        return nowBrasilia.TimeOfDay < TimeSpan.FromHours(3)
            ? DateOnly.FromDateTime(nowBrasilia.AddDays(-1))
            : DateOnly.FromDateTime(nowBrasilia);
    }

    public static (DateTime startUtc, DateTime endUtc) DisplayDayUtcBounds(DateOnly displayDay)
    {
        var startLocal = new DateTime(displayDay.Year, displayDay.Month, displayDay.Day, 3, 0, 0, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, Zone);
        return (startUtc, startUtc.AddDays(1));
    }

    public static (DateTime startUtc, DateTime endUtc) TodayUtcBounds()
    {
        return DisplayDayUtcBounds(GetTodayDisplayDay());
    }

    public static (DateTime startUtc, DateTime endUtc) OffsetUtcBounds(int daysFrom, int daysTo)
    {
        var today = GetTodayDisplayDay();
        var startLocal = new DateTime(today.Year, today.Month, today.Day, 3, 0, 0, DateTimeKind.Unspecified);
        
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal.AddDays(daysFrom), Zone);
        var endUtc   = TimeZoneInfo.ConvertTimeToUtc(startLocal.AddDays(daysTo),   Zone);
        return (startUtc, endUtc);
    }
}
