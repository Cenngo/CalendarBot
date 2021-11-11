using System;
using System.Threading.Tasks;

namespace CalendarBot
{
    public class Acknowledge : PreconditionAttribute
    {
        public bool Ephemeral { get; }

        public Acknowledge(bool ephemeral = false)
        {
            Ephemeral = ephemeral;
        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            await context.Interaction.DeferAsync(Ephemeral);
            return PreconditionResult.FromSuccess();
        }
    }
}
