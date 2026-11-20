using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Speech.Synthesis;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace _518byReplica
{
    public sealed class MainForm : Form
    {
        private readonly ListView queueView;
        private readonly TextBox nameBox;
        private readonly TextBox textBox;
        private readonly ComboBox voiceBox;
        private readonly NumericUpDown repeatCountBox;
        private readonly TrackBar rateBar;
        private readonly TrackBar volumeBar;
        private readonly Label voiceStatus;
        private readonly TextBox musicFolderBox;
        private readonly ListBox musicList;
        private readonly ToolStripStatusLabel statusLabel;
        private readonly SpeechSynthesizer synthesizer;
        private readonly AppSettings settings;
        private readonly string settingsPath;
        private readonly List<BroadcastItem> playQueue;
        private int playQueueIndex;
        private int repeatsLeft;
        private bool queuePlaying;
        private bool updatingEditor;
        private string currentMusicAlias;

        public MainForm()
        {
            Text = "518 超市播音 · 复刻原型";
            MinimumSize = new Size(940, 620);
            Size = new Size(1120, 740);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "518byReplica", "settings.xml");
            settings = LoadSettings(settingsPath);
            playQueue = new List<BroadcastItem>();
            currentMusicAlias = "replicaMusic";
            synthesizer = new SpeechSynthesizer();
            synthesizer.SpeakCompleted += OnSpeakCompleted;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            Controls.Add(root);

            var toolbar = new FlowLayoutPanel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(8, 7, 6, 4);
            toolbar.WrapContents = false;
            toolbar.BackColor = Color.FromArgb(242, 245, 249);
            root.Controls.Add(toolbar, 0, 0);

            var addButton = MakeButton("新增内容");
            var removeButton = MakeButton("删除");
            var upButton = MakeButton("上移");
            var downButton = MakeButton("下移");
            var toggleButton = MakeButton("启用/停用");
            var playButton = MakeButton("播报选中");
            var queueButton = MakeButton("播报全部");
            var stopButton = MakeButton("停止播报");
            var saveButton = MakeButton("保存设置");
            toolbar.Controls.Add(addButton);
            toolbar.Controls.Add(removeButton);
            toolbar.Controls.Add(upButton);
            toolbar.Controls.Add(downButton);
            toolbar.Controls.Add(toggleButton);
            toolbar.Controls.Add(playButton);
            toolbar.Controls.Add(queueButton);
            toolbar.Controls.Add(stopButton);
            toolbar.Controls.Add(saveButton);
            addButton.Click += delegate { AddItem(); };
            removeButton.Click += delegate { RemoveSelectedItem(); };
            upButton.Click += delegate { MoveSelectedItem(-1); };
            downButton.Click += delegate { MoveSelectedItem(1); };
            toggleButton.Click += delegate { ToggleSelectedItem(); };
            playButton.Click += delegate { SpeakSelected(); };
            queueButton.Click += delegate { SpeakQueue(); };
            stopButton.Click += delegate { StopSpeech(); };
            saveButton.Click += delegate { SaveSettings(); };

            var content = new SplitContainer();
            content.Dock = DockStyle.Fill;
            content.Size = new Size(1080, 660);
            content.SplitterDistance = 390;
            content.Panel1MinSize = 280;
            content.Panel2MinSize = 420;
            root.Controls.Add(content, 0, 1);

            queueView = new ListView();
            queueView.Dock = DockStyle.Fill;
            queueView.View = View.Details;
            queueView.FullRowSelect = true;
            queueView.HideSelection = false;
            queueView.MultiSelect = false;
            queueView.Columns.Add("启用", 54);
            queueView.Columns.Add("内容名称", 120);
            queueView.Columns.Add("播报文本", 260);
            queueView.SelectedIndexChanged += delegate { LoadSelectedItem(); };
            queueView.DoubleClick += delegate { SpeakSelected(); };
            content.Panel1.Controls.Add(queueView);

            var right = new TableLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.Padding = new Padding(10, 4, 10, 8);
            right.ColumnCount = 1;
            right.RowCount = 2;
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            content.Panel2.Controls.Add(right);

            var editorGroup = new GroupBox();
            editorGroup.Text = "播音内容与本机语音";
            editorGroup.Dock = DockStyle.Fill;
            right.Controls.Add(editorGroup, 0, 0);

            var editor = new TableLayoutPanel();
            editor.Dock = DockStyle.Fill;
            editor.Padding = new Padding(10);
            editor.ColumnCount = 2;
            editor.RowCount = 8;
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            editorGroup.Controls.Add(editor);

            editor.Controls.Add(MakeLabel("内容名称"), 0, 0);
            nameBox = new TextBox();
            nameBox.Dock = DockStyle.Fill;
            editor.Controls.Add(nameBox, 1, 0);
            nameBox.TextChanged += delegate { UpdateSelectedItem(); };

            editor.Controls.Add(MakeLabel("播报文本"), 0, 1);
            textBox = new TextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.Multiline = true;
            textBox.ScrollBars = ScrollBars.Vertical;
            textBox.AcceptsReturn = true;
            editor.Controls.Add(textBox, 1, 1);
            textBox.TextChanged += delegate { UpdateSelectedItem(); };

            editor.Controls.Add(MakeLabel("语音"), 0, 2);
            var voiceRow = new FlowLayoutPanel();
            voiceRow.Dock = DockStyle.Fill;
            voiceRow.WrapContents = false;
            voiceBox = new ComboBox();
            voiceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            voiceBox.Width = 280;
            voiceRow.Controls.Add(voiceBox);
            var refreshVoicesButton = MakeButton("刷新语音");
            refreshVoicesButton.Width = 86;
            refreshVoicesButton.Click += delegate { RefreshVoices(); };
            voiceRow.Controls.Add(refreshVoicesButton);
            editor.Controls.Add(voiceRow, 1, 2);
            voiceBox.SelectedIndexChanged += delegate { ApplyVoiceSelection(); };

            voiceStatus = new Label();
            voiceStatus.Dock = DockStyle.Fill;
            voiceStatus.ForeColor = Color.DimGray;
            editor.Controls.Add(voiceStatus, 1, 3);

            editor.Controls.Add(MakeLabel("重复次数"), 0, 4);
            repeatCountBox = new NumericUpDown();
            repeatCountBox.Minimum = 1;
            repeatCountBox.Maximum = 99;
            repeatCountBox.Value = 1;
            repeatCountBox.Width = 100;
            repeatCountBox.Anchor = AnchorStyles.Left;
            editor.Controls.Add(repeatCountBox, 1, 4);
            repeatCountBox.ValueChanged += delegate { UpdateSelectedItem(); };

            editor.Controls.Add(MakeLabel("语速"), 0, 5);
            rateBar = new TrackBar();
            rateBar.Minimum = -10;
            rateBar.Maximum = 10;
            rateBar.TickFrequency = 5;
            rateBar.Dock = DockStyle.Fill;
            editor.Controls.Add(rateBar, 1, 5);
            rateBar.ValueChanged += delegate { settings.SpeechRate = rateBar.Value; };

            editor.Controls.Add(MakeLabel("音量"), 0, 6);
            volumeBar = new TrackBar();
            volumeBar.Minimum = 0;
            volumeBar.Maximum = 100;
            volumeBar.TickFrequency = 25;
            volumeBar.Dock = DockStyle.Fill;
            editor.Controls.Add(volumeBar, 1, 6);
            volumeBar.ValueChanged += delegate { settings.SpeechVolume = volumeBar.Value; };

            var voiceHint = new Label();
            voiceHint.Text = "语音列表来自 Windows SAPI；不同位数程序看到的语音可能不同。";
            voiceHint.Dock = DockStyle.Fill;
            voiceHint.ForeColor = Color.DimGray;
            editor.Controls.Add(voiceHint, 1, 7);

            var musicGroup = new GroupBox();
            musicGroup.Text = "背景音乐";
            musicGroup.Dock = DockStyle.Fill;
            right.Controls.Add(musicGroup, 0, 1);

            var musicLayout = new TableLayoutPanel();
            musicLayout.Dock = DockStyle.Fill;
            musicLayout.Padding = new Padding(9);
            musicLayout.ColumnCount = 1;
            musicLayout.RowCount = 3;
            musicLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            musicLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            musicLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            musicGroup.Controls.Add(musicLayout);

            var folderRow = new TableLayoutPanel();
            folderRow.Dock = DockStyle.Fill;
            folderRow.ColumnCount = 2;
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            musicLayout.Controls.Add(folderRow, 0, 0);
            musicFolderBox = new TextBox();
            musicFolderBox.Dock = DockStyle.Fill;
            musicFolderBox.ReadOnly = true;
            folderRow.Controls.Add(musicFolderBox, 0, 0);
            var browseMusicButton = MakeButton("选择目录");
            browseMusicButton.Dock = DockStyle.Fill;
            folderRow.Controls.Add(browseMusicButton, 1, 0);
            browseMusicButton.Click += delegate { ChooseMusicFolder(); };

            musicList = new ListBox();
            musicList.Dock = DockStyle.Fill;
            musicList.DoubleClick += delegate { PlaySelectedMusic(); };
            musicLayout.Controls.Add(musicList, 0, 1);

            var musicButtons = new FlowLayoutPanel();
            musicButtons.Dock = DockStyle.Fill;
            musicButtons.FlowDirection = FlowDirection.LeftToRight;
            musicButtons.Controls.Add(MakeButton("播放背景音乐", delegate { PlaySelectedMusic(); }));
            musicButtons.Controls.Add(MakeButton("停止背景音乐", delegate { StopMusic(); }));
            musicLayout.Controls.Add(musicButtons, 0, 2);

            var statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("就绪");
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusStrip.Items.Add(statusLabel);
            root.Controls.Add(statusStrip, 0, 2);

            LoadStoredState();
            RefreshVoices();
            RefreshQueueView(null);
            if (!String.IsNullOrEmpty(settings.MusicFolder) && Directory.Exists(settings.MusicFolder))
            {
                musicFolderBox.Text = settings.MusicFolder;
                LoadMusicFiles();
            }
            FormClosing += OnFormClosing;
        }

        private static Button MakeButton(string text)
        {
            return MakeButton(text, null);
        }

        private static Button MakeButton(string text, EventHandler handler)
        {
            var button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.Height = 30;
            button.Margin = new Padding(3, 0, 3, 0);
            if (handler != null) button.Click += handler;
            return button;
        }

        private static Label MakeLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private void LoadStoredState()
        {
            updatingEditor = true;
            rateBar.Value = Clamp(settings.SpeechRate, rateBar.Minimum, rateBar.Maximum);
            volumeBar.Value = Clamp(settings.SpeechVolume, volumeBar.Minimum, volumeBar.Maximum);
            if (settings.Items == null) settings.Items = new List<BroadcastItem>();
            updatingEditor = false;
        }

        private void RefreshVoices()
        {
            string preferred = settings.SelectedVoice;
            voiceBox.Items.Clear();
            try
            {
                var installed = synthesizer.GetInstalledVoices();
                foreach (InstalledVoice voice in installed)
                {
                    if (voice.Enabled) voiceBox.Items.Add(voice.VoiceInfo.Name);
                }
                if (voiceBox.Items.Count > 0)
                {
                    int index = voiceBox.Items.IndexOf(preferred);
                    voiceBox.SelectedIndex = index >= 0 ? index : 0;
                    voiceStatus.Text = "检测到 " + voiceBox.Items.Count + " 个可用语音。";
                }
                else
                {
                    voiceStatus.Text = "没有检测到本机 SAPI 语音；请安装语音引擎后刷新。";
                }
            }
            catch (Exception ex)
            {
                voiceStatus.Text = "读取 SAPI 语音失败：" + ex.Message;
            }
        }

        private void ApplyVoiceSelection()
        {
            if (voiceBox.SelectedItem == null) return;
            settings.SelectedVoice = Convert.ToString(voiceBox.SelectedItem);
            if (!updatingEditor && queueView.SelectedItems.Count > 0)
            {
                BroadcastItem item = queueView.SelectedItems[0].Tag as BroadcastItem;
                if (item != null) item.Voice = settings.SelectedVoice;
            }
            try { synthesizer.SelectVoice(settings.SelectedVoice); }
            catch (Exception ex) { SetStatus("选择语音失败：" + ex.Message); }
        }

        private void RefreshQueueView(BroadcastItem select)
        {
            queueView.BeginUpdate();
            queueView.Items.Clear();
            int selectedIndex = -1;
            for (int i = 0; i < settings.Items.Count; i++)
            {
                BroadcastItem item = settings.Items[i];
                var row = new ListViewItem(item.Enabled ? "是" : "否");
                row.SubItems.Add(item.Name ?? "");
                row.SubItems.Add(ShortText(item.Text));
                row.Tag = item;
                queueView.Items.Add(row);
                if (Object.ReferenceEquals(item, select)) selectedIndex = i;
            }
            queueView.EndUpdate();
            if (selectedIndex >= 0 && selectedIndex < queueView.Items.Count)
            {
                queueView.Items[selectedIndex].Selected = true;
                queueView.Items[selectedIndex].Focused = true;
            }
            else if (queueView.Items.Count > 0 && queueView.SelectedItems.Count == 0)
            {
                queueView.Items[0].Selected = true;
            }
        }

        private void LoadSelectedItem()
        {
            if (queueView.SelectedItems.Count == 0) return;
            BroadcastItem item = queueView.SelectedItems[0].Tag as BroadcastItem;
            if (item == null) return;
            updatingEditor = true;
            nameBox.Text = item.Name ?? "";
            textBox.Text = item.Text ?? "";
            int voiceIndex = voiceBox.Items.IndexOf(item.Voice);
            if (voiceIndex >= 0) voiceBox.SelectedIndex = voiceIndex;
            repeatCountBox.Value = Clamp(item.RepeatCount, (int)repeatCountBox.Minimum, (int)repeatCountBox.Maximum);
            updatingEditor = false;
        }

        private void UpdateSelectedItem()
        {
            if (updatingEditor || queueView.SelectedItems.Count == 0) return;
            BroadcastItem item = queueView.SelectedItems[0].Tag as BroadcastItem;
            if (item == null) return;
            item.Name = nameBox.Text;
            item.Text = textBox.Text;
            item.Voice = voiceBox.SelectedItem == null ? settings.SelectedVoice : Convert.ToString(voiceBox.SelectedItem);
            item.RepeatCount = Decimal.ToInt32(repeatCountBox.Value);
            int index = queueView.SelectedItems[0].Index;
            queueView.Items[index].SubItems[1].Text = item.Name;
            queueView.Items[index].SubItems[2].Text = ShortText(item.Text);
        }

        private void AddItem()
        {
            var item = new BroadcastItem();
            item.Name = "播音内容 " + (settings.Items.Count + 1);
            item.Voice = settings.SelectedVoice;
            settings.Items.Add(item);
            RefreshQueueView(item);
            nameBox.Focus();
            nameBox.SelectAll();
        }

        private void RemoveSelectedItem()
        {
            if (queueView.SelectedItems.Count == 0) return;
            BroadcastItem item = queueView.SelectedItems[0].Tag as BroadcastItem;
            settings.Items.Remove(item);
            RefreshQueueView(null);
        }

        private void MoveSelectedItem(int delta)
        {
            if (queueView.SelectedItems.Count == 0) return;
            int index = queueView.SelectedItems[0].Index;
            int target = index + delta;
            if (target < 0 || target >= settings.Items.Count) return;
            BroadcastItem item = settings.Items[index];
            settings.Items.RemoveAt(index);
            settings.Items.Insert(target, item);
            RefreshQueueView(item);
        }

        private void ToggleSelectedItem()
        {
            if (queueView.SelectedItems.Count == 0) return;
            BroadcastItem item = queueView.SelectedItems[0].Tag as BroadcastItem;
            item.Enabled = !item.Enabled;
            RefreshQueueView(item);
        }

        private void SpeakSelected()
        {
            if (queueView.SelectedItems.Count == 0) return;
            BroadcastItem item = queueView.SelectedItems[0].Tag as BroadcastItem;
            if (item == null || String.IsNullOrWhiteSpace(item.Text))
            {
                SetStatus("请先输入播报文本。");
                return;
            }
            queuePlaying = false;
            synthesizer.SpeakAsyncCancelAll();
            Speak(item);
        }

        private void SpeakQueue()
        {
            playQueue.Clear();
            foreach (BroadcastItem item in settings.Items)
            {
                if (item.Enabled && !String.IsNullOrWhiteSpace(item.Text)) playQueue.Add(item);
            }
            if (playQueue.Count == 0)
            {
                SetStatus("没有已启用且含有文本的播音内容。");
                return;
            }
            synthesizer.SpeakAsyncCancelAll();
            queuePlaying = true;
            playQueueIndex = 0;
            repeatsLeft = Math.Max(1, playQueue[0].RepeatCount);
            Speak(playQueue[0]);
        }

        private void Speak(BroadcastItem item)
        {
            try
            {
                if (!String.IsNullOrEmpty(item.Voice)) synthesizer.SelectVoice(item.Voice);
                synthesizer.Rate = settings.SpeechRate;
                synthesizer.Volume = settings.SpeechVolume;
                synthesizer.SpeakAsync(item.Text);
                SetStatus("正在播报：" + (item.Name ?? "未命名内容"));
            }
            catch (Exception ex)
            {
                queuePlaying = false;
                SetStatus("播报失败：" + ex.Message);
            }
        }

        private void OnSpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (IsDisposed || !queuePlaying) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (!queuePlaying) return;
                    if (e.Error != null)
                    {
                        queuePlaying = false;
                        SetStatus("队列播报失败：" + e.Error.Message);
                        return;
                    }
                    repeatsLeft--;
                    if (repeatsLeft > 0)
                    {
                        Speak(playQueue[playQueueIndex]);
                        return;
                    }
                    playQueueIndex++;
                    if (playQueueIndex >= playQueue.Count)
                    {
                        queuePlaying = false;
                        SetStatus("播报队列已完成。");
                        return;
                    }
                    repeatsLeft = Math.Max(1, playQueue[playQueueIndex].RepeatCount);
                    Speak(playQueue[playQueueIndex]);
                });
            }
            catch (InvalidOperationException) { }
        }

        private void StopSpeech()
        {
            queuePlaying = false;
            playQueue.Clear();
            synthesizer.SpeakAsyncCancelAll();
            SetStatus("已停止播报。");
        }

        private void ChooseMusicFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择背景音乐文件夹";
                if (Directory.Exists(settings.MusicFolder)) dialog.SelectedPath = settings.MusicFolder;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    settings.MusicFolder = dialog.SelectedPath;
                    musicFolderBox.Text = dialog.SelectedPath;
                    LoadMusicFiles();
                }
            }
        }

        private void LoadMusicFiles()
        {
            musicList.Items.Clear();
            string folder = settings.MusicFolder;
            if (String.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
            string[] extensions = new string[] { "*.mp3", "*.wav", "*.wma" };
            foreach (string pattern in extensions)
            {
                try
                {
                    foreach (string file in Directory.GetFiles(folder, pattern)) musicList.Items.Add(file);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            if (musicList.Items.Count > 0) musicList.SelectedIndex = 0;
            SetStatus("背景音乐目录中找到 " + musicList.Items.Count + " 个 MP3/WAV/WMA 文件。");
        }

        private void PlaySelectedMusic()
        {
            if (musicList.SelectedItem == null)
            {
                SetStatus("请先选择一首背景音乐。");
                return;
            }
            string file = Convert.ToString(musicList.SelectedItem);
            StopMusic();
            string type = String.Equals(Path.GetExtension(file), ".wav", StringComparison.OrdinalIgnoreCase) ? "waveaudio" : "mpegvideo";
            int openResult = Mci("open \"" + file + "\" type " + type + " alias " + currentMusicAlias);
            if (openResult != 0)
            {
                SetStatus("无法打开背景音乐，请换一个 MP3 或 WAV 文件。");
                return;
            }
            int playResult = Mci("play " + currentMusicAlias + " repeat");
            if (playResult != 0)
            {
                Mci("close " + currentMusicAlias);
                SetStatus("播放背景音乐失败。");
                return;
            }
            SetStatus("背景音乐循环播放中：" + Path.GetFileName(file));
        }

        private void StopMusic()
        {
            Mci("stop " + currentMusicAlias);
            Mci("close " + currentMusicAlias);
        }

        private void SaveSettings()
        {
            try
            {
                string directory = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                var serializer = new XmlSerializer(typeof(AppSettings));
                using (var stream = new FileStream(settingsPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(stream, settings);
                }
                SetStatus("设置已保存到用户配置目录。");
            }
            catch (Exception ex)
            {
                SetStatus("保存失败：" + ex.Message);
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            StopSpeech();
            StopMusic();
            synthesizer.Dispose();
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.Text = message;
        }

        private static AppSettings LoadSettings(string path)
        {
            try
            {
                if (!File.Exists(path)) return new AppSettings();
                var serializer = new XmlSerializer(typeof(AppSettings));
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    AppSettings loaded = serializer.Deserialize(stream) as AppSettings;
                    return loaded ?? new AppSettings();
                }
            }
            catch { return new AppSettings(); }
        }

        private static string ShortText(string value)
        {
            if (String.IsNullOrEmpty(value)) return "";
            string singleLine = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return singleLine.Length > 80 ? singleLine.Substring(0, 80) + "…" : singleLine;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }

        private static int Mci(string command)
        {
            return mciSendString(command, null, 0, IntPtr.Zero);
        }

        [System.Runtime.InteropServices.DllImport("winmm.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr callback);
    }
}


