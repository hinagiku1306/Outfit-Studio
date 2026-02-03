# Outfit Set Feature Design Document

## Overview

The Outfit Set feature allows users to save, manage, and apply outfit configurations across game sessions. Sets can be stored globally (shared across all saves) or locally (specific to one save).

---

## Data Model

### OutfitSet

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `string` | Unique identifier (GUID), auto-generated |
| `Name` | `string` | User-defined display name |
| `ShirtId` | `string?` | Unqualified item ID, null = no item |
| `PantsId` | `string?` | Unqualified item ID, null = no item |
| `HatId` | `string?` | Unqualified item ID, null = no item |
| `Tags` | `List<string>` | Up to 5 tags per set (List for JSON serialization) |
| `IsFavorite` | `bool` | Marked as favorite |
| `IsGlobal` | `bool` | true = global (default), false = save-local |

**Runtime-only (not persisted, marked with `[JsonIgnore]`):**

| Field | Type | Description |
|-------|------|-------------|
| `IsValid` | `bool` | true = all items exist, false = has missing items |

---

## Storage Architecture

### Dual Persistence Model

| Scope | Storage Location | File/Key |
|-------|------------------|----------|
| Global sets | Mod folder JSON | `outfit-sets-global.json` |
| Local sets | SMAPI save data | `FittingRoom.LocalSets` |
| Tags | Mod folder JSON | Stored in global file |

### Global File Structure

```json
{
  "version": 1,
  "tags": ["Seasonal", "Wedding", "Combat", "Daily", "Beach Day"],
  "sets": [
    {
      "Id": "abc-123",
      "Name": "Spring Picnic",
      "ShirtId": "1001",
      "PantsId": "2",
      "HatId": null,
      "Tags": ["Seasonal"],
      "IsFavorite": true,
      "IsGlobal": true
    }
  ]
}
```

### Load Order

1. Load global file on mod initialization (available before save loaded)
2. Load local sets when save is loaded
3. Merge into unified in-memory store
4. Build runtime indexes
5. Validate all sets (cache `IsValid`)

### Save Behavior

- Global sets: Persist immediately to global JSON file
- Local sets: Persist via SMAPI save data system
- Scope change (local ↔ global): Move set between storage locations

---

## Runtime Indexes

All indexes are built on load and updated on mutations. Never persisted. Stored in `OutfitSetStore`.

| Index | Type | Purpose |
|-------|------|---------|
| `byId` | `Dictionary<string, OutfitSet>` | O(1) lookup by ID |
| `byTag` | `Dictionary<string, HashSet<string>>` | Tag → set IDs (for fast tag filtering) |
| `favoriteIds` | `HashSet<string>` | Quick favorite filter |
| `globalIds` | `HashSet<string>` | Quick scope filter |
| `localIds` | `HashSet<string>` | Quick scope filter |
| `validIds` | `HashSet<string>` | Quick validity filter |
| `itemDisplayNames` | `Dictionary<string, string>` | Item ID → display name cache |

**Note:** Even though `OutfitSet.Tags` is `List<string>` for serialization, indexes use `HashSet<string>` for O(1) lookups.

---

## Validation & Error Handling

### Missing Mod Item Detection

**Problem:** User saves global set with modded item, later uninstalls the mod. The saved item ID no longer exists in game.

**Detection Logic:**
- `null` or empty string → Intentionally empty slot (valid)
- ID exists in `ItemRegistry` → Valid item
- ID not in `ItemRegistry` → Missing mod item (invalid)

**Key Principle:** Never modify saved set data. Preserve original IDs so sets work again if user reinstalls the mod.

### Validation Timing

| Event | Action |
|-------|--------|
| On load | Validate all sets, cache `IsValid` per set |
| On apply | Re-check each item before equipping |

### Preview Behavior for Invalid Sets

- Show ⚠️ warning icon next to invalid item slots in detail panel
- Character preview shows empty slot (no item rendered) for invalid items
- Valid items in the same set display normally

### Apply Behavior for Invalid Sets

- Silently apply valid items only
- Skip invalid items without any dialog or warning
- Do NOT modify the saved set data

---

## Tag System

### Default Tags

On first run, the following tags are created:
- Seasonal
- Wedding
- Combat
- Daily

### Tag Behavior

- All tags stored in global file under `tags` array
- Tags shared across all sets (global and local)
- User can create custom tags via Add Tags popup
- User can delete any tag (including default tags)
- Deleting a tag removes it from all sets using it
- Tags sorted alphabetically in all dropdowns/lists

