# LightGrid - Space Engineers Inventory Script
## A lightweight alternative to ISY

**Author:** Harry (Glorktelligence)  
**Created:** January 2026  
**Goal:** Simple, performance-friendly inventory management without hammering the server

---

## Design Philosophy

- **One job per tick** - spread the load, no frame drops
- **Main grid only** - subgrids can fend for themselves
- **Simple text output** - no fancy graphics, just clear info
- **Config via Custom Data** - easy to tweak without editing code

---

## Tick Cycle

| Tick | Module | Job |
|------|--------|-----|
| 1 | Quota Manager | Check quotas, queue assembler jobs |
| 2 | Reactor Keeper | Check reactors, top up if needed |
| 3 | Dock & Yoink | Check connector, grab visitor cargo |
| 4 | Inventory Display | Rebuild "what have I got" LCD |
| 5 | Return to Tick 1 | |

---

## Module 1: Reactor Keeper

### Purpose
Keep all main grid reactors fed with uranium from storage. Visual status via emoticon block.

### Config (Custom Data)

```ini
[ReactorKeeper]
Max=1000
Min=600
WarnAt=800
EmoteBlock=Reactor Status Emote
StatusLCD=Reactor Status LCD
SourceGroup=Uranium Storage
```

### Logic

1. Get all reactors where `reactor.CubeGrid == Me.CubeGrid` (main grid only)
2. Calculate average uranium across all reactors
3. If ANY reactor at or below `Min`:
   - Try to top up ALL reactors to `Max` (keeps them synced)
   - Pull from cargo containers in `SourceGroup`
4. Update emoticon and LCD based on status

### Status Levels

| Average Uranium | Emote | LCD Status |
|-----------------|-------|------------|
| Above WarnAt (800) | Happy 😊 | "Chillin" |
| Between Min and WarnAt | Meh/Bored 😐 | "Getting Low - Mine Soon" |
| At/Below Min AND storage empty | Angry/Skull 💀 | "GO MINE YOU FOOL" |

### LCD Output

```
Reactors: 4 | Avg: 847 U
Status: Chillin
```

Or when critical:

```
Reactors: 4 | Avg: 312 U
Status: GO MINE YOU FOOL
```

---

## Module 2: Quota Manager

### Purpose
Read an LCD for desired stock levels, tell assemblers to produce up to those amounts.

### Config (Custom Data)

```ini
[QuotaManager]
QuotaLCD=Quota List LCD
```

### Quota LCD Format (user edits this LCD)

```
Iron Ingot: 10000
Steel Plate: 5000
Interior Plate: 2000
Computer: 500
```

### Logic

1. Parse the Quota LCD for "Item Name: Amount" lines
2. Count current inventory of each item (main grid)
3. If below quota, queue production in assemblers
4. Don't spam - only queue difference, check if already queued

### Notes

- No component learning - just reads what you've written
- Assemblers need the blueprints available obviously
- Could add a status LCD showing "Steel Plate: 3400/5000" etc

---

## Module 3: Dock & Yoink

### Purpose
When a ship connects to a named connector, automatically grab everything from visitor's cargo.

### Config (Custom Data)

```ini
[DockYoink]
ConnectorName=Yoink Connector
TargetGroup=Main Storage
```

### Logic

1. Find connector with matching name
2. Check if `connector.Status == Connected`
3. If connected and wasn't connected last tick (new connection):
   - Find all cargo on the OTHER grid (the visitor)
   - Find a non-full cargo in `TargetGroup`
   - Transfer everything from visitor to our storage
4. Track connection state to avoid repeated yoinking

### Notes

- Only triggers once per connection (not every tick while connected)
- Picks random non-full cargo to spread the load
- Visitor keeps their ship, we just empty their pockets

---

## Module 4: Inventory Display

### Purpose
Simple "here's what you've got" list on an LCD.

### Config (Custom Data)

```ini
[InventoryDisplay]
DisplayLCD=Inventory List LCD
ShowZero=false
SortBy=name
```

### LCD Output

```
=== Main Grid Inventory ===
Bulletproof Glass: 234
Computer: 1,203
Construction Comp: 4,521
Iron Ingot: 45,230
Iron Ore: 12,000
Steel Plate: 8,442
Uranium Ingot: 3,200
...
```

### Logic

1. Scan all cargo containers on main grid
2. Tally up totals per item type
3. Sort alphabetically (or by amount if configured)
4. Write to LCD
5. Optional: skip zero-count items

---

## Full Custom Data Template

