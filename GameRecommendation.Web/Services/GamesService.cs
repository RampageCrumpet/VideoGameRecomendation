namespace GameRecommendation.Web.Services
{
    /// <summary>
    /// Handles communication with the games API endpoints.
    /// </summary>
    public class GamesService
    {
        private readonly HttpClient httpClient;

        public GamesService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
    }
}