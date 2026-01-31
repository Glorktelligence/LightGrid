# LightGrid Configuration Reference

Configuration uses INI format in the Programmable Block Custom Data.

## INI Format Basics

```ini
[SectionName]
Key=Value
; This is a comment (ignored)
```

- Sections are in [Brackets]
- Keys and values separated by =
- Lines starting with ; are comments
- Blank lines are ignored

## Master Section

```ini
[LightGrid]
EnableReactorKeeper=true
EnableQuotaManager=true
EnableDockYoink=true
EnableInventoryDisplay=true
```

| Option | Default | Description |
|--------|---------|-------------|
| EnableReactorKeeper | true | Enable/disable reactor management |
| EnableQuotaManager | true | Enable/disable auto-crafting |
| EnableDockYoink | true | Enable/disable cargo transfer on dock |
| EnableInventoryDisplay | true | Enable/disable inventory LCD |

## Reactor Keeper

```ini
[ReactorKeeper]
Max=1000
Min=600
WarnAt=800
StatusLCD=Reactor Status LCD
SourceGroup=Uranium Storage
HappyTimer=Reactor Status Happy
WarningTimer=Reactor Status Warning
CriticalTimer=Reactor Status Critical
```

| Option | Default | Description |
|--------|---------|-------------|
| Max | 1000 | Target uranium per reactor |
| Min | 600 | Uranium level that triggers top-up |
| WarnAt | 800 | Uranium level for warning status |
| StatusLCD | Reactor Status LCD | LCD name for status display |
| SourceGroup | Uranium Storage | Block group name for uranium storage |
| HappyTimer | Reactor Status Happy | Timer block for happy emoticon |
| WarningTimer | Reactor Status Warning | Timer block for warning emoticon |
| CriticalTimer | Reactor Status Critical | Timer block for critical emoticon |

### Status Levels
- **Happy** (green): Average uranium > WarnAt
- **Warning** (yellow): Average uranium between Min and WarnAt
- **Critical** (red): Average uranium <= Min AND storage empty

## Quota Manager

```ini
[QuotaManager]
StatusLCD=Quota Status LCD
EnableDisassembly=true
DisassemblerName=Quota Disassembler
ExcessThreshold=1.5

; Quota definitions
Steel Plate=500
Interior Plate=200
Construction Comp=100
```

| Option | Default | Description |
|--------|---------|-------------|
| StatusLCD | Quota Status LCD | LCD name for status display |
| EnableDisassembly | true | Enable auto-disassembly of excess |
| DisassemblerName | Quota Disassembler | Name of assembler in Disassembly mode |
| ExcessThreshold | 1.5 | Multiplier for excess detection (1.5 = 150% of quota) |

### Quota Definitions

Add item quotas directly in the QuotaManager section:

```ini
ItemName=Amount
```

### Available Components (Vanilla)

| Config Name | Game Item |
|-------------|-----------|
| Steel Plate | Steel Plate |
| Interior Plate | Interior Plate |
| Construction Comp | Construction Component |
| Girder | Girder |
| Small Tube | Small Steel Tube |
| Large Tube | Large Steel Tube |
| Motor | Motor |
| Computer | Computer |
| Display | Display |
| Metal Grid | Metal Grid |
| Bulletproof Glass | Bulletproof Glass |
| Power Cell | Power Cell |
| Solar Cell | Solar Cell |
| Superconductor | Superconductor |
| Detector | Detector Component |
| Radio Comm | Radio-communication Component |
| Medical | Medical Component |
| Reactor Comp | Reactor Component |
| Thruster Comp | Thruster Component |
| Gravity Comp | Gravity Generator Component |
| Explosives | Explosives |

### Modded Components

For mods, use the item SubtypeId. Run the `scan` command to see raw item IDs in your inventory:

```
> scan
MyObjectBuilder_Component/MyModPart: 50
```

Add to config as: `MyModPart=100`

### Material Requirements

Components require these ingots to craft:

| Component | Required Materials |
|-----------|-------------------|
| Steel Plate | Iron |
| Interior Plate | Iron |
| Construction Comp | Iron |
| Girder | Iron |
| Small Tube | Iron |
| Large Tube | Iron |
| Motor | Iron, Nickel |
| Detector | Iron, Nickel |
| Computer | Iron, Silicon |
| Display | Iron, Silicon |
| Radio Comm | Iron, Silicon |
| Metal Grid | Iron, Nickel, Cobalt |
| Bulletproof Glass | Silicon |
| Solar Cell | Silicon, Nickel |
| Power Cell | Iron, Nickel, Silicon |
| Superconductor | Iron, Gold |
| Medical | Iron, Nickel, Silver |
| Reactor Comp | Iron, Gravel, Silver |
| Thruster Comp | Iron, Cobalt, Gold, Platinum |
| Gravity Comp | Iron, Cobalt, Gold, Silver |
| Explosives | Silicon, Magnesium |

