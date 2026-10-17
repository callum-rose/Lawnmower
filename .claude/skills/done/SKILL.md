---
name: done
description: Finish this chat's work — commit what's left, open a PR, merge it into master, then run /teardown to release everything the session created. Use when the user says "done", "ship it and clean up", or is wrapping up a chat.
argument-hint: "[--squash | --rebase] [--keep-branch] [--keep-worktree]"
disable-model-invocation: true
---

# Done

Ship this session's branch to `master` and clean up behind it. Running `/done` is the user's go-ahead to commit, push, open the PR and merge it — don't ask again for those. Still stop on anything below marked **stop**.

Arguments:
- `--squash` / `--rebase` — merge method. Default is a merge commit, matching the repo's history.
- `--keep-branch`, `--keep-worktree` — passed through to teardown.

## 1. Establish scope

```bash
WT=$(git rev-parse --show-toplevel)
BRANCH=$(git branch --show-current)
MAIN=$(git worktree list --porcelain | awk 'NR==1{print $2}')
```

**Stop** if `$BRANCH` is `master`/`main` or empty (detached HEAD) — there is nothing to PR. Offer to run teardown on its own.

```bash
git fetch origin master
git log --oneline origin/master..HEAD
git status --porcelain
```

If there are no commits ahead of `origin/master` and the tree is clean, there is nothing to ship: say so and go to step 5.

## 2. Commit remaining work

If `git status --porcelain` shows changes:

- Review them (`git diff`, untracked files). Leave out anything that clearly isn't part of the work — scratch files, logs, secrets, stray Unity-generated files outside the change. If unsure whether a file belongs, **stop** and ask.
- Commit with a conventional-commit message in the repo's style (`feat(scope): …`, `fix(scope): …`), ending with the attribution trailer from the system reminder.

## 3. Push and open the PR

```bash
git push -u origin "$BRANCH"
gh pr list --head "$BRANCH" --state all --json number,state,url
```

- **Open PR exists** → reuse it; update its body if the new commits change the story.
- **Merged PR exists** and no new commits since → skip to step 5.
- **No PR** → create one against `master`. Write the body with the `pr` skill (`mattpocock-skills:pr`) if available; otherwise follow recent merged PRs (`## What`, then notes/how to try it). Title in conventional-commit form. End the body with the attribution line from the system reminder.

```bash
gh pr create --base master --head "$BRANCH" --title "<title>" --body-file <scratchpad>/pr-body.md
```

Bind it with `mcp__ccd_pr__bind_pr` if `mcp__ccd_pr__get_status` doesn't already report it.

## 4. Merge

```bash
gh pr view <n> --json mergeable,mergeStateStatus,statusCheckRollup
```

- **Conflicts** (`mergeable: CONFLICTING`) → bring the branch up to date with `mcp__ccd_host__sync_with_base_branch` (in an app-made worktree) or `git merge origin/master`, resolve, commit, push, re-check. If a conflict isn't mechanical, **stop** and show it.
- **Checks running** → wait for them (`gh pr checks <n> --watch`, run in the background). **Failing checks** → **stop** and show the failure; don't merge.
- **Review required / blocked by branch protection** → **stop** and report; don't use `--admin`.

Then merge. Don't pass `--delete-branch`: `gh` would try to check out `master` locally, which fails because `master` is checked out in `$MAIN`.

```bash
gh pr merge <n> --merge      # or --squash / --rebase per arguments
gh pr view <n> --json state,mergeCommit   # expect MERGED
```

Once it reports `MERGED`, delete the remote branch — `/done` covers this, so teardown doesn't need to ask:

```bash
git push origin --delete "$BRANCH"
```

Update the primary checkout only if it's on `master` and clean; otherwise leave it and mention it:

```bash
[ "$(git -C "$MAIN" branch --show-current)" = master ] && [ -z "$(git -C "$MAIN" status --porcelain)" ] \
  && git -C "$MAIN" pull --ff-only origin master
```

## 5. Teardown

`teardown` has `disable-model-invocation`, so it can't be called through the Skill tool. Read `$WT/.claude/skills/teardown/SKILL.md` (or `$MAIN/.claude/skills/teardown/SKILL.md` if it's missing in the worktree) and follow it with `--yes` plus any `--keep-branch` / `--keep-worktree` passed to `/done`. Its safety checks still apply.

The PR is merged, so the branch counts as merged even if it was squashed or rebased — teardown's `branch -D` fallback applies. The remote branch is already gone.

## 6. Report

One short summary: PR link and merge commit, what teardown removed, and anything left behind and why. Then offer to archive the session (`mcp__ccd_session_mgmt__archive_session`) — the worktree is gone, so don't run more shell commands.
