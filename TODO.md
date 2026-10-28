# TODO / Roadmap

Lista de melhorias priorizadas. Marque `[x]` conforme for concluindo.

Cada item traz **Onde** (arquivos e trechos que provavelmente precisam mudar) e **Como** (abordagem sugerida). As linhas citadas referem-se ao código atual e podem mudar.

> **Convenção do projeto:** cada aba é um `UserControl` que implementa `IGameControl` ([Utils/IGameControl.cs](CP2077SaveEditor/Utils/IGameControl.cs)), é registrado em `MainForm()` via `RegisterControl(...)` e escuta `PropertyChanged` de `ActiveSaveFile` para se inicializar. O acesso ao save passa por [SaveFileHelper.cs](CP2077SaveEditor/Utils/SaveFileHelper.cs). Os dados estáticos do jogo ficam em `CP2077SaveEditor/Resources/*.json`, gerados pelo [ResourceGenerator](ResourceGenerator/Program.cs).

---

## ✅ Concluído

- [x] Mover itens entre inventário e cofre (`MoveItemsToStash` / `MoveItemsToInventory` em `InventoryControl.cs`)
- [x] Backups com data e hora a cada save (`sav.yyyyMMdd-HHmmss.bak`, mantém os 5 últimos — `MaxBackups` em `MainForm.cs`) + `sav.old` original preservado
- [x] Gravação segura: grava em `sav.dat.tmp`, relê com `CyberpunkSaveReader` (`VerifySaveFile`) e só então substitui o `sav.dat`
- [x] Aviso de alterações não salvas ao fechar ou abrir outro save (`ConfirmDiscardChanges`, compara o SHA-256 da serialização com `_savedStateHash`)
- [x] Botão **Save** + atalho `Ctrl+S` (`QuickSave`); o antigo botão virou **Save As...**
- [x] Copiar → Colar item (`CopyItem` / `PasteItem` / `CloneItemData` / `CloneItemStats` em `InventoryControl.cs` + fix em `CreateStatData`)
- [x] "Clear All Item Flags" com confirmação e opção de limpar apenas itens selecionados (`clearQuestFlagsButton_Click` em `InventoryControl.cs`)
- [x] Remover código morto do Debloat (`InventoryControl.cs` e `InventoryControl.Designer.cs`)
- [x] Corrigir falsos avisos de "unsaved changes" ao abrir abas ou `ItemDetails` (`_isLoading` em `PlayerStatsControl.cs`, `ItemDetails.cs`, `InventoryControl.cs` e `ModNodeDetails.cs`)
- [x] Suporte a High DPI (`Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` em `Program.cs`)
- [x] Corrigir build quebrada (`using WolvenKit.RED4.Save;` faltando em `InventoryControl.cs`, usado por `CloneItemStats`)
- [x] Saves de versão mais nova que a suportada abrem **somente leitura** (`_isReadOnly` / `MaxSupportedGameVersion` em `MainForm.cs`). Evita gravar um save 2.5 com o WolvenKit antigo, que apaga os stats
- [x] Veículos: lista regenerada do TweakDB 2.5 (+ Muramasa), aba com busca, **Unlock all** / **Lock all** (atuam sobre o resultado da busca), seleção múltipla, código limpo (`VehiclesControl.cs`)
- [x] `ResourceGenerator` aceita `[gameDir] [vehicles|items|mods|modifiergroups ...]`, compila com o WolvenKit atual (`new HashService()` registra os nomes do TweakDB), não quebra com records sem nome
- [x] Mensagens de erro ao carregar: versão do jogo no erro e no `error.txt`, dicas para "save mais novo que o editor" e "dados de mod desconhecidos", e o editor não fica habilitado com save carregado pela metade (`GetLoadErrorHint` em `MainForm.cs`)
- [x] "Add item" não quebra com itens sem nome conhecido (`AddItem.RefreshItems`)
- [x] **Suporte ao patch 2.5:** WolvenKit atualizado para o `main` (`65ea74b01`, com `update_2.5`), projetos em .NET 10 (`WFO1000` suprimido no `.csproj`). O round-trip do save 2.5 preserva o `StatsSystem` e o `ScriptableSystemsContainer`. O `PersistencySystem2` muda de tamanho, mas as 156 mil entradas ficam idênticas
- [x] **Stats no formato 2.5** ([OptimizedStatsHelper.cs](CP2077SaveEditor/Utils/OptimizedStatsHelper.cs)): expõe `gameSavedStatsDataOptimized` como `gameSavedStatsData`. O `FlushStats()` é chamado em `SerializeSave` e faz copy-on-write das fatias compartilhadas do `sharedDataBuffer`. Testado: materializar as 1.746 entradas e gravar dá um save idêntico; editar um item não afeta os que dividiam a fatia; clonar stats funciona; o flush é idempotente
- [x] `ItemClasses.json` regenerado do 2.5 (+191 itens, −27 loot tables obsoletas)
- [x] **Tier 5++**: botão "Make Tier 5++" no `ItemDetails` e no menu de contexto do inventário (vários itens de uma vez). `SetMaxTier` em `SaveFileHelper` usa o teto dos stats no TweakDB (`BaseStats.Quality` max 4, `BaseStats.IsItemPlus` max 2): soma modificadores aditivos até o total ser ≥ 4 / ≥ 2, então funciona para armas saqueadas e icônicas sem conhecer a qualidade base. Botão morto "Infuse Legendary Components" removido
- [x] **Add item** com busca (vários termos), filtro por tipo, lista, contador, duplo clique/Enter para adicionar e janela redimensionável
- [x] Busca do inventário: prefixos `quantity:`/`desc:` filtravam a coluna errada (desde que entrou a coluna Slot); adicionado `slot:` e os prefixos aparecem no placeholder. A ordenação por coluna já existia (clique no cabeçalho)
- [x] Barra de progresso (marquee) durante carregar/salvar/verificar e nome do save no título da janela
- [x] **Nomes do jogo** no inventário e no "Add item" (PT-BR se o Windows estiver em português, senão inglês). `ResourceGenerator ... names` gera `Resources/DisplayNames.json` a partir do `DisplayName` (LocKey) dos records de itens/veículos + `onscreens*.json` de `lang_en_text`/`lang_pt_text` (base e ep1). A coluna ID continua com o identificador técnico; a busca do "Add item" acha pelos dois
- [x] Aba de veículos com coluna "Name" (nome do jogo) e busca pelo nome
- [x] Removidos `items.bin` (30 MB, sem uso desde 2021) e o recurso `Mods.json` (não era usado; `ResourceGenerator ... mods` ainda gera o arquivo se precisar)
- [x] CI: `.github/workflows/build.yml` compila a cada push/PR (publish x64, zip como artefato) e anexa o zip à release em tags `v*`. README menciona o .NET 10 Desktop Runtime
- [x] Botão "Crafting components..." (Quick Actions do inventário): os 9 componentes de item/quickhack com nome do jogo, "Set all to 99999"; zero remove o item, valor > 0 cria se não existir ([CraftingComponentsDialog.cs](CP2077SaveEditor/Views/CraftingComponentsDialog.cs))
- [x] Logs: erros de carregar/salvar e exceções não tratadas vão para `logs\yyyy-MM-dd.log` ao lado do executável ([Logger.cs](CP2077SaveEditor/Utils/Logger.cs)); a mensagem mostra o caminho. Tratador global em `Program.cs` evita fechar o app sem registro
- [x] `Form2` renomeado para `MainForm` (classe, arquivos e handlers)
- [x] Aba **Perks** ([PerksControl.cs](CP2077SaveEditor/Views/Controls/PerksControl.cs) + [PerkHelper.cs](CP2077SaveEditor/Utils/PerkHelper.cs)): árvores 2.0 por atributo + Relic, nomes no idioma do Windows, tier, nível atual/máximo; duplo clique alterna, "Set level", "Max all", "Remove all"; opção de cobrar/devolver perk points (Relic points para Espionage). `ResourceGenerator ... perks` gera `Resources/Perks.json` (194 perks). Requisitos entre perks não são checados
- [x] Tela inicial "Saves" ([SaveBrowserControl.cs](CP2077SaveEditor/Views/Controls/SaveBrowserControl.cs)): lista as pastas de save com miniatura, nível, life path, gênero, tempo de jogo, patch, data e "modded" (do `metadata.9.json`), busca e filtro de autosaves; duplo clique abre (com aviso de alterações não salvas) e leva para a aba Player Stats
- [x] Testes de round-trip (`CP2077SaveEditor.Tests`, xUnit v3): save idêntico ao regravar, ler todos os stats não altera nada, edição não vaza para itens que dividem a fatia, Tier 5++ idempotente, clone independente, stats do jogador. Usam `CP2077_TEST_SAVE`; sem ela são pulados (CI). `SaveFileHelper.Serialize()` e `CloneStats()` agora são compartilhados pela UI e pelos testes
- [x] Correções após os testes de 28/10: crash da lista virtual ao encolher com itens selecionados (`TypeExtensions.RetrieveDefaultVirtualItem`); duplo clique no inventário abre a linha clicada (antes, o primeiro item selecionado); "Make Tier 5++" avisa quando o item já está no máximo; tooltips do `ModernButton` (label filho engolia o hover); CI sem zip duplo e com actions atualizadas
- [x] Aba Extras: "Unlock Fast Travel" só adiciona os pontos que faltam (antes apagava todos e recriava a partir do JSON, perdendo pontos que o JSON não conhecesse); confirmação/feedback e tooltips nas três ações
- [x] Ações em massa no menu do inventário: "Set Quantity..." (itens empilháveis) e "Make Tier 5++". Para mover tudo de um tipo: buscar `type:Weapon` e usar "Move All Listed Items to Stash"
- [x] Teste automatizado pelo `MainForm` real num save 2.5: carrega editável, sem aviso falso de alterações, Tier 5++ + salvar + verificar OK

