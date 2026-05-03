# K8S Monitor - Quick Start Guide

## Setup Instructions

### 1. Prerequisites Checklist

- [ ] .NET 8.0+ installed
- [ ] kubectl installed and configured
- [ ] Access to Kind/Kubernetes cluster
- [ ] OpenAI API key (from platform.openai.com)
- [ ] GitHub personal access token

### 2. Set Environment Variables

#### PowerShell (Windows):
```powershell
$env:OPENAI_API_KEY = "sk-YOUR_KEY_HERE"
$env:GITHUB_TOKEN = "ghp_YOUR_TOKEN_HERE"
$env:GITHUB_OWNER = "your-github-username"
$env:GITHUB_REPO = "example-voting-app"
$env:GITHUB_BASE_BRANCH = "main"
$env:DRY_RUN = "false"
```

#### Bash/Zsh (Linux/Mac):
```bash
export OPENAI_API_KEY="sk-YOUR_KEY_HERE"
export GITHUB_TOKEN="ghp_YOUR_TOKEN_HERE"
export GITHUB_OWNER="your-github-username"
export GITHUB_REPO="example-voting-app"
export GITHUB_BASE_BRANCH="main"
export DRY_RUN="false"
```

#### Windows Command Prompt:
```cmd
set OPENAI_API_KEY=sk-YOUR_KEY_HERE
set GITHUB_TOKEN=ghp_YOUR_TOKEN_HERE
set GITHUB_OWNER=your-github-username
set GITHUB_REPO=example-voting-app
set GITHUB_BASE_BRANCH=main
set DRY_RUN=false
```

### 3. Verify Kubernetes Access

```bash
# Check cluster access
kubectl get nodes

# List pods in default namespace
kubectl get pods -n default
```

### 4. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run the monitor
dotnet run
```

## Expected Output

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
✓ No errors found...
```

## Common Issues

### Issue: "OPENAI_API_KEY environment variable is required"

**Solution:** Ensure the environment variable is correctly set:

```powershell
# Check if set
$env:OPENAI_API_KEY

# If not set, set it
$env:OPENAI_API_KEY = "sk-..."
```

### Issue: "Failed to connect to Kubernetes cluster"

**Solution:** Verify kubeconfig:

```bash
# View kubeconfig path
kubectl config view

# Check cluster connectivity
kubectl cluster-info
```

### Issue: "Failed to create pull request"

**Solution:** Check GitHub token:

```bash
# Verify token is set correctly
echo $env:GITHUB_TOKEN  # PowerShell
echo $GITHUB_TOKEN       # Bash

# Token must have "repo" scope
# Create new at: https://github.com/settings/tokens
```

## Testing with Dry Run

To test without creating PRs:

```powershell
$env:DRY_RUN = "true"
dotnet run
```

## Next Steps

1. **Monitor in real-time**: Set up scheduled execution
2. **Customize error patterns**: Modify `KubernetesLogAnalyzer.cs`
3. **Adjust namespaces**: Update `config.Namespaces` in `Program.cs`
4. **Change model**: Use different OpenAI model in `OpenAIService.cs`

## Support Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [GitHub API Documentation](https://docs.github.com/en/rest)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
