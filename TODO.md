# TODO / Roadmap

Lista de melhorias priorizadas. Marque `[x]` conforme for concluindo.

Cada item traz **Onde** (arquivos e trechos que provavelmente precisam mudar) e **Como** (abordagem sugerida). As linhas citadas referem-se ao código atual e podem mudar.

> **Convenção do projeto:** cada aba é um `UserControl` que implementa `IGameControl` ([Utils/IGameControl.cs](CP2077SaveEditor/Utils/IGameControl.cs)), é registrado em `Form2()` via `RegisterControl(...)` e escuta `PropertyChanged` de `ActiveSaveFile` para se inicializar. O acesso ao save passa por [SaveFileHelper.cs](CP2077SaveEditor/Utils/SaveFileHelper.cs). Os dados estáticos do jogo ficam em `CP2077SaveEditor/Resources/*.json`, gerados pelo [ResourceGenerator](ResourceGenerator/Program.cs).

---

## ✅ Concluído

- [x] Mover itens entre inventário e cofre (`MoveItemsToStash` / `MoveItemsToInventory` em `InventoryControl.cs`)
- [x] Backups com data e hora a cada save (`sav.yyyyMMdd-HHmmss.bak`, mantém os 5 últimos — `MaxBackups` em `Form2.cs`) + `sav.old` original preservado
- [x] Gravação segura: grava em `sav.dat.tmp`, relê com `CyberpunkSaveReader` (`VerifySaveFile`) e só então substitui o `sav.dat`
- [x] Aviso de alterações não salvas ao fechar ou abrir outro save (`ConfirmDiscardChanges`, compara o SHA-256 da serialização com `_savedStateHash`)
- [x] Botão **Save** + atalho `Ctrl+S` (`QuickSave`); o antigo botão virou **Save As...**
- [x] Copiar → Colar item (`CopyItem` / `PasteItem` / `CloneItemData` / `CloneItemStats` em `InventoryControl.cs` + fix em `CreateStatData`)
- [x] "Clear All Item Flags" com confirmação e opção de limpar apenas itens selecionados (`clearQuestFlagsButton_Click` em `InventoryControl.cs`)
- [x] Remover código morto do Debloat (`InventoryControl.cs` e `InventoryControl.Designer.cs`)
- [x] Corrigir falsos avisos de "unsaved changes" ao abrir abas ou `ItemDetails` (`_isLoading` em `PlayerStatsControl.cs`, `ItemDetails.cs`, `InventoryControl.cs` e `ModNodeDetails.cs`)
- [x] Suporte a High DPI (`Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` em `Program.cs`)
- [x] Corrigir build quebrada (`using WolvenKit.RED4.Save;` faltando em `InventoryControl.cs`, usado por `CloneItemStats`)
- [x] Saves de versão mais nova que a suportada abrem **somente leitura** (`_isReadOnly` / `MaxSupportedGameVersion` em `Form2.cs`). Evita gravar um save 2.5 com o WolvenKit antigo, que apaga os stats
- [x] Veículos: lista regenerada do TweakDB 2.5 (+ Muramasa), aba com busca, **Unlock all** / **Lock all** (atuam sobre o resultado da busca), seleção múltipla, código limpo (`VehiclesControl.cs`)
- [x] `ResourceGenerator` aceita `[gameDir] [vehicles|items|mods|modifiergroups ...]`, compila com o WolvenKit atual (`new HashService()` registra os nomes do TweakDB), não quebra com records sem nome
- [x] Mensagens de erro ao carregar: versão do jogo no erro e no `error.txt`, dicas para "save mais novo que o editor" e "dados de mod desconhecidos", e o editor não fica habilitado com save carregado pela metade (`GetLoadErrorHint` em `Form2.cs`)
- [x] "Add item" não quebra com itens sem nome conhecido (`AddItem.RefreshItems`)
- [x] **Suporte ao patch 2.5:** WolvenKit atualizado para o `main` (`65ea74b01`, com `update_2.5`), projetos em .NET 10 (`WFO1000` suprimido no `.csproj`). O round-trip do save 2.5 preserva o `StatsSystem` e o `ScriptableSystemsContainer`. O `PersistencySystem2` muda de tamanho, mas as 156 mil entradas ficam idênticas
- [x] **Stats no formato 2.5** ([OptimizedStatsHelper.cs](CP2077SaveEditor/Utils/OptimizedStatsHelper.cs)): expõe `gameSavedStatsDataOptimized` como `gameSavedStatsData`. O `FlushStats()` é chamado em `SerializeSave` e faz copy-on-write das fatias compartilhadas do `sharedDataBuffer`. Testado: materializar as 1.746 entradas e gravar dá um save idêntico; editar um item não afeta os que dividiam a fatia; clonar stats funciona; o flush é idempotente
- [x] `ItemClasses.json` regenerado do 2.5 (+191 itens, −27 loot tables obsoletas)
- [x] **Tier 5++**: botão "Make Tier 5++" no `ItemDetails` e no menu de contexto do inventário (vários itens de uma vez). `SetMaxTier` em `SaveFileHelper` usa o teto dos stats no TweakDB (`BaseStats.Quality` max 4, `BaseStats.IsItemPlus` max 2): soma modificadores aditivos até o total ser ≥ 4 / ≥ 2, então funciona para armas saqueadas e icônicas sem conhecer a qualidade base. Botão morto "Infuse Legendary Components" removido
- [x] **Add item** com busca (vários termos), filtro por tipo, lista, contador, duplo clique/Enter para adicionar e janela redimensionável
- [x] Busca do inventário: prefixos `quantity:`/`desc:` filtravam a coluna errada (desde que entrou a coluna Slot); adicionado `slot:` e os prefixos aparecem no placeholder. A ordenação por coluna já existia (clique no cabeçalho)
- [x] Barra de progresso (marquee) durante carregar/salvar/verificar e nome do save no título da janela
- [x] **Nomes do jogo** no inventário e no "Add item" (PT-BR se o Windows estiver em português, senão inglês). `ResourceGenerator ... names` gera `Resources/DisplayNames.json` a partir do `DisplayName` (LocKey) dos records de itens/veículos + `onscreens*.json` de `lang_en_text`/`lang_pt_text` (base e ep1). A coluna ID continua com o identificador técnico; a busca do "Add item" acha pelos dois
- [x] Teste automatizado pelo `Form2` real num save 2.5: carrega editável, sem aviso falso de alterações, Tier 5++ + salvar + verificar OK

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
- **`Mods.json` não é usado** pelo app (só existe o acessor em `Properties/Resources.Designer.cs`). Remover do `.resx` ou usar na tarefa de mods do `ItemDetails`.
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


