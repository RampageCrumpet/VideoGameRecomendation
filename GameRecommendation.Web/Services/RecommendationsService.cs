namespace GameRecommendation.Web.Services
{
    /// <summary>
    /// Handles communication with the recommendations API endpoints.
    /// </summary>
    public class RecommendationsService
    {
        private readonly HttpClient httpClient;

        public RecommendationsService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
    }
}