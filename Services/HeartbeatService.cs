using Discord;

namespace Moe.Services;

public class HeartbeatService
{
  public void Init()
  {
    Task.Run(async () =>
    {
      while (true)
      {
        if (DiscordService.Discord.ConnectionState != ConnectionState.Connected)
        {
          continue;
        }

        var seconds = GetSecondsSinceEpoch().ToString();
        File.WriteAllText(".heartbeat", seconds);
        await Task.Delay(1000);
      }
    });
  }

  private int GetSecondsSinceEpoch()
  {
    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    return (int)t.TotalSeconds;
  }
}
