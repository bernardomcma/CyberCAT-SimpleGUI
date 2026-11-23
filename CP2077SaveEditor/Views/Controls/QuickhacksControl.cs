using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CP2077SaveEditor.ModSupport;
using CP2077SaveEditor.Utils;
using WolvenKit.RED4.Save.Classes;
using WolvenKit.RED4.Types;

namespace CP2077SaveEditor.Views.Controls;

public class QuickhacksControl : UserControl, IGameControl
{
    private sealed record QuickhackChoice(
        TweakDBID Id,
        string Label,
        ItemData OwnedItem = null,
        QuickhackHelper.QuickhackVariant Variant = null)
    {
        public override string ToString() => Label;
    }

    private sealed record LoadoutChoice(QuickhackLoadout Loadout, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly MainForm _parentForm;
    private readonly Label _deckStatus;
    private readonly ListView _slots;
    private readonly ComboBox _quickhackChoices;
    private readonly Button _setQuickhack;
    private readonly Button _unequipQuickhack;
    private readonly Button _swapSlots;
    private readonly Button _createSlots;
    private readonly Button _addSlot;
    private readonly Button _removeSlot;
    private readonly ListBox _loadouts;
    private readonly ListBox _loadoutQuickhacks;
    private readonly Label _loadoutStatus;
    private readonly ComboBox _catalogueQuickhacks;
    private readonly Button _addToLoadout;
    private readonly Button _removeFromLoadout;
    private readonly Button _moveLoadoutHackUp;
    private readonly Button _moveLoadoutHackDown;

    private QuickhackHelper _helper;
    private ItemData _cyberdeck;
    private QuickhackLoadoutSystem _loadoutSystem;

    public QuickhacksControl(MainForm parentForm)
    {
        _parentForm = parentForm;
        _parentForm.PropertyChanged += OnParentFormPropertyChanged;

        _deckStatus = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _slots = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false,
            MultiSelect = true
        };
        _slots.Columns.Add("Slot", 120);
        _slots.Columns.Add("Installed quickhack", 420);
        _slots.SelectedIndexChanged += (_, _) => UpdateVanillaButtons();
        _slots.DoubleClick += (_, _) => InstallSelectedQuickhack();

        _quickhackChoices = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 260,
            Margin = new Padding(3, 5, 6, 3)
        };
        _quickhackChoices.SelectedIndexChanged += (_, _) => UpdateVanillaButtons();

