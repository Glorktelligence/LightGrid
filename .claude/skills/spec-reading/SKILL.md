# Spec Reading - How to Use the Documentation

**Read this to understand where everything is and what to read when.**

---

## Documentation Structure

```
VoidSmasherBaseManagementScript/
├── Docs/
│   ├── LightGrid-Spec.md         # THE specification
│   └── checkpoints/               # Context checkpoint files
├── Source/
│   └── LightGrid.cs              # The actual script
└── .claude/
    └── skills/                    # Claude Code guidance
```

---

## When to Read What

| Task | Read First |
|------|------------|
| Starting ANY work | `Docs/LightGrid-Spec.md` |
| Writing code | `implementation` skill, then `se-api-patterns` skill |
| Performance concerns | `performance-guidelines` skill |
| Resuming work | Latest checkpoint in `Docs/checkpoints/` |
| Understanding Harry | `working-with-harry` skill |

---

## The Spec File

**`Docs/LightGrid-Spec.md`** is the source of truth.

It contains:
- Design philosophy
- Tick cycle architecture
- Module specifications (config, logic, outputs)
- Status levels and emote mappings
- Phase 1 and Phase 2 features
- Custom Data template
- Performance notes
- Development roadmap

**ALWAYS read this before implementing anything.**

---

## Skill Files

| Skill | When to Read |
|-------|--------------|
| `working-with-harry` | Once, to understand context |
| `project-context` | Quick reference for structure |
| `implementation` | Before writing ANY code |
| `se-api-patterns` | When using SE API features |
| `performance-guidelines` | Before and during coding |
| `checkpoint` | When context is getting full |

---

## Specification Authority

If there's a conflict:

| Source A | Source B | Winner |
|----------|----------|--------|
| Spec document | Your assumption | Spec document |
| Spec document | Existing code | Spec document (fix the code) |
| Harry's instruction | Spec document | Harry's instruction (update spec after) |

---

## Checkpoint Files

Location: `Docs/checkpoints/`

Format: `YYYY-MM-DD-[module-name]-checkpoint-N.md`

Use when:
- Resuming after context compaction
- Starting a new session on ongoing work
- Understanding where a module left off

---

## Quick Reference

| I need to... | Read... |
|--------------|---------|
| Understand the project | `Docs/LightGrid-Spec.md` |
| Write module code | `implementation` skill |
| Use SE API | `se-api-patterns` skill |
| Check performance | `performance-guidelines` skill |
| Resume work | checkpoint file |
| Understand config format | `Docs/LightGrid-Spec.md` |

---

## Remember

> "The spec exists. Don't reinvent it."

Harry already designed the architecture. Follow the spec, implement the modules, keep it light.
