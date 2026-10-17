---
name: teardown
description: Tear down everything this chat session created — Unity Editors, Blender processes, background tasks, dev servers, linked sessions, stash entries, the session's git worktree and its branch. Use when the user says "teardown", "clean up this session", "free up resources", or is done with a task.
argument-hint: "[--yes] [--keep-branch] [--keep-worktree]"
disable-model-invocation: true
---

# Teardown

Release every resource **this chat session** owns, then remove its worktree and branch. Scope is the current session only — never touch another session's worktree, Editor or process, and never touch anything outside this repo unless this chat started it.

Arguments:
- `--yes` — skip the confirmation step (safety stops in step 2 still apply).
- `--keep-branch` — leave the local branch in place.
- `--keep-worktree` — clean up processes only; leave the worktree and branch.

## 1. Establish scope

```bash
WT=$(git rev-parse --show-toplevel)                                # this session's worktree
BRANCH=$(git branch --show-current)
MAIN=$(git worktree list --porcelain | awk 'NR==1{print $2}')      # primary checkout
git -C "$MAIN" branch --show-current                               # usually master
```

If `$WT == $MAIN`, this session is not in a worktree: **skip worktree and branch removal entirely** and only clean up processes/tasks.

Also note from this conversation: the scratchpad directory, any PIDs you launched, background Bash/Monitor task IDs, cron jobs, preview servers, terminal tabs, sessions you started, and stash entries you pushed.

## 2. Safety checks (always run, even with `--yes`)

```bash
git -C "$WT" status --porcelain                                    # uncommitted / untracked work
git -C "$WT" log --oneline "@{upstream}..HEAD" 2>/dev/null         # unpushed commits
git -C "$WT" rev-parse --abbrev-ref "@{upstream}" 2>/dev/null      # has upstream?
git -C "$MAIN" merge-base --is-ancestor "$BRANCH" master && echo merged
gh pr list --head "$BRANCH" --state all --json number,state,url    # PR state
```

**Stop and ask** before destroying anything if any of these hold:
- Uncommitted or untracked changes in `$WT` (show them).
- Commits not on any remote and not merged into master (show `git log`).
- An open PR for the branch — removing the worktree is fine, but say the PR stays open.

Ignored files (`Library/`, `Temp/`, `Logs/`, `UserSettings/`) are expected and are not a reason to stop.

## 3. Inventory and confirm

Gather everything below, then present one list grouped by type (PID, path/port, what it is). Unless `--yes` was passed, ask the user to confirm before step 4. Anything ambiguous (e.g. a GUI Blender you drove through MCP) is listed separately as "ask".

### Unity Editors

```bash
unity editors running --json --no-banner
```

Keep only instances whose `projectPath` is `$WT` or under it.

### Blender

```bash
pgrep -fl -i blender
```

Include only processes whose command line contains `$WT` or the scratchpad path, or whose PID you launched in this chat (e.g. `blender --background --python …`).
**Never** include `blender-mcp` server processes (they belong to the agent app, not this session). A GUI Blender that the user opened is theirs — list it under "ask", don't kill it.

### Other processes running inside the worktree

```bash
lsof -d cwd -Fpn 2>/dev/null \
  | awk -v wt="$WT" '/^p/{p=substr($0,2)} /^n/{n=substr($0,2); if (n==wt || index(n, wt"/")==1) print p, n}'
```

Catches dev servers, shader compilers, watchers etc. Exclude your own shell (`$$` and its parents).

### Session-level resources

- Background Bash / Monitor tasks started in this chat → `TaskStop`.
- Cron jobs / wakeups created in this chat → `CronList`, then `CronDelete` only the ones you created.
- Browser preview servers → `mcp__Claude_Browser__preview_list`, then `preview_stop` the ones you started.
- Terminal tabs you opened with `run_in_terminal` → `mcp__terminal__list_terminal_tabs`, then `stop_terminal_tab`.
- Sessions this chat started → `mcp__ccd_session_mgmt__list_sessions {"linked": true}`. Running ones go under "ask"; finished ones can be offered for archiving.
- Stash entries you pushed (tagged with `-m`) → find by tag in `git stash list --format='%gd %gs'` and `git stash drop <stash@{n}>`. Never drop untagged or other sessions' entries.

## 4. Tear down — in this order

### 4a. Background tasks, crons, previews, terminal tabs

Stop them with the tools listed above.

### 4b. Unity Editors

For each matching Editor (see the `unity-cli` skill):

```bash
unity command editor_stop --project-path "<projectPath>"   # leave Play mode if in it (ignore errors)
unity command quit        --project-path "<projectPath>"   # graceful quit
```

Do **not** `save_all` — the worktree is about to be removed; anything worth keeping should already be committed (step 2).

Wait up to ~30s for the PID to exit (`kill -0 <pid>` in a short loop, or a `Monitor` until-loop). If it is still alive, or the Editor was unreachable (`reachable: false`, Safe Mode), send `kill <pid>` (SIGTERM) and wait again. Only use `kill -9` after asking.

### 4c. Blender and other worktree processes

`kill <pid>` (SIGTERM), wait, verify with `kill -0`. Escalate to `-9` only after asking.

### 4d. Worktree (skip if `--keep-worktree` or not in a worktree)

Run from the primary checkout so the removal doesn't depend on the current directory:

```bash
git -C "$MAIN" worktree remove "$WT"
git -C "$MAIN" worktree prune
```

If it refuses because of untracked/modified files, show them and ask before re-running with `--force`. If it refuses because a process still holds files, go back to the `lsof` check.

### 4e. Branch (skip if `--keep-branch`, `--keep-worktree`, or not in a worktree)

```bash
git -C "$MAIN" branch -d "$BRANCH"
```

`-d` refuses unmerged branches. Squash-merged PRs show as unmerged — if `gh` reports the PR as `MERGED`, `-D` is safe; otherwise ask. Never delete `master`/`main` or the branch checked out in `$MAIN`.

**Remote branch:** deleting it (`git push origin --delete "$BRANCH"`) is outward-facing — only do it if the PR is merged **and** the user confirms.

## 5. Verify and report

```bash
unity editors running --json --no-banner | grep -F "$WT"     # expect nothing
pgrep -fl -i blender                                          # only non-session processes remain
git -C "$MAIN" worktree list | grep -F "$WT"                  # expect nothing
git -C "$MAIN" branch --list "$BRANCH"                        # expect nothing
```

Report what was removed, what was left and why (open PR, user said keep, unowned process). After the worktree is gone this session's shell sits in a deleted directory, so finish by offering to archive the session (`mcp__ccd_session_mgmt__archive_session`) rather than running more commands.
