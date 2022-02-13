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
    public int XP { get; set; }
    public int LevelNumber
    {
      get => (int)Math.Pow(XP / AverageXPPerMessage, 1 / 2.38) + 1;
    }
    public DateTime LastUpdated { get; set; }

    public Level(int id, SocketGuildUser user, int xp, DateTime lastUpdated)
    {
      Id = id;
      User = user;
      XP = xp;
      LastUpdated = lastUpdated;
    }
  }
}
