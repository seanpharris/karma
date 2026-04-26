using Karma.Data;

namespace Karma.Core;

public static class PrototypeActions
{
    public const string HelpMaraId = "help_mara";
    public const string WhoopieCushionMaraId = "whoopie_cushion_mara";
    public const string StealFromMaraId = "steal_from_mara";
    public const string GiftBalloonToMaraId = "gift_balloon_to_mara";
    public const string MockMaraWithBalloonId = "mock_mara_with_balloon";
    public const string HelpPeerId = "help_peer";
    public const string AttackPeerId = "attack_peer";
    public const string RobPeerId = "rob_peer";
    public const string ReturnPeerItemId = "return_peer_item";
    public const string StartMaraEntanglementId = "start_mara_entanglement";
    public const string ExposeMaraEntanglementId = "expose_mara_entanglement";

    public static bool TryGet(string actionId, out KarmaAction action)
    {
        action = actionId switch
        {
            HelpMaraId => HelpMara(),
            WhoopieCushionMaraId => WhoopieCushionMara(),
            StealFromMaraId => StealFromMara(),
            GiftBalloonToMaraId => GiftBalloonToMara(),
            MockMaraWithBalloonId => MockMaraWithBalloon(),
            HelpPeerId => HelpPeer(),
            AttackPeerId => AttackPeer(),
            RobPeerId => RobPeer(),
            ReturnPeerItemId => ReturnPeerItem(),
            StartMaraEntanglementId => StartMaraEntanglement(),
            ExposeMaraEntanglementId => ExposeMaraEntanglement(),
            _ => null
        };

        return action is not null;
    }

    public static KarmaAction HelpMara()
    {
        return new KarmaAction(
            "local_player",
            StarterNpcs.Mara.Id,
            new[] { "helpful", "generous" },
            "You helped Mara repair clinic filters.");
    }

    public static KarmaAction WhoopieCushionMara()
    {
        return new KarmaAction(
            "local_player",
            StarterNpcs.Mara.Id,
            new[] { "funny", "humiliating", "deceptive" },
            "You deployed a whoopie cushion during clinic hours.");
    }

    public static KarmaAction StealFromMara()
    {
        return new KarmaAction(
            "local_player",
            StarterNpcs.Mara.Id,
            new[] { "harmful", "selfish", "deceptive" },
            "You stole spare parts from Mara's workbench.");
    }

    public static KarmaAction GiftBalloonToMara()
    {
        return new KarmaAction(
            "local_player",
            StarterNpcs.Mara.Id,
            new[] { "helpful", "funny", "generous" },
            "You gave Mara a deflated balloon as a strangely sincere clinic mascot.");
    }

    public static KarmaAction MockMaraWithBalloon()
    {
        return new KarmaAction(
            "local_player",
            StarterNpcs.Mara.Id,
            new[] { "harmful", "humiliating", "selfish" },
            "You waved a deflated balloon at Mara's clinic crisis like it was a punchline.");
    }

    public static KarmaAction HelpPeer()
    {
        return new KarmaAction(
            "local_player",
            "peer_stand_in",
            new[] { "helpful", "protective", "generous" },
            "You helped a stranded player patch their gear.");
    }

    public static KarmaAction AttackPeer()
    {
        return new KarmaAction(
            "local_player",
            "peer_stand_in",
            new[] { "violent", "harmful", "chaotic" },
            "You attacked another player outside a duel.");
    }

    public static KarmaAction RobPeer()
    {
        return new KarmaAction(
            "local_player",
            "peer_stand_in",
            new[] { "harmful", "selfish", "deceptive" },
            "You robbed another player's dropped satchel.");
    }

    public static KarmaAction ReturnPeerItem()
    {
        return new KarmaAction(
            "local_player",
            "peer_stand_in",
            new[] { "helpful", "generous", "lawful" },
            "You returned the satchel you stole. Awkward, but better.");
    }

    public static KarmaAction StartMaraEntanglement()
    {
        return new KarmaAction(
            "local_player",
            StarterNpcs.Mara.Id,
            new[] { "romantic", "betrayal", "deceptive", "forbidden" },
            "You started a secret entanglement with Mara, betraying Dallen's trust.");
    }

    public static KarmaAction ExposeMaraEntanglement()
    {
        return new KarmaAction(
            "local_player",
            StarterNpcs.Mara.Id,
            new[] { "harmful", "humiliating", "betrayal", "chaotic" },
            "You exposed the secret entanglement and turned private harm into public damage.");
    }
}
