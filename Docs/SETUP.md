# LightGrid Setup Guide

## Prerequisites

- A Programmable Block on your main grid
- LCDs for status displays (optional but recommended)
- Timer blocks for emoticon status indicators (optional)

## Installation

### Step 1: Create the Programmable Block

1. Place a Programmable Block on your main grid
2. Open the block's terminal
3. Click "Edit" to open the code editor
4. Paste the entire contents of `Source/LightGrid.cs`
5. Click "Check Code" to verify (should show no errors)
6. Click "Remember & Exit"

### Step 2: First Run

1. In the Programmable Block terminal, click "Run"
2. The script writes default configuration to Custom Data
3. Check the Programmable Block's info panel for status messages

### Step 3: Configure

1. Open the Programmable Block's Custom Data
2. Edit block names to match your grid
3. Uncomment and set quota amounts
4. Run `refresh` to apply changes

## Block Naming

LightGrid finds blocks by exact name match. Name your blocks to match the config.

### Default Block Names

| Config Setting | Default Name |
|----------------|--------------|
| Reactor StatusLCD | `Reactor Status LCD` |
| Reactor SourceGroup | `Uranium Storage` |
| Reactor HappyTimer | `Reactor Status Happy` |
| Reactor WarningTimer | `Reactor Status Warning` |
| Reactor CriticalTimer | `Reactor Status Critical` |
| Quota StatusLCD | `Quota Status LCD` |
| Quota DisassemblerName | `Quota Disassembler` |
| DockYoink ConnectorName | `Yoink Connector` |
| DockYoink TargetGroup | `Main Storage` |
| Inventory DisplayLCD | `Inventory List LCD` |
| Gas StatusLCD | `Gas Status LCD` |
| Gas HappyTimer | `Gas Status Happy` |
| Gas WarningTimer | `Gas Status Warning` |
| Gas CriticalTimer | `Gas Status Critical` |
| Battery StatusLCD | `Battery Status LCD` |
| Sound AlertSound | `LightGrid Alert Sound` |

## Block Groups

Some features use block groups instead of individual blocks:

### Uranium Storage Group
Create a group named `Uranium Storage` containing cargo containers that store uranium. Reactor Keeper pulls from this group to top up reactors.

### Main Storage Group
Create a group named `Main Storage` containing your main cargo containers. Dock & Yoink deposits items here from visiting ships.

## Timer Block Workaround (Emoticons)

Space Engineers doesn't let scripts directly control Emoticon blocks. The workaround uses Timer Blocks:

### Setup for Each Status Level

1. **Create Timer Block** - Name it exactly as configured (e.g., `Reactor Status Happy`)
2. **Configure Action** - Add an action to change the Emoticon block's face
3. **Set Delay** - Doesn't matter, we trigger immediately

### Example: Reactor Status Emoticons

1. Place an Emoticon block, name it `Reactor Emote`
2. Create three Timer Blocks:
   - `Reactor Status Happy` - triggers happy face
   - `Reactor Status Warning` - triggers meh/bored face
   - `Reactor Status Critical` - triggers skull/angry face
3. For each timer, add action: `Reactor Emote` > `Change face` > select appropriate face

The script triggers the appropriate timer based on status.

## Module Requirements

### Reactor Keeper
- At least one Reactor
- Cargo containers in `Uranium Storage` group
- Optional: Status LCD, Timer blocks for emoticons

### Quota Manager
- At least one Assembler (set to Assembly mode)
- Optional: Status LCD
- Optional: Dedicated Disassembler (set to Disassembly mode)

### Dock & Yoink
- One Connector named `Yoink Connector`
- Cargo containers in `Main Storage` group

### Inventory Display
- One LCD named `Inventory List LCD`

### Gas Keeper
- H2 and/or O2 tanks
- Optional: Status LCD, Timer blocks for emoticons

### Battery Status
- At least one Battery
- Optional: Status LCD

### Sound Alerts
- One Sound Block named `LightGrid Alert Sound`
- Configure the sound block with your preferred alert sound

## Example Base Layout

```
Main Grid:
├── Programmable Block (running LightGrid)
├── Reactors (any number)
├── Assemblers (any number, first one in Assembly mode used)
├── Disassembler (optional, set to Disassembly mode)
├── Connectors
│   └── "Yoink Connector" (for auto-transfer)
├── Cargo Containers
│   ├── Group: "Uranium Storage" (for reactor fuel)
│   └── Group: "Main Storage" (general storage)
├── Gas Tanks (H2 and O2)
├── Batteries
├── LCDs
│   ├── "Reactor Status LCD"
│   ├── "Quota Status LCD"
│   ├── "Inventory List LCD"
│   ├── "Gas Status LCD"
│   └── "Battery Status LCD"
├── Timer Blocks (for emoticons)
│   ├── "Reactor Status Happy/Warning/Critical"
│   └── "Gas Status Happy/Warning/Critical"
├── Emoticon Blocks (optional, for visual status)
└── Sound Block
    └── "LightGrid Alert Sound"
```

## Verification

After setup, run these commands to verify:

1. `status` - Shows block counts, verify all expected blocks found
2. `debug` - Shows quota configuration and inventory counts
3. `gas` - Shows detected gas tanks and their classification
4. `intervals` - Shows tick interval configuration

If blocks show "NOT FOUND", check:
- Block name matches config exactly (case-sensitive)
- Block is on the main grid (not a subgrid/rotor/piston)
- Block is functional (not damaged)

## Updating Configuration

After changing Custom Data:
1. Run `refresh` command to re-scan blocks
2. Or wait for automatic cache refresh (happens on stale block detection)
