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
        private Panel listContainer; // Kontainer tambahan untuk menyembunyikan scrollbar
        private Panel detailPane;
        private Label lblStatus;

        private PictureBox detailIcon;
        private Label lblDetailName;
        private Label lblDetailInfo;

        private List<string> allGamePaths = new List<string>();
        private string configPath = "config.json";
        private string currentRootPath = "";

        private string[] ignoreWords = { "helper", "crash", "unity", "setup", "startup", "notification", "unins", "redist", "vc_redist", "dxwebsetup", "engine", "win64", "win32", "x64", "x86", "commonredist", "dotnet", "framework", "physx", "touchup", "cleanup", "workshop" };

        public Form1()
        {
            SetupUI();
            LoadConfig();
            this.SizeChanged += (s, e) =>
            {
                UpdateScrollHiddenWidth();
                RefreshList();
            };
        }

        private void SetupUI()
        {
            this.Text = "Game Launcher Pro v1.7";
            this.Icon = new Icon("launchergame.ico");
            this.Size = new Size(1100, 750);
            this.MinimumSize = new Size(950, 600);
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Font = new Font("Segoe UI", 10);

            // --- TOP BAR ---
            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(30, 30, 30) };
            txtSearch = new TextBox
            {
                Location = new Point(25, 25),
                Width = 350,
                PlaceholderText = " Cari game...",
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => RefreshList();

            Button btnBrowse = new Button
            {
                Text = "Set Folder",
                Location = new Point(390, 24),
                Width = 120,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += HandleBrowse;
            topBar.Controls.Add(txtSearch); topBar.Controls.Add(btnBrowse);

            // --- BOTTOM BAR ---
            Panel bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(25, 25, 25) };
            lblStatus = new Label { Text = "0 Games Found", ForeColor = Color.Gray, Location = new Point(15, 7), AutoSize = true, Font = new Font("Segoe UI", 8) };
            bottomBar.Controls.Add(lblStatus);

            // --- DETAIL PANE ---
            detailPane = new Panel { Dock = DockStyle.Right, Width = 350, BackColor = Color.FromArgb(25, 25, 25), Padding = new Padding(10) };
            detailIcon = new PictureBox { Size = new Size(128, 128), Location = new Point(111, 40), SizeMode = PictureBoxSizeMode.StretchImage };
            lblDetailName = new Label { Text = "Pilih Game", ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Location = new Point(10, 185), Width = 330, Height = 70, TextAlign = ContentAlignment.TopCenter };
            lblDetailInfo = new Label { Text = "", ForeColor = Color.LightGray, Location = new Point(30, 265), Width = 290, Height = 250 };

            Button btnOpenLoc = new Button { Text = "Open File Location", Dock = DockStyle.Bottom, Height = 45, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(50, 50, 50) };
            btnOpenLoc.FlatAppearance.BorderSize = 0;
            btnOpenLoc.Click += (s, e) => { if (lblDetailInfo.Tag != null) Process.Start("explorer.exe", Path.GetDirectoryName(lblDetailInfo.Tag.ToString())); };

            detailPane.Controls.Add(detailIcon); detailPane.Controls.Add(lblDetailName);
            detailPane.Controls.Add(lblDetailInfo); detailPane.Controls.Add(btnOpenLoc);

            // --- LIST CONTAINER (Trik Sembunyikan Scrollbar) ---
            listContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0) };

            flowGameList = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                AutoScroll = true,
                BackColor = Color.Transparent,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(20, 10, 20, 10)
            };

            listContainer.Controls.Add(flowGameList);
            this.Controls.Add(listContainer);
            this.Controls.Add(detailPane);
            this.Controls.Add(bottomBar);
            this.Controls.Add(topBar);
        }

        private void UpdateScrollHiddenWidth()
        {
            // Buat lebar flowGameList lebih lebar 25px dari kontainernya agar scrollbar sembunyi di kanan
            flowGameList.Width = listContainer.Width + 25;
            flowGameList.Height = listContainer.Height;
        }

        private void RefreshList()
        {
            if (flowGameList == null) return;
            flowGameList.Controls.Clear();
            flowGameList.SuspendLayout();

            string query = txtSearch.Text.ToLower();
            int count = 0;

            foreach (var path in allGamePaths)
            {
                string displayName = GetGameName(path);
                if (!string.IsNullOrEmpty(query) && !displayName.ToLower().Contains(query)) continue;
                count++;

                // Lebar kartu disesuaikan agar pas di mata (dikurangi 45px dari kontainer asli)
                int cardWidth = listContainer.Width - 45;
                Panel card = new Panel { Size = new Size(cardWidth, 75), Margin = new Padding(0, 0, 0, 10), BackColor = Color.FromArgb(35, 35, 35), Cursor = Cursors.Hand };

                card.Click += (s, e) => UpdateDetailPane(path, displayName);
                card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(50, 50, 50);
                card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(35, 35, 35);

                PictureBox pic = new PictureBox { Image = GetIconSafely(path), Size = new Size(48, 48), Location = new Point(12, 13), SizeMode = PictureBoxSizeMode.StretchImage };
                pic.Click += (s, e) => UpdateDetailPane(path, displayName);

                Label lbl = new Label
                {
                    Text = displayName,
                    ForeColor = Color.White,
                    Location = new Point(70, 26),
                    AutoSize = false,
                    Width = card.Width - 150,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    AutoEllipsis = true
                };
                lbl.Click += (s, e) => UpdateDetailPane(path, displayName);

                Button btnPlay = new Button
                {
                    Text = "▶",
                    Location = new Point(card.Width - 65, 17),
                    Size = new Size(50, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 14),
                    Anchor = AnchorStyles.Right,
                    Cursor = Cursors.Hand
                };
                btnPlay.FlatAppearance.BorderSize = 0;
                btnPlay.Click += (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(path) }); }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                };

                card.Controls.Add(pic); card.Controls.Add(lbl); card.Controls.Add(btnPlay);
                flowGameList.Controls.Add(card);
            }
            flowGameList.ResumeLayout();
            lblStatus.Text = $"{count} Games Found | {currentRootPath}";
        }

        private void UpdateDetailPane(string path, string name)
        {
            try
            {
                detailIcon.Image = Icon.ExtractAssociatedIcon(path)?.ToBitmap();
                lblDetailName.Text = name;
                FileInfo fi = new FileInfo(path);
                double sizeMb = fi.Exists ? fi.Length / (1024.0 * 1024.0) : 0;
                lblDetailInfo.Text = $"File: {Path.GetFileName(path)}\n\nSize: {sizeMb:F2} MB\n\nLocation:\n{Path.GetDirectoryName(path)}\n\nType: {Path.GetExtension(path).ToUpper()}";
                lblDetailInfo.Tag = path;
            }
            catch { }
        }

        private Bitmap GetIconSafely(string path) { try { return Icon.ExtractAssociatedIcon(path)?.ToBitmap(); } catch { return new Bitmap(1, 1); } }

        private string GetGameName(string path)
        {
            if (path.ToLower().EndsWith(".lnk") || path.ToLower().EndsWith(".url")) return Path.GetFileNameWithoutExtension(path);
            string relativePath = Path.GetRelativePath(currentRootPath, path);
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar);
            return (parts.Length > 1) ? parts[0] : Path.GetFileNameWithoutExtension(path);
        }

        private void HandleBrowse(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    currentRootPath = fbd.SelectedPath;
                    File.WriteAllText(configPath, JsonSerializer.Serialize(new AppConfig { LastFolderPath = currentRootPath }));
                    ScanGames(currentRootPath);
                }
            }
        }

        private void ScanGames(string path)
        {
            try
            {
                allGamePaths = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(file =>
                    {
                        string lowFile = file.ToLower();
                        return (lowFile.EndsWith(".exe") || lowFile.EndsWith(".lnk") || lowFile.EndsWith(".url")) && !ignoreWords.Any(word => lowFile.Contains(word));
                    }).ToList();
                UpdateScrollHiddenWidth();
                RefreshList();
            }
            catch { }
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath));
                    if (config != null && Directory.Exists(config.LastFolderPath))
                    {
                        currentRootPath = config.LastFolderPath; ScanGames(currentRootPath);
                    }
                }
                catch { }
            }
        }
    }
    public class AppConfig { public string LastFolderPath { get; set; } = ""; }
}