using System;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal class InclusiveRange : ParameterPreconditionAttribute
    {
        public int LowerBoundry { get; }
        public int UpperBoundry { get; }

        public InclusiveRange(int lowerBoundry, int upperBoundry)
        {
            LowerBoundry = lowerBoundry;
            UpperBoundry = upperBoundry;
        }

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, IParameterInfo parameterInfo, object value, IServiceProvider services)
        {
            if (value is int intValue) {
                if (intValue <= UpperBoundry && intValue >= LowerBoundry)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                else
                    return Task.FromResult(PreconditionResult.FromError($"Parameter value isn't within bounds {LowerBoundry}-{UpperBoundry}"));
            }
            else
                return Task.FromResult(PreconditionResult.FromError("Parameter is not an integer"));
        }
    }
}
