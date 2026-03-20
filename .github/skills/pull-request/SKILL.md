---
name: pull-request
description: Create a pull request for CsWinRT. Use when the user asks to create a PR, open a PR, submit a PR, or make a pull request. Handles target branch selection, title, description, labels, and reviewers.
---

# Create a pull request

Create a well-structured pull request for the CsWinRT repository with proper title, description, labels, and reviewers.

## Step 1: determine the target branch

Choose the target branch based on user instructions:

- If the user **explicitly names a target branch**, use that (e.g. `master`, `staging/3.0`, `feature/xyz`)
- If the user says something like "for the 3.0 release" or "for CsWinRT 3.0", target `staging/3.0` (the staging branch for the current release)
- If the user says "for master" or doesn't specify, target `master`
- When in doubt, ask the user which branch to target

## Step 2: analyze the changes

Before writing the PR description:

1. Run `git log <target_branch>..HEAD --oneline` to see the commits
2. Run `git diff <target_branch> --stat` to see affected files
3. Run `git diff <target_branch>` to read the full diff
4. Understand what changed, why, and how

## Step 3: write the PR title

- Keep it concise but descriptive (under 72 characters ideally)
- Use imperative mood (e.g. "Add Copilot instructions for CsWinRT 3.0", not "Added" or "Adds")
- Don't include a PR number or branch name in the title

## Step 4: write the PR description

Structure the description with these sections:

```markdown
## Summary

Brief summary of what this PR does (1-3 sentences).

## Motivation

Explain *why* these changes are being made. What problem do they solve?
What improvement do they bring? If the user provided motivation when
asking for the PR, use that. Otherwise, infer from the diff, commit
messages, and code comments.

## Changes

List the key changes in the PR:

- **`path/to/file.md`**: Description of what changed in this file
- **`path/to/other/`**: Description of what changed in this directory
- ...
```

**Formatting rules:**
- Use clean, standard markdown (no HTML entities, no non-printable characters)
- Use backticks for file paths, type names, and code references
- Keep bullet points concise
- Don't include raw diff output in the description
- Make sure the description renders well on GitHub

## Step 5: select labels

Fetch the available labels for the repository using the GitHub API tools:

```
github-mcp-server-list_issues with a query to discover labels
```

Or search for labels using the GitHub MCP tools. Apply labels only when they are **clearly applicable** based on the PR content. Don't force labels that don't fit.

## Step 6: Add reviewers

Always add these reviewers (unless one of them is the PR author):
- **ManodasanW**
- **Sergio0694**

To determine the current user (PR author), check `git config user.name` or `git config user.email` and match against the GitHub usernames.

## Step 7: Create the PR

Use the GitHub MCP tools to create the pull request with:
- The determined target branch (`base`)
- The current branch (`head`)
- The title and description
- The labels
- The reviewers

## Example PR description

```markdown
## Summary

Add comprehensive Copilot instructions documenting the CsWinRT 3.0
architecture, build pipeline, and coding conventions.

## Motivation

The CsWinRT 3.0 codebase is complex, with multiple interrelated build
tools and a multi-phase build pipeline. Having detailed Copilot
instructions helps contributors and AI assistants understand the
architecture and make correct changes without extensive ramp-up time.

## Changes

- **`.github/copilot-instructions.md`**: New file with comprehensive
  project documentation covering all 8 CsWinRT 3.0 projects, the build
  pipeline, code conventions, and key technical concepts
- **`.github/skills/update-copilot-instructions/SKILL.md`**: Skill to
  keep the instructions file up to date
- **`.github/skills/testing/SKILL.md`**: Skill for adding tests to the
  right test project
```
