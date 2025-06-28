namespace Netstr.Options.Limits
{
    /// <summary>
    /// Configuration limits for NIP-50 search functionality
    /// </summary>
    public class SearchLimits
    {
        /// <summary>
        /// Maximum length of search terms
        /// </summary>
        public int MaxSearchTermLength { get; set; } = 100;

        /// <summary>
        /// Maximum number of search results returned
        /// </summary>
        public int MaxSearchResults { get; set; } = 1000;

        /// <summary>
        /// Enable advanced search extensions (include:, domain:, etc.)
        /// </summary>
        public bool EnableAdvancedSearch { get; set; } = true;

        /// <summary>
        /// Enable PostgreSQL full-text search for better performance
        /// </summary>
        public bool EnableFullTextSearch { get; set; } = true;

        /// <summary>
        /// Minimum search term length required
        /// </summary>
        public int MinSearchTermLength { get; set; } = 2;
    }
}