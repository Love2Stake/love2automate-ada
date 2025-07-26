---
id: uninstall-cardano-node
title: Uninstalling Cardano Node
sidebar_label: Uninstall Cardano Node
description: Instructions for uninstalling the Cardano Node using Ansible playbooks
---

# Uninstalling Cardano Node

This guide explains how to uninstall the Cardano Node and all its components using the provided Ansible playbooks.

## Prerequisites

Before running the uninstall process, ensure you have:

- Ansible installed on your system
- Sudo privileges for removing system files
- Access to the love2automate-ada repository

## Uninstall Process

To uninstall the Cardano Node and all related components, navigate to the playbook directory and run:

```bash
cd $HOME/git/love2automate-ada
ansible-playbook Uninstall.yml -i inventory.ini -c local --ask-become-pass
```

### Command Breakdown

- `ansible-playbook Uninstall.yml`: Runs the main uninstall playbook
- `-i inventory.ini`: Specifies the inventory file
- `-c local`: Runs the playbook locally instead of over SSH
- `--ask-become-pass`: Prompts for sudo password when needed

## Expected Output

When you run the uninstall command, you'll see output similar to the following:

```
BECOME password: 

PLAY [Remove_Cardano_Node] *************************************************************

TASK [Gathering Facts] *****************************************************************
ok: [localhost]

TASK [Remove Cardano node binary from /usr/local/bin] **********************************
ok: [localhost]

TASK [Remove Cardano CLI binary from /usr/local/bin] ***********************************
ok: [localhost]

TASK [Check if extracted cardano-node binary archive exists] ***************************
ok: [localhost]

TASK [Remove extracted cardano-node binary archive] ************************************
skipping: [localhost]

TASK [Remove cardano-binary directory] *************************************************
ok: [localhost]

TASK [Check if cardano node directory exists] ******************************************
ok: [localhost]

TASK [Remove cardano node directory] ***************************************************
ok: [localhost]

TASK [Remove cloned cardano-node git repo] *********************************************
ok: [localhost]

PLAY [Remove Cardano node install (Steps 1–7)] *****************************************

TASK [Gathering Facts] *****************************************************************
ok: [localhost]

TASK [Remove Cardano environment variables from .bashrc] *******************************
ok: [localhost]

TASK [Remove GHCup and installed binaries] *********************************************
changed: [localhost]

TASK [Remove installed libsodium (requires root)] **************************************
ok: [localhost]

TASK [Remove installed secp256k1 (requires root)] **************************************
ok: [localhost]

TASK [Remove installed blst (requires root)] *******************************************
ok: [localhost]

TASK [Remove blst pkgconfig (requires root)] *******************************************
ok: [localhost]

TASK [Remove blst header (requires root)] **********************************************
ok: [localhost]

TASK [Remove blst hpp (requires root)] *************************************************
ok: [localhost]

TASK [Remove blst aux header (requires root)] ******************************************
ok: [localhost]

PLAY [Remove Chrony and related files on Ubuntu] ***************************************

TASK [Gathering Facts] *****************************************************************
ok: [localhost]

TASK [Stop chrony service if running] **************************************************
changed: [localhost]

TASK [Uninstall chrony package] ********************************************************
changed: [localhost]

TASK [Remove chrony configuration directory] *******************************************
ok: [localhost]

TASK [Remove chrony log directory] *****************************************************
ok: [localhost]

TASK [Remove chrony drift file] ********************************************************
ok: [localhost]

TASK [Display message] *****************************************************************
ok: [localhost] => {
    "msg": "Chrony and all related files have been removed."
}

PLAY RECAP *****************************************************************************
localhost                  : ok=25   changed=3    unreachable=0    failed=0    skipped=1    rescued=0    ignored=0
```

## What Gets Removed

The uninstall process removes the following components:

1. **Cardano Node Binaries**:
   - Cardano node binary from `/usr/local/bin`
   - Cardano CLI binary from `/usr/local/bin`
   - Cardano node directories and git repositories

2. **Development Tools**:
   - GHCup and installed binaries
   - libsodium library
   - secp256k1 library
   - blst library and headers

3. **System Components**:
   - Chrony service and all related configuration files

## Troubleshooting

### SSH Connection Issues

If you encounter SSH connection issues like:
```
fatal: [localhost]: UNREACHABLE! => {"changed": false, "msg": "Failed to connect to the host via ssh: danu@localhost: Permission denied (publickey,password).", "unreachable": true}
```

Make sure to use the `-c local` flag to run the playbook locally.

### Permission Denied Errors

If you see permission errors like:
```
fatal: [localhost]: FAILED! => {"changed": false, "module_stderr": "sudo: a password is required\n", "module_stdout": "", "msg": "MODULE FAILURE\nSee stdout/stderr for the exact error", "rc": 1}
```

Ensure you're using the `--ask-become-pass` flag and provide your sudo password when prompted.
