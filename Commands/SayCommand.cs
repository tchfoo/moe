using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class SayCommand : SlashCommandBase
  {
    private readonly SayService service;
    public SayCommand(SayService service) : base("say")
    {
      Description = "Make the bot repeat your message";
      Options = new SlashCommandOptionBuilder()
        .AddOption("message", ApplicationCommandOptionType.String, "The message you want the bot to repeat", isRequired: true)
        .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to send the message to", isRequired: false, channelTypes: new List<ChannelType>() { ChannelType.Text });
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.User;
      if (cmd.HasOption("channel"))
      {
        if (!service.IsAuthorized(user, ModrankLevel.Moderator, out var error))
        {
          await cmd.RespondAsync($"{Emotes.ErrorEmote} " + error);
          return;
        }
      }

      var channel = cmd.GetOption<SocketTextChannel>("channel") ?? (SocketTextChannel)cmd.Channel;
      var message = cmd.GetOption<string>("message")!;

      if (message.Length > 2000)
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} The message is too long");
        return;
      }

      await channel.SendMessageAsync(message);
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Message sent", ephemeral: true);
    }
  }
}
