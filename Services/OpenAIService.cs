using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace K8SMonitor.Services;

public class CodeFix
{
    public string FilePath { get; set; } = string.Empty;
    public string OriginalCode { get; set; } = string.Empty;
    public string FixedCode { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

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

    public async Task<CodeFix> GenerateCodeFixAsync(string podName, string namespaceName, string errorLogs, string? deploymentYaml = null)
    {
        try
        {
            var systemPrompt = @"You are an expert C# and Kubernetes developer. Your task is to analyze pod errors and generate exact code fixes.
You MUST respond with JSON only (no other text). The JSON must have this exact structure:
{
  ""filePath"": ""src/Services/YourService.cs"",
  ""explanation"": ""Why this fix is needed"",
  ""originalCode"": ""The buggy code snippet"",
  ""fixedCode"": ""The corrected code snippet""
}

Focus on:
- Environment variable handling
- Configuration issues
- Connection/authentication problems
- Null reference exceptions
- Resource constraints
- Deployment misconfigurations";

            var userPrompt = $@"Fix the Kubernetes pod error:

Pod: {podName}
Namespace: {namespaceName}

Error Logs:
{errorLogs}

{(deploymentYaml != null ? $"Deployment YAML:\n{deploymentYaml}" : "")}

Provide the exact C# code fix needed. Return ONLY valid JSON with no markdown formatting.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var completion = await _client.CompleteChatAsync(messages);
            var responseText = completion.Value.Content[0].Text ?? "{}";

            // Parse JSON response
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(responseText, @"\{[\s\S]*\}");
            if (!jsonMatch.Success)
            {
                throw new InvalidOperationException("Failed to parse JSON response from OpenAI");
            }

            var jsonText = jsonMatch.Value;
            var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            return new CodeFix
            {
                FilePath = root.GetProperty("filePath").GetString() ?? "Unknown.cs",
                Explanation = root.GetProperty("explanation").GetString() ?? "",
                OriginalCode = root.GetProperty("originalCode").GetString() ?? "",
                FixedCode = root.GetProperty("fixedCode").GetString() ?? ""
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate code fix: {ex.Message}", ex);
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