---

## 🔴 Prioridade alta — correções

### Testar o patch 2.5 no jogo
- [ ] Editar um save 2.5 (cópia!) no app: mudar a Quality de uma arma, clonar um item, mudar dinheiro/atributos. Carregar no jogo e conferir que o item editado mudou, que **outras armas do mesmo modelo não mudaram** e que o save carrega sem erros.
- [ ] "Make Tier 5++" numa arma saqueada (ex.: Pulsar) e numa icônica (ex.: Warden): conferir no jogo que ficam Tier 5++ e que o dano subiu. As icônicas usam curvas próprias e são as mais incertas.
- [ ] Repetir com um save 2.3, se tiver, para garantir que o caminho antigo (`Keys`/`Values`) continua funcionando.
- **Riscos conhecidos:**
  - O significado das flags de `gameBufferSlice.Combined` (bits 30–31) foi deduzido do save: 0 = modificadores de item, 1 = modificadores de entidade (o jogador), 2 = modifier groups.
  - Quando o `sharedDataBuffer` enche, ele cresce para a próxima potência de 2 (o jogo usou 32768). Isso ainda não foi testado no jogo: o save atual tem ~10 KB livres.
  - Fatias que deixam de ser usadas continuam no buffer, e cada edição salva acrescenta uma fatia nova (sem compactação).