## Dock and Yoink

```ini
[DockYoink]
ConnectorName=Yoink Connector
TargetGroup=Main Storage
```

| Option | Default | Description |
|--------|---------|-------------|
| ConnectorName | Yoink Connector | Name of connector to monitor |
| TargetGroup | Main Storage | Block group for destination cargo |

Dock and Yoink triggers once per connection. When a ship connects to the named connector, all items from the visitor cargo, connectors, drills, welders, and grinders are transferred to your storage.

## Gas Keeper

```ini
[GasKeeper]
Enable=true
StatusLCD=Gas Status LCD
HappyTimer=Gas Status Happy
WarningTimer=Gas Status Warning
CriticalTimer=Gas Status Critical
H2WarnPercent=50
H2CriticalPercent=25
O2WarnPercent=50
O2CriticalPercent=25
```

| Option | Default | Description |
|--------|---------|-------------|
| Enable | true | Enable/disable gas monitoring |
| StatusLCD | Gas Status LCD | LCD name for status display |
| HappyTimer | Gas Status Happy | Timer block for happy emoticon |
| WarningTimer | Gas Status Warning | Timer block for warning emoticon |
| CriticalTimer | Gas Status Critical | Timer block for critical emoticon |
| H2WarnPercent | 50 | Hydrogen warning threshold |
| H2CriticalPercent | 25 | Hydrogen critical threshold |
| O2WarnPercent | 50 | Oxygen warning threshold |
| O2CriticalPercent | 25 | Oxygen critical threshold |

### Tank Detection
- Tanks with "Hydrogen" in SubtypeId are classified as H2
- All other tanks are classified as O2 (including default O2 tanks with empty SubtypeId)

## Inventory Display

```ini
[InventoryDisplay]
DisplayLCD=Inventory List LCD
ShowZero=false
SortBy=name
AutoFontSize=true
MaxFontSize=1.2
MinFontSize=0.5
CategoryHeaders=true
```

| Option | Default | Description |
|--------|---------|-------------|
| DisplayLCD | Inventory List LCD | LCD name for inventory display |
| ShowZero | false | Show items with zero quantity |
| SortBy | name | Sort order: `name` or `amount` |
| AutoFontSize | true | Auto-adjust font to fit content |
| MaxFontSize | 1.2 | Maximum font size |
| MinFontSize | 0.5 | Minimum font size |
| CategoryHeaders | true | Group items by category |

### Categories
Items are grouped into: Ores, Ingots, Components, Tools, Ammo, Bottles, Other

## Battery Status

```ini
[BatteryStatus]
Enable=true
StatusLCD=Battery Status LCD
WarnPercent=25
CriticalPercent=10
```

| Option | Default | Description |
|--------|---------|-------------|
| Enable | true | Enable/disable battery monitoring |
| StatusLCD | Battery Status LCD | LCD name for status display |
| WarnPercent | 25 | Charge % for warning (when draining) |
| CriticalPercent | 10 | Charge % for critical (when draining) |

Status colors depend on both charge level AND whether batteries are charging or draining.

## Sound Alerts

```ini
[SoundAlerts]
Enable=true
AlertSound=LightGrid Alert Sound
```

| Option | Default | Description |
|--------|---------|-------------|
| Enable | true | Enable/disable sound alerts |
| AlertSound | LightGrid Alert Sound | Name of sound block |

Sound plays when ANY system enters critical state. Stops when all systems recover.

## Tick Intervals

```ini
[TickIntervals]
ReactorKeeper=1
QuotaManager=2
DockYoink=1
InventoryDisplay=3
GasKeeper=1
BatteryStatus=1
SoundAlerts=1
```

| Option | Default | Description |
|--------|---------|-------------|
| ReactorKeeper | 1 | Rotations between reactor checks |
| QuotaManager | 2 | Rotations between quota checks |
| DockYoink | 1 | Rotations between connector checks |
| InventoryDisplay | 3 | Rotations between inventory updates |
| GasKeeper | 1 | Rotations between gas checks |
| BatteryStatus | 1 | Rotations between battery checks |
| SoundAlerts | 1 | Rotations between alert checks |

Higher values = less CPU usage but slower response. The script runs at Update10 (every 10 game ticks), so:
- Interval 1 = ~6 checks per second
- Interval 2 = ~3 checks per second
- Interval 3 = ~2 checks per second

Recommended:
- Keep DockYoink at 1 (fast response to docking)
- InventoryDisplay can be higher (visual update does not need speed)
- QuotaManager at 2+ (crafting does not need real-time response)
