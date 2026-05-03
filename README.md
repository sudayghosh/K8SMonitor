# K8S Monitor - Kubernetes Error Detection & Auto-Fix with AI

A sophisticated Kubernetes monitoring system that automatically detects pod errors, analyzes them using OpenAI's GPT-4o Mini model, and creates GitHub pull requests with AI-suggested fixes.

## Features

✨ **Core Capabilities:**
- 📊 Monitor local Kind clusters (and any Kubernetes cluster)
- 🔍 Analyze pod logs for errors automatically
- 🤖 Use OpenAI GPT-4o Mini to analyze errors and suggest fixes
- 📝 Auto-generate pull requests on GitHub with solutions
- 🌿 Smart branch naming and conflict management
- 🔄 Multi-namespace support

## Prerequisites

### Requirements
- .NET 8.0 or later
- Kubernetes cluster (Kind, Docker Desktop, AKS, etc.)
- kubectl configured with access to your cluster
- OpenAI API key with GPT-4o Mini access
- GitHub personal access token with repo permissions

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/K8SMonitor.git
   cd K8SMonitor
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the project:**
   ```bash
   dotnet build
   ```

## Configuration

### Environment Variables

Set these environment variables before running:

```bash
# Required: OpenAI API Key
export OPENAI_API_KEY="sk-..."

# Required: GitHub Personal Access Token
export GITHUB_TOKEN="ghp_..."

# Required: GitHub repository owner
export GITHUB_OWNER="your-username"

# Required: GitHub repository name
export GITHUB_REPO="example-voting-app"

# Optional: GitHub base branch (default: main)
export GITHUB_BASE_BRANCH="main"

# Optional: Enable dry-run mode (no actual PRs created)
export DRY_RUN="false"
```

### For Windows PowerShell:
```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:GITHUB_TOKEN = "ghp_..."
$env:GITHUB_OWNER = "your-username"
$env:GITHUB_REPO = "example-voting-app"
$env:GITHUB_BASE_BRANCH = "main"
$env:DRY_RUN = "false"
```

## Usage

### Basic Usage

Run the monitor:
```bash
dotnet run
```

### Dry Run (No PRs Created)
```bash
export DRY_RUN="true"
dotnet run
```

### Output Example

```
╔═══════════════════════════════════════════════════════╗
║         K8S Monitor with AI Auto-Fix                 ║
╚═══════════════════════════════════════════════════════╝

📡 Connecting to Kubernetes cluster...
✓ Connected to cluster

📊 CLUSTER INFORMATION
═══════════════════════════════════════════════════════

Nodes: 1
  • docker-desktop - ✓ Ready

🔍 ANALYZING POD LOGS FOR ERRORS
═══════════════════════════════════════════════════════

Scanning namespace: default
  ⚠️  voting-app: 2 error(s) found
      • 2024-01-15T10:30:45.123Z ERROR: Database connection failed
      • 2024-01-15T10:30:46.456Z ERROR: Retrying connection...

🤖 ANALYZING ERRORS WITH OPENAI GPT-4o Mini
═══════════════════════════════════════════════════════

[voting-app] Sending error analysis to OpenAI...
✓ AI Analysis Complete:
───────────────────────────────────────────────────
  The application is failing to connect to the database service.
  This is likely due to a missing environment variable or incorrect...

📝 Creating Pull Request...
✓ Pull Request created: #42
```

## How It Works

### 1. **Cluster Monitoring**
- Connects to your Kubernetes cluster using kubeconfig
- Lists all nodes and their status
- Scans configured namespaces for pods

### 2. **Error Detection**
- Reads pod logs for each pod
- Searches for error patterns (error, exception, failed, timeout, etc.)
- Extracts error lines for analysis

### 3. **AI Analysis**
- Sends detected errors to OpenAI GPT-4o Mini
- Gets structured analysis including:
  - Root cause analysis
  - Specific fixes needed
  - Code/configuration changes

### 4. **Pull Request Creation**
- Generates PR title and description using AI
- Creates a feature branch: `k8s-fix-<pod-name>-<timestamp>`
- Commits fix recommendations
- Creates pull request on GitHub
- Includes error details and AI analysis

## Architecture

```
┌─────────────────────────────────────────┐
│     K8SMonitor (Main Orchestrator)      │
└─────────────────────────────────────────┘
           ↓            ↓            ↓
    ┌──────────────┬──────────────┬──────────────┐
    ↓             ↓              ↓              ↓
KubernetesLogAnalyzer | OpenAIService | GitHubPRService | Config
    │             │              │
    └─────┬───────┴──────┬───────┘
          │              │
    [Kind Cluster]   [OpenAI API]  [GitHub API]
```

## Services

### KubernetesLogAnalyzer
- `GetNodesAsync()` - List cluster nodes
- `GetPodsAsync()` - List pods in namespace
- `AnalyzePodLogsAsync()` - Analyze single pod logs
- `AnalyzeAllPodsInNamespaceAsync()` - Batch analyze all pods

