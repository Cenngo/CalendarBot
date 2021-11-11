using CalendarBot.Entities;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarBot
{
    public class EventAutocompleter : Autocompleter
    {
        public ILiteCollection<CalendarEvent> Events {  get; set; }
        public CultureInfo Culture {  get; set; }
        public IMemoryCache Cache { get; set; }

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, SocketAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var value = autocompleteInteraction.Data.Current.Value as string;

            if (string.IsNullOrEmpty(value))
                return Task.FromResult(AutocompletionResult.FromSuccess(null));
            else {
                var guildEvents = Cache.GetOrCreate(context.Guild.Id, entry => {
                    var entryValue = new GuildEventCache(context.Guild.Id, Events.Find(x => x.GuildId == context.Guild.Id));
                    entry.SetValue(entryValue);
                    entry.SetSlidingExpiration(TimeSpan.FromSeconds(2));
                    return entryValue;
                }).GuildEvents;

                var suggestions = guildEvents.Where(x => x.Name.StartsWith(value));

                return Task.FromResult(AutocompletionResult.FromSuccess(suggestions
                    .Select(x => new AutocompleteResult($"{x.Name} ({x.DateAndTime.ToString(Culture.DateTimeFormat.ShortTimePattern)} {x.DateAndTime.ToString(Culture.DateTimeFormat.ShortDatePattern)})", x.Id.ToString()))));
            }
        }

        protected override string GetLogString(IInteractionContext context) =>
            $"Autocomplete: \"{base.ToString()}\" for {context.User} in {context.Guild}/{context.Channel}";
    }
}
