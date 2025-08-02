#!/usr/bin/env bash
set -e

# Use first argument as Cardano node version, or default to 10.5.1 if not provided
CARDANO_NODE_VERSION="${1:-10.5.1}"

echo "==> Cardano node version: $CARDANO_NODE_VERSION"
echo

# Create temporary files to cache downloaded flake.lock files
NODE_FLAKE_TMP=$(mktemp)
IOHKNIX_FLAKE_TMP=$(mktemp)

# Clean up temp files on exit
cleanup() {
  rm -f "$NODE_FLAKE_TMP" "$IOHKNIX_FLAKE_TMP"
}
trap cleanup EXIT

# Download Cardano node flake.lock
curl -s -o "$NODE_FLAKE_TMP" "https://raw.githubusercontent.com/IntersectMBO/cardano-node/$CARDANO_NODE_VERSION/flake.lock"

# Extract iohk-nix version commit hash from cached node flake.lock
IOHKNIX_VERSION=$(jq -r '.nodes.iohkNix.locked.rev' "$NODE_FLAKE_TMP")

# Download iohk-nix flake.lock using cached iohk-nix version
curl -s -o "$IOHKNIX_FLAKE_TMP" "https://raw.githubusercontent.com/input-output-hk/iohk-nix/$IOHKNIX_VERSION/flake.lock"

SODIUM_VERSION=$(jq -r '.nodes.sodium.original.rev' "$IOHKNIX_FLAKE_TMP") # Extract libsodium version commit hash from cached iohk-nix flake.lock
SECP256K1_VERSION=$(jq -r '.nodes.secp256k1.original.ref' "$IOHKNIX_FLAKE_TMP") # Extract secp256k1 version tag from cached iohk-nix flake.lock
BLST_VERSION=$(jq -r '.nodes.blst.original.ref' "$IOHKNIX_FLAKE_TMP") # Extract blst version ref from cached iohk-nix flake.lock

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
    if [[ -z "$CABAL_VERSION" ]]; then
      CABAL_VERSION="${BASH_REMATCH[1]}"
    else
      CABAL_VERSION="$CABAL_VERSION ${BASH_REMATCH[1]}"
    fi
  fi
done

# echo "==> Dependencies versions"
# cat <<EOF
# cardano-node:   $CARDANO_NODE_VERSION
# iohk-nix:       $IOHKNIX_VERSION
# libsodium:      $SODIUM_VERSION
# secp256k1:      $SECP256K1_VERSION
# blst:           $BLST_VERSION
# GHC:            $GHC_VERSION
# Cabal:          $CABAL_VERSION
# EOF

# Write JSON output to /tmp/cardano_node_${CARDANO_NODE_VERSION}_deps_version.json
JSON_FILE="/tmp/cardano_node_${CARDANO_NODE_VERSION}_deps_version.json"

jq -n \
  --arg cardano_node "$CARDANO_NODE_VERSION" \
  --arg iohk_nix "$IOHKNIX_VERSION" \
  --arg libsodium "$SODIUM_VERSION" \
  --arg secp256k1 "$SECP256K1_VERSION" \
  --arg blst "$BLST_VERSION" \
  --arg ghc "$GHC_VERSION" \
  --arg cabal "$CABAL_VERSION" \
  '{
    "cardano-node": $cardano_node,
    "iohk-nix": $iohk_nix,
    "libsodium": $libsodium,
    "secp256k1": $secp256k1,
    "blst": $blst,
    "ghc": $ghc,
    "cabal": $cabal
  }' > "$JSON_FILE"
echo "Dependency versions written to: $JSON_FILE"