# Implementation Standards - Space Engineers Scripting

**Read this before writing any code.**

---

## Core Principle

This script must be LIGHTWEIGHT.
ISY hammers servers. We don't.

---

## SE Sandbox Constraints

### What You CAN'T Do
- No file I/O
- No networking
- No reflection
- No async/await
- No threading
- Limited string operations
- No LINQ (performance killer)

### What You CAN Do
- GridTerminalSystem for block access
- Custom Data for configuration
- LCD text panels for output
- Block properties and actions
- Storage string (persists between sessions)

---

## Performance Patterns

### DO: Spread Work Across Ticks

```csharp
int tickCounter = 0;

public void Main(string argument, UpdateType updateSource)
{
    tickCounter++;
    
    switch (tickCounter % 4)
    {
        case 0: RunReactorKeeper(); break;
        case 1: RunQuotaManager(); break;
        case 2: RunDockYoink(); break;
        case 3: RunInventoryDisplay(); break;
    }
}
```

### DON'T: Do Everything Every Tick

```csharp
// ❌ BAD - Will cause lag
public void Main(string argument, UpdateType updateSource)
{
    RunReactorKeeper();
    RunQuotaManager();
    RunDockYoink();
    RunInventoryDisplay();
}
```

---

### DO: Cache Block References

```csharp
List<IMyReactor> reactors = new List<IMyReactor>();
bool blocksCached = false;

void CacheBlocks()
{
    if (blocksCached) return;
    
    GridTerminalSystem.GetBlocksOfType(reactors, 
        b => b.CubeGrid == Me.CubeGrid);
    
    blocksCached = true;
}

// Call CacheBlocks() once, then reuse 'reactors' list
```

### DON'T: Query Blocks Every Tick

```csharp
// ❌ BAD - Queries every single tick
void CheckReactors()
{
    var reactors = new List<IMyReactor>();
    GridTerminalSystem.GetBlocksOfType(reactors);
    // ...
}
```

---

### DO: Use StringBuilder for LCD Output

```csharp
StringBuilder sb = new StringBuilder();

void UpdateDisplay(IMyTextPanel lcd)
{
    sb.Clear();
    sb.AppendLine("=== Status ===");
    sb.AppendLine($"Reactors: {reactorCount}");
    sb.AppendLine($"Uranium: {uraniumAmount}");
    
    lcd.WriteText(sb.ToString());
}
```

### DON'T: Concatenate Strings

```csharp
// ❌ BAD - Creates garbage every concatenation
string output = "";
output += "=== Status ===\n";
output += "Reactors: " + reactorCount + "\n";
output += "Uranium: " + uraniumAmount + "\n";
```

---

### DO: Main Grid Filter

```csharp
// Only get blocks on THIS grid
GridTerminalSystem.GetBlocksOfType(reactors, 
    b => b.CubeGrid == Me.CubeGrid);
```

### DON'T: Process All Grids

```csharp
// ❌ BAD - Gets subgrid blocks too
GridTerminalSystem.GetBlocksOfType(reactors);
```

---

## Config Parsing Pattern

```csharp
void ParseConfig()
{
    string[] lines = Me.CustomData.Split('\n');
    string currentSection = "";
    
    foreach (string line in lines)
    {
        string trimmed = line.Trim();
        
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            currentSection = trimmed.Substring(1, trimmed.Length - 2);
            continue;
        }
        
        if (trimmed.Contains("="))
        {
            string[] parts = trimmed.Split('=');
            string key = parts[0].Trim();
            string value = parts[1].Trim();
            
            ApplyConfig(currentSection, key, value);
        }
    }
}
```

---

## Inventory Transfer Pattern

```csharp
void TransferItems(IMyInventory source, IMyInventory target, 
    MyItemType itemType, MyFixedPoint amount)
{
    if (!source.CanTransferItemTo(target, itemType))
        return;
    
    var items = new List<MyInventoryItem>();
    source.GetItems(items, i => i.Type == itemType);
    
    if (items.Count == 0) return;
    
    MyFixedPoint available = items[0].Amount;
    MyFixedPoint toTransfer = MyFixedPoint.Min(amount, available);
    
    source.TransferItemTo(target, items[0], toTransfer);
}
```

---

## Error Handling

```csharp
void RunModule()
{
    try
    {
        // Module logic here
    }
    catch (Exception e)
    {
        Echo($"Error: {e.Message}");
        // Don't crash the whole script
    }
}
```

---

## State Machine Pattern

For complex logic, use explicit states:

```csharp
enum DockYoinkState { Idle, Detecting, Transferring, Complete }
DockYoinkState yoinkState = DockYoinkState.Idle;

void RunDockYoink()
{
    switch (yoinkState)
    {
        case DockYoinkState.Idle:
            if (CheckForNewConnection())
                yoinkState = DockYoinkState.Detecting;
            break;
            
        case DockYoinkState.Detecting:
            if (FindVisitorCargo())
                yoinkState = DockYoinkState.Transferring;
            else
                yoinkState = DockYoinkState.Idle;
            break;
            
        // etc...
    }
}
```

---

## Code Quality Checklist

Before marking a module complete:

- [ ] Works on main grid only
- [ ] Blocks are cached (not queried every tick)
- [ ] Uses StringBuilder for string building
- [ ] Error handling in place
- [ ] Tested in creative mode
- [ ] No noticeable frame drops
- [ ] Config parsing works
- [ ] LCD output is clean and readable

---

## Remember

> "One job per tick. Cache everything. No LINQ."

If you're ever unsure whether something will cause lag, assume it will and find a lighter approach.
