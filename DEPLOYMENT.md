# K8S Monitor - Deployment Guide

## Deployment Options

### 1. Local Development

**Prerequisites:**
- .NET 8.0+
- kubectl with cluster access
- OpenAI API key
- GitHub personal access token

**Steps:**
```bash
# Set environment variables
export OPENAI_API_KEY="sk-..."
export GITHUB_TOKEN="ghp_..."
export GITHUB_OWNER="your-username"
export GITHUB_REPO="example-voting-app"

# Build and run
dotnet build
dotnet run
```

---

### 2. Docker Container

**Build image:**
```bash
docker build -t k8s-monitor:latest .
```

**Run container:**
```bash
docker run -e OPENAI_API_KEY="sk-..." \
           -e GITHUB_TOKEN="ghp_..." \
           -e GITHUB_OWNER="your-username" \
           -e GITHUB_REPO="example-voting-app" \
           -e DRY_RUN="false" \
           -v ~/.kube/config:/root/.kube/config:ro \
           k8s-monitor:latest
```

**Push to registry:**
```bash
# Tag image
docker tag k8s-monitor:latest ghcr.io/your-username/k8s-monitor:latest

# Login to registry
echo $GITHUB_TOKEN | docker login ghcr.io -u your-username --password-stdin

# Push
docker push ghcr.io/your-username/k8s-monitor:latest
```

---

### 3. Kubernetes CronJob Deployment

**Prerequisites:**
- Kubernetes cluster (1.20+)
- kubectl access with cluster-admin privileges
- Image pushed to registry

**Deployment steps:**

1. **Update the manifest** (`k8s-deployment.yaml`):
```yaml
# Update image reference
image: ghcr.io/your-username/k8s-monitor:latest

# Update secrets
stringData:
  OPENAI_API_KEY: "sk-your-actual-key"
  GITHUB_TOKEN: "ghp_your-actual-token"
  GITHUB_OWNER: "your-username"
  GITHUB_REPO: "example-voting-app"
```

2. **Deploy to cluster**:
```bash
# Create namespace and resources
kubectl apply -f k8s-deployment.yaml

# Verify deployment
kubectl get cronjobs -n monitoring
kubectl get serviceaccounts -n monitoring

# Check status
kubectl describe cronjob k8s-monitor -n monitoring
```

3. **Trigger immediate run** (for testing):
```bash
# Create a manual job from the cronjob template
kubectl create job --from=cronjob/k8s-monitor test-run -n monitoring

# Watch execution
kubectl logs -f job/test-run -n monitoring
```

4. **View logs**:
```bash
# Latest job logs
kubectl logs -n monitoring -l job-name=$(kubectl get jobs -n monitoring -o jsonpath='{.items[-1].metadata.name}')

# Watch in real-time
kubectl logs -f -n monitoring -l job-name=$(kubectl get jobs -n monitoring -o jsonpath='{.items[-1].metadata.name}')
```

---

### 4. GitHub Actions Workflow

**Setup:**

1. Add secrets to GitHub:
```bash
# Go to Settings → Secrets and variables → Actions
```

Add these secrets:
- `OPENAI_API_KEY` - Your OpenAI API key
- `GITHUB_TOKEN` - (Usually auto-created by GitHub)

2. The workflow will:
   - Build on every push to `main` or `example-voting-app`
   - Run tests
   - Push Docker image
   - Execute K8S Monitor on `example-voting-app` branch

**View workflow runs:**
```
https://github.com/your-username/example-voting-app/actions
```

---

## Advanced Configuration

### Custom Error Patterns

Modify `KubernetesLogAnalyzer.cs`:
```csharp
private readonly string[] _errorPatterns = new[]
{
    "error", "exception", "failed", "fatal",
    "panic", "crash", "timeout", "deadlock",
    "connection refused", "out of memory",
    "segmentation fault", "null reference",
    "your-custom-error-pattern"  // Add here
};
```

### Custom Namespaces to Monitor

In `Program.cs`:
```csharp
config.Namespaces = new[] { 
    "production", 
    "staging", 
    "monitoring",
    "kube-system" 
};
```

### Different OpenAI Model

In `OpenAIService.cs`:
```csharp
// Instead of:
_client = new ChatClient("gpt-4o-mini", _apiKey);

// Use:
_client = new ChatClient("gpt-4-turbo", _apiKey);
_client = new ChatClient("gpt-3.5-turbo", _apiKey);
```

### Scheduled Monitoring

Option A: CronJob (Kubernetes):
```yaml
schedule: "*/5 * * * *"  # Every 5 minutes
schedule: "0 * * * *"    # Hourly
schedule: "0 0 * * *"    # Daily at midnight
```

Option B: Windows Task Scheduler:
```powershell
$trigger = New-ScheduledTaskTrigger -AtStartup
$action = New-ScheduledTaskAction -Execute "dotnet" -Argument "run" -WorkingDirectory "C:\K8SMonitor"
Register-ScheduledTask -TaskName "K8SMonitor" -Trigger $trigger -Action $action
```

Option C: systemd (Linux):
```ini
# /etc/systemd/system/k8s-monitor.service
[Unit]
Description=K8S Monitor
After=network.target

[Service]
Type=oneshot
User=k8smonitor
WorkingDirectory=/opt/k8s-monitor
ExecStart=/usr/bin/dotnet run
Environment="OPENAI_API_KEY=sk-..."
Environment="GITHUB_TOKEN=ghp_..."

[Install]
WantedBy=multi-user.target
```

Then create timer:
```ini
# /etc/systemd/system/k8s-monitor.timer
[Unit]
Description=Run K8S Monitor every 5 minutes
Requires=k8s-monitor.service

[Timer]
OnBootSec=1min
OnUnitActiveSec=5min
Unit=k8s-monitor.service

[Install]
WantedBy=timers.target
```

