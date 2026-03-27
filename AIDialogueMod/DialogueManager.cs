using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIDialogueMod.Actions;
using AIDialogueMod.AI;
using AIDialogueMod.Config;
using AIDialogueMod.Personality;
using MegaCrit.Sts2.Core.Logging;

namespace AIDialogueMod;

public enum DialogueState
{
    Idle, WaitingForPlayer, WaitingForAI, Ended,
}

public class DialogueManager
{
    public const int MaxRounds = 5;

    private readonly AIService _aiService;
    private readonly PromptBuilder _promptBuilder;
    private readonly PersonalityGenerator _personalityGenerator;
    private readonly StolenCardManager _stolenCardManager;

    private string _systemPrompt = "";
    private List<ChatMessage> _conversationHistory = new();
    private List<PersonalityType> _currentPersonalities = new();
    private int _currentRound;

    public DialogueState State { get; private set; } = DialogueState.Idle;
    public int CurrentRound => _currentRound;
    public List<PersonalityType> CurrentPersonalities => _currentPersonalities;
    public StolenCardManager StolenCards => _stolenCardManager;

    /// <summary>Whether this manager has been started at least once (has context).</summary>
    public bool HasStarted => _conversationHistory.Count > 0;

    public event Action<string, string>? OnNpcMessage;
    public event Action<List<GameAction>>? OnActionsExecuted;
    public event Action? OnConversationEnded;
    public event Action? OnWaitingForAI;
    public event Action<string>? OnReplayPlayerMessage;

    /// <summary>Clear all event handlers (call before re-subscribing from a new panel).</summary>
    public void ClearEventHandlers()
    {
        OnNpcMessage = null;
        OnActionsExecuted = null;
        OnConversationEnded = null;
        OnWaitingForAI = null;
        OnReplayPlayerMessage = null;
    }

    public DialogueManager(ModConfig config)
    {
        _aiService = new AIService(config);
        _promptBuilder = new PromptBuilder();
        _personalityGenerator = new PersonalityGenerator();
        _stolenCardManager = new StolenCardManager();
    }

    public async Task StartDialogue(
        string characterName, CharacterType characterType, string language,
        int playerHp, int playerMaxHp, int playerGold, string enemyInfo)
    {
        try
        {
            _currentRound = 0;
            _conversationHistory = new List<ChatMessage>();
            _currentPersonalities = _personalityGenerator.Generate(characterType);
            _systemPrompt = _promptBuilder.BuildSystemPrompt(
                characterName, characterType, _currentPersonalities,
                language, playerHp, playerMaxHp, playerGold, enemyInfo);

            State = DialogueState.WaitingForAI;
            OnWaitingForAI?.Invoke();

            string openingPrompt = _promptBuilder.BuildOpeningMessage(language);
            _conversationHistory.Add(new ChatMessage { Role = "user", Content = openingPrompt });

            string rawResponse = await _aiService.SendMessageAsync(_systemPrompt, _conversationHistory);
            HandleAIResponse(rawResponse, isOpening: true);
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] StartDialogue failed: {ex.Message}");
            EndConversation();
        }
    }

    /// <summary>
    /// Resume a previously started dialogue. Re-emits the last NPC message
    /// so the new panel shows the conversation state, then waits for player input.
    /// </summary>
    public void ResumeDialogue()
    {
        if (State == DialogueState.Ended)
        {
            // Already ended — just notify
            OnConversationEnded?.Invoke();
            return;
        }
        // Re-emit all previous messages for the new panel
        for (int i = 0; i < _conversationHistory.Count; i++)
        {
            var msg = _conversationHistory[i];
            if (msg.Role == "user" && i > 0) // skip the opening prompt (index 0)
            {
                // Extract just the player text from the wrapped prompt
                OnReplayPlayerMessage?.Invoke(msg.Content);
            }
            else if (msg.Role == "assistant")
            {
                var parsed = ResponseParser.Parse(msg.Content);
                OnNpcMessage?.Invoke(parsed.Dialogue, parsed.Emotion);
            }
        }
        State = DialogueState.WaitingForPlayer;
    }

    public async Task SendPlayerMessage(string message, int playerHp, int playerGold)
    {
        if (State != DialogueState.WaitingForPlayer) return;
        try
        {
            _currentRound++;
            State = DialogueState.WaitingForAI;
            OnWaitingForAI?.Invoke();

            string userMsg = _promptBuilder.BuildUserMessage(message, _currentRound, MaxRounds);
            _conversationHistory.Add(new ChatMessage { Role = "user", Content = userMsg });

            string rawResponse = await _aiService.SendMessageAsync(_systemPrompt, _conversationHistory);
            HandleAIResponse(rawResponse, isOpening: false, playerHp: playerHp, playerGold: playerGold);
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] SendPlayerMessage failed: {ex.Message}");
            OnNpcMessage?.Invoke(
                _systemPrompt.Contains("中文") ? "（对方沉默了一会...）" : "(They are silent for a moment...)",
                "neutral");
            State = DialogueState.WaitingForPlayer;
            if (_currentRound >= MaxRounds) EndConversation();
        }
    }

    public void AbandonDialogue() => EndConversation();

    private void HandleAIResponse(string rawResponse, bool isOpening, int playerHp = int.MaxValue, int playerGold = int.MaxValue)
    {
        AIResponse response;
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            response = new AIResponse
            {
                Dialogue = _systemPrompt.Contains("中文") ? "（对方沉默了一会...）" : "(They are silent for a moment...)",
                Emotion = "neutral",
            };
        }
        else
        {
            response = ResponseParser.Parse(rawResponse);
        }

        _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = rawResponse });

        bool isEnding = response.EndConversation || _currentRound >= MaxRounds;
        var validatedActions = ActionValidator.Validate(
            response.Actions, _currentPersonalities, isEnding, playerHp, playerGold);

        OnNpcMessage?.Invoke(response.Dialogue, response.Emotion);
        if (validatedActions.Count > 0) OnActionsExecuted?.Invoke(validatedActions);

        if (isEnding) EndConversation();
        else State = DialogueState.WaitingForPlayer;
    }

    private void EndConversation()
    {
        State = DialogueState.Ended;
        OnConversationEnded?.Invoke();
    }
}