### Tag Constraints

- Maximum 5 tags per set
- No distinction between "default" and "custom" tags - all tags are equal

---

## UI Layouts

### Templates Overlay

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  Outfit Templates                                                [  X  ]     │
├──────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────┐  ┌──────────────────────┐ │
│  │  Templates                                   │  │  Template Details    │ │
│  │                                              │  │                      │ │
│  │  [Set ▾] [_______________________]           │  │  ┌───────────────┐   │ │
│  │  [Tags ▾] [Filter ▾]                         │  │  │   Preview     │   │ │
│  │  [✓] Match All   [✓] Show Invalid            │  │  │   (sprite)    │   │ │
│  │  ──────────────────────────────────────────  │  │  └───────────────┘   │ │
│  │  ★ Spring Picnic                             │  │                      │ │
│  │  ★ Rainy Day Cozy                            │  │  Name: ____________  │ │
│  │    Mines Run                                 │  │  Tags: [____][+]     │ │
│  │  ⚠️ Summer Beach                              │  │                      │ │
│  │  ...                                         │  │   Shirt: _________   │ │
│  │  (scrollable list)                           │  │   Pants: _________   │ │
│  │                                              │  │   Hat:   _________   │ │
│  └──────────────────────────────────────────────┘  └──────────────────────┘ │
│                                                                              │
│  [ Load ] [ Apply & Close ]    [ New ] [ Duplicate ] [ Rename ] [ Delete ]   │
│                           [ ★ Favorite ]                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Filter Bar Structure

**Row 1 - Search:**
```
[Set ▾] [_______________________]
```

**Row 2 - Filter Dropdowns:**
```
[Tags ▾] [Filter ▾]
```

**Row 3 - Filter Options:**
```
[✓] Match All   [✓] Show Invalid
```

### Save Set Overlay

```
┌──────────────────── SAVE OUTFIT SET ────────────────────┐
│                                                         │
│  Name: [_____________________________]                  │
│                                                         │
│  ┌───────────────┐   ┌──────┐                           │
│  │               │   │ [■]  │                           │
│  │   Character   │   │ [■]  │                           │
│  │    Preview    │   │ [■]  │                           │
│  │               │   │      │                           │
│  └───────────────┘   └──────┘                           │
│                                                         │
│  Tags: [ Seasonal ] [ Town ] [+ Add ]                   │
│                                                         │
│  ★ Favorite                                             │
│  ☐ Save to this farm only                               │
│                                                         │
│                 [ Save ]    [ Cancel ]                  │
└─────────────────────────────────────────────────────────┘
```

Note: "Save to this farm only" checkbox controls `IsGlobal` (unchecked = global, checked = local)

### Add Tags Popup

```
┌────────────── ADD TAGS ──────────────┐
│                                      │
│  Select tags:                        │
│                                      │
│  [ ] Beach Day                       │
│  [ ] Combat                          │
│  [ ] Daily                           │
│  [ ] Seasonal                        │
│  [ ] Wedding                         │
│                                      │
│  (alphabetically sorted)             │
│                                      │
│  ──────────────────────────────────  │
│  Custom: [____________] [Add]        │
│                                      │
│              [ Save ]   [ Cancel ]   │
└──────────────────────────────────────┘
```

### List Item Display

```
★ Spring Picnic        ← favorite (star icon)
  Mines Run            ← normal (no icon, indented for alignment)
⚠️ Summer Beach         ← invalid (warning icon)
⚠️ ★ Winter Formal      ← invalid + favorite (both icons)
```

### List Sorting

1. Favorites first (sorted alphabetically by name)
2. Non-favorites (sorted alphabetically by name)

### Detail Panel - Item Display States

```
Shirt: Blue Shirt           ← normal item
Pants: ⚠️ Missing Item       ← invalid item (warning icon, distinct style)
Hat:   (None)               ← intentionally empty slot
```

---

## Filter System

### Filter State Model

Stored in `SetFilterState` class (runtime only, not persisted).

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `SearchScope` | `SearchScope` enum | Set | Set, Item, or All |
| `SearchText` | `string` | "" | Search query |
| `SelectedTags` | `HashSet<string>` | empty | Selected tags for filtering |
| `MatchAllTags` | `bool` | false | false=OR, true=AND |
| `FavoritesOnly` | `bool` | false | Show only favorites |
| `ShowGlobal` | `bool` | true | Include global sets |
| `ShowLocal` | `bool` | true | Include local sets |
| `ShowInvalid` | `bool` | true | Include invalid sets |

