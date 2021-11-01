﻿using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal class EventAutocompleter : Autocompleter
    {
        public override Task<IEnumerable<AutocompleteResult>> GenerateSuggestionsAsync(IInteractionCommandContext context, AutocompleteOption option, IServiceProvider services)
        {
            var events = services.GetRequiredService<ILiteCollection<CalendarEvent>>();

            var value = option.Value as string;

            if (string.IsNullOrEmpty(value))
                return Task.FromResult<IEnumerable<AutocompleteResult>>(null);
            else {
                var suggestions = events.Find(x => x.Name.StartsWith(value));
                return Task.FromResult(suggestions.Select(x => new AutocompleteResult(x.Name, x.Id.ToString())));
            }
        }
    }
}
