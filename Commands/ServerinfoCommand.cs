using System;
using Discord;
using Discord.WebSocket;

namespace TNTBot.Commands
{
  public class ServerinfoCommand : SlashCommandBase
  {
    public ServerinfoCommand() : base("serverinfo")
    {
      Description = "Get the info of the server";
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var guild = (cmd.Channel as SocketGuildChannel)!.Guild;

      var humans = guild.Users.Count(x => !x.IsBot);
      var bots = guild.Users.Count(x => x.IsBot);

      var embed = new EmbedBuilder()
        .WithTitle($"Info for {guild.Name}")
        .WithThumbnailUrl(guild.IconUrl)
        .AddField("Owner", $"{guild.Owner.Mention}", inline: true)
        .AddField("Channels", $"Text: {guild.TextChannels.Count}\nVoice: {guild.VoiceChannels.Count}", inline: true)
        .AddField("Members", $"Total: {guild.MemberCount}\nHumans: {humans}\nBots: {bots}", inline: true)
        .AddField("Roles", $"{guild.Roles.Count}", inline: true)
        .WithColor(Colors.Blurple)
        .WithFooter($"ID: {guild.Id} • Created at {guild.CreatedAt:yyyy-MM-dd}");

      await cmd.RespondAsync(embed: embed.Build());
    }
  }
}