#### Ações em massa
- **Onde:** o menu de contexto é montado em `inventoryListView_MouseDown` ([InventoryControl.cs:376](CP2077SaveEditor/Views/Controls/InventoryControl.cs:376)), que já trata múltipla seleção (`Delete {n} Items`, mover para o cofre).
- **Como:** adicionar ao menu "Set Tier…" (aplica a lógica da tarefa do lendário a todos os selecionados), "Set quantity…" e "Move all of type X". Seguir o padrão de `MoveSelectedToStash` (coleta `ItemData` do `Tag` e depois chama `RefreshInventory()`).

#### Componentes de crafting
- **Onde:** o dinheiro já tem campo (`moneyUpDown_ValueChanged`, `:72`). Os componentes são itens `Items.*Material*`/`*Component*` no inventário 1.
- **Como:** generalizar o padrão do dinheiro: um painel com `NumericUpDown` por componente, procurando o item por ID e criando-o se não existir (lógica de quantidade do `AddItem.btn_Add_Click` para itens `IsSingleInstance`).

#### Cor por raridade
- **Onde:** criação do `ListViewItem` em `RefreshInventory` (já muda `BackColor` dos equipados).
- **Como:** ler `Quality` com `GetStatsFromItemId` e pintar o `ForeColor`. Cuidado com o desempenho: fazer cache por seed.

