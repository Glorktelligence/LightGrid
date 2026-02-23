// LightGrid - Lightweight Space Engineers Inventory Script
// Author: VoidSmasher (Glorktelligence)
// A lightweight alternative to ISY that doesn't hammer servers

// ============================================================
// ============================================================

const string DEFAULT_CONFIG = @"[LightGrid]
; Master enable/disable for modules
EnableReactorKeeper=true
EnableQuotaManager=true
EnableDockYoink=true
EnableInventoryDisplay=true
EnableGasBottleManager=true

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
EnableDisassembly=true
DisassemblerName=Quota Disassembler
ExcessThreshold=1.5

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
AutoFontSize=true
MaxFontSize=1.2
MinFontSize=0.5
CategoryHeaders=true
Columns=2

[GasBottleManager]
Enable=true
StatusLCD=Bottle Status LCD
H2O2Generator=
BottleStorage=
H2BottleMin=25
H2BottleMax=95
O2BottleMin=25
O2BottleMax=95
IceMin=100
IceMax=500
IceSource=
HappyTimer=Bottle Status Happy
WarningTimer=Bottle Status Warning
CriticalTimer=Bottle Status Critical

[BatteryStatus]
Enable=true
StatusLCD=Battery Status LCD
WarnPercent=25
CriticalPercent=10

[SoundAlerts]
Enable=true
AlertSound=LightGrid Alert Sound

[TickIntervals]
; How many ticks between each module run (1 = every rotation, 2 = every other, etc.)
; Higher values = less CPU usage but slower response
ReactorKeeper=1
QuotaManager=2
DockYoink=1
InventoryDisplay=3
GasKeeper=1
GasBottleManager=1
BatteryStatus=1
SoundAlerts=1
";

// ============================================================
// ============================================================

int tickCounter = 0;
bool blocksCached = false;
StringBuilder sb = new StringBuilder();

List<IMyReactor> reactors = new List<IMyReactor>();
List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
List<IMyAssembler> assemblers = new List<IMyAssembler>();
List<IMyShipConnector> connectors = new List<IMyShipConnector>();

bool enableReactorKeeper = true;
bool enableQuotaManager = true;
bool enableDockYoink = true;
bool enableInventoryDisplay = true;

int reactorMax = 1000;
int reactorMin = 600;
int reactorWarnAt = 800;
string reactorStatusLCDName = "Reactor Status LCD";
string reactorSourceGroupName = "Uranium Storage";
string reactorHappyTimerName = "Reactor Status Happy";
string reactorWarningTimerName = "Reactor Status Warning";
string reactorCriticalTimerName = "Reactor Status Critical";

IMyTextPanel reactorStatusLCD = null;
IMyTimerBlock reactorHappyTimer = null;
IMyTimerBlock reactorWarningTimer = null;
IMyTimerBlock reactorCriticalTimer = null;
List<IMyCargoContainer> reactorSourceCargo = new List<IMyCargoContainer>();
bool reactorBlocksCached = false;

string quotaStatusLCDName = "Quota Status LCD";
bool enableDisassembly = true;
string disassemblerName = "Quota Disassembler";
float excessThreshold = 1.5f;

IMyTextPanel quotaStatusLCD = null;
IMyAssembler quotaDisassembler = null;
bool quotaBlocksCached = false;

List<MyInventoryItem> quotaItemBuffer = new List<MyInventoryItem>();
List<MyProductionItem> quotaQueueBuffer = new List<MyProductionItem>();
Dictionary<string, int> quotaTargets = new Dictionary<string, int>();
Dictionary<string, MyFixedPoint> inventoryCounts = new Dictionary<string, MyFixedPoint>();

string yoinkConnectorName = "Yoink Connector";
string yoinkTargetGroupName = "Main Storage";

IMyShipConnector yoinkConnector = null;
List<IMyCargoContainer> yoinkTargetCargo = new List<IMyCargoContainer>();
bool yoinkBlocksCached = false;

bool yoinkWasConnected = false;
List<MyInventoryItem> yoinkItemBuffer = new List<MyInventoryItem>();

string inventoryDisplayLCDName = "Inventory List LCD";
bool inventoryShowZero = false;
string inventorySortBy = "name"; // "name" or "amount"
bool inventoryAutoFontSize = true;
float inventoryMaxFontSize = 1.2f;
float inventoryMinFontSize = 0.5f;
bool inventoryCategoryHeaders = true;
int inventoryColumns = 2; // 1 = single column, 2 = dual column (split by category)

IMyTextPanel inventoryDisplayLCD = null;
bool inventoryBlocksCached = false;

Dictionary<string, MyFixedPoint> inventoryTotals = new Dictionary<string, MyFixedPoint>();
Dictionary<string, string> inventoryCategories = new Dictionary<string, string>(); // displayName -> category
List<MyInventoryItem> inventoryItemBuffer = new List<MyInventoryItem>();
List<KeyValuePair<string, MyFixedPoint>> inventorySortBuffer = new List<KeyValuePair<string, MyFixedPoint>>();
List<string> inventoryColumnLeft = new List<string>(); // Left column lines for dual-column mode
List<string> inventoryColumnRight = new List<string>(); // Right column lines for dual-column mode

bool enableGasKeeper = true;
string gasStatusLCDName = "Gas Status LCD";
string gasHappyTimerName = "Gas Status Happy";
string gasWarningTimerName = "Gas Status Warning";
string gasCriticalTimerName = "Gas Status Critical";
int gasH2WarnPercent = 50;
int gasH2CriticalPercent = 25;
int gasO2WarnPercent = 50;
int gasO2CriticalPercent = 25;

List<IMyGasTank> h2Tanks = new List<IMyGasTank>();
List<IMyGasTank> o2Tanks = new List<IMyGasTank>();
IMyTextPanel gasStatusLCD = null;
IMyTimerBlock gasHappyTimer = null;
IMyTimerBlock gasWarningTimer = null;
IMyTimerBlock gasCriticalTimer = null;
bool gasBlocksCached = false;

bool enableBatteryStatus = true;
string batteryStatusLCDName = "Battery Status LCD";
int batteryWarnPercent = 25;
int batteryCriticalPercent = 10;

List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
IMyTextPanel batteryStatusLCD = null;
bool batteryBlocksCached = false;

bool enableSoundAlerts = true;
string alertSoundBlockName = "LightGrid Alert Sound";

IMySoundBlock alertSoundBlock = null;
bool soundAlertBlocksCached = false;

bool currentReactorCritical = false;
bool currentGasCritical = false;
bool currentBatteryCritical = false;
bool currentBottleCritical = false;
bool lastReactorCritical = false;
bool lastGasCritical = false;
bool lastBatteryCritical = false;
bool lastBottleCritical = false;

bool enableGasBottleManager = true;
string bottleStatusLCDName = "Bottle Status LCD";
string bottleH2O2GeneratorName = ""; // blank = auto-detect
string bottleStorageName = ""; // blank = any cargo
int h2BottleMin = 25;
int h2BottleMax = 95;
int o2BottleMin = 25;
int o2BottleMax = 95;
int iceMin = 100;
int iceMax = 500;
string iceSourceName = ""; // blank = any cargo
string bottleHappyTimerName = "Bottle Status Happy";
string bottleWarningTimerName = "Bottle Status Warning";
string bottleCriticalTimerName = "Bottle Status Critical";

List<IMyGasGenerator> h2o2Generators = new List<IMyGasGenerator>();
List<IMyCargoContainer> bottleStorageCargo = new List<IMyCargoContainer>();
List<IMyCargoContainer> iceSourceCargo = new List<IMyCargoContainer>();
IMyTextPanel bottleStatusLCD = null;
IMyTimerBlock bottleHappyTimer = null;
IMyTimerBlock bottleWarningTimer = null;
IMyTimerBlock bottleCriticalTimer = null;
bool bottleBlocksCached = false;

List<MyInventoryItem> bottleItemBuffer = new List<MyInventoryItem>();
int bottleState = 0; // 0=LOADING, 1=FILLING, 2=EJECTING, 3=IDLE
int bottleFillTicks = 0;
int bottleIdleTicks = 0;
int lastBottleCount = -1; // track cargo bottle count to detect new bottles
const int BOTTLE_FILL_DURATION = 30;
const int BOTTLE_IDLE_DURATION = 300;