```csharp
public enum SearchScope { Set, Item, All }
```

### Dropdown Contents

**Search Scope Dropdown (single-select):**
- Set (default)
- Item
- All

**Tags Dropdown (multi-select):**
- All tags alphabetically sorted
- Checkboxes for multi-select

**Filter Dropdown (multi-select):**
- Favorites
- (separator)
- Global
- Local

### Filter Logic

**Pipeline order:**

```
AllSets
  → ApplyTagFilter(selectedTags, matchAllTags)
  → ApplyOtherFilters(favoritesOnly, showGlobal, showLocal)
  → ApplyValidityFilter(showInvalid)
  → ApplySearch(searchText, searchScope)
  → Sort(favorites first, then alphabetical)
  → Result
```

**Tag Filter:**
- No tags selected → no tag filter applied (show all)
- OR mode (Match All unchecked): set has ANY of the selected tags
- AND mode (Match All checked): set has ALL of the selected tags

**Other Filters:**
- Favorites: when checked, show only sets where `IsFavorite = true`
- Scope: both checked or both unchecked → show all; one checked → filter to that scope
- Validity: when unchecked, hide sets where `IsValid = false`

**Search (applied to filtered results):**
- Set: match against set name (case-insensitive contains)
- Item: match against any item's display name
- All: match against set name OR any item name

---

## Key Operations

### Create New Set

1. User clicks [Save] in main menu or [New] in templates overlay
2. Open Save Set Overlay with current outfit
3. User enters name, selects tags, sets favorite/scope
4. On save: Generate GUID, create OutfitSet, add to store, update indexes, persist

### Edit Set

1. User selects set in list, modifies fields in detail panel
2. Changes apply immediately to in-memory model
3. Persist on blur/confirm (debounced)
4. Update affected indexes

### Replace Clothing (Keep Metadata)

1. User has set selected
2. User clicks "Update with current outfit" (or similar)
3. Replace ShirtId, PantsId, HatId with current equipment
4. Keep Name, Tags, IsFavorite, IsGlobal unchanged
5. Re-validate, update indexes, persist

### Change Scope

1. User toggles scope in detail panel or via action
2. Update `IsGlobal` flag
3. Move set from one persistence location to other
4. Update scope indexes

### Delete Set

1. User clicks [Delete]
2. Confirm dialog
3. Remove from store and all indexes
4. Persist (remove from appropriate file/save data)

### Apply Set

1. User clicks [Load] or [Apply & Close]
2. For each item slot:
   - If item ID is null → skip (keep current equipment or unequip based on game logic)
   - If item exists in game → equip it
   - If item missing → skip silently
3. [Apply & Close] also closes the overlay

---

## Performance Considerations

### Assumptions

- Total sets < 1000
- Tags per set ≤ 5
- Total tags < 100

### Why <1000 Sets?

Beyond 1000 sets, potential issues include:

| Concern | Impact |
|---------|--------|
| Index rebuild time | Rebuilding on every add/delete may cause micro-stutters (~10-50ms at 10K sets) |
| Text search latency | Linear scan of names becomes noticeable (~30ms+ at 10K sets) |
| Memory usage | ~1-2MB at 10K sets (minor but unnecessary) |
| UI list rendering | Would need virtualization for smooth scrolling |

**Practical note:** 1000 sets is already far beyond typical usage. Most users will have 10-50 sets. The limit is a safe design assumption, not a hard cap.

### Optimizations

1. **Index-based filtering**: Use HashSet intersections instead of linear scans
2. **Search last**: Text search is most expensive, apply after other filters reduce candidate set
3. **Lazy validation**: Cache `IsValid` per set, only recompute when needed
4. **Display name cache**: Cache item ID → display name mapping on load

### Estimated Complexity

| Operation | Complexity |
|-----------|------------|
| Get by ID | O(1) |
| Filter by single tag | O(1) index lookup |
| Filter by multiple tags | O(k) where k = selected tags |
| Combined filters | O(n) worst case, typically much less |
| Search | O(n × m) where m = items per set (3) |

---

## Implementation Notes

### Integration Strategy

This feature **replaces** the existing template system. The old `OutfitTemplate` and `TemplateManager` will be deprecated.

