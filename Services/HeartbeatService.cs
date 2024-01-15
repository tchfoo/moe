using System.Net;
using Discord;

namespace Moe.Services;

public class HeartbeatService
{
  public void Init()
  {
    Task.Run(async () =>
    {
      HttpListener listener = new HttpListener();
      var url = $"http://+:{ConfigService.Environment.StatusPort}/";
      listener.Prefixes.Add(url);
      listener.Start();
      await LogService.LogToFileAndConsole($"HTTP server started on {url}");

      while (true)
      {
        if (DiscordService.Discord.ConnectionState != ConnectionState.Connected)
        {
          continue;
        }

        HttpListenerContext context = listener.GetContext();
        // return 200
        context.Response.OutputStream.Close();
      }
    });
  }
}
