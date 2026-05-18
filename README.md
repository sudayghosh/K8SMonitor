# K8S Monitor - Kubernetes Error Detection & Auto-Fix with AI

A .NET 8 console application that scans a Kubernetes cluster for pod errors, asks **OpenAI GPT-4o Mini** to analyze the failure and generate a concrete code fix, then opens a **unified GitHub Pull Request** containing the patch — automatically.

> **Status:** Single-run console job. For continuous monitoring it is intended to be deployed as a Kubernetes `CronJob` (manifest included) or run via the included GitHub Actions workflow.

---

## Table of Contents

- [What it does](#what-it-does)
- [How it works](#how-it-works)
- [Project structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Build & run](#build--run)
- [Docker](#docker)
- [Kubernetes deployment (CronJob)](#kubernetes-deployment-cronjob)
- [GitHub Actions CI/CD](#github-actions-cicd)
- [Service reference](#service-reference)
- [Customization](#customization)
- [Troubleshooting](#troubleshooting)
- [Roadmap](#roadmap)
- [License](#license)

---

## What it does

On each run, K8S Monitor:

1. **Connects** to the cluster pointed at by your kubeconfig (`%USERPROFILE%\.kube\config` on Windows, `~/.kube/config` elsewhere).
2. **Enumerates nodes** and reports `Ready` / `NotReady` status.
3. **Scans pods** in the configured namespaces. By default it processes only pods whose names contain `worker` (see [Customization](#customization)).
4. **Reads the tail** of each pod's logs (default `50` lines) and matches against an error keyword list.
5. **Extracts file references** from .NET stack-trace lines (`at … in /source/Program.cs:line 24`) using regex.
6. **Asks OpenAI** to summarize root cause and generate a structured `CodeFix` (JSON with `filePath`, `originalCode`, `fixedCode`, `explanation`).
7. **Resolves the file** in the target GitHub repository — first via stack trace, then by searching the repo tree by filename, with an AI fallback (`IdentifyBuggyFileAsync`).
8. **Opens one PR per affected pod** containing fixes for every affected file on a branch named `k8s-fix-<pod-name>-<unix-timestamp>`.
9. Supports a **dry-run mode** that performs analysis without modifying the target repository.

---

## How it works

```
┌──────────────────────────────────────────────────────────────┐
│                       Program.cs (entry)                     │
│  Loads .env → builds K8SMonitorConfig → validates → runs     │
└─────────────────────────────┬────────────────────────────────┘
                              ↓
        ┌─────────────────────────────────────────────┐
        │  KubernetesLogAnalyzer                      │
        │  • GetNodesAsync                            │
        │  • GetPodsAsync                             │
        │  • AnalyzePodLogsAsync                      │
        │  • AnalyzeAllPodsInNamespaceAsync           │
        │  • ExtractAllFilePathsFromStackTrace (regex)│
        └──────────────────────┬──────────────────────┘
                               ↓
        ┌─────────────────────────────────────────────┐
        │  OpenAIService  (gpt-4o-mini)               │
        │  • AnalyzePodErrorAsync (root cause)        │
        │  • IdentifyBuggyFileAsync (fallback)        │
        │  • GenerateCodeFixAsync (JSON CodeFix)      │
        │  • GeneratePullRequestTitleAndDescription   │
        └──────────────────────┬──────────────────────┘
                               ↓
        ┌─────────────────────────────────────────────┐
        │  GitHubPRService  (Octokit)                 │
        │  • GetFileContentAsync                      │
        │  • SearchFileByNameAsync (recursive tree)   │
        │  • CreatePullRequestWithMultipleCodeFixes   │
        │  • CreatePullRequestWithCodeFixAsync        │
        └─────────────────────────────────────────────┘
```

The orchestration in `Program.cs` is:

`scan pods → detect errors → for each erroring pod → resolve files → generate fixes → commit all to one branch → open one PR`.

---

## Project structure

```
K8SMonitor/
├── Program.cs                          # Entry point and orchestration
├── Employee.cs                         # Sample/leftover SQL helper (not used by the monitor)
├── Configuration/
│   └── K8SMonitorConfig.cs             # Env-driven config + validation
├── Services/
│   ├── KubernetesLogAnalyzer.cs        # K8s API access, log + stack-trace parsing
│   ├── OpenAIService.cs                # GPT-4o-mini calls, CodeFix DTO
│   └── GitHubPRService.cs              # Octokit branch/file/PR operations
├── K8SMonitor.csproj                   # .NET 8, package references
├── K8SMonitor.sln
├── Dockerfile                          # Multi-stage build, runtime image
├── k8s-deployment.yaml                 # ConfigMap, Secret, CronJob, RBAC, Namespace
├── .github/workflows/k8s-monitor.yml   # CI build + image push + optional run
├── QUICKSTART.md
├── DEPLOYMENT.md
└── README.md                           # This file
```

NuGet packages (`K8SMonitor.csproj`):

| Package | Version | Purpose |
|---|---|---|
| `KubernetesClient` | 19.0.2 | Talk to the cluster API |
| `OpenAI` | 2.0.0 | GPT-4o-mini chat completions |
| `Octokit` | 5.0.0 | GitHub REST API client |
| `DotNetEnv` | 3.2.0 | Load `.env` at startup |
| `System.Data.SqlClient` | 4.9.1 | Used by `Employee.cs` sample only |

---

## Prerequisites

- **.NET SDK 8.0+** — <https://dotnet.microsoft.com>
- **kubectl** with a working context — verify with `kubectl get nodes`
- A reachable **Kubernetes cluster** (Docker Desktop, Kind, Minikube, EKS, AKS, GKE, …)
- **OpenAI API key** with access to `gpt-4o-mini` — <https://platform.openai.com/api-keys>
- **GitHub Personal Access Token (classic)** with the `repo` scope — <https://github.com/settings/tokens>

---

## Configuration

All configuration is read from environment variables (or a `.env` file in the working directory, which is auto-loaded by `DotNetEnv`).

### Required

| Variable | Description |
|---|---|
| `OPENAI_API_KEY` | OpenAI secret key (starts with `sk-`) |
| `GITHUB_TOKEN`   | GitHub PAT with `repo` scope (starts with `ghp_`) |

### Optional

| Variable | Default (from `Program.cs`) | Description |
|---|---|---|
| `GITHUB_OWNER`       | `sudayghosh`         | Repo owner / org for PRs |
| `GITHUB_REPO`        | `example-voting-app` | Repo to open PRs against |
| `GITHUB_BASE_BRANCH` | `main`               | Base branch for PRs |
| `DRY_RUN`            | `false`              | `true` runs analysis but skips PR creation |

> Note: `K8SMonitorConfig` exposes additional knobs (`Namespaces`, `TailLines`, `CheckIntervalSeconds`, `EnableAutoPR`, `KubeConfigPath`) but they are not currently wired to env vars — adjust them in code as shown in [Customization](#customization).

### Example `.env`

```env
OPENAI_API_KEY=sk-xxxxxxxxxxxxxxxxxxxxxxxx
GITHUB_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxxxxxx
GITHUB_OWNER=my-org
GITHUB_REPO=my-app
GITHUB_BASE_BRANCH=main
DRY_RUN=false
```

The project marks `.env` as `CopyToOutputDirectory=Always`, so it is also picked up when running the built binary.

### Setting variables manually

PowerShell (Windows):

```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:GITHUB_TOKEN   = "ghp_..."
$env:GITHUB_OWNER   = "my-org"
$env:GITHUB_REPO    = "my-app"
$env:DRY_RUN        = "false"
```

Bash / Zsh:

```bash
export OPENAI_API_KEY="sk-..."
export GITHUB_TOKEN="ghp_..."
export GITHUB_OWNER="my-org"
export GITHUB_REPO="my-app"
export DRY_RUN="false"
```

cmd.exe:

```cmd
set OPENAI_API_KEY=sk-...
set GITHUB_TOKEN=ghp_...
set GITHUB_OWNER=my-org
set GITHUB_REPO=my-app
set DRY_RUN=false
```

---

## Build & run

```bash
git clone https://github.com/<owner>/K8SMonitor.git
cd K8SMonitor

dotnet restore
dotnet build

# Recommended for the first run
$env:DRY_RUN = "true"   # PowerShell
dotnet run

# Once the output looks correct
$env:DRY_RUN = "false"
dotnet run
```

> The app calls `Console.ReadLine()` at the end of execution, so it stays open after the run when launched from a terminal. Send EOF / press Enter to exit. Remove this line if you want the process to terminate immediately (useful for cron / CI scenarios).

### Sample output

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
  ⚠️  worker-7f8c9d-abcde: 4 error(s) found

🤖 ANALYZING ERRORS WITH OPENAI GPT-4o Mini
═══════════════════════════════════════════════════════

[worker-7f8c9d-abcde] Sending error analysis to OpenAI...
✓ AI Analysis Complete

🔍 Identifying buggy files...
  Found 2 file(s) in stack trace:
    • /source/Worker/Program.cs (line: 42)
      ✓ Resolved to: src/Worker/Program.cs

📥 Fetching file context from GitHub: src/Worker/Program.cs
⚙️ Generating AI code fix with file context...
✓ Code fix generated for: src/Worker/Program.cs

🔀 Generated 1 fix(es) for 1 file(s)

📝 Creating unified PR with all fixes...
✓ Created branch: k8s-fix-worker-7f8c9d-abcde-1716020055
✓ Fixed file: src/Worker/Program.cs
✓ Created Pull Request #42 with 1 file fix(es): https://github.com/.../pull/42

📊 Summary: 1 pull request(s) created
✓ K8S Monitor completed successfully
```

---

## Docker

Build:

```bash
docker build -t k8s-monitor:latest .
```

Run (mount your kubeconfig read-only):

```bash
docker run --rm \
  -e OPENAI_API_KEY="sk-..." \
  -e GITHUB_TOKEN="ghp_..." \
  -e GITHUB_OWNER="my-org" \
  -e GITHUB_REPO="my-app" \
  -e DRY_RUN="false" \
  -v $HOME/.kube/config:/root/.kube/config:ro \
  k8s-monitor:latest
```

Push to GHCR:

```bash
docker tag k8s-monitor:latest ghcr.io/<owner>/k8s-monitor:latest
docker push ghcr.io/<owner>/k8s-monitor:latest
```

The Dockerfile uses a `dotnet/sdk:8.0` build stage and `dotnet/runtime:8.0` runtime stage, installs `curl`, sets `DRY_RUN=false`, and exposes a basic `HEALTHCHECK`.

---

## Kubernetes deployment (CronJob)

`k8s-deployment.yaml` provisions:

- A `monitoring` namespace
- A `ConfigMap` (`k8s-monitor-config`) with `TAIL_LINES`, `CHECK_INTERVAL_SECONDS`, `NAMESPACES`
- A `Secret` (`k8s-monitor-secrets`) with `OPENAI_API_KEY`, `GITHUB_TOKEN`, `GITHUB_OWNER`, `GITHUB_REPO`
- A `ServiceAccount` + `ClusterRole` + `ClusterRoleBinding` granting read access to nodes, pods, pod logs, and events
- A `CronJob` (`k8s-monitor`) that runs every 5 minutes (`*/5 * * * *`) with resource requests/limits and a non-privileged security context

Deploy:

```bash
# Edit secrets and image reference first
kubectl apply -f k8s-deployment.yaml

kubectl get cronjobs -n monitoring
kubectl describe cronjob k8s-monitor -n monitoring

# Trigger an ad-hoc run
kubectl create job --from=cronjob/k8s-monitor test-run -n monitoring
kubectl logs -n monitoring job/test-run -f
```

See `DEPLOYMENT.md` for the full guide.

---

## GitHub Actions CI/CD

`.github/workflows/k8s-monitor.yml`:

- Triggered on push to `main` / `example-voting-app`, on PRs to `main`, and via `workflow_dispatch`
- Restores, builds, tests, and publishes the .NET project
- Logs into `ghcr.io` and pushes a multi-tag Docker image (branch, semver, sha) on non-PR events
- Has an optional `deploy-k8s-monitor` job that runs `dotnet run` against the cluster on pushes to the `example-voting-app` branch, using `OPENAI_API_KEY` and `GITHUB_TOKEN` repository secrets

---

## Service reference

### `K8SMonitorConfig` (`Configuration/K8SMonitorConfig.cs`)

| Property | Type | Default | Notes |
|---|---|---|---|
| `KubeConfigPath` | `string` | `%USERPROFILE%\.kube\config` | Not currently read by `Program.cs` (uses `BuildConfigFromConfigFile()` default) |
| `OpenAIApiKey` | `string` | `OPENAI_API_KEY` env | Required |
| `GitHubToken` | `string` | `GITHUB_TOKEN` env | Required |
| `GitHubOwner` | `string` | `your-username` / `sudayghosh` | Override via env |
| `GitHubRepo` | `string` | `example-voting-app` | Override via env |
| `GitHubBaseBranch` | `string` | `main` | |
| `Namespaces` | `string[]` | `["default"]` | Edit in code to monitor others |
| `TailLines` | `int` | `50` | Log lines per pod |
| `CheckIntervalSeconds` | `int` | `300` | Reserved for future polling loop |
| `EnableAutoPR` | `bool` | `true` | Set `false` to suppress PR creation |
| `DryRun` | `bool` | `false` | `DRY_RUN=true` to skip PRs |

`Validate()` throws if `OPENAI_API_KEY` or `GITHUB_TOKEN` are missing.

### `KubernetesLogAnalyzer`

- `GetNodesAsync()` — list nodes with `Ready` status.
- `GetPodsAsync(namespace)` — list pods with restart counts.
- `AnalyzePodLogsAsync(pod, namespace, tailLines)` — fetch tail logs, scan for error keywords, extract stack-trace file references.
- `AnalyzeAllPodsInNamespaceAsync(namespace, tailLines)` — **currently filters pods whose name contains `worker`** (see code, line ~114). Remove or change this guard to scan more.
- Error keywords: `error, exception, failed, fatal, panic, crash, timeout, deadlock, connection refused, out of memory, segmentation fault, null reference, undefined`.
- Stack-trace extraction regex: `\s+at\s+.+\s+in\s+(?<path>[^:]+):line\s+(?<line>\d+)`.

### `OpenAIService` (model: `gpt-4o-mini`)

- `AnalyzePodErrorAsync(pod, ns, logs)` — natural-language root cause + fix suggestion.
- `IdentifyBuggyFileAsync(errorLogs)` — returns a single file path string (or `"unknown"`).
- `GenerateCodeFixAsync(pod, ns, errorLogs, filePath, fileContent, deploymentYaml?)` — returns a `CodeFix { FilePath, OriginalCode, FixedCode, Explanation }` parsed from JSON.
- `GeneratePullRequestTitleAndDescriptionAsync(pod, errorSummary)` — convenience helper.

### `GitHubPRService` (Octokit)

- `GetFileContentAsync(path)` — fetch and base64-decode file content + SHA from the base branch.
- `SearchFileByNameAsync(fileName)` — walks the recursive Git tree to find a file by name.
- `CreatePullRequestWithMultipleCodeFixesAsync(branch, title, description, codeFixes)` — creates a branch off the base, applies every `CodeFix` via `string.Replace(OriginalCode, FixedCode)`, commits each file, and opens a single PR with a rich body listing every change.
- `CreatePullRequestWithCodeFixAsync(...)` — single-file variant.
- `CreatePullRequestAsync(branch, title, description, fileName, fileContent)` — generic create-or-update helper (used when shipping a free-form file).

Branch name format produced by `Program.cs`:

```
k8s-fix-<pod-name-lowercase>-<unix-timestamp>
```

PR title format:

```
Auto-fix: <pod-name> - <file1>, <file2>, ...
```

---

## Customization

Open the file, change the value, rebuild.

**Monitor different namespaces** (`Program.cs` / `K8SMonitorConfig.cs`):

```csharp
var config = new K8SMonitorConfig
{
    Namespaces = new[] { "production", "staging", "monitoring" }
};
```

**Scan all pods, not just `worker-*`** (`Services/KubernetesLogAnalyzer.cs`, `AnalyzeAllPodsInNamespaceAsync`):

```csharp
foreach (var pod in pods)
{
    var analysis = await AnalyzePodLogsAsync(pod.Name, namespaceName, tailLines);
    analyses.Add(analysis);
}
```

**Add custom error patterns** (`Services/KubernetesLogAnalyzer.cs`):

```csharp
private readonly string[] _errorPatterns = new[]
{
    "error", "exception", "failed", "fatal",
    "panic", "crash", "timeout", "deadlock",
    "your-custom-token"
};
```

**Change AI model** (`Services/OpenAIService.cs`):

```csharp
_client = new ChatClient("gpt-4o", _apiKey); // or "gpt-4", "gpt-3.5-turbo"
```

**Increase log context** (`K8SMonitorConfig.cs`):

```csharp
public int TailLines { get; set; } = 200;
```

**Disable PR creation** (`K8SMonitorConfig.cs`):

```csharp
public bool EnableAutoPR { get; set; } = false;
```

---

## Troubleshooting

**`OPENAI_API_KEY environment variable is required` / `GITHUB_TOKEN environment variable is required`**
Set the variable in your shell or place it in `.env`. `Validate()` runs before any work is performed.

**`Failed to connect to Kubernetes cluster`**
Make sure `kubectl get nodes` succeeds from the same shell. The app uses `KubernetesClientConfiguration.BuildConfigFromConfigFile()` and respects `KUBECONFIG`.

**`Code fix could not be applied — original code not found in file`**
The AI's `originalCode` did not match a substring in the file (whitespace mismatch, stale content, etc.). The fix is skipped; rerun or refine the prompt in `OpenAIService.GenerateCodeFixAsync`.

**`Could not find file: <path>` during PR creation**
The resolved path does not exist on the base branch of the target repo. Confirm `GITHUB_OWNER`, `GITHUB_REPO`, and `GITHUB_BASE_BRANCH`, and verify the file exists with the same casing.

**`Rate limit exceeded` from OpenAI**
Lower the volume of erroring pods per run or upgrade your OpenAI plan; consider increasing the CronJob interval.

**`404 Repository not found` / `403 Forbidden` from GitHub**
The PAT does not have `repo` scope, has expired, or lacks access to the target repository.

**No pods are analyzed**
By default `AnalyzeAllPodsInNamespaceAsync` only inspects pods whose names contain `worker`. Remove that filter to scan everything (see [Customization](#customization)).

---

## Roadmap

- Wire `Namespaces`, `TailLines`, `CheckIntervalSeconds` to env vars
- Continuous monitoring loop (today: single-shot per execution)
- Persistent error history + dedupe to avoid duplicate PRs
- Slack / Teams / email notifications
- Pluggable LLM providers (Anthropic, local models)
- Prometheus metrics export
- Web dashboard
- Multi-cluster support

---

## License

MIT — see `LICENSE` if present.
