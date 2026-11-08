#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/../.." && pwd)"
[ -x /opt/homebrew/bin/dotnet ] && export PATH="/opt/homebrew/bin:$PATH"

tags="${1:?usage: publish.sh <tag>[;<tag>…] [registry]   (no registry: the local Docker daemon)}"
registry="${2:-}"

for host in Api Gateway Migrator Website; do
  echo "== $host"
  dotnet publish "$root/src/Hosts/GSBC.ChurchApp.Hosts.$host" -c Release -t:PublishContainer -nologo \
    -p:ContainerRuntimeIdentifier=linux-arm64 \
    -p:ContainerImageTags="\"$tags\"" \
    ${registry:+-p:ContainerRegistry="$registry"}
done
