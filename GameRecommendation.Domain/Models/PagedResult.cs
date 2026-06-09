namespace GameRecommendation.Domain.Models
{
    /// <summary>
    /// A paginated result set returned from a service method.
    /// </summary>
    /// <typeparam name="T">The type of items in the result set.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items on the current page.
        /// </summary>
        public IEnumerable<T> Items { get; set; } = [];

        /// <summary>
        /// The total number of items across all pages.
        /// </summary>
        public int TotalCount { get; set; }
    }
}