### Recursos estáticos — o que sobrou
- `Vehicles.json` ✅ e `ModifierGroups.json` (regenerado, sem mudanças) ✅.
- `ItemClasses.json` ✅ (regenerado com o WolvenKit novo; todos os nomes resolvem).
- **`FastTravel.json`** não tem gerador. Criar a partir dos records `gamedataFastTravelPoint_Record` (o `MarkerRef` precisa vir do mundo/quests; ver como os pontos atuais foram obtidos). **`Facts.json`** provavelmente veio de um dump externo; documentar a origem.
- **`AppearanceValues.json`:** `GenerateAppearanceValues` lê `basegame_1_engine.archive`; checar se ainda funciona.

### Testar na interface
- [ ] Aba de veículos: busca, Unlock all/Lock all com e sem filtro, double-click com vários selecionados.
- [ ] Editor de stats (`StatsControl`) em item de save 2.5: listar, adicionar e remover modificadores. O botão de criar "forced modifiers" fica desabilitado (o formato 2.5 não tem esse campo).
- [ ] A trava de somente leitura agora só dispara para saves de versão acima do 2.5 (`gameGameVersion.Current` = 2500).

---

## 🟠 Prioridade média — usabilidade

### Inventário

#### Tiers intermediários (1–5) e "+" exatos
- Hoje só existe "Tier 5++", que funciona porque o jogo limita o valor no máximo. Para um tier exato, é preciso conhecer a qualidade base, que vem do record (curvas `quality_curves`, `LootLevel`, `NPCWeaponDropRandomizer`, `MaxQualityWhenLooted`) e, nas icônicas, de `WasItemUpgraded` + `Quality=-4`.
- **Como:** ler os `statModifiers` do record no TweakDB (o harness `tdb` usado nesta investigação lê `BaseStats.Quality`) e avaliar as curvas, ou usar `Quality/Multiplier = 0` + aditivo, se o jogo aplicar Multiplier antes do Additive. Testar no jogo.


