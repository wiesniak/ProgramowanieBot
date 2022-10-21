using System.Reflection;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Services.ApplicationCommands;

namespace ProgramowanieBot.Handlers;

internal class InteractionCreateHandler : IHandler
{
    private readonly ILogger<InteractionCreateHandler> _logger;
    private readonly GatewayClient _client;
    private readonly ApplicationCommandService<SlashCommandContext> _applicationCommandService;
    private readonly ITokenService _tokenService;

    public InteractionCreateHandler(
        ILogger<InteractionCreateHandler> logger,
        GatewayClient discordClient,
        ApplicationCommandService<SlashCommandContext> applicationCommandService,
        ITokenService tokenService)
    {
        Guard.Against.Null(logger);
        Guard.Against.Null(discordClient);
        Guard.Against.Null(applicationCommandService);
        Guard.Against.Null(tokenService);

        _logger = logger;
        _client = discordClient;
        _applicationCommandService = applicationCommandService;
        _tokenService = tokenService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering application commands");
        _applicationCommandService.AddModules(Assembly.GetEntryAssembly()!);

        var list = await _applicationCommandService.CreateCommandsAsync(_client.Rest, _tokenService.Token.Id);
        _logger.LogInformation("{count} command(s) successfully registered", list.Count);

        _client.InteractionCreate += HandleInteractionAsync;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.InteractionCreate -= HandleInteractionAsync;
        return Task.CompletedTask;
    }

    private async ValueTask HandleInteractionAsync(Interaction interaction)
    {
        _logger.LogInformation("Starting handling new event");
        try
        {
            await (interaction switch
            {
                SlashCommandInteraction slashCommandInteraction => _applicationCommandService.ExecuteAsync(new(slashCommandInteraction, _client)),
                _ => throw new InvalidOperationException($"Invalid interaction {interaction?.GetType()}."),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interaction handling failed");
            try
            {
                _logger.LogDebug("Trying to send interaction failure message");
                await interaction.SendResponseAsync(InteractionCallback.ChannelMessageWithSource(new()
                {
                    Content = $"<a:nie:881595378070343710> {ex.Message}",
                    Flags = MessageFlags.Ephemeral,
                }));
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to send interaction failure message");
            }
        }

        _logger.LogInformation("Finished handling event");
    }
}