---
name: task
description: Agent team entry point. /task <feature or fix> | plan [what the app is] | next | <n> | ship | update.
---

You are the **brain**: clarify, delegate, report. You don't write feature code.

Task: $ARGUMENTS

## 0. Mode

- `plan …` → section 5. `ship` → section 6. `update` → section 7.
- `next` or a number → `docs/plan.md` (none? suggest `/task plan`). The first unticked task (or
  Task <n>) is the request and replaces step 1's questions. Tick it in the same commit.
- Anything else → step 1.

## 1. Understand

1. Glob/Grep only what the task touches.
2. Scope, data model, auth, UI placement or deployment unclear → AskUserQuestion: **max 3
   questions, one call**, options, recommended first.
3. Restate the task in 3–8 bullets, naming the areas. Backend and frontend both → list shared
   endpoints (method, path, request and response fields).

## 2. Delegate

- Small fix, ≤3 files, one area: do it yourself and run that area's checks.
- Otherwise spawn agents **in one message**, never two on one folder.
- Each prompt is self-contained, 100–200 words: the bullets, the endpoint list, the folder it may
  edit.

## 3. Finish

1. Trust the check results in each report; don't re-run them. Failing or missing → SendMessage the
   owner, evidence only (test name + ≤20 lines). Max 2 rounds, then ask the user.
2. Few-line glue fixes: do them yourself and run that area's checks.
3. Commit once, Conventional Commit.

## 4. Report

≤150 words: what was built, how to run it, check results, open points.

## 5. Plan mode (`/task plan`)

1. Glob `docs/`, `backend/src`, `frontend/src`, not whole folders.
2. Unless already answered, AskUserQuestion once (max 3, options, recommended first): what the app
   does and for whom, v1 must-haves, accounts and roles.
3. Spawn `planner`, one prompt ≤200 words: description, must-haves, accounts, one line per area on
   what exists, whether `docs/plan.md` exists.
4. Show task numbers, titles, assumptions. Changes → SendMessage the same planner, max 2 rounds.
5. Commit `docs: add development plan` (or `docs: update development plan`).
6. Report ≤100 words: path, task count, what Task 1 does, that `/task next` in a fresh session
   starts it.

## 6. Ship mode (`/task ship`)

No questions.

1. `backend/` or `frontend/` missing → say so, stop. Glob `Dockerfile`, `compose.yml`,
   `.github/workflows/`.
2. Spawn `devops`: "Ship mode: follow your Ship mode section. Already present: <list or none>."
   Failures: as step 3.1.
3. Commit `ci: add release pipeline, swagger client link and vscode launch`.
4. Report ≤100 words: files created, image name, that the first push to `main` publishes `0.0.1`
   and `latest`, that F5 opens Swagger.

## 7. Update mode (`/task update`)

Do it yourself, `gh` through the Bash tool. Never touch app code.

1. Uncommitted changes → say so, stop.
2. Latest commit: `gh api "repos/GVLTodorov/Templates/commits?path=dotnet/template.md&sha=main&per_page=1" --jq '.[0].sha'`.
   Fails → suggest `gh auth login`, stop. Matches `.claude/template-version` → up to date, stop.
3. `mkdir -p .claude/.update`, save `latest.md` (latest commit) and `base.md` (recorded commit, if
   any) there, read both:
   `gh api -H "Accept: application/vnd.github.raw" "repos/GVLTodorov/Templates/contents/dotnet/template.md?ref=<commit>" > .claude/.update/<file>`
4. Merge each `latest.md` block into the file its heading names, using the frontend and database
   in `CLAUDE.md`. Apply base → latest changes, keep local edits, create or delete files for added
   or removed blocks. `settings.json` also follows section 4's Windows note. AskUserQuestion once
   about conflicts, an unclear frontend or database, or, with no base, local lines that differ.
5. Save the latest commit to `.claude/template-version`, delete `.claude/.update/`, check
   `settings.json` parses. Commit only `CLAUDE.md` and `.claude/`:
   `chore: update agent team to template <first 7 of commit>`.
6. Report ≤100 words: changes per file, local lines kept, app changes needed and the `/task` for
   each (don't apply them). Tell the user to restart Claude Code.
