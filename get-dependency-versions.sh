#!/usr/bin/env bash
set -e

# Set Cardano node version (edit this variable for a different version)
CARDANO_NODE_VERSION="10.5.1"

echo "==> Cardano node version: $CARDANO_NODE_VERSION"
echo

# Get the iohk-nix version commit hash used by cardano-node
IOHKNIX_VERSION=$(curl -s "https://raw.githubusercontent.com/IntersectMBO/cardano-node/$CARDANO_NODE_VERSION/flake.lock" | jq -r '.nodes.iohkNix.locked.rev')
echo "iohk-nix version: $IOHKNIX_VERSION"

# Get libsodium version commit hash
SODIUM_VERSION=$(curl -s "https://raw.githubusercontent.com/input-output-hk/iohk-nix/$IOHKNIX_VERSION/flake.lock" | jq -r '.nodes.sodium.original.rev')
echo "libsodium version: $SODIUM_VERSION"

# Get secp256k1 version tag
SECP256K1_VERSION=$(curl -s "https://raw.githubusercontent.com/input-output-hk/iohk-nix/$IOHKNIX_VERSION/flake.lock" | jq -r '.nodes.secp256k1.original.ref')
echo "secp256k1 version: $SECP256K1_VERSION"

# Get blst version ref
BLST_VERSION=$(curl -s "https://raw.githubusercontent.com/input-output-hk/iohk-nix/$IOHKNIX_VERSION/flake.lock" | jq -r '.nodes.blst.original.ref')
echo "blst version: $BLST_VERSION"
echo

# Get GHC and Cabal versions from release notes and store in variables
readarray -t COMPILER_VERSIONS < <(
  curl -s "https://api.github.com/repos/IntersectMBO/cardano-node/releases/tags/$CARDANO_NODE_VERSION" \
    | jq -r '.body' \
    | grep -i -E "ghc|cabal" \
    | sed -nE 's/.*(ghc)[^0-9]*([0-9]+\.[0-9]+(\.[0-9]+)?).*/GHC: \2/I p; s/.*(cabal)[^0-9]*([0-9]+(\.[0-9]+)?(\.[0-9]+)?).*/Cabal: \2/I p'
)

# Initialize variables
GHC_VERSION=""
CABAL_VERSION=""

# Assign the versions
for line in "${COMPILER_VERSIONS[@]}"; do
  if [[ "$line" =~ ^GHC:\ (.+)$ ]]; then
    GHC_VERSION="${BASH_REMATCH[1]}"
  elif [[ "$line" =~ ^Cabal:\ (.+)$ ]]; then
    # To handle multiple Cabal versions, concatenate with space or choose first only
    if [[ -z "$CABAL_VERSION" ]]; then
      CABAL_VERSION="${BASH_REMATCH[1]}"
    else
      CABAL_VERSION="$CABAL_VERSION ${BASH_REMATCH[1]}"
    fi
  fi
done

echo "==> Dependencies versions"
cat <<EOF
cardano-node:   $CARDANO_NODE_VERSION
iohk-nix:       $IOHKNIX_VERSION
libsodium:      $SODIUM_VERSION
secp256k1:      $SECP256K1_VERSION
blst:           $BLST_VERSION
GHC:            $GHC_VERSION
Cabal:          $CABAL_VERSION
EOF