        _setQuickhack = new Button { Text = "Install / Replace", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _setQuickhack.Click += (_, _) => InstallSelectedQuickhack();

        _unequipQuickhack = new Button { Text = "Unequip", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _unequipQuickhack.Click += (_, _) => UnequipSelectedQuickhack();

        _swapSlots = new Button { Text = "Swap selected slots", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _swapSlots.Click += (_, _) => SwapSelectedSlots();

        _createSlots = new Button { Text = "Create 4 standard slots", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _createSlots.Click += (_, _) => CreateStandardSlots();

        _addSlot = new Button { Text = "Add new slot", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _addSlot.Click += (_, _) => AddQuickhackSlot();

        _removeSlot = new Button { Text = "Remove selected slot", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _removeSlot.Click += (_, _) => RemoveSelectedSlot();

        var vanillaActions = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 78,
            ColumnCount = 1,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        vanillaActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        vanillaActions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        vanillaActions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var quickhackActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(3, 1, 3, 1)
        };
        quickhackActions.Controls.AddRange(new Control[]
        {
            _quickhackChoices, _setQuickhack, _unequipQuickhack
        });

        var slotActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(3, 1, 3, 1)
        };
        slotActions.Controls.AddRange(new Control[] { _addSlot, _removeSlot, _createSlots, _swapSlots });

        vanillaActions.Controls.Add(quickhackActions, 0, 0);
        vanillaActions.Controls.Add(slotActions, 0, 1);

        var vanillaTab = new TabPage("Equipped");
        vanillaTab.Controls.Add(_slots);
        vanillaTab.Controls.Add(vanillaActions);
        vanillaTab.Controls.Add(_deckStatus);

        _loadouts = new ListBox { Dock = DockStyle.Left, Width = 240 };
        _loadouts.SelectedIndexChanged += (_, _) => RefreshLoadoutQuickhacks();

        _loadoutQuickhacks = new ListBox { Dock = DockStyle.Fill };
        _loadoutQuickhacks.SelectedIndexChanged += (_, _) => UpdateLoadoutButtons();

        _catalogueQuickhacks = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 330,
            Margin = new Padding(3, 5, 6, 3)
        };
        _catalogueQuickhacks.SelectedIndexChanged += (_, _) => UpdateLoadoutButtons();

        _addToLoadout = new Button { Text = "Add to loadout", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _addToLoadout.Click += (_, _) => AddQuickhackToLoadout();

        _removeFromLoadout = new Button { Text = "Remove", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _removeFromLoadout.Click += (_, _) => RemoveQuickhackFromLoadout();

        _moveLoadoutHackUp = new Button { Text = "Move up", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _moveLoadoutHackUp.Click += (_, _) => MoveLoadoutQuickhack(-1);

        _moveLoadoutHackDown = new Button { Text = "Move down", AutoSize = true, Margin = new Padding(3, 3, 6, 3) };
        _moveLoadoutHackDown.Click += (_, _) => MoveLoadoutQuickhack(1);

        var loadoutActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(3, 1, 3, 1)
        };
        loadoutActions.Controls.AddRange(new Control[]
        {
            _catalogueQuickhacks, _addToLoadout, _removeFromLoadout, _moveLoadoutHackUp, _moveLoadoutHackDown
        });

        _loadoutStatus = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var loadoutTab = new TabPage("Mod Loadouts");
        loadoutTab.Controls.Add(_loadoutQuickhacks);
        loadoutTab.Controls.Add(_loadouts);
        loadoutTab.Controls.Add(loadoutActions);
        loadoutTab.Controls.Add(_loadoutStatus);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(vanillaTab);
        tabs.TabPages.Add(loadoutTab);
        Controls.Add(tabs);

        var toolTip = new ToolTip();
        Theme.SetToolTip(toolTip, _setQuickhack, "Install the selected quickhack in the cyberdeck slot. Unowned quickhacks are added to V's inventory; displaced quickhacks return there.");
        Theme.SetToolTip(toolTip, _unequipQuickhack, "Move the installed quickhack back to V's inventory.");
        Theme.SetToolTip(toolTip, _swapSlots, "Swap the quickhacks in the two selected cyberdeck slots.");
        Theme.SetToolTip(toolTip, _createSlots, "Add four empty CyberdeckProgram slots when the equipped deck has no saved quickhack slots.");
        Theme.SetToolTip(toolTip, _addSlot, "Add an empty quickhack slot using the next available CyberdeckProgram index.");
        Theme.SetToolTip(toolTip, _removeSlot, "Remove the selected quickhack slot. Any installed quickhack returns to V's inventory.");

        RefreshPage();
    }

    public string GameControlName => "Quickhacks";
    public string Icon => "";

    private void OnParentFormPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_parentForm.ActiveSaveFile))
        {
            this.InvokeIfRequired(RefreshPage);
        }
    }

    private void RefreshPage()
    {
        var saveFile = _parentForm.ActiveSaveFile;
        _helper = saveFile == null ? null : new QuickhackHelper(saveFile);
        _cyberdeck = _helper?.GetEquippedCyberdeck();
        _loadoutSystem = _helper?.GetModLoadoutSystem();

        RefreshVanillaSlots();
        RefreshLoadouts();
    }

    private void RefreshVanillaSlots()
    {
        _slots.BeginUpdate();
        _slots.Items.Clear();
        _quickhackChoices.Items.Clear();

        if (_helper == null)
        {
            _deckStatus.Text = "Open a save to edit quickhacks.";
            _slots.EndUpdate();
            UpdateVanillaButtons();
            return;
        }

        if (_cyberdeck == null)
        {
            _deckStatus.Text = "No cyberdeck is equipped in the Operating System slot, or the equipped deck is missing from V's inventory.";
            _slots.EndUpdate();
            UpdateVanillaButtons();
            return;
        }

        var slots = QuickhackHelper.GetQuickhackSlots(_cyberdeck);
        _deckStatus.Text = $"Equipped cyberdeck: {QuickhackHelper.GetDisplayName(_cyberdeck.ItemInfo.ItemId.Id)}";
        foreach (var slot in slots)
        {
            var slotIndex = QuickhackHelper.GetSlotIndex(slot);
            var quickhackId = slot.ItemInfo?.ItemId?.Id ?? 0;
            var hackName = (ulong)quickhackId == 0
                ? "(empty)"
                : QuickhackHelper.IsQuickhack(quickhackId)
                    ? QuickhackHelper.GetQuickhackVariant(slot.ItemInfo.ItemId, _parentForm.ActiveSaveFile).Label
                    : QuickhackHelper.GetDisplayName(quickhackId);
            _slots.Items.Add(new ListViewItem(new[] { $"Program {slotIndex}", hackName }) { Tag = slot });
        }

        var ownedQuickhacks = QuickhackHelper.GetOwnedQuickhacks(_parentForm.ActiveSaveFile);
        var ownedVariants = ownedQuickhacks
            .Select(x => QuickhackHelper.GetQuickhackVariant(x, _parentForm.ActiveSaveFile))
            .ToList();
        foreach (var item in ownedQuickhacks)
        {
            var itemId = item.ItemInfo.ItemId.Id;
            _quickhackChoices.Items.Add(new QuickhackChoice(
                itemId,
                $"{QuickhackHelper.GetDisplayName(item, _parentForm.ActiveSaveFile)} [Inventory]",
                item));
        }

        var knownVariantKeys = ownedVariants.Select(x => x.Key).ToHashSet();
        foreach (var slot in slots.Where(x => (ulong)(x.ItemInfo?.ItemId?.Id ?? 0) != 0 &&
                                              QuickhackHelper.IsQuickhack(x.ItemInfo.ItemId.Id)))
        {
            knownVariantKeys.Add(
                QuickhackHelper.GetQuickhackVariant(slot.ItemInfo.ItemId, _parentForm.ActiveSaveFile).Key);
        }

        foreach (var variant in QuickhackHelper.GetQuickhackVariants()
                     .Where(x => !knownVariantKeys.Contains(x.Key)))
        {
            _quickhackChoices.Items.Add(new QuickhackChoice(
                variant.ItemId,
                $"{variant.Label} [Not owned]",
                Variant: variant));
        }

        _slots.EndUpdate();
        UpdateVanillaButtons();
    }

    private void UpdateVanillaButtons()
    {
        var selectedCount = _slots.SelectedItems.Count;
        var hasDeck = _cyberdeck != null;
        var hasSlots = QuickhackHelper.GetQuickhackSlots(_cyberdeck).Count > 0;
        _setQuickhack.Enabled = hasDeck && selectedCount == 1 && _quickhackChoices.SelectedItem is QuickhackChoice;
        _unequipQuickhack.Enabled = hasDeck && selectedCount == 1 &&
                                    _slots.SelectedItems[0].Tag is ItemSlotPart slot &&
                                    (ulong)(slot.ItemInfo?.ItemId?.Id ?? 0) != 0;
        _swapSlots.Enabled = hasDeck && selectedCount == 2;
        _createSlots.Visible = hasDeck && !hasSlots;
        _createSlots.Enabled = hasDeck && !hasSlots;
        _addSlot.Enabled = hasDeck;
        _removeSlot.Enabled = hasDeck && selectedCount == 1;
        _quickhackChoices.Enabled = hasDeck && hasSlots;
    }

    private void InstallSelectedQuickhack()
    {
        if (_quickhackChoices.SelectedItem is not QuickhackChoice choice)
        {
            return;
        }

        if (choice.OwnedItem != null)
        {
            SetSelectedQuickhack(choice.OwnedItem);
        }
        else if (choice.Variant != null)
        {
            SetSelectedQuickhack(choice.Variant);
        }
    }

    private void UnequipSelectedQuickhack()
    {
        SetSelectedQuickhack((ItemData)null);
    }

    private void SetSelectedQuickhack(ItemData replacement)
    {
        ApplySelectedQuickhack(slot => _helper.SetQuickhack(_cyberdeck, slot, replacement));
    }

    private void SetSelectedQuickhack(QuickhackHelper.QuickhackVariant replacement)
    {
        ApplySelectedQuickhack(slot => _helper.SetQuickhack(_cyberdeck, slot, replacement));
    }

    private void ApplySelectedQuickhack(Func<ItemSlotPart, bool> setQuickhack)
    {
        if (_helper == null || _cyberdeck == null || _slots.SelectedItems.Count != 1 ||
            _slots.SelectedItems[0].Tag is not ItemSlotPart slot)
        {
            return;
        }

        try
        {
            if (setQuickhack(slot))
            {
                ChangeTracker.MarkChanged();
                RefreshVanillaSlots();
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(this, ex.Message, "Quickhacks", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SwapSelectedSlots()
    {
        if (_slots.SelectedItems.Count != 2 ||
            _slots.SelectedItems[0].Tag is not ItemSlotPart first ||
            _slots.SelectedItems[1].Tag is not ItemSlotPart second)
        {
            return;
        }

        if (QuickhackHelper.SwapQuickhacks(first, second))
        {
            ChangeTracker.MarkChanged();
            RefreshVanillaSlots();
        }
    }

    private void CreateStandardSlots()
    {
        if (_helper == null || _cyberdeck == null)
        {
            return;
        }

        var result = MessageBox.Show(
            "This cyberdeck has no saved quickhack slot entries. Create four empty standard slots (CyberdeckProgram1-4)?",
            "Quickhacks",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _helper.AddStandardSlots(_cyberdeck);
        ChangeTracker.MarkChanged();
        RefreshVanillaSlots();
    }

    private void AddQuickhackSlot()
    {
        if (_helper == null || _cyberdeck == null)
        {
            return;
        }

        try
        {
            var slot = _helper.AddQuickhackSlot(_cyberdeck);
            ChangeTracker.MarkChanged();
            RefreshVanillaSlots();

            var addedItem = _slots.Items.Cast<ListViewItem>().FirstOrDefault(x => ReferenceEquals(x.Tag, slot));
            if (addedItem != null)
            {
                addedItem.Selected = true;
                addedItem.Focused = true;
                addedItem.EnsureVisible();
            }
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Quickhacks", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveSelectedSlot()
    {
        if (_helper == null || _cyberdeck == null || _slots.SelectedItems.Count != 1 ||
            _slots.SelectedItems[0].Tag is not ItemSlotPart slot)
        {
            return;
        }

        try
        {
            _helper.RemoveQuickhackSlot(_cyberdeck, slot);
            ChangeTracker.MarkChanged();
            RefreshVanillaSlots();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(this, ex.Message, "Quickhacks", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshLoadouts()
    {
        var selectedIndex = _loadouts.SelectedIndex;
        _loadouts.BeginUpdate();
        _loadouts.Items.Clear();

        if (_loadoutSystem?.QuickhackLoadouts != null)
        {
            var index = 1;
            foreach (var handle in _loadoutSystem.QuickhackLoadouts)
            {
                var loadout = handle?.GetValue() as QuickhackLoadout;
                if (loadout != null)
                {
                    var name = loadout.Name.ToString();
                    _loadouts.Items.Add(new LoadoutChoice(
                        loadout,
                        string.IsNullOrWhiteSpace(name) ? $"Loadout {index}" : name));
                    index++;
                }
            }
        }

        _loadoutStatus.Text = _loadoutSystem == null
            ? "No Quickhack Loadouts mod data was found in this save."
            : _loadouts.Items.Count == 0
                ? "The Quickhack Loadouts mod is present, but this save has no presets."
                : "Editing a preset changes only the saved list; use Equipped to edit the active cyberdeck.";

        if (_loadouts.Items.Count > 0)
        {
            _loadouts.SelectedIndex = Math.Clamp(selectedIndex, 0, _loadouts.Items.Count - 1);
        }

        _loadouts.EndUpdate();
        RefreshLoadoutQuickhacks();
    }

    private void RefreshLoadoutQuickhacks()
    {
        _loadoutQuickhacks.BeginUpdate();
        _loadoutQuickhacks.Items.Clear();
        _catalogueQuickhacks.Items.Clear();

        var loadout = (_loadouts.SelectedItem as LoadoutChoice)?.Loadout;
        if (loadout?.Quickhacks != null)
        {
            foreach (var itemId in loadout.Quickhacks)
            {
                _loadoutQuickhacks.Items.Add(new QuickhackChoice(itemId, QuickhackHelper.GetDisplayName(itemId)));
            }
        }

        if (_helper != null)
        {
            foreach (var itemId in QuickhackHelper.GetQuickhackCatalogue())
            {
                _catalogueQuickhacks.Items.Add(new QuickhackChoice(itemId, QuickhackHelper.GetDisplayName(itemId)));
            }
        }

        _loadoutQuickhacks.EndUpdate();
        UpdateLoadoutButtons();
    }

    private void UpdateLoadoutButtons()
    {
        var loadout = (_loadouts.SelectedItem as LoadoutChoice)?.Loadout;
        var selectedHackIndex = _loadoutQuickhacks.SelectedIndex;
        _catalogueQuickhacks.Enabled = loadout != null;
        _addToLoadout.Enabled = loadout != null && _catalogueQuickhacks.SelectedItem is QuickhackChoice;
        _removeFromLoadout.Enabled = loadout != null && selectedHackIndex >= 0;
        _moveLoadoutHackUp.Enabled = selectedHackIndex > 0;
        _moveLoadoutHackDown.Enabled = loadout?.Quickhacks != null &&
                                       selectedHackIndex >= 0 &&
                                       selectedHackIndex < loadout.Quickhacks.Count - 1;
    }

    private void AddQuickhackToLoadout()
    {
        if ((_loadouts.SelectedItem as LoadoutChoice)?.Loadout is not { } loadout ||
            _catalogueQuickhacks.SelectedItem is not QuickhackChoice choice)
        {
            return;
        }

        loadout.Quickhacks ??= new CArray<TweakDBID>();
        if (loadout.Quickhacks.Contains(choice.Id))
        {
            return;
        }

        loadout.Quickhacks.Add(choice.Id);
        ChangeTracker.MarkChanged();
        RefreshLoadoutQuickhacks();
        _loadoutQuickhacks.SelectedIndex = _loadoutQuickhacks.Items.Count - 1;
    }

    private void RemoveQuickhackFromLoadout()
    {
        if ((_loadouts.SelectedItem as LoadoutChoice)?.Loadout is not { Quickhacks: not null } loadout ||
            _loadoutQuickhacks.SelectedIndex < 0)
        {
            return;
        }

        loadout.Quickhacks.RemoveAt(_loadoutQuickhacks.SelectedIndex);
        ChangeTracker.MarkChanged();
        RefreshLoadoutQuickhacks();
    }

    private void MoveLoadoutQuickhack(int direction)
    {
        if ((_loadouts.SelectedItem as LoadoutChoice)?.Loadout is not { Quickhacks: not null } loadout)
        {
            return;
        }

        var index = _loadoutQuickhacks.SelectedIndex;
        var targetIndex = index + direction;
        if (index < 0 || targetIndex < 0 || targetIndex >= loadout.Quickhacks.Count)
        {
            return;
        }

        var quickhack = loadout.Quickhacks[index];
        loadout.Quickhacks.RemoveAt(index);
        loadout.Quickhacks.Insert(targetIndex, quickhack);
        ChangeTracker.MarkChanged();
        RefreshLoadoutQuickhacks();
        _loadoutQuickhacks.SelectedIndex = targetIndex;
    }
}
