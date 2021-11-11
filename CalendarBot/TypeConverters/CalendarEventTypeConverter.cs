using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBot
{
    public class CalendarEventTypeConverter : TypeConverter<CalendarEvent>
    {
        public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;
        public override Task<TypeConverterResult> ReadAsync(IInteractionContext context, SocketSlashCommandDataOption option, IServiceProvider services)
        {
            if (!Guid.TryParse(option.Value as string, out var guid))
                return Task.FromResult(TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "A valid Guid must be a 36 character string in the form 8-4-4-4-12"));

            var events = services.GetRequiredService<ILiteCollection<CalendarEvent>>();

            var ev = events.FindById(guid);

            if (ev is not null)
                return Task.FromResult(TypeConverterResult.FromSuccess(ev));
            else
                return Task.FromResult(TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Event couldn't be found, it might be deleted."));
        }
    }
}
