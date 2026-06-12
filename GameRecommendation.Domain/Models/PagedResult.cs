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

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages calculated from TotalCount and PageSize.
        /// </summary>
        /// <remarks>Rounds up when TotalCount is not a multiple of PageSize. Returns 0 when PageSize is
        /// less than or equal to 0 to avoid division by zero.</remarks>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    }
}
