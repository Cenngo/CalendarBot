using System;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal class GuidTypeConverter : TypeConverter<Guid>
    {
        public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;
        public override Task<TypeConverterResult> ReadAsync(IInteractionContext context, SocketSlashCommandDataOption option, IServiceProvider services)
        {
            if (Guid.TryParse(option.Value as string, out var guid))
                return Task.FromResult(TypeConverterResult.FromSuccess(guid));
            else
                return Task.FromResult(TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "A valid Guid must be a 36 character string in the form 8-4-4-4-12"));
        }
    }
}
