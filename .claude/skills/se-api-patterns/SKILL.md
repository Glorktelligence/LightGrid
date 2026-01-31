# Space Engineers API Patterns

**Common patterns for working with the SE Programmable Block API.**

---

## Block Types We Use

| Interface | Block |
|-----------|-------|
| `IMyReactor` | Reactors (large/small) |
| `IMyAssembler` | Assemblers |
| `IMyCargoContainer` | Cargo containers |
| `IMyShipConnector` | Connectors |
| `IMyTextPanel` | LCDs and text panels |
| `IMyGasTank` | Hydrogen/Oxygen tanks |
| `IMyBatteryBlock` | Batteries |
| `IMySoundBlock` | Sound blocks |
| `IMyEmotionControllerBlock` | Emoticon blocks |

---

## Getting Blocks

### All Blocks of Type (Filtered)

```csharp
List<IMyReactor> reactors = new List<IMyReactor>();

// Main grid only
GridTerminalSystem.GetBlocksOfType(reactors, 
    b => b.CubeGrid == Me.CubeGrid);

// By name pattern
GridTerminalSystem.GetBlocksOfType(reactors, 
    b => b.CustomName.Contains("Main"));

// By group
IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName("Uranium Storage");
group?.GetBlocksOfType(reactors);
```

### Single Block by Name

```csharp
IMyTextPanel lcd = GridTerminalSystem.GetBlockWithName("Status LCD") as IMyTextPanel;
if (lcd != null)
{
    // Use lcd
}
```

---

## Reactor Operations

### Get Uranium Amount

```csharp
MyFixedPoint GetUraniumAmount(IMyReactor reactor)
{
    var inventory = reactor.GetInventory(0);
    var items = new List<MyInventoryItem>();
    inventory.GetItems(items);
    
    foreach (var item in items)
    {
        if (item.Type.SubtypeId == "Uranium")
            return item.Amount;
    }
    
    return 0;
}
```

### Transfer Uranium to Reactor

```csharp
void TopUpReactor(IMyReactor reactor, IMyCargoContainer source, MyFixedPoint targetAmount)
{
    MyFixedPoint current = GetUraniumAmount(reactor);
    MyFixedPoint needed = targetAmount - current;
    
    if (needed <= 0) return;
    
    var sourceInv = source.GetInventory(0);
    var reactorInv = reactor.GetInventory(0);
    
    var items = new List<MyInventoryItem>();
    sourceInv.GetItems(items, i => i.Type.SubtypeId == "Uranium");
    
    if (items.Count == 0) return;
    
    MyFixedPoint toTransfer = MyFixedPoint.Min(needed, items[0].Amount);
    sourceInv.TransferItemTo(reactorInv, items[0], toTransfer);
}
```

---

## Connector Operations

### Check Connection Status

```csharp
bool IsConnected(IMyShipConnector connector)
{
    return connector.Status == MyShipConnectorStatus.Connected;
}

// Get the OTHER grid (the visitor)
IMyCubeGrid GetConnectedGrid(IMyShipConnector connector)
{
    if (connector.Status != MyShipConnectorStatus.Connected)
        return null;
    
    var other = connector.OtherConnector;
    return other?.CubeGrid;
}
```

### Get Cargo from Another Grid

```csharp
List<IMyCargoContainer> GetVisitorCargo(IMyCubeGrid visitorGrid)
{
    var allCargo = new List<IMyCargoContainer>();
    GridTerminalSystem.GetBlocksOfType(allCargo, 
        b => b.CubeGrid == visitorGrid);
    return allCargo;
}
```

---

## Assembler Operations

### Queue Production

```csharp
void QueueProduction(IMyAssembler assembler, MyDefinitionId blueprint, MyFixedPoint amount)
{
    if (assembler.CanUseBlueprint(blueprint))
    {
        assembler.AddQueueItem(blueprint, amount);
    }
}

// Blueprint ID format: "MyObjectBuilder_BlueprintDefinition/SteelPlate"
MyDefinitionId GetBlueprintId(string subtypeId)
{
    return MyDefinitionId.Parse($"MyObjectBuilder_BlueprintDefinition/{subtypeId}");
}
```

### Check Current Queue

```csharp
MyFixedPoint GetQueuedAmount(IMyAssembler assembler, MyDefinitionId blueprint)
{
    var queue = new List<MyProductionItem>();
    assembler.GetQueue(queue);
    
    foreach (var item in queue)
    {
        if (item.BlueprintId == blueprint)
            return item.Amount;
    }
    
    return 0;
}
```

---

## LCD/Text Panel Operations

### Write Text

