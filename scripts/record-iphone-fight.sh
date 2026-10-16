#!/bin/sh
# Record only the selected iPhone process. No build/install/quality changes.
set -eu
if [ "$#" -lt 3 ] || [ "$#" -gt 4 ]; then
  echo 'Usage: record-iphone-fight.sh DEVICE_UDID PROCESS_NAME_OR_PID NEW_OUTPUT.trace [game|power]' >&2
  exit 2
fi
diag_device=$1
diag_process=$2
diag_output=$3
diag_mode=${4:-game}
if [ -z "$diag_device" ] || [ -z "$diag_process" ]; then
  echo 'Device and process must be explicit and nonempty' >&2
  exit 2
fi
case "$diag_mode" in
  game) diag_template='Game Performance' ;;
  power) diag_template='Power Profiler' ;;
  *) echo 'Mode must be game or power' >&2; exit 2 ;;
esac
case "$diag_output" in
  /*.trace) ;;
  *) echo 'Output must be an absolute path ending in .trace' >&2; exit 2 ;;
esac
if [ -e "$diag_output" ]; then
  echo 'Refusing to overwrite an existing trace' >&2
  exit 2
fi
exec xcrun xctrace record --template "$diag_template" --device "$diag_device" --attach "$diag_process" --time-limit 10m --output "$diag_output"
