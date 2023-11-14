using Discord;
using Discord.WebSocket;
using Moe.Services;
using System.Text.RegularExpressions;

namespace Moe.Commands;

public class PinSlashCommand : SlashCommandBase
{
  private readonly PinService service;
  public PinSlashCommand(PinService service) : base("pin")
  {
    Description = "Pin an existing message using a message link";
    Options = new SlashCommandOptionBuilder()
      .AddOption("link", ApplicationCommandOptionType.String, "The link of a message", isRequired: true);
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    var messageUrl = cmd.GetOption<string>("link")!;

    var formattedMessageUrl = Regex.Match(messageUrl, @"^https:\/\/(canary\.|ptb\.)?discord\.com\/channels\/(\d+)\/(\d+)\/(\d+)");

    if (!formattedMessageUrl.Success)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Invalid message link");
      return;
    }

    var guildId = ((SocketGuildUser)cmd.User).Guild.Id;
    var channelId = cmd.Channel.Id;

    var groups = formattedMessageUrl.Groups;

    ulong inputGuildId;
    ulong inputChannelId;
    ulong inputMessageId;

    try
    {
      inputGuildId = ulong.Parse(groups[1].Value);
      inputChannelId = ulong.Parse(groups[2].Value);
      inputMessageId = ulong.Parse(groups[3].Value);
    }
    catch (Exception)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Invalid message link");
      return;
    }

    if (inputGuildId != guildId || inputChannelId != channelId)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} You can only pin messages from this channel");
      return;
    }

    var channel = (SocketTextChannel)cmd.Channel;

    var message = await channel.GetMessageAsync(inputMessageId);

    if (message == null)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Invalid message link");
      return;
    }

    await service.TryPinningMessage(cmd, message);
  }
}
