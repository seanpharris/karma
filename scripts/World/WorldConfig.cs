using Karma.Net;

namespace Karma.World;

public sealed record WorldConfig(
    string WorldId,
    WorldSeed Seed,
    ServerConfig Server,
    int WidthTiles,
    int HeightTiles)
{
    public static WorldConfig CreatePrototype()
    {
        return FromServerConfig(
            "local-prototype",
            new WorldSeed(8675309, "Frontier Clinic", "western-sci-fi"),
            ServerConfig.Prototype4Player);
    }

    public static WorldConfig FromServerConfig(string worldId, WorldSeed seed, ServerConfig server)
    {
        server.Validate();
        var size = server.Scale switch
        {
            WorldScale.Small => 64,
            WorldScale.Medium => 160,
            WorldScale.Large => 1000,
            _ => 64
        };

        return new WorldConfig(worldId, seed, server, size, size);
    }
}
