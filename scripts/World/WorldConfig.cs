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
        var baseConfig = FromServerConfig(
            "local-prototype",
            new WorldSeed(8675309, "Medieval Prototype", "medieval"),
            ServerConfig.Prototype4Player);
        return baseConfig with
        {
            WidthTiles = 80,
            HeightTiles = 72
        };
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
