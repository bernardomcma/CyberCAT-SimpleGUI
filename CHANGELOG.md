# Changelog

## 0.39.0

- **Quickhacks Tab (`Quickhacks`, contributed by `@RikkaDoesThings` in PR #14):**
  - Inspect and edit quickhacks installed in the currently equipped cyberdeck (`Install / Replace`, `Unequip`, `Swap selected slots`).
  - Install owned quickhacks directly from V's inventory (automatically returning displaced quickhacks to inventory) or create any quickhack variant across **T1–T5**, **T1+–T5+**, and **T5++** (including *Blackwall Gateway*).
  - Manage cyberdeck program slots (`Create 4 standard slots`, `Add new slot`, `Remove selected slot`).
  - **Mod Loadouts:** view, add, remove, and reorder quickhacks in saved *Quickhack Loadouts* mod presets.

## 0.38.0

- **Bonus Player Stats Dialog (`Player Stats` → `Bonus Stats (HP/Armor/RAM)...`):** add clean, persistent additive bonuses directly to V for **Max Health**, **Armor**, **Max Stamina**, **Cyberdeck RAM**, **Carry Capacity**, **Crit Chance %**, **Crit Damage %**, and **Movement Speed** (solves GitHub Issue #146).
- **Bulk Add Iconic Cyberware (`Inventory` → `Bulk Add ▾` → `Add Missing Iconic Cyberware (Tier 5++)`):** scans V's inventory and adds every missing Iconic Cyberware implant from Phantom Liberty / 2.0+ directly at **Tier 5++**.
- **Expanded Quest Facts Presets (`Quest Facts` → `Presets ▾`):** added 1-click presets for **Apartment Pets** (*Adopt Nibbles the Cat* & *Hatch Iguana*), **Save Takemura** (`q112_takemura_dead = 0`), **Friendly Fingers MD**, and **Fix Pacifica Rollercoaster**.

## 0.37.0

- **Respec Attributes & Max All Skills (`Player Stats` tab):** added **`Respec Attributes`** (resets Core Attributes to 3 and refunds all spent points into Attribute Points) and **`Max All Skills (60)`** (sets all 5 Skill Progressions to 60 in one click).
- **Bulk Cyberware Upgrade (`Cyberware` tab):** added **`ALL TIER 5++`** to upgrade every currently equipped cyberware implant to Tier 5++ at once.
- **Strip Weapon/Scope Mods (`ItemDetails` → `Mods` tab):** added **`Strip Weapon/Scope Mods`** to cleanly remove locked weapon mods, scopes, and muzzles without deleting base weapon parts.
- **Save Diff / Compare Saves (`Extras` → `Compare With Save...`):** compare the active save against any other `sav.dat` across Attributes, Perks, Quest Facts, Player Inventory, and Vehicles, with live filtering, Copy, and `.txt` export.

## 0.36.0

- **Wardrobe Clothing Sets, Quickhacks & Chimera Mods (`Inventory` → `Bulk Add ▾`):** added 1-click bulk packs to add missing **Iconic Quickhacks** (17), **Tier 5 Quickhacks** (17), **Iconic Weapon Mods / Chimera** (5), and **Wardrobe Clothing Sets** (Johnny Silverhand, MaxTac, GOG/Witcher Rewards, Phantom Liberty FIA/NUSA, Legendary Role Sets, V's Lifepath/Prologue) straight to V's Inventory or Stash.
- **Save Browser Batch Management (`Saves` tab):** added **Lifepath filter**, **multi-select**, **`Delete Selected`**, and **`Clean Autosaves...`** (keeps the 5 newest autosaves and safely sends older ones to the Windows Recycle Bin).
- **Quest Facts Presets & Import (`Quest Facts` tab):** added **`Presets ▾`** (including Skippy mode switchers *Stone Cold Killer* / *Puppy-Loving Pacifist*, Secret Ending, and Romance flags) plus **`Import JSON`** to restore exported quest fact files.
- **`ItemDetails` Quick Action:** added **`Unlock + Tier 5++`** to clear Quest/NotUnequippable flags and upgrade an item to Tier 5++ in one click.

## 0.35.0

- **Iconic Weapons Filter & Bulk Stash (`Add Item` & `Inventory`):** added an **Iconic only** filter checkbox in the `Add Item` dialog covering all 358 iconic weapon presets from TweakDB, plus an **Add Missing Iconics** quick action in the Inventory tab that checks both V's Inventory and Stash and adds every missing iconic weapon (115 unique weapons) directly to V's Stash at Tier 5++.
- **Build Export / Import (`Perks` tab):** added **Export Build...** and **Import Build...** buttons to save and transfer complete character builds (Attributes, 2.0 + Relic Perks, and equipped Cyberware per area/slot) as portable `.json` presets between saves.

## 0.34.1

- **Editor Settings dialog (`Settings` in the bottom status bar):** configure **Theme** (Windows / Light / Dark), **Undo steps** (`0` to disable Undo/Redo), and **Check for updates** directly from the UI without manually creating `config.json` (useful in the Standalone `.exe`).
- **Save metadata sync:** saving now updates `level`, `streetCred`, and `lifePath` in `metadata.9.json` so the in-game Load Save screen and Save Browser reflect edited values immediately.

## 0.34.0

- **CD PROJEKT RED cross-save cloud sync (`Cloud...`):** sign in via browser OAuth device flow, list your cloud saves, download any slot into the editor, and upload the edited save back (with automatic local backup and byte-for-byte verification). Works with PS5, Xbox, and PC/Game Pass cloud saves without needing Cyberpunk 2077 installed.
- **High-DPI scaling fixes:** scaled sidebar layout, HUD rules, button icons, and Save Browser toolbar on 125%+ displays.
- **Patch 2.5 stats & modded saves (`v0.30.1` upstream merge):** fixed `Index was out of range` (`ArgumentOutOfRangeException`) when opening some Patch 2.5 saves, preserved forced stat modifiers per slice kind, and added support for modded saves (unknown stat types/enums, script mod fields, and custom lifepaths).
- **Fewer antivirus false positives:** excluded unused third-party native DLLs (`wwtools.dll`, `DirectXTex`, `texconv.dll`, etc.) from release builds.

## 0.30.1

- **Patch 2.5 stats are saved correctly.** V's attribute and skill values (and other "forced" modifiers) were merged into the regular modifiers when saving, even without edits, and edits to them were lost. Stats that mention a stat type the editor didn't know (from a newer game version or a mod) made the whole entry read-only, so for example extra cyberware capacity wasn't saved. Each kind of stat data is now written back to where it came from, and unchanged stats stay byte for byte the same.
- **Opening item stats on some patch 2.5 saves crashed** with "Index was out of range". Fixed.
- **Raising cyberware capacity crashed on some patch 2.0 saves** whose player stats had no modifiers yet. Fixed.
- **Saves using mods load.** Saves with stat types or enum values added by mods (for example Weapon Conditioning) failed with `CEnum "…" could not be found`. Those values are now kept and written back unchanged. Data that script mods add to V and NPCs is read and kept too, so changing body gender works on those saves.
- **Changing body gender works on patch 2.5.** It used to leave V with a mix of both body types and the old gender's head, and the game could crash at a mirror. The new body gets a complete default appearance made for patch 2.5, the voice stays the same, and a change that can't be made leaves the save as it was. Loading an appearance preset for the other body gender only applies it if the change worked.
- **Saves using lifepath mods load.** Saves from mods like Fresh Start failed with "Specified argument was out of the range of valid values". Their lifepath now shows as "Modded" and is kept.
- **Large saves load and save.** Saves with a compression layout the editor didn't know failed with "Exception of type 'System.Exception' was thrown", and saves that grew while editing couldn't be saved.
- **Clearer messages** when `kraken.dll` can't be loaded (usually an incomplete extraction or antivirus), and on what "Make all characters romanceable" can do (some romances also need a mod).
- **Fewer files in the download.** Texture, 3D model, DirectX and database libraries the editor never used are left out, which should also mean fewer antivirus false positives.
- **License files are included.** The release contains GPL-3.0 components (WolvenKit, Kraken), see THIRD-PARTY-NOTICES.md.
