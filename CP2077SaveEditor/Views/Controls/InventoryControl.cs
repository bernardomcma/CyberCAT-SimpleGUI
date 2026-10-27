using CP2077SaveEditor.Extensions;
using CP2077SaveEditor.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CP2077SaveEditor.ModSupport;
using WolvenKit.RED4.Save.Classes;
using WolvenKit.RED4.Types;

namespace CP2077SaveEditor.Views.Controls
{
    public partial class InventoryControl : UserControl, IGameControl
    {
        private enum Operation
        {
            None,
            Cut,
            Copy
        }

        private readonly Form2 _parentForm;

        private Random _random = new();

        private Operation _itemOperation;
        private ItemData _itemHolder;

        private string debloatInfo = "";

        private const ulong PlayerInventoryId = 0x1;
        private const ulong CarStashInventoryId = 0xF4240;
        private const ulong StashInventoryId = 0xE5F556FCBB62A706;

        private readonly Dictionary<ulong, string> _inventoryNames = new()
        {
            { 0x1, "V's Inventory" },
            { 0xF4240, "Car Stash" },
            { 0x895724, "Nomad Intro Items" },
            { 0x895956, "Street Kid Intro Items" },
            { 0x8959E8, "Corpo Intro Items" },
            { 0x38E8D0C9F9A087AE, "Panam's Stash" },
            { 0x6E48C594562422DE, "Judy's Stash" },
            { 0x7901DE03D136A5AF, "V's Wardrobe" },
            { 0xE5F556FCBB62A706, "V's Stash" },
            { 0xEDAD8C9B086A615E, "River's Stash" }
        };

        public InventoryControl(Form2 parentForm)
        {
            InitializeComponent();

            _parentForm = parentForm;
            _parentForm.PropertyChanged += OnParentFormPropertyChanged;
        }

        public string GameControlName => "Inventory";

