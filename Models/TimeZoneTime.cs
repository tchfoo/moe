using System.Globalization;
using System.Text.RegularExpressions;

namespace Moe.Models;

public class TimeZoneTime
{
  private static readonly Regex timeAndTimeZoneRegex = new(@"(?<time>\d{1,2}((\.|:)\d{1,2})?\s?(am|pm)?)?(\s?(?<timeZone>(\w{3,}\s?)+)\s?((?<sign>\+|-)(?<offsetHours>(\d{1,2}|yaha))((\.|:)(?<offsetMinutes>\d{1,2}))?)?)?", RegexOptions.IgnoreCase);

  public DateTime? Time { get; set; }
  public TimeZoneInfo? TimeZoneBase { get; set; }
  public TimeSpan TimeZoneOffset { get; set; } = TimeSpan.Zero;
  public TimeSpan? UtcOffset => TimeZoneBase?.BaseUtcOffset + TimeZoneOffset;
  public string TimeZoneString
  {
    get
    {
      var timeZoneName = TimeZoneBase?.Id;
      var sign = TimeZoneOffset < TimeSpan.Zero ? "-" : "+";
      var offset = TimeZoneOffset.ToString("hh\\:mm");
      return timeZoneName + sign + offset;
    }
  }

  public static TimeZoneTime Parse(string input, TimeZoneTime? defaultTimeZone = null)
  {
    if (string.IsNullOrEmpty(input))
    {
      return new TimeZoneTime()
      {
        Time = defaultTimeZone?.Time ?? DateTime.Now,
        TimeZoneBase = defaultTimeZone?.TimeZoneBase ?? TimeZoneInfo.Utc,
        TimeZoneOffset = defaultTimeZone?.TimeZoneOffset ?? TimeSpan.Zero
      };
    }

    var match = timeAndTimeZoneRegex.Match(input);
    if (!match.Success)
    {
      throw new FormatException("Invalid time or time zone");
    }

    var groups = match.Groups;
    var time = groups["time"].Value;
    var timeZone = groups["timeZone"].Value;
    var sign = groups["sign"].Value;
    var offsetHours = groups["offsetHours"].Value;
    var offsetMinutes = groups["offsetMinutes"].Value;

    var isTimeZoneSpecified = !string.IsNullOrEmpty(timeZone);
    return isTimeZoneSpecified
      ? new TimeZoneTime
      {
        Time = ParseTime(time),
        TimeZoneBase = ParseTimeZone(timeZone),
        TimeZoneOffset = ParseOffset(sign, offsetHours, offsetMinutes)
      }
      : new TimeZoneTime()
      {
        Time = ParseTime(time),
        TimeZoneBase = defaultTimeZone?.TimeZoneBase ?? TimeZoneInfo.Utc,
        TimeZoneOffset = defaultTimeZone?.TimeZoneOffset ?? TimeSpan.Zero
      };
  }

  private static DateTime ParseTime(string time)
  {
    if (string.IsNullOrEmpty(time))
    {
      return DateTime.Now;
    }

    if (DateTime.TryParse(time, out var result))
    {
      return result;
    }

    throw new FormatException("Invalid time");
  }

  private static TimeZoneInfo ParseTimeZone(string timeZone)
  {
    var textInfo = new CultureInfo("en-US", false).TextInfo;
    timeZone = timeZone.Contains(' ') ? textInfo.ToTitleCase(timeZone) : textInfo.ToUpper(timeZone);

    var found = TimeZoneInfo.GetSystemTimeZones()
      .FirstOrDefault(x => x.StandardName == timeZone ||
        x.DaylightName == timeZone ||
        x.Id == timeZone);
    if (found is not null)
    {
      return found;
    }

    try
    {
      return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
    }
    catch (TimeZoneNotFoundException)
    {
      throw new FormatException("Invalid time zone");
    }
  }

  private static TimeSpan ParseOffset(string sign, string offsetHours, string offsetMinutes)
  {
    if (string.IsNullOrEmpty(sign))
    {
      sign = "+";
    }
    if (string.IsNullOrEmpty(offsetHours))
    {
      offsetHours = "0";
    }
    if (string.IsNullOrEmpty(offsetMinutes))
    {
      offsetMinutes = "0";
    }

    var isYaha = string.Equals(offsetHours, "yaha", StringComparison.OrdinalIgnoreCase);

    var hours = isYaha ? -4 : int.Parse(offsetHours);
    var minutes = int.Parse(offsetMinutes);
    var offset = new TimeSpan(hours, minutes, 0);
    return sign == "+" ? offset : -offset;
  }
}
