using OpenAI.Chat;
using System.ClientModel;

namespace K8SMonitor.Services;

public class OpenAIService
{
    private readonly string _apiKey;
    private readonly ChatClient _client;

    public OpenAIService(string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _client = new ChatClient("gpt-4o-mini", _apiKey);
    }

    public async Task<string> AnalyzePodErrorAsync(string podName, string namespaceName, string logs)
    {
        try
        {
            var systemPrompt = @"You are a Kubernetes expert assistant. 
Your task is to analyze pod logs and identify issues. 
For each error found, provide:
1. What the error is
2. Root cause analysis
3. Specific code or configuration fix needed

Format your response as a structured fix that can be applied to the codebase.";

            var userPrompt = $@"Analyze the following Kubernetes pod log and suggest fixes:

Pod: {podName}
Namespace: {namespaceName}

Logs:
{logs}

Please provide actionable fixes that can be implemented immediately.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var completion = await _client.CompleteChatAsync(messages);
            
            return completion.Value.Content[0].Text ?? "No analysis available";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to analyze pod error: {ex.Message}", ex);
        }
    }

    public async Task<string> GeneratePullRequestTitleAndDescriptionAsync(string podName, string errorSummary)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new UserChatMessage($@"Generate a GitHub pull request title and description for fixing the following Kubernetes issue:

Pod: {podName}
Error: {errorSummary}

Format your response as:
TITLE: [PR Title]
DESCRIPTION: [PR Description]")
            };

            var completion = await _client.CompleteChatAsync(messages);
            return completion.Value.Content[0].Text ?? "Auto-fix from K8SMonitor";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate PR info: {ex.Message}", ex);
        }
    }
}
