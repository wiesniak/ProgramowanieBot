using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using ProgramowanieBot.Models.Options;

namespace ProgramowanieBot.Handlers;

internal class GuildThreadCreateHandler : IHandler
{
    private readonly ILogger<GuildThreadCreateHandler> _logger;
    private readonly GatewayClient _client;
    private readonly GuildThreadCreateOptions _options;

    public GuildThreadCreateHandler(ILogger<GuildThreadCreateHandler> logger, IOptions<GuildThreadCreateOptions> options, GatewayClient discordClient)
    {
        Guard.Against.Null(logger);
        Guard.Against.Null(options);
        Guard.Against.Null(discordClient);

        _logger = logger;
        _client = discordClient;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.GuildThreadCreate += HandleThreadCreateAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.GuildThreadCreate -= HandleThreadCreateAsync;
        return Task.CompletedTask;
    }

    private async ValueTask HandleThreadCreateAsync(GuildThreadCreateEventArgs args)
    {
        _logger.LogInformation("Starting handling new event");

        if (!args.NewlyCreated ||
            args.Thread is not PublicGuildThread thread ||
            thread.ParentId != _options.ForumStartingPost!.ForumChannelId ||
            thread.AppliedTags is null)
        {
            _logger.LogInformation("Skipping event processing = not matching requirements");
            return;
        }

        var appliedTags = thread.AppliedTags;
        Snowflake roleId = default;
        if (appliedTags.Any(t => _options.ForumStartingPost.ForumTagsRoles!.TryGetValue(t, out var roleId)))
        {
            var message = await SendStartMessageAsync(thread, $"{_options.ForumStartingPost.Message}\nPing: <@&{roleId}>");

            if (!message.Flags.HasFlag(MessageFlags.FailedToMentionSomeRolesInThread) || !_client.Guilds.TryGetValue(args.Thread.GuildId, out var guild))
            {
                _logger.LogInformation("Skipping message send and delete");
                return;
            }

            StringBuilder stringBuilder = new(2000, 2000);
            List<Task> tasks = new(1);

            foreach (var user in guild.Users.Values.Where(u => u.RoleIds.Contains(roleId)))
            {
                var mention = user.ToString();
                if (stringBuilder.Length + mention.Length > 2000)
                {
                    tasks.Add(SendAndDeleteMessageAsync(thread, stringBuilder.ToString()));
                    stringBuilder.Clear();
                }

                stringBuilder.Append(mention);
            }

            if (stringBuilder.Length != 0)
            {
                tasks.Add(SendAndDeleteMessageAsync(thread, stringBuilder.ToString()));
            }

            await Task.WhenAll(tasks);
        }
        else
        {
            await SendStartMessageAsync(thread, _options.ForumStartingPost!.Message!);
        }

        _logger.LogInformation("Finished handling event");
    }

    private async Task<RestMessage> SendStartMessageAsync(PublicGuildThread thread, MessageProperties messageProperties)
    {
        _logger.LogDebug("Sending start message");

        RestMessage message;
        while (true) // TODO: consider retry policy instead of forever loop
        {
            try
            {
                message = await thread.SendMessageAsync(messageProperties);

                _logger.LogDebug("Message sent successfuly");
                break;
            }
            catch (RestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                if ((await JsonDocument.ParseAsync(await ex.ResponseContent.ReadAsStreamAsync())).RootElement.GetProperty("code").GetInt32() != 40058)
                {
                    _logger.LogError(ex, "Sending message failed");
                    throw;
                }
                else
                {
                    _logger.LogWarning(ex, "Sending message failed, silencing error.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sending message failed");
            }
        }

        return message;
    }

    private async Task SendAndDeleteMessageAsync(PublicGuildThread thread, string content)
    {
        _logger.LogDebug("Sending message with deletion");

        try
        {
            var message = await thread.SendMessageAsync(content);

            _logger.LogDebug("Deleting messag");
            await message.DeleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending or deleting message failed");
        }
    }
}