### Personagem

#### Editor de perks
- **Onde:** [PlayerStatsControl.cs](CP2077SaveEditor/Views/Controls/PlayerStatsControl.cs) — hoje edita só atributos, proficiências e pontos (`_attributeFields`, `_proficiencyFields`, `_devPointFields`). Os dados vêm de `GetPlayerDevelopmentData()` ([SaveFileHelper.cs:63](CP2077SaveEditor/Utils/SaveFileHelper.cs:63)).
- **Como:** explorar `PlayerDevelopmentData` (no 2.x os perks ficam em `AttributesData`/`Perks` e `NewPerks`) e criar um `PerksControl` (nova aba registrada em `Form2()`) com uma árvore por atributo. Gerar a lista de perks (`gamedataNewPerk_Record`) pelo `ResourceGenerator`.

#### Respec
- **Onde:** mesmo `PlayerDevelopmentData`.
- **Como:** zerar os perks comprados e somar os pontos de volta em `DevPoints` (`gamedataDevelopmentPointType.Primary`). Para atributos, voltar a 3 e devolver a diferença em `Attribute`.

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
- **Onde:** as cores estão fixas no Designer (`Color.WhiteSmoke`, `LightGray` em `Form2.Designer.cs` e nos controles); os botões customizados são `ModernButton`/`ModernValuePicker` em [Utils/Controls.cs](CP2077SaveEditor/Utils/Controls.cs).
- **Como:** criar um `Theme.cs` (paleta clara e escura) e uma função `ApplyTheme(Control)` recursiva chamada no `Form2()` e ao abrir cada diálogo. Fazer `ModernButton` ler as cores do tema. Para o modo escuro do Windows, ler `HKCU\...\Themes\Personalize\AppsUseLightTheme`. No .NET 9+ existe `Application.SetColorMode`, mas o projeto está em .NET 8.

#### DPI alto e redimensionamento
- **Onde:** [Program.cs](CP2077SaveEditor/Program.cs) (sem `SetHighDpiMode`); `AutoScaleMode.Font` em todos os Designers; tamanhos fixos (`ClientSize = 1024x594`).
- **Como:** `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` antes do `Run`, e testar com 150%. Revisar os `Anchor`/`Dock` dos controles e trocar posicionamento absoluto por `TableLayoutPanel` onde quebrar.

#### Barra lateral com ícones
- **Onde:** [ScrollMenuControl.cs](CP2077SaveEditor/Views/Controls/ScrollMenuControl.cs) (`AddButton(text, handler)`) e `RegisterControl` em `Form2.cs`.
- **Como:** adicionar uma propriedade `Icon` em `IGameControl` e um destaque visual no botão ativo, guardando o botão selecionado em `SwitchPanel`.

#### Indicador de alterações não salvas
- **Onde:** `Form2.cs` — hoje só existe a comparação de hash, sob demanda.
- **Como:** um hash por edição seria caro (~3 s). Alternativa: marcar "possivelmente alterado" (`*` no `Text` do form) em qualquer `ValueChanged`/`Click` dentro do `pnl_Content` (handler recursivo ou `IMessageFilter`) e usar o hash só para confirmar ao fechar.

#### Tela inicial com saves recentes
- **Onde:** novo `UserControl` (ex.: `SaveBrowserControl`) registrado como landing page em `Form2()` (hoje é `PlayerStatsControl`); pasta padrão em `SavesRootDirectory`.
- **Como:** listar as subpastas ordenadas por data, mostrando `screenshot.png` como miniatura e lendo de `metadata.9.json` o nome, nível, life path e tempo de jogo (`System.Text.Json`). O double-click chama `LoadSave`. Guardar os recentes em `config.json`.

