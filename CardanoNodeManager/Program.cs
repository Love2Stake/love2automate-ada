using System.CommandLine;
using System.Diagnostics;

namespace CardanoNodeManager;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Cardano Node Management Tool - Automates Ansible playbook execution for Cardano node operations");

        // Install command
        var installCommand = new Command("install", "Install Cardano node components");
        var installTarget = new Argument<string>("target", "Target to install (e.g., cardano-node)");
        installCommand.AddArgument(installTarget);
        installCommand.SetHandler(async (target) => await HandleInstall(target), installTarget);

        // Uninstall command
        var uninstallCommand = new Command("uninstall", "Uninstall Cardano node components");
        var uninstallTarget = new Argument<string>("target", "Target to uninstall (e.g., cardano-node)");
        uninstallCommand.AddArgument(uninstallTarget);
        uninstallCommand.SetHandler(async (target) => await HandleUninstall(target), uninstallTarget);

        // Upgrade command
        var upgradeCommand = new Command("upgrade", "Upgrade Cardano node components");
        var upgradeTarget = new Argument<string>("target", "Target to upgrade (e.g., cardano-node)");
        upgradeCommand.AddArgument(upgradeTarget);
        upgradeCommand.SetHandler(async (target) => await HandleUpgrade(target), upgradeTarget);

        // Status command
        var statusCommand = new Command("status", "Check status of Cardano node");
        statusCommand.SetHandler(async () => await HandleStatus());

        rootCommand.AddCommand(installCommand);
        rootCommand.AddCommand(uninstallCommand);
        rootCommand.AddCommand(upgradeCommand);
        rootCommand.AddCommand(statusCommand);

        return await rootCommand.InvokeAsync(args);
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

    private static async Task<int> HandleUpgrade(string target)
    {
        Console.WriteLine($"Upgrading {target}...");
        
        if (target.ToLower() == "cardano-node")
        {
            Console.WriteLine("Upgrade functionality will execute upgrade steps...");
            // For upgrade, we might want to run specific upgrade playbooks
            Console.WriteLine("Note: Upgrade steps are available in upgrade-steps/ directory");
            Console.WriteLine("Consider implementing specific upgrade playbook execution here");
            return 0;
        }
        
        Console.WriteLine($"Unknown target: {target}");
        Console.WriteLine("Available targets: cardano-node");
        return 1;
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
        var currentDir = Directory.GetCurrentDirectory();
        
        // Look for the project root by finding key files
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "Build.yml")))
        {
            var parent = Directory.GetParent(currentDir);
            if (parent == null) break;
            currentDir = parent.FullName;
        }
        
        return currentDir ?? Directory.GetCurrentDirectory();
    }
}