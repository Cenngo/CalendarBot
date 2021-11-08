using LiteDB;
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

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionCommandContext context, SocketAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var value = autocompleteInteraction.Data.Current.Value as string;

            if (string.IsNullOrEmpty(value))
                return Task.FromResult(AutocompletionResult.FromSuccess(null));
            else {
                var suggestions = Events.Find(x => x.Name.StartsWith(value));
                return Task.FromResult(AutocompletionResult.FromSuccess(suggestions
                    .Select(x => new AutocompleteResult($"{x.Name} ({x.DateAndTime.ToString(Culture.DateTimeFormat.ShortTimePattern)} {x.DateAndTime.ToString(Culture.DateTimeFormat.ShortDatePattern)})", x.Id.ToString()))));
            }
        }

        protected override string GetLogString(IInteractionCommandContext context) =>
            $"Autocomplete: \"{base.ToString()}\" for {context.User} in {context.Guild}/{context.Channel}";
    }
}
