using Discord.WebSocket;
using MathNet.Numerics.RootFinding;

namespace MoeBot.Models;

public class Level
{
  public const int MinXPPerMessage = 15;
  public const int MaxXPPerMessage = 25;

  public int Id { get; set; }
  public SocketGuildUser User { get; set; }
  public int TotalXP { get; set; }
  public DateTime LastUpdated { get; set; }
  public double PercentageToNextLevel { get => XPToLevel(TotalXP) % 1; }
  public int LevelNumber { get => (int)XPToLevel(TotalXP); }
  public int TotalLevelXP
  {
    get
    {
      var currentLevelXP = LevelToXP(LevelNumber);
      var nextLevelXP = LevelToXP(LevelNumber + 1);
      return nextLevelXP - currentLevelXP;
    }
  }
  public int XPFromThisLevel { get => TotalXP - LevelToXP(LevelNumber); }
  public int XPToNextLevel { get => TotalLevelXP - XPFromThisLevel; }

  public Level(int id, SocketGuildUser user, int xp, DateTime lastUpdated)
  {
    Id = id;
    User = user;
    TotalXP = xp;
    LastUpdated = lastUpdated;
  }

  private int LevelToXP(double level)
  {
    var xp = 5d / 6d * ((2 * Math.Pow(level, 3)) + (27 * Math.Pow(level, 2)) + (91 * level));
    return (int)Math.Round(xp);
  }

  private double XPToLevel(int xp)
  {
    var d = -6d / 5d * xp;
    var roots = Cubic.Roots(d, 91, 27, 2);
    return roots.Item2.Real;
  }
}