```ini
[LightGrid]
; Master enable/disable for modules
EnableReactorKeeper=true
EnableQuotaManager=true
EnableDockYoink=true
EnableInventoryDisplay=true

[ReactorKeeper]
Max=1000
Min=600
WarnAt=800
EmoteBlock=Reactor Status Emote
StatusLCD=Reactor Status LCD
SourceGroup=Uranium Storage

[QuotaManager]
QuotaLCD=Quota List LCD
StatusLCD=Quota Status LCD

[DockYoink]
ConnectorName=Yoink Connector
TargetGroup=Main Storage

[InventoryDisplay]
DisplayLCD=Inventory List LCD
ShowZero=false
SortBy=name
```

---

## Performance Notes

- Each module runs once every 4 ticks (assuming 60 tick/sec, that's 15 checks per second per module - plenty fast)
- No sorting = no chain transfers eating up API calls
- No constant LCD rebuilding for fancy graphics
- Simple string building, minimal concatenation
- Main grid filter means we ignore complex subgrid nonsense

---

## Planned Features (Phase 2)

### Module 5: Gas Keeper (Hydrogen/Oxygen)

Same pattern as Reactor Keeper but for H2/O2 tanks.

**Config:**

```ini
[GasKeeper]
EnableHydrogen=true
EnableOxygen=true
H2WarnPercent=30
H2CriticalPercent=15
O2WarnPercent=25
O2CriticalPercent=10
EmoteBlock=Gas Status Emote
StatusLCD=Gas Status LCD
```

**LCD Output:**

```
Hydrogen: 78% [████████░░]
Oxygen: 92% [█████████░]
Status: Chillin
```

**Emote Logic:**
- Happy: Both gases above warn threshold
- Meh: One or both below warn but above critical
- Skull: Either gas below critical (you're about to suffocate or your jetpack is a backpack)

---

### Module 6: Battery Status

Monitor battery charge and drain/charge rate.

**Config:**

```ini
[BatteryStatus]
WarnPercent=30
CriticalPercent=15
EmoteBlock=Power Status Emote
StatusLCD=Battery Status LCD
```

**LCD Output:**

```
Batteries: 8 | Charge: 67%
Rate: +2.4 MW (Charging)
Time to Full: 1h 23m
Status: Chillin
```

Or when draining:

```
Batteries: 8 | Charge: 23%
Rate: -4.1 MW (Draining)
Time to Empty: 45m
Status: FIND POWER SOURCE
```

---

### Module 7: Sound Alerts

Optional audio feedback for critical states.

**Config:**

```ini
[SoundAlerts]
EnableSounds=true
SoundBlock=Alert Speaker
CriticalSound=SoundBlockAlert2
WarningSound=SoundBlockAlert1
```

**Logic:**
- Play warning sound once when any system enters warning state
- Play critical sound once when any system enters critical state
- Don't spam - only plays on state CHANGE, not every tick

---

### Core Enhancement: Configurable Tick Intervals

Not everything needs 15 checks per second. Let the user slow down modules that don't need rapid response.

**Config:**

```ini
[LightGrid]
; Tick intervals (higher = less frequent, lighter load)
; Default 1 = every cycle, 10 = every 10th cycle
ReactorKeeperInterval=2
QuotaManagerInterval=5
DockYoinkInterval=1
InventoryDisplayInterval=10
GasKeeperInterval=2
BatteryStatusInterval=3
```

**Why this matters:**
- Inventory Display doesn't need to update 15x/sec - every 10 cycles (1.5/sec) is plenty
- Quota Manager can be lazy - checking every 5 cycles still catches up fast
- Dock & Yoink should stay fast so you don't have to wait at connectors
- Reactor/Gas/Battery at 2-3 is responsive without being wasteful

---

## Next Steps

### Phase 1 - Core Modules
1. ✅ Spec complete
2. ⬜ Build Reactor Keeper module first (most self-contained)
3. ⬜ Test in creative
4. ⬜ Add Quota Manager
5. ⬜ Add Dock & Yoink
6. ⬜ Add Inventory Display
7. ⬜ Test full Phase 1 on Gmod's server

### Phase 2 - Extended Modules
8. ⬜ Add configurable tick intervals to core
9. ⬜ Add Gas Keeper (H2/O2)
10. ⬜ Add Battery Status
11. ⬜ Add Sound Alerts
12. ⬜ Full testing

### Phase 3 - Victory Lap
13. ⬜ Profit (and bigger builds because PCU go brrrr)
14. ⬜ Smugly watch other players' scripts lag the server while yours sips resources