int reactorKeeperInterval = 1;
int quotaManagerInterval = 2;
int dockYoinkInterval = 1;
int inventoryDisplayInterval = 3;
int gasKeeperInterval = 1;
int gasBottleManagerInterval = 1;
int batteryStatusInterval = 1;
int soundAlertsInterval = 1;

int reactorTickCounter = 0;
int quotaTickCounter = 0;
int dockYoinkTickCounter = 0;
int inventoryTickCounter = 0;
int gasTickCounter = 0;
int gasBottleTickCounter = 0;
int batteryTickCounter = 0;
int soundAlertsTickCounter = 0;

// ============================================================
// ============================================================

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    EnsureConfig();
}

public void Main(string argument, UpdateType updateSource)
{
    if (!string.IsNullOrEmpty(argument))
    {
        HandleCommand(argument.ToLower().Trim());
        return;
    }

    EnsureBlockCache();

    tickCounter++;

    switch (tickCounter % 8)
    {
        case 0:
            reactorTickCounter++;
            if (reactorTickCounter >= reactorKeeperInterval)
            {
                RunReactorKeeper();
                reactorTickCounter = 0;
            }
            break;
        case 1:
            quotaTickCounter++;
            if (quotaTickCounter >= quotaManagerInterval)
            {
                RunQuotaManager();
                quotaTickCounter = 0;
            }
            break;
        case 2:
            dockYoinkTickCounter++;
            if (dockYoinkTickCounter >= dockYoinkInterval)
            {
                RunDockYoink();
                dockYoinkTickCounter = 0;
            }
            break;
        case 3:
            inventoryTickCounter++;
            if (inventoryTickCounter >= inventoryDisplayInterval)
            {
                RunInventoryDisplay();
                inventoryTickCounter = 0;
            }
            break;
        case 4:
            gasTickCounter++;
            if (gasTickCounter >= gasKeeperInterval)
            {
                RunGasKeeper();
                gasTickCounter = 0;
            }
            break;
        case 5:
            gasBottleTickCounter++;
            if (gasBottleTickCounter >= gasBottleManagerInterval)
            {
                RunGasBottleManager();
                gasBottleTickCounter = 0;
            }
            break;
        case 6:
            batteryTickCounter++;
            if (batteryTickCounter >= batteryStatusInterval)
            {
                RunBatteryStatus();
                batteryTickCounter = 0;
            }
            break;
        case 7:
            soundAlertsTickCounter++;
            if (soundAlertsTickCounter >= soundAlertsInterval)
            {
                RunSoundAlerts();
                soundAlertsTickCounter = 0;
            }
            break;
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

        EnsureYoinkBlockCache();
        if (yoinkConnector != null)
        {
            string connStatus = yoinkConnector.Status.ToString();
            Echo($"Yoink Connector: {connStatus}");
        }

        EnsureBottleBlockCache();
        Echo($"H2O2 Generators: {h2o2Generators.Count}");
        Echo($"Bottle Storage: {bottleStorageCargo.Count}");

        EnsureSoundAlertBlockCache();
        Echo($"Alert Sound: {(alertSoundBlock != null ? "Found" : "NOT FOUND")}");
        Echo($"Critical States: R={currentReactorCritical} G={currentGasCritical} B={currentBatteryCritical} Bot={currentBottleCritical}");
    }
    else if (command == "intervals")
    {
        Echo("=== Tick Intervals ===");
        Echo($"ReactorKeeper: {reactorKeeperInterval} (counter: {reactorTickCounter})");
        Echo($"QuotaManager: {quotaManagerInterval} (counter: {quotaTickCounter})");
        Echo($"DockYoink: {dockYoinkInterval} (counter: {dockYoinkTickCounter})");
        Echo($"InventoryDisplay: {inventoryDisplayInterval} (counter: {inventoryTickCounter})");
        Echo($"GasKeeper: {gasKeeperInterval} (counter: {gasTickCounter})");
        Echo($"GasBottleManager: {gasBottleManagerInterval} (counter: {gasBottleTickCounter})");
        Echo($"BatteryStatus: {batteryStatusInterval} (counter: {batteryTickCounter})");
        Echo($"SoundAlerts: {soundAlertsInterval} (counter: {soundAlertsTickCounter})");
    }
    else if (command == "debug" || command == "quota")
    {
        EnsureBlockCache();
        EnsureQuotaBlockCache();

        Echo($"Configured {quotaTargets.Count} quotas:");
        foreach (var kvp in quotaTargets)
        {
            Echo($"  {kvp.Key}: {kvp.Value}");
        }

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

            bool isH2 = subtype.Contains("Hydrogen");
            Echo($"  Classified as: {(isH2 ? "H2" : "O2")}");
            Echo("");
        }
    }
    else if (command == "scan" || command == "items")
    {
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
    else if (command == "bottles")
    {
        EnsureBlockCache();
        EnsureBottleBlockCache();
        Echo($"=== Bottle Debug ===");
        string[] stateNames = { "LOADING", "FILLING", "EJECTING", "IDLE" };
        Echo($"State: {(bottleState < 4 ? stateNames[bottleState] : "?")}");
        Echo($"Fill ticks: {bottleFillTicks}/{BOTTLE_FILL_DURATION}");
        Echo($"Idle ticks: {bottleIdleTicks}/{BOTTLE_IDLE_DURATION}");
        Echo($"Last bottle count: {lastBottleCount}");
        Echo($"Generators: {h2o2Generators.Count}");

        for (int g = 0; g < h2o2Generators.Count; g++)
        {
            IMyGasGenerator gen = h2o2Generators[g];
            if (gen == null || gen.Closed) continue;
            IMyInventory inv = gen.GetInventory(0);
            if (inv == null) continue;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inv.GetItems(items);
            Echo($"  Gen '{gen.CustomName}':");
            for (int i = 0; i < items.Count; i++)
            {
                Echo($"    {items[i].Type.TypeId}/{items[i].Type.SubtypeId} x{items[i].Amount}");
            }
        }

        List<IMyCargoContainer> scan = bottleStorageCargo.Count > 0 ? bottleStorageCargo : cargoContainers;
        Echo($"Cargo ({scan.Count} containers):");
        for (int c = 0; c < scan.Count; c++)
        {
            IMyCargoContainer cargo = scan[c];
            if (cargo == null || cargo.Closed) continue;
            IMyInventory inv = cargo.GetInventory(0);
            if (inv == null) continue;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inv.GetItems(items);
            for (int i = 0; i < items.Count; i++)
            {
                string tid = items[i].Type.TypeId;
                if (tid.EndsWith("OxygenContainerObject") || tid.EndsWith("GasContainerObject"))
                {
                    Echo($"  {cargo.CustomName}: {tid}/{items[i].Type.SubtypeId}");
                }
            }
        }
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
    quotaDisassembler = null;
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
    h2o2Generators.Clear();
    bottleStorageCargo.Clear();
    iceSourceCargo.Clear();
    bottleStatusLCD = null;
    bottleHappyTimer = null;
    bottleWarningTimer = null;
    bottleCriticalTimer = null;
    bottleBlocksCached = false;
    alertSoundBlock = null;
    soundAlertBlocksCached = false;
    ParseConfig();
}

// ============================================================
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

        if (trimmed.Length == 0 || trimmed.StartsWith(";")) continue;

        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            currentSection = trimmed.Substring(1, trimmed.Length - 2);
            continue;
        }

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
        else if (key == "EnableGasBottleManager") enableGasBottleManager = ParseBool(value);
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
        else if (key == "EnableDisassembly") enableDisassembly = ParseBool(value);
        else if (key == "DisassemblerName") disassemblerName = value;
        else if (key == "ExcessThreshold")
        {
            float threshold;
            if (float.TryParse(value, out threshold) && threshold >= 1.0f)
                excessThreshold = threshold;
        }
        else
        {
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
        else if (key == "AutoFontSize") inventoryAutoFontSize = ParseBool(value);
        else if (key == "CategoryHeaders") inventoryCategoryHeaders = ParseBool(value);
        else if (key == "MaxFontSize")
        {
            float val;
            if (float.TryParse(value, out val) && val > 0) inventoryMaxFontSize = val;
        }
        else if (key == "MinFontSize")
        {
            float val;
            if (float.TryParse(value, out val) && val > 0) inventoryMinFontSize = val;
        }
        else if (key == "Columns")
        {
            int val;
            if (int.TryParse(value, out val) && val >= 1 && val <= 2) inventoryColumns = val;
        }
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
    else if (section == "SoundAlerts")
    {
        if (key == "Enable") enableSoundAlerts = ParseBool(value);
        else if (key == "AlertSound") alertSoundBlockName = value;
    }
    else if (section == "GasBottleManager")
    {
        if (key == "Enable") enableGasBottleManager = ParseBool(value);
        else if (key == "StatusLCD") bottleStatusLCDName = value;
        else if (key == "H2O2Generator") bottleH2O2GeneratorName = value;
        else if (key == "BottleStorage") bottleStorageName = value;
        else if (key == "H2BottleMin") int.TryParse(value, out h2BottleMin);
        else if (key == "H2BottleMax") int.TryParse(value, out h2BottleMax);
        else if (key == "O2BottleMin") int.TryParse(value, out o2BottleMin);
        else if (key == "O2BottleMax") int.TryParse(value, out o2BottleMax);
        else if (key == "IceMin") int.TryParse(value, out iceMin);
        else if (key == "IceMax") int.TryParse(value, out iceMax);
        else if (key == "IceSource") iceSourceName = value;
        else if (key == "HappyTimer") bottleHappyTimerName = value;
        else if (key == "WarningTimer") bottleWarningTimerName = value;
        else if (key == "CriticalTimer") bottleCriticalTimerName = value;
    }
    else if (section == "TickIntervals")
    {
        int interval;
        if (key == "ReactorKeeper" && int.TryParse(value, out interval) && interval >= 1)
            reactorKeeperInterval = interval;
        else if (key == "QuotaManager" && int.TryParse(value, out interval) && interval >= 1)
            quotaManagerInterval = interval;
        else if (key == "DockYoink" && int.TryParse(value, out interval) && interval >= 1)
            dockYoinkInterval = interval;
        else if (key == "InventoryDisplay" && int.TryParse(value, out interval) && interval >= 1)
            inventoryDisplayInterval = interval;
        else if (key == "GasKeeper" && int.TryParse(value, out interval) && interval >= 1)
            gasKeeperInterval = interval;
        else if (key == "GasBottleManager" && int.TryParse(value, out interval) && interval >= 1)
            gasBottleManagerInterval = interval;
        else if (key == "BatteryStatus" && int.TryParse(value, out interval) && interval >= 1)
            batteryStatusInterval = interval;
        else if (key == "SoundAlerts" && int.TryParse(value, out interval) && interval >= 1)
            soundAlertsInterval = interval;
    }
}

