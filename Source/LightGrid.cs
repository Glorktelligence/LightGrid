// LightGrid - Lightweight Space Engineers Inventory Script
// Author: Harry (Glorktelligence)
// A lightweight alternative to ISY that doesn't hammer servers

// ============================================================
// CONFIGURATION DEFAULTS
// ============================================================

const string DEFAULT_CONFIG = @"[LightGrid]
; Master enable/disable for modules
EnableReactorKeeper=true
EnableQuotaManager=true
EnableDockYoink=true
EnableInventoryDisplay=true

[ReactorKeeper]
Max=1000
Min=600
WarnAt=800
StatusLCD=Reactor Status LCD
SourceGroup=Uranium Storage
HappyTimer=Reactor Status Happy
WarningTimer=Reactor Status Warning
CriticalTimer=Reactor Status Critical

[QuotaManager]
StatusLCD=Quota Status LCD

; ========== QUOTA LIST ==========
; Format: ItemName=Amount
; Uncomment (remove ;) to enable a quota
; Set to 0 or delete line to disable

; --- Basic Components ---
Steel Plate=500
Interior Plate=200
Construction Comp=100
;Girder=50
;Small Tube=100
;Large Tube=50
;Motor=100
;Computer=50
;Display=25

; --- Advanced Components ---
;Metal Grid=50
;Bulletproof Glass=25
;Power Cell=25
;Solar Cell=20
;Superconductor=10
;Detector=10
;Radio Comm=10
;Medical=10
;Reactor Comp=10
;Thruster Comp=20
;Gravity Comp=5
;Explosives=25

; --- Modded Components ---
; For mods, use the exact SubtypeId from the mod
; Example: MyModComponent=100
; The script will try to match it automatically
; If it fails, check the mod's blueprint name

[DockYoink]
ConnectorName=Yoink Connector
TargetGroup=Main Storage

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

[InventoryDisplay]
DisplayLCD=Inventory List LCD
ShowZero=false
SortBy=name

[BatteryStatus]
Enable=true
StatusLCD=Battery Status LCD
WarnPercent=25
CriticalPercent=10
";

// ============================================================
// STATE
// ============================================================

int tickCounter = 0;
bool blocksCached = false;
StringBuilder sb = new StringBuilder();

// Block caches (populated once, reused forever)
List<IMyReactor> reactors = new List<IMyReactor>();
List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
List<IMyAssembler> assemblers = new List<IMyAssembler>();
List<IMyShipConnector> connectors = new List<IMyShipConnector>();

// Config values (parsed from Custom Data)
bool enableReactorKeeper = true;
bool enableQuotaManager = true;
bool enableDockYoink = true;
bool enableInventoryDisplay = true;

// ReactorKeeper config
int reactorMax = 1000;
int reactorMin = 600;
int reactorWarnAt = 800;
string reactorStatusLCDName = "Reactor Status LCD";
string reactorSourceGroupName = "Uranium Storage";
string reactorHappyTimerName = "Reactor Status Happy";
string reactorWarningTimerName = "Reactor Status Warning";
string reactorCriticalTimerName = "Reactor Status Critical";

// ReactorKeeper block references (cached separately since they're by name)
IMyTextPanel reactorStatusLCD = null;
IMyTimerBlock reactorHappyTimer = null;
IMyTimerBlock reactorWarningTimer = null;
IMyTimerBlock reactorCriticalTimer = null;
List<IMyCargoContainer> reactorSourceCargo = new List<IMyCargoContainer>();
bool reactorBlocksCached = false;

// QuotaManager config
string quotaStatusLCDName = "Quota Status LCD";

// QuotaManager block references
IMyTextPanel quotaStatusLCD = null;
bool quotaBlocksCached = false;

// QuotaManager working data (reused to avoid allocations)
List<MyInventoryItem> quotaItemBuffer = new List<MyInventoryItem>();
List<MyProductionItem> quotaQueueBuffer = new List<MyProductionItem>();
Dictionary<string, int> quotaTargets = new Dictionary<string, int>();
Dictionary<string, MyFixedPoint> inventoryCounts = new Dictionary<string, MyFixedPoint>();

// DockYoink config
string yoinkConnectorName = "Yoink Connector";
string yoinkTargetGroupName = "Main Storage";

// DockYoink block references
IMyShipConnector yoinkConnector = null;
List<IMyCargoContainer> yoinkTargetCargo = new List<IMyCargoContainer>();
bool yoinkBlocksCached = false;

// DockYoink state - track connection to detect NEW connections
bool yoinkWasConnected = false;
List<MyInventoryItem> yoinkItemBuffer = new List<MyInventoryItem>();

// InventoryDisplay config
string inventoryDisplayLCDName = "Inventory List LCD";
bool inventoryShowZero = false;
string inventorySortBy = "name"; // "name" or "amount"

// InventoryDisplay block references
IMyTextPanel inventoryDisplayLCD = null;
bool inventoryBlocksCached = false;

// InventoryDisplay working data
Dictionary<string, MyFixedPoint> inventoryTotals = new Dictionary<string, MyFixedPoint>();
List<MyInventoryItem> inventoryItemBuffer = new List<MyInventoryItem>();
List<KeyValuePair<string, MyFixedPoint>> inventorySortBuffer = new List<KeyValuePair<string, MyFixedPoint>>();

// GasKeeper config
bool enableGasKeeper = true;
string gasStatusLCDName = "Gas Status LCD";
string gasHappyTimerName = "Gas Status Happy";
string gasWarningTimerName = "Gas Status Warning";
string gasCriticalTimerName = "Gas Status Critical";
int gasH2WarnPercent = 50;
int gasH2CriticalPercent = 25;
int gasO2WarnPercent = 50;
int gasO2CriticalPercent = 25;

// GasKeeper block references
List<IMyGasTank> h2Tanks = new List<IMyGasTank>();
List<IMyGasTank> o2Tanks = new List<IMyGasTank>();
IMyTextPanel gasStatusLCD = null;
IMyTimerBlock gasHappyTimer = null;
IMyTimerBlock gasWarningTimer = null;
IMyTimerBlock gasCriticalTimer = null;
bool gasBlocksCached = false;

// BatteryStatus config
bool enableBatteryStatus = true;
string batteryStatusLCDName = "Battery Status LCD";
int batteryWarnPercent = 25;
int batteryCriticalPercent = 10;

// BatteryStatus block references
List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
IMyTextPanel batteryStatusLCD = null;
bool batteryBlocksCached = false;

// ============================================================
// ENTRY POINT
// ============================================================

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    EnsureConfig();
}

public void Main(string argument, UpdateType updateSource)
{
    // Handle commands
    if (!string.IsNullOrEmpty(argument))
    {
        HandleCommand(argument.ToLower().Trim());
        return;
    }

    EnsureBlockCache();

    tickCounter++;

    // One job per tick - spread the load
    switch (tickCounter % 6)
    {
        case 0: RunReactorKeeper(); break;
        case 1: RunQuotaManager(); break;
        case 2: RunDockYoink(); break;
        case 3: RunInventoryDisplay(); break;
        case 4: RunGasKeeper(); break;
        case 5: RunBatteryStatus(); break;
    }
}

