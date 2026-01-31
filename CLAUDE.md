# LightGrid - Claude Code Instructions

## Project Overview

**LightGrid** is a lightweight Space Engineers Programmable Block script. It's designed as an alternative to ISY that doesn't hammer servers.

**Language:** C# (Space Engineers Programmable Block sandbox)
**Status:** Specification complete, implementation starting

## Before You Start

**READ THESE IN ORDER:**

1. `Docs/LightGrid-Spec.md` - THE specification (features, config, logic)
2. `.claude/skills/implementation/SKILL.md` - Code patterns and standards
3. `.claude/skills/se-api-patterns/SKILL.md` - SE API reference
4. `.claude/skills/performance-guidelines/SKILL.md` - Why this project exists

## Key Principles

### Performance Is Everything
ISY hammers servers. We don't. Every design decision optimizes for:
- One job per tick (spread the load)
- Block caching (don't query every tick)
- StringBuilder (no string concatenation)
- Main grid only (ignore subgrids)
- No LINQ (creates garbage)

### Implementation Standards
- No placeholders or TODOs
- Complete implementations only
- Test in creative mode first
- Cache all block references
- Handle errors gracefully

## File Locations

| What | Where |
|------|-------|
| Specification | `Docs/LightGrid-Spec.md` |
| Main Script | `Source/LightGrid.cs` |
| Checkpoints | `Docs/checkpoints/` |
| Skills | `.claude/skills/` |

## Development Order

Phase 1:
1. Reactor Keeper (most self-contained)
2. Quota Manager
3. Dock & Yoink
4. Inventory Display

Phase 2:
5. Configurable tick intervals
6. Gas Keeper
7. Battery Status
8. Sound Alerts

## Working with Harry

- He has ADHD - follow context switches smoothly
- He knows what he's doing - skip basic explanations
- Time is valuable - get to the point
- Read the spec before asking questions
- Complete work fully or say you can't

## When Context Gets Full

Create a checkpoint in `Docs/checkpoints/` following the format in the checkpoint skill. Commit everything before compacting.

## Quick Commands

```bash
# Navigate to project
cd G:\Personal-Projects\VoidSmasherBaseManagementScript

# Commit progress
git add .
git commit -m "feat: [description]"
git push origin main
```

## Remember

> "The fastest code is code that doesn't run."

Skip work when state hasn't changed. Cache everything. Spread work across ticks. Don't recreate ISY's problems.