```csharp
void WriteToLCD(IMyTextPanel lcd, string text)
{
    lcd.ContentType = ContentType.TEXT_AND_IMAGE;
    lcd.WriteText(text);
}

// Append instead of replace
void AppendToLCD(IMyTextPanel lcd, string text)
{
    lcd.WriteText(text, true);
}
```

### Read Text (for quota LCD)

```csharp
string ReadLCD(IMyTextPanel lcd)
{
    return lcd.GetText();
}
```

---

## Emoticon Block Operations

The Emotion Controller uses **Terminal Actions** to change faces, NOT direct properties.
It has 16 emotions accessible via actions named `Emotion1` through `Emotion16`.

### Set Emote via Action

```csharp
void SetEmote(IMyEmotionControllerBlock emoteBlock, int emotionNumber)
{
    // Emotions are 1-16, accessed via terminal actions
    // Action names: "Emotion1", "Emotion2", ... "Emotion16"
    ITerminalAction action = emoteBlock.GetActionWithName($"Emotion{emotionNumber}");
    if (action != null)
    {
        action.Apply(emoteBlock);
    }
}

// Alternative: Use ApplyAction directly
void SetEmoteSimple(IMyTerminalBlock emoteBlock, int emotionNumber)
{
    emoteBlock.ApplyAction($"Emotion{emotionNumber}");
}
```

### Emotion Numbers (may vary, test in-game)

Common mappings (verify these in your game):
- Emotion1-4: Happy variants
- Emotion5-8: Neutral/Confused variants  
- Emotion9-12: Sad/Worried variants
- Emotion13-16: Angry/Skull variants

**TIP:** Open the Emotion Controller in the terminal, go to the toolbar config,
and note which emotion number corresponds to which face.

### IMPORTANT: Timer Workaround Required!

The Emotion Controller **cannot be controlled directly via script API**.
The ApplyAction calls silently fail.

**Workaround:** Use Timer Blocks as intermediaries:
1. Create timer blocks named like "Reactor Status Happy", "Reactor Status Dead"
2. Assign the emotion action to each timer's toolbar
3. Script calls `timer.ApplyAction("TriggerNow")` instead

```csharp
// This DOES NOT WORK:
emoteBlock.ApplyAction("Happy"); // Silently fails!

// This WORKS:
var timer = GridTerminalSystem.GetBlockWithName("Reactor Status Happy") as IMyTimerBlock;
if (timer != null)
    timer.ApplyAction("TriggerNow");
```

This is a common SE pattern - using timers to trigger actions scripts can't do directly.

---

## Gas Tank Operations

### Get Fill Percentage

```csharp
double GetTankFillPercent(IMyGasTank tank)
{
    return tank.FilledRatio * 100;
}

// Check if hydrogen or oxygen
bool IsHydrogenTank(IMyGasTank tank)
{
    return tank.BlockDefinition.SubtypeId.Contains("Hydrogen");
}
```

---

## Battery Operations

### Get Charge Info

```csharp
float GetChargePercent(IMyBatteryBlock battery)
{
    return battery.CurrentStoredPower / battery.MaxStoredPower * 100f;
}

float GetNetPowerFlow(IMyBatteryBlock battery)
{
    // Positive = charging, Negative = draining
    return battery.CurrentInput - battery.CurrentOutput;
}
```

---

## Sound Block Operations

### Play Sound Once

```csharp
void PlayAlert(IMySoundBlock soundBlock, string soundName)
{
    soundBlock.SelectedSound = soundName;
    soundBlock.Play();
}

// Common sounds:
// "SoundBlockAlert1" - Warning beep
// "SoundBlockAlert2" - Alarm
// "SoundBlockObjectiveComplete" - Success chime
```

---

## Custom Data (Config) Pattern

### Default Config Template

```csharp
const string DEFAULT_CONFIG = @"[LightGrid]
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
";

void EnsureConfig()
{
    if (string.IsNullOrWhiteSpace(Me.CustomData))
    {
        Me.CustomData = DEFAULT_CONFIG;
    }
}
```

---

## Item Type Constants

Common item type IDs:

```csharp
// Ingots
MyItemType URANIUM = new MyItemType("MyObjectBuilder_Ingot", "Uranium");
MyItemType IRON_INGOT = new MyItemType("MyObjectBuilder_Ingot", "Iron");

// Ores
MyItemType IRON_ORE = new MyItemType("MyObjectBuilder_Ore", "Iron");

// Components
MyItemType STEEL_PLATE = new MyItemType("MyObjectBuilder_Component", "SteelPlate");
MyItemType COMPUTER = new MyItemType("MyObjectBuilder_Component", "Computer");
```

---

## Remember

> "Check for null. Always check for null."

Blocks can be destroyed, removed, or renamed at any time. Always validate.