        private void OnParentFormPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_parentForm.ActiveSaveFile))
            {
                if (_parentForm.ActiveSaveFile != null)
                {
                    this.InvokeIfRequired(RefreshInventories);
                }
            }
        }

        private void moneyUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (moneyUpDown.Enabled)
            {
                var playerInventory = _parentForm.ActiveSaveFile.GetInventory(1);
                playerInventory.Items[playerInventory.Items.IndexOf(playerInventory.Items.FirstOrDefault(x => x.ItemInfo.ItemId.Id.ResolvedText == "Items.money"))].Quantity = (uint)moneyUpDown.Value;
            }
        }

        private void clearQuestFlagsButton_Click(object sender, EventArgs e)
        {
            foreach (SubInventory inventory in _parentForm.ActiveSaveFile.GetInventoriesContainer().SubInventories)
            {
                foreach (ItemData item in inventory.Items)
                {
                    item.Flags = 0;
                }
            }
            MessageBox.Show("All item flags cleared.");
        }

        private void debloatButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This process will remove redundant data from your save. Just in case, it's recommended that you back up your save before continuing. Continue?", "Notice", MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                return;
            }

            debloatWorker.RunWorkerAsync();
            debloatTimer.Start();

            //editorPanel.Enabled = false;
            //optionsPanel.Enabled = false;
            //openSaveButton.Enabled = false;
            //swapSaveType.Enabled = false;
        }

        private void debloatWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            //var entryCounter = 1;
            //foreach (CyberCAT.Core.Classes.DumpedClasses.GameSavedStatsData stats in activeSaveFile.GetStatsMap().Values)
            //{
            //    if (stats.StatModifiers != null)
            //    {
            //        var craftedModifiers = stats.StatModifiers.Where(x => x.Value.StatType == gamedataStatType.IsItemCrafted);

            //        if (craftedModifiers != null && craftedModifiers.Count() > 100)
            //        {
            //            debloatInfo = "DE-BLOAT IN PROGRESS :: (1/2) :: Entry: " + entryCounter.ToString() + "/" + activeSaveFile.GetStatsMap().Values.Length.ToString();

            //            var ids = new HashSet<uint>();
            //            foreach (Handle<CyberCAT.Core.Classes.DumpedClasses.GameStatModifierData> modifierData in craftedModifiers)
            //            {
            //                ids.Add(modifierData.Id);
            //            }

            //            activeSaveFile.GetStatsContainer().RemoveHandles(ids);
            //            stats.StatModifiers = new Handle<CyberCAT.Core.Classes.DumpedClasses.GameStatModifierData>[] { };
            //        }
            //    }
            //    entryCounter++;
            //}

            //entryCounter = 0;
            //uint handleInd = 1;
            //foreach (CyberCAT.Core.Classes.DumpedClasses.GameSavedStatsData value in activeSaveFile.GetStatsMap().Values)
            //{
            //    if (value.StatModifiers != null && value.StatModifiers.Count() > 0)
            //    {
            //        var handleCounter = 0;
            //        foreach (Handle<CyberCAT.Core.Classes.DumpedClasses.GameStatModifierData> modifierData in value.StatModifiers)
            //        {
            //            modifierData.Id = handleInd;
            //            debloatInfo = "DE-BLOAT IN PROGRESS :: (2/2) :: Entry: " + entryCounter.ToString() + "/" + activeSaveFile.GetStatsMap().Values.Length.ToString() + " -- Handle: " + handleCounter.ToString() + "/" + value.StatModifiers.Count().ToString();
            //            handleInd++; handleCounter++;
            //        }
            //    }
            //    entryCounter++;
            //}
        }

        private void debloatWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            debloatTimer.Stop();
            //editorPanel.Enabled = true;
            //optionsPanel.Enabled = true;
            //openSaveButton.Enabled = true;
            //swapSaveType.Enabled = true;
            //
            //statusLabel.Text = "De-bloat complete.";
            MessageBox.Show("De-bloat complete.");
        }

        private void debloatTimer_Tick(object sender, EventArgs e)
        {
            //statusLabel.Text = debloatInfo;
        }

        private void containersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Selected indices belong to the previous container's rows; drop them before switching.
            inventoryListView.SelectedIndices.Clear();

            if (containersListBox.SelectedIndex > -1)
            {
                RefreshInventory();
            }
            else
            {
                containerGroupBox.Visible = false;
            }
        }

        private void RefreshInventories()
        {
            containersListBox.Items.Clear();
            foreach (var container in _parentForm.ActiveSaveFile.GetInventoriesContainer().SubInventories)
            {
                var containerId = container.InventoryId.ToString();
                if (_inventoryNames.Keys.Contains(container.InventoryId))
                {
                    containerId = _inventoryNames[container.InventoryId];
                }
                containersListBox.Items.Add(containerId);
            }

            if (containersListBox.Items.Count > 0)
            {
                containersListBox.SelectedIndex = 0;
            }

            RefreshInventory();
        }

        public bool RefreshInventory()
        {
            if (inventorySearchBox.Text != "" && inventorySearchBox.Text != "Search")
            {
                inventorySearchBox_TextChanged(null, new EventArgs());
            }
            else
            {
                RefreshInventory("", -1);
            }
            return true;
        }

        public bool RefreshInventory(string search, int searchField)
        {
            var listViewRows = new List<ListViewItem>();
            var containerID = containersListBox.SelectedItem.ToString();

            var hasEnhancedCraft = EnhancedCraftHelper.IsInstalled(_parentForm.ActiveSaveFile);

            containerGroupBox.Visible = true;
            containerGroupBox.Text = containerID;

            if (_inventoryNames.Values.Contains(containerID))
            {
                containerID = _inventoryNames.FirstOrDefault(x => x.Value == containerID).Key.ToString();
            }

            foreach (var item in _parentForm.ActiveSaveFile.GetInventory(ulong.Parse(containerID)).Items)
            {
                var name = item.ItemInfo.ItemId.Id.ResolvedText;
                if (string.IsNullOrEmpty(name) && hasEnhancedCraft)
                {
                    name = EnhancedCraftHelper.GetName(_parentForm.ActiveSaveFile, item);
                    if (name != null)
                    {

                    }
                }

                var row = new string[] { name, "", item.ItemInfo.ItemId.Id.ResolvedText, "", "1", item.ItemInfo.ItemId.Id.ResolvedText };

                if (item.IsQuantityOnly())
                {
                    row[4] = item.Quantity.ToString();
                    row[1] = "[S] ";
                }
                else if (item.IsExtendedOnly())
                {
                    row[1] = "[M] ";
                }
                else
                {
                    row[4] = item.Quantity.ToString();
                    row[1] = "[M+] ";
                }

                if (ResourceHelper.ItemClasses.TryGetValue(item.ItemInfo.ItemId.Id, out var itemData))
                {
                    row[1] += itemData.Type;
                }
                else
                {
                    row[1] += "Unknown";
                }

                if (search != "")
                {
                    if (searchField > -1)
                    {
                        if (!row[searchField].ToLower().Contains(search.ToLower()))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        var containsSearchString = false;
                        foreach (string rowItem in row)
                        {
                            if (!string.IsNullOrEmpty(rowItem) && rowItem.ToLower().Contains(search.ToLower()))
                            {
                                containsSearchString = true;
                                break;
                            }
                        }
                        if (!containsSearchString)
                        {
                            continue;
                        }
                    }
                }

                var newItem = new ListViewItem(row);
                newItem.Tag = item;

                listViewRows.Add(newItem);
                if (item.ItemInfo.ItemId.Id.ResolvedText == "Items.money" && containerID == "1")
                {
                    var money = item.Quantity;

                    if (money > moneyUpDown.Maximum)
                    {
                        moneyUpDown.Maximum = money;
                    }

                    moneyUpDown.Value = money;
                    moneyUpDown.Enabled = true;
                }
            }

            if (containerID == "1")
            {
                foreach (KeyValuePair<gameItemID, string> equipInfo in _parentForm.ActiveSaveFile.GetEquippedItems().Reverse())
                {
                    var equippedItem = listViewRows.FirstOrDefault(x => ((ItemData)x.Tag).ItemInfo.ItemId.Id == equipInfo.Key.Id);
                    if (equippedItem != null)
                    {
                        equippedItem.SubItems[3].Text = equipInfo.Value;
                        equippedItem.BackColor = Color.FromArgb(248, 248, 248);
                        listViewRows.Remove(equippedItem);
                        listViewRows.Insert(0, equippedItem);
                    }
                }
            }

            inventoryListView.SetVirtualItems(listViewRows);
            return true;
        }

        private void inventorySearchBox_TextChanged(object sender, EventArgs e)
        {
            if (inventorySearchBox.ForeColor != Color.Silver)
            {
                var query = inventorySearchBox.Text;
                if (query.StartsWith("name:"))
                {
                    query = query.Remove(0, 5);
                    RefreshInventory(query, 0);
                }
                else if (query.StartsWith("type:"))
                {
                    query = query.Remove(0, 5);
                    RefreshInventory(query, 1);
                }
                else if (query.StartsWith("id:"))
                {
                    query = query.Remove(0, 3);
                    RefreshInventory(query, 2);
                }
                else if (query.StartsWith("quantity:"))
                {
                    query = query.Remove(0, 9);
                    RefreshInventory(query, 3);
                }
                else if (query.StartsWith("desc:"))
                {
                    query = query.Remove(0, 5);
                    RefreshInventory(query, 4);
                }
                else
                {
                    RefreshInventory(query, -1);
                }
            }
        }

        private void inventoryListView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var containerId = containersListBox.SelectedItem.ToString();
                if (_inventoryNames.Values.Contains(containerId))
                {
                    containerId = _inventoryNames.FirstOrDefault(x => x.Value == containerId).Key.ToString();
                }

                var contextMenu = new ContextMenuStrip();

                var hitTest = inventoryListView.HitTest(e.Location);
                //if (hitTest.Item != null)
                //{
                //    contextMenu.Items.Add("Cut", null, CutItem).Tag = hitTest.Item;
                //    contextMenu.Items.Add("Copy", null, CopyItem).Tag = hitTest.Item;
                //}
                //
                //if (_itemHolder != null)
                //{
                //    contextMenu.Items.Add("Paste").Click += PasteItem;
                //}

                // Right-clicking a row outside the current selection makes it the only selected row,
                // so menu actions always apply to what the user sees highlighted.
                if (hitTest.Item != null && !inventoryListView.SelectedIndices.Contains(hitTest.Item.Index))
                {
                    inventoryListView.SelectedIndices.Clear();
                    inventoryListView.SelectedIndices.Add(hitTest.Item.Index);
                }

                var selectedCount = inventoryListView.SelectedIndices.Count;

                if (containerId == PlayerInventoryId.ToString())
                {
                    if (selectedCount > 0)
                    {
                        var moveText = selectedCount == 1 ? "Move to Stash" : $"Move {selectedCount} Items to Stash";
                        contextMenu.Items.Add(moveText, null, MoveSelectedToStash);
                    }

                    if (inventoryListView.VirtualListSize > 0)
                    {
                        contextMenu.Items.Add(IsSearchActive() ? "Move All Listed Items to Stash" : "Move All to Stash", null, MoveAllToStash);
                    }

                    if (contextMenu.Items.Count > 0)
                    {
                        contextMenu.Items.Add(new ToolStripSeparator());
                    }
                }

                if (inventoryListView.VirtualListSize > 0)
                {
                    contextMenu.Items.Add("Select All (Ctrl+A)", null, (_, _) => SelectAllInventoryItems());
                }

                if (containerId == "1" && hitTest.Item != null && selectedCount == 1)
                {
                    var activeItem = (ItemData)hitTest.Item.Tag;
                    var equipSlot = _parentForm.ActiveSaveFile.GetEquippedItems().FirstOrDefault(x => x.Key.Id == activeItem.ItemInfo.ItemId.Id).Key;

                    if (equipSlot != null)
                    {
                        var unequipItem = contextMenu.Items.Add("Unequip");
                        unequipItem.Tag = equipSlot;
                        unequipItem.Click += UnequipInventoryItem;
                    }
                    else
                    {
                        var equipIn = new ToolStripMenuItem("Equip in Slot");
                        foreach (var area in _parentForm.ActiveSaveFile.GetEquipAreas())
                        {
                            int counter = 1;
                            foreach (var slot in area.EquipSlots)
                            {
                                var slotItem = equipIn.DropDownItems.Add(area.AreaType.ToString() + " " + counter.ToString());
                                slotItem.Tag = slot;
                                slotItem.Click += EquipInventoryItem;
                                counter++;
                            }
                        }
                        contextMenu.Items.Add(equipIn);
                    }
                }

                if (hitTest.Item != null)
                {
                    contextMenu.Items.Add(selectedCount > 1 ? $"Delete {selectedCount} Items" : "Delete", null, DeleteInventoryItem);
                }

                contextMenu.Show(Cursor.Position);
            }
        }

        private void CutItem(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: ListViewItem { Tag: ItemData selectedItemData } selectedItem })
            {
                return;
            }

            var containerId = containersListBox.SelectedItem.ToString();
            if (_inventoryNames.Values.Contains(containerId))
            {
                containerId = _inventoryNames.FirstOrDefault(x => x.Value == containerId).Key.ToString();
            }

            _itemOperation = Operation.Cut;
            _itemHolder = selectedItemData;

            _parentForm.ActiveSaveFile.GetInventory(ulong.Parse(containerId)).Items.Remove(selectedItemData);
            RefreshInventory();
        }

        private void CopyItem(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: ListViewItem { Tag: ItemData selectedItemData } selectedItem })
            {
                return;
            }

            _itemOperation = Operation.Cut;
            _itemHolder = selectedItemData;
        }

        private void PasteItem(object sender, EventArgs e)
        {
            if (_itemHolder == null)
            {
                return;
            }

            var containerId = containersListBox.SelectedItem.ToString();
            if (_inventoryNames.Values.Contains(containerId))
            {
                containerId = _inventoryNames.FirstOrDefault(x => x.Value == containerId).Key.ToString();
            }

            if (_itemOperation == Operation.Cut)
            {
                _parentForm.ActiveSaveFile.GetInventory(ulong.Parse(containerId)).Items.Add(_itemHolder);
                RefreshInventory();

                _itemOperation = Operation.Copy;
                return;
            }

            if (_itemOperation == Operation.Copy)
            {

            }
        }

        private void EquipInventoryItem(object sender, EventArgs e)
        {
            var slot = (gameSEquipSlot)((ToolStripItem)sender).Tag;
            var currentItem = (ItemData)inventoryListView.SelectedVirtualItems()[0].Tag;
            slot.ItemID = new gameItemID() { Id = currentItem.ItemInfo.ItemId.Id };
            RefreshInventory();
            inventoryListView.SelectedVirtualItems()[0].Selected = false;
        }

        private void UnequipInventoryItem(object sender, EventArgs e)
        {
            var equipId = (gameItemID)((ToolStripItem)sender).Tag;
            foreach (var equipSlot in _parentForm.ActiveSaveFile.GetEquipSlotsFromID(equipId))
            {
                equipSlot.ItemID = new gameItemID();
            }

            RefreshInventory();
            if (inventoryListView.SelectedVirtualItems().Count > 0)
            {
                inventoryListView.SelectedVirtualItems()[0].Selected = false;
            }
        }

        private void DeleteInventoryItem(object sender = null, EventArgs e = null)
        {
            var selectedRows = inventoryListView.SelectedVirtualItems();
            if (selectedRows.Count == 0)
            {
                return;
            }

            var containerId = GetSelectedContainerId();
            var inventory = _parentForm.ActiveSaveFile.GetInventory(ulong.Parse(containerId));
            var virtualInfo = inventoryListView.GetVirtualInfo();

            foreach (var row in selectedRows)
            {
                if (row.Tag is not ItemData selectedItemData)
                {
                    continue;
                }

                inventory.Items.Remove(selectedItemData);

                if (selectedItemData.ItemInfo.ItemId.Id.ResolvedText == "Items.money" && containerId == "1")
                {
                    moneyUpDown.Enabled = false;
                    moneyUpDown.Value = 0;
                }

                virtualInfo.Items.Remove(row);
            }

            inventoryListView.SelectedIndices.Clear();
            inventoryListView.VirtualListSize = virtualInfo.Items.Count;
            inventoryListView.Invalidate();
            //statusLabel.Text = "'" + activeItem.Header.ItemId.Id.ResolvedText + "' deleted.";
        }

        private string GetSelectedContainerId()
        {
            if (containersListBox.SelectedItem == null)
            {
                return string.Empty;
            }

            var containerId = containersListBox.SelectedItem.ToString();
            if (_inventoryNames.Values.Contains(containerId))
            {
                containerId = _inventoryNames.FirstOrDefault(x => x.Value == containerId).Key.ToString();
            }

            return containerId;
        }

        private bool IsSearchActive()
        {
            return !string.IsNullOrEmpty(inventorySearchBox.Text) &&
                   !(inventorySearchBox.Text == "Search" && inventorySearchBox.ForeColor == Color.Silver);
        }

        private void SelectAllInventoryItems()
        {
            inventoryListView.BeginUpdate();
            try
            {
                inventoryListView.SelectedIndices.Clear();
                for (var i = 0; i < inventoryListView.VirtualListSize; i++)
                {
                    inventoryListView.SelectedIndices.Add(i);
                }
            }
            finally
            {
                inventoryListView.EndUpdate();
            }
        }

        private void btn_MoveToStash_Click(object sender, EventArgs e)
        {
            if (_parentForm.ActiveSaveFile == null || containersListBox.SelectedItem == null)
            {
                return;
            }

            var containerId = GetSelectedContainerId();
            if (containerId != PlayerInventoryId.ToString())
            {
                MessageBox.Show("Please select \"V's Inventory\" to move items to V's Stash.", "Move to Stash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRows = inventoryListView.SelectedVirtualItems();
            if (selectedRows.Count > 0)
            {
                MoveItemsToStash(selectedRows.Select(x => x.Tag).OfType<ItemData>().ToList());
                return;
            }

            var allListedItems = inventoryListView.GetVirtualInfo()?.Items
                .Select(x => x.Tag)
                .OfType<ItemData>()
                .Where(x => x.ItemInfo.ItemId.Id.ResolvedText != "Items.money")
                .ToList() ?? new List<ItemData>();

            if (allListedItems.Count == 0)
            {
                MessageBox.Show("No items available to move to V's Stash.", "Move to Stash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var prompt = IsSearchActive()
                ? $"No items are selected. Do you want to move all {allListedItems.Count} listed items to V's Stash?"
                : $"No items are selected. Do you want to move all {allListedItems.Count} items from V's Inventory to V's Stash?";

            if (MessageBox.Show(prompt, "Move to Stash", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                MoveItemsToStash(allListedItems);
            }
        }

        private void MoveSelectedToStash(object sender, EventArgs e)
        {
            var selectedItems = inventoryListView.SelectedVirtualItems()
                .Select(x => x.Tag)
                .OfType<ItemData>()
                .ToList();

            MoveItemsToStash(selectedItems);
        }

        private void MoveAllToStash(object sender, EventArgs e)
        {
            var allListedItems = inventoryListView.GetVirtualInfo()?.Items
                .Select(x => x.Tag)
                .OfType<ItemData>()
                .Where(x => x.ItemInfo.ItemId.Id.ResolvedText != "Items.money")
                .ToList() ?? new List<ItemData>();

            if (allListedItems.Count == 0)
            {
                MessageBox.Show("No items available to move to V's Stash.", "Move to Stash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var prompt = IsSearchActive()
                ? $"Move all {allListedItems.Count} listed items to V's Stash?"
                : $"Move all {allListedItems.Count} items from V's Inventory to V's Stash?";

            if (MessageBox.Show(prompt, "Move to Stash", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                MoveItemsToStash(allListedItems);
            }
        }

        private void MoveItemsToStash(List<ItemData> itemsToMove)
        {
            if (_parentForm.ActiveSaveFile == null || itemsToMove == null || itemsToMove.Count == 0)
            {
                return;
            }

            var playerInventory = _parentForm.ActiveSaveFile.GetInventory(PlayerInventoryId);
            var stashInventory = GetOrCreateStashInventory(out var stashDisplayName);

            if (playerInventory == null)
            {
                MessageBox.Show("Could not find V's Inventory in the active save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (stashInventory == null)
            {
                MessageBox.Show("Could not find or create Stash in the active save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var movedCount = 0;
            foreach (var item in itemsToMove)
            {
                if (item.ItemInfo.ItemId.Id.ResolvedText == "Items.money")
                {
                    continue;
                }

                if (!playerInventory.Items.Remove(item))
                {
                    continue;
                }

                foreach (var equipSlot in _parentForm.ActiveSaveFile.GetEquipSlotsFromID(item.ItemInfo.ItemId))
                {
                    equipSlot.ItemID = new gameItemID();
                }

                if (item.IsQuantityOnly())
                {
                    var existingStashItem = stashInventory.Items.FirstOrDefault(
                        x => x.IsQuantityOnly() &&
                             x.ItemInfo.ItemId.Id == item.ItemInfo.ItemId.Id &&
                             x.ItemInfo.ItemId.RngSeed == item.ItemInfo.ItemId.RngSeed);

                    if (existingStashItem != null)
                    {
                        existingStashItem.Quantity += item.Quantity;
                    }
                    else
                    {
                        stashInventory.Items.Add(item);
                    }
                }
                else
                {
                    stashInventory.Items.Add(item);
                }

                movedCount++;
            }

            inventoryListView.SelectedIndices.Clear();
            RefreshInventory();

            if (movedCount == 0)
            {
                MessageBox.Show("No movable items were selected (Money cannot be moved to Stash).", "Move to Stash", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Moved {movedCount} item(s) to {stashDisplayName}.", "Move to Stash", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private SubInventory GetOrCreateStashInventory(out string stashDisplayName)
        {
            var stashInventory = _parentForm.ActiveSaveFile.GetInventory(StashInventoryId);
            if (stashInventory != null)
            {
                stashDisplayName = _inventoryNames[StashInventoryId];
                return stashInventory;
            }

            stashInventory = _parentForm.ActiveSaveFile.GetInventory(CarStashInventoryId);
            if (stashInventory != null)
            {
                stashDisplayName = _inventoryNames[CarStashInventoryId];
                return stashInventory;
            }

            var inventoriesContainer = _parentForm.ActiveSaveFile.GetInventoriesContainer();
            if (inventoriesContainer?.SubInventories == null)
            {
                stashDisplayName = "Stash";
                return null;
            }

            stashInventory = new SubInventory
            {
                InventoryId = CarStashInventoryId
            };
            inventoriesContainer.SubInventories.Add(stashInventory);
            stashDisplayName = _inventoryNames[CarStashInventoryId];

            if (!containersListBox.Items.Contains(stashDisplayName))
            {
                containersListBox.Items.Add(stashDisplayName);
            }

            return stashInventory;
        }

        private void inventoryListView_DoubleClick(object sender, EventArgs e)
        {
            if (inventoryListView.SelectedIndices.Count > 0)
            {
                var activeDetails = new ItemDetails();
                activeDetails.LoadItem((ItemData)inventoryListView.SelectedVirtualItems()[0].Tag, _parentForm.ActiveSaveFile, RefreshInventory, _random);
            }
        }

        private void inventoryListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                SelectAllInventoryItems();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (inventoryListView.SelectedIndices.Count > 0 && e.KeyCode == Keys.Delete)
            {
                DeleteInventoryItem();
            }
        }

        private void SearchBoxGotFocus(object sender, EventArgs e)
        {
            if (((TextBox)sender).Text == "Search" && ((TextBox)sender).ForeColor == Color.Silver)
            {
                ((TextBox)sender).Text = "";
                ((TextBox)sender).ForeColor = Color.Black;
            }
        }

        private void SearchBoxLostFocus(object sender, EventArgs e)
        {
            if (((TextBox)sender).Text == "")
            {
                ((TextBox)sender).ForeColor = Color.Silver;
                ((TextBox)sender).Text = "Search";
            }
        }

        private void btn_AddItem_Click(object sender, EventArgs e)
        {
            var containerID = containersListBox.SelectedItem.ToString();
            if (_inventoryNames.Values.Contains(containerID))
            {
                containerID = _inventoryNames.FirstOrDefault(x => x.Value == containerID).Key.ToString();
            }

            var itemDialog = new AddItem();
            itemDialog.LoadDialog(_parentForm.ActiveSaveFile.GetInventory(ulong.Parse(containerID)), _parentForm.ActiveSaveFile);

            RefreshInventory();
        }
    }
}
