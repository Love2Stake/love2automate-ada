---
id: install-ansible
title: Installing Ansible
sidebar_label: Install Ansible
description: Simple instructions for installing Ansible on Ubuntu
---

# Installing Ansible

This guide explains how to install Ansible on Ubuntu systems in the simplest way possible.

## Prerequisites

- Ubuntu system (20.04 or later recommended)
- Internet connection
- Sudo privileges

## Installation Steps

1. Update your system package list:
   ```bash
   sudo apt update
   ```

2. Install Ansible using apt:
   ```bash
   sudo apt install ansible
   ```

3. Verify the installation:
   ```bash
   ansible --version
   ```

## That's It!

You now have Ansible installed and ready to use for running the Cardano Node installation playbooks.

## Alternative Installation Methods

If you prefer other installation methods, you can also install Ansible using pip:

```bash
# Install pip if you don't have it
sudo apt install python3-pip

# Install Ansible using pip
pip3 install ansible
```

Or using the official Ansible PPA for the latest version:

```bash
sudo apt update
sudo apt install software-properties-common
sudo add-apt-repository --yes --update ppa:ansible/ansible
sudo apt install ansible
```

## Next Steps

Once Ansible is installed, you can proceed with:
- [Installing Cardano Node](install-cardano-node.md)
- [Uninstalling Cardano Node](uninstall-cardano-node.md)
