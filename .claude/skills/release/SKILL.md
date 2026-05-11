---
name: release
description: Ship pending work from develop to master. Commits any uncommitted changes (after confirmation), pushes develop, waits for the GitHub Actions build to go green, then fast-forwards master to develop and pushes master. Use when the user says "release", "releasen", "ship", "deploy", "auf master" or similar.
---

# Release develop → master

Goal: get the current state of `develop` onto `master` once CI is green. Pure git-and-CI plumbing — no version bumps, no tags, no changelog.

## Preconditions

- Current branch must be `develop`. If not, abort and tell the user. Do not switch branches yourself.
- `origin` remote must exist. If `gh` is not authenticated, abort with the auth hint.
- The user must have explicitly said "release"/"ship"/equivalent. Don't run this skill speculatively.

## Procedure

### 1. Handle uncommitted changes

Run `git status` and `git diff`.

- **No uncommitted changes**: skip to step 2.
- **Uncommitted changes present**:
  1. Show a short summary of what's modified (file list + 1-2 sentences on what the diff does).
  2. Propose a commit message in the repo's style (imperative, English, short — see `git log --oneline -5`).
  3. Ask the user: "Diese Änderungen committen mit Message 'X'? Oder welche Files weglassen?" Always ask — never auto-commit, even if the diff looks clean. The user might have WIP drift mixed in (e.g. `.claude/settings.local.json`).
  4. Stage only the files the user confirmed (specific paths, never `git add -A`/`.`).
  5. Commit. **No** `Co-Authored-By` or other AI branding (project rule). No `--no-verify`.
  6. If a hook fails, fix the underlying issue and create a new commit — never `--amend` after a failed hook.

### 2. Push develop

- If `git status` reports "Your branch is up to date with 'origin/develop'" and there are no local commits ahead, skip to step 3.
- Otherwise: `git push origin develop`.

### 3. Wait for the build

- Find the run for the latest develop commit: `gh run list --branch develop --limit 1 --json databaseId,headSha,status,conclusion`. Confirm `headSha` matches `git rev-parse HEAD`.
- If no run yet, retry every few seconds — push triggers may take a moment.
- Once you have the run id, wait for completion. Use `gh run watch <id> --exit-status` (run in background, you get notified on exit).
- Past runs on this repo take ~7-10 minutes — set a generous timeout (~15min).

### 4. Branch on outcome

- **Build green** (exit 0 / `conclusion == "success"`): proceed to step 5.
- **Build red / cancelled / timed out**: abort the release.
  - Run `gh run view <id> --log-failed` (or `gh run view <id>` for a summary) and surface a concise failure summary to the user (which job, which step, top error lines).
  - Do **not** touch master. Do **not** retry without the user's say-so.

### 5. Merge into master and push

This repo's `master` history uses merge commits (`Merge branch 'develop'`), not fast-forwards. So the procedure is the standard one:

```bash
git checkout master
git pull origin master
git merge develop      # creates a "Merge branch 'develop'" commit
git push origin master
git checkout develop   # always return the user to develop
```

Don't use `git push origin develop:master` — that requires a fast-forward, which fails here because master always has the prior merge commit on top.

If `git merge develop` reports conflicts, abort the release and tell the user — don't try to resolve. (Conflicts shouldn't happen in this flow but if they do, the history is unusual and needs human attention.)

If the user gets stuck on master because something failed mid-flow, run `git checkout develop` to put them back.

### 6. Report

One short summary: commit SHA released, link to the successful build run (`gh run view <id> --json url --jq .url`), confirmation that master is now at that SHA.

## Notes

- Don't open a PR — this is a direct ship workflow, not a review workflow. If the user wants review, they'll use `/ultrareview` or open a PR manually.
- Don't push tags or create releases. Out of scope.
- If the user invokes this with a dirty working tree on a feature branch, the precondition check catches it — don't try to be clever.
