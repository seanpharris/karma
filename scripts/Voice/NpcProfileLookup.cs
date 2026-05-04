using Karma.Data;

namespace Karma.Voice;

public static class NpcProfileLookup
{
    public static bool TryGet(string npcId, out NpcProfile profile)
    {
        profile = npcId switch
        {
            var id when id == StarterNpcs.Mara.Id => StarterNpcs.Mara,
            var id when id == StarterNpcs.Dallen.Id => StarterNpcs.Dallen,
            _ => null
        };

        return profile is not null;
    }
}
