using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CP2077SaveEditor.ModSupport;
using CP2077SaveEditor.Utils;
using CP2077SaveEditor.Views.Controls;
using WolvenKit.Common.Services;
using WolvenKit.Core.Compression;
using WolvenKit.RED4.Save.IO;
using WolvenKit.RED4.TweakDB.Helper;
using WolvenKit.RED4.Types.Pools;

namespace CP2077SaveEditor.Views
{
    public partial class Form2 : Form, INotifyPropertyChanged
    {
        internal static TweakDBStringHelper TweakDbStringHelper;
        internal HashService HashService;

        internal bool IsLoaded;

        private SaveFileHelper _activeSaveFile;

        private bool _isSaving = false;
        private bool _isCheckingChanges = false;
        private bool _closeConfirmed = false;

        private const int MaxBackups = 5;

        private string _activeSavePath;
        private byte[] _savedStateHash;

        public Form2()
        {
            InitializeComponent();

            CompressionSettings.Get().UseOodle = false;
            ModManager.LoadTypes();

            RegisterControl(new PlayerStatsControl(this), true);
            RegisterControl(new AppearanceControl(this));
            RegisterControl(new InventoryControl(this));
            RegisterControl(new VehiclesControl(this));
            RegisterControl(new QuestFactsControl(this));
            RegisterControl(new ExtrasControl(this));

            if (File.Exists(Environment.CurrentDirectory + "\\config.json"))
            {
                try
                {
                    var config = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(Environment.CurrentDirectory + "\\config.json"));
                    if (config.ContainsKey("EnablePSData") && config["EnablePSData"] == 0)
                    {
                        // Global.PSDataEnabled = false;
                    }

                    if (config.ContainsKey("EnableWIPFeatures") && config["EnableWIPFeatures"] == 1)
                    {
                        Global.IsDebug = true;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to read config.json", "Notice");
                }
            }
        }

        private async void Form2_Load(object sender, EventArgs e)
        {
            SetStatus("Init...");

            await Init();

            openSaveButton.Enabled = true;

            SetStatus("Idle");
        }

        internal SaveFileHelper ActiveSaveFile
        {
            get => _activeSaveFile;
            private set => SetField(ref _activeSaveFile, value);
        }

        #region Controls

        private void RegisterControl(UserControl userControl, bool isLandingPage = false)
        {
            if (userControl is not IGameControl gc)
            {
                throw new Exception();
            }

            userControl.Dock = DockStyle.Fill;

            sm_Menu.AddButton(gc.GameControlName, (_, _) => { SwitchPanel(userControl); });
            if (isLandingPage)
            {
                SwitchPanel(userControl);
            }
        }

        private void SwitchPanel(UserControl userControl)
        {
            if (userControl == null)
            {
                return;
            }

            this.InvokeIfRequired(() =>
            {
                if (pnl_Content.Controls.Count != 0)
                {
                    pnl_Content.Controls.Clear();
                }

                pnl_Content.Controls.Add(userControl);
            });
        }

        #endregion Controls

        private async void openSaveButton_Click(object sender, EventArgs e)
        {
            if (!await ConfirmDiscardChanges())
            {
                return;
            }

            var fileWindow = new OpenFileDialog();
            fileWindow.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Saved Games\\CD Projekt Red\\Cyberpunk 2077\\";
            fileWindow.Filter = "Cyberpunk 2077 Save File|*.dat";
            if (fileWindow.ShowDialog() == DialogResult.OK)
            {
                LoadSave(fileWindow.FileName);
            }
        }

        private async Task Init()
        {
            if (TweakDbStringHelper == null)
            {
                var tweakDbStrStream = typeof(TweakDBService).Assembly.GetManifestResourceStream("WolvenKit.Common.Resources.tweakdbstr.kark");

                TweakDbStringHelper = new TweakDBStringHelper();
                TweakDbStringHelper.LoadFromStream(tweakDbStrStream);
            }

            if (HashService == null)
            {
                await Task.Run(() => HashService = new HashService());
            }
        }

