using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Core;
using Karma.Data;
using Karma.Net;
using Karma.UI;

namespace Karma.Npc;

public partial class NpcController : Area2D
{
    private bool _playerNearby;
    private readonly List<string> _choiceIds = new();
    private HudController _hud;
    private PrototypeServerSession _serverSession;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        _hud = GetNodeOrNull<HudController>("/root/Main/Hud");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerNearby || !(@event is InputEventKey { Pressed: true, Echo: false } key))
        {
            return;
        }

        var choiceIndex = KeyToChoiceIndex(key.Keycode);
        if (choiceIndex >= 0 && choiceIndex < _choiceIds.Count)
        {
            SelectDialogueChoice(_choiceIds[choiceIndex]);
        }
        else if (key.Keycode == Key.Key6)
        {
            StartOrCompleteClinicQuest();
        }
        else if (key.Keycode == Key.Key7)
        {
            StartMaraEntanglement();
        }
        else if (key.Keycode == Key.Key8)
        {
            ExposeMaraEntanglement();
        }
        else if (key.Keycode == Key.Key9)
        {
            _serverSession?.SendLocal(
                IntentType.KarmaBreak,
                new System.Collections.Generic.Dictionary<string, string>());
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is CharacterBody2D)
        {
            _playerNearby = true;
            StartDialogue();
            ShowPromptFromSnapshot();
            GD.Print("Mara interaction choices available.");
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is CharacterBody2D)
        {
            _playerNearby = false;
            _hud?.HidePrompt();
        }
    }

    private void StartDialogue()
    {
        if (_serverSession is null)
        {
            return;
        }

        var result = _serverSession.SendLocal(
            IntentType.StartDialogue,
            new Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id
            });
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
        }
    }

    private void SelectDialogueChoice(string choiceId)
    {
        if (_serverSession is null)
        {
            return;
        }

        var result = _serverSession.SendLocal(
            IntentType.SelectDialogueChoice,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id,
                ["choiceId"] = choiceId
            });
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
            return;
        }

        ShowPromptFromSnapshot();
    }

    private void StartOrCompleteClinicQuest()
    {
        var gameState = GetNode<GameState>("/root/GameState");
        var quest = gameState.Quests.Get(StarterQuests.MaraClinicFiltersId);
        if (_serverSession is null)
        {
            return;
        }

        var intentType = quest.Status == QuestStatus.Available
            ? IntentType.StartQuest
            : IntentType.CompleteQuest;
        var result = _serverSession.SendLocal(
            intentType,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = StarterQuests.MaraClinicFiltersId
            });
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
            return;
        }

        ShowPromptFromSnapshot();
    }

    private void StartMaraEntanglement()
    {
        if (_serverSession is null)
        {
            return;
        }

        var result = _serverSession.SendLocal(
            IntentType.StartEntanglement,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id,
                ["affectedNpcId"] = StarterNpcs.Dallen.Id,
                ["type"] = EntanglementType.Romantic.ToString(),
                ["action"] = PrototypeActions.StartMaraEntanglementId
            });
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
            return;
        }

        ShowPromptFromSnapshot();
    }

    private void ExposeMaraEntanglement()
    {
        var gameState = GetNode<GameState>("/root/GameState");
        if (_serverSession is null)
        {
            return;
        }

        if (!gameState.Entanglements.TryGetActive(
                GameState.LocalPlayerId,
                StarterNpcs.Mara.Id,
                EntanglementType.Romantic,
                out var entanglement))
        {
            _hud?.ShowPrompt("There is no active scandal to expose.");
            return;
        }

        var result = _serverSession.SendLocal(
            IntentType.ExposeEntanglement,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entanglementId"] = entanglement.Id,
                ["action"] = PrototypeActions.ExposeMaraEntanglementId
            });
        if (!result.WasAccepted)
        {
            _hud?.ShowPrompt(result.RejectionReason);
            return;
        }

        ShowPromptFromSnapshot();
    }

    private void ShowPromptFromSnapshot()
    {
        _choiceIds.Clear();
        var dialogue = _serverSession?.LastLocalSnapshot?.Dialogues
            .FirstOrDefault(candidate => candidate.NpcId == StarterNpcs.Mara.Id);
        var lines = new List<string>
        {
            "Mara Venn needs clinic filters fixed.",
            string.Empty
        };

        if (dialogue is null)
        {
            lines.Add("Dialogue unavailable.");
        }
        else
        {
            foreach (var choice in dialogue.Choices.Take(5))
            {
                _choiceIds.Add(choice.Id);
                lines.Add($"{_choiceIds.Count} - {choice.Label}");
            }
        }

        lines.Add("6 - Start/complete Clinic Filters quest");
        lines.Add("7 - Start a secret entanglement");
        lines.Add("8 - Expose the secret entanglement");
        lines.Add("9 - Test Karma Break");
        _hud?.ShowPrompt(string.Join("\n", lines));
    }

    private static int KeyToChoiceIndex(Key key)
    {
        return key switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            _ => -1
        };
    }
}
