using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class CustomCommandHandlerDM
  {
    private readonly CustomCommandService service;

    public CustomCommandHandlerDM(CustomCommandService service)
    {
      this.service = service;
    }

    public async Task<bool> TryHandleCommand(SocketMessage message, string name, List<string> args)
    {
      var channel = message.Channel;
      var guild = (channel as SocketGuildChannel)!.Guild;
      var command = await service.GetCommand(guild, name);
      if (command == null)
      {
        return false;
      }

      if (!ValidateParameters(command, args, out var error))
      {
        await channel.SendMessageAsync(error);
        return true;
      }

      var lines = FormatResponse(command.Response, args);
      lines.ForEach(x => channel.SendMessageAsync(x));
      return true;
    }

    private bool ValidateParameters(CustomCommand command, List<string> args, out string error)
    {
      error = default!;
      var infos = service.GetParameterErrorInfos(command.Response);
      if (infos.Count != args.Count)
      {
        error = $"Invalid number of parameters. Expected {infos.Count} but got {args.Count}\n" +
          $"Maybe this will help: {command.Description}";
        return false;
      }

      return true;
    }

    private List<string> FormatResponse(string response, List<string> args)
    {
      for (int i = 0; i < args.Count; i++)
      {
        response = response.Replace("$" + (i + 1), args[i]);
      }

      response = response.Replace("\\n", "\n");
      var lines = response.Split("\\m");
      return lines.ToList();
    }
  }
}
