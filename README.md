# K8S Monitor - Kubernetes Error Detection & Auto-Fix with AI

A powerful Kubernetes monitoring application that automatically detects pod errors, analyzes them using OpenAI's GPT-4o Mini AI model, and creates pull requests on GitHub with suggested code fixes. This tool helps teams quickly identify and resolve issues in their Kubernetes clusters.

## 🎯 What This Application Does

**K8S Monitor** continuously watches your Kubernetes cluster for errors in pod logs. When it detects an error:

1. **Fetches pod logs** from your Kubernetes cluster
2. **Analyzes errors** using OpenAI's GPT-4o Mini model
3. **Generates code fixes** with explanations
4. **Creates pull requests** on GitHub with the suggested solutions
5. **Manages branches** automatically with smart naming

## ✨ Key Features

- **🔍 Automatic Error Detection** - Scans pod logs for common error patterns (exceptions, timeouts, crashes, etc.)
- **🤖 AI-Powered Analysis** - Uses OpenAI GPT-4o Mini to understand root causes and suggest specific code fixes
- **🌐 Multi-Cluster Support** - Works with Kind, Docker Desktop, AWS EKS, Azure AKS, and any Kubernetes cluster
- **📝 Auto PR Generation** - Automatically creates GitHub pull requests with detailed fix descriptions
- **🔄 Multi-Namespace Monitoring** - Monitor errors across multiple Kubernetes namespaces
- **🎛️ Dry-Run Mode** - Test the system without creating actual pull requests
- **⚙️ Flexible Configuration** - Easily configure cluster settings, namespaces, and check intervals

## 📋 Prerequisites

Before you get started, make sure you have the following:

### Required Software
- **.NET 8.0 or later** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com)
- **Kubernetes Cluster** - You can use:
  - **Kind** (Kubernetes in Docker) - Great for local development
  - **Docker Desktop** - Built-in Kubernetes support
  - **Minikube** - Single-node cluster
  - **Cloud managed services** - AWS EKS, Azure AKS, Google GKE
- **kubectl** - Command-line tool configured to access your cluster
  - Test with: `kubectl get nodes`