        private async void LoadSave(string savePath)
        {
            IsLoaded = false;
            pnl_Content.Enabled = false;
            openSaveButton.Enabled = false;
            saveChangesButton.Enabled = false;
            quickSaveButton.Enabled = false;

            _activeSavePath = null;
            _savedStateHash = null;

            SetStatus("Loading save...");

            var status = EFileReadErrorCodes.NoCSav;

            try
            {
                await Task.Run(() =>
                {
                    using var fs = File.Open(savePath, FileMode.Open);
                    using var reader = new CyberpunkSaveReader(fs);

                    status = reader.ReadFile(out var save);
                    switch (status)
                    {
                        case EFileReadErrorCodes.NoError:
                            ActiveSaveFile = new SaveFileHelper { SaveFile = save };

                            var metadataPath = Path.Combine(Path.GetDirectoryName(savePath), "metadata.9.json");
                            if (File.Exists(metadataPath))
                            {
                                ActiveSaveFile.Metadata = File.ReadAllBytes(metadataPath);
                            }

                            var screenshotPath = Path.Combine(Path.GetDirectoryName(savePath), "screenshot.png");
                            if (File.Exists(screenshotPath))
                            {
                                ActiveSaveFile.ImageData = File.ReadAllBytes(screenshotPath);
                            }

                            // Taken after the controls have initialized, since some of them normalize save data on load
                            _savedStateHash = SHA256.HashData(SerializeSave());
                            _activeSavePath = savePath;

                            this.InvokeIfRequired(() =>
                            {
                                saveChangesButton.Enabled = true;
                                quickSaveButton.Enabled = true;
                            });
                            break;
                        case EFileReadErrorCodes.NoCSav:
                            MessageBox.Show("Failed to parse save file: File contains invalid data");
                            return;
                        case EFileReadErrorCodes.UnsupportedVersion:
                            fs.Position = 0;
                            reader.ReadFileInfo(out var info);

                            MessageBox.Show(
                                $"Failed to parse save file: Game version {info.Value.GameVersion} is not supported!");
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });

                filePathLabel.Text = Path.GetFileName(Path.GetDirectoryName(savePath));

                GC.Collect();
            }
            catch (Exception e)
            {
                try
                {
                    var message = e.Message;
                    if (message == "CEnum \"gamedataStatType.ItemTierQuality\" could not be found!")
                    {
                        message += Environment.NewLine + "Loading of modded saves with this data is currently not supported! See \"Known Issues\" on Nexus mods";
                    }

                    await File.WriteAllTextAsync("error.txt", message + Environment.NewLine + e.StackTrace);
                    MessageBox.Show("Failed to parse save file:" + Environment.NewLine + 
                                    message + Environment.NewLine +
                                    " An error.txt file has been generated with additional information.");
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed to parse save file: " + Environment.NewLine + 
                                    e.Message + Environment.NewLine + Environment.NewLine +
                                    "Stack Trace:" + Environment.NewLine + 
                                    e.StackTrace);
                }
            }

            SetStatus("Idle");

            openSaveButton.Enabled = true;

            if (status == EFileReadErrorCodes.NoError)
            {
                pnl_Content.Enabled = true;
                IsLoaded = true;
            }
        }

        internal void SetStatus(string message)
        {
            this.InvokeIfRequired(() => { tssl_Status.Text = message; });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private static string SavesRootDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Saved Games\\CD Projekt Red\\Cyberpunk 2077";

        private async void saveChangesButton_Click(object sender, EventArgs e)
        {
            var initialDirectory = SavesRootDirectory;

            var saveWindow = new FolderBrowserDialog();
            saveWindow.InitialDirectory = initialDirectory;
            if (saveWindow.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var fileDirectory = saveWindow.SelectedPath;

            if (fileDirectory == initialDirectory)
            {
                MessageBox.Show("Saving into the root folder isn't allowed");
                return;
            }

            await SaveToDirectory(fileDirectory);
        }

        private async void quickSaveButton_Click(object sender, EventArgs e)
        {
            await QuickSave();
        }

        private async void Form2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                await QuickSave();
            }
        }

        private async Task QuickSave()
        {
            if (!quickSaveButton.Enabled || _activeSavePath == null)
            {
                return;
            }

            await SaveToDirectory(Path.GetDirectoryName(_activeSavePath));
        }

