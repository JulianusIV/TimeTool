using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using System.Reflection;
using System.Text;

namespace TimeTool
{
    public class Commands
    {
        [Command("set_timezone")]
        [InteractionAllowedContexts(
            DiscordInteractionContextType.BotDM,
            DiscordInteractionContextType.PrivateChannel,
            DiscordInteractionContextType.Guild)]
        public static async Task SetTimezone(SlashCommandContext ctx, [SlashAutoCompleteProvider<TimezoneAutocompleteProvider>] string timezoneName)
        {
            await ctx.DeferResponseAsync(true);

            bool valid = TimeZoneInfo.TryFindSystemTimeZoneById(timezoneName, out TimeZoneInfo? tzInfo);
            if (!valid)
            {
                await ctx.RespondAsync($"{timezoneName} is not a valid timezone name.");
                return;
            }

            using SupaContext db = new();
            if (db.Timezones.Any(x => x.Id == ctx.User.Id))
                db.Timezones.Update(new Timezone { Id = ctx.User.Id, IANADescriptor = timezoneName });
            else
                db.Timezones.Add(new Timezone { Id = ctx.User.Id, IANADescriptor = timezoneName });
            db.SaveChanges();

            await ctx.RespondAsync($"Okay, your timezone is now set to {timezoneName}");
        }

        [Command("unregister")]
        [InteractionAllowedContexts(
            DiscordInteractionContextType.BotDM,
            DiscordInteractionContextType.PrivateChannel,
            DiscordInteractionContextType.Guild)]
        public static async Task Unregister(SlashCommandContext ctx)
        {
            await ctx.DeferResponseAsync(true);

            using SupaContext db = new();
            Timezone? tz = db.Timezones.Find(ctx.User.Id);
            if (tz is not null)
            {
                db.Timezones.Remove(tz);
                db.SaveChanges();
                await ctx.RespondAsync("Successfully deleted your data.");
            }
            else
                await ctx.RespondAsync("Cannot find an entry for you in the database.");
        }

        [Command("time_in")]
        [InteractionAllowedContexts(
            DiscordInteractionContextType.BotDM,
            DiscordInteractionContextType.PrivateChannel,
            DiscordInteractionContextType.Guild)]
        public static async Task TimeIn(
            SlashCommandContext ctx, 
            [SlashAutoCompleteProvider<TimezoneAutocompleteProvider>] string timezoneName, 
            TimeSpan? time = null)
        {
            await ctx.DeferResponseAsync();
            StringBuilder responseBuilder = new();
            TimeZoneInfo sourceTz = TimeZoneInfo.Utc;
            DateTime sourceTime = DateTime.UtcNow;
            bool tzSet = false;
            if (time is not null)
            {
                using SupaContext db = new();
                var tz = db.Timezones.Find(ctx.User.Id);
                if (tz is not null)
                {
                    sourceTz = TimeZoneInfo.FindSystemTimeZoneById(tz.IANADescriptor);
                    tzSet = true;
                }
                sourceTime = TimeZoneInfo.ConvertTime(sourceTime, TimeZoneInfo.Utc, sourceTz);
                sourceTime = sourceTime.Date + (TimeSpan)time;
                responseBuilder.Append($"{time:hh\\:mm} your time is");
            }
            else
                responseBuilder.Append("It is currently");

            TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezoneName);
            DateTime localTime = TimeZoneInfo.ConvertTime(sourceTime, sourceTz, tzInfo);
            responseBuilder.AppendLine($" {localTime.TimeOfDay:hh\\:mm} in that timezone.");
            if (!tzSet && time is not null)
            {
                responseBuilder.AppendLine("Could not find your timezone, so UTC was assumed. ");
                var commands = await ctx.Client.GetGlobalApplicationCommandsAsync();
                var mention = commands.First(x =>
                {
                    CommandAttribute? attribute = (CommandAttribute?)typeof(Commands)
                        .GetMethod(nameof(SetTimezone))?
                        .GetCustomAttribute(typeof(CommandAttribute));
                    return attribute is not null && x.Name == attribute.Name;
                }).Mention;
                responseBuilder.AppendLine($"Use {mention} to set your timezone.");
            }
            await ctx.RespondAsync(responseBuilder.ToString());
        }

        [Command("time_for")]
        [InteractionAllowedContexts(
            DiscordInteractionContextType.PrivateChannel,
            DiscordInteractionContextType.Guild)]
        [SlashCommandTypes(
            DiscordApplicationCommandType.UserContextMenu,
            DiscordApplicationCommandType.SlashCommand)]
        public static async Task TimeFor(SlashCommandContext ctx, DiscordUser user, TimeSpan? time = null)
        {
            await ctx.DeferResponseAsync();
            using SupaContext db = new();
            string? targetTz = db.Timezones.FirstOrDefault(x => x.Id == user.Id)?.IANADescriptor;
            if (targetTz is null)
            {
                var commands = await ctx.Client.GetGlobalApplicationCommandsAsync();
                var mention = commands.First(x =>
                {
                    CommandAttribute? attribute = (CommandAttribute?)typeof(Commands)
                        .GetMethod(nameof(SetTimezone))?
                        .GetCustomAttribute(typeof(CommandAttribute));
                    return attribute is not null && x.Name == attribute.Name;
                }).Mention;

                var referralMessage = new DiscordMessageBuilder()
                    .EnableV2Components()
                    .AddTextDisplayComponent("This user has not set their timezone.\n" + $"To do so they can use the {mention} command.")
                    .AddActionRowComponent(new DiscordLinkButtonComponent("https://discord.com/oauth2/authorize?client_id=1443796292164653136", "Add me on Discord."));

                await ctx.RespondAsync(referralMessage);
                return;
            }

            StringBuilder responseBuilder = new();
            TimeZoneInfo sourceTz = TimeZoneInfo.Utc;
            DateTime sourceTime = DateTime.UtcNow;
            bool tzSet = false;
            if (time is not null)
            {
                var tz = db.Timezones.Find(ctx.User.Id);
                if (tz is not null)
                {
                    sourceTz = TimeZoneInfo.FindSystemTimeZoneById(tz.IANADescriptor);
                    tzSet = true;
                }
                sourceTime = TimeZoneInfo.ConvertTime(sourceTime, TimeZoneInfo.Utc, sourceTz);
                sourceTime = sourceTime.Date + (TimeSpan)time;
                responseBuilder.Append($"{time:hh\\:mm} your time is");
            }
            else
                responseBuilder.Append("It is currently");

            TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(targetTz);
            DateTime localTime = TimeZoneInfo.ConvertTime(sourceTime, sourceTz, tzInfo);
            responseBuilder.AppendLine($" {localTime.TimeOfDay:hh\\:mm} in {user.Mention}s timezone.");
            if (!tzSet && time is not null)
            {
                responseBuilder.AppendLine("Could not find your timezone, so UTC was assumed. ");
                var commands = await ctx.Client.GetGlobalApplicationCommandsAsync();
                var mention = commands.First(x =>
                {
                    CommandAttribute? attribute = (CommandAttribute?)typeof(Commands)
                        .GetMethod(nameof(SetTimezone))?
                        .GetCustomAttribute(typeof(CommandAttribute));
                    return attribute is not null && x.Name == attribute.Name;
                }).Mention;
                responseBuilder.AppendLine($"Use {mention} to set your timezone.");
            }
            await ctx.RespondAsync(responseBuilder.ToString());
        }
    }
}
