using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Karma.Voice;

public sealed record LlamaCppDialogueOptions(
    string BaseUrl,
    string Model,
    float Temperature = 0.7f,
    int MaxTokens = 80,
    float TopP = 0.95f,
    bool IncludeSystemPrompt = true);

public sealed record LlamaCppDialogueReply(
    string Reply,
    string Model,
    string RawResponseJson,
    NpcLlmPromptPackage PromptPackage);

public static class LlamaCppNpcDialogueClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(45) };

    public static async Task<LlamaCppDialogueReply> GenerateReplyAsync(
        NpcLlmPromptPackage promptPackage,
        LlamaCppDialogueOptions options)
    {
        if (promptPackage is null)
        {
            throw new ArgumentNullException(nameof(promptPackage));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var baseUrl = (options.BaseUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Local llama.cpp base URL is not configured.");
        }

        var messages = new List<Dictionary<string, string>>();
        if (options.IncludeSystemPrompt && !string.IsNullOrWhiteSpace(promptPackage.SystemPrompt))
        {
            messages.Add(new Dictionary<string, string>
            {
                ["role"] = "system",
                ["content"] = promptPackage.SystemPrompt
            });
        }

        messages.Add(new Dictionary<string, string>
        {
            ["role"] = "user",
            ["content"] = promptPackage.UserPrompt
        });

        var requestBody = JsonSerializer.Serialize(new
        {
            model = string.IsNullOrWhiteSpace(options.Model) ? "phi-3.5-mini-instruct" : options.Model,
            messages,
            temperature = options.Temperature,
            top_p = options.TopP,
            max_tokens = options.MaxTokens,
            stream = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/chat/completions")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        using var response = await Http.SendAsync(request).ConfigureAwait(false);
        var rawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var reply = ParseReply(rawResponse);
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new InvalidOperationException("Local llama.cpp returned an empty reply.");
        }

        return new LlamaCppDialogueReply(reply, options.Model, rawResponse, promptPackage);
    }

    private static string ParseReply(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message))
        {
            return string.Empty;
        }

        if (!message.TryGetProperty("content", out var content))
        {
            return string.Empty;
        }

        return Clean(content.GetString());
    }

    private static string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Replace('\n', ' ').Trim();
    }
}
