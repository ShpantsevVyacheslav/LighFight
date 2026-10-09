#!/bin/sh
set -eu
project_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
if command -v dotnet >/dev/null 2>&1; then
  dotnet_command=dotnet
elif [ -x "$project_root/.tools/dotnet/dotnet" ]; then
  dotnet_command="$project_root/.tools/dotnet/dotnet"
else
  echo 'Install .NET SDK 8 or run Unity EditMode tests.' >&2
  exit 1
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
exec "$dotnet_command" run --project "$project_root/verification/Combat/Combat.csproj" -- --noresult
