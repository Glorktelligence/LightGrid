# LightGrid

**Lightweight Space Engineers Base Management**

A performance-friendly alternative to ISY that doesn't hammer servers. One job per tick, no lag, no drama.

## Features

| Module | What It Does |
|--------|--------------|
| **Reactor Keeper** | Keeps reactors topped up with uranium, warns when low |
| **Quota Manager** | Auto-crafts components to target amounts, disassembles excess |
| **Dock & Yoink** | Grabs cargo from ships when they dock |
| **Inventory Display** | Shows what you've got on an LCD |
| **Gas Keeper** | Monitors H2/O2 levels with warnings |
| **Battery Status** | Tracks charge level and time estimates |
| **Sound Alerts** | Plays alarm when systems go critical |

## Quick Start

1. **Place a Programmable Block** on your main grid
2. **Paste the script** from `Source/LightGrid.cs`
3. **Edit Custom Data** to configure block names and quotas
4. **Run the script** - it handles the rest

The script writes default config to Custom Data on first run. Edit it to match your block names.

## Documentation

- [Setup Guide](Docs/SETUP.md) - Detailed installation and block setup
- [Configuration Reference](Docs/CONFIGURATION.md) - All config options explained
- [Troubleshooting](Docs/TROUBLESHOOTING.md) - Common issues and fixes

## Why LightGrid?

ISY is powerful but hammers server performance. LightGrid takes a different approach:

- **One job per tick** - spreads work across multiple ticks
- **Main grid only** - ignores subgrid complexity
- **Block caching** - finds blocks once, reuses references
- **No LINQ** - avoids garbage collection spikes
- **Configurable intervals** - slow down modules that don't need speed

The result: smooth performance even on multiplayer servers.

## Console Commands

Run these via the Programmable Block's argument field:

| Command | Description |
|---------|-------------|
| `refresh` | Re-scan all blocks |
| `status` | Show block counts |
| `debug` | Show quota calculations |
| `gas` | Debug gas tank detection |
| `scan` | Show raw item IDs (for mods) |
| `intervals` | Show tick interval config |

## Project Structure

```
VoidSmasherBaseManagementScript/
├── Source/
│   └── LightGrid.cs       # The script
├── Docs/
│   ├── SETUP.md           # Installation guide
│   ├── CONFIGURATION.md   # Config reference
│   ├── TROUBLESHOOTING.md # Problem solving
│   └── LightGrid-Spec.md  # Technical specification
├── README.md              # You are here
└── LICENSE                # Usage terms
```

## Author

Harry (Chaos Admiral / Glorktelligence)

## License

See [LICENSE](LICENSE) for terms. Personal use and modification allowed with attribution.
