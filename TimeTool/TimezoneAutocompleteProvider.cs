using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace TimeTool
{
    internal record TzId(string Id, string FullLower, string[] Segments);

    public class TimezoneAutocompleteProvider : IAutoCompleteProvider
    {
        private static readonly IReadOnlyList<TzId> tzIds;

        static TimezoneAutocompleteProvider()
        {
            TzId[] timezones = [.. TimeZoneInfo
                .GetSystemTimeZones()
                .Select(tz => new TzId(
                    tz.Id,
                    tz.Id.ToLowerInvariant(),
                    tz.Id.ToLowerInvariant().Split('/', '_')
                    ))];
            tzIds = timezones;
        }

        private static IEnumerable<string> Match(string userInput)
        {
            foreach (var tz in tzIds)
            {
                int score = 0;
                if (tz.FullLower.StartsWith(userInput))
                    score += 100;

                foreach (var segment in tz.Segments)
                    if (segment.StartsWith(userInput))
                        score += 80;

                if (tz.FullLower.Contains(userInput))
                    score += 50;
                
                if (score >= 50)
                    yield return tz.Id;
            }
        }

        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            if (string.IsNullOrWhiteSpace(context.UserInput))
                return ValueTask.FromResult(
                    tzIds
                        .Take(25)
                        .Select(x => new DiscordAutoCompleteChoice(x.Id, x.Id)));
            return ValueTask.FromResult(
                    Match(context.UserInput.ToLowerInvariant())
                        .Take(25)
                        .Select(x => new DiscordAutoCompleteChoice(x, x)));
        }
    }
}
