using k8s;
using k8s.Models;

namespace K8SMonitor.Services;

public class KubernetesLogAnalyzer
{
    private readonly IKubernetes _client;
    private readonly string[] _errorPatterns = new[]
    {
        "error", "exception", "failed", "fatal", "panic", "crash",
        "timeout", "deadlock", "connection refused", "out of memory",
        "segmentation fault", "null reference", "undefined"
    };

    public KubernetesLogAnalyzer(IKubernetes client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<List<ClusterNode>> GetNodesAsync()
    {
        try
        {
            var nodes = await _client.CoreV1.ListNodeAsync();
            return nodes.Items
                .Select(n => new ClusterNode
                {
                    Name = n.Metadata.Name,
                    Status = n.Status.Conditions?.FirstOrDefault()?.Type ?? "Unknown",
                    Ready = n.Status.Conditions?.FirstOrDefault(c => c.Type == "Ready")?.Status == "True"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to list nodes: {ex.Message}", ex);
        }
    }

    public async Task<List<Pod>> GetPodsAsync(string namespaceName = "default")
    {
        try
        {
            var pods = await _client.CoreV1.ListNamespacedPodAsync(namespaceName);
            return pods.Items
                .Select(p => new Pod
                {
                    Name = p.Metadata.Name,
                    Namespace = p.Metadata.NamespaceProperty,
                    Status = p.Status.Phase,
                    Containers = p.Spec.Containers?.Count ?? 0,
                    RestartCount = p.Status.ContainerStatuses?.Sum(cs => cs.RestartCount) ?? 0
                })
                .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to list pods in namespace {namespaceName}: {ex.Message}", ex);
        }
    }

    public async Task<PodLogAnalysis> AnalyzePodLogsAsync(string podName, string namespaceName = "default", int tailLines = 50)
    {
        try
        {
            var logStream = await _client.CoreV1.ReadNamespacedPodLogAsync(
                name: podName,
                namespaceParameter: namespaceName,
                tailLines: tailLines,
                timestamps: true,
                previous: false
            );

            var logs = await ReadStreamAsStringAsync(logStream);
            var hasErrors = ContainsErrors(logs);
            var errorLines = ExtractErrorLines(logs);

            return new PodLogAnalysis
            {
                PodName = podName,
                Namespace = namespaceName,
                FullLogs = logs,
                HasErrors = hasErrors,
                ErrorLines = errorLines,
                ErrorCount = errorLines.Count,
                AnalyzedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read logs for pod {podName}: {ex.Message}", ex);
        }
    }

    public async Task<List<PodLogAnalysis>> AnalyzeAllPodsInNamespaceAsync(string namespaceName = "default", int tailLines = 30)
    {
        try
        {
            var pods = await GetPodsAsync(namespaceName);
            var analyses = new List<PodLogAnalysis>();

            foreach (var pod in pods)
            {
                try
                {
                    if (pod.Name.Contains("worker"))
                    {
                        var analysis = await AnalyzePodLogsAsync(pod.Name, namespaceName, tailLines);
                        analyses.Add(analysis);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to analyze pod {pod.Name}: {ex.Message}");
                }
            }

            return analyses;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to analyze pods: {ex.Message}", ex);
        }
    }

    private bool ContainsErrors(string logs)
    {
        if (string.IsNullOrEmpty(logs))
            return false;

        var lowerLogs = logs.ToLower();
        return _errorPatterns.Any(pattern => lowerLogs.Contains(pattern));
    }

    private List<string> ExtractErrorLines(string logs)
    {
        if (string.IsNullOrEmpty(logs))
            return new List<string>();

        return logs.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Where(line => _errorPatterns.Any(pattern => line.ToLower().Contains(pattern)))
            .ToList();
    }

    public async Task<PreviousLogsAnalysis> AnalyzePreviousPodLogsAsync(string podName, string namespaceName = "default")
    {
        try
        {
            var logStream = await _client.CoreV1.ReadNamespacedPodLogAsync(
                name: podName,
                namespaceParameter: namespaceName,
                previous: true
            );

            var logs = await ReadStreamAsStringAsync(logStream);
            return new PreviousLogsAnalysis
            {
                PodName = podName,
                Namespace = namespaceName,
                PreviousLogs = logs,
                HasCrashIndication = ContainsErrors(logs),
                AnalyzedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note: No previous logs available for {podName}: {ex.Message}");
            return new PreviousLogsAnalysis
            {
                PodName = podName,
                Namespace = namespaceName,
                PreviousLogs = null,
                HasCrashIndication = false
            };
        }
    }

    private async Task<string> ReadStreamAsStringAsync(System.IO.Stream stream)
    {
        using (var reader = new System.IO.StreamReader(stream))
        {
            return await reader.ReadToEndAsync();
        }
    }
}

public class ClusterNode
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Ready { get; set; }
}

public class Pod
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Containers { get; set; }
    public int RestartCount { get; set; }
}

public class PodLogAnalysis
{
    public string PodName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullLogs { get; set; } = string.Empty;
    public bool HasErrors { get; set; }
    public List<string> ErrorLines { get; set; } = new();
    public int ErrorCount { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class PreviousLogsAnalysis
{
    public string PodName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string? PreviousLogs { get; set; }
    public bool HasCrashIndication { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
