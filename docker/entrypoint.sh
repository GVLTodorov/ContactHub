#!/usr/bin/env bash
# Runs nginx (the UI) and the API side by side. If either one exits, stop the other and exit
# with its status, so the container restarts instead of half-running.
set -uo pipefail

nginx -e stderr -g 'daemon off;' &
dotnet /app/ContactHub.Api.dll &

trap 'kill -TERM $(jobs -p) 2>/dev/null' TERM INT

wait -n
status=$?
kill -TERM $(jobs -p) 2>/dev/null
wait
exit "$status"
