---
id: install-cardano-node
title: Installing Cardano Node
sidebar_label: Install Cardano Node
description: Instructions for installing the Cardano Node using Ansible playbooks
---

# Installing Cardano Node

This guide explains how to install the Cardano Node and all its components using the provided Ansible playbooks.

## Prerequisites

Before running the installation process, ensure you have:

- Ubuntu system (20.04 or later recommended)
- Ansible installed on your system
- Sudo privileges for installing system packages
- Access to the love2automate-ada repository

## Installation Process

To install the Cardano Node and all related components, run the following command from the repository root:

```bash
ansible-playbook Build.yml -i inventory.ini -c local --ask-become-pass
```

### Command Breakdown

- `ansible-playbook Build.yml`: Runs the main installation playbook
- `-i inventory.ini`: Specifies the inventory file
- `-c local`: Runs the playbook locally instead of over SSH
- `--ask-become-pass`: Prompts for sudo password when needed

## Configuration

Before running the installation, you may want to customize the `install_param.yml` file to match your requirements:

- Set the appropriate versions for GHC, Cabal, and Cardano Node
- Adjust paths and settings as needed for your environment

## What Gets Installed

The installation process sets up the following components:

1. **System Components**:
   - Chrony for time synchronization
   - Required OS packages

2. **Development Tools**:
   - GHCup for Haskell development
   - libsodium cryptographic library
   - secp256k1 cryptographic library
   - blst cryptographic library

3. **Cardano Node**:
   - Cardano Node binaries (built from source or downloaded)
   - Environment configuration

## Troubleshooting

If you encounter any issues during installation, please refer to the specific error messages and check:

1. All prerequisites are met
2. Sudo password is correctly provided when prompted
3. Internet connectivity is available for package downloads
4. Sufficient disk space is available for the build process