void HandleCommand(string command)
{
    if (command == "refresh" || command == "reset")
    {
        InvalidateCache();
        Echo("Cache invalidated. Blocks will be re-scanned.");
    }
    else if (command == "status")
    {
        Echo($"Tick: {tickCounter}");
        Echo($"Reactors: {reactors.Count}");
        Echo($"Cargo: {cargoContainers.Count}");
        Echo($"Assemblers: {assemblers.Count}");
        Echo($"Connectors: {connectors.Count}");

        // Yoink status
        EnsureYoinkBlockCache();
        if (yoinkConnector != null)
        {
            string connStatus = yoinkConnector.Status.ToString();
            Echo($"Yoink Connector: {connStatus}");
        }
    }
    else if (command == "debug" || command == "quota")
    {
        // Debug quota system
        EnsureBlockCache();
        EnsureQuotaBlockCache();

        // Show configured quotas
        Echo($"Configured {quotaTargets.Count} quotas:");
        foreach (var kvp in quotaTargets)
        {
            Echo($"  {kvp.Key}: {kvp.Value}");
        }

        // Count and show inventory
        inventoryCounts.Clear();
        CountInventory();
        Echo("Inventory counts:");
        foreach (var kvp in inventoryCounts)
        {
            Echo($"  {kvp.Key}: {(int)(float)kvp.Value}");
        }
    }
    else if (command == "gas" || command == "tanks")
    {
        // Debug gas tanks
        EnsureBlockCache();
        List<IMyGasTank> allTanks = new List<IMyGasTank>();
        GridTerminalSystem.GetBlocksOfType(allTanks, b => b.CubeGrid == Me.CubeGrid);

        Echo($"=== All Gas Tanks ({allTanks.Count}) ===");
        for (int i = 0; i < allTanks.Count; i++)
        {
            IMyGasTank tank = allTanks[i];
            string name = tank.CustomName;
            string subtype = tank.BlockDefinition.SubtypeId;
            string typeId = tank.BlockDefinition.TypeIdString;
            double fill = tank.FilledRatio * 100;

            Echo($"{name}");
            Echo($"  SubtypeId: '{subtype}'");
            Echo($"  TypeId: {typeId}");
            Echo($"  Fill: {fill:F0}%");

            // Check what we'd classify it as
            bool isH2 = subtype.Contains("Hydrogen");
            Echo($"  Classified as: {(isH2 ? "H2" : "O2")}");
            Echo("");
        }
    }
    else if (command == "scan" || command == "items")
    {
        // Scan inventory and show raw SubtypeIds - useful for finding mod item names
        EnsureBlockCache();
        Echo("=== Raw Item SubtypeIds ===");
        Echo("Use these names for modded items:");

        Dictionary<string, MyFixedPoint> rawItems = new Dictionary<string, MyFixedPoint>();

        for (int i = 0; i < cargoContainers.Count; i++)
        {
            IMyCargoContainer cargo = cargoContainers[i];
            if (cargo == null) continue;

            IMyInventory inv = cargo.GetInventory(0);
            if (inv == null) continue;

            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inv.GetItems(items);

            for (int j = 0; j < items.Count; j++)
            {
                MyInventoryItem item = items[j];
                string key = $"{item.Type.TypeId}/{item.Type.SubtypeId}";
                if (rawItems.ContainsKey(key))
                    rawItems[key] += item.Amount;
                else
                    rawItems[key] = item.Amount;
            }
        }

        foreach (var kvp in rawItems)
        {
            Echo($"  {kvp.Key}: {(int)(float)kvp.Value}");
        }
        Echo("");
        Echo("For mods, use the SubtypeId part");
        Echo("e.g., MyObjectBuilder_Component/MyModPart");
        Echo("Add to config as: MyModPart=100");
    }
}

void InvalidateCache()
{
    blocksCached = false;
    reactorBlocksCached = false;
    quotaBlocksCached = false;
    yoinkBlocksCached = false;
    inventoryBlocksCached = false;
    gasBlocksCached = false;
    reactors.Clear();
    cargoContainers.Clear();
    assemblers.Clear();
    connectors.Clear();
    reactorSourceCargo.Clear();
    reactorStatusLCD = null;
    reactorHappyTimer = null;
    reactorWarningTimer = null;
    reactorCriticalTimer = null;
    quotaStatusLCD = null;
    quotaTargets.Clear();
    yoinkConnector = null;
    yoinkTargetCargo.Clear();
    inventoryDisplayLCD = null;
    h2Tanks.Clear();
    o2Tanks.Clear();
    gasStatusLCD = null;
    gasHappyTimer = null;
    gasWarningTimer = null;
    gasCriticalTimer = null;
    batteries.Clear();
    batteryStatusLCD = null;
    batteryBlocksCached = false;
    ParseConfig();
}

// ============================================================
// SETUP
// ============================================================

void EnsureConfig()
{
    if (string.IsNullOrWhiteSpace(Me.CustomData))
    {
        Me.CustomData = DEFAULT_CONFIG;
        Echo("Default config written to Custom Data");
    }
    ParseConfig();
}

void ParseConfig()
{
    string[] lines = Me.CustomData.Split('\n');
    string currentSection = "";

    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i];
        if (line == null) continue;

        string trimmed = line.Trim();

        // Skip empty lines and comments
        if (trimmed.Length == 0 || trimmed.StartsWith(";")) continue;

        // Section header
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            currentSection = trimmed.Substring(1, trimmed.Length - 2);
            continue;
        }

        // Key=Value pair
        int eqIndex = trimmed.IndexOf('=');
        if (eqIndex > 0)
        {
            string key = trimmed.Substring(0, eqIndex).Trim();
            string value = trimmed.Substring(eqIndex + 1).Trim();
            ApplyConfig(currentSection, key, value);
        }
    }
}

void ApplyConfig(string section, string key, string value)
{
    if (section == "LightGrid")
    {
        if (key == "EnableReactorKeeper") enableReactorKeeper = ParseBool(value);
        else if (key == "EnableQuotaManager") enableQuotaManager = ParseBool(value);
        else if (key == "EnableDockYoink") enableDockYoink = ParseBool(value);
        else if (key == "EnableInventoryDisplay") enableInventoryDisplay = ParseBool(value);
    }
    else if (section == "ReactorKeeper")
    {
        if (key == "Max") int.TryParse(value, out reactorMax);
        else if (key == "Min") int.TryParse(value, out reactorMin);
        else if (key == "WarnAt") int.TryParse(value, out reactorWarnAt);
        else if (key == "StatusLCD") reactorStatusLCDName = value;
        else if (key == "SourceGroup") reactorSourceGroupName = value;
        else if (key == "HappyTimer") reactorHappyTimerName = value;
        else if (key == "WarningTimer") reactorWarningTimerName = value;
        else if (key == "CriticalTimer") reactorCriticalTimerName = value;
    }
    else if (section == "QuotaManager")
    {
        if (key == "StatusLCD") quotaStatusLCDName = value;
        else
        {
            // Treat any other key as a quota definition: ItemName=Amount
            string itemName = NormalizeItemName(key);
            int amount;
            if (int.TryParse(value, out amount) && amount > 0)
            {
                quotaTargets[itemName] = amount;
            }
        }
    }
    else if (section == "DockYoink")
    {
        if (key == "ConnectorName") yoinkConnectorName = value;
        else if (key == "TargetGroup") yoinkTargetGroupName = value;
    }
    else if (section == "InventoryDisplay")
    {
        if (key == "DisplayLCD") inventoryDisplayLCDName = value;
        else if (key == "ShowZero") inventoryShowZero = ParseBool(value);
        else if (key == "SortBy") inventorySortBy = value.ToLower();
    }
    else if (section == "GasKeeper")
    {
        if (key == "Enable") enableGasKeeper = ParseBool(value);
        else if (key == "StatusLCD") gasStatusLCDName = value;
        else if (key == "HappyTimer") gasHappyTimerName = value;
        else if (key == "WarningTimer") gasWarningTimerName = value;
        else if (key == "CriticalTimer") gasCriticalTimerName = value;
        else if (key == "H2WarnPercent") int.TryParse(value, out gasH2WarnPercent);
        else if (key == "H2CriticalPercent") int.TryParse(value, out gasH2CriticalPercent);
        else if (key == "O2WarnPercent") int.TryParse(value, out gasO2WarnPercent);
        else if (key == "O2CriticalPercent") int.TryParse(value, out gasO2CriticalPercent);
    }
    else if (section == "BatteryStatus")
    {
        if (key == "Enable") enableBatteryStatus = ParseBool(value);
        else if (key == "StatusLCD") batteryStatusLCDName = value;
        else if (key == "WarnPercent") int.TryParse(value, out batteryWarnPercent);
        else if (key == "CriticalPercent") int.TryParse(value, out batteryCriticalPercent);
    }
}

