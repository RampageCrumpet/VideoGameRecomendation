namespace GameRecommendation.Web.Services
{
    /// <summary>
    /// Handles communication with the ratings API endpoints.
    /// </summary>
    public class RatingsService
    {
        private readonly HttpClient httpClient;

        public RatingsService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
    }
}