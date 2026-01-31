# LightGrid Troubleshooting

## Common Issues

### LCD Not Updating

**Symptom:** LCD stays blank or shows old data

**Solutions:**
1. Check block name matches config exactly (case-sensitive)
2. Run `status` command to verify LCD was found
3. Run `refresh` to re-scan blocks
4. Verify LCD is on the main grid (not on a rotor/piston subgrid)

### Emoticon Not Changing

**Symptom:** Emoticon block stays on one face

**Cause:** Scripts cannot directly control Emoticon blocks

**Solution:** Use the Timer Block workaround:
1. Create three Timer Blocks (Happy/Warning/Critical)
2. Each timer triggers the Emoticon to show appropriate face
3. Name timers exactly as configured
4. See [SETUP.md](SETUP.md) for detailed instructions

### Reactors Not Refueling

**Symptom:** Reactors run low but don't get uranium

**Check:**
1. `Uranium Storage` group exists and contains cargo containers
2. Cargo containers have uranium ingots (not ore)
3. Reactors are on main grid
4. Run `status` to verify source group found

### O2 Tanks Not Detected

**Symptom:** Gas Keeper shows "No tanks" for oxygen

**Cause:** Space Engineers API quirk - O2 tanks have empty SubtypeId

**Note:** This is handled in LightGrid. If still not working:
1. Run `gas` command to see tank classification
2. Verify tanks are on main grid
3. Run `refresh` to re-scan

### Endless Production Loop

**Symptom:** Assemblers keep queuing the same items

**Cause:** Items in assembler inventories were being counted toward quota

**Note:** Fixed in LightGrid - only cargo containers count. If still happening:
1. Clear assembler queues manually
2. Run `refresh`
3. Check `debug` output for inventory counts

### "NO BLUEPRINT" / Items Won't Craft

**Symptom:** Quota shows items but they never get queued

**Causes:**
1. Assembler doesn't have the blueprint unlocked
2. Item name doesn't match expected format

**Solutions:**
1. Unlock the blueprint in your assembler
2. Check item name in config matches expected format (see Configuration docs)
3. For mods, run `scan` to see exact SubtypeId

### Disassembly Not Working

**Symptom:** Excess items aren't being disassembled

**Check:**
1. `EnableDisassembly=true` in config
2. Assembler named exactly as configured exists
3. That assembler is set to **Disassembly** mode (not Assembly)
4. ExcessThreshold is reasonable (default 1.5 = 150% of quota)

### Yoink Not Transferring

**Symptom:** Ships dock but cargo isn't transferred

**Check:**
1. Connector named exactly as configured
2. `Main Storage` group exists with cargo containers
3. Ship actually docked (not just proximity)
4. Run `status` to check connector state

**Note:** Yoink only triggers ONCE per connection. If you disconnect and reconnect, it will transfer again.

### Script Errors on Run

**Symptom:** Red errors in Programmable Block info

**Common causes:**
1. Incomplete paste - ensure entire script was copied
2. Corrupted Custom Data - delete and let script regenerate defaults
3. Mod conflicts - some mods break standard API calls

**Solution:**
1. Click "Edit" on Programmable Block
2. Delete all code
3. Re-paste from `Source/LightGrid.cs`
4. "Check Code" to verify
5. Delete Custom Data content (will regenerate on run)

## Debugging Commands

### status
Shows block counts and cache state:
```
Tick: 1234
Reactors: 4
Cargo: 12
Assemblers: 2
Connectors: 3
```

If counts are 0 or lower than expected, blocks aren't being found.

### debug / quota
Shows quota configuration and current inventory:
```
Configured 5 quotas:
  Steel Plate: 500
  Interior Plate: 200
  ...
Inventory counts:
  Steel Plate: 347
  Interior Plate: 89
  ...
```

Compare inventory to quotas to understand what should be crafting.

### gas / tanks
Shows all gas tanks and how they're classified:
```
=== All Gas Tanks (4) ===
Large Hydrogen Tank
  SubtypeId: 'LargeHydrogenTank'
  TypeId: MyObjectBuilder_GasTank
  Fill: 78%
  Classified as: H2

Oxygen Tank
  SubtypeId: ''
  TypeId: MyObjectBuilder_GasTank
  Fill: 92%
  Classified as: O2
```

Useful for understanding why tanks might be misclassified.

### scan / items
Shows raw item TypeId/SubtypeId for everything in cargo:
```
=== Raw Item SubtypeIds ===
MyObjectBuilder_Ingot/Iron: 45000
MyObjectBuilder_Component/SteelPlate: 500
MyObjectBuilder_Component/Construction: 200
```

Use this to find correct names for modded items.

### intervals
Shows tick interval configuration:
```
=== Tick Intervals ===
ReactorKeeper: 1 (counter: 0)
QuotaManager: 2 (counter: 1)
...
```

### refresh / reset
Clears all caches and re-scans blocks. Use after:
- Renaming blocks
- Adding/removing blocks
- Changing Custom Data

## Performance Issues

### Script Using Too Much CPU

1. Increase tick intervals for non-critical modules:
```ini
[TickIntervals]
InventoryDisplay=5
QuotaManager=3
```

2. Disable unused modules:
```ini
[LightGrid]
EnableInventoryDisplay=false
```

### LCD Flickering

Usually caused by font size constantly recalculating. Try:
```ini
[InventoryDisplay]
AutoFontSize=false
```

## Known Limitations

1. **Main Grid Only** - LightGrid ignores subgrids (rotors, pistons, connectors to other ships). This is intentional for performance.

2. **Single Assembler** - Quota Manager uses the first assembler in Assembly mode. Cooperative mode means other assemblers will help automatically.

3. **No Sorting** - LightGrid doesn't sort items between containers. It's an inventory manager, not a sorter.

4. **Emoticon Workaround** - Requires timer blocks because SE API doesn't expose direct emoticon control.

5. **Blueprint Names** - Some items have different names for the item vs blueprint. The script handles vanilla items but mods may need manual adjustment.

## Getting Help

If you're stuck:
1. Run `status`, `debug`, and `gas` commands
2. Check all block names match config exactly
3. Verify blocks are on main grid
4. Try `refresh` to re-scan everything
5. Check Programmable Block info panel for error messages