Enable:
```bash
sudo systemctl enable k8s-monitor.timer
sudo systemctl start k8s-monitor.timer
sudo systemctl status k8s-monitor.timer
```

---

## Troubleshooting Deployment

### Issue: Pod can't access kubeconfig

**Solution:**
```yaml
volumeMounts:
- name: kubeconfig
  mountPath: /root/.kube/config
  readOnly: true
volumes:
- name: kubeconfig
  hostPath:
    path: /etc/kubernetes/admin.conf
```

### Issue: Permission denied errors

**Verify RBAC:**
```bash
# Test if service account has permissions
kubectl auth can-i get pods --as=system:serviceaccount:monitoring:k8s-monitor -n monitoring

# Test pod logs access
kubectl auth can-i get pods/log --as=system:serviceaccount:monitoring:k8s-monitor
```

### Issue: Docker image push fails

**Solution:**
```bash
# Verify Docker login
docker logout ghcr.io
echo $GITHUB_TOKEN | docker login ghcr.io -u YOUR_USERNAME --password-stdin

# Retry push
docker push ghcr.io/your-username/k8s-monitor:latest
```

### Issue: CronJob not triggering

**Debug:**
```bash
# Check CronJob syntax
kubectl get cronjobs -n monitoring

# View CronJob events
kubectl describe cronjob k8s-monitor -n monitoring

# Check if controller is running
kubectl get pods -n kube-system | grep cronjob

# Manually test
kubectl create job --from=cronjob/k8s-monitor test -n monitoring
```

---

## Monitoring the Monitor

### Check recent runs:
```bash
kubectl get jobs -n monitoring -o wide

# With more detail
kubectl get jobs -n monitoring -o jsonpath='{range .items[*]}{.metadata.name}{"\t"}{.status.startTime}{"\t"}{.status.completionTime}{"\n"}{end}'
```

### View execution logs:
```bash
# Get latest job name
LATEST_JOB=$(kubectl get jobs -n monitoring -o jsonpath='{.items[-1].metadata.name}')

# View logs
kubectl logs -n monitoring -l job-name=$LATEST_JOB

# Follow logs in real-time
kubectl logs -f -n monitoring -l job-name=$LATEST_JOB
```

### Monitor OpenAI API usage:
```bash
# Check your usage: https://platform.openai.com/account/usage/overview
```

### Track GitHub PRs created:
```bash
# View all k8s-fix branches
git branch -a | grep k8s-fix

# Get PR statistics
curl -H "Authorization: token $GITHUB_TOKEN" \
     https://api.github.com/repos/YOUR_OWNER/YOUR_REPO/pulls?state=open&head=YOUR_OWNER:k8s-fix
```

---

## Scaling Considerations

### Multiple Clusters

Modify `Program.cs` to support multiple clusters:
```csharp
var clusters = new[] { "cluster1", "cluster2", "cluster3" };

foreach (var cluster in clusters)
{
    var config = KubernetesClientConfiguration.BuildConfigFromConfigFile($"~/.kube/{cluster}");
    var client = new Kubernetes(config);
    // ... analyze cluster
}
```

### Rate Limiting

Add delay between API calls:
```csharp
// In OpenAIService
await Task.Delay(1000); // Wait 1 second between calls
```

### Resource Constraints

**For Kubernetes deployment**, adjust:
```yaml
resources:
  requests:
    cpu: 100m        # Minimum CPU
    memory: 256Mi     # Minimum memory
  limits:
    cpu: 500m        # Maximum CPU
    memory: 512Mi     # Maximum memory
```

---

## CI/CD Integration Examples

### GitLab CI
```yaml
deploy-k8s-monitor:
  image: mcr.microsoft.com/dotnet:8.0
  script:
    - dotnet restore
    - dotnet build --configuration Release
    - dotnet run --configuration Release
  only:
    - main
  variables:
    OPENAI_API_KEY: $OPENAI_API_KEY
    GITHUB_TOKEN: $GITHUB_TOKEN
```

### Jenkins
```groovy
pipeline {
    agent any
    
    environment {
        OPENAI_API_KEY = credentials('openai-key')
        GITHUB_TOKEN = credentials('github-token')
        GITHUB_OWNER = 'your-username'
        GITHUB_REPO = 'example-voting-app'
    }
    
    stages {
        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release'
            }
        }
        stage('Run Monitor') {
            steps {
                sh 'dotnet run --configuration Release'
            }
        }
    }
}
```

---

## Performance Optimization

1. **Reduce tail lines**: Lower `TAIL_LINES` for faster analysis
2. **Limit namespaces**: Monitor only critical namespaces
3. **Cache API responses**: Implement response caching
4. **Batch processing**: Process multiple pods in parallel
5. **Adjust check interval**: Increase `CHECK_INTERVAL_SECONDS` for less frequent runs

---

## Security Best Practices

1. **Store secrets in vault**: Use Kubernetes Secrets or external vault
2. **Use RBAC**: Limit service account permissions to minimum required
3. **Network policies**: Restrict pod traffic
4. **Read-only filesystem**: Use `readOnlyRootFilesystem: true`
5. **No privilege escalation**: Use `allowPrivilegeEscalation: false`
6. **Regular updates**: Keep dependencies updated
7. **API key rotation**: Regularly rotate OpenAI and GitHub tokens

---

## Support & Resources

- [K8s Monitor Documentation](README.md)
- [Quick Start Guide](QUICKSTART.md)
- [OpenAI Documentation](https://platform.openai.com/docs)
- [Kubernetes Documentation](https://kubernetes.io/docs)
- [GitHub API Docs](https://docs.github.com/en/rest)
