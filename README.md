# CyberCAT-SimpleGUI

A simplified offshoot of SirBitesalot's CyberCAT.

## Features

**Feature**                                                                     | **Stability**
------------------------------------------------------------------------------- | -----------
Save and load presets for your character's appearance.                          | Stable
Edit the quantity, flags, & mod tree of items in your inventory.                | Stable
Edit quest facts.                                                               | Stable
Quick actions - dedicated controls for changing money & making items legendary. | Semi-Stable

## Game Version Support

**Game Version**    | **Support** | **CyberCAT-SimpleGUI Version**
--------------------| ------------|-----------------------------
Patch 2.5           | Full        | Unreleased (this branch)
Patch 2.0 - 2.3     | Full        | >=v0.28c
Patch 1.5X          | Full        | >=v0.20a
Patch 1.3           | Partial     | >=v0.10a_r1
Patch 1.23          | Full        | >=v0.10a

## Usage

Requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64).

1. Run **CP2077SaveEditor.exe**
2. Click **"Load Save"**
3. Make changes to your save.
    - Double-click items in your inventory to edit them.
    - Double-click nodes in an item's mod tree to edit them
4. Click **"Save Changes"**

## Todo

- Refactor, refactor, refactor.
- Eat some of the spaghetti.
- Improve the functionality of the appearance tab.
- Move contribution section from readme to github wiki

## Credits

[CyberCAT by SirBitesalot and other contributors](https://github.com/WolvenKit/CyberCAT)
[WolvenKit by the WolvenKit team](https://github.com/WolvenKit/WolvenKit)

## Contribution

Clone with submodules (`git clone --recursive`, or `git submodule update --init` on an existing clone).
Newer game patches usually need the `WolvenKit` submodule updated to a commit that supports them.

### Tests

Round-trip tests (load, edit, save, reload) run against a real save, which is not part of the repository.
Point `CP2077_TEST_SAVE` to a `sav.dat` (ideally a copy) and run:

```
dotnet test --project CP2077SaveEditor.Tests/CP2077SaveEditor.Tests.csproj -p:Platform=x64
```

Without the variable the tests are skipped.

### VSCodium

**Task** | **Info**
-------- | ----------------------------------
Requires | .NET 10 SDK
Setup    | None
Build    | Terminal > Run Build Task...
Debug    | Unsupported

### Visual Studio

**Task** | **Info**
-------- | ----------------------------------
Requires | A version with .NET 10 support, C# Desktop workspace
Setup    | Solution > Restore NuGet Packages
Build    | Build > Rebuild Solution
Debug    | Debug > Start Debugging
