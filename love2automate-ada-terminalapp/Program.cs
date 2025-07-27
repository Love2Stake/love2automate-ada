using System.CommandLine;
using System.Diagnostics;
using System.Net.Http;
using System.IO.Compression;

namespace CardanoNodeManager;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Cardano Node Management Tool - Automates Ansible playbook execution for Cardano node operations");

        // Target argument (optional)
        var targetArgument = new Argument<string?>("target", () => null, "Target to operate on (e.g., cardano-node). Not required for status checks.");
        rootCommand.AddArgument(targetArgument);

        // Install option
        var installOption = new Option<bool>(new[] { "--install", "-i" }, "Install Cardano node components");
        rootCommand.AddOption(installOption);

        // Uninstall option
        var uninstallOption = new Option<bool>(new[] { "--uninstall", "-u" }, "Uninstall Cardano node components");
        rootCommand.AddOption(uninstallOption);

        // Upgrade option
        var upgradeOption = new Option<bool>(new[] { "--upgrade", "-g" }, "Upgrade Cardano node components");
        rootCommand.AddOption(upgradeOption);

        // Status option
        var statusOption = new Option<bool>(new[] { "--status", "-s" }, "Check status of Cardano node");
        rootCommand.AddOption(statusOption);

        // Setup option
        var setupOption = new Option<bool>(new[] { "--setup" }, "Download Ansible playbooks and configuration files from repository");
        rootCommand.AddOption(setupOption);

        rootCommand.SetHandler(HandleCommand, targetArgument, installOption, uninstallOption, upgradeOption, statusOption, setupOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> HandleCommand(string? target, bool install, bool uninstall, bool upgrade, bool status, bool setup)
    {
        // Count how many options are set
        int optionCount = (install ? 1 : 0) + (uninstall ? 1 : 0) + (upgrade ? 1 : 0) + (status ? 1 : 0) + (setup ? 1 : 0);
        
        if (optionCount == 0)
        {
            Console.WriteLine("Please specify an operation: --install/-i, --uninstall/-u, --upgrade/-g, --status/-s, or --setup");
            return 1;
        }
        
        if (optionCount > 1)
        {
            Console.WriteLine("Please specify only one operation at a time.");
            return 1;
        }

        if (install)
        {
            if (string.IsNullOrEmpty(target))
            {
                Console.WriteLine("Target is required for install operation. Available targets: cardano-node");
                return 1;
            }
            return await HandleInstall(target);
        }
        else if (uninstall)
        {
            if (string.IsNullOrEmpty(target))
            {
                Console.WriteLine("Target is required for uninstall operation. Available targets: cardano-node");
                return 1;
            }
            return await HandleUninstall(target);
        }
        else if (upgrade)
        {
            if (string.IsNullOrEmpty(target))
            {
                Console.WriteLine("Target is required for upgrade operation. Available targets: cardano-node");
                return 1;
            }
            return await HandleUpgrade(target);
        }
        else if (status)
            return await HandleStatus();
        else if (setup)
            return await HandleSetup();
            
        return 1;
    }

    private static async Task<int> HandleInstall(string target)
    {
        Console.WriteLine($"Installing {target}...");
        
        if (target.ToLower() == "cardano-node")
        {
            return await ExecuteAnsiblePlaybook("Build.yml", "install_param.yml");
        }
        
        Console.WriteLine($"Unknown target: {target}");
        Console.WriteLine("Available targets: cardano-node");
        return 1;
    }

    private static async Task<int> HandleUninstall(string target)
    {
        Console.WriteLine($"Uninstalling {target}...");
        
        if (target.ToLower() == "cardano-node")
        {
            return await ExecuteAnsiblePlaybook("Uninstall.yml", "uninstall-steps/uninstall_param.yml");
        }
        
        Console.WriteLine($"Unknown target: {target}");
        Console.WriteLine("Available targets: cardano-node");
        return 1;
    }

    private static Task<int> HandleUpgrade(string target)
    {
        Console.WriteLine($"Upgrading {target}...");
        
        if (target.ToLower() == "cardano-node")
        {
            Console.WriteLine("Upgrade functionality will execute upgrade steps...");
            // For upgrade, we might want to run specific upgrade playbooks
            Console.WriteLine("Note: Upgrade steps are available in upgrade-steps/ directory");
            Console.WriteLine("Consider implementing specific upgrade playbook execution here");
            return Task.FromResult(0);
        }
        
        Console.WriteLine($"Unknown target: {target}");
        Console.WriteLine("Available targets: cardano-node");
        return Task.FromResult(1);
    }

    private static async Task<int> HandleStatus()
    {
        Console.WriteLine("Checking Cardano node status...");
        
        // Check if cardano-node process is running
        var result = await ExecuteCommand("pgrep", "-f cardano-node");
        
        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
        {
            Console.WriteLine("✓ Cardano node is running");
            Console.WriteLine($"Process ID: {result.Output.Trim()}");
            
            // Additional status checks could go here
            var portCheck = await ExecuteCommand("netstat", "-tuln | grep :6002");
            if (portCheck.ExitCode == 0)
            {
                Console.WriteLine("✓ Port 6002 is listening");
            }
        }
        else
        {
            Console.WriteLine("✗ Cardano node is not running");
        }
        
        return 0;
    }

    private static async Task<int> HandleSetup()
    {
        Console.WriteLine("Setting up love2automate-ada...");
        Console.WriteLine("Downloading Ansible playbooks and configuration files from repository...");

        var setupDir = "/opt/love2automate-ada";

        try
        {
            // Check if we have permission to write to /opt
            if (!CanWriteToDirectory("/opt"))
            {
                Console.WriteLine("✗ Setup requires sudo privileges to install to /opt/love2automate-ada");
                Console.WriteLine("Please run: sudo love2automate-ada --setup");
                return 1;
            }

            // Create setup directory if it doesn't exist
            if (Directory.Exists(setupDir))
            {
                Console.WriteLine($"Setup directory already exists: {setupDir}");
                Console.Write("Do you want to overwrite existing files? (y/N): ");
                var userResponse = Console.ReadLine()?.ToLower();
                if (userResponse != "y" && userResponse != "yes")
                {
                    Console.WriteLine("Setup cancelled.");
                    return 0;
                }
                Directory.Delete(setupDir, true);
            }

            Directory.CreateDirectory(setupDir);

            // Download the repository archive
            using var httpClient = new HttpClient();
            Console.WriteLine("Downloading repository archive...");
            
            var repoUrl = "https://github.com/Love2Stake/love2automate-ada/archive/refs/heads/main.zip";
            var response = await httpClient.GetAsync(repoUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✗ Failed to download repository: {response.StatusCode}");
                return 1;
            }

            // Save and extract the zip file
            var zipPath = Path.Combine(setupDir, "repo.zip");
            await using (var fileStream = File.Create(zipPath))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            Console.WriteLine("Extracting files...");
            ZipFile.ExtractToDirectory(zipPath, setupDir);

            // Move files from extracted directory to setup directory
            var extractedDir = Path.Combine(setupDir, "love2automate-ada-main");
            if (Directory.Exists(extractedDir))
            {
                // Copy all files except the terminal app directory
                foreach (var item in Directory.GetFileSystemEntries(extractedDir))
                {
                    var itemName = Path.GetFileName(item);
                    if (itemName == "love2automate-ada-terminalapp") continue;

                    var destPath = Path.Combine(setupDir, itemName);
                    if (Directory.Exists(item))
                    {
                        CopyDirectory(item, destPath);
                    }
                    else
                    {
                        File.Copy(item, destPath, true);
                    }
                }

                // Clean up
                Directory.Delete(extractedDir, true);
            }

            File.Delete(zipPath);

            Console.WriteLine($"✓ Setup completed successfully!");
            Console.WriteLine($"Ansible files installed to: {setupDir}");
            Console.WriteLine();
            Console.WriteLine("You can now run:");
            Console.WriteLine("  love2automate-ada --install cardano-node");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Setup failed: {ex.Message}");
            if (Directory.Exists(setupDir))
            {
                try { Directory.Delete(setupDir, true); } catch { }
            }
            return 1;
        }
    }

    private static bool CanWriteToDirectory(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $"test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }
        
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }

    private static async Task<int> ExecuteAnsiblePlaybook(string playbookFile, string paramFile)
    {
        var projectRoot = GetProjectRoot();
        var playbookPath = Path.Combine(projectRoot, playbookFile);
        var paramPath = Path.Combine(projectRoot, paramFile);
        var inventoryPath = Path.Combine(projectRoot, "inventory.ini");

        if (!File.Exists(playbookPath))
        {
            Console.WriteLine($"Error: Playbook not found: {playbookPath}");
            return 1;
        }

        if (!File.Exists(paramPath))
        {
            Console.WriteLine($"Error: Parameter file not found: {paramPath}");
            return 1;
        }

        if (!File.Exists(inventoryPath))
        {
            Console.WriteLine($"Error: Inventory file not found: {inventoryPath}");
            return 1;
        }

        // Check if we can run sudo without password
        var sudoCheck = await ExecuteCommand("sudo", "-n true");
        string arguments;
        
        if (sudoCheck.ExitCode == 0)
        {
            // Can run sudo without password
            arguments = $"-i {inventoryPath} -e @{paramPath} {playbookPath}";
        }
        else
        {
            // Need to prompt for password
            arguments = $"-i {inventoryPath} -e @{paramPath} --ask-become-pass {playbookPath}";
            Console.WriteLine("Note: This playbook requires sudo privileges. You will be prompted for your password.");
        }

        Console.WriteLine($"Executing: ansible-playbook {arguments}");

        var result = await ExecuteCommand("ansible-playbook", arguments);
        
        if (result.ExitCode == 0)
        {
            Console.WriteLine("✓ Playbook executed successfully");
        }
        else
        {
            Console.WriteLine("✗ Playbook execution failed");
            if (!string.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine($"Error: {result.Error}");
            }
        }

        return result.ExitCode;
    }

    private static async Task<(int ExitCode, string Output, string Error)> ExecuteCommand(string command, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        try
        {
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            // Handle real-time output
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.Error.WriteLine(e.Data);
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();

            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command '{command} {arguments}': {ex.Message}");
            return (1, "", ex.Message);
        }
    }

    private static string GetProjectRoot()
    {
        // Always use the standard application directory
        var appDir = "/opt/love2automate-ada";
        
        // Check if setup has been run
        if (!Directory.Exists(appDir) || !File.Exists(Path.Combine(appDir, "Build.yml")))
        {
            Console.WriteLine("✗ Ansible files not found. Please run setup first:");
            Console.WriteLine("  sudo love2automate-ada --setup");
            Environment.Exit(1);
        }
        
        return appDir;
    }
}