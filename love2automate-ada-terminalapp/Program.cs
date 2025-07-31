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

        // Upgrade option (COMMENTED OUT - NOT TESTED YET)
        // var upgradeOption = new Option<bool>(new[] { "--upgrade", "-g" }, "Upgrade Cardano node components");
        // rootCommand.AddOption(upgradeOption);

        // Status option
        var statusOption = new Option<bool>(new[] { "--status", "-s" }, "Check status of Cardano node");
        rootCommand.AddOption(statusOption);

        // Setup option
        var setupOption = new Option<bool>(new[] { "--setup" }, "Download Ansible playbooks and configuration files from repository");
        rootCommand.AddOption(setupOption);

        // Setup dependencies option
        var setupDepsOption = new Option<bool>(new[] { "--setup-deps" }, "Install required dependencies (apt update, ansible, collections)");
        rootCommand.AddOption(setupDepsOption);

        // Complete removal option
        var completeRemovalOption = new Option<bool>(new[] { "--remove-all" }, "Completely remove all installed components and dependencies");
        rootCommand.AddOption(completeRemovalOption);

        // Port option
        var portOption = new Option<int?>(new[] { "--port", "-p" }, "Port number for Cardano node (updates install_param.yml)");
        rootCommand.AddOption(portOption);

        rootCommand.SetHandler(HandleCommand, targetArgument, installOption, uninstallOption, statusOption, setupOption, setupDepsOption, completeRemovalOption, portOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> HandleCommand(string? target, bool install, bool uninstall, bool status, bool setup, bool setupDeps, bool removeAll, int? port)
    {
        // Validate that --port is only used with --install
        if (port.HasValue && !install)
        {
            Console.WriteLine("✗ The --port parameter can only be used with the --install operation.");
            return 1;
        }

        // Count how many options are set
        int optionCount = (install ? 1 : 0) + (uninstall ? 1 : 0) + (status ? 1 : 0) + (setup ? 1 : 0) + (setupDeps ? 1 : 0) + (removeAll ? 1 : 0);
        
        if (optionCount == 0)
        {
            Console.WriteLine("Please specify an operation: --install/-i, --uninstall/-u, --status/-s, --setup, --setup-deps, or --remove-all");
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
            return await HandleInstall(target, port);
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
        // UPGRADE FUNCTIONALITY COMMENTED OUT - NOT TESTED YET
        /*
        else if (upgrade)
        {
            if (string.IsNullOrEmpty(target))
            {
                Console.WriteLine("Target is required for upgrade operation. Available targets: cardano-node");
                return 1;
            }
            return await HandleUpgrade(target);
        }
        */
        else if (status)
            return await HandleStatus();
        else if (setup)
            return await HandleSetup();
        else if (setupDeps)
            return await HandleSetupDependencies();
        else if (removeAll)
            return await HandleCompleteRemoval();
            
        return 1;
    }

    private static async Task<int> HandleInstall(string target, int? port = null)
    {
        Console.WriteLine($"Installing {target}...");
        
        // Check prerequisites before proceeding
        var prereqCheck = await CheckPrerequisites();
        if (prereqCheck != 0)
        {
            return prereqCheck;
        }
        
        if (target.ToLower() == "cardano-node")
        {
            var paramFile = await PrepareParameterFile("install_param.yml", port);
            var result = await ExecuteAnsiblePlaybook("Build.yml", paramFile);
            
            // Store configuration after successful installation
            if (result == 0)
            {
                await StoreInstallationConfig(port);
            }
            
            return result;
        }
        
        Console.WriteLine($"Unknown target: {target}");
        Console.WriteLine("Available targets: cardano-node");
        return 1;
    }

    private static async Task<int> HandleUninstall(string target)
    {
        Console.WriteLine($"Uninstalling {target}...");
        
        // Check prerequisites before proceeding
        var prereqCheck = await CheckPrerequisites();
        if (prereqCheck != 0)
        {
            return prereqCheck;
        }
        
        if (target.ToLower() == "cardano-node")
        {
            return await ExecuteAnsiblePlaybook("Uninstall.yml", "uninstall-steps/uninstall_param.yml");
        }
        
        Console.WriteLine($"Unknown target: {target}");
        Console.WriteLine("Available targets: cardano-node");
        return 1;
    }

    // UPGRADE FUNCTIONALITY COMMENTED OUT - NOT TESTED YET
    /*
    private static async Task<int> HandleUpgrade(string target)
    {
        Console.WriteLine($"Upgrading {target}...");
        
        // Check prerequisites before proceeding
        var prereqCheck = await CheckPrerequisites();
        if (prereqCheck != 0)
        {
            return prereqCheck;
        }
        
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
    */

    private static async Task<int> HandleStatus()
    {
        Console.WriteLine("Checking Cardano node status...");
        
        // Get the configured port
        var configuredPort = await GetConfiguredPort();
        
        // Check if cardano-node process is running
        var result = await ExecuteCommand("pgrep", "-f cardano-node");
        
        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
        {
            Console.WriteLine("✓ Cardano node is running");
            Console.WriteLine($"Process ID: {result.Output.Trim()}");
            
            // Check if the configured port is listening
            var portCheck = await ExecuteCommand("netstat", $"-tuln | grep :{configuredPort}");
            if (portCheck.ExitCode == 0)
            {
                Console.WriteLine($"✓ Port {configuredPort} is listening");
            }
            else
            {
                Console.WriteLine($"⚠️  Port {configuredPort} is not listening (node may be starting up)");
            }
        }
        else
        {
            Console.WriteLine("✗ Cardano node is not running");
        }
        
        Console.WriteLine($"Configured port: {configuredPort}");
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

    private static async Task<int> HandleSetupDependencies()
    {
        Console.WriteLine("Setting up dependencies for love2automate-ada...");
        Console.WriteLine("This will install Ansible and required collections.");
        
        try
        {
            // Update package index
            Console.WriteLine("Updating package index...");
            var updateResult = await ExecuteCommand("sudo", "apt update");
            if (updateResult.ExitCode != 0)
            {
                Console.WriteLine("✗ Failed to update package index");
                return 1;
            }
            Console.WriteLine("✓ Package index updated");

            // Install required packages including pipx for modern Python package management
            Console.WriteLine("Installing required packages...");
            var packagesResult = await ExecuteCommand("sudo", "apt install -y python3-pip python3-venv pipx");
            if (packagesResult.ExitCode != 0)
            {
                Console.WriteLine("✗ Failed to install required packages");
                return 1;
            }
            Console.WriteLine("✓ Required packages installed");

            // Install ansible-core using pipx (includes ansible-playbook and ansible-galaxy)
            Console.WriteLine("Installing Ansible Core using pipx...");
            var ansibleCoreResult = await ExecuteCommand("pipx", "install ansible-core");
            if (ansibleCoreResult.ExitCode != 0)
            {
                Console.WriteLine("Pipx installation failed, trying with pip3 --user --break-system-packages...");
                var fallbackResult = await ExecuteCommand("pip3", "install --user --break-system-packages ansible-core");
                if (fallbackResult.ExitCode != 0)
                {
                    Console.WriteLine("✗ Failed to install ansible-core with both methods");
                    return 1;
                }
            }
            Console.WriteLine("✓ Ansible Core installed");

            // Also install the full ansible package for additional modules
            Console.WriteLine("Installing full Ansible package...");
            var ansibleFullResult = await ExecuteCommand("pipx", "install ansible");
            if (ansibleFullResult.ExitCode != 0)
            {
                Console.WriteLine("Full ansible package installation failed, continuing with core only...");
            }
            else
            {
                Console.WriteLine("✓ Full Ansible package installed");
            }

            // Ensure pipx bin directory is in PATH
            Console.WriteLine("Ensuring pipx is properly configured...");
            await ExecuteCommand("pipx", "ensurepath");

            // Add to PATH in .bashrc if not already present
            Console.WriteLine("Updating PATH in .bashrc...");
            var homeDir = Environment.GetEnvironmentVariable("HOME");
            var bashrcPath = Path.Combine(homeDir ?? "/root", ".bashrc");
            var pathExportPip = "export PATH=\"$HOME/.local/bin:$PATH\"";
            
            if (File.Exists(bashrcPath))
            {
                var bashrcContent = await File.ReadAllTextAsync(bashrcPath);
                var needsUpdate = false;
                var pathsToAdd = new List<string>();
                
                if (!bashrcContent.Contains("$HOME/.local/bin"))
                {
                    pathsToAdd.Add(pathExportPip);
                    needsUpdate = true;
                }
                
                if (needsUpdate)
                {
                    var pathSection = "\n# Added by love2automate-ada setup\n" + string.Join("\n", pathsToAdd) + "\n";
                    await File.AppendAllTextAsync(bashrcPath, pathSection);
                    Console.WriteLine("✓ PATH updated in .bashrc");
                }
                else
                {
                    Console.WriteLine("✓ PATH already configured in .bashrc");
                }
            }

            // Install Ansible collections
            Console.WriteLine("Installing Ansible collections...");
            
            // Try to find ansible-galaxy in common locations
            var ansibleGalaxyPaths = new[]
            {
                "ansible-galaxy", // If in PATH
                $"{homeDir}/.local/bin/ansible-galaxy", // pipx/pip user install
                "/usr/bin/ansible-galaxy" // system install
            };
            
            string? workingAnsibleGalaxy = null;
            foreach (var path in ansibleGalaxyPaths)
            {
                var testResult = await ExecuteCommand("which", path, suppressOutput: true);
                if (testResult.ExitCode == 0 || File.Exists(path))
                {
                    workingAnsibleGalaxy = path;
                    break;
                }
            }
            
            if (workingAnsibleGalaxy == null)
            {
                Console.WriteLine("✗ Could not find ansible-galaxy command");
                return 1;
            }
            
            // Install community.general collection
            var communityResult = await ExecuteCommand(workingAnsibleGalaxy, "collection install community.general");
            if (communityResult.ExitCode != 0)
            {
                Console.WriteLine("✗ Failed to install community.general collection");
                return 1;
            }
            Console.WriteLine("✓ community.general collection installed");

            // Install ansible.posix collection
            var posixResult = await ExecuteCommand(workingAnsibleGalaxy, "collection install ansible.posix");
            if (posixResult.ExitCode != 0)
            {
                Console.WriteLine("✗ Failed to install ansible.posix collection");
                return 1;
            }
            Console.WriteLine("✓ ansible.posix collection installed");

            Console.WriteLine();
            Console.WriteLine("✓ Dependencies setup completed successfully!");
            Console.WriteLine();
            Console.WriteLine("⚠️  IMPORTANT: You MUST restart your terminal or run 'source ~/.bashrc' for PATH changes to take effect.");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("1. Restart your terminal (or run: source ~/.bashrc)");
            Console.WriteLine("2. Run: love2automate-ada --setup");
            Console.WriteLine("3. Configure your inventory.ini file");
            Console.WriteLine("4. Run: love2automate-ada --install cardano-node");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Dependencies setup failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleCompleteRemoval()
    {
        Console.WriteLine("⚠️  WARNING: This will completely remove ALL installed components and dependencies!");
        Console.WriteLine("This includes:");
        Console.WriteLine("- Cardano node installation");
        Console.WriteLine("- All build dependencies");
        Console.WriteLine("- GHCup installation");
        Console.WriteLine("- Ansible playbook files");
        Console.WriteLine("- Environment variables");
        Console.WriteLine();
        Console.Write("Are you sure you want to proceed? Type 'YES' to confirm: ");
        
        var confirmation = Console.ReadLine();
        if (confirmation != "YES")
        {
            Console.WriteLine("Cleanup cancelled.");
            return 0;
        }

        Console.WriteLine("Starting cleanup process...");

        try
        {
            // Stop cardano-node if running
            Console.WriteLine("Stopping Cardano node...");
            await ExecuteCommand("sudo", "pkill -f cardano-node");
            await ExecuteCommand("sudo", "systemctl stop cardano-node");
            await ExecuteCommand("sudo", "systemctl disable cardano-node");

            // Run the Uninstall.yml playbook to properly remove Cardano node
            Console.WriteLine("Running Uninstall.yml playbook...");
            var uninstallResult = await ExecuteAnsiblePlaybook("Uninstall.yml", "uninstall-steps/uninstall_param.yml");
            if (uninstallResult != 0)
            {
                Console.WriteLine("⚠️  Uninstall playbook failed, but continuing with cleanup...");
            }

            // Remove ansible files
            Console.WriteLine("Removing Ansible files...");
            await ExecuteCommand("sudo", "rm -rf /opt/love2automate-ada");

            // Clean up environment variables from .bashrc
            Console.WriteLine("Cleaning up environment variables...");
            var homeDir = Environment.GetEnvironmentVariable("HOME");
            var bashrcPath = Path.Combine(homeDir ?? "/root", ".bashrc");
            
            if (File.Exists(bashrcPath))
            {
                var bashrcContent = await File.ReadAllTextAsync(bashrcPath);
                var lines = bashrcContent.Split('\n').ToList();
                
                // Remove lines added by love2automate-ada
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (lines[i].Contains("love2automate-ada") || 
                        lines[i].Contains("LD_LIBRARY_PATH") ||
                        lines[i].Contains("PKG_CONFIG_PATH") ||
                        (i > 0 && lines[i-1].Contains("love2automate-ada")))
                    {
                        lines.RemoveAt(i);
                    }
                }
                
                await File.WriteAllTextAsync(bashrcPath, string.Join('\n', lines));
            }

            // Reload systemd
            await ExecuteCommand("sudo", "systemctl daemon-reload");

            Console.WriteLine();
            Console.WriteLine("✓ Cleanup completed successfully!");
            Console.WriteLine("The system has been restored to a clean state.");
            Console.WriteLine("Note: You may need to restart your shell to clear environment variables.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Cleanup failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> CheckPrerequisites()
    {
        Console.WriteLine("Checking prerequisites...");
        
        // Check 1: Ansible installation
        Console.Write("Checking Ansible installation... ");
        
        // Try to find ansible-playbook in common locations
        var homeDir = Environment.GetEnvironmentVariable("HOME");
        var ansiblePlaybookPaths = new[]
        {
            "ansible-playbook", // If in PATH
            $"{homeDir}/.local/bin/ansible-playbook", // pipx/pip user install
            "/usr/bin/ansible-playbook" // system install
        };
        
        bool ansibleFound = false;
        foreach (var path in ansiblePlaybookPaths)
        {
            var testResult = await ExecuteCommand("which", path, suppressOutput: true);
            if (testResult.ExitCode == 0 || File.Exists(path))
            {
                ansibleFound = true;
                break;
            }
        }
        
        if (!ansibleFound)
        {
            Console.WriteLine("✗ NOT FOUND");
            Console.WriteLine();
            Console.WriteLine("Ansible is not installed or not in PATH.");
            Console.WriteLine("Please run: love2automate-ada --setup-deps");
            return 1;
        }
        Console.WriteLine("✓ FOUND");

        // Check 2: Required Ansible collections
        Console.Write("Checking Ansible collections... ");
        
        // Try to find ansible-galaxy in common locations
        var ansibleGalaxyPaths = new[]
        {
            "ansible-galaxy", // If in PATH
            $"{homeDir}/.local/bin/ansible-galaxy", // pipx/pip user install
            "/usr/bin/ansible-galaxy" // system install
        };
        
        string? workingAnsibleGalaxy = null;
        foreach (var path in ansibleGalaxyPaths)
        {
            var testResult = await ExecuteCommand("which", path, suppressOutput: true);
            if (testResult.ExitCode == 0 || File.Exists(path))
            {
                workingAnsibleGalaxy = path;
                break;
            }
        }
        
        if (workingAnsibleGalaxy == null)
        {
            Console.WriteLine("✗ MISSING");
            Console.WriteLine();
            Console.WriteLine("ansible-galaxy command not found.");
            Console.WriteLine("Please run: love2automate-ada --setup-deps");
            return 1;
        }
        
        // Check for each collection individually since the combined command might fail
        var communityCheck = await ExecuteCommand(workingAnsibleGalaxy, "collection list community.general", suppressOutput: true);
        var posixCheck = await ExecuteCommand(workingAnsibleGalaxy, "collection list ansible.posix", suppressOutput: true);
        
        if (communityCheck.ExitCode != 0 || posixCheck.ExitCode != 0)
        {
            Console.WriteLine("✗ MISSING");
            Console.WriteLine();
            if (communityCheck.ExitCode != 0)
                Console.WriteLine("Missing: community.general collection");
            if (posixCheck.ExitCode != 0)
                Console.WriteLine("Missing: ansible.posix collection");
            Console.WriteLine("Please run: love2automate-ada --setup-deps");
            return 1;
        }
        Console.WriteLine("✓ FOUND");

        // Check 3: Ansible files setup
        Console.Write("Checking Ansible files... ");
        var appDir = "/opt/love2automate-ada";
        var buildYmlPath = Path.Combine(appDir, "Build.yml");
        var inventoryPath = Path.Combine(appDir, "inventory.ini");
        
        if (!Directory.Exists(appDir) || !File.Exists(buildYmlPath))
        {
            Console.WriteLine("✗ NOT FOUND");
            Console.WriteLine();
            Console.WriteLine("Ansible playbook files are not installed.");
            Console.WriteLine("Please run: love2automate-ada --setup");
            return 1;
        }
        Console.WriteLine("✓ FOUND");

        // Check 4: Inventory file
        Console.Write("Checking inventory.ini... ");
        if (!File.Exists(inventoryPath))
        {
            Console.WriteLine("✗ NOT FOUND");
            Console.WriteLine();
            Console.WriteLine("Inventory file is missing. Please create inventory.ini in /opt/love2automate-ada/");
            Console.WriteLine("Example content:");
            Console.WriteLine("[cardano_nodes]");
            Console.WriteLine("localhost ansible_connection=local");
            return 1;
        }
        
        // Check if inventory file has content
        var inventoryContent = await File.ReadAllTextAsync(inventoryPath);
        if (string.IsNullOrWhiteSpace(inventoryContent) || inventoryContent.Trim().StartsWith("#"))
        {
            Console.WriteLine("✗ EMPTY");
            Console.WriteLine();
            Console.WriteLine("Inventory file exists but appears to be empty or only contains comments.");
            Console.WriteLine("Please configure inventory.ini in /opt/love2automate-ada/");
            Console.WriteLine("Example content:");
            Console.WriteLine("[cardano_nodes]");
            Console.WriteLine("localhost ansible_connection=local");
            return 1;
        }
        Console.WriteLine("✓ CONFIGURED");

        Console.WriteLine("✓ All prerequisites satisfied!");
        Console.WriteLine();
        return 0;
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

        // Always use --ask-become-pass for safety since playbooks require sudo
        string arguments = $"-i {inventoryPath} -e @{paramPath} --ask-become-pass {playbookPath}";
        Console.WriteLine("Note: This playbook requires sudo privileges. You will be prompted for your password.");
        Console.WriteLine($"Executing: ansible-playbook {arguments}");

        var result = await ExecuteCommandInteractive("ansible-playbook", arguments);
        
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

    private static async Task<(int ExitCode, string Output, string Error)> ExecuteCommand(string command, string arguments, bool suppressOutput = false)
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
                    if (!suppressOutput)
                        Console.WriteLine(e.Data);
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (!suppressOutput)
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
            if (!suppressOutput)
                Console.WriteLine($"Error executing command '{command} {arguments}': {ex.Message}");
            return (1, "", ex.Message);
        }
    }

    private static async Task<(int ExitCode, string Output, string Error)> ExecuteCommandInteractive(string command, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            RedirectStandardInput = false
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        try
        {
            process.Start();
            await process.WaitForExitAsync();

            return (process.ExitCode, "", "");
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

    private static async Task<string> PrepareParameterFile(string baseParameterFile, int? port = null)
    {
        var projectRoot = GetProjectRoot();
        var baseParamPath = Path.Combine(projectRoot, baseParameterFile);

        if (!File.Exists(baseParamPath))
        {
            throw new FileNotFoundException($"Base parameter file not found: {baseParamPath}");
        }

        // If no custom parameters are specified, use the original file
        if (!port.HasValue)
        {
            return baseParameterFile;
        }

        // Validate port if specified
        if (port.HasValue && (port.Value < 1 || port.Value > 65535))
        {
            throw new ArgumentException("Port must be between 1 and 65535");
        }

        // Create custom parameter file in a temporary directory that's user-writable
        var tempDir = Path.GetTempPath();
        var customParamFile = $"install_param_custom_{DateTime.Now:yyyyMMdd_HHmmss}.yml";
        var customParamPath = Path.Combine(tempDir, customParamFile);

        Console.WriteLine($"Creating custom parameter file: {customParamPath}");

        try
        {
            var content = await File.ReadAllTextAsync(baseParamPath);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (port.HasValue && lines[i].TrimStart().StartsWith("cardano_port:"))
                {
                    var indentation = lines[i].Substring(0, lines[i].Length - lines[i].TrimStart().Length);
                    lines[i] = $"{indentation}cardano_port: {port.Value}";
                    Console.WriteLine($"✓ Updated port to {port.Value}");
                }
            }

            await File.WriteAllTextAsync(customParamPath, string.Join('\n', lines));
            Console.WriteLine($"✓ Custom parameter file created: {customParamPath}");
            
            return customParamPath; // Return full path since it's now in temp directory
        }
        catch (Exception ex)
        {
            // Clean up the file if creation failed
            if (File.Exists(customParamPath))
            {
                try { File.Delete(customParamPath); } catch { }
            }
            throw new Exception($"Failed to create custom parameter file: {ex.Message}");
        }
    }

    private static async Task StoreInstallationConfig(int? port)
    {
        try
        {
            var homeDir = Environment.GetEnvironmentVariable("HOME") ?? "/root";
            var configDir = Path.Combine(homeDir, ".love2automate-ada");
            var configFile = Path.Combine(configDir, "config.json");

            // Create directory if it doesn't exist
            Directory.CreateDirectory(configDir);

            // Determine the actual port used
            int actualPort = port ?? 6002; // Default to 6002 if no custom port was specified

            var config = new
            {
                cardano_port = actualPort,
                last_installation = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(configFile, jsonContent);
            Console.WriteLine($"✓ Configuration saved (port: {actualPort})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Warning: Could not save configuration: {ex.Message}");
        }
    }

    private static async Task<int> GetConfiguredPort()
    {
        try
        {
            var homeDir = Environment.GetEnvironmentVariable("HOME") ?? "/root";
            var configFile = Path.Combine(homeDir, ".love2automate-ada", "config.json");

            if (!File.Exists(configFile))
            {
                // Fall back to reading from the default parameter file
                return await GetPortFromParameterFile();
            }

            var jsonContent = await File.ReadAllTextAsync(configFile);
            using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
            
            if (doc.RootElement.TryGetProperty("cardano_port", out var portElement))
            {
                return portElement.GetInt32();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Warning: Could not read stored configuration: {ex.Message}");
        }

        // Fall back to reading from parameter file
        return await GetPortFromParameterFile();
    }

    private static async Task<int> GetPortFromParameterFile()
    {
        try
        {
            var projectRoot = GetProjectRoot();
            var paramPath = Path.Combine(projectRoot, "install_param.yml");

            if (!File.Exists(paramPath))
            {
                return 6002; // Default fallback
            }

            var content = await File.ReadAllTextAsync(paramPath);
            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("cardano_port:"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int port))
                    {
                        return port;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Warning: Could not read parameter file: {ex.Message}");
        }

        return 6002; // Default fallback
    }
}