bool ParseBool(string value)
{
    return value.ToLower() == "true" || value == "1";
}

void EnsureBlockCache()
{
    if (blocksCached) return;
    
    GridTerminalSystem.GetBlocksOfType(reactors, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(cargoContainers, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(assemblers, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(connectors, b => b.CubeGrid == Me.CubeGrid);
    
    blocksCached = true;
    Echo($"Cached: {reactors.Count} reactors, {cargoContainers.Count} cargo, {assemblers.Count} assemblers, {connectors.Count} connectors");
}

// ============================================================
// ============================================================

void RunReactorKeeper()
{
    if (!enableReactorKeeper) return;
    if (reactors.Count == 0) return;

    EnsureReactorBlockCache();

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

    if (validReactorCount != reactors.Count)
    {
        blocksCached = false;
        reactorBlocksCached = false;
        EnsureBlockCache();
        EnsureReactorBlockCache();

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

    bool needsTopUp = lowestInt <= reactorMin;
    bool storageEmpty = false;

    if (needsTopUp)
    {
        storageEmpty = !TopUpReactors();
    }

    string status;
    IMyTimerBlock emoteTimer;

    if (avgUranium > reactorWarnAt)
    {
        status = "Chillin";
        emoteTimer = reactorHappyTimer;
        currentReactorCritical = false;
    }
    else if (avgUranium > reactorMin)
    {
        status = "Getting Low - Mine Soon";
        emoteTimer = reactorWarningTimer;
        currentReactorCritical = false;
    }
    else
    {
        if (storageEmpty)
        {
            status = "GO MINE YOU FOOL";
            emoteTimer = reactorCriticalTimer;
            currentReactorCritical = true;
        }
        else
        {
            status = "Getting Low - Mine Soon";
            emoteTimer = reactorWarningTimer;
            currentReactorCritical = false;
        }
    }

    UpdateReactorLCD(reactorCount, avgUranium, status);

    TriggerEmoteTimer(emoteTimer);
}

void EnsureReactorBlockCache()
{
    if (reactorBlocksCached) return;

    reactorStatusLCD = GridTerminalSystem.GetBlockWithName(reactorStatusLCDName) as IMyTextPanel;

    reactorHappyTimer = GridTerminalSystem.GetBlockWithName(reactorHappyTimerName) as IMyTimerBlock;
    reactorWarningTimer = GridTerminalSystem.GetBlockWithName(reactorWarningTimerName) as IMyTimerBlock;
    reactorCriticalTimer = GridTerminalSystem.GetBlockWithName(reactorCriticalTimerName) as IMyTimerBlock;

    reactorSourceCargo.Clear();
    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(reactorSourceGroupName);
    if (group != null)
    {
        group.GetBlocksOfType(reactorSourceCargo);
    }

    reactorBlocksCached = true;

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
    for (int i = 0; i < reactors.Count; i++)
    {
        IMyReactor reactor = reactors[i];
        if (reactor == null || !reactor.IsFunctional) continue;

        MyFixedPoint current = GetUraniumInReactor(reactor);
        MyFixedPoint needed = (MyFixedPoint)reactorMax - current;

        if (needed <= 0) continue;

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
// ============================================================

void RunQuotaManager()
{
    if (!enableQuotaManager) return;
    if (quotaTargets.Count == 0) return;

    EnsureQuotaBlockCache();

    inventoryCounts.Clear();
    CountInventory();

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

    sb.Clear();
    sb.AppendLine("=== Quota Status ===");
    int lineCount = 1;

    bool allMet = true;
    bool anyCanProduce = false;
    bool anyBlocked = false;      // Missing materials (critical)
    bool anyUnavailable = false;  // No blueprint (not critical)

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
            int excessLimit = (int)(target * excessThreshold);

            if (enableDisassembly && quotaDisassembler != null && currentInt > excessLimit)
            {
                int excess = currentInt - target;
                int moved = MoveToDisassembler(itemName, excess);
                if (moved > 0)
                {
                    sb.AppendLine($"{itemName}: {currentInt}/{target} [EXCESS -> DISASSEMBLE]");
                }
                else
                {
                    sb.AppendLine($"{itemName}: {currentInt}/{target} [EXCESS]");
                }
            }
            else
            {
                sb.AppendLine($"{itemName}: {currentInt}/{target} [OK]");
            }
            lineCount++;
            continue;
        }

        allMet = false;

        string missingMaterial = GetMissingMaterial(itemName);

        if (missingMaterial != null)
        {
            sb.AppendLine($"{itemName}: {currentInt}/{target} [NEED {missingMaterial}]");
            anyBlocked = true;
        }
        else
        {
            int queued = QueueProduction(targetAssembler, itemName, needed);
            if (queued > 0)
            {
                sb.AppendLine($"{itemName}: {currentInt}/{target} [QUEUED]");
                anyCanProduce = true;
            }
            else
            {
                sb.AppendLine($"{itemName}: {currentInt}/{target} [UNAVAILABLE]");
                anyUnavailable = true;
            }
        }
        lineCount++;
    }

    ClearAssemblerOutputs();

    if (quotaStatusLCD != null)
    {
        quotaStatusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
        quotaStatusLCD.WriteText(sb.ToString());

        if (lineCount > 0)
        {
            Vector2 surfaceSize = quotaStatusLCD.SurfaceSize;
            float baseLineHeight = 28.8f;
            float lcdCapacity = surfaceSize.Y / baseLineHeight;
            float targetLines = lineCount + 1;
            float idealFontSize = lcdCapacity / targetLines;

            if (idealFontSize > 1.2f) idealFontSize = 1.2f;
            if (idealFontSize < 0.5f) idealFontSize = 0.5f;

            quotaStatusLCD.FontSize = idealFontSize;
        }

        if (allMet)
        {
            quotaStatusLCD.BackgroundColor = new Color(0, 80, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
        else if (anyCanProduce && !anyBlocked)
        {
            quotaStatusLCD.BackgroundColor = new Color(120, 100, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
        else if (anyUnavailable && !anyBlocked && !anyCanProduce)
        {
            quotaStatusLCD.BackgroundColor = new Color(120, 100, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
        else if (anyCanProduce && anyBlocked)
        {
            quotaStatusLCD.BackgroundColor = new Color(120, 60, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
        else
        {
            quotaStatusLCD.BackgroundColor = new Color(120, 0, 0);
            quotaStatusLCD.FontColor = Color.White;
        }
    }
}

void EnsureQuotaBlockCache()
{
    if (quotaBlocksCached) return;

    quotaStatusLCD = GridTerminalSystem.GetBlockWithName(quotaStatusLCDName) as IMyTextPanel;

    if (enableDisassembly)
    {
        IMyAssembler asm = GridTerminalSystem.GetBlockWithName(disassemblerName) as IMyAssembler;
        if (asm != null && asm.Mode == MyAssemblerMode.Disassembly)
        {
            quotaDisassembler = asm;
        }
        else
        {
            quotaDisassembler = null;
        }
    }

    quotaBlocksCached = true;

    Echo($"Quota Status LCD '{quotaStatusLCDName}': {(quotaStatusLCD != null ? "Found" : "NOT FOUND")}");
    if (enableDisassembly)
    {
        Echo($"Disassembler '{disassemblerName}': {(quotaDisassembler != null ? "Found" : "NOT FOUND or not in Disassembly mode")}");
    }
    Echo($"Quotas configured: {quotaTargets.Count}");
}

string NormalizeItemName(string input)
{
    string lower = input.ToLower().Trim();

    if (lower == "construction component" || lower == "construction comp" || lower == "construction")
        return "Construction Comp";

    if (lower == "steel plate" || lower == "steelplate" || lower == "steel")
        return "Steel Plate";

    if (lower == "interior plate" || lower == "interiorplate" || lower == "interior")
        return "Interior Plate";

    if (lower == "computer" || lower == "computer component" || lower == "computers")
        return "Computer";

    if (lower == "motor" || lower == "motor component" || lower == "motors")
        return "Motor";

    if (lower == "metal grid" || lower == "metalgrid")
        return "Metal Grid";

    if (lower == "display" || lower == "displays")
        return "Display";

    if (lower == "bulletproof glass" || lower == "bulletproofglass" || lower == "glass")
        return "Bulletproof Glass";

    if (lower == "girder" || lower == "girders" || lower == "girder component")
        return "Girder";

    if (lower == "small tube" || lower == "smalltube" || lower == "small steel tube")
        return "Small Tube";
    if (lower == "large tube" || lower == "largetube" || lower == "large steel tube")
        return "Large Tube";

    if (lower == "detector" || lower == "detector component" || lower == "detectors")
        return "Detector";

    if (lower == "radio comm" || lower == "radio communication" || lower == "radio component" || lower == "radio")
        return "Radio Comm";

    if (lower == "medical" || lower == "medical component" || lower == "medical comp")
        return "Medical";

    if (lower == "reactor comp" || lower == "reactor component" || lower == "reactor")
        return "Reactor Comp";

    if (lower == "thruster comp" || lower == "thruster component" || lower == "thrust component" || lower == "thruster")
        return "Thruster Comp";

    if (lower == "gravity comp" || lower == "gravity component" || lower == "gravity generator component" || lower == "gravity")
        return "Gravity Comp";

    if (lower == "solar cell" || lower == "solarcell" || lower == "solar")
        return "Solar Cell";

    if (lower == "power cell" || lower == "powercell" || lower == "power")
        return "Power Cell";

    if (lower == "superconductor" || lower == "superconductor component" || lower == "super conductor")
        return "Superconductor";

    if (lower == "explosives" || lower == "explosive" || lower == "explosives component")
        return "Explosives";

    return input;
}

void CountInventory()
{
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
}

string GetItemDisplayName(MyItemType itemType)
{
    string subtype = itemType.SubtypeId;
    string typeId = itemType.TypeId;

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
        case "Superconductor": return "Superconductor";
        case "Thruster Comp": return "ThrustComponent";
        case "Reactor Comp": return "ReactorComponent";
        case "Explosives": return "ExplosivesComponent";
        case "Canvas": return "Canvas";
        case "Zone Chip": return "ZoneChip";
        default:
            return displayName.Replace(" ", "");
    }
}

string GetMissingMaterial(string componentName)
{
    string[] required = GetRequiredMaterials(componentName);
    if (required == null) return null;

    for (int i = 0; i < required.Length; i++)
    {
        string material = required[i];
        bool found = false;

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
            return new string[] { "Iron Ingot" };
    }
}

void ClearAssemblerOutputs()
{
    for (int i = 0; i < assemblers.Count; i++)
    {
        IMyAssembler asm = assemblers[i];
        if (asm == null || !asm.IsFunctional) continue;
        if (asm.Mode != MyAssemblerMode.Assembly) continue;

        IMyInventory outputInv = asm.GetInventory(1);
        if (outputInv == null || outputInv.ItemCount == 0) continue;

        for (int j = 0; j < cargoContainers.Count; j++)
        {
            IMyCargoContainer cargo = cargoContainers[j];
            if (cargo == null || !cargo.IsFunctional) continue;

            if (IsInUraniumStorage(cargo)) continue;

            IMyInventory cargoInv = cargo.GetInventory(0);
            if (cargoInv == null) continue;

            quotaItemBuffer.Clear();
            outputInv.GetItems(quotaItemBuffer);

            for (int k = quotaItemBuffer.Count - 1; k >= 0; k--)
            {
                MyInventoryItem item = quotaItemBuffer[k];
                if (outputInv.TransferItemTo(cargoInv, item, item.Amount))
                {
                }
            }

            if (outputInv.ItemCount == 0) break;
        }
    }
}

bool IsInUraniumStorage(IMyCargoContainer cargo)
{
    for (int i = 0; i < reactorSourceCargo.Count; i++)
    {
        if (reactorSourceCargo[i] == cargo) return true;
    }
    return false;
}

int QueueProduction(IMyAssembler assembler, string displayName, int amount)
{
    string blueprintSubtype = GetBlueprintSubtype(displayName);

    MyDefinitionId blueprintId;
    if (!MyDefinitionId.TryParse($"MyObjectBuilder_BlueprintDefinition/{blueprintSubtype}", out blueprintId))
    {
        return 0;
    }

    if (!assembler.CanUseBlueprint(blueprintId))
    {
        return 0;
    }

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

int MoveToDisassembler(string displayName, int amount)
{
    if (quotaDisassembler == null || amount <= 0) return 0;

    IMyInventory disassemblerInv = quotaDisassembler.GetInventory(0); // Input inventory
    if (disassemblerInv == null) return 0;

    MyItemType targetType = GetItemTypeFromDisplayName(displayName);
    if (targetType.TypeId == "") return 0;

    string blueprintSubtype = GetBlueprintSubtype(displayName);
    MyDefinitionId blueprintId;
    if (!MyDefinitionId.TryParse($"MyObjectBuilder_BlueprintDefinition/{blueprintSubtype}", out blueprintId))
    {
        return 0;
    }

    if (!quotaDisassembler.CanUseBlueprint(blueprintId))
    {
        return 0;
    }

    int totalMoved = 0;
    int remaining = amount;

    for (int i = 0; i < cargoContainers.Count; i++)
    {
        if (remaining <= 0) break;

        IMyCargoContainer cargo = cargoContainers[i];
        if (cargo == null || !cargo.IsFunctional) continue;

        if (IsInUraniumStorage(cargo)) continue;

        IMyInventory cargoInv = cargo.GetInventory(0);
        if (cargoInv == null) continue;

        quotaItemBuffer.Clear();
        cargoInv.GetItems(quotaItemBuffer);

        for (int j = quotaItemBuffer.Count - 1; j >= 0; j--)
        {
            if (remaining <= 0) break;

            MyInventoryItem item = quotaItemBuffer[j];
            if (item.Type != targetType) continue;

            int available = (int)(float)item.Amount;
            int toTransfer = available < remaining ? available : remaining;

            if (cargoInv.TransferItemTo(disassemblerInv, item, (MyFixedPoint)toTransfer))
            {
                totalMoved += toTransfer;
                remaining -= toTransfer;
            }
        }
    }

    if (totalMoved > 0)
    {
        quotaDisassembler.AddQueueItem(blueprintId, (MyFixedPoint)totalMoved);
    }

    return totalMoved;
}

MyItemType GetItemTypeFromDisplayName(string displayName)
{
    string subtypeId = "";

    switch (displayName)
    {
        case "Construction Comp": subtypeId = "Construction"; break;
        case "Computer": subtypeId = "Computer"; break;
        case "Metal Grid": subtypeId = "MetalGrid"; break;
        case "Steel Plate": subtypeId = "SteelPlate"; break;
        case "Interior Plate": subtypeId = "InteriorPlate"; break;
        case "Small Tube": subtypeId = "SmallTube"; break;
        case "Large Tube": subtypeId = "LargeTube"; break;
        case "Motor": subtypeId = "Motor"; break;
        case "Display": subtypeId = "Display"; break;
        case "Bulletproof Glass": subtypeId = "BulletproofGlass"; break;
        case "Girder": subtypeId = "Girder"; break;
        case "Radio Comm": subtypeId = "RadioCommunication"; break;
        case "Detector": subtypeId = "Detector"; break;
        case "Solar Cell": subtypeId = "SolarCell"; break;
        case "Power Cell": subtypeId = "PowerCell"; break;
        case "Medical": subtypeId = "Medical"; break;
        case "Gravity Comp": subtypeId = "GravityGenerator"; break;
        case "Superconductor": subtypeId = "Superconductor"; break;
        case "Thruster Comp": subtypeId = "Thrust"; break;
        case "Reactor Comp": subtypeId = "Reactor"; break;
        case "Explosives": subtypeId = "Explosives"; break;
        case "Canvas": subtypeId = "Canvas"; break;
        case "Zone Chip": subtypeId = "ZoneChip"; break;
        default:
            subtypeId = displayName.Replace(" ", "");
            break;
    }

    return new MyItemType("MyObjectBuilder_Component", subtypeId);
}

// ============================================================
// ============================================================

void RunDockYoink()
{
    if (!enableDockYoink) return;

    EnsureYoinkBlockCache();

    if (yoinkConnector == null) return;

    bool isConnected = yoinkConnector.Status == MyShipConnectorStatus.Connected;

    if (isConnected && !yoinkWasConnected)
    {
        YoinkVisitorCargo();
    }

    yoinkWasConnected = isConnected;
}

void EnsureYoinkBlockCache()
{
    if (yoinkBlocksCached) return;

    yoinkConnector = GridTerminalSystem.GetBlockWithName(yoinkConnectorName) as IMyShipConnector;

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

    IMyShipConnector otherConnector = yoinkConnector.OtherConnector;
    if (otherConnector == null) return;

    IMyCubeGrid visitorGrid = otherConnector.CubeGrid;
    if (visitorGrid == null) return;

    List<IMyInventory> visitorInventories = new List<IMyInventory>();

    List<IMyCargoContainer> visitorCargo = new List<IMyCargoContainer>();
    GridTerminalSystem.GetBlocksOfType(visitorCargo, b => b.CubeGrid == visitorGrid);
    for (int i = 0; i < visitorCargo.Count; i++)
    {
        IMyInventory inv = visitorCargo[i].GetInventory(0);
        if (inv != null) visitorInventories.Add(inv);
    }

    List<IMyShipConnector> visitorConnectors = new List<IMyShipConnector>();
    GridTerminalSystem.GetBlocksOfType(visitorConnectors, b => b.CubeGrid == visitorGrid);
    for (int i = 0; i < visitorConnectors.Count; i++)
    {
        IMyInventory inv = visitorConnectors[i].GetInventory(0);
        if (inv != null) visitorInventories.Add(inv);
    }

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

    for (int i = 0; i < visitorInventories.Count; i++)
    {
        IMyInventory sourceInv = visitorInventories[i];
        if (sourceInv == null || sourceInv.ItemCount == 0) continue;

        yoinkItemBuffer.Clear();
        sourceInv.GetItems(yoinkItemBuffer);

        for (int j = yoinkItemBuffer.Count - 1; j >= 0; j--)
        {
            MyInventoryItem item = yoinkItemBuffer[j];

            for (int k = 0; k < yoinkTargetCargo.Count; k++)
            {
                IMyCargoContainer target = yoinkTargetCargo[k];
                if (target == null || !target.IsFunctional) continue;

                IMyInventory targetInv = target.GetInventory(0);
                if (targetInv == null) continue;

                if (!targetInv.CanItemsBeAdded(item.Amount, item.Type)) continue;

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
// ============================================================

void RunInventoryDisplay()
{
    if (!enableInventoryDisplay) return;

    EnsureInventoryBlockCache();

    if (inventoryDisplayLCD == null) return;

    inventoryTotals.Clear();
    inventoryCategories.Clear();
    CountAllInventory();

    inventorySortBuffer.Clear();
    foreach (var kvp in inventoryTotals)
    {
        if (!inventoryShowZero && (float)kvp.Value <= 0) continue;
        inventorySortBuffer.Add(kvp);
    }

    if (inventorySortBy == "amount")
    {
        inventorySortBuffer.Sort((a, b) => ((float)b.Value).CompareTo((float)a.Value));
    }
    else
    {
        inventorySortBuffer.Sort((a, b) => a.Key.CompareTo(b.Key));
    }

    sb.Clear();
    int lineCount = 0;

    if (inventoryColumns >= 2 && inventoryCategoryHeaders)
    {
        lineCount = BuildDualColumnDisplay();
    }
    else if (inventoryCategoryHeaders)
    {
        lineCount = BuildCategoryDisplay();
    }
    else
    {
        sb.AppendLine("=== Main Grid Inventory ===");
        sb.AppendLine();
        lineCount = 2;

        for (int i = 0; i < inventorySortBuffer.Count; i++)
        {
            string name = inventorySortBuffer[i].Key;
            int amount = (int)(float)inventorySortBuffer[i].Value;
            sb.AppendLine($"{name}: {FormatNumber(amount)}");
            lineCount++;
        }
    }

    if (inventoryAutoFontSize && lineCount > 0)
    {
        Vector2 surfaceSize = inventoryDisplayLCD.SurfaceSize;
        float baseLineHeight = 28.8f;

        float lcdCapacityAtSize1 = surfaceSize.Y / baseLineHeight;

        float targetLines = lineCount + 1;

        float idealFontSize = lcdCapacityAtSize1 / targetLines;

        if (idealFontSize > inventoryMaxFontSize) idealFontSize = inventoryMaxFontSize;
        if (idealFontSize < inventoryMinFontSize) idealFontSize = inventoryMinFontSize;

        inventoryDisplayLCD.FontSize = idealFontSize;

        Echo($"Inventory: {lineCount} lines, LCD capacity ~{lcdCapacityAtSize1:F0}, font {idealFontSize:F2}");
    }

    inventoryDisplayLCD.ContentType = ContentType.TEXT_AND_IMAGE;
    inventoryDisplayLCD.WriteText(sb.ToString());
}

int BuildCategoryDisplay()
{
    string[] categoryOrder = { "Ores", "Ingots", "Components", "Tools", "Ammo", "Bottles", "Other" };
    int lineCount = 0;

    for (int c = 0; c < categoryOrder.Length; c++)
    {
        string category = categoryOrder[c];

        bool hasItems = false;
        for (int i = 0; i < inventorySortBuffer.Count; i++)
        {
            string name = inventorySortBuffer[i].Key;
            string itemCat;
            if (!inventoryCategories.TryGetValue(name, out itemCat)) itemCat = "Other";

            if (itemCat == category)
            {
                if (!hasItems)
                {
                    if (lineCount > 0) { sb.AppendLine(); lineCount++; }
                    sb.AppendLine($"=== {category} ===");
                    lineCount++;
                    hasItems = true;
                }

                int amount = (int)(float)inventorySortBuffer[i].Value;
                sb.AppendLine($"{name}: {FormatNumber(amount)}");
                lineCount++;
            }
        }
    }

    return lineCount;
}

int BuildDualColumnDisplay()
{
    string[] leftCategories = { "Components" };
    string[] rightCategories = { "Ores", "Ingots", "Tools", "Ammo", "Bottles", "Other" };

    int columnWidth = 24;
    string columnGap = "      ";

    inventoryColumnLeft.Clear();
    inventoryColumnLeft.Add("=== Components ===");
    BuildColumnItemsOnly(leftCategories, inventoryColumnLeft);

    inventoryColumnRight.Clear();
    inventoryColumnRight.Add("=== Other Items ===");
    BuildColumnItemsOnly(rightCategories, inventoryColumnRight);

    int maxLines = inventoryColumnLeft.Count > inventoryColumnRight.Count
        ? inventoryColumnLeft.Count
        : inventoryColumnRight.Count;

    int lineCount = 0;

    for (int i = 0; i < maxLines; i++)
    {
        string left = i < inventoryColumnLeft.Count ? inventoryColumnLeft[i] : "";
        string right = i < inventoryColumnRight.Count ? inventoryColumnRight[i] : "";

        string paddedLeft = PadRight(left, columnWidth);
        sb.AppendLine(paddedLeft + columnGap + right);
        lineCount++;
    }

    return lineCount;
}

void BuildColumnItemsOnly(string[] categories, List<string> lines)
{
    for (int c = 0; c < categories.Length; c++)
    {
        string category = categories[c];

        for (int i = 0; i < inventorySortBuffer.Count; i++)
        {
            string name = inventorySortBuffer[i].Key;
            string itemCat;
            if (!inventoryCategories.TryGetValue(name, out itemCat)) itemCat = "Other";

            if (itemCat == category)
            {
                int amount = (int)(float)inventorySortBuffer[i].Value;
                lines.Add($"{name}: {FormatNumber(amount)}");
            }
        }
    }
}

string PadRight(string text, int width)
{
    if (text.Length >= width) return text;

    int padding = width - text.Length;
    char[] padded = new char[width];

    for (int i = 0; i < text.Length; i++)
    {
        padded[i] = text[i];
    }

    for (int i = text.Length; i < width; i++)
    {
        padded[i] = ' ';
    }

    return new string(padded);
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
    for (int i = 0; i < cargoContainers.Count; i++)
    {
        IMyCargoContainer cargo = cargoContainers[i];
        if (cargo == null || !cargo.IsFunctional) continue;

        CountInventoryFrom(cargo.GetInventory(0));
    }

    for (int i = 0; i < assemblers.Count; i++)
    {
        IMyAssembler asm = assemblers[i];
        if (asm == null || !asm.IsFunctional) continue;

        CountInventoryFrom(asm.GetInventory(0)); // Input
        CountInventoryFrom(asm.GetInventory(1)); // Output
    }

    for (int i = 0; i < reactors.Count; i++)
    {
        IMyReactor reactor = reactors[i];
        if (reactor == null || !reactor.IsFunctional) continue;

        CountInventoryFrom(reactor.GetInventory(0));
    }

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
            inventoryCategories[displayName] = GetItemCategory(item.Type.TypeId);
        }
    }
}

string GetItemCategory(string typeId)
{
    if (typeId.EndsWith("Ore")) return "Ores";
    if (typeId.EndsWith("Ingot")) return "Ingots";
    if (typeId.EndsWith("Component")) return "Components";
    if (typeId.EndsWith("PhysicalGunObject")) return "Tools";
    if (typeId.EndsWith("AmmoMagazine")) return "Ammo";
    if (typeId.EndsWith("GasContainerObject")) return "Bottles";
    if (typeId.EndsWith("OxygenContainerObject")) return "Bottles";
    return "Other";
}

string FormatNumber(int number)
{
    if (number < 1000) return number.ToString();

    string result = "";
    int remaining = number;

    while (remaining > 0)
    {
        int chunk = remaining % 1000;
        remaining = remaining / 1000;

        if (result.Length == 0)
        {
            result = chunk.ToString().PadLeft(3, '0');
        }
        else if (remaining > 0)
        {
            result = chunk.ToString().PadLeft(3, '0') + "," + result;
        }
        else
        {
            result = chunk.ToString() + "," + result;
        }
    }

    return result;
}

// ============================================================
// ============================================================

void RunGasKeeper()
{
    if (!enableGasKeeper) return;

    EnsureGasBlockCache();

    int validH2 = CountValidTanks(h2Tanks);
    int validO2 = CountValidTanks(o2Tanks);

    if (validH2 != h2Tanks.Count || validO2 != o2Tanks.Count)
    {
        gasBlocksCached = false;
        EnsureGasBlockCache();
    }

    double h2Percent = CalculateAverageGasLevel(h2Tanks);
    double o2Percent = CalculateAverageGasLevel(o2Tanks);

    string h2Status = GetGasStatus(h2Percent, gasH2WarnPercent, gasH2CriticalPercent);
    string o2Status = GetGasStatus(o2Percent, gasO2WarnPercent, gasO2CriticalPercent);

    bool isCritical = h2Status == "CRITICAL" || o2Status == "CRITICAL";
    bool isWarning = h2Status == "WARNING" || o2Status == "WARNING";

    currentGasCritical = isCritical;

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

    if (emoteTimer != null)
    {
        emoteTimer.Trigger();
    }

    UpdateGasLCD(h2Percent, h2Status, o2Percent, o2Status, isCritical, isWarning);
}

void EnsureGasBlockCache()
{
    if (gasBlocksCached) return;

    h2Tanks.Clear();
    o2Tanks.Clear();

    List<IMyGasTank> allTanks = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType(allTanks, b => b.CubeGrid == Me.CubeGrid);

    for (int i = 0; i < allTanks.Count; i++)
    {
        IMyGasTank tank = allTanks[i];
        string subtypeId = tank.BlockDefinition.SubtypeId;

        if (subtypeId.Contains("Hydrogen"))
        {
            h2Tanks.Add(tank);
        }
        else
        {
            o2Tanks.Add(tank);
        }
    }

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

    if (h2Tanks.Count > 0)
    {
        sb.AppendLine($"Hydrogen: {h2Percent:F0}% [{h2Status}]");
    }
    else
    {
        sb.AppendLine("Hydrogen: No tanks");
    }

    if (o2Tanks.Count > 0)
    {
        sb.AppendLine($"Oxygen: {o2Percent:F0}% [{o2Status}]");
    }
    else
    {
        sb.AppendLine("Oxygen: No tanks");
    }

    gasStatusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
    gasStatusLCD.WriteText(sb.ToString());

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
// ============================================================


void RunGasBottleManager()
{
    if (!enableGasBottleManager) return;

    EnsureBottleBlockCache();

    if (h2o2Generators.Count == 0) return;

    int h2Filling = 0;
    int o2Filling = 0;
    int h2Stored = 0;
    int o2Stored = 0;
    bool movedBottle = false;

    // State machine: 0=LOADING, 1=FILLING, 2=EJECTING, 3=IDLE
    if (bottleState == 0)
    {
        // LOADING: Move one bottle per tick from cargo to generator
        List<IMyCargoContainer> scanCargo = bottleStorageCargo.Count > 0 ? bottleStorageCargo : cargoContainers;

        for (int c = 0; c < scanCargo.Count; c++)
        {
            if (movedBottle) break;

            IMyCargoContainer cargo = scanCargo[c];
            if (cargo == null || cargo.Closed || !cargo.IsFunctional) continue;

            IMyInventory cargoInv = cargo.GetInventory(0);
            if (cargoInv == null) continue;

            bottleItemBuffer.Clear();
            cargoInv.GetItems(bottleItemBuffer);

            for (int i = bottleItemBuffer.Count - 1; i >= 0; i--)
            {
                if (movedBottle) break;

                string typeId = bottleItemBuffer[i].Type.TypeId;
                if (!IsBottle(typeId)) continue;

                if (MoveBottleToGenerator(cargoInv, bottleItemBuffer[i]))
                {
                    movedBottle = true;
                }
            }
        }

        if (!movedBottle)
        {
            bottleFillTicks = 0;
            bottleState = 1;
        }
    }
    else if (bottleState == 1)
    {
        // FILLING: Wait for bottles to fill
        bottleFillTicks++;
        if (bottleFillTicks >= BOTTLE_FILL_DURATION)
        {
            bottleState = 2;
        }
    }
    else if (bottleState == 2)
    {
        // EJECTING: Move one bottle per tick from generator back to storage
        for (int g = 0; g < h2o2Generators.Count; g++)
        {
            if (movedBottle) break;

            IMyGasGenerator gen = h2o2Generators[g];
            if (gen == null || gen.Closed || !gen.IsFunctional) continue;

            IMyInventory genInv = gen.GetInventory(0);
            if (genInv == null) continue;

            bottleItemBuffer.Clear();
            genInv.GetItems(bottleItemBuffer);

            for (int i = bottleItemBuffer.Count - 1; i >= 0; i--)
            {
                if (movedBottle) break;

                string typeId = bottleItemBuffer[i].Type.TypeId;
                if (!IsBottle(typeId)) continue;

                if (MoveBottleToStorage(genInv, bottleItemBuffer[i]))
                {
                    movedBottle = true;
                }
            }
        }

        if (!movedBottle)
        {
            // All ejected - snapshot bottle count and go idle
            lastBottleCount = CountCargoBottles();
            bottleIdleTicks = 0;
            bottleState = 3;
        }
    }
    else if (bottleState == 3)
    {
        // IDLE: Bottles are filled. Wait until new bottles appear or cooldown expires.
        int currentCount = CountCargoBottles();
        bottleIdleTicks++;

        if (currentCount > lastBottleCount)
        {
            // New bottles appeared (player added some) - go fill them
            bottleState = 0;
        }
        else if (bottleIdleTicks >= BOTTLE_IDLE_DURATION)
        {
            // Long cooldown expired - do a maintenance cycle
            bottleState = 0;
        }
    }

    // Count bottles in generators
    for (int g = 0; g < h2o2Generators.Count; g++)
    {
        IMyGasGenerator gen = h2o2Generators[g];
        if (gen == null || gen.Closed || !gen.IsFunctional) continue;

        IMyInventory genInv = gen.GetInventory(0);
        if (genInv == null) continue;

        bottleItemBuffer.Clear();
        genInv.GetItems(bottleItemBuffer);

        for (int i = 0; i < bottleItemBuffer.Count; i++)
        {
            string typeId = bottleItemBuffer[i].Type.TypeId;
            if (!IsBottle(typeId)) continue;
            if (IsH2Bottle(bottleItemBuffer[i])) h2Filling++;
            else o2Filling++;
        }
    }

    // Count bottles in cargo
    List<IMyCargoContainer> cargos = bottleStorageCargo.Count > 0 ? bottleStorageCargo : cargoContainers;
    for (int c = 0; c < cargos.Count; c++)
    {
        IMyCargoContainer cargo = cargos[c];
        if (cargo == null || cargo.Closed || !cargo.IsFunctional) continue;

        IMyInventory cargoInv = cargo.GetInventory(0);
        if (cargoInv == null) continue;

        bottleItemBuffer.Clear();
        cargoInv.GetItems(bottleItemBuffer);

        for (int i = 0; i < bottleItemBuffer.Count; i++)
        {
            string typeId = bottleItemBuffer[i].Type.TypeId;
            if (!IsBottle(typeId)) continue;
            if (IsH2Bottle(bottleItemBuffer[i])) h2Stored++;
            else o2Stored++;
        }
    }

    MyFixedPoint totalIce = 0;
    for (int g = 0; g < h2o2Generators.Count; g++)
    {
        IMyGasGenerator gen = h2o2Generators[g];
        if (gen == null || gen.Closed || !gen.IsFunctional) continue;

        totalIce += GetIceInInventory(gen.GetInventory(0));
    }

    int iceAmount = (int)(float)totalIce;
    bool needIce = iceAmount < iceMin;

    if (needIce)
    {
        TransferIceToGenerators(iceAmount);
    }

    bool isRefilling = h2Filling > 0 || o2Filling > 0;
    bool isCritical = needIce && (h2Stored == 0 && h2Filling == 0) && (o2Stored == 0 && o2Filling == 0);
    bool isWarning = isRefilling || needIce;

    currentBottleCritical = isCritical;

    IMyTimerBlock emoteTimer;
    if (isCritical)
    {
        emoteTimer = bottleCriticalTimer;
    }
    else if (isWarning)
    {
        emoteTimer = bottleWarningTimer;
    }
    else
    {
        emoteTimer = bottleHappyTimer;
    }

    if (emoteTimer != null)
    {
        emoteTimer.Trigger();
    }

    UpdateBottleLCD(h2Stored, h2Filling, o2Stored, o2Filling, iceAmount, needIce, isCritical, isWarning);
}

void EnsureBottleBlockCache()
{
    if (bottleBlocksCached) return;

    h2o2Generators.Clear();
    if (!string.IsNullOrEmpty(bottleH2O2GeneratorName))
    {
        IMyGasGenerator gen = GridTerminalSystem.GetBlockWithName(bottleH2O2GeneratorName) as IMyGasGenerator;
        if (gen != null && gen.CubeGrid == Me.CubeGrid)
        {
            h2o2Generators.Add(gen);
        }
        else
        {
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(bottleH2O2GeneratorName);
            if (group != null)
            {
                group.GetBlocksOfType(h2o2Generators, b => b.CubeGrid == Me.CubeGrid);
            }
        }
    }
    else
    {
        GridTerminalSystem.GetBlocksOfType(h2o2Generators, b => b.CubeGrid == Me.CubeGrid);
    }

    bottleStorageCargo.Clear();
    if (!string.IsNullOrEmpty(bottleStorageName))
    {
        IMyCargoContainer cargo = GridTerminalSystem.GetBlockWithName(bottleStorageName) as IMyCargoContainer;
        if (cargo != null && cargo.CubeGrid == Me.CubeGrid)
        {
            bottleStorageCargo.Add(cargo);
        }
        else
        {
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(bottleStorageName);
            if (group != null)
            {
                group.GetBlocksOfType(bottleStorageCargo, b => b.CubeGrid == Me.CubeGrid);
            }
        }
    }

    iceSourceCargo.Clear();
    if (!string.IsNullOrEmpty(iceSourceName))
    {
        IMyCargoContainer cargo = GridTerminalSystem.GetBlockWithName(iceSourceName) as IMyCargoContainer;
        if (cargo != null && cargo.CubeGrid == Me.CubeGrid)
        {
            iceSourceCargo.Add(cargo);
        }
        else
        {
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(iceSourceName);
            if (group != null)
            {
                group.GetBlocksOfType(iceSourceCargo, b => b.CubeGrid == Me.CubeGrid);
            }
        }
    }

    bottleStatusLCD = GridTerminalSystem.GetBlockWithName(bottleStatusLCDName) as IMyTextPanel;
    bottleHappyTimer = GridTerminalSystem.GetBlockWithName(bottleHappyTimerName) as IMyTimerBlock;
    bottleWarningTimer = GridTerminalSystem.GetBlockWithName(bottleWarningTimerName) as IMyTimerBlock;
    bottleCriticalTimer = GridTerminalSystem.GetBlockWithName(bottleCriticalTimerName) as IMyTimerBlock;

    bottleBlocksCached = true;

    Echo($"Bottle LCD '{bottleStatusLCDName}': {(bottleStatusLCD != null ? "Found" : "NOT FOUND")}");
    Echo($"H2O2 Generators: {h2o2Generators.Count}");
    Echo($"Bottle Storage: {bottleStorageCargo.Count} (0 = using all cargo)");
    Echo($"Ice Source: {iceSourceCargo.Count} (0 = using all cargo)");
}


bool MoveBottleToStorage(IMyInventory sourceInv, MyInventoryItem bottle)
{
    List<IMyCargoContainer> targets = bottleStorageCargo.Count > 0 ? bottleStorageCargo : cargoContainers;

    for (int i = 0; i < targets.Count; i++)
    {
        IMyCargoContainer cargo = targets[i];
        if (cargo == null || cargo.Closed || !cargo.IsFunctional) continue;

        IMyInventory targetInv = cargo.GetInventory(0);
        if (targetInv == null) continue;

        if (sourceInv.TransferItemTo(targetInv, bottle, bottle.Amount))
        {
            return true;
        }
    }

    return false;
}

bool MoveBottleToGenerator(IMyInventory sourceInv, MyInventoryItem bottle)
{
    for (int g = 0; g < h2o2Generators.Count; g++)
    {
        IMyGasGenerator gen = h2o2Generators[g];
        if (gen == null || gen.Closed || !gen.IsFunctional) continue;

        IMyInventory genInv = gen.GetInventory(0);
        if (genInv == null) continue;

        if (sourceInv.TransferItemTo(genInv, bottle, bottle.Amount))
        {
            return true;
        }
    }

    return false;
}

bool IsBottle(string typeId)
{
    // Both H2 and O2 bottles may use OxygenContainerObject in SE
    return typeId.EndsWith("OxygenContainerObject") || typeId.EndsWith("GasContainerObject");
}

bool IsH2Bottle(MyInventoryItem item)
{
    // H2 bottles: SubtypeId contains "Hydrogen"
    // O2 bottles: SubtypeId contains "Oxygen" (or anything else)
    return item.Type.SubtypeId.Contains("Hydrogen");
}

int CountCargoBottles()
{
    int count = 0;
    List<IMyCargoContainer> scan = bottleStorageCargo.Count > 0 ? bottleStorageCargo : cargoContainers;
    for (int c = 0; c < scan.Count; c++)
    {
        IMyCargoContainer cargo = scan[c];
        if (cargo == null || cargo.Closed || !cargo.IsFunctional) continue;
        IMyInventory inv = cargo.GetInventory(0);
        if (inv == null) continue;
        bottleItemBuffer.Clear();
        inv.GetItems(bottleItemBuffer);
        for (int i = 0; i < bottleItemBuffer.Count; i++)
        {
            if (IsBottle(bottleItemBuffer[i].Type.TypeId)) count++;
        }
    }
    return count;
}

MyFixedPoint GetIceInInventory(IMyInventory inv)
{
    if (inv == null) return 0;

    bottleItemBuffer.Clear();
    inv.GetItems(bottleItemBuffer);

    MyFixedPoint total = 0;
    for (int i = 0; i < bottleItemBuffer.Count; i++)
    {
        if (bottleItemBuffer[i].Type.SubtypeId == "Ice")
        {
            total += bottleItemBuffer[i].Amount;
        }
    }

    return total;
}

void TransferIceToGenerators(int currentIce)
{
    int needed = iceMax - currentIce;
    if (needed <= 0) return;

    List<IMyCargoContainer> sources = iceSourceCargo.Count > 0 ? iceSourceCargo : cargoContainers;
    MyFixedPoint remaining = (MyFixedPoint)needed;

    for (int s = 0; s < sources.Count; s++)
    {
        if (remaining <= 0) break;

        IMyCargoContainer source = sources[s];
        if (source == null || source.Closed || !source.IsFunctional) continue;

        IMyInventory sourceInv = source.GetInventory(0);
        if (sourceInv == null) continue;

        bottleItemBuffer.Clear();
        sourceInv.GetItems(bottleItemBuffer);

        for (int i = bottleItemBuffer.Count - 1; i >= 0; i--)
        {
            if (remaining <= 0) break;
            if (bottleItemBuffer[i].Type.SubtypeId != "Ice") continue;

            MyFixedPoint available = bottleItemBuffer[i].Amount;
            MyFixedPoint toTransfer = MyFixedPoint.Min(remaining, available);

            for (int g = 0; g < h2o2Generators.Count; g++)
            {
                IMyGasGenerator gen = h2o2Generators[g];
                if (gen == null || gen.Closed || !gen.IsFunctional) continue;

                IMyInventory genInv = gen.GetInventory(0);
                if (genInv == null) continue;

                if (sourceInv.TransferItemTo(genInv, bottleItemBuffer[i], toTransfer))
                {
                    remaining -= toTransfer;
                    break;
                }
            }
        }
    }
}

void UpdateBottleLCD(int h2Stored, int h2Filling, int o2Stored, int o2Filling, int iceAmount, bool needIce, bool isCritical, bool isWarning)
{
    if (bottleStatusLCD == null) return;

    sb.Clear();
    sb.AppendLine("=== Bottle Status ===");
    sb.AppendLine($"H2 Bottles: {h2Stored} stored, {h2Filling} filling");
    sb.AppendLine($"O2 Bottles: {o2Stored} stored, {o2Filling} filling");

    string iceStatus = needIce ? "LOW" : "OK";
    sb.AppendLine($"H2O2 Ice: {iceAmount} [{iceStatus}]");
    sb.AppendLine();

    if (isCritical)
    {
        sb.AppendLine("Status: NEED ICE");
    }
    else if (bottleState == 1)
    {
        sb.AppendLine("Status: Filling");
    }
    else if (bottleState == 0 && (h2Filling > 0 || o2Filling > 0))
    {
        sb.AppendLine("Status: Loading");
    }
    else if (bottleState == 2)
    {
        sb.AppendLine("Status: Collecting");
    }
    else if (needIce)
    {
        sb.AppendLine("Status: Ice Low");
    }
    else
    {
        sb.AppendLine("Status: All Good");
    }

    bottleStatusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
    bottleStatusLCD.WriteText(sb.ToString());

    if (isCritical)
    {
        bottleStatusLCD.BackgroundColor = new Color(120, 0, 0); // Red
    }
    else if (isWarning)
    {
        bottleStatusLCD.BackgroundColor = new Color(120, 100, 0); // Yellow
    }
    else
    {
        bottleStatusLCD.BackgroundColor = new Color(0, 80, 0); // Green
    }
    bottleStatusLCD.FontColor = Color.White;
}

// ============================================================
// ============================================================

void RunBatteryStatus()
{
    if (!enableBatteryStatus) return;

    EnsureBatteryBlockCache();

    if (batteries.Count == 0) return;

    float totalStored = 0f;
    float totalMax = 0f;
    float totalInput = 0f;
    float totalOutput = 0f;
    int validBatteryCount = 0;

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

    if (validBatteryCount != batteries.Count)
    {
        batteryBlocksCached = false;
        EnsureBatteryBlockCache();
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

    float chargePercent = totalMax > 0 ? (totalStored / totalMax) * 100f : 0f;
    float netFlow = totalInput - totalOutput; // Positive = charging, Negative = draining

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

    batteryStatusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
    batteryStatusLCD.WriteText(sb.ToString());

    bool isBatteryCritical = chargePercent <= batteryCriticalPercent && !isCharging;
    currentBatteryCritical = isBatteryCritical;

    if (isBatteryCritical)
    {
        batteryStatusLCD.BackgroundColor = new Color(120, 0, 0); // Red
    }
    else if (chargePercent <= batteryWarnPercent && !isCharging)
    {
        batteryStatusLCD.BackgroundColor = new Color(120, 100, 0); // Yellow
    }
    else if (isDraining && chargePercent <= 50)
    {
        batteryStatusLCD.BackgroundColor = new Color(120, 100, 0); // Yellow
    }
    else
    {
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
// ============================================================

void RunSoundAlerts()
{
    if (!enableSoundAlerts) return;

    EnsureSoundAlertBlockCache();

    if (alertSoundBlock == null) return;

    bool wasAnyCritical = lastReactorCritical || lastGasCritical || lastBatteryCritical || lastBottleCritical;
    bool isAnyCritical = currentReactorCritical || currentGasCritical || currentBatteryCritical || currentBottleCritical;

    if (isAnyCritical && !wasAnyCritical)
    {
        alertSoundBlock.Play();
    }

    if (!isAnyCritical && wasAnyCritical)
    {
        alertSoundBlock.Stop();
    }

    lastReactorCritical = currentReactorCritical;
    lastGasCritical = currentGasCritical;
    lastBatteryCritical = currentBatteryCritical;
    lastBottleCritical = currentBottleCritical;
}

void EnsureSoundAlertBlockCache()
{
    if (soundAlertBlocksCached) return;

    alertSoundBlock = GridTerminalSystem.GetBlockWithName(alertSoundBlockName) as IMySoundBlock;

    soundAlertBlocksCached = true;

    Echo($"Alert Sound '{alertSoundBlockName}': {(alertSoundBlock != null ? "Found" : "NOT FOUND")}");
}

// ============================================================
// ============================================================