bool ParseBool(string value)
{
    return value.ToLower() == "true" || value == "1";
}

void EnsureBlockCache()
{
    if (blocksCached) return;
    
    // Cache all blocks we need - MAIN GRID ONLY
    GridTerminalSystem.GetBlocksOfType(reactors, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(cargoContainers, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(assemblers, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(connectors, b => b.CubeGrid == Me.CubeGrid);
    
    blocksCached = true;
    Echo($"Cached: {reactors.Count} reactors, {cargoContainers.Count} cargo, {assemblers.Count} assemblers, {connectors.Count} connectors");
}

// ============================================================
// MODULE: REACTOR KEEPER
// ============================================================

void RunReactorKeeper()
{
    if (!enableReactorKeeper) return;
    if (reactors.Count == 0) return;

    EnsureReactorBlockCache();

    // Calculate uranium levels - also count valid reactors
    MyFixedPoint totalUranium = 0;
    MyFixedPoint lowestUranium = (MyFixedPoint)int.MaxValue;
    int validReactorCount = 0;

    for (int i = 0; i < reactors.Count; i++)
    {
        IMyReactor reactor = reactors[i];
        if (reactor == null || reactor.Closed || !reactor.IsFunctional) continue;
        if (reactor.CubeGrid != Me.CubeGrid) continue;

        validReactorCount++;
        MyFixedPoint amount = GetUraniumInReactor(reactor);
        totalUranium += amount;

        if (amount < lowestUranium)
        {
            lowestUranium = amount;
        }
    }

    // Auto-refresh cache if we lost reactors (stale references)
    if (validReactorCount != reactors.Count)
    {
        blocksCached = false;
        reactorBlocksCached = false;
        EnsureBlockCache();
        EnsureReactorBlockCache();

        // Recalculate with fresh data
        totalUranium = 0;
        lowestUranium = (MyFixedPoint)int.MaxValue;
        validReactorCount = 0;

        for (int i = 0; i < reactors.Count; i++)
        {
            IMyReactor reactor = reactors[i];
            if (reactor == null || reactor.Closed || !reactor.IsFunctional) continue;
            if (reactor.CubeGrid != Me.CubeGrid) continue;

            validReactorCount++;
            MyFixedPoint amount = GetUraniumInReactor(reactor);
            totalUranium += amount;

            if (amount < lowestUranium)
            {
                lowestUranium = amount;
            }
        }
    }

    int reactorCount = validReactorCount;
    int avgUranium = reactorCount > 0 ? (int)((float)totalUranium / reactorCount) : 0;
    int lowestInt = (int)(float)lowestUranium;

    // Check if we need to top up (any reactor at or below Min)
    bool needsTopUp = lowestInt <= reactorMin;
    bool storageEmpty = false;

    if (needsTopUp)
    {
        storageEmpty = !TopUpReactors();
    }

    // Determine status and select timer
    string status;
    IMyTimerBlock emoteTimer;

    if (avgUranium > reactorWarnAt)
    {
        status = "Chillin";
        emoteTimer = reactorHappyTimer;
    }
    else if (avgUranium > reactorMin)
    {
        status = "Getting Low - Mine Soon";
        emoteTimer = reactorWarningTimer;
    }
    else
    {
        if (storageEmpty)
        {
            status = "GO MINE YOU FOOL";
            emoteTimer = reactorCriticalTimer;
        }
        else
        {
            status = "Getting Low - Mine Soon";
            emoteTimer = reactorWarningTimer;
        }
    }

    // Update LCD
    UpdateReactorLCD(reactorCount, avgUranium, status);

    // Update emoticon via timer
    TriggerEmoteTimer(emoteTimer);
}

void EnsureReactorBlockCache()
{
    if (reactorBlocksCached) return;

    // Get LCD by name
    reactorStatusLCD = GridTerminalSystem.GetBlockWithName(reactorStatusLCDName) as IMyTextPanel;

    // Get timer blocks for emote control
    reactorHappyTimer = GridTerminalSystem.GetBlockWithName(reactorHappyTimerName) as IMyTimerBlock;
    reactorWarningTimer = GridTerminalSystem.GetBlockWithName(reactorWarningTimerName) as IMyTimerBlock;
    reactorCriticalTimer = GridTerminalSystem.GetBlockWithName(reactorCriticalTimerName) as IMyTimerBlock;

    // Get source cargo from group
    reactorSourceCargo.Clear();
    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(reactorSourceGroupName);
    if (group != null)
    {
        group.GetBlocksOfType(reactorSourceCargo);
    }

    reactorBlocksCached = true;

    // Debug output
    Echo($"LCD '{reactorStatusLCDName}': {(reactorStatusLCD != null ? "Found" : "NOT FOUND")}");
    Echo($"Happy timer: {(reactorHappyTimer != null ? "Found" : "NOT FOUND")}");
    Echo($"Warning timer: {(reactorWarningTimer != null ? "Found" : "NOT FOUND")}");
    Echo($"Critical timer: {(reactorCriticalTimer != null ? "Found" : "NOT FOUND")}");
    Echo($"Source group '{reactorSourceGroupName}': {reactorSourceCargo.Count} containers");
}

MyFixedPoint GetUraniumInReactor(IMyReactor reactor)
{
    IMyInventory inventory = reactor.GetInventory(0);
    if (inventory == null) return 0;

    List<MyInventoryItem> items = new List<MyInventoryItem>();
    inventory.GetItems(items);

    for (int i = 0; i < items.Count; i++)
    {
        if (items[i].Type.SubtypeId == "Uranium")
        {
            return items[i].Amount;
        }
    }

    return 0;
}

bool TopUpReactors()
{
    // Returns false if storage is empty
    for (int i = 0; i < reactors.Count; i++)
    {
        IMyReactor reactor = reactors[i];
        if (reactor == null || !reactor.IsFunctional) continue;

        MyFixedPoint current = GetUraniumInReactor(reactor);
        MyFixedPoint needed = (MyFixedPoint)reactorMax - current;

        if (needed <= 0) continue;

        // Try each source cargo until we get enough
        for (int j = 0; j < reactorSourceCargo.Count; j++)
        {
            IMyCargoContainer source = reactorSourceCargo[j];
            if (source == null || !source.IsFunctional) continue;

            MyFixedPoint transferred = TransferUranium(source, reactor, needed);
            if (transferred > 0)
            {
                needed -= transferred;
            }

            if (needed <= 0) break;
        }
    }

    // Check if there's any uranium left in storage
    for (int i = 0; i < reactorSourceCargo.Count; i++)
    {
        IMyCargoContainer source = reactorSourceCargo[i];
        if (source == null) continue;

        if (GetUraniumInCargo(source) > 0)
        {
            return true; // Storage not empty
        }
    }

    return false; // Storage empty
}

MyFixedPoint TransferUranium(IMyCargoContainer source, IMyReactor reactor, MyFixedPoint amount)
{
    IMyInventory sourceInv = source.GetInventory(0);
    IMyInventory reactorInv = reactor.GetInventory(0);

    if (sourceInv == null || reactorInv == null) return 0;

    List<MyInventoryItem> items = new List<MyInventoryItem>();
    sourceInv.GetItems(items);

    for (int i = 0; i < items.Count; i++)
    {
        if (items[i].Type.SubtypeId == "Uranium")
        {
            MyFixedPoint available = items[i].Amount;
            MyFixedPoint toTransfer = MyFixedPoint.Min(amount, available);

            if (sourceInv.TransferItemTo(reactorInv, items[i], toTransfer))
            {
                return toTransfer;
            }
        }
    }

    return 0;
}

MyFixedPoint GetUraniumInCargo(IMyCargoContainer cargo)
{
    IMyInventory inventory = cargo.GetInventory(0);
    if (inventory == null) return 0;

    List<MyInventoryItem> items = new List<MyInventoryItem>();
    inventory.GetItems(items);

    for (int i = 0; i < items.Count; i++)
    {
        if (items[i].Type.SubtypeId == "Uranium")
        {
            return items[i].Amount;
        }
    }

    return 0;
}

void UpdateReactorLCD(int reactorCount, int avgUranium, string status)
{
    if (reactorStatusLCD == null) return;

    sb.Clear();
    sb.AppendLine($"Reactors: {reactorCount} | Avg: {avgUranium} U");
    sb.AppendLine($"Status: {status}");

    reactorStatusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
    reactorStatusLCD.WriteText(sb.ToString());
}

void TriggerEmoteTimer(IMyTimerBlock timer)
{
    if (timer == null) return;
    timer.Trigger();
}

// ============================================================
// MODULE: QUOTA MANAGER
// ============================================================

void RunQuotaManager()
{
    if (!enableQuotaManager) return;
    if (quotaTargets.Count == 0) return;

    EnsureQuotaBlockCache();

    // Count current inventory
    inventoryCounts.Clear();
    CountInventory();

    // Find an assembler to use (first one in assembly mode)
    IMyAssembler targetAssembler = null;
    for (int i = 0; i < assemblers.Count; i++)
    {
        IMyAssembler asm = assemblers[i];
        if (asm != null && asm.IsFunctional && asm.Mode == MyAssemblerMode.Assembly)
        {
            targetAssembler = asm;
            break;
        }
    }

    if (targetAssembler == null) return;

    // Build status output and queue production
    sb.Clear();
    sb.AppendLine("=== Quota Status ===");

    bool allMet = true;
    bool anyCanProduce = false;
    bool anyBlocked = false;

    foreach (var kvp in quotaTargets)
    {
        string itemName = kvp.Key;
        int target = kvp.Value;

        MyFixedPoint current = 0;
        if (inventoryCounts.ContainsKey(itemName))
        {
            current = inventoryCounts[itemName];
        }

        int currentInt = (int)(float)current;
        int needed = target - currentInt;

        if (needed <= 0)
        {
            // Quota met
            sb.AppendLine($"{itemName}: {currentInt}/{target} [OK]");
            continue;
        }

        allMet = false;

        // Check if we have required materials
        string missingMaterial = GetMissingMaterial(itemName);

        if (missingMaterial != null)
        {
            // Can't produce - missing materials
            sb.AppendLine($"{itemName}: {currentInt}/{target} [NEED {missingMaterial}]");
            anyBlocked = true;
        }
        else
        {
            // Can produce - queue it
            int queued = QueueProduction(targetAssembler, itemName, needed);
            if (queued > 0)
            {
                sb.AppendLine($"{itemName}: {currentInt}/{target} [QUEUED]");
                anyCanProduce = true;
            }
            else
            {
                sb.AppendLine($"{itemName}: {currentInt}/{target} [NO BLUEPRINT]");
                anyBlocked = true;
            }
        }
    }

    // Clear assembler outputs to cargo
    ClearAssemblerOutputs();

    // Update status LCD with color
    if (quotaStatusLCD != null)
    {
        quotaStatusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
        quotaStatusLCD.WriteText(sb.ToString());

        // Set background color based on status
        if (allMet)
        {
            // GREEN - all quotas met
            quotaStatusLCD.BackgroundColor = new Color(0, 80, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
        else if (anyCanProduce && !anyBlocked)
        {
            // YELLOW - all needed items are queued and producing
            quotaStatusLCD.BackgroundColor = new Color(120, 100, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
        else if (anyCanProduce)
        {
            // ORANGE - some producing, some blocked
            quotaStatusLCD.BackgroundColor = new Color(120, 60, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
        else
        {
            // RED - nothing can be produced
            quotaStatusLCD.BackgroundColor = new Color(120, 0, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
    }
}

void EnsureQuotaBlockCache()
{
    if (quotaBlocksCached) return;

    quotaStatusLCD = GridTerminalSystem.GetBlockWithName(quotaStatusLCDName) as IMyTextPanel;

    quotaBlocksCached = true;

    Echo($"Quota Status LCD '{quotaStatusLCDName}': {(quotaStatusLCD != null ? "Found" : "NOT FOUND")}");
    Echo($"Quotas configured: {quotaTargets.Count}");
}

string NormalizeItemName(string input)
{
    // Handle common variations users might type
    string lower = input.ToLower().Trim();

    // Construction Component variations
    if (lower == "construction component" || lower == "construction comp" || lower == "construction")
        return "Construction Comp";

    // Steel Plate variations
    if (lower == "steel plate" || lower == "steelplate" || lower == "steel")
        return "Steel Plate";

    // Interior Plate variations
    if (lower == "interior plate" || lower == "interiorplate" || lower == "interior")
        return "Interior Plate";

    // Computer variations
    if (lower == "computer" || lower == "computer component" || lower == "computers")
        return "Computer";

    // Motor variations
    if (lower == "motor" || lower == "motor component" || lower == "motors")
        return "Motor";

    // Metal Grid variations
    if (lower == "metal grid" || lower == "metalgrid")
        return "Metal Grid";

    // Display variations
    if (lower == "display" || lower == "displays")
        return "Display";

    // Bulletproof Glass variations
    if (lower == "bulletproof glass" || lower == "bulletproofglass" || lower == "glass")
        return "Bulletproof Glass";

    // Girder variations
    if (lower == "girder" || lower == "girders" || lower == "girder component")
        return "Girder";

    // Small/Large Tube variations
    if (lower == "small tube" || lower == "smalltube" || lower == "small steel tube")
        return "Small Tube";
    if (lower == "large tube" || lower == "largetube" || lower == "large steel tube")
        return "Large Tube";

    // Detector variations
    if (lower == "detector" || lower == "detector component" || lower == "detectors")
        return "Detector";

    // Radio Comm variations
    if (lower == "radio comm" || lower == "radio communication" || lower == "radio component" || lower == "radio")
        return "Radio Comm";

    // Medical variations
    if (lower == "medical" || lower == "medical component" || lower == "medical comp")
        return "Medical";

    // Reactor Comp variations
    if (lower == "reactor comp" || lower == "reactor component" || lower == "reactor")
        return "Reactor Comp";

    // Thruster Comp variations
    if (lower == "thruster comp" || lower == "thruster component" || lower == "thrust component" || lower == "thruster")
        return "Thruster Comp";

    // Gravity Comp variations
    if (lower == "gravity comp" || lower == "gravity component" || lower == "gravity generator component" || lower == "gravity")
        return "Gravity Comp";

    // Solar Cell variations
    if (lower == "solar cell" || lower == "solarcell" || lower == "solar")
        return "Solar Cell";

    // Power Cell variations
    if (lower == "power cell" || lower == "powercell" || lower == "power")
        return "Power Cell";

    // Superconductor variations
    if (lower == "superconductor" || lower == "superconductor component" || lower == "super conductor")
        return "Superconductor";

    // Explosives variations
    if (lower == "explosives" || lower == "explosive" || lower == "explosives component")
        return "Explosives";

    // If no match, return original with title case
    return input;
}

void CountInventory()
{
    // Count items in all cargo containers on main grid
    for (int i = 0; i < cargoContainers.Count; i++)
    {
        IMyCargoContainer cargo = cargoContainers[i];
        if (cargo == null || !cargo.IsFunctional) continue;

        IMyInventory inv = cargo.GetInventory(0);
        if (inv == null) continue;

        quotaItemBuffer.Clear();
        inv.GetItems(quotaItemBuffer);

        for (int j = 0; j < quotaItemBuffer.Count; j++)
        {
            MyInventoryItem item = quotaItemBuffer[j];
            string displayName = GetItemDisplayName(item.Type);

            if (inventoryCounts.ContainsKey(displayName))
            {
                inventoryCounts[displayName] += item.Amount;
            }
            else
            {
                inventoryCounts[displayName] = item.Amount;
            }
        }
    }

    // Also count assembler output inventories - ONLY assemblers in Assembly mode
    // Don't count disassemblers as those items are being destroyed
    for (int i = 0; i < assemblers.Count; i++)
    {
        IMyAssembler asm = assemblers[i];
        if (asm == null || !asm.IsFunctional) continue;
        if (asm.Mode != MyAssemblerMode.Assembly) continue; // Skip disassemblers

        IMyInventory inv = asm.GetInventory(1); // Output inventory
        if (inv == null) continue;

        quotaItemBuffer.Clear();
        inv.GetItems(quotaItemBuffer);

        for (int j = 0; j < quotaItemBuffer.Count; j++)
        {
            MyInventoryItem item = quotaItemBuffer[j];
            string displayName = GetItemDisplayName(item.Type);

            if (inventoryCounts.ContainsKey(displayName))
            {
                inventoryCounts[displayName] += item.Amount;
            }
            else
            {
                inventoryCounts[displayName] = item.Amount;
            }
        }
    }
}

string GetItemDisplayName(MyItemType itemType)
{
    // Convert SubtypeId to display name
    string subtype = itemType.SubtypeId;
    string typeId = itemType.TypeId;

    // Ingots
    if (typeId.EndsWith("Ingot"))
    {
        switch (subtype)
        {
            case "Iron": return "Iron Ingot";
            case "Nickel": return "Nickel Ingot";
            case "Cobalt": return "Cobalt Ingot";
            case "Silicon": return "Silicon Wafer";
            case "Silver": return "Silver Ingot";
            case "Gold": return "Gold Ingot";
            case "Platinum": return "Platinum Ingot";
            case "Uranium": return "Uranium Ingot";
            case "Magnesium": return "Magnesium Powder";
            case "Stone": return "Gravel";
            default: return subtype + " Ingot";
        }
    }

    // Components - SubtypeIds are the ITEM names, not blueprint names
    switch (subtype)
    {
        case "Construction": return "Construction Comp";
        case "Computer": return "Computer";
        case "MetalGrid": return "Metal Grid";
        case "SteelPlate": return "Steel Plate";
        case "InteriorPlate": return "Interior Plate";
        case "SmallTube": return "Small Tube";
        case "LargeTube": return "Large Tube";
        case "Motor": return "Motor";
        case "Display": return "Display";
        case "BulletproofGlass": return "Bulletproof Glass";
        case "Girder": return "Girder";
        case "RadioCommunication": return "Radio Comm";
        case "Detector": return "Detector";
        case "SolarCell": return "Solar Cell";
        case "PowerCell": return "Power Cell";
        case "Medical": return "Medical";
        case "GravityGenerator": return "Gravity Comp";
        case "Superconductor": return "Superconductor";
        case "Thrust": return "Thruster Comp";
        case "Reactor": return "Reactor Comp";
        case "Explosives": return "Explosives";
        case "Canvas": return "Canvas";
        case "ZoneChip": return "Zone Chip";
        default: return subtype;
    }
}

string GetBlueprintSubtype(string displayName)
{
    // Convert display name back to blueprint subtype
    // Vanilla components have specific blueprint names
    switch (displayName)
    {
        case "Construction Comp": return "ConstructionComponent";
        case "Computer": return "ComputerComponent";
        case "Metal Grid": return "MetalGrid";
        case "Steel Plate": return "SteelPlate";
        case "Interior Plate": return "InteriorPlate";
        case "Small Tube": return "SmallTube";
        case "Large Tube": return "LargeTube";
        case "Motor": return "MotorComponent";
        case "Bulletproof Glass": return "BulletproofGlass";
        case "Girder": return "GirderComponent";
        case "Radio Comm": return "RadioCommunicationComponent";
        case "Detector": return "DetectorComponent";
        case "Solar Cell": return "SolarCell";
        case "Power Cell": return "PowerCell";
        case "Medical": return "MedicalComponent";
        case "Gravity Comp": return "GravityGeneratorComponent";
        case "Superconductor": return "SuperconductorComponent";
        case "Thruster Comp": return "ThrustComponent";
        case "Reactor Comp": return "ReactorComponent";
        case "Explosives": return "ExplosivesComponent";
        case "Canvas": return "Canvas";
        case "Zone Chip": return "ZoneChip";
        default:
            // For mods or unknown items, try the name as-is first
            // Most mods use the same name for item and blueprint
            return displayName.Replace(" ", "");
    }
}

string GetMissingMaterial(string componentName)
{
    // Returns the first missing required material, or null if all present
    string[] required = GetRequiredMaterials(componentName);
    if (required == null) return null;

    for (int i = 0; i < required.Length; i++)
    {
        string material = required[i];
        bool found = false;

        // Check if this material exists in inventory
        foreach (var kvp in inventoryCounts)
        {
            if (kvp.Key == material && (float)kvp.Value > 0)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            return material.ToUpper();
        }
    }

    return null;
}

string[] GetRequiredMaterials(string componentName)
{
    // Returns array of required ingot display names for each component
    switch (componentName)
    {
        case "Steel Plate":
        case "Interior Plate":
        case "Construction Comp":
            return new string[] { "Iron Ingot" };

        case "Computer":
        case "Display":
        case "Radio Comm":
            return new string[] { "Iron Ingot", "Silicon Wafer" };

        case "Motor":
        case "Detector":
            return new string[] { "Iron Ingot", "Nickel Ingot" };

        case "Metal Grid":
            return new string[] { "Iron Ingot", "Nickel Ingot", "Cobalt Ingot" };

        case "Bulletproof Glass":
            return new string[] { "Silicon Wafer" };

        case "Reactor Comp":
            return new string[] { "Iron Ingot", "Gravel", "Silver Ingot" };

        case "Thruster Comp":
            return new string[] { "Iron Ingot", "Cobalt Ingot", "Gold Ingot", "Platinum Ingot" };

        case "Gravity Comp":
            return new string[] { "Iron Ingot", "Cobalt Ingot", "Gold Ingot", "Silver Ingot" };

        case "Medical":
            return new string[] { "Iron Ingot", "Nickel Ingot", "Silver Ingot" };

        case "Explosives":
            return new string[] { "Silicon Wafer", "Magnesium Powder" };

        case "Solar Cell":
            return new string[] { "Silicon Wafer", "Nickel Ingot" };

        case "Power Cell":
            return new string[] { "Iron Ingot", "Nickel Ingot", "Silicon Wafer" };

        case "Superconductor":
            return new string[] { "Iron Ingot", "Gold Ingot" };

        case "Girder":
            return new string[] { "Iron Ingot" };

        case "Small Tube":
        case "Large Tube":
            return new string[] { "Iron Ingot" };

        default:
            // Unknown component - assume iron
            return new string[] { "Iron Ingot" };
    }
}

void ClearAssemblerOutputs()
{
    // Move items from assembler output inventories to cargo containers
    // Skip uranium storage group, only use main grid cargo
    for (int i = 0; i < assemblers.Count; i++)
    {
        IMyAssembler asm = assemblers[i];
        if (asm == null || !asm.IsFunctional) continue;
        if (asm.Mode != MyAssemblerMode.Assembly) continue;

        IMyInventory outputInv = asm.GetInventory(1);
        if (outputInv == null || outputInv.ItemCount == 0) continue;

        // Find a cargo container to transfer to (not in uranium storage group)
        for (int j = 0; j < cargoContainers.Count; j++)
        {
            IMyCargoContainer cargo = cargoContainers[j];
            if (cargo == null || !cargo.IsFunctional) continue;

            // Skip if this cargo is in the uranium storage group
            if (IsInUraniumStorage(cargo)) continue;

            IMyInventory cargoInv = cargo.GetInventory(0);
            if (cargoInv == null) continue;

            // Transfer all items from assembler output to cargo
            quotaItemBuffer.Clear();
            outputInv.GetItems(quotaItemBuffer);

            for (int k = quotaItemBuffer.Count - 1; k >= 0; k--)
            {
                MyInventoryItem item = quotaItemBuffer[k];
                if (outputInv.TransferItemTo(cargoInv, item, item.Amount))
                {
                    // Successfully transferred, continue to next item
                }
            }

            // If output is empty, we're done with this assembler
            if (outputInv.ItemCount == 0) break;
        }
    }
}

bool IsInUraniumStorage(IMyCargoContainer cargo)
{
    // Check if this cargo is in the reactor source group
    for (int i = 0; i < reactorSourceCargo.Count; i++)
    {
        if (reactorSourceCargo[i] == cargo) return true;
    }
    return false;
}

int QueueProduction(IMyAssembler assembler, string displayName, int amount)
{
    // Returns: amount already queued + amount newly queued (0 if nothing in progress)
    string blueprintSubtype = GetBlueprintSubtype(displayName);

    // Try to parse the blueprint ID
    MyDefinitionId blueprintId;
    if (!MyDefinitionId.TryParse($"MyObjectBuilder_BlueprintDefinition/{blueprintSubtype}", out blueprintId))
    {
        return 0;
    }

    // Check if assembler can use this blueprint
    if (!assembler.CanUseBlueprint(blueprintId))
    {
        return 0;
    }

    // Check ALL assemblers' queues to avoid spamming (cooperative mode shares work)
    MyFixedPoint alreadyQueued = 0;
    for (int i = 0; i < assemblers.Count; i++)
    {
        IMyAssembler asm = assemblers[i];
        if (asm == null || !asm.IsFunctional) continue;
        if (asm.Mode != MyAssemblerMode.Assembly) continue; // Skip disassemblers

        quotaQueueBuffer.Clear();
        asm.GetQueue(quotaQueueBuffer);

        for (int j = 0; j < quotaQueueBuffer.Count; j++)
        {
            if (quotaQueueBuffer[j].BlueprintId == blueprintId)
            {
                alreadyQueued += quotaQueueBuffer[j].Amount;
            }
        }
    }

    int alreadyQueuedInt = (int)(float)alreadyQueued;
    int toQueue = amount - alreadyQueuedInt;

    if (toQueue > 0)
    {
        assembler.AddQueueItem(blueprintId, (MyFixedPoint)toQueue);
        return alreadyQueuedInt + toQueue;
    }

    return alreadyQueuedInt;
}

// ============================================================
// MODULE: DOCK & YOINK
// ============================================================

void RunDockYoink()
{
    if (!enableDockYoink) return;

    EnsureYoinkBlockCache();

    if (yoinkConnector == null) return;

    // Check current connection status
    bool isConnected = yoinkConnector.Status == MyShipConnectorStatus.Connected;

    // Only yoink on NEW connection (transition from disconnected to connected)
    if (isConnected && !yoinkWasConnected)
    {
        // New connection detected - yoink everything!
        YoinkVisitorCargo();
    }

    // Update state for next tick
    yoinkWasConnected = isConnected;
}

void EnsureYoinkBlockCache()
{
    if (yoinkBlocksCached) return;

    // Find the yoink connector by name
    yoinkConnector = GridTerminalSystem.GetBlockWithName(yoinkConnectorName) as IMyShipConnector;

    // Get target cargo from group
    yoinkTargetCargo.Clear();
    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(yoinkTargetGroupName);
    if (group != null)
    {
        group.GetBlocksOfType(yoinkTargetCargo);
    }

    yoinkBlocksCached = true;

    Echo($"Yoink connector '{yoinkConnectorName}': {(yoinkConnector != null ? "Found" : "NOT FOUND")}");
    Echo($"Yoink target group '{yoinkTargetGroupName}': {yoinkTargetCargo.Count} containers");
}

void YoinkVisitorCargo()
{
    if (yoinkConnector == null) return;
    if (yoinkConnector.Status != MyShipConnectorStatus.Connected) return;

    // Get the other connector (visitor's)
    IMyShipConnector otherConnector = yoinkConnector.OtherConnector;
    if (otherConnector == null) return;

    // Get the visitor's grid
    IMyCubeGrid visitorGrid = otherConnector.CubeGrid;
    if (visitorGrid == null) return;

    // Build list of all inventories to yoink from
    List<IMyInventory> visitorInventories = new List<IMyInventory>();

    // Get cargo containers on visitor grid
    List<IMyCargoContainer> visitorCargo = new List<IMyCargoContainer>();
    GridTerminalSystem.GetBlocksOfType(visitorCargo, b => b.CubeGrid == visitorGrid);
    for (int i = 0; i < visitorCargo.Count; i++)
    {
        IMyInventory inv = visitorCargo[i].GetInventory(0);
        if (inv != null) visitorInventories.Add(inv);
    }

    // Get connectors on visitor grid (items get stuck here due to small conveyor restrictions)
    List<IMyShipConnector> visitorConnectors = new List<IMyShipConnector>();
    GridTerminalSystem.GetBlocksOfType(visitorConnectors, b => b.CubeGrid == visitorGrid);
    for (int i = 0; i < visitorConnectors.Count; i++)
    {
        IMyInventory inv = visitorConnectors[i].GetInventory(0);
        if (inv != null) visitorInventories.Add(inv);
    }

    // Also check drills, welders, grinders - common places for items to get stuck
    List<IMyShipDrill> visitorDrills = new List<IMyShipDrill>();
    GridTerminalSystem.GetBlocksOfType(visitorDrills, b => b.CubeGrid == visitorGrid);
    for (int i = 0; i < visitorDrills.Count; i++)
    {
        IMyInventory inv = visitorDrills[i].GetInventory(0);
        if (inv != null) visitorInventories.Add(inv);
    }

    List<IMyShipWelder> visitorWelders = new List<IMyShipWelder>();
    GridTerminalSystem.GetBlocksOfType(visitorWelders, b => b.CubeGrid == visitorGrid);
    for (int i = 0; i < visitorWelders.Count; i++)
    {
        IMyInventory inv = visitorWelders[i].GetInventory(0);
        if (inv != null) visitorInventories.Add(inv);
    }

    List<IMyShipGrinder> visitorGrinders = new List<IMyShipGrinder>();
    GridTerminalSystem.GetBlocksOfType(visitorGrinders, b => b.CubeGrid == visitorGrid);
    for (int i = 0; i < visitorGrinders.Count; i++)
    {
        IMyInventory inv = visitorGrinders[i].GetInventory(0);
        if (inv != null) visitorInventories.Add(inv);
    }

    if (visitorInventories.Count == 0) return;

    int itemsTransferred = 0;

    // Transfer from each visitor inventory to our storage
    for (int i = 0; i < visitorInventories.Count; i++)
    {
        IMyInventory sourceInv = visitorInventories[i];
        if (sourceInv == null || sourceInv.ItemCount == 0) continue;

        // Get items from this source
        yoinkItemBuffer.Clear();
        sourceInv.GetItems(yoinkItemBuffer);

        // Transfer each item to our storage
        for (int j = yoinkItemBuffer.Count - 1; j >= 0; j--)
        {
            MyInventoryItem item = yoinkItemBuffer[j];

            // Find a target container with space
            for (int k = 0; k < yoinkTargetCargo.Count; k++)
            {
                IMyCargoContainer target = yoinkTargetCargo[k];
                if (target == null || !target.IsFunctional) continue;

                IMyInventory targetInv = target.GetInventory(0);
                if (targetInv == null) continue;

                // Check if target can accept this item
                if (!targetInv.CanItemsBeAdded(item.Amount, item.Type)) continue;

                // Transfer the item
                if (sourceInv.TransferItemTo(targetInv, item, item.Amount))
                {
                    itemsTransferred++;
                    break; // Item transferred, move to next item
                }
            }
        }
    }

    if (itemsTransferred > 0)
    {
        Echo($"Yoinked {itemsTransferred} item stacks from visitor!");
    }
}

// ============================================================
// MODULE: INVENTORY DISPLAY
// ============================================================

void RunInventoryDisplay()
{
    if (!enableInventoryDisplay) return;

    EnsureInventoryBlockCache();

    if (inventoryDisplayLCD == null) return;

    // Clear and count all inventory
    inventoryTotals.Clear();
    CountAllInventory();

    // Sort the inventory
    inventorySortBuffer.Clear();
    foreach (var kvp in inventoryTotals)
    {
        // Skip zero amounts unless ShowZero is enabled
        if (!inventoryShowZero && (float)kvp.Value <= 0) continue;
        inventorySortBuffer.Add(kvp);
    }

    // Sort based on config
    if (inventorySortBy == "amount")
    {
        // Sort by amount descending (most items first)
        inventorySortBuffer.Sort((a, b) => ((float)b.Value).CompareTo((float)a.Value));
    }
    else
    {
        // Sort by name alphabetically
        inventorySortBuffer.Sort((a, b) => a.Key.CompareTo(b.Key));
    }

    // Build display output
    sb.Clear();
    sb.AppendLine("=== Main Grid Inventory ===");
    sb.AppendLine();

    for (int i = 0; i < inventorySortBuffer.Count; i++)
    {
        string name = inventorySortBuffer[i].Key;
        int amount = (int)(float)inventorySortBuffer[i].Value;
        sb.AppendLine($"{name}: {FormatNumber(amount)}");
    }

    // Write to LCD
    inventoryDisplayLCD.ContentType = ContentType.TEXT_AND_IMAGE;
    inventoryDisplayLCD.WriteText(sb.ToString());
}

void EnsureInventoryBlockCache()
{
    if (inventoryBlocksCached) return;

    inventoryDisplayLCD = GridTerminalSystem.GetBlockWithName(inventoryDisplayLCDName) as IMyTextPanel;

    inventoryBlocksCached = true;

    Echo($"Inventory LCD '{inventoryDisplayLCDName}': {(inventoryDisplayLCD != null ? "Found" : "NOT FOUND")}");
}

void CountAllInventory()
{
    // Count items in all cargo containers
    for (int i = 0; i < cargoContainers.Count; i++)
    {
        IMyCargoContainer cargo = cargoContainers[i];
        if (cargo == null || !cargo.IsFunctional) continue;

        CountInventoryFrom(cargo.GetInventory(0));
    }

    // Also count assembler inventories (both input and output)
    for (int i = 0; i < assemblers.Count; i++)
    {
        IMyAssembler asm = assemblers[i];
        if (asm == null || !asm.IsFunctional) continue;

        CountInventoryFrom(asm.GetInventory(0)); // Input
        CountInventoryFrom(asm.GetInventory(1)); // Output
    }

    // Count reactor inventories (uranium)
    for (int i = 0; i < reactors.Count; i++)
    {
        IMyReactor reactor = reactors[i];
        if (reactor == null || !reactor.IsFunctional) continue;

        CountInventoryFrom(reactor.GetInventory(0));
    }

    // Count connector inventories
    for (int i = 0; i < connectors.Count; i++)
    {
        IMyShipConnector conn = connectors[i];
        if (conn == null || !conn.IsFunctional) continue;
        if (conn.CubeGrid != Me.CubeGrid) continue; // Main grid only

        CountInventoryFrom(conn.GetInventory(0));
    }
}

void CountInventoryFrom(IMyInventory inv)
{
    if (inv == null || inv.ItemCount == 0) return;

    inventoryItemBuffer.Clear();
    inv.GetItems(inventoryItemBuffer);

    for (int i = 0; i < inventoryItemBuffer.Count; i++)
    {
        MyInventoryItem item = inventoryItemBuffer[i];
        string displayName = GetItemDisplayName(item.Type);

        if (inventoryTotals.ContainsKey(displayName))
        {
            inventoryTotals[displayName] += item.Amount;
        }
        else
        {
            inventoryTotals[displayName] = item.Amount;
        }
    }
}

string FormatNumber(int number)
{
    // Format with thousand separators: 1234567 -> 1,234,567
    if (number < 1000) return number.ToString();

    string result = "";
    int remaining = number;

    while (remaining > 0)
    {
        int chunk = remaining % 1000;
        remaining = remaining / 1000;

        if (result.Length == 0)
        {
            // Rightmost chunk - pad it (there's always more since number >= 1000)
            result = chunk.ToString().PadLeft(3, '0');
        }
        else if (remaining > 0)
        {
            // Middle chunk - pad with zeros, add comma
            result = chunk.ToString().PadLeft(3, '0') + "," + result;
        }
        else
        {
            // Leftmost chunk - no leading zeros, add comma
            result = chunk.ToString() + "," + result;
        }
    }

    return result;
}

// ============================================================
// MODULE: GAS KEEPER
// ============================================================

void RunGasKeeper()
{
    if (!enableGasKeeper) return;

    EnsureGasBlockCache();

    // Check for stale tank references and auto-refresh if needed
    int validH2 = CountValidTanks(h2Tanks);
    int validO2 = CountValidTanks(o2Tanks);

    if (validH2 != h2Tanks.Count || validO2 != o2Tanks.Count)
    {
        gasBlocksCached = false;
        EnsureGasBlockCache();
    }

    // Calculate average fill percentages
    double h2Percent = CalculateAverageGasLevel(h2Tanks);
    double o2Percent = CalculateAverageGasLevel(o2Tanks);

    // Determine status for each gas
    string h2Status = GetGasStatus(h2Percent, gasH2WarnPercent, gasH2CriticalPercent);
    string o2Status = GetGasStatus(o2Percent, gasO2WarnPercent, gasO2CriticalPercent);

    // Determine overall status (worst of the two)
    bool isCritical = h2Status == "CRITICAL" || o2Status == "CRITICAL";
    bool isWarning = h2Status == "WARNING" || o2Status == "WARNING";

    // Select timer for emote
    IMyTimerBlock emoteTimer;
    if (isCritical)
    {
        emoteTimer = gasCriticalTimer;
    }
    else if (isWarning)
    {
        emoteTimer = gasWarningTimer;
    }
    else
    {
        emoteTimer = gasHappyTimer;
    }

    // Trigger emote timer
    if (emoteTimer != null)
    {
        emoteTimer.Trigger();
    }

    // Update LCD
    UpdateGasLCD(h2Percent, h2Status, o2Percent, o2Status, isCritical, isWarning);
}

void EnsureGasBlockCache()
{
    if (gasBlocksCached) return;

    // Find all gas tanks on main grid and separate by type
    h2Tanks.Clear();
    o2Tanks.Clear();

    List<IMyGasTank> allTanks = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType(allTanks, b => b.CubeGrid == Me.CubeGrid);

    for (int i = 0; i < allTanks.Count; i++)
    {
        IMyGasTank tank = allTanks[i];
        string subtypeId = tank.BlockDefinition.SubtypeId;

        // Hydrogen tanks clearly have "Hydrogen" in SubtypeId
        // Everything else (including default/empty SubtypeId) is assumed to be Oxygen
        if (subtypeId.Contains("Hydrogen"))
        {
            h2Tanks.Add(tank);
        }
        else
        {
            o2Tanks.Add(tank);
        }
    }

    // Get LCD and timers
    gasStatusLCD = GridTerminalSystem.GetBlockWithName(gasStatusLCDName) as IMyTextPanel;
    gasHappyTimer = GridTerminalSystem.GetBlockWithName(gasHappyTimerName) as IMyTimerBlock;
    gasWarningTimer = GridTerminalSystem.GetBlockWithName(gasWarningTimerName) as IMyTimerBlock;
    gasCriticalTimer = GridTerminalSystem.GetBlockWithName(gasCriticalTimerName) as IMyTimerBlock;

    gasBlocksCached = true;

    Echo($"Gas LCD '{gasStatusLCDName}': {(gasStatusLCD != null ? "Found" : "NOT FOUND")}");
    Echo($"H2 Tanks: {h2Tanks.Count}");
    Echo($"O2 Tanks: {o2Tanks.Count}");
}

int CountValidTanks(List<IMyGasTank> tanks)
{
    int count = 0;
    for (int i = 0; i < tanks.Count; i++)
    {
        IMyGasTank tank = tanks[i];
        if (tank == null || tank.Closed || !tank.IsFunctional) continue;
        if (tank.CubeGrid != Me.CubeGrid) continue;
        count++;
    }
    return count;
}

double CalculateAverageGasLevel(List<IMyGasTank> tanks)
{
    if (tanks.Count == 0) return 100.0; // No tanks = assume OK

    double totalRatio = 0;
    int validTanks = 0;

    for (int i = 0; i < tanks.Count; i++)
    {
        IMyGasTank tank = tanks[i];
        if (tank == null || tank.Closed || !tank.IsFunctional) continue;
        if (tank.CubeGrid != Me.CubeGrid) continue;

        totalRatio += tank.FilledRatio;
        validTanks++;
    }

    if (validTanks == 0) return 100.0;

    return (totalRatio / validTanks) * 100.0;
}

string GetGasStatus(double percent, int warnThreshold, int criticalThreshold)
{
    if (percent <= criticalThreshold) return "CRITICAL";
    if (percent <= warnThreshold) return "WARNING";
    return "OK";
}

void UpdateGasLCD(double h2Percent, string h2Status, double o2Percent, string o2Status, bool isCritical, bool isWarning)
{
    if (gasStatusLCD == null) return;

    sb.Clear();
    sb.AppendLine("=== Gas Status ===");
    sb.AppendLine();

    // Hydrogen line
    if (h2Tanks.Count > 0)
    {
        sb.AppendLine($"Hydrogen: {h2Percent:F0}% [{h2Status}]");
    }
    else
    {
        sb.AppendLine("Hydrogen: No tanks");
    }

    // Oxygen line
    if (o2Tanks.Count > 0)
    {
        sb.AppendLine($"Oxygen: {o2Percent:F0}% [{o2Status}]");
    }
    else
    {
        sb.AppendLine("Oxygen: No tanks");
    }

    // Write to LCD
    gasStatusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
    gasStatusLCD.WriteText(sb.ToString());

    // Set background color
    if (isCritical)
    {
        gasStatusLCD.BackgroundColor = new Color(120, 0, 0); // Red
    }
    else if (isWarning)
    {
        gasStatusLCD.BackgroundColor = new Color(120, 100, 0); // Yellow
    }
    else
    {
        gasStatusLCD.BackgroundColor = new Color(0, 80, 0); // Green
    }
    gasStatusLCD.FontColor = Color.White;
}

// ============================================================
// MODULE: BATTERY STATUS
// ============================================================

void RunBatteryStatus()
{
    if (!enableBatteryStatus) return;

    EnsureBatteryBlockCache();

    if (batteries.Count == 0) return;

    // Calculate totals - also count valid batteries
    float totalStored = 0f;
    float totalMax = 0f;
    float totalInput = 0f;
    float totalOutput = 0f;
    int validBatteryCount = 0;

    for (int i = 0; i < batteries.Count; i++)
    {
        IMyBatteryBlock battery = batteries[i];

        // Check if battery is still valid (exists and on our grid)
        if (battery == null || battery.Closed || !battery.IsFunctional) continue;
        if (battery.CubeGrid != Me.CubeGrid) continue;

        validBatteryCount++;
        totalStored += battery.CurrentStoredPower;
        totalMax += battery.MaxStoredPower;
        totalInput += battery.CurrentInput;
        totalOutput += battery.CurrentOutput;
    }

    // Auto-refresh cache if we lost batteries (stale references)
    if (validBatteryCount != batteries.Count)
    {
        batteryBlocksCached = false;
        EnsureBatteryBlockCache();
        // Recalculate with fresh data
        totalStored = 0f;
        totalMax = 0f;
        totalInput = 0f;
        totalOutput = 0f;
        validBatteryCount = 0;

        for (int i = 0; i < batteries.Count; i++)
        {
            IMyBatteryBlock battery = batteries[i];
            if (battery == null || battery.Closed || !battery.IsFunctional) continue;
            if (battery.CubeGrid != Me.CubeGrid) continue;

            validBatteryCount++;
            totalStored += battery.CurrentStoredPower;
            totalMax += battery.MaxStoredPower;
            totalInput += battery.CurrentInput;
            totalOutput += battery.CurrentOutput;
        }
    }

    // Calculate percentage and net flow
    float chargePercent = totalMax > 0 ? (totalStored / totalMax) * 100f : 0f;
    float netFlow = totalInput - totalOutput; // Positive = charging, Negative = draining

    // Calculate time estimates
    string timeEstimate = "";
    bool isCharging = netFlow > 0.001f;
    bool isDraining = netFlow < -0.001f;

    if (isCharging)
    {
        float remaining = totalMax - totalStored;
        float hoursToFull = remaining / netFlow;
        timeEstimate = FormatTime(hoursToFull);
    }
    else if (isDraining)
    {
        float hoursToEmpty = totalStored / (-netFlow);
        timeEstimate = FormatTime(hoursToEmpty);
    }

    // Update LCD
    UpdateBatteryLCD(validBatteryCount, chargePercent, totalStored, totalMax, netFlow, isCharging, isDraining, timeEstimate);
}

void EnsureBatteryBlockCache()
{
    if (batteryBlocksCached) return;

    batteries.Clear();
    GridTerminalSystem.GetBlocksOfType(batteries, b => b.CubeGrid == Me.CubeGrid);

    batteryStatusLCD = GridTerminalSystem.GetBlockWithName(batteryStatusLCDName) as IMyTextPanel;

    batteryBlocksCached = true;

    Echo($"Battery LCD '{batteryStatusLCDName}': {(batteryStatusLCD != null ? "Found" : "NOT FOUND")}");
    Echo($"Batteries: {batteries.Count}");
}

void UpdateBatteryLCD(int batteryCount, float chargePercent, float stored, float max, float netFlow, bool isCharging, bool isDraining, string timeEstimate)
{
    if (batteryStatusLCD == null) return;

    sb.Clear();
    sb.AppendLine("=== Battery Status ===");
    sb.AppendLine();
    sb.AppendLine($"Batteries: {batteryCount}");
    sb.AppendLine($"Charge: {chargePercent:F0}% ({stored:F1} / {max:F1} MWh)");
    sb.AppendLine();

    // Status line
    if (isCharging)
    {
        sb.AppendLine("Status: Charging");
        sb.AppendLine($"Net Flow: +{netFlow:F2} MW");
        sb.AppendLine($"Time to Full: {timeEstimate}");
    }
    else if (isDraining)
    {
        sb.AppendLine("Status: Draining");
        sb.AppendLine($"Net Flow: {netFlow:F2} MW");
        sb.AppendLine($"Time to Empty: {timeEstimate}");
    }
    else
    {
        sb.AppendLine("Status: Stable");
        sb.AppendLine("Net Flow: 0.00 MW");
    }

    // Write to LCD
    batteryStatusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
    batteryStatusLCD.WriteText(sb.ToString());

    // Set background color based on charge level and charging state
    if (chargePercent <= batteryCriticalPercent && !isCharging)
    {
        // Critical - low and draining
        batteryStatusLCD.BackgroundColor = new Color(120, 0, 0); // Red
    }
    else if (chargePercent <= batteryWarnPercent && !isCharging)
    {
        // Warning - low and draining
        batteryStatusLCD.BackgroundColor = new Color(120, 100, 0); // Yellow
    }
    else if (isDraining && chargePercent <= 50)
    {
        // Draining but not critical yet
        batteryStatusLCD.BackgroundColor = new Color(120, 100, 0); // Yellow
    }
    else
    {
        // Good - charging or high charge
        batteryStatusLCD.BackgroundColor = new Color(0, 80, 0); // Green
    }
    batteryStatusLCD.FontColor = Color.White;
}

string FormatTime(float hours)
{
    if (hours < 0 || float.IsInfinity(hours) || float.IsNaN(hours))
    {
        return "--";
    }

    int totalMinutes = (int)(hours * 60);

    if (totalMinutes < 60)
    {
        return $"{totalMinutes}m";
    }

    int h = totalMinutes / 60;
    int m = totalMinutes % 60;

    if (h > 99)
    {
        return "99h+";
    }

    return $"{h}h {m:D2}m";
}

// ============================================================
// UTILITY FUNCTIONS
// ============================================================
