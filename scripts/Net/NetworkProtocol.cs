using System.Text.Json;
using System.Text.Json.Serialization;

namespace Karma.Net;

public enum NetworkClientMessageType
{
    Join,
    Intent,
    SnapshotRequest,
    Ping
}

public enum NetworkServerMessageType
{
    JoinResult,
    IntentResult,
    Snapshot,
    Pong,
    Error
}

public sealed record NetworkClientMessage(
    string MessageId,
    string PlayerId,
    NetworkClientMessageType Type,
    string DisplayName,
    long AfterTick,
    ServerIntent Intent)
{
    public static NetworkClientMessage Join(string messageId, string playerId, string displayName)
    {
        return new NetworkClientMessage(messageId, playerId, NetworkClientMessageType.Join, displayName, 0, null);
    }

    public static NetworkClientMessage SendIntent(string messageId, ServerIntent intent)
    {
        return new NetworkClientMessage(messageId, intent.PlayerId, NetworkClientMessageType.Intent, string.Empty, 0, intent);
    }

    public static NetworkClientMessage RequestSnapshot(string messageId, string playerId, long afterTick)
    {
        return new NetworkClientMessage(messageId, playerId, NetworkClientMessageType.SnapshotRequest, string.Empty, afterTick, null);
    }

    public static NetworkClientMessage Ping(string messageId, string playerId)
    {
        return new NetworkClientMessage(messageId, playerId, NetworkClientMessageType.Ping, string.Empty, 0, null);
    }
}

public sealed record NetworkServerMessage(
    string MessageId,
    string CorrelationId,
    string WorldId,
    long Tick,
    NetworkServerMessageType Type,
    ServerJoinResult JoinResult,
    ServerProcessResult IntentResult,
    ClientInterestSnapshot Snapshot,
    string Error)
{
    public static NetworkServerMessage FromJoin(
        string correlationId,
        string worldId,
        long tick,
        ServerJoinResult result)
    {
        return new NetworkServerMessage(
            $"{correlationId}:join_result",
            correlationId,
            worldId,
            tick,
            NetworkServerMessageType.JoinResult,
            result,
            null,
            null,
            string.Empty);
    }

    public static NetworkServerMessage FromIntent(
        string correlationId,
        string worldId,
        long tick,
        ServerProcessResult result,
        ClientInterestSnapshot snapshot)
    {
        return new NetworkServerMessage(
            $"{correlationId}:intent_result",
            correlationId,
            worldId,
            tick,
            NetworkServerMessageType.IntentResult,
            null,
            result,
            snapshot,
            string.Empty);
    }

    public static NetworkServerMessage FromSnapshot(
        string correlationId,
        string worldId,
        long tick,
        ClientInterestSnapshot snapshot)
    {
        return new NetworkServerMessage(
            $"{correlationId}:snapshot",
            correlationId,
            worldId,
            tick,
            NetworkServerMessageType.Snapshot,
            null,
            null,
            snapshot,
            string.Empty);
    }

    public static NetworkServerMessage Pong(string correlationId, string worldId, long tick)
    {
        return new NetworkServerMessage(
            $"{correlationId}:pong",
            correlationId,
            worldId,
            tick,
            NetworkServerMessageType.Pong,
            null,
            null,
            null,
            string.Empty);
    }

    public static NetworkServerMessage ErrorResponse(string correlationId, string worldId, long tick, string error)
    {
        return new NetworkServerMessage(
            $"{correlationId}:error",
            correlationId,
            worldId,
            tick,
            NetworkServerMessageType.Error,
            null,
            null,
            null,
            error);
    }
}

public static class AuthoritativeNetworkProtocol
{
    public static NetworkServerMessage Handle(AuthoritativeWorldServer server, NetworkClientMessage message)
    {
        return message.Type switch
        {
            NetworkClientMessageType.Join => NetworkServerMessage.FromJoin(
                message.MessageId,
                server.WorldId,
                server.Tick,
                server.JoinPlayer(message.PlayerId, message.DisplayName)),
            NetworkClientMessageType.Intent when message.Intent is not null => HandleIntent(server, message),
            NetworkClientMessageType.SnapshotRequest => NetworkServerMessage.FromSnapshot(
                message.MessageId,
                server.WorldId,
                server.Tick,
                server.CreateInterestSnapshot(message.PlayerId, message.AfterTick)),
            NetworkClientMessageType.Ping => NetworkServerMessage.Pong(message.MessageId, server.WorldId, server.Tick),
            _ => NetworkServerMessage.ErrorResponse(message.MessageId, server.WorldId, server.Tick, "Unsupported or malformed client message.")
        };
    }

    private static NetworkServerMessage HandleIntent(AuthoritativeWorldServer server, NetworkClientMessage message)
    {
        var result = server.ProcessIntent(message.Intent);
        var snapshot = server.CreateInterestSnapshot(message.Intent.PlayerId, message.AfterTick);
        return NetworkServerMessage.FromIntent(message.MessageId, server.WorldId, server.Tick, result, snapshot);
    }
}

public static class NetworkProtocolJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    static NetworkProtocolJson()
    {
        Options.Converters.Add(new JsonStringEnumConverter());
    }

    public static string WriteClient(NetworkClientMessage message)
    {
        return JsonSerializer.Serialize(message, Options);
    }

    public static NetworkClientMessage ReadClient(string json)
    {
        return JsonSerializer.Deserialize<NetworkClientMessage>(json, Options);
    }

    public static string WriteServer(NetworkServerMessage message)
    {
        return JsonSerializer.Serialize(message, Options);
    }

    public static NetworkServerMessage ReadServer(string json)
    {
        return JsonSerializer.Deserialize<NetworkServerMessage>(json, Options);
    }
}
