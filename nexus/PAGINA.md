# Página do Nexus Mods — o que preencher

Os textos que vão para o Nexus estão em inglês. A descrição completa (BBCode) está em [description.bbcode](description.bbcode): cole no campo "Description" com o editor no modo BBCode.

## Dados básicos

| Campo | Valor |
| --- | --- |
| Mod name | CyberCAT-SimpleGUI - Save Editor |
| Version | 0.32.1 |
| Category | Utilities |
| Language | English |
| Author | Deweh, bernardomcma (o original é do Deweh; no Nexus, "Created by" pode listar os dois) |
| Contains adult content | No |
| Mod manager download | **Desligado**: é um programa, não vai para a pasta do jogo |

**Brief overview** (limite de 350 caracteres; este tem 281):

> Save editor for Cyberpunk 2077 with patch 2.5 and Phantom Liberty support. Edit perks, attributes, cyberware (beyond the capacity limit), inventory, stash, Tier 5++, vehicles and fast travel. Save browser, verified saving with backups, undo/redo. Standalone exe, no install needed.

**Tags sugeridas:** Utilities, Save Game, Tools, Character, Cyberware, Items, Phantom Liberty.

## Arquivos (aba Files)

Envie os dois zips da [release v0.32.1](https://github.com/bernardomcma/CyberCAT-SimpleGUI/releases/tag/v0.32.1).

**1. Main file — CP2077SaveEditor-Standalone.zip**
- Name: `CyberCAT-SimpleGUI (Standalone)`
- Version: `0.32.1`
- Description:
  > Recommended. A single CP2077SaveEditor.exe with .NET included, nothing else to install. Extract anywhere and run.

**2. Optional file — CP2077SaveEditor.zip**
- Name: `CyberCAT-SimpleGUI (requires .NET 10)`
- Version: `0.32.1`
- Description:
  > Smaller download for people who already have the .NET 10 Desktop Runtime (x64) installed.

## Imagens (aba Images)

Pasta [screenshots](screenshots), 1424×892, nesta ordem. A primeira vira a imagem de capa.

| Arquivo | Legenda sugerida |
| --- | --- |
| 03-cyberware.png | Ripperdoc-style cyberware screen: equip anything, raise the capacity |
| 02-player-stats.png | Level, attributes, skills, points and life path |
| 01-save-browser.png | Save browser |
| 04-perks.png | Perks editor for every 2.0 skill tree and the Relic |
| 05-inventory.png | Inventory with in-game names, filters and rarity colors |
| 06-vehicles.png | Unlock any vehicle |
| 08-quest-facts.png | Quest facts |
| 07-appearance.png | Appearance presets |
| 09-extras.png | Fast travel and other extras |

Sugestão de capa: a 03 (cyberware), que é a mais chamativa.

## Changelog (aba "Changelogs")

**0.32.1**
- Standalone download: a single exe with .NET included
- Smaller regular download (35 MB)
- Inventory context menu and advanced appearance labels fixed

**0.32.0**
- Cyberware screen redone ripperdoc-style: body map, slot icons, capacity and armor gauges

**0.31.0**
- New Cyberpunk-styled interface, now the default theme
- Tooltips on every page, responsive Player Stats with life path art

**0.30.0**
- Undo/redo (Ctrl+Z / Ctrl+Y), sidebar icons

**0.29.0**
- Patch 2.5 support, including the optimized stats format
- Perks editor, cyberware tab with extra capacity, save browser
- Stash moves, Tier 5++, bulk quantity, rarity colors, localized item names
- Verified saving with backups, unsaved changes warning, update check

## Permissões (aba "Permissions and credits")

- O código é MIT, então pode marcar que outros podem usar o código e os assets com crédito.
- Credits: Deweh (CyberCAT-SimpleGUI original), SirBitesalot e contribuidores (CyberCAT), WolvenKit team.
- Source: https://github.com/bernardomcma/CyberCAT-SimpleGUI

## Antes de publicar

- [ ] Abrir e salvar um save com o exe do **Standalone** baixado da release (ainda não foi testado).
- [ ] Conferir a prévia da descrição no Nexus: o editor deles às vezes muda espaços em listas.
- [ ] No Cyberware, o título "OPERATING SYSTEM" aparece cortado em janelas mais estreitas (fica visível no print 03).
