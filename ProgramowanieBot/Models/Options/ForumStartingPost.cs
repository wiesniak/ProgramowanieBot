using NetCord;

namespace ProgramowanieBot.Models.Options
{
    internal class ForumStartingPost
    {
        public string? Message { get; set; }
        public Snowflake? ForumChannelId { get; set; }

        public IReadOnlyDictionary<Snowflake, Snowflake>? ForumTagsRoles { get; set; }
    }
}