### OpenAIService
- `AnalyzePodErrorAsync()` - Get AI error analysis
- `GeneratePullRequestTitleAndDescriptionAsync()` - Generate PR metadata

### GitHubPRService
- `CreatePullRequestAsync()` - Create PR with changes
- `GetRecentErrorBranchesAsync()` - List recent fix branches

## Configuration Class

The `K8SMonitorConfig` class provides:
- Namespace configuration
- Tail lines for log reading (default: 50)
- Check interval (default: 5 minutes)
- Auto-PR enablement
- Dry-run mode support

## Error Patterns

The system detects errors matching these patterns:
- `error`, `exception`, `failed`, `fatal`
- `panic`, `crash`, `timeout`, `deadlock`
- `connection refused`, `out of memory`
- `segmentation fault`, `null reference`

Add custom patterns by modifying the `_errorPatterns` array in `KubernetesLogAnalyzer.cs`.

## Branch Naming Convention

Auto-generated branches follow this format:
```
k8s-fix-<pod-name>-<unix-timestamp>
```

Example: `k8s-fix-voting-app-1705314645`

## Getting API Keys

### OpenAI API Key
1. Sign up at https://platform.openai.com
2. Go to API keys: https://platform.openai.com/api-keys
3. Create new secret key
4. Copy the key (starts with `sk-`)

### GitHub Token
1. Go to Settings → Developer settings → Personal access tokens
2. Click "Tokens (classic)" or "Fine-grained tokens"
3. Create new token with `repo` scope
4. Copy the token (starts with `ghp_`)

## Example Workflow

```bash
# 1. Set up environment
export OPENAI_API_KEY="sk-..."
export GITHUB_TOKEN="ghp_..."
export GITHUB_OWNER="octocat"
export GITHUB_REPO="Hello-World"

# 2. Run monitor
dotnet run

# 3. Check GitHub for new PRs
# Navigate to https://github.com/octocat/Hello-World/pulls
```

## Troubleshooting

### "OPENAI_API_KEY environment variable is required"
- Ensure the environment variable is set correctly
- Check with: `echo $OPENAI_API_KEY` (Linux/Mac) or `echo $env:OPENAI_API_KEY` (PowerShell)

### "Failed to connect to Kubernetes cluster"
- Verify kubeconfig: `kubectl config view`
- Check cluster access: `kubectl get nodes`

### "Failed to create pull request"
- Verify GitHub token has `repo` scope
- Check token isn't expired
- Verify repository name is correct

### No errors found in pods
- This is good! Your cluster is healthy
- Pod logs might be limited (use higher `tailLines` value)
- Ensure you're checking the right namespaces

## Advanced Configuration

### Custom Namespaces

Edit the code to monitor specific namespaces:
```csharp
config.Namespaces = new[] { "production", "staging", "monitoring" };
```

### Custom Error Patterns

Add new error patterns in `KubernetesLogAnalyzer.cs`:
```csharp
private readonly string[] _errorPatterns = new[]
{
    "error", "exception", "failed", "fatal",
    "YOUR_CUSTOM_PATTERN" // Add here
};
```

### Change Model

Modify `OpenAIService.cs` to use different model:
```csharp
_client = new ChatClient("gpt-4-turbo", _apiKey); // or another model
```

## API Reference

### Environment Variables
| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `OPENAI_API_KEY` | Yes | - | OpenAI API key |
| `GITHUB_TOKEN` | Yes | - | GitHub personal access token |
| `GITHUB_OWNER` | Yes | - | GitHub repository owner |
| `GITHUB_REPO` | Yes | - | GitHub repository name |
| `GITHUB_BASE_BRANCH` | No | main | Base branch for PRs |
| `DRY_RUN` | No | false | Enable dry-run mode |

## Performance Considerations

- **Log tail lines**: Default 50 lines. Increase for more context, may slow down analysis
- **Batch processing**: Analyzes all pods serially. For many pods, may take time
- **API costs**: Each error analysis calls OpenAI API (small cost per call)
- **PR creation rate**: GitHub may rate-limit PR creation if too many errors

## Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/enhancement`
3. Commit changes: `git commit -am 'Add new feature'`
4. Push to branch: `git push origin feature/enhancement`
5. Submit a pull request

## License

MIT License - see LICENSE file for details

## Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Check existing issues for solutions
- Review the troubleshooting section

## Roadmap

- [ ] Persistent error tracking and trending
- [ ] Scheduled monitoring (cron-like scheduling)
- [ ] Slack/email notifications
- [ ] Custom fix templates per error type
- [ ] PR review workflow integration
- [ ] Metrics export (Prometheus compatible)

---

**Built with** ❤️ **using .NET, Kubernetes Client, OpenAI API, and GitHub API**
