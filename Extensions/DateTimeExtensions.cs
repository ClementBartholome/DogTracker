namespace DogTracker.Extensions;

public static class DateTimeExtensions
{
    public static int GetQuarter(this DateTime date)
    {
        return (date.Month + 2)/3;
    }

    public static DateTime GetQuarterFirstMonth(this int quarter)
    {
        return quarter switch
        {
            1 => new DateTime(DateTime.Now.Year, 1, 1).ToUniversalTime().AddHours(1),
            2 => new DateTime(DateTime.Now.Year, 4, 1).ToUniversalTime().AddHours(1),
            3 => new DateTime(DateTime.Now.Year, 7, 1).ToUniversalTime().AddHours(1),
            4 => new DateTime(DateTime.Now.Year, 10, 1).ToUniversalTime().AddHours(1),
            _ => throw new ArgumentOutOfRangeException(nameof(quarter), quarter, null)
        };
    }

    public static DateTime GetQuarterLastMonth(this int quarter)
    {
        return quarter switch
        {
            1 => new DateTime(DateTime.Now.Year, 3, 31).ToUniversalTime().AddHours(1),
            2 => new DateTime(DateTime.Now.Year, 6, 30).ToUniversalTime().AddHours(1),
            3 => new DateTime(DateTime.Now.Year, 9, 30).ToUniversalTime().AddHours(1),
            4 => new DateTime(DateTime.Now.Year, 12, 31).ToUniversalTime().AddHours(1),
            _ => throw new ArgumentOutOfRangeException(nameof(quarter), quarter, null)
        };
    }
}