        private async Task<bool> SaveToDirectory(string fileDirectory)
        {
            SetUiEnabled(false);

            var fileName = Path.Combine(fileDirectory, "sav.dat");
            var tempFileName = fileName + ".tmp";
            var success = false;

            SetStatus("Saving...");
            _isSaving = true;

            try
            {
                var data = await Task.Run(SerializeSave);

                // Write to a temporary file first, so a failed save never touches the original
                await File.WriteAllBytesAsync(tempFileName, data);

                SetStatus("Verifying...");
                await Task.Run(() => VerifySaveFile(tempFileName));

                if (File.Exists(fileName))
                {
                    if (!File.Exists(Path.Combine(fileDirectory, "sav.old")))
                    {
                        File.Copy(fileName, Path.Combine(fileDirectory, "sav.old"));
                    }

                    CreateBackup(fileName);
                }

                File.Move(tempFileName, fileName, true);

                var metadataPath = Path.Combine(fileDirectory, "metadata.9.json");
                if (!File.Exists(metadataPath))
                {
                    if (ActiveSaveFile.Metadata != null)
                    {
                        await File.WriteAllBytesAsync(metadataPath, ActiveSaveFile.Metadata);
                    }
                    else
                    {
                        MessageBox.Show("Error while saving file: metadata.9.json could not be found.");
                    }
                }

                var screenshotPath = Path.Combine(fileDirectory, "screenshot.png");
                if (!File.Exists(screenshotPath))
                {
                    if (ActiveSaveFile.ImageData != null)
                    {
                        await File.WriteAllBytesAsync(screenshotPath, ActiveSaveFile.ImageData);
                    }
                    else
                    {
                        MessageBox.Show("Error while saving file: screenshot.png could not be found.");
                    }
                }

                _savedStateHash = SHA256.HashData(data);
                _activeSavePath = fileName;
                filePathLabel.Text = Path.GetFileName(fileDirectory);

                success = true;
            }
            catch (Exception err)
            {
                try
                {
                    if (File.Exists(tempFileName))
                    {
                        File.Delete(tempFileName);
                    }

                    await File.WriteAllTextAsync("error.txt", err.Message + '\n' + err.TargetSite + '\n' + err.StackTrace);
                    MessageBox.Show("Failed to save changes: " + err.Message + " An error.txt file has been generated with additional information.");
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed to save changes: " + err.Message + err.StackTrace);
                }
            }

            SetUiEnabled(true);

            _isSaving = false;
            SetStatus(success ? "File saved." : "Save failed.");

            GC.Collect();

            return success;
        }

        private byte[] SerializeSave()
        {
            using var ms = new MemoryStream();
            using (var writer = new CyberpunkSaveWriter(ms, Encoding.UTF8, true))
            {
                writer.WriteFile(ActiveSaveFile.SaveFile, true);
            }

            return ms.ToArray();
        }

        private static void VerifySaveFile(string path)
        {
            using var fs = File.OpenRead(path);
            using var reader = new CyberpunkSaveReader(fs);

            var status = reader.ReadFile(out _);
            if (status != EFileReadErrorCodes.NoError)
            {
                throw new InvalidDataException($"The written save could not be read back ({status}). The original save was not modified.");
            }
        }

        private static void CreateBackup(string fileName)
        {
            var directory = Path.GetDirectoryName(fileName)!;

            File.Copy(fileName, Path.Combine(directory, $"sav.{DateTime.Now:yyyyMMdd-HHmmss}.bak"), true);

            // The timestamp format sorts chronologically by name
            foreach (var oldBackup in Directory.GetFiles(directory, "sav.*.bak").OrderByDescending(x => x).Skip(MaxBackups))
            {
                File.Delete(oldBackup);
            }
        }

        private void SetUiEnabled(bool enabled)
        {
            pnl_Content.Enabled = enabled;
            openSaveButton.Enabled = enabled;
            saveChangesButton.Enabled = enabled;
            quickSaveButton.Enabled = enabled && _activeSavePath != null;
        }

        /// <summary>
        /// Asks the user what to do with unsaved changes. Returns false if the pending action should be cancelled.
        /// </summary>
        private async Task<bool> ConfirmDiscardChanges()
        {
            if (!IsLoaded || _savedStateHash == null)
            {
                return true;
            }

            SetUiEnabled(false);
            SetStatus("Checking for unsaved changes...");
            _isCheckingChanges = true;

            byte[] currentHash;
            try
            {
                currentHash = await Task.Run(() => SHA256.HashData(SerializeSave()));
            }
            catch (Exception)
            {
                // If the save can't be serialized we can't tell, so assume it changed
                currentHash = null;
            }

            _isCheckingChanges = false;
            SetUiEnabled(true);
            SetStatus("Idle");

            if (currentHash != null && currentHash.AsSpan().SequenceEqual(_savedStateHash))
            {
                return true;
            }

            var result = MessageBox.Show("There are unsaved changes. Do you want to save them first?", "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                return await SaveToDirectory(Path.GetDirectoryName(_activeSavePath));
            }

            return result == DialogResult.No;
        }

        private async void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isSaving)
            {
                if (MessageBox.Show("Saving in progress. Closing now will lead to corrupted saves.\r\nAre you sure you want to exit?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
                return;
            }

            if (_closeConfirmed || e.CloseReason == CloseReason.WindowsShutDown)
            {
                return;
            }

            e.Cancel = true;

            if (_isCheckingChanges)
            {
                return;
            }

            if (await ConfirmDiscardChanges())
            {
                _closeConfirmed = true;
                Close();
            }
        }
    }
}
