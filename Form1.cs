using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace gamelauncher
{
    public partial class Form1 : Form
    {
        private TextBox txtSearch;
        private FlowLayoutPanel flowGameList;
        private Panel listContainer, detailPane;
        private Panel customScrollTrack, customScrollThumb;
        private bool isDraggingScroll = false;
        private int dragStartY = 0;
        private Label lblStatus, lblDetailName, lblDetailInfo;
        private PictureBox detailIcon;
        private FileSystemWatcher watcher;
        private List<string> currentFavorites = new List<string>();
        private Button btnFavorite;

        private List<string> allGamePaths = new List<string>();
        private string configPath = "config.json", currentRootPath = "";
        private Image rawBgImage = null;

        private string[] ignoreWords = { "helper", "crash", "unity", "setup", "startup", "notification", "unins", "redist", "vc_redist",
                                         "dxwebsetup", "engine", "win64", "win32", "x64", "x86", "commonredist", "dotnet", "framework",
                                         "physx", "touchup", "cleanup", "workshop", "bug", "opinion", "config"};

        public Form1()
        {
            SetupUI();
            LoadConfig();
            this.SizeChanged += (s, e) =>
            {
                UpdateScrollHiddenWidth();
                foreach (Control c in flowGameList.Controls) c.Width = listContainer.Width - 45;
            };
        }

        private void SetupUI()
        {
            this.Text = "Game Launcher BETA v0.1 (Auto-Sync)";

            // AMBIL ICON DARI EMBEDDED RESOURCE
            try
            {
                var assembly = typeof(Form1).Assembly;
                string[] resources = assembly.GetManifestResourceNames();
                string iconName = resources.FirstOrDefault(r => r.EndsWith("launchergame.ico"));

                if (iconName != null)
                {
                    using (var stream = assembly.GetManifestResourceStream(iconName))
                    {
                        if (stream != null) this.Icon = new Icon(stream);
                    }
                }
            }
            catch { }

            this.Size = new Size(1100, 800);
            this.MinimumSize = new Size(1100, 800);
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Font = new Font("Segoe UI", 10);

            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(30, 30, 30) };
            txtSearch = new TextBox { Location = new Point(25, 25), Width = 350, PlaceholderText = " Cari game...", BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.TextChanged += (s, e) => RefreshList();

            Button btnBrowse = CreateButton("Set Folder", new Point(390, 24), 110, Color.FromArgb(60, 60, 60));
            btnBrowse.Click += HandleBrowse;

            Button btnRemoveBg = CreateButton("❌ Remove BG", new Point(topBar.Width - 260, 24), 110, Color.FromArgb(60, 60, 60));
            btnRemoveBg.Anchor = AnchorStyles.Right;
            btnRemoveBg.Click += (s, e) =>
            {
                rawBgImage?.Dispose();
                rawBgImage = null;
                listContainer.Invalidate();
                SaveConfig(currentRootPath, "");
            };

            Button btnBg = CreateButton("🖼 Change BG", new Point(topBar.Width - 140, 24), 110, Color.FromArgb(0, 120, 215));
            btnBg.Anchor = AnchorStyles.Right;
            btnBg.Click += HandleChangeBg;

            topBar.Controls.AddRange(new Control[] { txtSearch, btnBrowse, btnRemoveBg, btnBg });

            Panel bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(25, 25, 25) };
            lblStatus = new Label { Text = "0 Games Found", ForeColor = Color.Gray, Location = new Point(15, 7), AutoSize = true, Font = new Font("Segoe UI", 8) };
            bottomBar.Controls.Add(lblStatus);

            detailPane = new Panel { Dock = DockStyle.Right, Width = 350, BackColor = Color.FromArgb(25, 25, 25), Padding = new Padding(10) };
            detailIcon = new PictureBox { Size = new Size(128, 128), Location = new Point(111, 40), SizeMode = PictureBoxSizeMode.StretchImage };
            lblDetailName = new Label { Text = "Pilih Game", ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Location = new Point(10, 185), Width = 330, Height = 70, TextAlign = ContentAlignment.TopCenter };
            lblDetailInfo = new Label { Text = "", ForeColor = Color.LightGray, Location = new Point(30, 255), Width = 290, Height = 250 };
            Panel detailBottomPane = new Panel { Dock = DockStyle.Bottom, Height = 100, BackColor = Color.Transparent };

            Button btnOpenLoc = CreateButton("Open File Location", Point.Empty, 0, Color.FromArgb(50, 50, 50));
            btnOpenLoc.Dock = DockStyle.Bottom; btnOpenLoc.Height = 45;
            btnOpenLoc.Click += (s, e) => { if (lblDetailInfo.Tag != null) Process.Start("explorer.exe", Path.GetDirectoryName(lblDetailInfo.Tag.ToString())); };

            btnFavorite = CreateButton("☆ Favorite", Point.Empty, 0, Color.FromArgb(40, 160, 40));
            btnFavorite.Dock = DockStyle.Top; btnFavorite.Height = 45;
            btnFavorite.Visible = false;
            btnFavorite.Click += (s, e) =>
            {
                if (lblDetailInfo.Tag == null) return;
                string path = lblDetailInfo.Tag.ToString();
                if (currentFavorites.Contains(path)) currentFavorites.Remove(path); else currentFavorites.Add(path);
                SaveConfig(currentRootPath, null);
                UpdateDetailPane(path, lblDetailName.Text);
                RefreshList();
            };

            detailBottomPane.Controls.AddRange(new Control[] { btnFavorite, btnOpenLoc });
            detailPane.Controls.AddRange(new Control[] { detailIcon, lblDetailName, lblDetailInfo, detailBottomPane });

            listContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 20) };
            listContainer.Paint += DrawBackground;

            customScrollTrack = new Panel { Location = new Point(8, 10), Width = 6, BackColor = Color.FromArgb(40, 40, 40), Cursor = Cursors.Hand };
            customScrollThumb = new Panel { Width = 6, BackColor = Color.FromArgb(0, 120, 215) };
            customScrollTrack.Controls.Add(customScrollThumb);

            customScrollThumb.MouseDown += (s, e) => { isDraggingScroll = true; dragStartY = e.Y; };
            customScrollThumb.MouseUp += (s, e) => { isDraggingScroll = false; };
            customScrollThumb.MouseMove += (s, e) =>
            {
                if (isDraggingScroll)
                {
                    int newTop = customScrollThumb.Top + e.Y - dragStartY;
                    if (newTop < 0) newTop = 0;
                    if (newTop > customScrollTrack.Height - customScrollThumb.Height) newTop = customScrollTrack.Height - customScrollThumb.Height;
                    customScrollThumb.Top = newTop;

                    float ratio = (float)newTop / (customScrollTrack.Height - customScrollThumb.Height);
                    int maxScroll = flowGameList.VerticalScroll.Maximum - flowGameList.VerticalScroll.LargeChange + 1;
                    if (maxScroll < 1) maxScroll = 1;
                    flowGameList.AutoScrollPosition = new Point(0, (int)(ratio * maxScroll));
                }
            };

            flowGameList = new FlowLayoutPanel { Location = new Point(22, 0), AutoScroll = true, BackColor = Color.Transparent, WrapContents = false, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10, 10, 20, 10) };
            flowGameList.Scroll += (s, e) => UpdateCustomScroll();
            flowGameList.MouseWheel += (s, e) => UpdateCustomScroll();

            listContainer.Controls.Add(customScrollTrack);
            listContainer.Controls.Add(flowGameList);

            this.Controls.AddRange(new Control[] { listContainer, detailPane, bottomBar, topBar });
        }

        private Button CreateButton(string text, Point loc, int w, Color bg) => new Button { Text = text, Location = loc, Width = w, Height = 32, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = bg, Cursor = Cursors.Hand };

        private void SetupWatcher(string path)
        {
            if (!Directory.Exists(path)) return;
            watcher?.Dispose();
            watcher = new FileSystemWatcher(path) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite, EnableRaisingEvents = true };
            watcher.Created += (s, e) => this.Invoke(new Action(() => ScanGames(currentRootPath)));
            watcher.Deleted += (s, e) => this.Invoke(new Action(() => ScanGames(currentRootPath)));
            watcher.Renamed += (s, e) => this.Invoke(new Action(() => ScanGames(currentRootPath)));
        }

        private void DrawBackground(object sender, PaintEventArgs e)
        {
            if (rawBgImage == null) return;
            float scale = Math.Max((float)listContainer.Width / rawBgImage.Width, (float)listContainer.Height / rawBgImage.Height);
            int newW = (int)(rawBgImage.Width * scale), newH = (int)(rawBgImage.Height * scale);
            var matrix = new System.Drawing.Imaging.ColorMatrix(new float[][] { new float[] { 0.4f, 0, 0, 0, 0 }, new float[] { 0, 0.4f, 0, 0, 0 }, new float[] { 0, 0, 0.4f, 0, 0 }, new float[] { 0, 0, 0, 1, 0 }, new float[] { 0, 0, 0, 0, 1 } });
            var attr = new System.Drawing.Imaging.ImageAttributes(); attr.SetColorMatrix(matrix);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(rawBgImage, new Rectangle((listContainer.Width - newW) / 2, (listContainer.Height - newH) / 2, newW, newH), 0, 0, rawBgImage.Width, rawBgImage.Height, GraphicsUnit.Pixel, attr);
        }

        private void HandleChangeBg(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp" })
                if (ofd.ShowDialog() == DialogResult.OK) { SetBackground(ofd.FileName); SaveConfig(currentRootPath, ofd.FileName); }
        }

        private void SetBackground(string path)
        {
            if (!File.Exists(path)) return;
            rawBgImage?.Dispose();
            rawBgImage = Image.FromFile(path);
            listContainer.Invalidate();
        }

        private void UpdateScrollHiddenWidth()
        {
            if (listContainer != null && flowGameList != null && customScrollTrack != null)
            {
                flowGameList.Width = listContainer.Width - 22 + 25;
                flowGameList.Height = listContainer.Height;
                customScrollTrack.Height = listContainer.Height - 20;
                UpdateCustomScroll();
            }
        }

        private void UpdateCustomScroll()
        {
            if (customScrollTrack == null || customScrollThumb == null || flowGameList == null) return;
            if (this.InvokeRequired) { this.Invoke(new Action(UpdateCustomScroll)); return; }

            int contentHeight = flowGameList.PreferredSize.Height;
            int visibleHeight = listContainer.Height;
            if (contentHeight <= visibleHeight) { customScrollTrack.Visible = false; return; }

            customScrollTrack.Visible = true;

            float heightRatio = (float)visibleHeight / contentHeight;
            int thumbHeight = (int)(customScrollTrack.Height * heightRatio);
            customScrollThumb.Height = Math.Max(30, thumbHeight);

            if (!isDraggingScroll)
            {
                int maxScroll = flowGameList.VerticalScroll.Maximum - flowGameList.VerticalScroll.LargeChange + 1;
                if (maxScroll < 1) maxScroll = 1;
                float scrollRatio = (float)flowGameList.VerticalScroll.Value / maxScroll;

                int newTop = (int)(scrollRatio * (customScrollTrack.Height - customScrollThumb.Height));
                if (newTop < 0) newTop = 0;
                if (newTop > customScrollTrack.Height - customScrollThumb.Height) newTop = customScrollTrack.Height - customScrollThumb.Height;
                customScrollThumb.Top = newTop;
            }
        }

        private void RefreshList()
        {
            if (flowGameList == null) return;
            flowGameList.SuspendLayout();
            flowGameList.Controls.Clear();
            var query = txtSearch.Text.ToLower();

            foreach (var path in allGamePaths.OrderByDescending(p => currentFavorites.Contains(p)).ThenBy(p => GetGameName(p)))
            {
                string displayName = GetGameName(path);
                if (!string.IsNullOrEmpty(query) && !displayName.ToLower().Contains(query)) continue;

                Panel card = new Panel { Size = new Size(listContainer.Width - 45, 75), Margin = new Padding(0, 0, 0, 10), BackColor = Color.FromArgb(160, 35, 35, 35), Cursor = Cursors.Hand };
                card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(200, 50, 50, 50);
                card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(160, 35, 35, 35);
                card.Click += (s, e) => UpdateDetailPane(path, displayName);

                PictureBox pic = new PictureBox { Image = GetIconSafely(path), Size = new Size(48, 48), Location = new Point(12, 13), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.Transparent, Enabled = false };
                Label lbl = new Label { Text = displayName, ForeColor = Color.White, Location = new Point(70, 26), Width = card.Width - 180, Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoEllipsis = true, BackColor = Color.Transparent, Enabled = false };
                Button btnPlay = new Button { Text = "▶", Location = new Point(card.Width - 65, 17), Size = new Size(50, 40), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, Font = new Font("Segoe UI", 14), Anchor = AnchorStyles.Right, Cursor = Cursors.Hand };
                btnPlay.FlatAppearance.BorderSize = 0;
                btnPlay.Click += (s, e) => { try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(path) }); } catch (Exception ex) { MessageBox.Show(ex.Message); } };

                var controls = new List<Control> { pic, lbl, btnPlay };
                if (currentFavorites.Contains(path))
                {
                    Label lblStar = new Label { Text = "★", ForeColor = Color.Gold, Location = new Point(card.Width - 100, 24), Font = new Font("Segoe UI", 14), AutoSize = true, BackColor = Color.Transparent, Enabled = false, Anchor = AnchorStyles.Right };
                    controls.Add(lblStar);
                }

                card.Controls.AddRange(controls.ToArray());
                flowGameList.Controls.Add(card);
            }
            flowGameList.ResumeLayout();
            lblStatus.Text = $"{flowGameList.Controls.Count} Games Found | {currentRootPath}";
            UpdateCustomScroll();
        }

        private void UpdateDetailPane(string path, string name)
        {
            try
            {
                detailIcon.Image = Icon.ExtractAssociatedIcon(path)?.ToBitmap();
                lblDetailName.Text = name;
                lblDetailInfo.Text = $"File: {Path.GetFileName(path)}\n\nSize: Menghitung...\n\nLocation:\n{Path.GetDirectoryName(path)}\n\nType: {Path.GetExtension(path).ToUpper()}";
                lblDetailInfo.Tag = path;
                btnFavorite.Visible = true;
                btnFavorite.Text = currentFavorites.Contains(path) ? "★ Unfavorite" : "☆ Favorite";
                btnFavorite.BackColor = currentFavorites.Contains(path) ? Color.FromArgb(160, 40, 40) : Color.FromArgb(40, 160, 40);

                System.Threading.Tasks.Task.Run(() =>
                {
                    double sizeMB = GetGameSizeMB(path);
                    string sizeStr = sizeMB >= 1024 ? $"{(sizeMB / 1024.0):F2} GB" : $"{sizeMB:F2} MB";
                    this.Invoke(new Action(() =>
                    {
                        if (lblDetailInfo.Tag?.ToString() == path)
                        {
                            lblDetailInfo.Text = $"File: {Path.GetFileName(path)}\n\nSize: {sizeStr}\n\nLocation:\n{Path.GetDirectoryName(path)}\n\nType: {Path.GetExtension(path).ToUpper()}";
                        }
                    }));
                });
            }
            catch { }
        }

        private double GetGameSizeMB(string path)
        {
            try
            {
                if (path.ToLower().EndsWith(".lnk") || path.ToLower().EndsWith(".url"))
                    return new FileInfo(path).Length / 1048576.0;

                string[] parts = Path.GetRelativePath(currentRootPath, path).Split(Path.DirectorySeparatorChar);
                if (parts.Length > 1)
                {
                    string topFolder = Path.Combine(currentRootPath, parts[0]);
                    long size = new DirectoryInfo(topFolder).EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
                    return size / 1048576.0;
                }
                return new FileInfo(path).Length / 1048576.0;
            }
            catch { return 0; }
        }

        private Bitmap GetIconSafely(string path) { try { return Icon.ExtractAssociatedIcon(path)?.ToBitmap(); } catch { return new Bitmap(1, 1); } }

        private string GetGameName(string path)
        {
            if (path.ToLower().EndsWith(".lnk") || path.ToLower().EndsWith(".url")) return Path.GetFileNameWithoutExtension(path);
            string[] parts = Path.GetRelativePath(currentRootPath, path).Split(Path.DirectorySeparatorChar);
            return parts.Length > 1 ? parts[0] : Path.GetFileNameWithoutExtension(path);
        }

        private void HandleBrowse(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                if (fbd.ShowDialog() == DialogResult.OK) { currentRootPath = fbd.SelectedPath; SaveConfig(currentRootPath, null); ScanGames(currentRootPath); }
        }

        private void SaveConfig(string folder, string bg)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

            string configDir = Path.Combine(folder, "mygamelauncher");
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

            string path = Path.Combine(configDir, "config.json");
            string oldBg = "";
            if (File.Exists(path))
            {
                try { oldBg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path))?.BgPath ?? ""; } catch { }
            }

            string newBg = (bg == null) ? oldBg : bg;

            File.WriteAllText(path, JsonSerializer.Serialize(new AppConfig { LastFolderPath = folder, BgPath = newBg, Favorites = currentFavorites }));
            File.WriteAllText(configPath, JsonSerializer.Serialize(new AppConfig { LastFolderPath = folder, BgPath = "" }));
        }

        private void ScanGames(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            try
            {
                allGamePaths = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => (f.ToLower().EndsWith(".exe") || f.ToLower().EndsWith(".lnk") || f.ToLower().EndsWith(".url")) && !ignoreWords.Any(w => f.ToLower().Contains(w))).ToList();
                SetupWatcher(path);
                UpdateScrollHiddenWidth(); RefreshList();
            }
            catch { }
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    var baseConfig = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath));
                    if (baseConfig != null && Directory.Exists(baseConfig.LastFolderPath))
                    {
                        currentRootPath = baseConfig.LastFolderPath;
                        ScanGames(currentRootPath);

                        string path = Path.Combine(currentRootPath, "mygamelauncher", "config.json");
                        if (File.Exists(path))
                        {
                            var folderConfig = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path));
                            if (folderConfig != null)
                            {
                                if (!string.IsNullOrEmpty(folderConfig.BgPath)) SetBackground(folderConfig.BgPath);
                                if (folderConfig.Favorites != null) currentFavorites = folderConfig.Favorites;
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }
    public class AppConfig { public string LastFolderPath { get; set; } = ""; public string BgPath { get; set; } = ""; public List<string> Favorites { get; set; } = new List<string>(); }
}