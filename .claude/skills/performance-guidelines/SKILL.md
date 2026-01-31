# Performance Guidelines

**THE reason this project exists. ISY hammers servers. We don't.**

---

## Why Performance Matters

Space Engineers runs scripts in a shared environment:
- Your script + other players' scripts + game logic = one thread
- Lag from one script affects EVERYONE on the server
- Server admins WILL ban laggy scripts
- Gmod raises PCU limits - we must not abuse that trust

---

## The Golden Rules

### 1. ONE JOB PER TICK

```csharp
// ✅ GOOD - Spreads load
switch (tickCounter % 4)
{
    case 0: RunReactorKeeper(); break;
    case 1: RunQuotaManager(); break;
    case 2: RunDockYoink(); break;
    case 3: RunInventoryDisplay(); break;
}
```

```csharp
// ❌ BAD - Everything at once
RunReactorKeeper();
RunQuotaManager();
RunDockYoink();
RunInventoryDisplay();
```

### 2. CACHE BLOCK REFERENCES

```csharp
// ✅ GOOD - Query once, reuse forever
List<IMyReactor> reactors = new List<IMyReactor>();
bool cached = false;

void EnsureCache()
{
    if (!cached)
    {
        GridTerminalSystem.GetBlocksOfType(reactors, b => b.CubeGrid == Me.CubeGrid);
        cached = true;
    }
}
```

```csharp
// ❌ BAD - Query every tick
void CheckReactors()
{
    var reactors = new List<IMyReactor>();
    GridTerminalSystem.GetBlocksOfType(reactors); // EXPENSIVE!
}
```

### 3. NO LINQ

```csharp
// ✅ GOOD - Manual loop
MyFixedPoint total = 0;
foreach (var reactor in reactors)
{
    total += GetUraniumAmount(reactor);
}
```

```csharp
// ❌ BAD - LINQ creates garbage
var total = reactors.Sum(r => GetUraniumAmount(r)); // ALLOCATES!
```

### 4. STRINGBUILDER ALWAYS

```csharp
// ✅ GOOD - Reusable StringBuilder
StringBuilder sb = new StringBuilder();

void UpdateDisplay()
{
    sb.Clear();
    sb.AppendLine("Status");
    sb.AppendLine($"Reactors: {count}");
    lcd.WriteText(sb.ToString());
}
```

```csharp
// ❌ BAD - String concatenation
string output = "";
output += "Status\n";           // Allocates new string
output += "Reactors: " + count; // Allocates AGAIN
```

### 5. MAIN GRID ONLY

```csharp
// ✅ GOOD - Only our grid
GridTerminalSystem.GetBlocksOfType(blocks, b => b.CubeGrid == Me.CubeGrid);
```

```csharp
// ❌ BAD - All connected grids
GridTerminalSystem.GetBlocksOfType(blocks); // Includes subgrids!
```

---

## Tick Frequency Guidelines

| Update Frequency | Use Case |
|------------------|----------|
| UpdateFrequency.Update1 | Real-time needs (rare) |
| UpdateFrequency.Update10 | Most status checks |
| UpdateFrequency.Update100 | Slow updates, displays |

For LightGrid, `Update10` is probably ideal:
- 6 updates per second
- Plenty responsive for status monitoring
- Very light on server

---

## What Causes Lag

### HIGH IMPACT (Avoid)
- `GridTerminalSystem.GetBlocksOfType()` every tick
- String concatenation in loops
- LINQ expressions (`.Where()`, `.Select()`, `.Sum()`)
- Large LCD updates every tick
- Processing all inventories at once

### MEDIUM IMPACT (Use Carefully)
- Inventory scanning (spread across ticks)
- Inventory transfers (batch if possible)
- LCD text updates

### LOW IMPACT (Fine)
- Reading block properties
- Simple math
- Cached block operations
- StringBuilder operations

---

## Measuring Performance

Use `Echo()` with timing:

```csharp
DateTime start = DateTime.Now;

// Do stuff

TimeSpan elapsed = DateTime.Now - start;
Echo($"Module took: {elapsed.TotalMilliseconds:F2}ms");
```

Target: Under 1ms per tick. Ideally under 0.5ms.

---

## Emergency Bailout

If a tick is taking too long, bail:

```csharp
DateTime tickStart;
const double MAX_MS = 0.5;

void RunModule()
{
    tickStart = DateTime.Now;
    
    foreach (var item in bigList)
    {
        ProcessItem(item);
        
        if ((DateTime.Now - tickStart).TotalMilliseconds > MAX_MS)
        {
            // Save position, continue next tick
            break;
        }
    }
}
```

---

## Testing Performance

### In Creative Mode
1. Build a test base with:
   - 10+ reactors
   - 20+ cargo containers
   - Multiple connectors
   - Several LCDs
2. Run script
3. Watch for frame drops (F12 or Shift+F11)
4. Check `Echo()` output for timing

### On Server
1. Ask if there's a test/creative server first
2. Monitor server sim speed
3. Be ready to disable script if issues

---

## Comparison: ISY vs LightGrid

| Aspect | ISY | LightGrid |
|--------|-----|-----------|
| Inventory scanning | Every tick, all grids | Spread across ticks, main grid only |
| Sorting | Constant moves | None |
| LCD updates | Fancy graphics every tick | Simple text, less frequent |
| Component tracking | Full database | Just quota checks |
| Block queries | Every tick | Cached |

---

## Remember

> "The fastest code is code that doesn't run."

- Skip work when state hasn't changed
- Update displays less frequently than logic
- Cache everything possible
- Spread work across ticks
- Bail early if taking too long

**Harry chose to build this BECAUSE ISY is heavy. Don't recreate that problem.**