#### Desfazer/refazer
- **Como:** um snapshot completo por edição é pesado. Opção viável: a cada "Apply" ou ação, serializar com `SerializeSave()` em background e guardar até N snapshots comprimidos. Desfazer recarrega via `CyberpunkSaveReader` a partir do snapshot. Exige que os controles saibam reinicializar (já fazem isso ao trocar `ActiveSaveFile`).

#### Tooltips e tradução
- **Como:** um `ToolTip` por controle (Designer) explicando o risco das ações destrutivas. Para tradução, mover os textos para `Properties/Resources.resx` + `Resources.pt-BR.resx` e trocar as strings literais (ex.: `"Saving into the root folder isn't allowed"`) por `Resources.Xxx`.

#### WPF/Avalonia?
- **Recomendação:** só depois dos itens acima. A lógica está acoplada aos controles WinForms (por exemplo, o `InventoryControl` mexe direto no save). Um primeiro passo seria extrair a lógica para serviços (`InventoryService` etc.), o que já ajuda nos testes.

---

## 🔵 Infraestrutura e manutenção

- [ ] **CI (GitHub Actions):** criar `.github/workflows/build.yml` com `actions/checkout` (`submodules: recursive`), `setup-dotnet` 8.x e `dotnet publish CP2077SaveEditor -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`. Em tags `v*`, anexar o zip à release.
- [ ] **Testes de round-trip:** novo projeto `CP2077SaveEditor.Tests` (xUnit) referenciando o app. Testar carregar → `SerializeSave` → reler → comparar hash, e editar → hash diferente (o harness usado na feature de backup já faz isso). Os saves de exemplo não devem ir para o git; ler de uma pasta configurável por variável de ambiente.
- [ ] **README:** trocar .NET 5/VS2019 por .NET 8/VS2022, explicar o `git submodule update --init`, atualizar a tabela de versões do jogo (2.3), adicionar prints e as features novas.
- [ ] **`items.bin` (30 MB) não é usado:** nenhuma referência no código desde 2021 (commit `5660be1`, da era CyberCAT). Remover com `git rm items.bin`.
- [ ] **ResourceGenerator com um comando só:** em [ResourceGenerator/Program.cs](ResourceGenerator/Program.cs), `_gameDir` está fixo (`C:\Games\Steam\...`) e as chamadas estão comentadas. Receber o caminho do jogo por `args[0]` e um `--all`, gravando direto em `CP2077SaveEditor/Resources/`.
- [ ] **Checar atualização:** ao abrir, consultar `https://api.github.com/repos/<owner>/<repo>/releases/latest` e comparar com a versão (`tssl_Info.Text = "v0.28c"` no `Form2.Designer.cs`; mover para `<Version>` no `.csproj` e ler via `Assembly`).
- [ ] **Logs:** hoje os erros vão para `error.txt` no diretório atual (`Form2.cs`, `LoadSave` e `SaveToDirectory`). Criar um `Logger` simples que grava em `logs/yyyy-MM-dd.log` com o stack trace completo.
- [ ] **Refatorar arquivos grandes:** `InventoryControl.cs` (~1080 linhas) → separar a lógica de cofre (`MoveItemsTo*`), o menu de contexto e o clipboard em classes próprias. `AppearanceHelper.cs` (~2000 linhas) parece substituído por `AppearanceHelper2.cs`; verificar o uso e remover.
- [ ] **Warnings:** a build gera ~400 warnings (muitos de nulabilidade e campos não usados). Limpar por arquivo.
- [ ] **Renomear `Form2` → `MainForm`:** renomear a classe, os arquivos `.cs`/`.Designer.cs`/`.resx` e todas as referências (`Form2 parentForm` em todos os controles).
- [ ] **`config.json`:** `EnablePSData` é lido mas ignorado (`Form2()`, código comentado). Remover ou reativar, e documentar as chaves.
