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

    // ─── Email notification settings ─────────────────────────────────
    public bool EmailEnabled { get; set; } =
        (Environment.GetEnvironmentVariable("EMAIL_ENABLED") ?? "false")
            .Equals("true", StringComparison.OrdinalIgnoreCase);

    public string SmtpHost { get; set; } = Environment.GetEnvironmentVariable("SMTP_HOST") ?? string.Empty;
    public int SmtpPort { get; set; } =
        int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 587;
    public string SmtpUser { get; set; } = Environment.GetEnvironmentVariable("SMTP_USER") ?? string.Empty;
    public string SmtpPassword { get; set; } = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? string.Empty;
    public bool SmtpUseSsl { get; set; } =
        !(Environment.GetEnvironmentVariable("SMTP_USE_SSL") ?? "true")
            .Equals("false", StringComparison.OrdinalIgnoreCase);

    public string EmailFrom { get; set; } =
        Environment.GetEnvironmentVariable("EMAIL_FROM")
        ?? Environment.GetEnvironmentVariable("SMTP_USER")
        ?? string.Empty;
    public string EmailFromName { get; set; } =
        Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? "K8S Monitor";

    /// <summary>
    /// Comma- or semicolon-separated list of recipient email addresses.
    /// Configured via the PR_NOTIFY_EMAILS environment variable.
    /// </summary>
    public string[] PrNotifyEmails { get; set; } =
        (Environment.GetEnvironmentVariable("PR_NOTIFY_EMAILS") ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public void Validate()
    {
        if (string.IsNullOrEmpty(OpenAIApiKey))
            throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
        
        if (string.IsNullOrEmpty(GitHubToken))
            throw new InvalidOperationException("GITHUB_TOKEN environment variable is required");

        if (EmailEnabled)
        {
            if (string.IsNullOrWhiteSpace(SmtpHost))
                throw new InvalidOperationException("SMTP_HOST is required when EMAIL_ENABLED=true");
            if (string.IsNullOrWhiteSpace(EmailFrom))
                throw new InvalidOperationException("EMAIL_FROM (or SMTP_USER) is required when EMAIL_ENABLED=true");
            if (PrNotifyEmails.Length == 0)
                throw new InvalidOperationException("PR_NOTIFY_EMAILS must contain at least one recipient when EMAIL_ENABLED=true");
        }
    }
}