#### Cor por raridade
- **Onde:** criação do `ListViewItem` em `RefreshInventory` (já muda `BackColor` dos equipados).
- **Como:** ler `Quality` com `GetStatsFromItemId` e pintar o `ForeColor`. Cuidado com o desempenho: fazer cache por seed.

### Personagem

#### Respec — baixa prioridade
- Desde o 2.0 o jogo devolve perks de graça no próprio menu e oferece um reset de atributos; a aba Player Stats já edita atributos e pontos (`DevPoints`) diretamente. Só vale fazer se alguém pedir.
- **Onde, se for fazer:** `PlayerDevelopmentData.AttributesData[].UnlockedPerks` (perks 2.0) e `DevPoints` (`Attribute`, `Primary`, `Espionage`) via `GetPlayerDevelopmentData()`.

#### Cyberware
- **Onde:** os slots equipados estão em `GetEquipAreas()` ([SaveFileHelper.cs:112](CP2077SaveEditor/Utils/SaveFileHelper.cs:112)) — áreas `*Cyberware*`. A capacidade é um stat do jogador (`GetStatsFromEntityId(1)`, stat `Humanity`/`CyberwareCapacity` no 2.x).
- **Como:** uma aba que liste as áreas de cyberware com o item equipado (reaproveitar `EquipInventoryItem`/`UnequipInventoryItem` do inventário) e um campo de capacidade extra via `SetConstantStat`.

#### Aba de aparência
- **Onde:** [AppearanceControl.cs](CP2077SaveEditor/Views/Controls/AppearanceControl.cs) (gera os campos via reflection sobre `AppearanceHelper2`, escondendo `[Browsable(false)]` fora do modo debug); [AppearanceHelper.cs](CP2077SaveEditor/Utils/AppearanceHelper.cs) com 2000 linhas; preview em `Resources/*.png` sem uso real.
- **Como:** agrupar os campos por seção (rosto, cabelo, corpo) em vez de uma lista solta; formato de preset JSON compartilhável (já existe [PresetDto.cs](CP2077SaveEditor/Utils/PresetDto.cs)); remover o código comentado do `Global.IsDebug`.

### Mundo
- [ ] **Gigs / NCPD:** são quest facts. Criar uma aba ou seção no [QuestFactsControl.cs](CP2077SaveEditor/Views/Controls/QuestFactsControl.cs) com presets de facts por gig (padrão já usado em `enableSecretEndingButton_Click` e `makeAllRomanceableButton_Click`, que chamam `SetFactByName`).
- [ ] **Fast travel:** "Unlock all" já existe em `ExtrasControl.btn_UnlockAll_Click` e depende do `FastTravel.json` atualizado. Ver "Recursos estáticos".
- [ ] **Guarda-roupa:** inventário `0x7901DE03D136A5AF` ("V's Wardrobe" em `_inventoryNames`) e `ModSupport/ExtraWardrobeSlots`. Avaliar um editor de conjuntos.

---

## 🟡 GUI moderna

#### Tema escuro e visual Cyberpunk
- **Onde:** as cores estão fixas no Designer (`Color.WhiteSmoke`, `LightGray` em `MainForm.Designer.cs` e nos controles); os botões customizados são `ModernButton`/`ModernValuePicker` em [Utils/Controls.cs](CP2077SaveEditor/Utils/Controls.cs).
- **Como:** criar um `Theme.cs` (paleta clara e escura) e uma função `ApplyTheme(Control)` recursiva chamada no `MainForm()` e ao abrir cada diálogo. Fazer `ModernButton` ler as cores do tema. Para o modo escuro do Windows, ler `HKCU\...\Themes\Personalize\AppsUseLightTheme`. No .NET 9+ existe `Application.SetColorMode`, mas o projeto está em .NET 8.

