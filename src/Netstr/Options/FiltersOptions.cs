namespace Netstr.Options
{
    /// <summary>
    /// Feature flags / compatibility switches for subscription filters.
    /// </summary>
    public class FiltersOptions
    {
        /// <summary>
        /// Enables non-standard AND-tag filters using the '&amp;' modifier (e.g. "&amp;p": ["a","b"]).
        /// When disabled, any '&amp;x' filter keys are rejected as unsupported.
        /// </summary>
        public bool AllowAndTagFilters { get; init; } = false;
    }
}

