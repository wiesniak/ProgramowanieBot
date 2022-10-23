using NetCord;

namespace ProgramowanieBot.Models.Options
{
    internal class ForumStartingPost
    {
        public string? Message { get; set; }
        public ulong? ForumChannelId { get; set; }

        public IReadOnlyDictionary<ulong, ulong>? ForumTagsRoles { get; set; }
    }
}