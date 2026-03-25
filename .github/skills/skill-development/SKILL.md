---
name: skill-development
description: Create and improve skills for GitHub Copilot CLI. Use when the user implies they want to create a skill, write a skill, improve a skill, if they ask why is a skill not showing up, or if they need guidance on skill structure for Copilot CLI.
---

# Skill development for GitHub Copilot CLI

Create effective skills that extend Copilot CLI capabilities with specialized knowledge and workflows.

## Skill structure

```
~/.copilot/skills/
└── skill-name/           # Folder name = skill identifier
    ├── SKILL.md          # Required: Instructions and metadata
    ├── metadata.json     # Optional: Registry metadata
    ├── references/       # Optional: Detailed docs loaded on demand
    ├── scripts/          # Optional: Executable utilities
    └── assets/           # Optional: Templates, files for output
```

## `SKILL.md` format

<critical>
The `name:` field in frontmatter must match the folder name exactly (lowercase, hyphenated). Mismatches cause the skill to not appear in `/skills list`.
</critical>

### Required frontmatter

```yaml
---
name: my-skill-name        # MUST match folder name exactly
description: Brief description. Use when user implies <trigger action 1>, <trigger action 2>, ..., or needs X.
---
```

### Frontmatter rules

| Field | Required | Notes |
|-------|----------|-------|
| `name` | Yes | Must match folder name exactly (lowercase, hyphenated) |
| `description` | Yes | Include trigger phrases users would say or imply |
| `version` | No | Most skills omit this |
| `risk_level` | No | Use for dangerous skills (HIGH, MEDIUM) |
| `model` | No | Suggest a model (sonnet, opus, haiku) |

### Description best practices

Include specific trigger phrases:

**Good:**
```yaml
description: Review GitHub pull requests with high signal-to-noise code review. Use when user provides a GitHub PR URL, asks to review a GitHub pull request, or mentions reviewing code in GitHub.
```

**Bad:**
```yaml
description: Help with code review.  # Too vague, no triggers
```

## `SKILL.md` body

Write instructions for a model to follow when the skill is invoked.

### Structure pattern

```markdown
---
name: skill-name
description: ...
---

# Skill title

Brief overview of what this skill does.

## When to use

- Trigger condition 1
- Trigger condition 2

## Workflow

### Step 1: ...
### Step 2: ...

## Additional resources

- `references/detailed-guide.md` - Extended documentation
- `scripts/utility.py` - Helper script
```

### Writing style

Use imperative form, not second person:

- **Good:** "Read the file before making changes."
- **Bad:** "You should read the file before making changes."

For Claude 4.x models, use XML tags for behavioral instructions:

```xml
<investigate_before_answering>
Read relevant files before proposing changes. Do not speculate about code not yet inspected.
</investigate_before_answering>

<use_parallel_tool_calls>
Launch independent operations in parallel for efficiency.
</use_parallel_tool_calls>
```

## Optional: `metadata.json`

Used by skill registries. Not required for local skills but helps with organization:

```json
{
  "name": "skill-name",
  "description": "Same as SKILL.md description",
  "repo": "github.com/user/repo",
  "category": "development",
  "tags": ["tag1", "tag2"],
  "source": "local"
}
```

## Optional: `references/`

Move detailed content here to keep `SKILL.md` lean. Claude models load these on demand.

**When to use:**
- Detailed patterns (>500 words on a subtopic)
- API documentation
- Advanced techniques
- Examples that are too long for `SKILL.md`

**Reference in `SKILL.md`:**
```markdown
## Additional Resources

For detailed patterns, see `references/patterns.md`.
```

## Optional: `scripts/`

Executable utilities that provide deterministic, reliable operations.

**When to use:**
- Operations repeated frequently
- Tasks requiring exact reproducibility
- Complex parsing/validation logic

**Example:** `scripts/validate-schema.py`, `scripts/find-files.ps1`

## Troubleshooting

### Skill not appearing in `/skills list`

1. **Name mismatch** - `name:` in frontmatter must match folder name exactly
2. **Missing `SKILL.md`** - File must exist and have valid YAML frontmatter
3. **Syntax error** - Check YAML frontmatter is valid (use `---` delimiters)
4. **Run `/skills reload`** - Skills are cached; reload after changes

### Skill not triggering

1. **Weak description** - Add specific trigger phrases users would say
2. **Too generic** - Be specific about when to use the skill
3. **Competing skills** - Check if another skill has overlapping triggers

## Example: minimal skill

```
my-skill/
└── SKILL.md
```

```markdown
---
name: my-skill
description: Does X when user implies they want to do X, perform action X, or needs help with X.
---

# My skill

Brief description.

## Workflow

1. Step one
2. Step two
```

## Example: full skill

```
advanced-skill/
├── SKILL.md
├── metadata.json
├── references/
│   ├── patterns.md
│   └── api-reference.md
└── scripts/
    └── validate.py
```

## Skill creation checklist

- Folder name is lowercase, hyphenated
- `name:` in frontmatter matches folder name exactly
- Description includes trigger phrases
- `SKILL.md` body uses imperative form
- Referenced files actually exist
- Tested with `/skills reload` and `/skills list`
- Triggers correctly when using listed phrases