using System.Text;
using k8s;
using K8SMonitor.Configuration;
using K8SMonitor.Services;

try
{
    var zero = 0;
    var test = 10 / zero;
}
catch (DivideByZeroException ex)
{
    Console.WriteLine(ex.ToString());
}


DotNetEnv.Env.Load(); // loads .env from current directory
// Load configuration from environment variables
var config = new K8SMonitorConfig
{
    OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty,
    GitHubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty,
    GitHubOwner = Environment.GetEnvironmentVariable("GITHUB_OWNER") ?? "sudayghosh",
    GitHubRepo = Environment.GetEnvironmentVariable("GITHUB_REPO") ?? "example-voting-app",
    GitHubBaseBranch = Environment.GetEnvironmentVariable("GITHUB_BASE_BRANCH") ?? "main",
    DryRun = Environment.GetEnvironmentVariable("DRY_RUN")?.ToLower() == "true"
};

config.Validate();

Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
Console.WriteLine("║         K8S Monitor with AI Auto-Fix                 ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════╝\n");

try
{
    // Initialize Kubernetes client
    Console.WriteLine("📡 Connecting to Kubernetes cluster...");
    var kubernetesConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile();
    var kubeClient = new Kubernetes(kubernetesConfig);
    
    var analyzer = new KubernetesLogAnalyzer(kubeClient);
    var openAI = new OpenAIService(config.OpenAIApiKey);
    var githubPR = new GitHubPRService(config.GitHubToken, config.GitHubOwner, config.GitHubRepo, config.GitHubBaseBranch);

    // Get cluster information
    Console.WriteLine("✓ Connected to cluster\n");
    
    Console.WriteLine("📊 CLUSTER INFORMATION");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    
    var nodes = await analyzer.GetNodesAsync();
    Console.WriteLine($"\nNodes: {nodes.Count}");
    foreach (var node in nodes)
    {
        var readyStatus = node.Ready ? "✓ Ready" : "✗ NotReady";
        Console.WriteLine($"  • {node.Name} - {readyStatus}");
    }

    // Analyze all pods for errors
    Console.WriteLine("\n🔍 ANALYZING POD LOGS FOR ERRORS");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    
    var allAnalyses = new List<(string Namespace, PodLogAnalysis Analysis)>();
    
    foreach (var ns in config.Namespaces)
    {
        try
        {
            Console.WriteLine($"\nScanning namespace: {ns}");
            var analyses = await analyzer.AnalyzeAllPodsInNamespaceAsync(ns, config.TailLines);
            
            foreach (var analysis in analyses)
            {
                allAnalyses.Add((ns, analysis));
                
                if (analysis.HasErrors)
                {
                    Console.WriteLine($"  ⚠️  {analysis.PodName}: {analysis.ErrorCount} error(s) found");
                    foreach (var errorLine in analysis.ErrorLines.Take(3))
                    {
                        Console.WriteLine($"      • {errorLine.Substring(0, Math.Min(80, errorLine.Length))}...");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️  Error scanning namespace {ns}: {ex.Message}");
        }
    }

    // Process errors with AI and create PRs
    var errorAnalyses = allAnalyses.Where(a => a.Analysis.HasErrors).ToList();
    
    if (errorAnalyses.Any())
    {
        Console.WriteLine("\n🤖 ANALYZING ERRORS WITH OPENAI GPT-4o Mini");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        
        var prCount = 0;
        
        foreach (var (ns, analysis) in errorAnalyses)
        {
            try
            {
                Console.WriteLine($"\n[{analysis.PodName}] Sending error analysis to OpenAI...");
                
                // Get AI analysis
                var aiAnalysis = await openAI.AnalyzePodErrorAsync(
                    analysis.PodName,
                    ns,
                    $"Error Count: {analysis.ErrorCount}\n\nError Lines:\n" + 
                    string.Join("\n", analysis.ErrorLines.Take(10))
                );
                
                Console.WriteLine($"✓ AI Analysis Complete:");
                Console.WriteLine($"───────────────────────────────────────────────────");
                var analysisLines = aiAnalysis.Split('\n').Take(5);
                foreach (var line in analysisLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        Console.WriteLine($"  {line}");
                }
                
                if (!config.DryRun && config.EnableAutoPR)
                {
                    Console.WriteLine($"\n🔍 Identifying buggy file...");
                    var errorLogsForFix = $"Error Count: {analysis.ErrorCount}\n\nError Lines:\n" + string.Join("\n", analysis.ErrorLines.Take(15));

                    string? filePath = null;

                    // 1. Prefer the file path extracted directly from the stack trace
                    if (!string.IsNullOrEmpty(analysis.StackTraceFilePath))
                    {
                        var stackFileName = System.IO.Path.GetFileName(analysis.StackTraceFilePath);
                        Console.WriteLine($"  ✓ Stack trace points to: {analysis.StackTraceFilePath} (file: {stackFileName}, line: {analysis.StackTraceFileLine})");
                        
                        // Search the repo tree for the exact filename
                        filePath = await githubPR.SearchFileByNameAsync(stackFileName);
                        if (filePath != null)
                            Console.WriteLine($"  ✓ Resolved to repo path: {filePath}");
                    }

                    // 2. Fall back to AI identification if stack trace didn't yield a result
                    if (filePath == null)
                    {
                        Console.WriteLine("  → Falling back to AI file identification...");
                        var aiFilePath = await openAI.IdentifyBuggyFileAsync(errorLogsForFix);

                        if (aiFilePath == "unknown")
                        {
                            Console.WriteLine("  ⚠️  Could not identify the specific file to fix from the logs.");
                            continue;
                        }

                        Console.WriteLine($"  ✓ AI identified file: {aiFilePath}");

                        // Try exact path first, then search by filename
                        var fileContext2 = await githubPR.GetFileContentAsync(aiFilePath);
                        if (fileContext2 != null)
                        {
                            filePath = aiFilePath;
                        }
                        else
                        {
                            var fileName = System.IO.Path.GetFileName(aiFilePath);
                            filePath = await githubPR.SearchFileByNameAsync(fileName);
                            if (filePath != null)
                                Console.WriteLine($"  ✓ Resolved AI file to repo path: {filePath}");
                        }
                    }

                    if (filePath == null)
                    {
                        Console.WriteLine($"  ⚠️  Could not find file in the repository. Skipping auto-fix.");
                        continue;
                    }

                    Console.WriteLine($"\n📥 Fetching file context from GitHub: {filePath}");
                    var fileContext = await githubPR.GetFileContentAsync(filePath);

                    if (fileContext == null)
                    {
                        Console.WriteLine($"  ⚠️  Could not fetch file '{filePath}' from the repository. Skipping auto-fix.");
                        continue;
                    }

                    Console.WriteLine($"\n⚙️ Generating AI code fix with file context...");
                    
                    // Get AI to generate actual code fix
                    var codeFix = await openAI.GenerateCodeFixAsync(
                        analysis.PodName,
                        ns,
                        errorLogsForFix,
                        filePath,
                        fileContext.Content
                    );
                    
                    Console.WriteLine($"✓ Code fix generated:");
                    Console.WriteLine($"───────────────────────────────────────────────────");
                    Console.WriteLine($"  File: {codeFix.FilePath}");
                    Console.WriteLine($"  Reason: {codeFix.Explanation.Substring(0, Math.Min(80, codeFix.Explanation.Length))}...");
                    
                    // Create branch name with timestamp
                    var branchName = $"k8s-fix-{analysis.PodName.ToLower()}-{DateTimeOffset.Now.ToUnixTimeSeconds()}";
                    
                    // Create PR with actual code fixes
                    Console.WriteLine($"\n📝 Applying fix to GitHub repository...");
                    var prNumber = await githubPR.CreatePullRequestWithCodeFixAsync(
                        branchName,
                        $"Auto-fix: {analysis.PodName} - {codeFix.Explanation.Split('\n')[0]}",
                        $"{aiAnalysis}\n\n**Pod:** `{analysis.PodName}`\n**Namespace:** `{ns}`",
                        codeFix
                    );
                    
                    prCount++;
                    Console.WriteLine($"✓ Pull Request created: #{prNumber}");
                }
                else if (config.DryRun)
                {
                    Console.WriteLine($"[DRY RUN] Would create PR for {analysis.PodName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error processing {analysis.PodName}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"\n📊 Summary: {prCount} pull request(s) created");
    }
    else
    {
        Console.WriteLine("\n✓ No errors found in cluster pods - everything looks good!");
    }
    
    Console.WriteLine("\n✓ K8S Monitor completed successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ FATAL ERROR: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    Environment.Exit(1);
}

Console.ReadLine();