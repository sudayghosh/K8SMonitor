using System.Text;
using k8s;
using K8SMonitor.Configuration;
using K8SMonitor.Services;
using Worker;


//try
//{
//    var connectionString = "Server=your_server;Database=your_database;User Id=your_username;Password=your_password;";

//    var employees = Employee.GetAllEmployees(connectionString);
//}
//catch (Exception ex)
//{
//    Console.WriteLine(ex.ToString());
//}


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
                    Console.WriteLine($"\n🔍 Identifying buggy files...");
                    var errorLogsForFix = $"Error Count: {analysis.ErrorCount}\n\nError Lines:\n" + string.Join("\n", analysis.ErrorLines.Take(15));

                    // Get all unique file paths from stack traces
                    var filePathsToProcess = new List<string>();

                    // 1. Collect file paths from stack trace references
                    if (analysis.StackTraceFileReferences.Any())
                    {
                        Console.WriteLine($"  Found {analysis.StackTraceFileReferences.Count} file(s) in stack trace:");
                        
                        foreach (var fileRef in analysis.StackTraceFileReferences)
                        {
                            var stackFileName = System.IO.Path.GetFileName(fileRef.FilePath);
                            Console.WriteLine($"    • {fileRef.FilePath} (line: {fileRef.LineNumber})");
                            
                            // Search the repo tree for the exact filename
                            var resolvedPath = await githubPR.SearchFileByNameAsync(stackFileName);
                            if (resolvedPath != null)
                            {
                                filePathsToProcess.Add(resolvedPath);
                                Console.WriteLine($"      ✓ Resolved to: {resolvedPath}");
                            }
                        }
                    }

                    // 2. Fall back to AI identification if stack trace didn't yield results
                    if (!filePathsToProcess.Any())
                    {
                        Console.WriteLine("  → Falling back to AI file identification...");
                        var aiFilePath = await openAI.IdentifyBuggyFileAsync(errorLogsForFix);

                        if (aiFilePath != "unknown")
                        {
                            Console.WriteLine($"  ✓ AI identified file: {aiFilePath}");

                            // Try exact path first, then search by filename
                            var fileContext2 = await githubPR.GetFileContentAsync(aiFilePath);
                            if (fileContext2 != null)
                            {
                                filePathsToProcess.Add(aiFilePath);
                            }
                            else
                            {
                                var fileName = System.IO.Path.GetFileName(aiFilePath);
                                var resolvedPath = await githubPR.SearchFileByNameAsync(fileName);
                                if (resolvedPath != null)
                                {
                                    filePathsToProcess.Add(resolvedPath);
                                    Console.WriteLine($"  ✓ Resolved AI file to repo path: {resolvedPath}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("  ⚠️  Could not identify any files to fix from the logs.");
                        }
                    }

                    if (!filePathsToProcess.Any())
                    {
                        Console.WriteLine($"  ⚠️  Could not find any files in the repository. Skipping auto-fix.");
                        continue;
                    }

                    // Collect all code fixes for all files
                    var allCodeFixes = new List<CodeFix>();
                    var filesProcessed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var filePath in filePathsToProcess)
                    {
                        // Skip if we've already processed this file in this iteration
                        if (filesProcessed.Contains(filePath))
                            continue;

                        filesProcessed.Add(filePath);

                        Console.WriteLine($"\n📥 Fetching file context from GitHub: {filePath}");
                        var fileContext = await githubPR.GetFileContentAsync(filePath);

                        if (fileContext == null)
                        {
                            Console.WriteLine($"  ⚠️  Could not fetch file '{filePath}' from the repository. Skipping.");
                            continue;
                        }

                        Console.WriteLine($"⚙️ Generating AI code fix with file context...");
                        
                        // Get AI to generate actual code fix
                        var codeFix = await openAI.GenerateCodeFixAsync(
                            analysis.PodName,
                            ns,
                            errorLogsForFix,
                            filePath,
                            fileContext.Content
                        );
                        
                        Console.WriteLine($"✓ Code fix generated for: {codeFix.FilePath}");
                        allCodeFixes.Add(codeFix);
                    }

                    if (!allCodeFixes.Any())
                    {
                        Console.WriteLine($"\n⚠️  Could not generate any fixes for the pod errors. Skipping PR creation.");
                        continue;
                    }

                    Console.WriteLine($"\n🔀 Generated {allCodeFixes.Count} fix(es) for {filesProcessed.Count} file(s)");
                    
                    // Create ONE branch name with timestamp
                    var branchName = $"k8s-fix-{analysis.PodName.ToLower()}-{DateTimeOffset.Now.ToUnixTimeSeconds()}";
                    
                    // Create SINGLE PR with all fixes
                    Console.WriteLine($"\n📝 Creating unified PR with all fixes...");
                    var filesSection = string.Join(", ", allCodeFixes.Select(f => System.IO.Path.GetFileName(f.FilePath)));
                    var prTitle = $"Auto-fix: {analysis.PodName} - {filesSection}";
                    
                    var prNumber = await githubPR.CreatePullRequestWithMultipleCodeFixesAsync(
                        branchName,
                        prTitle,
                        $"{aiAnalysis}\n\n**Pod:** `{analysis.PodName}`\n**Namespace:** `{ns}`\n**Total Fixes:** {allCodeFixes.Count}",
                        allCodeFixes
                    );
                    
                    prCount++;
                    Console.WriteLine($"✓ Pull Request created: #{prNumber}");
                }
                else if (config.DryRun)
                {
                    Console.WriteLine($"[DRY RUN] Would create PR(s) for {analysis.PodName}");
                    if (analysis.StackTraceFileReferences.Any())
                    {
                        Console.WriteLine($"[DRY RUN] Files to process: {string.Join(", ", analysis.StackTraceFileReferences.Select(f => f.FilePath))}");
                    }
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