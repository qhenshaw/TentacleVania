#!/usr/bin/env bash
# Shared helpers for the Tools/UnityCheck scripts. Each script sources this, which moves to the project root and
# provides the Unity CLI call plus a little sed/awk JSON reading, since jq isn't available here.

cd "$(dirname "${BASH_SOURCE[0]}")/../.."
source Tools/project.sh

Unity=(unity command --caller plugin --skill unity-cli --result-only)

# a JSON string value on one pretty-printed line, unescaped
json_string() { sed -n "s/^ *\"$1\": \"\(.*\)\",\?$/\1/p" | head -1 | sed 's/\\r//g; s/\\n/\n/g;s/\\"/"/g; s/\\\\/\\/g'; }

# a JSON number or bool on one pretty-printed line
json_number() { sed -n "s/^ *\"$1\": \([-0-9.a-z]*\),\?$/\1/p" | head -1; }

# runs a C# eval and prints its result (string results unescaped); on failure the CLI's error goes to stderr
eval_result() {
    local out
    out=$("${Unity[@]}" eval "$1" 2>&1)
    # just after a domain reload an eval can compile against the old assemblies and miss new types, so retry once
    if grep -q 'does not exist in the current context' <<<"$out"; then
        sleep 3
        out=$("${Unity[@]}" eval "$1" 2>&1)
    fi
    if ! grep -q '"success": true' <<<"$out"; then echo "$out" >&2; return 1; fi
    if grep -q '"result": "' <<<"$out"; then json_string result <<<"$out"; else json_number result <<<"$out"; fi
}

# polls until Unity has finished compiling and importing; calls fail during a domain reload, so failure counts as busy
wait_idle() {
    local timeout="${1:-180}" start=$SECONDS
    until [[ "$(eval_result 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating;' 2>/dev/null)" == false ]]; do
        if (( SECONDS - start > timeout )); then
            echo "Timed out after ${timeout}s waiting for Unity to finish compiling."
            return 1
        fi
        sleep 2
    done
}

# reads `console` JSON on stdin and prints one line per distinct message, in first-seen order, as
# "(xN) message  (first Assets/ frame)". It drops the CLI's own "Failed to handle /api/..." timeouts, which are noise
# from polling while the main thread is busy.
summarize_console() {
    awk '
        function unescape(s) { gsub(/\\"/, "\"", s); gsub(/\\\\/, "\\", s); return s }
        /^ *"message": "Failed to handle \/api\// { skip = 1; next }
        /^ *"message": "/    { skip = 0; m = $0; sub(/^ *"message": "/, "", m); sub(/",?$/, "", m)
                               sub(/\\n.*/, "", m); msg = unescape(substr(m, 1, 300)) }
        /^ *"stackTrace": "/ && skip { next }
        /^ *"stackTrace": "/ { at = ""
                               if (match($0, /\(at Assets\/[^)]*\)/)) at = "  " substr($0, RSTART, RLENGTH)
                               key = msg at
                               if (!(key in count)) order[++n] = key
                               count[key]++ }
        END { for (i = 1; i <= n; i++) printf "(x%d) %s\n", count[order[i]], order[i] }
    '
}

# prints the summarized console entries of one level since a cursor
console_since() {
    local level="$1" since="$2" session="${3:-}"
    local args=(console get --level "$level" --since "$since")
    [[ -n "$session" ]] && args+=(--since_session "$session")
    "${Unity[@]}" "${args[@]}" 2>/dev/null | summarize_console
}

# prints "cursor session" for the console buffer right now
console_cursor() {
    local s
    s=$("${Unity[@]}" console status 2>/dev/null) || return 1
    echo "$(json_number cursor <<<"$s") $(json_string session <<<"$s")"
}
