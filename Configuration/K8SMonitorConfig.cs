namespace K8SMonitor.Configuration;

public class K8SMonitorConfig
{
    public string KubeConfigPath { get; set; } = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\.kube\\config");
    public string OpenAIApiKey { get; set; } = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
    public string GitHubToken { get; set; } = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty;
    public string GitHubOwner { get; set; } = Environment.GetEnvironmentVariable("GITHUB_OWNER") ?? "your-username";
    public string GitHubRepo { get; set; } = Environment.GetEnvironmentVariable("GITHUB_REPO") ?? "example-voting-app";
    public string GitHubBaseBranch { get; set; } = "main";
    public string[] Namespaces { get; set; } = new[] { "default"};
    public int TailLines { get; set; } = 50;
    public int CheckIntervalSeconds { get; set; } = 300; // 5 minutes
    public bool EnableAutoPR { get; set; } = true;
    public bool DryRun { get; set; } = false;

    public void Validate()
    {
        if (string.IsNullOrEmpty(OpenAIApiKey))
            throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
        
        if (string.IsNullOrEmpty(GitHubToken))
            throw new InvalidOperationException("GITHUB_TOKEN environment variable is required");
    }
}