| Old | New | Notes |
|-----|-----|-------|
| `OutfitTemplate` | `OutfitSet` | New model with multi-tag, scope support |
| `OutfitTemplateData` | `OutfitSetGlobalData`, `OutfitSetLocalData` | Split storage |
| `TemplateManager` | `OutfitSetStore` | New service with indexes |
| `TemplatesOverlay` | Modify existing | Update to use new store and filters |
| `SaveSetOverlay` | Modify existing | Add scope checkbox, multi-tag UI |

### New Files to Create

```
Models/
  └─ OutfitSet.cs              - Data model + container classes

Services/
  └─ OutfitSetStore.cs         - CRUD, indexes, persistence, filtering

Managers/
  └─ SetFilterState.cs         - Filter/search state for templates overlay
```

### Files to Modify

```
UI/
  ├─ TemplatesOverlay.cs       - Use OutfitSetStore, add filter bar
  ├─ TemplatesUIBuilder.cs     - Add filter bar layout
  ├─ SaveSetOverlay.cs         - Add scope checkbox, multi-tag support
  └─ SaveSetUIBuilder.cs       - Layout changes

Core/
  └─ ModEntry.cs               - Initialize OutfitSetStore
```

### Service Initialization

Follow existing pattern in `ModEntry.cs`:

```
OnGameLaunched():
  - Load global sets from JSON file (available before save)
  - Initialize OutfitSetStore with global data only

OnSaveLoaded():
  - Load local sets from SMAPI save data
  - Merge into OutfitSetStore
  - Build indexes
  - Validate all sets

OnReturnedToTitle():
  - Clear local sets from memory
  - Keep global sets loaded
  - Rebuild indexes
```

### File Paths

| File | Location | API |
|------|----------|-----|
| Global JSON | `Helper.DirectoryPath + "/outfit-sets-global.json"` | `Helper.Data.WriteJsonFile` / `ReadJsonFile` |
| Local sets | SMAPI save data | `Helper.Data.WriteSaveData` / `ReadSaveData` with key `"FittingRoom.LocalSets"` |

### Serialization

**HashSet<string> for Tags:**
- Serializes as JSON array: `["Seasonal", "Combat"]`
- Use `List<string>` in serialization classes, convert to `HashSet` on load

**Serialization Classes:**

```csharp
// For global JSON file
public class OutfitSetGlobalData
{
    public int Version { get; set; } = 1;
    public List<string> Tags { get; set; } = new();
    public List<OutfitSet> Sets { get; set; } = new();
}

// For SMAPI save data (local sets only)
public class OutfitSetLocalData
{
    public List<OutfitSet> Sets { get; set; } = new();
}

// The model (tags as List for serialization)
public class OutfitSet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string? ShirtId { get; set; }
    public string? PantsId { get; set; }
    public string? HatId { get; set; }
    public List<string> Tags { get; set; } = new();  // Max 5
    public bool IsFavorite { get; set; }
    public bool IsGlobal { get; set; } = true;

    [JsonIgnore]
    public bool IsValid { get; set; } = true;
}
```

### Item Validation API

Use Stardew Valley's `ItemRegistry` to check if item exists:

```csharp
using StardewValley.ItemTypeDefinitions;

bool IsItemValid(string? itemId)
{
    if (string.IsNullOrEmpty(itemId)) return true;  // Empty slot is valid

    // Qualify the ID based on expected type
    string qualifiedId = ItemRegistry.QualifyItemId(itemId);
    if (qualifiedId == null) return false;

    return ItemRegistry.Exists(qualifiedId);
}
```

For getting display names:

```csharp
string? GetItemDisplayName(string? itemId)
{
    if (string.IsNullOrEmpty(itemId)) return null;

    var data = ItemRegistry.GetData(ItemRegistry.QualifyItemId(itemId));
    return data?.DisplayName;
}
```

### Equipping Items

Use existing pattern from the mod (see `OutfitMenu` apply logic):

```csharp
// Get qualified ID for the item type
string qualifiedShirtId = "(S)" + set.ShirtId;  // Shirts
string qualifiedPantsId = "(P)" + set.PantsId;  // Pants
string qualifiedHatId = "(H)" + set.HatId;      // Hats

// Create and equip item
var item = ItemRegistry.Create(qualifiedId);
Game1.player.changeShirt(shirtId);
Game1.player.changePants(pantsId);
// For hats, set Game1.player.hat.Value
```

