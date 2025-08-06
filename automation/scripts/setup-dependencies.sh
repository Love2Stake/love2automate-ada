#!/bin/bash

# setup-dependencies.sh
# Script to set up dependencies for love2automate-ada
# This installs Ansible and required collections

set -e  # Exit on any error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠️${NC} $1"
}

print_info() {
    echo "$1"
}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to check if a file contains a string
file_contains() {
    grep -q "$1" "$2" 2>/dev/null
}

# Function to find ansible-galaxy binary
find_ansible_galaxy() {
    local paths=(
        "ansible-galaxy"                    # If in PATH
        "$HOME/.local/bin/ansible-galaxy"   # pipx/pip user install
        "/usr/bin/ansible-galaxy"           # system install
    )
    
    for path in "${paths[@]}"; do
        if command_exists "$path" || [ -f "$path" ]; then
            echo "$path"
            return 0
        fi
    done
    
    return 1
}

main() {
    print_info "Setting up dependencies for love2automate-ada..."
    print_info "This will install Ansible and required collections."
    print_info ""

    # Update package index
    print_info "Updating package index..."
    if sudo apt update >/dev/null 2>&1; then
        print_success "Package index updated"
    else
        print_error "Failed to update package index"
        exit 1
    fi

    # Install required packages including pipx for modern Python package management
    print_info "Installing required packages..."
    if sudo apt install -y python3-pip python3-venv pipx >/dev/null 2>&1; then
        print_success "Required packages installed"
    else
        print_error "Failed to install required packages"
        exit 1
    fi

    # Install ansible-core using pipx (includes ansible-playbook and ansible-galaxy)
    print_info "Installing Ansible Core using pipx..."
    if pipx install ansible-core >/dev/null 2>&1; then
        print_success "Ansible Core installed"
    else
        print_info "Pipx installation failed, trying with pip3 --user --break-system-packages..."
        if pip3 install --user --break-system-packages ansible-core >/dev/null 2>&1; then
            print_success "Ansible Core installed (fallback method)"
        else
            print_error "Failed to install ansible-core with both methods"
            exit 1
        fi
    fi

    # Also install the full ansible package for additional modules
    print_info "Installing full Ansible package..."
    if pipx install ansible >/dev/null 2>&1; then
        print_success "Full Ansible package installed"
    else
        print_info "Full ansible package installation failed, continuing with core only..."
    fi

    # Ensure pipx bin directory is in PATH
    print_info "Ensuring pipx is properly configured..."
    pipx ensurepath >/dev/null 2>&1 || true

    # Add to PATH in .bashrc if not already present
    print_info "Updating PATH in .bashrc..."
    BASHRC_PATH="$HOME/.bashrc"
    PATH_EXPORT='export PATH="$HOME/.local/bin:$PATH"'
    
    if [ -f "$BASHRC_PATH" ]; then
        if ! file_contains "\$HOME/.local/bin" "$BASHRC_PATH"; then
            echo "" >> "$BASHRC_PATH"
            echo "# Added by love2automate-ada setup" >> "$BASHRC_PATH"
            echo "$PATH_EXPORT" >> "$BASHRC_PATH"
            print_success "PATH updated in .bashrc"
        else
            print_success "PATH already configured in .bashrc"
        fi
    else
        print_warning ".bashrc not found, creating one..."
        echo "$PATH_EXPORT" > "$BASHRC_PATH"
        print_success "Created .bashrc with PATH configuration"
    fi

    # Install Ansible collections
    print_info "Installing Ansible collections..."
    
    # Try to find ansible-galaxy in common locations
    if ANSIBLE_GALAXY=$(find_ansible_galaxy); then
        print_info "Found ansible-galaxy at: $ANSIBLE_GALAXY"
    else
        print_error "Could not find ansible-galaxy command"
        exit 1
    fi
    
    # Install community.general collection
    print_info "Installing community.general collection..."
    if "$ANSIBLE_GALAXY" collection install community.general >/dev/null 2>&1; then
        print_success "community.general collection installed"
    else
        print_error "Failed to install community.general collection"
        exit 1
    fi

    # Install ansible.posix collection
    print_info "Installing ansible.posix collection..."
    if "$ANSIBLE_GALAXY" collection install ansible.posix >/dev/null 2>&1; then
        print_success "ansible.posix collection installed"
    else
        print_error "Failed to install ansible.posix collection"
        exit 1
    fi

    print_info ""
    print_success "Dependencies setup completed successfully!"
    print_info ""
    print_warning "IMPORTANT: You MUST restart your terminal or run 'source ~/.bashrc' for PATH changes to take effect."
    print_info ""
    print_info "Next steps:"
    print_info "1. Restart your terminal (or run: source ~/.bashrc)"
    print_info "2. Run: love2automate-ada --setup"
    print_info "3. Configure your inventory.ini file"
    print_info "4. Run: love2automate-ada --install cardano-node"

    exit 0
}

# Handle script interruption
trap 'print_error "Script interrupted"; exit 1' INT TERM

# Run main function
main "$@"
