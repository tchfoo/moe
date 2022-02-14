using Discord.WebSocket;

namespace TNTBot.Models
{
  public class Level
  {
    public const int MinXPPerMessage = 15;
    public const int MaxXPPerMessage = 25;
    public const double AverageXPPerMessage = (MinXPPerMessage + MaxXPPerMessage) / 2;

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
      return (int)(Math.Pow(level - 1, 2.38) * AverageXPPerMessage);
    }

    private double XPToLevel(int xp)
    {
      return Math.Pow(xp / AverageXPPerMessage, 1 / 2.38) + 1;
    }
  }
}
