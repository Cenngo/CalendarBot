using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal class EventAutocompleter : Autocompleter
    {
        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionCommandContext context, SocketAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var events = services.GetRequiredService<ILiteCollection<CalendarEvent>>();

            var value = autocompleteInteraction.Data.Current.Value as string;

            if (string.IsNullOrEmpty(value))
                return Task.FromResult(AutocompletionResult.FromSuccess(null));
            else {
                var suggestions = events.Find(x => x.Name.StartsWith(value));
                return Task.FromResult(AutocompletionResult.FromSuccess(suggestions.Select(x => new AutocompleteResult(x.Name, x.Id.ToString()))));
            }
        }

        protected override string GetLogString(IInteractionCommandContext context) =>
            $"Autocomplete: \"{base.ToString()}\" for {context.User} in {context.Guild}/{context.Channel}";
    }
}
