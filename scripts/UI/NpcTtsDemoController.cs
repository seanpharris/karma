using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Karma.Audio;
using Karma.Data;
using Karma.Net;
using Karma.Player;
using Karma.Voice;

namespace Karma.UI;

public partial class NpcTtsDemoController : Node2D
{
    private const int MaxTranscriptLines = 10;
    private static readonly Vector2 DemoPlayerSpawnPosition = new(-240f, 128f);
    private const string GroundPath = "res://assets/art/themes/medieval/boarding_school/grass_tiles_1_32.png";
    private const string BuildingPath = "res://assets/art/themes/medieval/buildings/smithy.png";
    private const string PlayerPath = "res://assets/art/sprites/themes/medieval/generated/acolyte_female_32x64_8dir_4row.png";
    private const string MaraPath = "res://assets/art/sprites/themes/medieval/generated/alchemist_male_32x64_8dir_4row.png";
    private const string LastRecordingUserPath = "user://stt/mara-ptt.wav";
    private static readonly HashSet<string> RejectedShortTranscripts = new(StringComparer.OrdinalIgnoreCase)
    {
        "you",
        "yeah",
        "ya",
        "uh",
        "um",
        "hmm",
        "mm",
        "the",
        "he",
        "i",
        "so"
    };
    private static readonly string[] MaraAmbientGreetingsFallback =
    {
        "Well hello there, traveler. Mind the sparks. What brings you by?",
        "Easy there. You nearly caught the hot side of the forge. What can I do for you?",
        "You look like you have come a fair way. Looking for something, are you?",
        "Afternoon. If you are here for the forge, speak up. If not, mind the tools."
    };
    private static readonly string[] MaraIdleNudgesFallback =
    {
        "You have that look like something is on your mind. Go on, then.",
        "No rush, but if you came to ask something, I am listening.",
        "If you need a word, say it plain. I do not bite.",
        "Standing there thoughtful, are you? Speak when you are ready."
    };
    private static readonly string[] MaraThinkingCueLines =
    {
        "Hmm.",
        "Uhhh.",
        "Uhmmm."
    };

    [Export]
    public bool PreferLocalLlm { get; set; } = true;

    [Export]
    public bool FallbackToStubDialogue { get; set; } = true;

    [Export]
    public string LocalLlmBaseUrl { get; set; } = "http://127.0.0.1:18080";

    [Export]
    public string LocalLlmModel { get; set; } = "phi-3.5-mini-instruct";

    [Export]
    public float LocalLlmTemperature { get; set; } = 0.95f;

    [Export]
    public int LocalLlmMaxTokens { get; set; } = 120;

    [Export]
    public string VoiceboxSttBaseUrl { get; set; } = "http://127.0.0.1:17493";

    [Export]
    public string VoiceboxSttLanguage { get; set; } = "en";

    [Export]
    public double MinSttHoldSeconds { get; set; } = 0.15d;

    [Export]
    public int MinAcceptedTranscriptChars { get; set; } = 5;

    [Export]
    public float MaraConversationDistancePixels { get; set; } = 40f;

    [Export]
    public float MaraNoticeDistancePixels { get; set; } = 300f;

    [Export]
    public float MaraFarVoiceVolumeScale { get; set; } = 0f;

    [Export]
    public double MaraIdleNoticeDelaySeconds { get; set; } = 6.0d;

    [Export]
    public double MaraThinkingCueDelaySeconds { get; set; } = 1.2d;

    [Export]
    public float MaraFollowSpeedPixelsPerSecond { get; set; } = 84f;

    [Export]
    public float MaraFollowPreferredDistancePixels { get; set; } = 44f;

    private PrototypeServerSession _serverSession = null!;
    private VoiceboxSpeechPlayer _voiceboxPlayer = null!;
    private PlayerController _player = null!;
    private Area2D _npc = null!;
    private Label _statusLabel = null!;
    private Label _voiceStatusLabel = null!;
    private Label _promptStatusLabel = null!;
    private Label _sttStatusLabel = null!;
    private Label _recordingDebugLabel = null!;
    private PanelContainer _speechBubble = null!;
    private Label _speechBubbleLabel = null!;
    private Label _heardLineLabel = null!;
    private PanelContainer _recordingIndicator = null!;
    private RichTextLabel _transcriptLabel = null!;
    private Button _dialogueToggleButton = null!;
    private string _lastBubbleEventKey = string.Empty;
    private string _lastPromptSpoken = string.Empty;
    private string _lastThinkingCueLine = string.Empty;
    private double _speechBubbleExpiresAt = -1d;
    private bool _wasNearMara;
    private bool _pendingPromptSpeech;
    private bool _idleNudgeSpoken;
    private bool _playerRespondedSinceNotice;
    private bool _dialogueOpen;
    private bool _dialogueRequestInFlight;
    private bool _ambientLineRequestInFlight;
    private bool _npcSpeechInProgress;
    private bool _maraFollowingPlayer;
    private bool _sttRecordingActive;
    private ulong _sttRecordStartedAtMsec;
    private ulong _enteredNoticeAtMsec;
    private ulong _lastAmbientSpeechAtMsec;
    private ulong _lastPlayerEngagementAtMsec;
    private int _thinkingCueRequestSerial;
    private string[] _transcriptLines = Array.Empty<string>();
    private string[] _recentGreetingLines = Array.Empty<string>();
    private WindowsMicrophoneRecorder _windowsMicrophoneRecorder = null!;

    public override void _Ready()
    {
        _serverSession = GetNode<PrototypeServerSession>("/root/PrototypeServerSession");
        _voiceboxPlayer = GetNode<VoiceboxSpeechPlayer>("VoiceboxSpeechPlayer");
        _player = GetNode<PlayerController>("Player");
        _npc = GetNode<Area2D>("Npc");
        _windowsMicrophoneRecorder = new WindowsMicrophoneRecorder();
        ApplyTextures();
        BuildOverlay();
        BuildSpeechBubble();
        _voiceboxPlayer.StatusChanged += OnVoiceboxStatusChanged;
        _serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
        CallDeferred(nameof(EnsureDemoSpawnPosition));
        _statusLabel.Text = "Walk over to Mara and let her start the conversation.";
        SetDialogueOpen(false);
    }

    public override void _ExitTree()
    {
        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged -= OnLocalSnapshotChanged;
        }

