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
        private Label lblStatus, lblDetailName, lblDetailInfo;
        private PictureBox detailIcon;
        private FileSystemWatcher watcher; // Penjaga folder

        private List<string> allGamePaths = new List<string>();
        private string configPath = "config.json", currentRootPath = "";
        private Image rawBgImage = null;

        private string[] ignoreWords = { "helper", "crash", "unity", "setup", "startup", "notification", "unins", "redist", "vc_redist", "dxwebsetup", "engine", "win64", "win32", "x64",
                                        "x86", "commonredist", "dotnet", "framework", "physx", "touchup", "cleanup", "workshop", "bug", "opinion" };

        public Form1()
        {
            SetupUI();
            LoadConfig();
            this.SizeChanged += (s, e) => { UpdateScrollHiddenWidth(); RefreshList(); };
        }

        private void SetupUI()
        {
            this.Text = "Game Launcher BETA v0.2 (Auto-Sync)";
            try { this.Icon = new Icon("launchergame.ico"); } catch { }
            this.Size = new Size(1100, 750);
            this.MinimumSize = new Size(950, 600);
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Font = new Font("Segoe UI", 10);

            // --- TOP BAR ---
            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(30, 30, 30) };
            txtSearch = new TextBox { Location = new Point(25, 25), Width = 350, PlaceholderText = " Cari game...", BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.TextChanged += (s, e) => RefreshList();

            Button btnBrowse = CreateButton("Set Folder", new Point(390, 24), 110, Color.FromArgb(60, 60, 60));
            btnBrowse.Click += HandleBrowse;

            Button btnBg = CreateButton("🖼 Change BG", new Point(topBar.Width - 140, 24), 110, Color.FromArgb(0, 120, 215));
            btnBg.Anchor = AnchorStyles.Right;
            btnBg.Click += HandleChangeBg;

            topBar.Controls.AddRange(new Control[] { txtSearch, btnBrowse, btnBg });

            // --- BOTTOM BAR ---
            Panel bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(25, 25, 25) };
            lblStatus = new Label { Text = "0 Games Found", ForeColor = Color.Gray, Location = new Point(15, 7), AutoSize = true, Font = new Font("Segoe UI", 8) };
            bottomBar.Controls.Add(lblStatus);

            // --- DETAIL PANE ---
            detailPane = new Panel { Dock = DockStyle.Right, Width = 350, BackColor = Color.FromArgb(25, 25, 25), Padding = new Padding(10) };
            detailIcon = new PictureBox { Size = new Size(128, 128), Location = new Point(111, 40), SizeMode = PictureBoxSizeMode.StretchImage };
            lblDetailName = new Label { Text = "Pilih Game", ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Location = new Point(10, 185), Width = 330, Height = 70, TextAlign = ContentAlignment.TopCenter };
            lblDetailInfo = new Label { Text = "", ForeColor = Color.LightGray, Location = new Point(30, 255), Width = 290, Height = 250 };
            Button btnOpenLoc = CreateButton("Open File Location", Point.Empty, 0, Color.FromArgb(50, 50, 50));
            btnOpenLoc.Dock = DockStyle.Bottom; btnOpenLoc.Height = 45;
            btnOpenLoc.Click += (s, e) => { if (lblDetailInfo.Tag != null) Process.Start("explorer.exe", Path.GetDirectoryName(lblDetailInfo.Tag.ToString())); };

            detailPane.Controls.AddRange(new Control[] { detailIcon, lblDetailName, lblDetailInfo, btnOpenLoc });

            // --- LIST CONTAINER ---
            listContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 20) };
            listContainer.Paint += DrawBackground;
            flowGameList = new FlowLayoutPanel { Location = new Point(0, 0), AutoScroll = true, BackColor = Color.Transparent, WrapContents = false, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20, 10, 20, 10) };
            listContainer.Controls.Add(flowGameList);

            this.Controls.AddRange(new Control[] { listContainer, detailPane, bottomBar, topBar });
        }

        private Button CreateButton(string text, Point loc, int w, Color bg) => new Button { Text = text, Location = loc, Width = w, Height = 32, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = bg, Cursor = Cursors.Hand };

        // --- LOGIKA AUTO-UPDATE FOLDER ---
        private void SetupWatcher(string path)
        {
            if (!Directory.Exists(path)) return;
            watcher?.Dispose();
            watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            // Jika ada perubahan, panggil ScanGames
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

        private void UpdateScrollHiddenWidth() { if (listContainer != null) { flowGameList.Width = listContainer.Width + 25; flowGameList.Height = listContainer.Height; } }

        private void RefreshList()
        {
            if (flowGameList == null) return;
            flowGameList.Controls.Clear();
            flowGameList.SuspendLayout();
            foreach (var path in allGamePaths)
            {
                string displayName = GetGameName(path);
                if (!string.IsNullOrEmpty(txtSearch.Text) && !displayName.ToLower().Contains(txtSearch.Text.ToLower())) continue;

                Panel card = new Panel { Size = new Size(listContainer.Width - 45, 75), Margin = new Padding(0, 0, 0, 10), BackColor = Color.FromArgb(160, 35, 35, 35), Cursor = Cursors.Hand };
                card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(200, 50, 50, 50);
                card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(160, 35, 35, 35);
                card.Click += (s, e) => UpdateDetailPane(path, displayName);

                PictureBox pic = new PictureBox { Image = GetIconSafely(path), Size = new Size(48, 48), Location = new Point(12, 13), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.Transparent, Enabled = false };
                Label lbl = new Label { Text = displayName, ForeColor = Color.White, Location = new Point(70, 26), Width = card.Width - 150, Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoEllipsis = true, BackColor = Color.Transparent, Enabled = false };
                Button btnPlay = new Button { Text = "▶", Location = new Point(card.Width - 65, 17), Size = new Size(50, 40), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, Font = new Font("Segoe UI", 14), Anchor = AnchorStyles.Right, Cursor = Cursors.Hand };
                btnPlay.FlatAppearance.BorderSize = 0;
                btnPlay.Click += (s, e) => { try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(path) }); } catch (Exception ex) { MessageBox.Show(ex.Message); } };

                card.Controls.AddRange(new Control[] { pic, lbl, btnPlay });
                flowGameList.Controls.Add(card);
            }
            flowGameList.ResumeLayout();
            lblStatus.Text = $"{flowGameList.Controls.Count} Games Found | {currentRootPath}";
        }

        private void UpdateDetailPane(string path, string name)
        {
            try
            {
                detailIcon.Image = Icon.ExtractAssociatedIcon(path)?.ToBitmap();
                lblDetailName.Text = name;
                lblDetailInfo.Text = $"File: {Path.GetFileName(path)}\n\nSize: {(new FileInfo(path).Length / 1048576.0):F2} MB\n\nLocation:\n{Path.GetDirectoryName(path)}\n\nType: {Path.GetExtension(path).ToUpper()}";
                lblDetailInfo.Tag = path;
            }
            catch { }
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
                if (fbd.ShowDialog() == DialogResult.OK) { currentRootPath = fbd.SelectedPath; SaveConfig(currentRootPath, ""); ScanGames(currentRootPath); }
        }

        private void SaveConfig(string folder, string bg)
        {
            string oldBg = File.Exists(configPath) ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath)).BgPath : "";
            File.WriteAllText(configPath, JsonSerializer.Serialize(new AppConfig { LastFolderPath = folder, BgPath = string.IsNullOrEmpty(bg) ? oldBg : bg }));
        }

        private void ScanGames(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            try
            {
                allGamePaths = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => (f.ToLower().EndsWith(".exe") || f.ToLower().EndsWith(".lnk") || f.ToLower().EndsWith(".url")) && !ignoreWords.Any(w => f.ToLower().Contains(w))).ToList();
                SetupWatcher(path); // Update penjaga folder
                UpdateScrollHiddenWidth(); RefreshList();
            }
            catch { }
        }

        private void LoadConfig()
        {
            if (!File.Exists(configPath)) return;
            try
            {
                var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath));
                if (config == null) return;
                if (Directory.Exists(config.LastFolderPath)) { currentRootPath = config.LastFolderPath; ScanGames(currentRootPath); }
                if (!string.IsNullOrEmpty(config.BgPath)) SetBackground(config.BgPath);
            }
            catch { }
        }
    }
    public class AppConfig { public string LastFolderPath { get; set; } = ""; public string BgPath { get; set; } = ""; }
}