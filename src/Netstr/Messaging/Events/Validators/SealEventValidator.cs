using Netstr.Messaging.Models;

namespace Netstr.Messaging.Events.Validators
{
    public class SealEventValidator : IEventValidator
    {
        public string? Validate(Event e, ClientContext context)
        {
            if (e.Kind != 13)
            {
                return null;
            }

            if (e.Tags.Length > 0)
            {
                return Messages.InvalidEmptyTagsForKind13;
            }

            return null;
        }
    }
}
