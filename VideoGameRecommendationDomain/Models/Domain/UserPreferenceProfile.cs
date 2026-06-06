namespace GameRecommendation.Domain.Models.Domain
{
    /// <summary>
    /// This class holds the user's preferences for game tags, which are used to generate personalized game recommendations. 
    /// A tags weight is a scalar value that represents the user's preference for a particular tag. A higher weight indicates a stronger preference for games with that tag, while a lower weight indicates a weaker preference.
    /// </summary>
    public class UserPreferenceProfile
    {
        /// <summary>
        /// The collection of tag weights representing the user's preferences for different game tags. The key is the tag's Id and the value is the weight representing the user's preference for that tag.
        /// </summary>
        public Dictionary<int, double> TagWeights { get; set; } = new Dictionary<int, double>();
    }
}
