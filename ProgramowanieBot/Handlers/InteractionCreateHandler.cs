using System.Reflection;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Services.ApplicationCommands;

namespace ProgramowanieBot.Handlers;

internal class InteractionCreateHandler : BaseHandler
{
    private readonly ApplicationCommandService<SlashCommandContext> _applicationCommandService;
    private readonly ITokenService _tokenService;

    public InteractionCreateHandler(
        ILogger<InteractionCreateHandler> logger,
        GatewayClient discordClient,
        ApplicationCommandService<SlashCommandContext> applicationCommandService,
        ITokenService tokenService)
        : base(logger, discordClient)
    {
        Guard.Against.Null(applicationCommandService);
        Guard.Against.Null(tokenService);

        _applicationCommandService = applicationCommandService;
        _tokenService = tokenService;
    }

    protected override async Task StartHandlerAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Registering application commands");
        _applicationCommandService.AddModules(Assembly.GetEntryAssembly()!);

        var commands = await _applicationCommandService.CreateCommandsAsync(Client.Rest, _tokenService.Token.Id);
        Logger.LogInformation("{count} command(s) successfully registered", commands.Count);

        Client.InteractionCreate += HandleInteractionAsync;
    }

    protected override Task StopHandlerAsync(CancellationToken cancellationToken)
    {
        Client.InteractionCreate -= HandleInteractionAsync;
        return Task.CompletedTask;
    }

    private async ValueTask HandleInteractionAsync(Interaction interaction)
    {
        Logger.LogInformation("Starting handling new event");
        try
        {
            await (interaction switch
            {
                SlashCommandInteraction slashCommandInteraction => _applicationCommandService.ExecuteAsync(new(slashCommandInteraction, Client)),
                _ => throw new InvalidOperationException($"Invalid interaction {interaction?.GetType()}."),
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Interaction handling failed");
            try
            {
                Logger.LogDebug("Trying to send interaction failure message");
                await interaction.SendResponseAsync(InteractionCallback.ChannelMessageWithSource(new()
                {
                    Content = $"<a:nie:881595378070343710> {ex.Message}",
                    Flags = MessageFlags.Ephemeral,
                }));
            }
            catch (Exception innerEx)
            {
                Logger.LogError(innerEx, "Failed to send interaction failure message");
            }
        }

        Logger.LogInformation("Finished handling event");
    }
}