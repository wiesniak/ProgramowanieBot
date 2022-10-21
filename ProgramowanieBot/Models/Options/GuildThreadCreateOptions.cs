namespace ProgramowanieBot.Models.Options
{
    internal class GuildThreadCreateOptions
    {
        public const string SectionPath = "EventHandlers:GuildThreadCreate";

        public ForumStartingPost? ForumStartingPost { get; set; }
    }
}