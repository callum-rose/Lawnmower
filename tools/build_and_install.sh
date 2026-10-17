#!/usr/bin/env bash
# Build Bunny Heaven for iOS and install it on the test iPhone.
#
#   tools/build_and_install.sh [--development] [--skip-unity] [--skip-xcode] [--no-install] [--no-launch]
#
# --development makes a Unity development player (Profiler-connectable, logs [Perf] FPS/memory lines).
#
# Builds the git worktree it is invoked from (the current directory), so the same
# script works from any worktree. Steps:
#   1. Unity iOS build -> Builds/iOS/Unity-iPhone.xcodeproj
#      (through the live editor if one has this project open, otherwise `unity build` in batch mode)
#   2. xcodebuild, automatic signing -> Builds/DerivedData/.../BunnyHeaven.app
#   3. devicectl install (retried; the phone drops connections)
#   4. devicectl launch (a launch failure is reported but does not fail the script)
#
# Env overrides: DEVICE_ID, TEAM_ID, BUNDLE_ID, INSTALL_TIMEOUT_MIN (default 30).

set -euo pipefail

DEVICE_ID="${DEVICE_ID:-00008120-000651EA2240201E}"
TEAM_ID="${TEAM_ID:-7R68U6FFLK}"
BUNDLE_ID="${BUNDLE_ID:-com.callumrose.bunnyheaven}"
INSTALL_TIMEOUT_MIN="${INSTALL_TIMEOUT_MIN:-30}"
BUILD_METHOD="Lawnmower.Editor.Build.IOSBuild.BuildIOS"

DEVELOPMENT=0; SKIP_UNITY=0; SKIP_XCODE=0; DO_INSTALL=1; DO_LAUNCH=1
for arg in "$@"; do
	case "$arg" in
		--development) DEVELOPMENT=1 ;;
		--skip-unity) SKIP_UNITY=1 ;;
		--skip-xcode) SKIP_XCODE=1 ;;
		--no-install) DO_INSTALL=0; DO_LAUNCH=0 ;;
		--no-launch) DO_LAUNCH=0 ;;
		-h|--help) sed -n '2,18p' "$0"; exit 0 ;;
		*) echo "Unknown argument: $arg" >&2; exit 2 ;;
	esac
done

log() { printf '\n==> %s\n' "$*"; }
die() { printf '\n!!! FAILED: %s\n' "$*" >&2; exit 1; }

PROJECT="$(git rev-parse --show-toplevel 2>/dev/null)" || die "run this from inside a Lawnmower worktree"
[[ -f "$PROJECT/ProjectSettings/ProjectVersion.txt" ]] || die "$PROJECT is not a Unity project"
cd "$PROJECT"

IOS_DIR="$PROJECT/Builds/iOS"
XCODEPROJ="$IOS_DIR/Unity-iPhone.xcodeproj"
DERIVED="$PROJECT/Builds/DerivedData"
LOG_DIR="$PROJECT/Builds/logs"
STAMP="$(date +%Y%m%d-%H%M%S)"
mkdir -p "$LOG_DIR"

echo "Project: $PROJECT ($(git rev-parse --abbrev-ref HEAD) @ $(git rev-parse --short HEAD))"

# --- 1. Unity ------------------------------------------------------------------
live_editor_ready() {
	unity status --project-path "$PROJECT" --json 2>/dev/null \
		| python3 -c 'import json,sys
d=json.load(sys.stdin)
sys.exit(0 if any(i.get("state")=="ready" for i in (d.get("data") or {}).get("instances",[])) else 1)'
}

