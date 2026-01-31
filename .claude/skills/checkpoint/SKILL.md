# Checkpoint - Context Management

**Read this when context is filling up or before starting complex work.**

---

## Purpose

Checkpoints preserve your work and enable seamless continuation.
A good checkpoint means Harry doesn't have to re-explain anything.

---

## When To Checkpoint

Watch for these signs:
- Context usage showing high in UI
- Responses getting shorter or less detailed
- About to start complex multi-module work
- Working 30+ minutes on intensive implementation

**If task is almost done (<5 min):** Finish it, then checkpoint.

---

## Checkpoint Process

### 1. Commit Current Work

```bash
cd G:\Personal-Projects\VoidSmasherBaseManagementScript
git add .
git commit -m "wip: [what you completed so far]"
git push origin main
```

### 2. Create Checkpoint File

**Location:** `G:\Personal-Projects\VoidSmasherBaseManagementScript\Docs\checkpoints\`

**Filename:** `YYYY-MM-DD-[module-name]-checkpoint-N.md`

**Example:** `2026-01-30-reactor-keeper-checkpoint-1.md`

### 3. Document Everything

Use this template:

```markdown
# Checkpoint: [Module] - [Date] - #N

**Time**: HH:MM  
**Status**: In Progress  
**Reason**: Context approaching limit

## Completed ✅

- [x] Config parsing for [section]
- [x] Block caching
- [x] Basic logic flow

## In Progress ⏳

- [ ] Uranium transfer logic
  - File: Source/LightGrid.cs (line 145)
  - Status: [exactly where you stopped]
  - Next: [specific next action]

## Remaining ⭐

- [ ] Emoticon block integration
- [ ] LCD output
- [ ] Testing

## Key Context

- Config format decided: [format]
- Emote names to use: [list]
- Edge cases noted: [list]

## Resume Instructions

1. Open Source/LightGrid.cs
2. Go to line X
3. Continue implementing [specific thing]
4. Then do [next step]
```

### 4. Notify Harry

```
⚠️ Context approaching limit.

Progress saved:
- Commit: [hash]
- Checkpoint: Docs/checkpoints/[filename].md

Options:
A) /compact to continue here
B) New session with resume prompt
```

---

## Resuming From Checkpoint

When Harry provides a resume prompt:

1. **Read** the checkpoint file first (before anything else)
2. **Check** last commit
3. **Continue** from exactly where documented
4. **Do not** restart from beginning
5. **Do not** redo completed work

---

## Checkpoint Quality

### Good Checkpoint Includes:
- Specific file locations
- Exact line numbers
- Clear "in progress" status
- Explicit next steps
- Any SE API quirks discovered

### Poor Checkpoint Has:
- Vague descriptions
- Missing context
- Unclear next steps
- No commit references

---

## Remember

Good checkpoint = 5 minute resume.
Bad checkpoint = 30 minute re-explanation.
