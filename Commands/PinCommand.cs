using System;
using Discord;
using Discord.WebSocket;
using TNTBot.Services;
using System.Text.RegularExpressions;

namespace TNTBot.Commands
{
  public class PinCommand : SlashCommandBase
  {
    public PinCommand() : base("pin")
    {
      Description = "Pin an existing message using a message link.";
      Options = new SlashCommandOptionBuilder()
        .AddOption("link", ApplicationCommandOptionType.String, "The link of a message", isRequired: true);
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var messageUrl = cmd.GetOption<string>("link")!;

      var formattedMessageUrl = Regex.Match(messageUrl, @"^https:\/\/discord\.com\/channels\/(\d+)\/(\d+)\/(\d+)");

      if (!formattedMessageUrl.Success)
      {
        await cmd.RespondAsync("Invalid message link.", ephemeral: true);
        return;
      }

      try
      {
        if (ulong.Parse(formattedMessageUrl.Groups[1].Value) == ((SocketGuildUser)cmd.User).Guild.Id && ulong.Parse(formattedMessageUrl.Groups[2].Value) == cmd.Channel.Id)
        {
          var message = await ((SocketTextChannel)cmd.Channel).GetMessageAsync(ulong.Parse(formattedMessageUrl.Groups[3].Value));

          if (message == null)
          {
            await cmd.RespondAsync("Invalid message link.", ephemeral: true);
            return;
          }

          var pinChannel = (SocketTextChannel)DiscordService.Discord.GetChannel(938860776591089674);

          await cmd.RespondAsync("Message successfully pinned.");
          var roles = ((SocketGuildUser)message.Author).Roles
            .Where(x => x.Color != Color.Default)
            .OrderBy(x => x.Position);

          if (message.Attachments.Any())
          {
            var embed = new EmbedBuilder()
            .WithAuthor(message.Author)
            .WithImageUrl(message.Attachments.First().Url)
            .WithDescription($"{message.Content}\n\n[Jump to message]({message.GetJumpUrl()})")
            .WithFooter($"{message.Timestamp.DateTime:yyyy-MM-dd • H:m}")
            .WithColor(roles.Last().Color);

            await pinChannel.SendMessageAsync(embed: embed.Build());
          }
          else
          {
            var embed = new EmbedBuilder()
              .WithAuthor(message.Author)
              .WithDescription($"{message.Content}\n\n[Jump to message]({message.GetJumpUrl()})")
              .WithFooter($"{message.Timestamp.DateTime:yyyy-MM-dd • H:m}")
              .WithColor(roles.Last().Color);

            await pinChannel.SendMessageAsync(embed: embed.Build());
          }

        }
        else
        {
          await cmd.RespondAsync("You can only pin messages from this channel.", ephemeral: true);
          return;
        }
      }
      catch (Exception)
      {
        await cmd.RespondAsync("Invalid message link.", ephemeral: true);
        return;
      }
    }
  }
}