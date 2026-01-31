# Project Context - LightGrid

**Quick reference for project structure and purpose.**

---

## Project Overview

**Name**: LightGrid (VoidSmasherBaseManagementScript)
**What**: Lightweight Space Engineers inventory/status management script
**Why**: ISY hammers servers - this won't
**Language**: C# (SE Programmable Block sandbox)
**Status**: Specification complete, implementation Phase 1

---

## Directory Structure

```
VoidSmasherBaseManagementScript/
├── .claude/                      # Claude Code configuration
│   └── skills/                   # Task-specific skills
├── Docs/
│   └── LightGrid-Spec.md         # THE specification (read this first)
├── Source/
│   └── LightGrid.cs              # The actual script
├── Testing/
│   └── test-scenarios.md         # Test cases for creative mode
└── README.md                     # Project overview
```

---

## Core Design Principles

### One Job Per Tick
Each module runs on its own tick in rotation:
- Tick 1: Reactor Keeper
- Tick 2: Quota Manager  
- Tick 3: Dock & Yoink
- Tick 4: Inventory Display
- Repeat

This spreads the load - no frame drops.

### Main Grid Only
`block.CubeGrid == Me.CubeGrid` filter on everything.
Subgrids fend for themselves.

### Config via Custom Data
All settings in the Programmable Block's Custom Data field.
INI-style format for easy editing.

### Minimal String Work
Simple text output. No fancy graphics.
String building is expensive in SE.

---

## The Modules

### Phase 1 (Core)

| Module | Purpose |
|--------|---------|
| Reactor Keeper | Keep reactors fed, emoticon status |
| Quota Manager | Read LCD quotas, queue assembler jobs |
| Dock & Yoink | Grab cargo from connected ships |
| Inventory Display | Simple "what have I got" list |

### Phase 2 (Extended)

| Module | Purpose |
|--------|---------|
| Gas Keeper | H2/O2 tank monitoring |
| Battery Status | Charge level and time estimates |
| Sound Alerts | Audio warnings on state change |
| Tick Intervals | Per-module frequency config |

---

## Key Files

| File | Purpose |
|------|---------|
| `Docs/LightGrid-Spec.md` | Complete specification - READ FIRST |
| `Source/LightGrid.cs` | The script itself |
| `.claude/skills/` | Claude Code guidance |

---

## Development Workflow

1. Read the spec
2. Implement one module at a time
3. Test in creative mode
4. Verify no performance issues
5. Test on multiplayer server
6. Move to next module

---

## For Full Details

**Read**: `Docs/LightGrid-Spec.md`

This contains:
- Complete module specifications
- Custom Data format
- Status levels and emotes
- LCD output formats
- Performance notes
- Phase roadmap
