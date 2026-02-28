using Microsoft.Extensions.Options;
using Netstr.Messaging.Models;
using Netstr.Options;

namespace Netstr.Messaging.Events.Validators
{
    public class AuthCreatedAtValidator : IEventValidator
    {
        private readonly IOptions<AuthOptions> authOptions;

        public AuthCreatedAtValidator(IOptions<AuthOptions> authOptions)
        {
            this.authOptions = authOptions;
        }

        public string? Validate(Event e, ClientContext context)
        {
            if (e.Kind != (long)EventKind.Auth)
            {
                return null;
            }

            var tolerance = this.authOptions.Value.AuthCreatedAtWindowSeconds;

            if (tolerance <= 0)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;

            if (e.CreatedAt < now.AddSeconds(-tolerance) || e.CreatedAt > now.AddSeconds(tolerance))
            {
                return Messages.InvalidCreatedAt;
            }

            return null;
        }
    }
}
