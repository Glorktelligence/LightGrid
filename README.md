# LightGrid

A lightweight Space Engineers inventory management script. An alternative to ISY that doesn't hammer your server.

## Why?

ISY (Isy's Inventory Manager) is feature-rich but performance-heavy. It scans every inventory, every tick, across all grids, and does complex sorting operations constantly. On multiplayer servers, this adds up.

LightGrid takes a different approach:
- **One job per tick** - spread the work, no frame drops
- **Main grid only** - subgrids manage themselves
- **No sorting** - you have a search bar, use it
- **Simple displays** - text lists, no fancy graphics

## Features

### Phase 1 (Core)
- **Reactor Keeper** - Keep reactors fed with uranium, emoticon status display
- **Quota Manager** - Read quotas from an LCD, queue assembler production
- **Dock & Yoink** - Auto-grab cargo when ships connect
- **Inventory Display** - Simple "what have I got" list

### Phase 2 (Extended)
- Gas Keeper (H2/O2 monitoring)
- Battery Status with time estimates
- Sound Alerts on state change
- Configurable tick intervals per module

## Usage

1. Place a Programmable Block
2. Load the script
3. Configure Custom Data (template provided)
4. Name your blocks to match config
5. Recompile and run

## Configuration

All settings are in the Programmable Block's Custom Data. See `Docs/LightGrid-Spec.md` for full documentation.

## Project Structure

```
VoidSmasherBaseManagementScript/
├── .claude/skills/       # Claude Code guidance
├── Docs/
│   ├── LightGrid-Spec.md # Full specification
│   └── checkpoints/      # Progress checkpoints
├── Source/
│   └── LightGrid.cs      # The script
└── README.md             # You are here
```

## License

Do whatever you want with it. Just don't blame me if your base blows up.

## Author

Harry (Glorktelligence) - 2026