### Required Accounts & Keys
- **OpenAI API Key** - For AI error analysis
  - Sign up at [platform.openai.com](https://platform.openai.com)
  - Ensure you have access to GPT-4o Mini model
- **GitHub Personal Access Token** - For creating pull requests
  - Go to GitHub → Settings → Developer settings → Personal access tokens
  - Required scopes: `repo` (full control of private repositories)

## 📦 Installation

### Step 1: Clone the Repository
```bash
git clone https://github.com/your-username/K8SMonitor.git
cd K8SMonitor
```

### Step 2: Restore NuGet Dependencies
```bash
dotnet restore
```

### Step 3: Build the Project
```bash
dotnet build
```

### Step 4: Verify Installation
```bash
dotnet run --help
```

## ⚙️ Configuration

### Environment Variables

The application reads configuration from environment variables. Set these before running:

#### Required Variables
```bash
# Your OpenAI API key (used for AI error analysis)
export OPENAI_API_KEY="sk-..."

# Your GitHub personal access token (used to create PRs)
export GITHUB_TOKEN="ghp_..."

# GitHub repository owner (your username or organization name)
export GITHUB_OWNER="your-username"

# GitHub repository name (where PRs will be created)
export GITHUB_REPO="example-voting-app"
```

#### Optional Variables
```bash
# Base branch for creating pull requests (default: main)
export GITHUB_BASE_BRANCH="main"

# Enable dry-run mode - analyzes errors but doesn't create PRs (default: false)
export DRY_RUN="false"

# Kubernetes namespace to monitor (default: default)
# You can monitor multiple namespaces by modifying the code
export KUBE_NAMESPACE="default"
```

### Configuration Examples

#### For Linux/macOS:
```bash
export OPENAI_API_KEY="sk-1234567890abcdef"
export GITHUB_TOKEN="ghp_1234567890abcdef"
export GITHUB_OWNER="myusername"
export GITHUB_REPO="my-app"
export DRY_RUN="false"
```

#### For Windows PowerShell:
```powershell
$env:OPENAI_API_KEY = "sk-1234567890abcdef"
$env:GITHUB_TOKEN = "ghp_1234567890abcdef"
$env:GITHUB_OWNER = "myusername"
$env:GITHUB_REPO = "my-app"
$env:DRY_RUN = "false"
```

#### For Windows Command Prompt (cmd.exe):
```cmd
set OPENAI_API_KEY=sk-1234567890abcdef
set GITHUB_TOKEN=ghp_1234567890abcdef
set GITHUB_OWNER=myusername
set GITHUB_REPO=my-app
set DRY_RUN=false
```

### Using a .env File

Create a `.env` file in the project root directory:
```env
OPENAI_API_KEY=sk-1234567890abcdef
GITHUB_TOKEN=ghp_1234567890abcdef
GITHUB_OWNER=myusername
GITHUB_REPO=my-app
GITHUB_BASE_BRANCH=main
DRY_RUN=false
```

The application will automatically load this file when it starts.

## 🚀 Usage

### Running the Application

#### 1. Standard Mode (With PR Creation)
```bash
dotnet run
```
This will:
- Connect to your Kubernetes cluster
- Monitor all configured namespaces
- Detect errors in pod logs
- Create pull requests for each error found

#### 2. Dry-Run Mode (Without PR Creation)
```bash
export DRY_RUN="true"
dotnet run
```
Use this to test the error detection and AI analysis without creating actual pull requests.

#### 3. Run with Custom Configuration
```bash
export OPENAI_API_KEY="your-key"
export GITHUB_TOKEN="your-token"
export GITHUB_OWNER="owner"
export GITHUB_REPO="repo"
dotnet run
```

### Understanding the Output

When you run the application, you'll see output like this:

```
╔═══════════════════════════════════════════════════════╗
║         K8S Monitor with AI Auto-Fix                 ║
╚═══════════════════════════════════════════════════════╝

📡 Connecting to Kubernetes cluster...
✓ Connected to cluster

📊 CLUSTER INFORMATION
═══════════════════════════════════════════════════════
Nodes: 3
Namespaces: 2
Pods with issues: 5

🔍 Analyzing pod errors...
Pod: api-service-xyz123  | Status: CrashLoopBackOff
  Error: NullReferenceException in ConnectionPool.cs:45
  AI Analysis: Connection to database is null...
  
  ✓ PR Created: Fix connection pool null reference
  Branch: fix/null-reference-connectionpool

📈 Summary
Total issues found: 5
Pull requests created: 5
```

### Example: Error Detection Workflow

1. **Error Occurs** - A pod crashes in your cluster
2. **Detection** - K8S Monitor detects the error in pod logs
3. **Analysis** - OpenAI GPT-4o Mini analyzes the error and root cause
4. **Fix Generation** - AI suggests specific code changes
5. **PR Creation** - A pull request is created on GitHub with:
   - Title: `fix: [issue description]`
   - Description: Analysis and solution details
   - Code: Suggested fix ready to review
   - Branch: Auto-created feature branch

## 🐳 Docker Usage

### Build Docker Image
```bash
docker build -t k8s-monitor:latest .
```

### Run in Docker
```bash
docker run \
  -e OPENAI_API_KEY="sk-..." \
  -e GITHUB_TOKEN="ghp_..." \
  -e GITHUB_OWNER="your-username" \
  -e GITHUB_REPO="my-app" \
  -v ~/.kube/config:/root/.kube/config:ro \
  k8s-monitor:latest
```

### Push to Container Registry
```bash
# GitHub Container Registry
docker tag k8s-monitor:latest ghcr.io/your-username/k8s-monitor:latest
docker push ghcr.io/your-username/k8s-monitor:latest
```

## 🔄 How It Works

The application follows this workflow:

### Step 1: Cluster Connection
- Connects to your Kubernetes cluster using kubeconfig file
- Lists all cluster nodes and their status (Ready, NotReady, etc.)
- Retrieves information about all namespaces being monitored

### Step 2: Pod Log Retrieval
- Scans all pods in the configured namespaces
- Reads the last 50 lines of logs from each pod (configurable)
- Gathers status information (Running, CrashLoopBackOff, Pending, etc.)

### Step 3: Error Detection
- Searches pod logs for error patterns including:
  - `error`, `exception`, `failed`, `fatal`
  - `panic`, `crash`, `timeout`, `deadlock`
  - `connection refused`, `out of memory`
  - And more...
- Extracts error lines and context for analysis
- Groups errors by pod for batch processing

### Step 4: AI Analysis
- Sends detected errors to OpenAI GPT-4o Mini API
- AI analyzes each error and provides:
  - **Root Cause** - What caused the error
  - **Analysis** - Why this is a problem
  - **Solution** - Specific code or configuration fix
  - **Implementation** - Steps to fix the issue

### Step 5: Pull Request Creation
- Generates PR title based on AI analysis
- Creates a new feature branch with pattern: `k8s-fix-<pod-name>-<timestamp>`
- Commits the suggested fix to the branch
- Creates pull request on GitHub with:
  - Detailed description of the error
  - Root cause analysis from AI
  - Suggested code changes
  - Instructions for review and merge
- Team can review, modify, and merge the PR

### Step 6: Notification
- Logs successful PR creation
- Reports summary of all issues found and fixes created
- In dry-run mode, skips actual PR creation but shows what would happen

## 🏗️ Project Architecture

### Application Flow Diagram
```
┌─────────────────────────────────────────────────────┐
│           Program.cs (Main Entry Point)             │
│  - Loads configuration from environment variables   │
│  - Orchestrates the entire monitoring workflow      │
└─────────────────────────┬───────────────────────────┘
                          ↓
        ┌─────────────────────────────────────┐
        │  KubernetesLogAnalyzer              │
        │  - Connects to K8s cluster          │
        │  - Retrieves pod logs               │
        │  - Detects error patterns           │
        └──────────┬──────────────────────────┘
                   ↓
        ┌──────────────────────────────────────┐
        │  OpenAIService                       │
        │  - Sends errors to OpenAI API        │
        │  - Gets AI analysis and fixes        │
        │  - Generates PR descriptions         │
        └──────────┬───────────────────────────┘
                   ↓
        ┌──────────────────────────────────────┐
        │  GitHubPRService                     │
        │  - Creates feature branches          │
        │  - Commits fixes to branches         │
        │  - Creates pull requests on GitHub   │
        └──────────────────────────────────────┘
```

### Folder Structure
```
K8SMonitor/
├── Program.cs                 # Entry point - orchestrates workflow
├── Configuration/
│   └── K8SMonitorConfig.cs    # Configuration settings and validation
├── Services/
│   ├── KubernetesLogAnalyzer.cs  # Kubernetes cluster operations
│   ├── OpenAIService.cs          # AI error analysis service
│   └── GitHubPRService.cs        # GitHub PR creation service
├── K8SMonitor.csproj         # .NET project file with dependencies
├── Dockerfile                # Container image definition
├── k8s-deployment.yaml       # Kubernetes deployment manifest
└── README.md                 # This file
```

## 🔧 Service Components

### KubernetesLogAnalyzer.cs
Handles all interactions with your Kubernetes cluster:
- **GetNodesAsync()** - Retrieves all cluster nodes and their status
- **GetPodsAsync()** - Lists all pods in a specific namespace
- **AnalyzePodLogsAsync()** - Reads and analyzes logs from a single pod
- **AnalyzeAllPodsInNamespaceAsync()** - Batch processes all pods in namespace

Error detection patterns:
```
error, exception, failed, fatal, panic, crash, 
timeout, deadlock, connection refused, out of memory,
segmentation fault, null reference, undefined
```

### OpenAIService.cs
Handles AI-powered error analysis:
- **AnalyzePodErrorAsync()** - Sends pod errors to OpenAI and gets analysis
- **GeneratePullRequestTitleAndDescriptionAsync()** - Creates PR title and description
- Uses OpenAI GPT-4o Mini model for fast, cost-effective analysis
- Returns structured analysis with root cause and solutions

### GitHubPRService.cs
Handles all GitHub operations:
- **GetFileContentAsync()** - Retrieves file content from GitHub
- **CreatePullRequestWithCodeFixAsync()** - Creates new branch and PR with code fix
- **UpdateFileAndCreatePullRequestAsync()** - Updates files and creates PR
- Manages branch naming: `k8s-fix-<pod-name>-<timestamp>`
- Automatically handles branch creation and PR submission

### K8SMonitorConfig.cs
Configuration management:
- Reads all settings from environment variables
- Validates required configurations at startup
- Provides defaults for optional settings
- Contains properties:
  - `OpenAIApiKey` - API key for error analysis
  - `GitHubToken` - Token for PR creation
  - `GitHubOwner` - Repository owner
  - `GitHubRepo` - Repository name
  - `GitHubBaseBranch` - Base branch for PRs (default: main)
  - `Namespaces` - Kubernetes namespaces to monitor
  - `TailLines` - Number of log lines to read (default: 50)
  - `CheckIntervalSeconds` - Monitoring interval (default: 300)
  - `DryRun` - Test mode without creating actual PRs

## 🌿 Branch Naming Convention

Auto-generated branches follow this pattern:
```
k8s-fix-<pod-name>-<unix-timestamp>
```

**Examples:**
- `k8s-fix-voting-app-1705314645` - Fix for voting-app pod
- `k8s-fix-api-service-1705314700` - Fix for api-service pod
- `k8s-fix-database-1705314755` - Fix for database pod

**Why timestamps?**
- Prevents branch name collisions
- Creates unique branches for each error detection
- Allows tracking when the error was detected

## 📚 Getting API Keys

### OpenAI API Key

1. Visit https://platform.openai.com/signup
2. Sign up for OpenAI account
3. Navigate to https://platform.openai.com/api-keys
4. Click "Create new secret key"
5. Copy the key (starts with `sk-`)
6. **Important:** Save it securely - you won't be able to see it again
7. Store in your environment variable: `OPENAI_API_KEY`

### GitHub Personal Access Token

1. Go to GitHub.com and log in
2. Click your profile icon (top right) → Settings
3. In left sidebar: Developer settings → Personal access tokens
4. Choose "Tokens (classic)" for broader access
5. Click "Generate new token"
6. Set these permissions:
   - ✅ `repo` - Full control of private repositories
   - ✅ `gist` (optional) - Create gists
7. Choose expiration (or "No expiration" for personal use)
8. Click "Generate token"
9. Copy the token (starts with `ghp_`)
10. **Important:** Save it securely - you won't be able to see it again
11. Store in your environment variable: `GITHUB_TOKEN`

## 🚀 Quick Start Example

```bash
# Step 1: Set environment variables
export OPENAI_API_KEY="sk-1234567890abcdef"
export GITHUB_TOKEN="ghp_1234567890abcdef"
export GITHUB_OWNER="my-username"
export GITHUB_REPO="my-app"

# Step 2: Build the application
dotnet build

# Step 3: Test with dry-run mode first (no PRs created)
export DRY_RUN="true"
dotnet run

# Step 4: If everything looks good, run for real
export DRY_RUN="false"
dotnet run

# Step 5: Check GitHub for new pull requests
# Navigate to https://github.com/my-username/my-app/pulls
```

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

## 🆘 Troubleshooting

### Configuration Errors

#### "OPENAI_API_KEY environment variable is required"
**Cause:** The OpenAI API key environment variable is not set.

**Solutions:**
```bash
# Check if the variable is set
echo $OPENAI_API_KEY  # Linux/macOS
echo $env:OPENAI_API_KEY  # PowerShell

# Set it if missing
export OPENAI_API_KEY="sk-your-key-here"  # Linux/macOS
$env:OPENAI_API_KEY = "sk-your-key-here"  # PowerShell

# Or use a .env file
echo "OPENAI_API_KEY=sk-your-key-here" > .env
```

#### "GITHUB_TOKEN environment variable is required"
**Cause:** The GitHub token environment variable is not set.

**Solutions:**
- Create a new GitHub token (see Getting API Keys section)
- Set the environment variable before running:
  ```bash
  export GITHUB_TOKEN="ghp_your-token-here"
  ```

### Kubernetes Connection Issues

#### "Failed to connect to Kubernetes cluster"
**Cause:** Kubeconfig not found or cluster is unreachable.

**Solutions:**
```bash
# Verify kubeconfig exists
ls ~/.kube/config  # Linux/macOS
dir %USERPROFILE%\.kube\config  # Windows

# Check cluster access
kubectl cluster-info
kubectl get nodes

# If using Docker Desktop, ensure Kubernetes is enabled
# Docker Desktop → Settings → Kubernetes → Enable Kubernetes
```

#### "Connection refused" or "Unable to reach server"
**Cause:** Kubernetes cluster is not running or kubeconfig points to wrong cluster.

**Solutions:**
```bash
# View all configured clusters
kubectl config view

# Switch to correct cluster
kubectl config use-context docker-desktop  # or your cluster name

# Test connection
kubectl get nodes
```

### GitHub Issues

#### "Failed to create pull request" or "404 Repository not found"
**Cause:** Repository name is wrong, token doesn't have permissions, or repository doesn't exist.

**Solutions:**
- Verify repository exists and you have access
- Check environment variables:
  ```bash
  echo $GITHUB_OWNER  # Should be username or organization
  echo $GITHUB_REPO   # Should be exact repository name
  ```
- Create a new token with full `repo` scope
- Verify the token is not expired

#### "403 Forbidden" or "Insufficient permissions"
**Cause:** GitHub token doesn't have required permissions.

**Solutions:**
- Create a new token with `repo` scope enabled
- Check that token hasn't expired
- Verify token has write access to the repository

### API & Cost Issues

#### "Rate limit exceeded" from OpenAI
**Cause:** Too many API calls hitting OpenAI's rate limits.

**Solutions:**
- Wait before running again (limits reset periodically)
- Check your OpenAI usage at https://platform.openai.com/usage
- Consider using batch mode for large clusters

#### "Unexpected AI response" or "JSON parse error"
**Cause:** OpenAI API format changed or unexpected response.

**Solutions:**
- Check that GPT-4o Mini model is available in your account
- Verify OpenAI API key is valid
- Ensure OpenAI account has credits
- Try running with `DRY_RUN=true` first

### No Errors Found

#### "No errors detected in any pods"
**This is actually good!** It means your cluster is healthy.

**If you expect errors:**
- Increase `TailLines` in config to read more log history
- Check if pods are actually running:
  ```bash
  kubectl get pods
  kubectl logs <pod-name>
  ```
- Verify you're monitoring the right namespaces:
  ```bash
  kubectl get namespaces
  kubectl get pods -n <namespace-name>
  ```

### Performance Issues

#### "Application runs very slowly"
**Cause:** Too many pods, reading too many logs, or network latency.

**Solutions:**
- Reduce `TailLines` (default: 50)
- Monitor specific namespaces instead of all
- Run in dry-run mode for testing
- Check network connectivity to cluster and APIs

#### "Out of memory" error
**Cause:** Processing too many pod logs at once.

**Solutions:**
- Reduce the number of namespaces monitored
- Decrease `TailLines` value
- Run on a machine with more memory

## ⚡ Advanced Configuration

### Monitoring Multiple Namespaces

Edit `Program.cs` to monitor specific namespaces:

```csharp
var config = new K8SMonitorConfig
{
    // ... other configuration ...
    Namespaces = new[] { "production", "staging", "monitoring" }
};
```

### Adding Custom Error Patterns

Edit `Services/KubernetesLogAnalyzer.cs`:

```csharp
private readonly string[] _errorPatterns = new[]
{
    "error", "exception", "failed", "fatal",
    "panic", "crash", "timeout", "deadlock",
    "connection refused", "out of memory",
    "segmentation fault", "null reference",
    "YOUR_CUSTOM_ERROR",  // Add custom patterns
    "ANOTHER_ERROR_TYPE"
};
```

### Changing AI Model

Edit `Services/OpenAIService.cs`:

```csharp
public OpenAIService(string apiKey)
{
    _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    _client = new ChatClient("gpt-4", _apiKey);  // Change from gpt-4o-mini to gpt-4
}
```

Available models:
- `gpt-4o-mini` - Fast, cost-effective (default)
- `gpt-4` - More powerful analysis
- `gpt-3.5-turbo` - Older but cheaper

### Adjusting Log Tail Lines

Change how many log lines are read from each pod:

```csharp
var config = new K8SMonitorConfig
{
    // ... other configuration ...
    TailLines = 100  // Read more lines (default: 50)
};
```

### Modifying Check Interval

Change how frequently the monitor checks for errors:

```csharp
var config = new K8SMonitorConfig
{
    // ... other configuration ...
    CheckIntervalSeconds = 600  // Check every 10 minutes (default: 300)
};
```

### Disabling Auto-PR Creation

Test without creating pull requests:

```csharp
var config = new K8SMonitorConfig
{
    // ... other configuration ...
    EnableAutoPR = false  // Analyze but don't create PRs
};
```

## 📖 API Reference

### Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `OPENAI_API_KEY` | ✅ Yes | - | OpenAI API secret key (starts with `sk-`) |
| `GITHUB_TOKEN` | ✅ Yes | - | GitHub personal access token (starts with `ghp_`) |
| `GITHUB_OWNER` | ✅ Yes | - | GitHub repository owner or organization name |
| `GITHUB_REPO` | ✅ Yes | - | GitHub repository name (exact match required) |
| `GITHUB_BASE_BRANCH` | No | `main` | Base branch for creating pull requests |
| `DRY_RUN` | No | `false` | Set to `true` to analyze without creating PRs |
| `KUBE_NAMESPACE` | No | `default` | Kubernetes namespace to monitor |

### Configuration Properties

The `K8SMonitorConfig` class contains:

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `KubeConfigPath` | string | `~/.kube/config` | Path to kubeconfig file |
| `OpenAIApiKey` | string | - | OpenAI API key from environment |
| `GitHubToken` | string | - | GitHub token from environment |
| `GitHubOwner` | string | `sudayghosh` | Repository owner |
| `GitHubRepo` | string | `example-voting-app` | Repository name |
| `GitHubBaseBranch` | string | `main` | Base branch for PRs |
| `Namespaces` | string[] | `["default"]` | Namespaces to monitor |
| `TailLines` | int | `50` | Number of log lines to read |
| `CheckIntervalSeconds` | int | `300` | Seconds between checks |
| `EnableAutoPR` | bool | `true` | Enable PR creation |
| `DryRun` | bool | `false` | Test mode without PRs |

## ⚡ Performance Considerations

### Log Processing
- **Default:** Reads last 50 lines per pod
- **Impact:** Increase `TailLines` for more context, but may slow analysis
- **Recommendation:** Start with 50, adjust based on your needs

### Batch Processing
- **Current:** Analyzes all pods sequentially
- **Impact:** Large clusters may take several minutes
- **Optimization:** Monitor specific namespaces instead of all

### API Costs
- **OpenAI:** Each error analysis costs ~$0.001 USD (very low with GPT-4o Mini)
- **GitHub:** Pull request creation is free
- **Estimated:** $0.10-1.00 USD per 1000 error analyses

### Rate Limiting
- **OpenAI:** Limits vary by plan (typically 3,500 RPM for free tier)
- **GitHub:** 5,000 requests/hour per user
- **Solution:** Implement longer `CheckIntervalSeconds` if hitting limits

### Network Performance
- **Cluster latency:** Reading logs depends on cluster network speed
- **API latency:** OpenAI and GitHub API calls add 1-3 seconds each
- **Optimization:** Run locally or in same cloud region as cluster

## 🤝 Contributing

We welcome contributions! Here's how to help:

### Development Workflow

1. **Fork the repository**
   ```bash
   # On GitHub, click "Fork" button
   git clone https://github.com/YOUR-USERNAME/K8SMonitor.git
   cd K8SMonitor
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Keep commits small and focused
   - Write clear commit messages
   - Test your changes

4. **Commit your work**
   ```bash
   git commit -am 'Add: Your feature description'
   ```

5. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Submit a Pull Request**
   - On GitHub, click "Compare & pull request"
   - Provide a clear description of changes
   - Link any related issues

### Development Guidelines

- **Code style:** Follow C# conventions
- **Naming:** Use clear, descriptive names
- **Comments:** Document complex logic
- **Testing:** Test locally before submitting PR
- **Documentation:** Update README if adding features

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

This means:
- ✅ You can use this in commercial projects
- ✅ You can modify and distribute the code
- ✅ You must include the license with distributions
- ❌ No warranty or liability

## 💬 Support & Questions

### Getting Help

1. **Check the FAQ** - Most common issues are documented in Troubleshooting
2. **Search existing issues** - Your question might already be answered
3. **Read the code** - Comments explain complex sections
4. **Check logs** - Run with `-v` for verbose output (if supported)

### Reporting Issues

1. **Describe the problem** - What were you trying to do?
2. **Include error message** - Copy the full error text
3. **Provide environment** - OS, .NET version, K8s version
4. **Share config** - Environment variables (without secrets!)
5. **Steps to reproduce** - Exact commands to replicate the issue

### Feature Requests

1. **Describe the feature** - What should it do?
2. **Explain the use case** - Why do you need it?
3. **Provide examples** - How would you use it?

## 🚀 Roadmap

Planned features and improvements:

### Short Term (Next Release)
- [ ] **Persistent Error Tracking** - Track errors over time with historical data
- [ ] **Error Trending Dashboard** - Visualize error patterns and frequency
- [ ] **Slack Integration** - Send notifications to Slack channels
- [ ] **Custom Fix Templates** - Create templates for common error types

### Medium Term (Future Releases)
- [ ] **Scheduled Monitoring** - Cron-like scheduling for checks
- [ ] **Email Notifications** - Email alerts for critical errors
- [ ] **PR Review Workflow** - Automatic approval of low-risk PRs
- [ ] **Multiple AI Models** - Support for other AI providers (Claude, LLaMA, etc.)
- [ ] **Metrics Export** - Prometheus-compatible metrics

### Long Term (Future Versions)
- [ ] **Web Dashboard** - Real-time monitoring UI
- [ ] **Multi-Cluster Support** - Monitor multiple clusters simultaneously
- [ ] **Advanced Analytics** - ML-powered error prediction
- [ ] **ChatOps Integration** - Control via chat commands
- [ ] **API Endpoints** - REST API for integration

## 📊 Project Statistics

- **Language:** C# (.NET 8.0)
- **License:** MIT
- **Main Dependencies:**
  - KubernetesClient (K8s API)
  - OpenAI (AI integration)
  - Octokit (GitHub API)
  - DotNetEnv (.env support)

---

## 🙏 Acknowledgments

- Built with ❤️ for the Kubernetes community
- Thanks to all contributors and supporters
- Powered by .NET, OpenAI, and GitHub APIs
