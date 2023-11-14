using Discord;
using Discord.WebSocket;
using Moe.Services;

namespace Moe.Commands;

public class ServerinfoCommand : SlashCommandBase
{
  private readonly SettingsService settingsService;

  public ServerinfoCommand(SettingsService settingsService) : base("serverinfo")
  {
    Description = "Get the info of the server";
    this.settingsService = settingsService;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    var guild = (cmd.Channel as SocketGuildChannel)!.Guild;

    var humans = guild.Users.Count(x => !x.IsBot);
    var bots = guild.Users.Count(x => x.IsBot);
    var prefix = await settingsService.GetCommandPrefix(guild);

    var embed = new EmbedBuilder()
      .WithTitle($"Info for {guild.Name}")
      .WithThumbnailUrl(guild.IconUrl)
      .AddField("Owner", $"{guild.Owner.Mention}", inline: true)
      .AddField("Channels", $"Text: {guild.TextChannels.Count}\nVoice: {guild.VoiceChannels.Count}", inline: true)
      .AddField("Members", $"Total: {guild.MemberCount}\nHumans: {humans}\nBots: {bots}", inline: true)
      .AddField("Roles", $"{guild.Roles.Count}", inline: true)
      .AddField("Custom command prefix", prefix, inline: true)
      .WithColor(Colors.Blurple)
      .WithFooter($"ID: {guild.Id} • Created at {guild.CreatedAt:yyyy-MM-dd}");

    await cmd.RespondAsync(embed: embed.Build());
  }
}
