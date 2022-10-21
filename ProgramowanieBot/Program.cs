using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Services.ApplicationCommands;
using ProgramowanieBot;
using ProgramowanieBot.Handlers;
using ProgramowanieBot.Models.Options;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder
            .ConfigureServices(ConfigureOptions)
            .ConfigureServices(services =>
            {
                services.AddSingleton<ITokenService, TokenService>()
                        .AddTransient<ApplicationCommandService<SlashCommandContext>>()
                        .AddEventHandlers()
                        .AddDiscordClient()
                        .AddHostedService<BotService>();
            });

        var host = builder.Build();
        await host.RunAsync();
    }

    private static IServiceCollection AddEventHandlers(this IServiceCollection services)
    {
        services.AddTransient<IHandler, GuildThreadCreateHandler>();
        services.AddTransient<IHandler, InteractionCreateHandler>();

        return services;
    }

    private static IServiceCollection AddDiscordClient(this IServiceCollection services)
    {
        services.AddTransient(provider =>
        {
            var tokenService = provider.GetService<ITokenService>();
            var logger = provider.GetService<ILogger<GatewayClient>>();

            var client = new GatewayClient(tokenService!.Token, new()
            {
                Intents = GatewayIntent.Guilds | GatewayIntent.GuildUsers | GatewayIntent.GuildPresences,
            });
            client.Log += message =>
            {
                logger!.Log(message.Severity switch
                {
                    LogSeverity.Info => LogLevel.Information,
                    LogSeverity.Error => LogLevel.Error,
                    _ => LogLevel.Warning
                }, "{message} {description}", message.Message, message.Description ?? string.Empty);
                return default;
            };

            return client;
        });

        return services;
    }

    private static void ConfigureOptions(HostBuilderContext context, IServiceCollection services)
    {
        var authOptions = new AuthOptions();
        context.Configuration.GetSection(AuthOptions.SectionPath)
                         .Bind(authOptions);

        var guildThreadCreateOptions = new GuildThreadCreateOptions();
        context.Configuration.GetSection(GuildThreadCreateOptions.SectionPath)
                         .Bind(guildThreadCreateOptions);

        services.Configure<AuthOptions>(context.Configuration.GetSection(AuthOptions.SectionPath));
        services.Configure<GuildThreadCreateOptions>(
            options =>
            {
                var forumStartingPostSection = context.Configuration.GetSection(GuildThreadCreateOptions.SectionPath).GetSection(nameof(GuildThreadCreateOptions.ForumStartingPost));

                options.ForumStartingPost = forumStartingPostSection.Get<ForumStartingPost>();
                options.ForumStartingPost.ForumChannelId = new Snowflake(forumStartingPostSection.GetValue<string>(nameof(ForumStartingPost.ForumChannelId)));
                options.ForumStartingPost.ForumTagsRoles = 
                    forumStartingPostSection
                    .GetSection(nameof(ForumStartingPost.ForumTagsRoles)).Get<Dictionary<string, string>>()
                    .ToDictionary(d => new Snowflake(d.Key), d=> new Snowflake(d.Value));
            });
    }
}