if [[ $SKIP_UNITY -eq 0 ]]; then
	command -v unity >/dev/null || die "Unity CLI ('unity') not on PATH"
	if live_editor_ready; then
		[[ $DEVELOPMENT -eq 0 ]] || die "--development needs a batch-mode build; close the editor on this project first"
		log "Unity iOS build via the live editor"
		# Pick up any on-disk changes (the editor may be unfocused) and wait for the compile.
		unity command --project-path "$PROJECT" --result-only eval \
			'UnityEditor.AssetDatabase.Refresh(); return "refreshed";' >/dev/null || true
		for _ in $(seq 1 120); do
			sleep 5
			state="$(unity command --project-path "$PROJECT" --result-only eval \
				'return (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) ? "busy" : (UnityEditor.EditorUtility.scriptCompilationFailed ? "failed" : "idle");' 2>/dev/null || echo busy)"
			[[ "$state" == *'"idle"'* ]] && break
			[[ "$state" == *'"failed"'* ]] && die "scripts do not compile in the live editor"
		done
		STATUS_REL="Builds/logs/unity-ios-status-$STAMP.txt"
		STATUS_FILE="$PROJECT/$STATUS_REL"
		unity command --project-path "$PROJECT" --result-only eval \
			"return ${BUILD_METHOD}Deferred(\"$STATUS_REL\");" \
			|| die "could not start the iOS build in the live editor (Safe Mode? try: unity pipeline list)"
		[[ -f "$STATUS_FILE" ]] || die "live editor did not schedule the iOS build"
		# The first iOS build in an editor switches platform and reimports, which can take a while.
		deadline=$(( $(date +%s) + 90 * 60 ))
		while [[ "$(cat "$STATUS_FILE")" == "running" ]]; do
			(( $(date +%s) < deadline )) || die "iOS build still running after 90 min"
			sleep 15
		done
		cat "$STATUS_FILE"; echo
		grep -q '\[IOSBuild\] Succeeded' "$STATUS_FILE" || die "Unity iOS build failed in the live editor (see its console)"
	else
		log "Unity iOS build in batch mode$([[ $DEVELOPMENT -eq 1 ]] && echo ' (development)')"
		DEV_ARGS=()
		[[ $DEVELOPMENT -eq 1 ]] && DEV_ARGS=(--args "-developmentBuild")
		UNITY_LOG="$LOG_DIR/unity-ios-$STAMP.log"
		unity build "$PROJECT" --target iOS --execute-method "$BUILD_METHOD" \
			--output-path "$IOS_DIR" --log-file "$UNITY_LOG" --no-tail --non-interactive ${DEV_ARGS[@]+"${DEV_ARGS[@]}"} \
			|| die "Unity iOS build failed; log: $UNITY_LOG"
		grep -q '\[IOSBuild\] Succeeded' "$UNITY_LOG" || die "Unity build exited 0 but did not report success; log: $UNITY_LOG"
	fi
fi
[[ -d "$XCODEPROJ" ]] || die "no Xcode project at $XCODEPROJ"

# --- 2. Xcode ------------------------------------------------------------------
if [[ $SKIP_XCODE -eq 0 ]]; then
	log "xcodebuild (Release, automatic signing, team $TEAM_ID)"
	XC_LOG="$LOG_DIR/xcodebuild-$STAMP.log"
	if ! xcodebuild -project "$XCODEPROJ" -scheme Unity-iPhone -configuration Release \
		-destination 'generic/platform=iOS' -allowProvisioningUpdates \
		DEVELOPMENT_TEAM="$TEAM_ID" CODE_SIGN_STYLE=Automatic \
		-derivedDataPath "$DERIVED" build >"$XC_LOG" 2>&1; then
		grep -E 'error:|Signing|provisioning|requires a development team' "$XC_LOG" | tail -20 >&2 || true
		die "xcodebuild failed; log: $XC_LOG"
	fi
fi

APP="$(find "$DERIVED/Build/Products/Release-iphoneos" -maxdepth 1 -name '*.app' -type d 2>/dev/null | head -1)"
[[ -n "$APP" && -d "$APP" ]] || die "no signed .app under $DERIVED/Build/Products/Release-iphoneos"
echo "APP: $APP"

[[ $DO_INSTALL -eq 1 ]] || { log "Done (install skipped). APP: $APP"; exit 0; }

# --- 3. Install (retry with backoff; the phone often drops the first connection) ---
log "Installing on $DEVICE_ID (retrying for up to $INSTALL_TIMEOUT_MIN min)"
deadline=$(( $(date +%s) + INSTALL_TIMEOUT_MIN * 60 ))
attempt=0
until xcrun devicectl device install app --device "$DEVICE_ID" "$APP"; do
	attempt=$((attempt + 1))
	if (( attempt >= 5 && $(date +%s) >= deadline )); then
		die "install failed after $attempt attempts. Retry later with:
  xcrun devicectl device install app --device $DEVICE_ID \"$APP\""
	fi
	delay=$(( 30 + (attempt % 4) * 10 ))
	echo "Install attempt $attempt failed; retrying in ${delay}s"
	sleep "$delay"
done
log "Installed $BUNDLE_ID"

# --- 4. Launch (best effort) ---------------------------------------------------
if [[ $DO_LAUNCH -eq 1 ]]; then
	launched=0
	for i in 1 2 3 4 5; do
		if xcrun devicectl device process launch --device "$DEVICE_ID" "$BUNDLE_ID"; then
			launched=1; break
		fi
		(( i < 5 )) || break
		echo "Launch attempt $i failed; retrying in 30s"
		sleep 30
	done
	[[ $launched -eq 1 ]] || echo "WARNING: installed but could not launch (the phone may be locked)."
fi

log "Done. APP: $APP"