#### DPI alto e redimensionamento
- **Onde:** [Program.cs](CP2077SaveEditor/Program.cs) (sem `SetHighDpiMode`); `AutoScaleMode.Font` em todos os Designers; tamanhos fixos (`ClientSize = 1024x594`).
- **Como:** `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` antes do `Run`, e testar com 150%. Revisar os `Anchor`/`Dock` dos controles e trocar posicionamento absoluto por `TableLayoutPanel` onde quebrar.

#### Barra lateral com ícones
- **Onde:** [ScrollMenuControl.cs](CP2077SaveEditor/Views/Controls/ScrollMenuControl.cs) (`AddButton(text, handler)`) e `RegisterControl` em `MainForm.cs`.
- **Como:** adicionar uma propriedade `Icon` em `IGameControl` e um destaque visual no botão ativo, guardando o botão selecionado em `SwitchPanel`.

#### Indicador de alterações não salvas
- **Onde:** `MainForm.cs` — hoje só existe a comparação de hash, sob demanda.
- **Como:** um hash por edição seria caro (~3 s). Alternativa: marcar "possivelmente alterado" (`*` no `Text` do form) em qualquer `ValueChanged`/`Click` dentro do `pnl_Content` (handler recursivo ou `IMessageFilter`) e usar o hash só para confirmar ao fechar.

#### Desfazer/refazer
- **Como:** um snapshot completo por edição é pesado. Opção viável: a cada "Apply" ou ação, serializar com `SerializeSave()` em background e guardar até N snapshots comprimidos. Desfazer recarrega via `CyberpunkSaveReader` a partir do snapshot. Exige que os controles saibam reinicializar (já fazem isso ao trocar `ActiveSaveFile`).

#### Tooltips e tradução
- **Como:** um `ToolTip` por controle (Designer) explicando o risco das ações destrutivas. Para tradução, mover os textos para `Properties/Resources.resx` + `Resources.pt-BR.resx` e trocar as strings literais (ex.: `"Saving into the root folder isn't allowed"`) por `Resources.Xxx`.

#### WPF/Avalonia?
- **Recomendação:** só depois dos itens acima. A lógica está acoplada aos controles WinForms (por exemplo, o `InventoryControl` mexe direto no save). Um primeiro passo seria extrair a lógica para serviços (`InventoryService` etc.), o que já ajuda nos testes.

---

## 🔵 Infraestrutura e manutenção

- [ ] **README:** trocar .NET 5/VS2019 por .NET 8/VS2022, explicar o `git submodule update --init`, atualizar a tabela de versões do jogo (2.3), adicionar prints e as features novas.
- [ ] **ResourceGenerator com um comando só:** em [ResourceGenerator/Program.cs](ResourceGenerator/Program.cs), `_gameDir` está fixo (`C:\Games\Steam\...`) e as chamadas estão comentadas. Receber o caminho do jogo por `args[0]` e um `--all`, gravando direto em `CP2077SaveEditor/Resources/`.
- [ ] **Checar atualização:** ao abrir, consultar `https://api.github.com/repos/<owner>/<repo>/releases/latest` e comparar com a versão (`tssl_Info.Text = "v0.28c"` no `MainForm.Designer.cs`; mover para `<Version>` no `.csproj` e ler via `Assembly`).
- [ ] **Refatorar arquivos grandes:** `InventoryControl.cs` (~1080 linhas) → separar a lógica de cofre (`MoveItemsTo*`), o menu de contexto e o clipboard em classes próprias. `AppearanceHelper.cs` (~2000 linhas) parece substituído por `AppearanceHelper2.cs`; verificar o uso e remover.
- [ ] **Warnings:** a build gera ~400 warnings (muitos de nulabilidade e campos não usados). Limpar por arquivo.
- [ ] **`config.json`:** `EnablePSData` é lido mas ignorado (`MainForm()`, código comentado). Remover ou reativar, e documentar as chaves.
