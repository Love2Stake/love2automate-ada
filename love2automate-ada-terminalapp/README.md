# Cardano Node Manager

A C# command-line application for managing Cardano node operations through Ansible playbooks.

## Features

- **Install**: Deploy Cardano node using Ansible playbooks
- **Uninstall**: Remove Cardano node installation
- **Upgrade**: Update Cardano node components
- **Status**: Check if Cardano node is running

## Prerequisites

- .NET 8.0 or later
- Ansible installed and configured
- Valid `inventory.ini` file in the project root

## Usage

### Build the application
```bash
cd CardanoNodeManager
dotnet build
dotnet run -- [command] [arguments]
```

### Commands

#### Install Cardano Node
```bash
dotnet run -- install cardano-node
```
Executes the `Build.yml` playbook with `install_param.yml` parameters.

#### Uninstall Cardano Node
```bash
dotnet run -- uninstall cardano-node
```
Executes the `Uninstall.yml` playbook with `uninstall-steps/uninstall_param.yml` parameters.

#### Upgrade Cardano Node
```bash
dotnet run -- upgrade cardano-node
```
Provides information about available upgrade steps.

#### Check Status
```bash
dotnet run -- status
```
Checks if the Cardano node process is running and if port 6002 is listening.

### Help
```bash
dotnet run -- --help
```

## Project Structure

The application automatically locates the project root by looking for `Build.yml` and uses the following files:
- `Build.yml` - Main installation playbook
- `Uninstall.yml` - Uninstallation playbook
- `install_param.yml` - Installation parameters
- `uninstall-steps/uninstall_param.yml` - Uninstallation parameters
- `inventory.ini` - Ansible inventory file

## Example Output

```bash
$ dotnet run -- install cardano-node
Installing cardano-node...
Executing: ansible-playbook -i inventory.ini -e @install_param.yml Build.yml
[Ansible playbook output...]
✓ Playbook executed successfully
```