        if (_voiceboxPlayer is not null)
        {
            _voiceboxPlayer.StatusChanged -= OnVoiceboxStatusChanged;
        }

        _windowsMicrophoneRecorder?.Dispose();
    }

    public override void _Process(double delta)
    {
        if (_speechBubble.Visible &&
            _speechBubbleExpiresAt > 0d &&
            Time.GetTicksMsec() >= _speechBubbleExpiresAt)
        {
            _speechBubble.Visible = false;
        }

        UpdateMaraVoiceVolume();
        UpdateMaraFollowMovement(delta);
        UpdateApproachSpeech();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Echo: false } sttKey && sttKey.Keycode == Key.F)
        {
            if (_dialogueOpen)
            {
                if (sttKey.Pressed)
                {
                    BeginSttRecording();
                }
                else
                {
                    _ = EndSttRecordingAndSubmitAsync();
                }

                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        if (key.Keycode == Key.E)
        {
            if (CanOpenDialogue())
            {
                SetDialogueOpen(!_dialogueOpen);
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (_dialogueOpen && key.Keycode == Key.Escape)
        {
            SetDialogueOpen(false);
            GetViewport().SetInputAsHandled();
        }
    }

    private void ApplyTextures()
    {
        ApplyTexture("GroundTile", GroundPath);
        ApplyTexture("Smithy", BuildingPath);
        ApplyTexture("Player/PlayerSprite", PlayerPath);
        ApplyTexture("Npc/NpcSprite", MaraPath);
    }

    private void ApplyTexture(string nodePath, string texturePath)
    {
        var sprite = GetNodeOrNull<Sprite2D>(nodePath);
        if (sprite is null)
        {
            return;
        }

        // Load directly from source PNG so this demo remains robust even when
        // import metadata is stale on a fresh checkout.
        var image = Image.LoadFromFile(ProjectSettings.GlobalizePath(texturePath));
        if (image is null || image.IsEmpty())
        {
            GD.PushWarning($"TTS demo could not load texture: {texturePath}");
            return;
        }

        sprite.Texture = ImageTexture.CreateFromImage(image);
    }

    private void BuildOverlay()
    {
        var layer = new CanvasLayer { Name = "DemoOverlay", Layer = 25 };
        AddChild(layer);

        var panel = new PanelContainer
        {
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 0f,
            AnchorBottom = 0f,
            OffsetLeft = 12f,
            OffsetTop = 12f,
            OffsetRight = 560f,
            OffsetBottom = 380f
        };
        layer.AddChild(panel);

        var root = new VBoxContainer();
        panel.AddChild(root);

        var title = new Label { Text = "Mara Voice Demo" };
        title.AddThemeFontSizeOverride("font_size", 20);
        root.AddChild(title);

        root.AddChild(new Label { Text = "WASD move. Walk up to Mara to hear her speak." });
        root.AddChild(new Label { Text = "Press E when Mara notices you, then hold F to talk." });

        _dialogueToggleButton = new Button { Text = "Open Dialogue" };
        _dialogueToggleButton.Pressed += OnDialogueTogglePressed;
        root.AddChild(_dialogueToggleButton);

        var heardTitle = new Label { Text = "Last heard" };
        heardTitle.AddThemeFontSizeOverride("font_size", 16);
        root.AddChild(heardTitle);

        _heardLineLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Your spoken line will appear here after transcription."
        };
        root.AddChild(_heardLineLabel);

        _statusLabel = new Label();
        root.AddChild(_statusLabel);

        _voiceStatusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Mara voice status will appear here."
        };
        root.AddChild(_voiceStatusLabel);

        _promptStatusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Mara thought process status will appear here."
        };
        root.AddChild(_promptStatusLabel);

        _sttStatusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Open dialogue, hold F to speak, then release to send."
        };
        root.AddChild(_sttStatusLabel);

        _recordingIndicator = BuildRecordingIndicator();
        root.AddChild(_recordingIndicator);

        var defaultMicName = OperatingSystem.IsWindows() && WindowsMicrophoneRecorder.HasInputDevice
            ? WindowsMicrophoneRecorder.GetDefaultDeviceName()
            : "No Windows microphone device detected";
        _recordingDebugLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = $"Mic: {defaultMicName}\nRecording path: {ProjectSettings.GlobalizePath(LastRecordingUserPath)}"
        };
        root.AddChild(_recordingDebugLabel);

        _transcriptLabel = new RichTextLabel
        {
            CustomMinimumSize = new Vector2(520f, 150f),
            ScrollActive = true,
            BbcodeEnabled = false
        };
        _transcriptLabel.Text = "Conversation transcript will appear here.";
        root.AddChild(_transcriptLabel);

    }

    private void BuildSpeechBubble()
    {
        var npc = GetNode<Area2D>("Npc");
        _speechBubble = new PanelContainer
        {
            Name = "SpeechBubble",
            Position = new Vector2(-110f, -122f),
            Size = new Vector2(220f, 88f),
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 50
        };

        var bubbleStyle = new StyleBoxFlat
        {
            BgColor = new Color("f5ebd5"),
            BorderColor = new Color("5a4030"),
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        _speechBubble.AddThemeStyleboxOverride("panel", bubbleStyle);

        _speechBubbleLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        _speechBubbleLabel.AddThemeColorOverride("font_color", new Color("2e221b"));
        _speechBubbleLabel.AddThemeFontSizeOverride("font_size", 16);

        _speechBubble.AddChild(_speechBubbleLabel);
        npc.AddChild(_speechBubble);
    }

    private void OnLocalSnapshotChanged(string _)
    {
        var snapshot = _serverSession?.LastLocalSnapshot;
        if (snapshot is null)
        {
            return;
        }

        UpdateStatusFromSnapshot(snapshot);

        if (snapshot.ServerEvents.Count == 0)
        {
            return;
        }

        for (var i = snapshot.ServerEvents.Count - 1; i >= 0; i--)
        {
            var serverEvent = snapshot.ServerEvents[i];
            var lineKey = serverEvent.EventId.Contains("dialogue_started")
                ? "npcPrompt"
                : serverEvent.EventId.Contains("dialogue_choice_selected")
                    ? "npcResponse"
                    : string.Empty;
            if (string.IsNullOrEmpty(lineKey) ||
                !serverEvent.Data.TryGetValue("npcId", out var npcId) ||
                npcId != StarterNpcs.Mara.Id ||
                !serverEvent.Data.TryGetValue(lineKey, out var line))
            {
                continue;
            }

            if (lineKey == "npcPrompt")
            {
                continue;
            }

            var eventKey = $"{serverEvent.Tick}:{serverEvent.EventId}:{lineKey}";
            if (eventKey == _lastBubbleEventKey)
            {
                continue;
            }

            serverEvent.Data.TryGetValue("npcName", out var npcName);
            var spokenLine = NormalizeDialogueLine(npcName, line);
            ShowSpeechBubble(npcName, spokenLine, lineKey == "npcPrompt" ? 5.5 : 4.5);
            AppendTranscript(string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName, spokenLine);
            if (lineKey == "npcResponse")
            {
                _voiceboxPlayer?.SpeakLine(spokenLine);
            }
            _lastBubbleEventKey = eventKey;
            return;
        }
    }

    private void EnsureDemoSpawnPosition()
    {
        if (_serverSession is null || _player is null)
        {
            return;
        }

        var targetTileX = Mathf.RoundToInt(DemoPlayerSpawnPosition.X / 32f);
        var targetTileY = Mathf.RoundToInt(DemoPlayerSpawnPosition.Y / 32f);
        var snapshot = _serverSession.LastLocalSnapshot;
        var localPlayer = snapshot?.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId);

        if (localPlayer?.TileX == targetTileX && localPlayer.TileY == targetTileY)
        {
            _player.GlobalPosition = DemoPlayerSpawnPosition;
            return;
        }

        _serverSession.SendLocal(
            IntentType.Move,
            new Dictionary<string, string>
            {
                ["x"] = targetTileX.ToString(),
                ["y"] = targetTileY.ToString()
            });

        _player.GlobalPosition = DemoPlayerSpawnPosition;
    }

    private void UpdateApproachSpeech()
    {
        if (_serverSession?.LastLocalSnapshot is not { } snapshot || _player is null || _npc is null)
        {
            return;
        }

        var distanceToMara = GetDistanceToMara();
        var MaraCanNoticePlayer = _maraFollowingPlayer || distanceToMara <= MaraNoticeDistancePixels;
        if (!MaraCanNoticePlayer)
        {
            _wasNearMara = false;
            _pendingPromptSpeech = false;
            _idleNudgeSpoken = false;
            _playerRespondedSinceNotice = false;
            _enteredNoticeAtMsec = 0;
            _lastAmbientSpeechAtMsec = 0;
            if (_dialogueOpen)
            {
                SetDialogueOpen(false);
                _promptStatusLabel.Text = "Dialogue closed because you stepped away from Mara.";
            }
            return;
        }

        if (!_wasNearMara)
        {
            _wasNearMara = true;
            _pendingPromptSpeech = true;
            _idleNudgeSpoken = false;
            _playerRespondedSinceNotice = false;
            _enteredNoticeAtMsec = Time.GetTicksMsec();
            _lastAmbientSpeechAtMsec = 0;
            _lastPlayerEngagementAtMsec = 0;
        }

        var dialogue = snapshot.Dialogues.FirstOrDefault(candidate => candidate.NpcId == StarterNpcs.Mara.Id);
        if (dialogue is null)
        {
            return;
        }

        if (_pendingPromptSpeech && CanStartAmbientSpeech())
        {
            _pendingPromptSpeech = false;
            _ = SpeakAmbientLineAsync(dialogue.NpcName, AmbientSpeechKind.Greeting, distanceToMara);
            return;
        }

        if (_playerRespondedSinceNotice || _idleNudgeSpoken || _dialogueOpen)
        {
            return;
        }

        var now = Time.GetTicksMsec();
        var delayMsec = (ulong)Math.Max(0d, MaraIdleNoticeDelaySeconds * 1000d);
        if (now < _enteredNoticeAtMsec + delayMsec ||
            now < _lastAmbientSpeechAtMsec + delayMsec ||
            (_lastPlayerEngagementAtMsec > 0 && now < _lastPlayerEngagementAtMsec + delayMsec))
        {
            return;
        }

        if (!CanStartAmbientSpeech())
        {
            return;
        }

        _idleNudgeSpoken = true;
        _ = SpeakAmbientLineAsync(dialogue.NpcName, AmbientSpeechKind.SilentFollowUp, distanceToMara);
    }

    private void OnVoiceboxStatusChanged(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return;
        }

        if (status.StartsWith("Playing:", StringComparison.Ordinal))
        {
            _npcSpeechInProgress = true;
        }
        else if (status.StartsWith("Playback finished.", StringComparison.Ordinal) ||
                 status.StartsWith("Voicebox failed:", StringComparison.Ordinal) ||
                 status.StartsWith("Decoded audio was empty.", StringComparison.Ordinal))
        {
            _npcSpeechInProgress = false;
        }

        if (status.StartsWith("Playback finished.", StringComparison.Ordinal))
        {
            return;
        }

        _voiceStatusLabel.Text = status;
    }

    private void OnDialogueTogglePressed()
    {
        if (_dialogueOpen)
        {
            SetDialogueOpen(false);
            return;
        }

        if (!CanOpenDialogue())
        {
            _promptStatusLabel.Text = "Step into Mara's notice range before opening dialogue.";
            return;
        }

        _lastPlayerEngagementAtMsec = Time.GetTicksMsec();
        SetDialogueOpen(true);
    }

    private void BeginSttRecording()
    {
        if (!_dialogueOpen || _dialogueRequestInFlight || _sttRecordingActive)
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            _sttStatusLabel.Text = "Live microphone capture is only wired for Windows in this prototype.";
            return;
        }

        if (!WindowsMicrophoneRecorder.HasInputDevice)
        {
            _sttStatusLabel.Text = "No Windows microphone device was detected.";
            return;
        }

        var wavPath = ProjectSettings.GlobalizePath(LastRecordingUserPath);
        Directory.CreateDirectory(Path.GetDirectoryName(wavPath) ?? ProjectSettings.GlobalizePath("user://"));

        _sttRecordingActive = true;
        _sttRecordStartedAtMsec = Time.GetTicksMsec();
        _playerRespondedSinceNotice = true;
        _lastPlayerEngagementAtMsec = _sttRecordStartedAtMsec;
        _windowsMicrophoneRecorder.StartRecording(wavPath);

        _heardLineLabel.Text = "Listening...";
        SetRecordingIndicatorVisible(true);
        _sttStatusLabel.Text = "Recording... release F to transcribe.";
        _voiceStatusLabel.Text = $"Listening to microphone: {WindowsMicrophoneRecorder.GetDefaultDeviceName()}";
    }

    private async Task EndSttRecordingAndSubmitAsync()
    {
        if (!_sttRecordingActive)
        {
            return;
        }

        _sttRecordingActive = false;
        SetRecordingIndicatorVisible(false);
        _sttStatusLabel.Text = "Transcribing speech with Voicebox...";

        try
        {
            var heldSeconds = (Time.GetTicksMsec() - _sttRecordStartedAtMsec) / 1000d;
            var recording = await _windowsMicrophoneRecorder.StopRecordingAsync();
            var capturedSeconds = Math.Max(heldSeconds, recording.DurationSeconds);
            var effectiveSeconds = Math.Max(heldSeconds, capturedSeconds);
            if (effectiveSeconds < MinSttHoldSeconds)
            {
                _sttStatusLabel.Text = $"That recording was too short. Hold F and speak for a bit longer. ({effectiveSeconds:0.00}s)";
                return;
            }

            _recordingDebugLabel.Text =
                $"Mic: {recording.DeviceName}\nSaved recording: {recording.FilePath} | {capturedSeconds:0.00}s | {recording.FileSizeBytes} bytes";

            await TranscribeAudioFileAsync(recording.FilePath, "That sounded too short or unclear");
        }
        catch (Exception ex)
        {
            _sttStatusLabel.Text = $"Voicebox STT failed: {ex.Message}";
        }
    }

    private async Task SubmitPlayerLineAsync(string text)
    {
        var cleaned = NormalizeDialogueLine(string.Empty, text);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            _promptStatusLabel.Text = "No player speech was available to send to Mara.";
            return;
        }

        if (!CanOpenDialogue())
        {
            _promptStatusLabel.Text = "Walk up to Mara before sending a line.";
            return;
        }

        if (!_dialogueOpen)
        {
            SetDialogueOpen(true);
        }

        if (_dialogueRequestInFlight)
        {
            _promptStatusLabel.Text = "Mara is still thinking. Give her a moment.";
            return;
        }

        _heardLineLabel.Text = cleaned;
        AppendTranscript("You", cleaned);
        _playerRespondedSinceNotice = true;
        _lastPlayerEngagementAtMsec = Time.GetTicksMsec();
        _dialogueRequestInFlight = true;
        _thinkingCueRequestSerial++;
        _voiceStatusLabel.Text = "Waiting for Mara's reply...";
        _ = PlayThinkingCueIfReplyIsSlowAsync(_thinkingCueRequestSerial);
        var interactionRequest = ParseInteractionRequest(cleaned);

        var generatedReply = await TryGenerateDialogueReplyAsync(cleaned, interactionRequest);
        _dialogueRequestInFlight = false;
        if (!generatedReply.Success)
        {
            _promptStatusLabel.Text = $"Mara reply failed: {generatedReply.Error}";
            return;
        }

        ShowSpeechBubble(generatedReply.NpcName, generatedReply.Reply, 6.0);
        AppendTranscript(generatedReply.NpcName, generatedReply.Reply);
        _voiceboxPlayer?.SetVolumeScale(CalculateMaraVoiceVolumeScale(GetDistanceToMara()));
        _voiceboxPlayer?.SpeakLine(generatedReply.Reply);
        _voiceStatusLabel.Text = $"Speaking {generatedReply.NpcName} via {generatedReply.BackendLabel}.";
        _promptStatusLabel.Text = BuildInteractionStatusText(generatedReply.StatusText, interactionRequest, generatedReply.Reply);
        _sttStatusLabel.Text = "Hold F again whenever you want to reply.";
    }

    private async Task PlayThinkingCueIfReplyIsSlowAsync(int requestSerial)
    {
        var delayMs = (int)Math.Max(0d, MaraThinkingCueDelaySeconds * 1000d);
        if (delayMs > 0)
        {
            await Task.Delay(delayMs);
        }

        if (!_dialogueRequestInFlight ||
            requestSerial != _thinkingCueRequestSerial ||
            _voiceboxPlayer is null ||
            _player is null ||
            _npc is null ||
            _npcSpeechInProgress ||
            _ambientLineRequestInFlight ||
            _sttRecordingActive)
        {
            return;
        }

        var cue = PickAmbientLine(MaraThinkingCueLines, new[] { _lastThinkingCueLine });
        if (string.IsNullOrWhiteSpace(cue))
        {
            return;
        }

        var liveDistance = GetDistanceToMara();
        if (liveDistance > MaraNoticeDistancePixels)
        {
            return;
        }

        ShowSpeechBubble(StarterNpcs.Mara.Name, cue, 2.0);
        _voiceboxPlayer.SetVolumeScale(CalculateMaraVoiceVolumeScale(liveDistance));
        _voiceboxPlayer.SpeakLine(cue);
        _lastThinkingCueLine = cue;
    }

    private async Task<GeneratedDialogueReply> TryGenerateDialogueReplyAsync(string playerLine, InteractionRequest interactionRequest)
    {
        var promptError = string.Empty;
        var additionalRuntimeFields = BuildInteractionRuntimeContext(interactionRequest);
        var userPromptOverride = BuildInteractionUserPrompt(playerLine, interactionRequest);
        if (PreferLocalLlm &&
            NpcLlmPromptBuilder.TryBuild(
                _serverSession,
                StarterNpcs.Mara.Id,
                playerLine,
                out var promptPackage,
                out promptError,
                additionalRuntimeFields,
                userPromptOverride))
        {
            try
            {
                _promptStatusLabel.Text = "Sending Mara context to local Phi-3.5 mini...";
                var llmReply = await LlamaCppNpcDialogueClient.GenerateReplyAsync(
                    promptPackage,
                    new LlamaCppDialogueOptions(
                        LocalLlmBaseUrl,
                        LocalLlmModel,
                        LocalLlmTemperature,
                        LocalLlmMaxTokens));
                return GeneratedDialogueReply.FromLlm(
                    promptPackage.NpcName,
                    llmReply.Reply,
                    llmReply.Model,
                    $"Built prompt from {promptPackage.ContextPath} and generated a local Phi-3.5 mini reply.");
            }
            catch (Exception ex) when (FallbackToStubDialogue)
            {
                _promptStatusLabel.Text = $"Local Phi-3.5 mini was unavailable, falling back to stub: {ex.Message}";
            }
            catch (Exception ex)
            {
                return GeneratedDialogueReply.Failure(ex.Message);
            }
        }
        else if (!string.IsNullOrWhiteSpace(promptError))
        {
            return GeneratedDialogueReply.Failure(promptError);
        }

        if (!NpcDialogueTestBackend.TryGenerateReply(
                _serverSession,
                StarterNpcs.Mara.Id,
                playerLine,
                out var stubReply,
                out var stubError))
        {
            return GeneratedDialogueReply.Failure(stubError);
        }

        return GeneratedDialogueReply.FromStub(
            stubReply.PromptPackage.NpcName,
            stubReply.Reply,
            stubReply.BackendLabel,
            $"Built prompt from {stubReply.PromptPackage.ContextPath} and generated a stub test reply.");
    }

    private async Task SpeakAmbientLineAsync(string npcName, AmbientSpeechKind kind, float requestedDistance)
    {
        _ambientLineRequestInFlight = true;
        try
        {
            var line = await TryGenerateAmbientLineAsync(kind);
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (_player is null || _npc is null)
            {
                return;
            }

            var liveDistance = GetDistanceToMara();
            if (liveDistance > MaraNoticeDistancePixels)
            {
                return;
            }

            var normalized = NormalizeDialogueLine(npcName, line);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (kind == AmbientSpeechKind.Greeting &&
                _recentGreetingLines.Any(previous => string.Equals(previous, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                normalized = PickAmbientLine(MaraAmbientGreetingsFallback, _recentGreetingLines);
            }
            else if (string.Equals(normalized, _lastPromptSpoken, StringComparison.OrdinalIgnoreCase))
            {
                normalized = PickAmbientLine(
                    kind == AmbientSpeechKind.Greeting ? MaraAmbientGreetingsFallback : MaraIdleNudgesFallback,
                    kind == AmbientSpeechKind.Greeting
                        ? _recentGreetingLines.Append(_lastPromptSpoken).ToArray()
                        : new[] { _lastPromptSpoken });
            }

            ShowSpeechBubble(npcName, normalized, kind == AmbientSpeechKind.Greeting ? 5.5 : 4.5);
            AppendTranscript(npcName, normalized);
            _voiceboxPlayer?.SetVolumeScale(CalculateMaraVoiceVolumeScale(Math.Min(requestedDistance, liveDistance)));
            _voiceboxPlayer?.SpeakLine(normalized);
            _lastPromptSpoken = normalized;
            if (kind == AmbientSpeechKind.Greeting)
            {
                RememberGreetingLine(normalized);
            }
            _lastAmbientSpeechAtMsec = Time.GetTicksMsec();
        }
        finally
        {
            _ambientLineRequestInFlight = false;
        }
    }

    private async Task<string> TryGenerateAmbientLineAsync(AmbientSpeechKind kind)
    {
        if (PreferLocalLlm &&
            NpcLlmPromptBuilder.TryBuild(
                _serverSession,
                StarterNpcs.Mara.Id,
                string.Empty,
                out var promptPackage,
                out _,
                BuildAmbientRuntimeContext(kind),
                BuildAmbientUserPrompt(kind)))
        {
            try
            {
                var llmReply = await LlamaCppNpcDialogueClient.GenerateReplyAsync(
                    promptPackage,
                    new LlamaCppDialogueOptions(
                        LocalLlmBaseUrl,
                        LocalLlmModel,
                        LocalLlmTemperature,
                        LocalLlmMaxTokens));
                return NormalizeDialogueLine(promptPackage.NpcName, llmReply.Reply);
            }
            catch (Exception ex) when (FallbackToStubDialogue)
            {
                _promptStatusLabel.Text = $"Ambient local LLM fallback: {ex.Message}";
            }
        }

        return PickAmbientLine(
            kind == AmbientSpeechKind.Greeting ? MaraAmbientGreetingsFallback : MaraIdleNudgesFallback,
            kind == AmbientSpeechKind.Greeting
                ? _recentGreetingLines.Append(_lastPromptSpoken).ToArray()
                : new[] { _lastPromptSpoken });
    }

    private PanelContainer BuildRecordingIndicator()
    {
        var indicator = new PanelContainer
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color("7b1e1e"),
            BorderColor = new Color("f2d7d7"),
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        indicator.AddThemeStyleboxOverride("panel", style);

        var label = new Label
        {
            Text = "Recording... release F when you finish speaking.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeColorOverride("font_color", new Color("fff4ef"));
        indicator.AddChild(label);
        return indicator;
    }

    private void SetRecordingIndicatorVisible(bool visible)
    {
        if (_recordingIndicator is not null)
        {
            _recordingIndicator.Visible = visible;
        }
    }

    private void UpdateStatusFromSnapshot(ClientInterestSnapshot snapshot)
    {
        if (_dialogueOpen)
        {
            _statusLabel.Text = "Dialogue open. Hold F to speak to Mara. Press Esc to close.";
            return;
        }

        if (_maraFollowingPlayer)
        {
            _statusLabel.Text = "Mara is following you. Press E to talk, or tell her to wait here.";
            return;
        }

        var dialogue = snapshot.Dialogues.FirstOrDefault(candidate => candidate.NpcId == StarterNpcs.Mara.Id);
        if (dialogue is null)
        {
            _statusLabel.Text = "Walk up to Mara to start talking.";
            return;
        }

        var distanceToMara = GetDistanceToMara();
        if (distanceToMara <= MaraConversationDistancePixels)
        {
            _statusLabel.Text = "Mara is ready. Press E to talk, then hold F when you want to speak.";
            return;
        }

        if (distanceToMara <= MaraNoticeDistancePixels)
        {
            _statusLabel.Text = "Mara can spot you from here. Press E to answer from here, or walk closer for a clearer conversation.";
            return;
        }

        _statusLabel.Text = "Walk up to Mara to start talking.";
    }

    private void ShowSpeechBubble(string npcName, string line, double visibleSeconds)
    {
        var bubbleText = FormatBubbleText(npcName, line);
        if (string.IsNullOrWhiteSpace(bubbleText))
        {
            return;
        }

        _speechBubbleLabel.Text = bubbleText;
        _speechBubble.Visible = true;
        _speechBubbleExpiresAt = Time.GetTicksMsec() + (visibleSeconds * 1000d);
    }

    private void AppendTranscript(string speaker, string line)
    {
        if (_transcriptLabel is null)
        {
            return;
        }

        var cleanedSpeaker = string.IsNullOrWhiteSpace(speaker) ? "Unknown" : speaker.Trim();
        var cleanedLine = NormalizeDialogueLine(string.Empty, line);
        if (string.IsNullOrWhiteSpace(cleanedLine))
        {
            return;
        }

        _transcriptLines = _transcriptLines
            .Append($"{cleanedSpeaker}: {cleanedLine}")
            .TakeLast(MaxTranscriptLines)
            .ToArray();
        _transcriptLabel.Text = string.Join("\n", _transcriptLines);
        _transcriptLabel.ScrollToLine(Math.Max(0, _transcriptLines.Length - 1));
    }

    private bool CanOpenDialogue()
    {
        return _player is not null &&
               _npc is not null &&
               GetDistanceToMara() <= MaraNoticeDistancePixels;
    }

    private void SetDialogueOpen(bool open)
    {
        _dialogueOpen = open;
        _player?.SetControlsEnabled(!open);
        if (_dialogueToggleButton is not null)
        {
            _dialogueToggleButton.Text = open ? "Close Dialogue" : "Open Dialogue";
        }

        if (_statusLabel is not null)
        {
            _statusLabel.Text = open
                ? "Dialogue open. Hold F to speak to Mara. Press Esc to close."
                : "Press E when Mara notices you to open dialogue.";
        }

        if (_sttStatusLabel is not null && !open)
        {
            _sttStatusLabel.Text = "Open dialogue, hold F to speak, then release to send.";
        }
    }

    private sealed record GeneratedDialogueReply(
        bool Success,
        string NpcName,
        string Reply,
        string BackendLabel,
        string StatusText,
        string Error)
    {
        public static GeneratedDialogueReply FromLlm(string npcName, string reply, string modelName, string statusText) =>
            new(true, npcName, reply, $"local llama.cpp ({modelName})", statusText, string.Empty);

        public static GeneratedDialogueReply FromStub(string npcName, string reply, string backendLabel, string statusText) =>
            new(true, npcName, reply, backendLabel, statusText, string.Empty);

        public static GeneratedDialogueReply Failure(string error) =>
            new(false, StarterNpcs.Mara.Name, string.Empty, string.Empty, string.Empty, error);
    }

    private static string FormatBubbleText(string npcName, string line)
    {
        var cleaned = NormalizeDialogueLine(npcName, line);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(npcName)
            ? cleaned
            : $"{npcName}: {cleaned}";
    }

    private static string NormalizeDialogueLine(string npcName, string line)
    {
        var cleaned = string.IsNullOrWhiteSpace(line)
            ? string.Empty
            : line.Replace('\n', ' ').Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(npcName) &&
            cleaned.StartsWith($"{npcName}:", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[(npcName.Length + 1)..].Trim();
        }

        if (cleaned.StartsWith("\"", StringComparison.Ordinal) &&
            cleaned.EndsWith("\"", StringComparison.Ordinal) &&
            cleaned.Length > 1)
        {
            cleaned = cleaned[1..^1].Trim();
        }

        return cleaned;
    }

    private bool LooksLikeRejectedShortTranscript(string text)
    {
        var cleaned = NormalizeDialogueLine(string.Empty, text);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return true;
        }

        if (cleaned.Length < MinAcceptedTranscriptChars)
        {
            return true;
        }

        return RejectedShortTranscripts.Contains(cleaned);
    }

    private void UpdateMaraVoiceVolume()
    {
        if (_voiceboxPlayer is null || _player is null || _npc is null)
        {
            return;
        }

        _voiceboxPlayer.SetVolumeScale(CalculateMaraVoiceVolumeScale(GetDistanceToMara()));
    }

    private float CalculateMaraVoiceVolumeScale(float distanceToMara)
    {
        if (distanceToMara >= MaraNoticeDistancePixels)
        {
            return 0f;
        }

        if (distanceToMara <= MaraConversationDistancePixels)
        {
            return 1f;
        }

        var range = Math.Max(1f, MaraNoticeDistancePixels - MaraConversationDistancePixels);
        var t = 1f - Mathf.Clamp((distanceToMara - MaraConversationDistancePixels) / range, 0f, 1f);
        return Mathf.Lerp(MaraFarVoiceVolumeScale, 1f, t);
    }

    private void UpdateMaraFollowMovement(double delta)
    {
        if (!_maraFollowingPlayer || _player is null || _npc is null)
        {
            return;
        }

        var distanceToPlayer = GetDistanceToMara();
        if (distanceToPlayer <= MaraFollowPreferredDistancePixels)
        {
            return;
        }

        var stepDistance = Math.Max(0f, MaraFollowSpeedPixelsPerSecond * (float)delta);
        var targetPosition = _npc.GlobalPosition.MoveToward(_player.GlobalPosition, stepDistance);
        if (targetPosition.DistanceTo(_player.GlobalPosition) < MaraFollowPreferredDistancePixels)
        {
            var directionAway = (targetPosition - _player.GlobalPosition).Normalized();
            if (directionAway.LengthSquared() > 0f)
            {
                targetPosition = _player.GlobalPosition + (directionAway * MaraFollowPreferredDistancePixels);
            }
        }

        _npc.GlobalPosition = targetPosition;
    }

    private float GetDistanceToMara()
    {
        if (_player is null || _npc is null)
        {
            return float.MaxValue;
        }

        return _player.GlobalPosition.DistanceTo(_npc.GlobalPosition);
    }

    private bool CanStartAmbientSpeech()
    {
        return !_dialogueOpen &&
               !_dialogueRequestInFlight &&
               !_ambientLineRequestInFlight &&
               !_sttRecordingActive &&
               !_npcSpeechInProgress;
    }

    private Dictionary<string, string> BuildAmbientRuntimeContext(AmbientSpeechKind kind)
    {
        var distance = GetDistanceToMara();
        var lingerSeconds = _enteredNoticeAtMsec == 0
            ? 0d
            : Math.Max(0d, (Time.GetTicksMsec() - _enteredNoticeAtMsec) / 1000d);
        var context = new Dictionary<string, string>
        {
            ["conversation_state"] = kind == AmbientSpeechKind.Greeting
                ? "The player has just entered Mara's notice range and has not spoken yet."
                : $"The player has lingered nearby for about {lingerSeconds:0.0} seconds without answering Mara.",
            ["player_response_state"] = _playerRespondedSinceNotice
                ? "The player has already answered Mara."
                : "The player has not answered Mara yet.",
            ["distance_to_player"] = $"{distance:0} pixels away.",
            ["last_npc_line"] = string.IsNullOrWhiteSpace(_lastPromptSpoken) ? "None yet." : _lastPromptSpoken,
            ["recent_ambient_greetings"] = _recentGreetingLines.Length == 0
                ? "None recently."
                : string.Join(" | ", _recentGreetingLines),
            ["ambient_goal"] = kind == AmbientSpeechKind.Greeting
                ? "Offer one natural greeting that fits Mara's work and current mood. Avoid repeating recent greeting wording."
                : "Offer one natural follow-up that notices the player's hesitation without sounding repetitive or annoyed too quickly."
        };
        return context;
    }

    private Dictionary<string, string> BuildInteractionRuntimeContext(InteractionRequest interactionRequest)
    {
        var fields = new Dictionary<string, string>();
        if (interactionRequest.Kind == InteractionRequestKind.None)
        {
            return fields;
        }

        var snapshot = _serverSession?.LastLocalSnapshot;
        var localPlayer = snapshot?.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId);
        var factionId = StarterFactions.ToId(StarterNpcs.Mara.Faction);
        var factionReputation = snapshot?.Factions.FirstOrDefault(candidate =>
            candidate.PlayerId == snapshot.PlayerId &&
            candidate.FactionId == factionId)?.Reputation ?? 0;
        var karmaScore = localPlayer?.Karma ?? 0;
        var standing = localPlayer?.Standing ?? LeaderboardRole.None;
        var followDisposition = ResolveFollowDisposition(karmaScore, standing, factionReputation);
        var urgency = ResolveMaraUrgency(snapshot);

        fields["interaction_request"] = interactionRequest.Kind switch
        {
            InteractionRequestKind.FollowPlayer => "The player is asking Mara to follow them.",
            InteractionRequestKind.StopFollowing => "The player is asking Mara to stop following and stay put.",
            _ => "No special interaction request."
        };
        fields["mara_follow_state"] = _maraFollowingPlayer
            ? "Mara is currently following the player."
            : "Mara is not currently following the player.";
        fields["follow_decision_basis"] =
            "Base follow decisions primarily on the player's karma and standing in Karma. Use Mara's urgency and current workload to decide whether the answer is yes right now or not yet.";
        fields["follow_disposition"] = followDisposition switch
        {
            FollowDisposition.Yes => "Karma says Mara is inclined to say yes if the moment allows it.",
            FollowDisposition.NotYet => "Karma says Mara is not ready to commit yet. She should lean toward not yet unless the situation strongly justifies going.",
            FollowDisposition.No => "Karma says Mara does not trust this player enough to follow them right now.",
            _ => "No clear follow disposition."
        };
        fields["player_follow_karma_summary"] =
            $"Player karma {karmaScore}, standing {standing}, faction reputation {factionReputation}.";
        fields["mara_current_urgency"] = urgency;
        fields["interaction_resolution_rule"] =
            "For follow requests, land on one of three clear outcomes: yes, no, or not yet. If Mara agrees, make the agreement plain in natural speech. If she refuses, make the refusal plain in natural speech. If she is not ready yet, make that hesitation unmistakable in natural speech.";
        return fields;
    }

    private string BuildInteractionUserPrompt(string playerLine, InteractionRequest interactionRequest)
    {
        if (interactionRequest.Kind == InteractionRequestKind.None)
        {
            return null;
        }

        return interactionRequest.Kind switch
        {
            InteractionRequestKind.FollowPlayer =>
                $"The player says: \"{playerLine}\"\nThey are asking Mara to come along and follow them in-world. Base Mara's decision primarily on the player's karma in Karma, along with standing and faction reputation. Use urgency and Mara's current work to decide whether the answer is yes right now or not yet. Do not hedge forever: choose yes, no, or not yet, and make that outcome unmistakable in Mara's spoken reply. Respond with Mara's spoken reply only.",
            InteractionRequestKind.StopFollowing =>
                $"The player says: \"{playerLine}\"\nThey are asking Mara to stop following and stay here. If Mara agrees, have her say so plainly in natural language. If she objects, make that clear too. Respond with Mara's spoken reply only.",
            _ => null
        };
    }

    private static string BuildAmbientUserPrompt(AmbientSpeechKind kind)
    {
        return kind == AmbientSpeechKind.Greeting
            ? "The player has just come within Mara's notice range but has not spoken yet. Give Mara's first spoken line for this moment."
            : "The player is still nearby and has not answered Mara yet. Give Mara's next spoken follow-up line only.";
    }

    private void RememberGreetingLine(string line)
    {
        var cleaned = NormalizeDialogueLine(string.Empty, line);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return;
        }

        _recentGreetingLines = _recentGreetingLines
            .Where(existing => !string.Equals(existing, cleaned, StringComparison.OrdinalIgnoreCase))
            .Append(cleaned)
            .TakeLast(3)
            .ToArray();
    }

    private static string PickAmbientLine(IReadOnlyList<string> options, IEnumerable<string> excludedLines)
    {
        if (options is null || options.Count == 0)
        {
            return string.Empty;
        }

        if (options.Count == 1)
        {
            return options[0];
        }

        var excluded = new HashSet<string>(
            (excludedLines ?? Array.Empty<string>()).Where(line => !string.IsNullOrWhiteSpace(line)),
            StringComparer.OrdinalIgnoreCase);
        var candidates = options
            .Where(option => !excluded.Contains(option))
            .ToArray();
        if (candidates.Length == 0)
        {
            candidates = options.ToArray();
        }

        return candidates[Random.Shared.Next(candidates.Length)];
    }

    private InteractionRequest ParseInteractionRequest(string playerLine)
    {
        var cleaned = NormalizeDialogueLine(string.Empty, playerLine).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return new InteractionRequest(InteractionRequestKind.None, string.Empty);
        }

        if (ContainsAny(cleaned,
                "follow me",
                "come with me",
                "come along",
                "walk with me",
                "join me"))
        {
            return new InteractionRequest(InteractionRequestKind.FollowPlayer, cleaned);
        }

        if (ContainsAny(cleaned,
                "wait here",
                "stay here",
                "stay put",
                "stop following",
                "hang back"))
        {
            return new InteractionRequest(InteractionRequestKind.StopFollowing, cleaned);
        }

        return new InteractionRequest(InteractionRequestKind.None, cleaned);
    }

    private string BuildInteractionStatusText(string baseStatusText, InteractionRequest interactionRequest, string reply)
    {
        var interactionStatus = ResolveInteractionOutcome(interactionRequest, reply);
        return string.IsNullOrWhiteSpace(interactionStatus)
            ? baseStatusText
            : $"{baseStatusText} {interactionStatus}".Trim();
    }

    private string ResolveInteractionOutcome(InteractionRequest interactionRequest, string reply)
    {
        if (interactionRequest.Kind == InteractionRequestKind.None)
        {
            return string.Empty;
        }

        var cleanedReply = NormalizeDialogueLine(string.Empty, reply).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cleanedReply))
        {
            return string.Empty;
        }

        return interactionRequest.Kind switch
        {
            InteractionRequestKind.FollowPlayer => TryResolveFollowAcceptance(cleanedReply),
            InteractionRequestKind.StopFollowing => TryResolveStopFollowingAcceptance(cleanedReply),
            _ => string.Empty
        };
    }

    private string TryResolveFollowAcceptance(string cleanedReply)
    {
        if (ContainsAny(cleanedReply, "not yet", "not now", "can't", "cannot", "won't", "will not", "stay here", "i've work", "i have work", "another time", "earn that first") ||
            StartsWithAny(cleanedReply, "no", "not a chance"))
        {
            _maraFollowingPlayer = false;
            return cleanedReply.Contains("not yet", StringComparison.OrdinalIgnoreCase) ||
                   cleanedReply.Contains("not now", StringComparison.OrdinalIgnoreCase) ||
                   cleanedReply.Contains("another time", StringComparison.OrdinalIgnoreCase)
                ? "Mara said not yet."
                : "Mara refused to follow.";
        }

        if (ContainsAny(cleanedReply,
                "i'll follow",
                "i will follow",
                "i'll come",
                "i will come",
                "come with you",
                "lead on",
                "very well",
                "all right",
                "alright",
                "show me",
                "go on then"))
        {
            _maraFollowingPlayer = true;
            return "Mara agreed to follow you.";
        }

        return string.Empty;
    }

    private static FollowDisposition ResolveFollowDisposition(int karmaScore, LeaderboardRole standing, int factionReputation)
    {
        if (standing == LeaderboardRole.Scourge || karmaScore <= -40 || factionReputation <= -20)
        {
            return FollowDisposition.No;
        }

        if (standing == LeaderboardRole.Saint || karmaScore >= 120 || factionReputation >= 35)
        {
            return FollowDisposition.Yes;
        }

        return FollowDisposition.NotYet;
    }

    private string ResolveMaraUrgency(ClientInterestSnapshot snapshot)
    {
        var dialoguePrompt = snapshot?.Dialogues
            .FirstOrDefault(candidate => candidate.NpcId == StarterNpcs.Mara.Id)?
            .Prompt;
        var currentProblem = string.IsNullOrWhiteSpace(dialoguePrompt)
            ? StarterNpcs.Mara.Need
            : dialoguePrompt;
        var normalized = NormalizeDialogueLine(string.Empty, currentProblem).ToLowerInvariant();

        if (ContainsAny(normalized, "clinic", "sick", "children", "nightfall", "urgent", "filter"))
        {
            return $"High. Mara is tied up with urgent work: {currentProblem}";
        }

        return $"Moderate. Mara is busy, but not in immediate crisis: {currentProblem}";
    }

    private string TryResolveStopFollowingAcceptance(string cleanedReply)
    {
        if (ContainsAny(cleanedReply, "not yet", "won't", "will not", "can't", "cannot") ||
            StartsWithAny(cleanedReply, "no"))
        {
            return "Mara kept following.";
        }

        if (ContainsAny(cleanedReply,
                "i'll stay",
                "i will stay",
                "stay put",
                "wait here",
                "i'll wait",
                "i will wait",
                "fine",
                "all right",
                "alright"))
        {
            _maraFollowingPlayer = false;
            return "Mara stopped and stayed put.";
        }

        return string.Empty;
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool StartsWithAny(string text, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private enum AmbientSpeechKind
    {
        Greeting,
        SilentFollowUp
    }

    private enum InteractionRequestKind
    {
        None,
        FollowPlayer,
        StopFollowing
    }

    private enum FollowDisposition
    {
        No,
        NotYet,
        Yes
    }

    private sealed record InteractionRequest(
        InteractionRequestKind Kind,
        string PlayerLine);

    private async Task TranscribeAudioFileAsync(string audioPath, string unclearPrefix = "That sounded too short or unclear")
    {
        try
        {
            _sttStatusLabel.Text = $"Transcribing {Path.GetFileName(audioPath)} with Voicebox...";
            var transcription = await VoiceboxSttClient.TranscribeFileAsync(
                VoiceboxSttBaseUrl,
                audioPath,
                VoiceboxSttLanguage);
            var text = NormalizeDialogueLine(string.Empty, transcription.Text);
            if (string.IsNullOrWhiteSpace(text))
            {
                _sttStatusLabel.Text = "Voicebox returned an empty transcript.";
                return;
            }

            if (LooksLikeRejectedShortTranscript(text))
            {
                _heardLineLabel.Text = text;
                _sttStatusLabel.Text = $"{unclearPrefix}: \"{text}\".";
                return;
            }

            _heardLineLabel.Text = text;
            _sttStatusLabel.Text = $"Heard: \"{text}\"";
            await SubmitPlayerLineAsync(text);
        }
        catch (Exception ex)
        {
            _sttStatusLabel.Text = $"Voicebox STT failed: {ex.Message}";
        }
    }

}
