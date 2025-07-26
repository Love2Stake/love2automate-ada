# love2automate-ada

Automated deployment and management of Cardano Node using Ansible playbooks.

## Overview

This repository contains Ansible playbooks for installing and uninstalling a Cardano Node on Ubuntu systems. The automation handles all the complex steps required for setting up a Cardano Node, including:

- System time synchronization with Chrony
- Installation of OS packages
- Setup of GHCup for Haskell development
- Building and installation of Cardano Node dependencies
- Configuration of environment variables
- Building or downloading Cardano Node binaries

## Documentation

- [Installing Cardano Node](docs/install-cardano-node.md) (Coming soon)
- [Uninstalling Cardano Node](docs/uninstall-cardano-node.md)

## Prerequisites

- Ubuntu system (20.04 or later recommended)
- Ansible installed
- Sudo privileges
- Internet connection

## Quick Start

### Installation

To install Cardano Node, run:

```bash
ansible-playbook Build.yml -i inventory.ini -c local --ask-become-pass
```

### Uninstallation

To uninstall Cardano Node and all related components, run:

```bash
ansible-playbook Uninstall.yml -i inventory.ini -c local --ask-become-pass
```

## Project Structure

```
.
├── Build.yml                 # Main installation playbook
├── Uninstall.yml             # Main uninstallation playbook
├── inventory.ini             # Ansible inventory file
├── install_param.yml         # Installation parameters
├── install-steps/            # Individual installation playbooks
├── uninstall-steps/          # Individual uninstallation playbooks
└── docs/                     # Documentation
```

## Support

For issues, questions, or contributions, please open an issue on the GitHub repository.