### Search Debounce

Debounce search input to avoid filtering on every keystroke:

- Debounce delay: **300ms**
- Trigger filter recalculation only after user stops typing
- Use a simple timer pattern (not async)

### Tag Rules

| Rule | Value |
|------|-------|
| Case sensitivity | Case-insensitive for comparison, preserve original case for display |
| Max tag length | 30 characters |
| Allowed characters | Alphanumeric, spaces, hyphens, underscores |
| Duplicate handling | Prevent adding duplicate tag (case-insensitive check) |
| Trim whitespace | Yes, on save |

### Translation Keys

Add to `i18n/default.json`:

```json
{
  "templates.filter.search.set": "Set",
  "templates.filter.search.item": "Item",
  "templates.filter.search.all": "All",
  "templates.filter.tags": "Tags",
  "templates.filter.filter": "Filter",
  "templates.filter.matchAll": "Match All",
  "templates.filter.showInvalid": "Show Invalid",
  "templates.filter.favorites": "Favorites",
  "templates.filter.global": "Global",
  "templates.filter.local": "Local",
  "templates.detail.missingItem": "Missing Item",
  "templates.detail.none": "(None)",
  "saveset.saveToFarmOnly": "Save to this farm only",
  "tags.popup.title": "Add Tags",
  "tags.popup.selectTags": "Select tags:",
  "tags.popup.custom": "Custom:",
  "tags.popup.add": "Add",
  "tags.defaultSeasonal": "Seasonal",
  "tags.defaultWedding": "Wedding",
  "tags.defaultCombat": "Combat",
  "tags.defaultDaily": "Daily"
}
```

---

## Edge Cases

### File & Data Errors

| Scenario | Handling |
|----------|----------|
| Global JSON file missing | Create new file with default tags, empty sets |
| Global JSON file corrupted | Log error, create new file, warn user via HUD message |
| Global JSON parse error | Same as corrupted |
| Local save data missing | Normal for new saves, initialize empty |
| Local save data corrupted | Log error, initialize empty, warn user |

### State Edge Cases

| Scenario | Handling |
|----------|----------|
| No save loaded, user opens templates | Show only global sets, disable "Save to farm only" option |
| User changes scope while no save loaded | Prevent changing to local scope, show tooltip explaining why |
| Set has tags that no longer exist in tag list | Display tag normally, it's just orphaned (user deleted tag after assigning) |
| All sets filtered out | Show "No sets match filters" message in list area |
| Search returns no results | Show "No results" message |
| User tries to add 6th tag | Disable [+ Add] button, show tooltip "Maximum 5 tags" |

### Tag Edge Cases

| Scenario | Handling |
|----------|----------|
| User enters empty tag name | Ignore, don't add |
| User enters whitespace-only tag | Trim to empty, ignore |
| User enters duplicate tag (case-insensitive) | Ignore, don't add duplicate |
| User enters tag exceeding 30 chars | Truncate to 30 chars |
| User deletes tag used by sets | Remove tag from all sets using it, then delete tag |
| Tag list becomes empty | Show "No tags" in dropdown, user can still add custom |

### Set Operation Edge Cases

| Scenario | Handling |
|----------|----------|
| Delete currently selected set | Clear selection, show empty detail panel |
| Edit set name to empty string | Revert to previous name or use "Unnamed Set" |
| Duplicate set | Create copy with name "{original} (Copy)", same tags/favorite, same scope |
| Change scope of set with local-only modded items | Allow it (mod availability is global anyway) |

### UI Edge Cases

| Scenario | Handling |
|----------|----------|
| Window resize while overlay open | Recalculate layout via `gameWindowSizeChanged` |
| Very long set name | Truncate with ellipsis in list, show full name in detail panel |
| Many tags on one set (5 tags) | Wrap tag chips or show "+N more" if space limited |
| Very long tag name | Truncate with ellipsis |
| Rapid clicking on filter checkboxes | Debounce filter recalculation (100ms) |

### First Run

| Scenario | Handling |
|----------|----------|
| First time mod runs | Create global file with default tags, no sets |
| Default tags list | Seasonal, Wedding, Combat, Daily |
| No sets exist | Show "No saved outfits" message with hint to use [New] button |

---

## Future Considerations

(Not in scope for initial implementation)

- Export/import sets to JSON file
- Share sets with other players
- Set grouping/folders
- Outfit preview without applying
