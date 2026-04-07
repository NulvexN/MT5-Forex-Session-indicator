using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        // ── Tema Renkleri ─────────────────────────────────────────
        private readonly Color BgBase = Color.FromArgb(10, 12, 18);
        private readonly Color BgSurface = Color.FromArgb(16, 19, 28);
        private readonly Color BgCard = Color.FromArgb(20, 24, 36);
        private readonly Color BgElevated = Color.FromArgb(26, 31, 46);
        private readonly Color BgHover = Color.FromArgb(33, 39, 58);

        // Session renkleri
        private readonly Color ColLondon = Color.FromArgb(64, 149, 255);   // mavi
        private readonly Color ColNewYork = Color.FromArgb(255, 160, 50);   // turuncu
        private readonly Color ColAsia = Color.FromArgb(170, 100, 255);  // mor
        private readonly Color ColOverlap = Color.FromArgb(80, 220, 130);   // yeşil (London+NY overlap)

        private readonly Color TextPrimary = Color.FromArgb(220, 228, 248);
        private readonly Color TextSecondary = Color.FromArgb(100, 112, 145);
        private readonly Color TextMuted = Color.FromArgb(52, 60, 85);
        private readonly Color BorderDim = Color.FromArgb(28, 255, 255, 255);
        private readonly Color BorderMid = Color.FromArgb(45, 255, 255, 255);

        // ── State ─────────────────────────────────────────────────
        private string activeNav = "Session";
        private string selectedPair = "EURUSD";
        private string selectedTF = "M15";
        private bool showLondon = true;
        private bool showNewYork = true;
        private bool showAsia = true;
        private bool showOverlap = true;
        private bool showVolume = true;
        private double livePrice = 1.08312;
        private readonly Random rng = new Random();
        private System.Windows.Forms.Timer ticker;

        // ── Kontroller ────────────────────────────────────────────
        private Panel pnlTitle, pnlSidebar, pnlMain, pnlHeader, pnlContent, pnlStatus;
        private Panel pnlChart;
        private Label lblPrice, lblPriceChg, lblClock, lblStatusTxt;
        private Button btnApply;
        private CheckBox chkLondon, chkNewYork, chkAsia, chkOverlap, chkVolume;
        private Panel pnlLondonInfo, pnlNYInfo, pnlAsiaInfo, pnlOverlapInfo;
        private ListView lvSessions;

        // ── Session Verileri ──────────────────────────────────────
        private class SessionInfo
        {
            public string Name;
            public string Open;       // UTC
            public string Close;
            public string Status;
            public double AvgRange;
            public double TodayRange;
            public double Volume;
            public Color Col;
        }

        private readonly List<SessionInfo> sessions = new List<SessionInfo>
        {
            new SessionInfo { Name="London",   Open="08:00", Close="17:00", Status="ACTIVE", AvgRange=0.0082, TodayRange=0.0071, Volume=38.4, Col=Color.FromArgb(64,149,255)  },
            new SessionInfo { Name="New York",  Open="13:00", Close="22:00", Status="UPCOMING", AvgRange=0.0094, TodayRange=0.0000, Volume=41.2, Col=Color.FromArgb(255,160,50) },
            new SessionInfo { Name="Asia",      Open="00:00", Close="09:00", Status="CLOSED",   AvgRange=0.0041, TodayRange=0.0033, Volume=19.1, Col=Color.FromArgb(170,100,255)},
            new SessionInfo { Name="Overlap",   Open="13:00", Close="17:00", Status="UPCOMING", AvgRange=0.0115, TodayRange=0.0000, Volume=58.7, Col=Color.FromArgb(80,220,130) },
        };

        public Form1()
        {
            BuildUI();
            StartTicker();
        }

        // ─────────────────────────────────────────────────────────
        private void BuildUI()
        {
            this.Text = "AutoScripts · Session Highlighter MT5";
            this.Size = new Size(1140, 720);
            this.MinimumSize = new Size(920, 620);
            this.BackColor = BgBase;
            this.ForeColor = TextPrimary;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9f);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;

            BuildTitleBar();
            BuildSidebar();
            BuildMain();
            BuildStatusBar();
            this.Resize += (s, e) => Relayout();
            Relayout();
        }

        // ── TitleBar ──────────────────────────────────────────────
        private void BuildTitleBar()
        {
            pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = BgSurface };
            pnlTitle.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // ikon — saat şekli
                DrawClockIcon(g, 16, 12, 20);

                using (var f = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (var br = new SolidBrush(TextPrimary))
                    g.DrawString("SESSION HIGHLIGHTER", f, br, 44, 12);

                using (var f = new Font("Consolas", 8f))
                using (var br = new SolidBrush(TextMuted))
                    g.DrawString("MT5 · London  /  New York  /  Asia", f, br, 46, 28);

                // UTC saat pill
                int pw = pnlTitle.Width;
                DrawPill(g, pw - 260, 11, 145, "UTC  " + DateTime.UtcNow.ToString("HH:mm:ss"), ColLondon);

                // session badge'leri
                DrawSmallBadge(g, pw - 104, 14, "LDN", ColLondon);
                DrawSmallBadge(g, pw - 74, 14, "NY", ColNewYork);
                DrawSmallBadge(g, pw - 44, 14, "ASI", ColAsia);

                using (var pen = new Pen(BorderDim))
                    g.DrawLine(pen, 0, 43, pnlTitle.Width, 43);
            };

            pnlTitle.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(this.Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
                }
            };

            // win buttons
            var btnCl = MakeCircleBtn(Color.FromArgb(255, 85, 75));
            var btnMn = MakeCircleBtn(Color.FromArgb(255, 185, 40));
            var btnMx = MakeCircleBtn(Color.FromArgb(35, 195, 55));
            btnCl.Click += (s, e) => this.Close();
            btnMn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnMx.Click += (s, e) => this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            pnlTitle.Controls.AddRange(new Control[] { btnMn, btnMx, btnCl });

            void PosWin()
            {
                btnCl.Location = new Point(pnlTitle.Width - 26, 16);
                btnMx.Location = new Point(pnlTitle.Width - 46, 16);
                btnMn.Location = new Point(pnlTitle.Width - 66, 16);
            }
            PosWin();
            pnlTitle.Resize += (s, e) => { PosWin(); pnlTitle.Invalidate(); };
            this.Controls.Add(pnlTitle);
        }

        private void DrawClockIcon(Graphics g, int x, int y, int size)
        {
            using (var pen = new Pen(ColLondon, 1.5f))
                g.DrawEllipse(pen, x, y, size, size);
            int cx = x + size / 2, cy = y + size / 2;
            using (var pen = new Pen(TextPrimary, 1.5f))
            {
                g.DrawLine(pen, cx, cy, cx, cy - 6);       // 12 yönü
                g.DrawLine(pen, cx, cy, cx + 5, cy + 2);   // 3 yönü
            }
            using (var br = new SolidBrush(ColLondon))
                g.FillEllipse(br, cx - 2, cy - 2, 4, 4);
        }

        private void DrawPill(Graphics g, int x, int y, int w, string text, Color col)
        {
            var r = new Rectangle(x, y, w, 22);
            using (var br = new SolidBrush(Color.FromArgb(25, col.R, col.G, col.B)))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(70, col.R, col.G, col.B)))
                g.DrawRectangle(pen, r);
            using (var br = new SolidBrush(col))
                g.FillEllipse(br, x + 7, y + 8, 6, 6);
            using (var f = new Font("Consolas", 8f, FontStyle.Bold))
            using (var br = new SolidBrush(col))
                g.DrawString(text, f, br, x + 18, y + 5);
        }

        private void DrawSmallBadge(Graphics g, int x, int y, string text, Color col)
        {
            var r = new Rectangle(x, y, 28, 16);
            using (var br = new SolidBrush(Color.FromArgb(35, col.R, col.G, col.B)))
                g.FillRectangle(br, r);
            using (var f = new Font("Consolas", 7f, FontStyle.Bold))
            using (var br = new SolidBrush(col))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(text, f, br, r, sf);
            }
        }

        private Panel MakeCircleBtn(Color col)
        {
            var p = new Panel { Size = new Size(12, 12), BackColor = col, Cursor = Cursors.Hand };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(p.BackColor))
                    e.Graphics.FillEllipse(br, 0, 0, 11, 11);
            };
            p.MouseEnter += (s, e) => p.BackColor = ControlPaint.Light(col, 0.3f);
            p.MouseLeave += (s, e) => p.BackColor = col;
            return p;
        }

        // ── Sidebar ───────────────────────────────────────────────
        private void BuildSidebar()
        {
            pnlSidebar = new Panel { BackColor = BgSurface };
            pnlSidebar.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderDim))
                    e.Graphics.DrawLine(pen, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
            };

            var items = new[]
            {
                new[]{ "MENU",       "" },
                new[]{ "▤",          "Library" },
                new[]{ "⌂",          "Home" },
                new[]{ "◈",          "Dashboard" },
                new[]{ "SESSIONS",   "" },
                new[]{ "🕒",          "Session" },
                new[]{ "▦",          "Volume Profile" },
                new[]{ "◧",          "Smart Money" },
                new[]{ "◫",          "Supply/Demand" },
                new[]{ "SYSTEM",     "" },
                new[]{ "ℹ",          "About" },
                new[]{ "✉",          "Contact" },
            };

            int y = 12;
            foreach (var item in items)
            {
                string icon = item[0], label = item[1];
                if (label == "")
                {
                    pnlSidebar.Controls.Add(new Label
                    {
                        Text = icon,
                        Location = new Point(14, y),
                        Size = new Size(172, 18),
                        ForeColor = TextMuted,
                        Font = new Font("Consolas", 7.5f, FontStyle.Bold),
                        BackColor = Color.Transparent,
                    });
                    y += 22;
                }
                else
                {
                    string nav = label;
                    bool act = nav == activeNav;
                    var pnl = new Panel
                    {
                        Location = new Point(0, y),
                        Size = new Size(210, 34),
                        BackColor = act ? Color.FromArgb(22, 64, 149, 255) : Color.Transparent,
                        Cursor = Cursors.Hand,
                        Tag = nav,
                    };
                    pnl.Paint += (s, e) =>
                    {
                        if ((string)pnl.Tag == activeNav)
                        {
                            using (var br = new LinearGradientBrush(new Point(0, 0), new Point(3, 0), ColLondon, Color.Transparent))
                                e.Graphics.FillRectangle(br, 0, 0, 3, pnl.Height);
                        }
                    };
                    pnl.MouseEnter += (s, e) => { if ((string)pnl.Tag != activeNav) pnl.BackColor = BgHover; };
                    pnl.MouseLeave += (s, e) => pnl.BackColor = (string)pnl.Tag == activeNav ? Color.FromArgb(22, 64, 149, 255) : Color.Transparent;

                    var icL = new Label { Text = icon, Location = new Point(14, 9), Size = new Size(18, 16), ForeColor = act ? ColLondon : TextSecondary, Font = new Font("Segoe UI", 9f), BackColor = Color.Transparent, Tag = nav + "_ic" };
                    var txL = new Label { Text = nav, Location = new Point(36, 9), Size = new Size(130, 16), ForeColor = act ? ColLondon : TextSecondary, Font = new Font("Segoe UI", 9f), BackColor = Color.Transparent, Tag = nav + "_tx" };

                    pnl.Click += (s, e) => SetNav(nav);
                    icL.Click += (s, e) => SetNav(nav);
                    txL.Click += (s, e) => SetNav(nav);
                    pnl.Controls.AddRange(new Control[] { icL, txL });

                    if (nav == "Session")
                    {
                        var badge = new Label { Text = "LIVE", Location = new Point(152, 10), Size = new Size(30, 14), BackColor = Color.FromArgb(200, 20, 80), ForeColor = TextPrimary, Font = new Font("Consolas", 6.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
                        badge.Click += (s, e) => SetNav(nav);
                        pnl.Controls.Add(badge);
                    }
                    pnlSidebar.Controls.Add(pnl);
                    y += 36;
                }
            }

            // footer
            var footer = new Panel { Size = new Size(210, 60), BackColor = Color.Transparent };
            footer.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(BorderDim)) g.DrawLine(pen, 0, 0, 210, 0);

                // session aktif göstergeler
                int fx = 14, fy = 10;
                DrawSessionDot(g, fx, fy, "LDN", ColLondon, true);
                DrawSessionDot(g, fx + 60, fy, "NY", ColNewYork, false);
                DrawSessionDot(g, fx + 110, fy, "ASI", ColAsia, false);

                using (var f = new Font("Consolas", 7f))
                using (var br = new SolidBrush(TextMuted))
                    g.DrawString("MetaQuotes-Demo · 12345678", f, br, 14, 38);
            };
            pnlSidebar.Controls.Add(footer);
            pnlSidebar.Resize += (s, e) => footer.Location = new Point(0, pnlSidebar.Height - 62);
            this.Controls.Add(pnlSidebar);
        }

        private void DrawSessionDot(Graphics g, int x, int y, string label, Color col, bool active)
        {
            using (var br = new SolidBrush(active ? col : Color.FromArgb(40, col.R, col.G, col.B)))
                g.FillEllipse(br, x, y + 3, 7, 7);
            using (var f = new Font("Consolas", 7.5f, active ? FontStyle.Bold : FontStyle.Regular))
            using (var br = new SolidBrush(active ? col : TextMuted))
                g.DrawString(label, f, br, x + 10, y);
        }

        private void SetNav(string nav)
        {
            activeNav = nav;
            foreach (Control c in pnlSidebar.Controls)
            {
                if (!(c is Panel p) || !(p.Tag is string t)) continue;
                bool act = t == nav;
                p.BackColor = act ? Color.FromArgb(22, 64, 149, 255) : Color.Transparent;
                p.Invalidate();
                foreach (Control ch in p.Controls)
                {
                    if (!(ch is Label l) || l.Text == "LIVE") continue;
                    bool isThis = l.Tag is string lt && (lt == nav + "_ic" || lt == nav + "_tx");
                    l.ForeColor = isThis ? ColLondon : TextSecondary;
                }
            }
        }

        // ── Main ──────────────────────────────────────────────────
        private void BuildMain()
        {
            pnlMain = new Panel { BackColor = BgBase };
            BuildHeader();
            BuildContent();
            this.Controls.Add(pnlMain);
        }

        private void BuildHeader()
        {
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = BgSurface };
            pnlHeader.Paint += Header_Paint;

            // Pair seçici
            var pairs = new[] { "EURUSD", "GBPUSD", "USDJPY", "AUDUSD", "USDCHF" };
            var cmbPair = new ComboBox { Location = new Point(14, 16), Size = new Size(90, 22), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = BgElevated, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Consolas", 9f, FontStyle.Bold) };
            cmbPair.Items.AddRange(pairs);
            cmbPair.SelectedIndex = 0;
            cmbPair.SelectedIndexChanged += (s, e) => { selectedPair = cmbPair.SelectedItem.ToString(); pnlChart?.Invalidate(); };

            // TF butonları
            string[] tfs = { "M1", "M5", "M15", "M30", "H1", "H4" };
            int tx = 114;
            foreach (var tfStr in tfs)
            {
                string tf = tfStr;
                var b = new Button
                {
                    Text = tf,
                    Location = new Point(tx, 16),
                    Size = new Size(36, 22),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = tf == selectedTF ? Color.FromArgb(25, 64, 149, 255) : Color.Transparent,
                    ForeColor = tf == selectedTF ? ColLondon : TextSecondary,
                    Font = new Font("Consolas", 8f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                };
                b.FlatAppearance.BorderColor = tf == selectedTF ? Color.FromArgb(80, 64, 149, 255) : Color.FromArgb(20, 255, 255, 255);
                b.FlatAppearance.BorderSize = 1;
                b.Click += (s, e) =>
                {
                    selectedTF = tf;
                    foreach (Control c in pnlHeader.Controls)
                    {
                        if (!(c is Button btn) || Array.IndexOf(tfs, btn.Text) < 0) continue;
                        bool sel = btn.Text == selectedTF;
                        btn.BackColor = sel ? Color.FromArgb(25, 64, 149, 255) : Color.Transparent;
                        btn.ForeColor = sel ? ColLondon : TextSecondary;
                        btn.FlatAppearance.BorderColor = sel ? Color.FromArgb(80, 64, 149, 255) : Color.FromArgb(20, 255, 255, 255);
                    }
                    pnlChart?.Invalidate();
                };
                pnlHeader.Controls.Add(b);
                tx += 40;
            }

            lblPrice = new Label { Text = "1.08312", Location = new Point(368, 14), Size = new Size(95, 24), ForeColor = ColLondon, Font = new Font("Consolas", 14f, FontStyle.Bold), BackColor = Color.Transparent };
            lblPriceChg = new Label { Text = "▲ +0.00021", Location = new Point(466, 19), Size = new Size(100, 16), ForeColor = ColLondon, Font = new Font("Consolas", 9f), BackColor = Color.Transparent };

            btnApply = new Button
            {
                Text = "▶  Apply to Chart",
                FlatStyle = FlatStyle.Flat,
                BackColor = ColLondon,
                ForeColor = BgBase,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Size = new Size(130, 26),
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;

            pnlHeader.Controls.AddRange(new Control[] { cmbPair, lblPrice, lblPriceChg, btnApply });

            void PosH()
            {
                btnApply.Location = new Point(pnlHeader.Width - 146, 14);
            }
            PosH();
            pnlHeader.Resize += (s, e) => PosH();
            pnlMain.Controls.Add(pnlHeader);
        }

        private void Header_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // aktif session bar (üst)
            using (var br = new LinearGradientBrush(new Point(0, 0), new Point(pnlHeader.Width, 0),
                Color.FromArgb(60, 64, 149, 255), Color.Transparent))
                g.FillRectangle(br, 0, 0, pnlHeader.Width, 3);

            using (var pen = new Pen(BorderDim))
                g.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        }

        // ── Content ───────────────────────────────────────────────
        private void BuildContent()
        {
            pnlContent = new Panel { BackColor = BgBase, AutoScroll = true };

            pnlChart = new Panel { BackColor = BgBase };
            pnlChart.Paint += Chart_Paint;

            pnlLondonInfo = BuildSessionCard("London", "08:00 – 17:00 UTC", ColLondon, "ACTIVE", "0.0082", "38.4%");
            pnlNYInfo = BuildSessionCard("New York", "13:00 – 22:00 UTC", ColNewYork, "UPCOMING", "0.0094", "41.2%");
            pnlAsiaInfo = BuildSessionCard("Asia", "00:00 – 09:00 UTC", ColAsia, "CLOSED", "0.0041", "19.1%");
            pnlOverlapInfo = BuildSessionCard("Overlap", "13:00 – 17:00 UTC", ColOverlap, "UPCOMING", "0.0115", "58.7%");

            BuildCheckboxPanel();
            lvSessions = BuildSessionTable();

            pnlContent.Controls.AddRange(new Control[]
            {
                pnlChart, pnlLondonInfo, pnlNYInfo, pnlAsiaInfo, pnlOverlapInfo, lvSessions
            });
            pnlMain.Controls.Add(pnlContent);
            pnlContent.Resize += (s, e) => LayoutContent();
            LayoutContent();
        }

        private Panel chkPanel;

        private void BuildCheckboxPanel()
        {
            chkPanel = new Panel { BackColor = BgCard };
            chkPanel.Paint += (s, e) => PaintCard(e.Graphics, chkPanel, "Visibility");

            chkLondon = MakeChk("London", ColLondon, true, 28);
            chkNewYork = MakeChk("New York", ColNewYork, true, 54);
            chkAsia = MakeChk("Asia", ColAsia, true, 80);
            chkOverlap = MakeChk("Overlap", ColOverlap, true, 106);
            chkVolume = MakeChk("Volume", Color.FromArgb(160, 180, 220), true, 132);

            chkLondon.CheckedChanged += (s, e) => { showLondon = chkLondon.Checked; pnlChart?.Invalidate(); };
            chkNewYork.CheckedChanged += (s, e) => { showNewYork = chkNewYork.Checked; pnlChart?.Invalidate(); };
            chkAsia.CheckedChanged += (s, e) => { showAsia = chkAsia.Checked; pnlChart?.Invalidate(); };
            chkOverlap.CheckedChanged += (s, e) => { showOverlap = chkOverlap.Checked; pnlChart?.Invalidate(); };
            chkVolume.CheckedChanged += (s, e) => { showVolume = chkVolume.Checked; pnlChart?.Invalidate(); };

            chkPanel.Controls.AddRange(new Control[] { chkLondon, chkNewYork, chkAsia, chkOverlap, chkVolume });
            pnlContent.Controls.Add(chkPanel);
        }

        private CheckBox MakeChk(string text, Color col, bool chk, int y)
        {
            var c = new CheckBox
            {
                Text = text,
                Checked = chk,
                Location = new Point(10, y),
                ForeColor = col,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent,
                AutoSize = true,
            };
            return c;
        }

        private void LayoutContent()
        {
            if (pnlContent == null) return;
            int W = Math.Max(pnlContent.ClientSize.Width - 24, 200);
            int pad = 10;

            // Chart
            pnlChart.Location = new Point(12, 12);
            pnlChart.Size = new Size(W, 240);

            // 4 session kartları
            int cw = (W - pad * 3) / 4;
            int row2Y = pnlChart.Bottom + pad;
            pnlLondonInfo.Location = new Point(12, row2Y);
            pnlLondonInfo.Size = new Size(cw, 150);
            pnlNYInfo.Location = new Point(12 + (cw + pad), row2Y);
            pnlNYInfo.Size = new Size(cw, 150);
            pnlAsiaInfo.Location = new Point(12 + (cw + pad) * 2, row2Y);
            pnlAsiaInfo.Size = new Size(cw, 150);
            pnlOverlapInfo.Location = new Point(12 + (cw + pad) * 3, row2Y);
            pnlOverlapInfo.Size = new Size(cw, 150);

            // Checkbox panel + tablo
            int row3Y = pnlLondonInfo.Bottom + pad;
            chkPanel.Location = new Point(12, row3Y);
            chkPanel.Size = new Size(170, 165);

            lvSessions.Location = new Point(chkPanel.Right + pad, row3Y);
            lvSessions.Size = new Size(W - 170 - pad, 165);
        }

        // ── Session Kartı ─────────────────────────────────────────
        private Panel BuildSessionCard(string name, string hours, Color col, string status, string avgRange, string vol)
        {
            var p = new Panel { BackColor = BgCard };
            p.Paint += (s, e) => DrawSessionCard(e.Graphics, p, name, hours, col, status, avgRange, vol);
            return p;
        }

        private void DrawSessionCard(Graphics g, Panel p, string name, string hours, Color col, string status, string avgRange, string vol)
        {
            int W = p.Width, H = p.Height;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // border
            using (var pen = new Pen(Color.FromArgb(35, col.R, col.G, col.B)))
                g.DrawRectangle(pen, 0, 0, W - 1, H - 1);

            // üst renk şeridi
            using (var br = new LinearGradientBrush(new Point(0, 0), new Point(W, 0),
                Color.FromArgb(50, col.R, col.G, col.B), Color.Transparent))
                g.FillRectangle(br, 0, 0, W, 4);

            // header arka plan
            using (var br = new SolidBrush(Color.FromArgb(18, col.R, col.G, col.B)))
                g.FillRectangle(br, 0, 4, W, 30);

            // ikon dot
            using (var br = new SolidBrush(col))
                g.FillEllipse(br, 10, 14, 8, 8);

            // isim
            using (var f = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (var br = new SolidBrush(col))
                g.DrawString(name, f, br, 24, 10);

            // status badge
            Color stCol = status == "ACTIVE" ? ColOverlap : status == "UPCOMING" ? ColNewYork : TextMuted;
            var stRect = new Rectangle(W - 68, 12, 58, 14);
            using (var br = new SolidBrush(Color.FromArgb(25, stCol.R, stCol.G, stCol.B)))
                g.FillRectangle(br, stRect);
            using (var f = new Font("Consolas", 7f, FontStyle.Bold))
            using (var br = new SolidBrush(stCol))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(status, f, br, stRect, sf);
            }

            // separator
            using (var pen = new Pen(Color.FromArgb(25, 255, 255, 255)))
                g.DrawLine(pen, 0, 34, W, 34);

            // içerik satırları
            int y = 42;
            DrawCardRow(g, W, y, "Hours", hours, TextSecondary, TextPrimary);
            DrawCardRow(g, W, y + 22, "Avg Rng", avgRange, TextSecondary, col);
            DrawCardRow(g, W, y + 44, "Volume", vol, TextSecondary, TextPrimary);

            // progress bar
            if (status == "ACTIVE")
            {
                float pct = 0.62f; // London'da %62 tamamlandı
                int barY = H - 18;
                using (var br = new SolidBrush(BgHover))
                    g.FillRectangle(br, 10, barY, W - 20, 6);
                using (var br = new LinearGradientBrush(
                    new Point(10, barY), new Point(10 + (int)((W - 20) * pct), barY),
                    col, Color.FromArgb(120, col.R, col.G, col.B)))
                    g.FillRectangle(br, 10, barY, (W - 20) * pct, 6);
                using (var f = new Font("Consolas", 7f))
                using (var br = new SolidBrush(TextMuted))
                    g.DrawString("62% elapsed", f, br, 10, barY - 12);
            }
        }

        private void DrawCardRow(Graphics g, int W, int y, string label, string val, Color lc, Color vc)
        {
            using (var f = new Font("Segoe UI", 8f))
            using (var br = new SolidBrush(lc))
                g.DrawString(label, f, br, 10, y);
            using (var f = new Font("Consolas", 8.5f, FontStyle.Bold))
            using (var br = new SolidBrush(vc))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(val, f, br, new RectangleF(0, y, W - 10, 16), sf);
            }
        }

        // ── Chart Paint ───────────────────────────────────────────
        private void Chart_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int W = pnlChart.Width, H = pnlChart.Height;
            if (W < 20 || H < 20) return;

            g.Clear(Color.FromArgb(10, 12, 18));

            // grid
            using (var pen = new Pen(Color.FromArgb(10, 255, 255, 255), 0.5f))
            {
                for (int i = 1; i < 10; i++) g.DrawLine(pen, i * W / 10, 0, i * W / 10, H);
                for (int i = 1; i < 6; i++) g.DrawLine(pen, 0, i * H / 6, W, i * H / 6);
            }

            double pMin = 1.0790, pMax = 1.0870;
            double pRng = pMax - pMin;
            int cL = 8, cR = W - 8;
            int nBar = 80;
            float bW = (float)(cR - cL) / nBar;

            // ── Session bölgeleri ────────────────────────────────
            // Her bar = 15 dakika (M15 varsayım)
            // Asia:    bar 0–36  (00:00–09:00)
            // London:  bar 32–68 (08:00–17:00)
            // NY:      bar 52–80 (13:00–22:00)
            // Overlap: bar 52–68 (13:00–17:00)

            void DrawSession(int barStart, int barEnd, Color col, bool show)
            {
                if (!show) return;
                float x1 = cL + barStart * bW;
                float x2 = cL + barEnd * bW;
                using (var br = new SolidBrush(Color.FromArgb(22, col.R, col.G, col.B)))
                    g.FillRectangle(br, x1, 0, x2 - x1, H - (showVolume ? 40 : 0));
                using (var pen = new Pen(Color.FromArgb(55, col.R, col.G, col.B), 1f) { DashStyle = DashStyle.Dot })
                {
                    g.DrawLine(pen, x1, 0, x1, H);
                    g.DrawLine(pen, x2, 0, x2, H);
                }
                // label
                using (var f = new Font("Consolas", 7.5f, FontStyle.Bold))
                using (var br = new SolidBrush(Color.FromArgb(140, col.R, col.G, col.B)))
                    g.DrawString(col == ColLondon ? "LDN" : col == ColNewYork ? "NY" : col == ColAsia ? "ASIA" : "OVL",
                        f, br, x1 + 4, 6);
            }

            DrawSession(0, 36, ColAsia, showAsia);
            DrawSession(32, 68, ColLondon, showLondon);
            DrawSession(52, 80, ColNewYork, showNewYork);
            // overlap
            if (showOverlap && showLondon && showNewYork)
            {
                float ox1 = cL + 52 * bW, ox2 = cL + 68 * bW;
                using (var br = new SolidBrush(Color.FromArgb(28, ColOverlap.R, ColOverlap.G, ColOverlap.B)))
                    g.FillRectangle(br, ox1, 0, ox2 - ox1, H - (showVolume ? 40 : 0));
                using (var f = new Font("Consolas", 7f, FontStyle.Bold))
                using (var br = new SolidBrush(Color.FromArgb(140, ColOverlap.R, ColOverlap.G, ColOverlap.B)))
                    g.DrawString("OVL", f, br, ox1 + 4, 18);
            }

            // ── Mumlar ────────────────────────────────────────────
            double p = 1.0820;
            for (int i = 0; i < nBar; i++)
            {
                double o = p, c = p + (rng.NextDouble() - 0.488) * 0.0005;
                double h = Math.Max(o, c) + rng.NextDouble() * 0.0002;
                double l = Math.Min(o, c) - rng.NextDouble() * 0.0002;
                p = c;

                float cx = cL + i * bW + bW / 2f;
                float yH = PY(h, pMin, pRng, H - (showVolume ? 40 : 0));
                float yL = PY(l, pMin, pRng, H - (showVolume ? 40 : 0));
                float yO = PY(o, pMin, pRng, H - (showVolume ? 40 : 0));
                float yC = PY(c, pMin, pRng, H - (showVolume ? 40 : 0));
                bool bull = c >= o;

                // session rengine göre mum rengi
                Color candleCol;
                if (i >= 52 && i < 68 && showLondon && showNewYork) candleCol = ColOverlap;
                else if (i >= 32 && i < 68 && showLondon) candleCol = ColLondon;
                else if (i >= 52 && i < 80 && showNewYork) candleCol = ColNewYork;
                else if (i < 36 && showAsia) candleCol = ColAsia;
                else candleCol = bull ? Color.FromArgb(80, 200, 160) : Color.FromArgb(200, 80, 110);

                using (var pen = new Pen(Color.FromArgb(160, candleCol.R, candleCol.G, candleCol.B), 0.8f))
                    g.DrawLine(pen, cx, yH, cx, yL);
                float top = Math.Min(yO, yC), bh = Math.Max(Math.Abs(yO - yC), 1.5f);
                using (var br = new SolidBrush(Color.FromArgb(bull ? 180 : 140, candleCol.R, candleCol.G, candleCol.B)))
                    g.FillRectangle(br, cx - bW * 0.38f, top, bW * 0.76f, bh);
            }

            // ── Volume barları ────────────────────────────────────
            if (showVolume)
            {
                int volY = H - 38;
                using (var pen = new Pen(Color.FromArgb(20, 255, 255, 255)))
                    g.DrawLine(pen, cL, volY, cR, volY);

                p = 1.0820;
                for (int i = 0; i < nBar; i++)
                {
                    double o = p, c = p + (rng.NextDouble() - 0.488) * 0.0005;
                    p = c;
                    float vol = (float)(rng.NextDouble() * 0.7 + 0.3);
                    float cx = cL + i * bW;
                    float bh = vol * 32;
                    bool bull = c >= o;

                    Color vc;
                    if (i >= 52 && i < 68) vc = ColOverlap;
                    else if (i >= 32 && i < 68) vc = ColLondon;
                    else if (i >= 52) vc = ColNewYork;
                    else vc = ColAsia;

                    using (var br = new SolidBrush(Color.FromArgb(bull ? 110 : 80, vc.R, vc.G, vc.B)))
                        g.FillRectangle(br, cx + 1, H - bh, bW - 2, bh);
                }
                using (var f = new Font("Consolas", 7f))
                using (var br = new SolidBrush(TextMuted))
                    g.DrawString("VOL", f, br, cL + 3, volY + 3);
            }

            // ── Canlı fiyat çizgisi ───────────────────────────────
            float curY = PY(livePrice, pMin, pRng, H - (showVolume ? 40 : 0));
            using (var pen = new Pen(Color.FromArgb(70, 255, 255, 255), 0.7f) { DashStyle = DashStyle.Dot })
                g.DrawLine(pen, cL, curY, cR, curY);

            // fiyat etiketi sağda
            var priceTag = new Rectangle(cR - 60, (int)curY - 9, 60, 18);
            using (var br = new SolidBrush(Color.FromArgb(180, 64, 149, 255)))
                g.FillRectangle(br, priceTag);
            using (var f = new Font("Consolas", 8f, FontStyle.Bold))
            using (var br = new SolidBrush(Color.White))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(livePrice.ToString("F5"), f, br, priceTag, sf);
            }

            // pair + TF etiketi
            using (var f = new Font("Consolas", 9f, FontStyle.Bold))
            using (var br = new SolidBrush(TextPrimary))
                g.DrawString($"{selectedPair}  ·  {selectedTF}", f, br, cL + 4, 6);
        }

        private static float PY(double price, double min, double range, int H)
            => (float)(H - ((price - min) / range) * H);

        // ── Session Table ─────────────────────────────────────────
        private ListView BuildSessionTable()
        {
            var lv = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 8.5f),
                OwnerDraw = true,
            };
            lv.Columns.Add("Session", 90);
            lv.Columns.Add("Open", 65);
            lv.Columns.Add("Close", 65);
            lv.Columns.Add("Status", 80);
            lv.Columns.Add("Avg Range", 80);
            lv.Columns.Add("Volume", 70);
            lv.Columns.Add("Today Rng", 80);

            foreach (var s in sessions)
            {
                var it = new ListViewItem(s.Name) { Tag = s };
                it.SubItems.Add(s.Open);
                it.SubItems.Add(s.Close);
                it.SubItems.Add(s.Status);
                it.SubItems.Add(s.AvgRange.ToString("F4"));
                it.SubItems.Add(s.Volume.ToString("F1") + "%");
                it.SubItems.Add(s.TodayRange > 0 ? s.TodayRange.ToString("F4") : "—");
                lv.Items.Add(it);
            }

            lv.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(BgSurface), e.Bounds);
                using (var pen = new Pen(BorderDim))
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                using (var f = new Font("Consolas", 7.5f, FontStyle.Bold))
                using (var br = new SolidBrush(TextMuted))
                    e.Graphics.DrawString(e.Header.Text.ToUpper(), f, br, e.Bounds.Left + 6, e.Bounds.Top + 5);
            };

            lv.DrawItem += (s, e) => e.DrawBackground();

            lv.DrawSubItem += (s, e) =>
            {
                if (!(e.Item.Tag is SessionInfo si)) return;
                var g = e.Graphics; var rc = e.Bounds;

                Color fg = TextPrimary;
                if (e.ColumnIndex == 0) fg = si.Col;
                if (e.ColumnIndex == 3)
                    fg = si.Status == "ACTIVE" ? ColOverlap : si.Status == "UPCOMING" ? ColNewYork : TextMuted;
                if (e.ColumnIndex == 4) fg = si.Col;

                // session name — renkli dot
                if (e.ColumnIndex == 0)
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var br = new SolidBrush(si.Col))
                        g.FillEllipse(br, rc.X + 4, rc.Y + 6, 6, 6);
                    using (var f = new Font("Consolas", 8.5f, FontStyle.Bold))
                    using (var br = new SolidBrush(fg))
                        g.DrawString(e.SubItem.Text, f, br, rc.X + 14, rc.Y + 4);
                }
                else
                {
                    using (var f = new Font("Consolas", 8.5f))
                    using (var br = new SolidBrush(fg))
                        g.DrawString(e.SubItem.Text, f, br, rc.X + 6, rc.Y + 4);
                }
                using (var pen = new Pen(Color.FromArgb(8, 255, 255, 255)))
                    g.DrawLine(pen, rc.Left, rc.Bottom - 1, rc.Right, rc.Bottom - 1);
            };

            return lv;
        }

        // ── Card Paint ────────────────────────────────────────────
        private void PaintCard(Graphics g, Panel p, string title)
        {
            using (var pen = new Pen(BorderDim))
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            using (var br = new SolidBrush(Color.FromArgb(15, 255, 255, 255)))
                g.FillRectangle(br, 0, 0, p.Width, 24);
            using (var pen = new Pen(BorderDim))
                g.DrawLine(pen, 0, 24, p.Width, 24);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var br = new SolidBrush(ColLondon))
                g.FillEllipse(br, 8, 10, 4, 4);
            using (var f = new Font("Consolas", 7.5f, FontStyle.Bold))
            using (var br = new SolidBrush(TextMuted))
                g.DrawString(title.ToUpper(), f, br, 16, 7);
        }

        // ── Status Bar ────────────────────────────────────────────
        private void BuildStatusBar()
        {
            pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = BgSurface };
            pnlStatus.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var pen = new Pen(BorderDim)) g.DrawLine(pen, 0, 0, pnlStatus.Width, 0);

                // session renk göstergesi (üst şerit)
                int segW = pnlStatus.Width / 4;
                using (var br = new SolidBrush(Color.FromArgb(50, ColAsia.R, ColAsia.G, ColAsia.B)))
                    g.FillRectangle(br, 0, 0, segW, 2);
                using (var br = new SolidBrush(Color.FromArgb(80, ColLondon.R, ColLondon.G, ColLondon.B)))
                    g.FillRectangle(br, segW, 0, segW, 2);
                using (var br = new SolidBrush(Color.FromArgb(80, ColOverlap.R, ColOverlap.G, ColOverlap.B)))
                    g.FillRectangle(br, segW * 2, 0, segW, 2);
                using (var br = new SolidBrush(Color.FromArgb(60, ColNewYork.R, ColNewYork.G, ColNewYork.B)))
                    g.FillRectangle(br, segW * 3, 0, segW, 2);

                using (var f = new Font("Consolas", 8f))
                using (var br = new SolidBrush(TextMuted))
                {
                    g.DrawString("EURUSD · Spread: 0.7", f, br, 200, 7);
                    g.DrawString("Session v1.0.0", f, br, pnlStatus.Width - 110, 7);
                }
            };

            lblStatusTxt = new Label { Text = "● London Active", Location = new Point(14, 6), Size = new Size(130, 14), ForeColor = ColLondon, Font = new Font("Consolas", 8f), BackColor = Color.Transparent };
            lblClock = new Label { Location = new Point(380, 6), Size = new Size(200, 14), ForeColor = TextMuted, Font = new Font("Consolas", 8f), BackColor = Color.Transparent };
            pnlStatus.Controls.AddRange(new Control[] { lblStatusTxt, lblClock });
            this.Controls.Add(pnlStatus);
        }

        // ── Relayout ──────────────────────────────────────────────
        private void Relayout()
        {
            int top = pnlTitle?.Height ?? 44;
            int bot = pnlStatus?.Height ?? 26;

            if (pnlSidebar != null)
            {
                pnlSidebar.Location = new Point(0, top);
                pnlSidebar.Size = new Size(210, this.ClientSize.Height - top - bot);
            }
            if (pnlMain != null)
            {
                pnlMain.Location = new Point(210, top);
                pnlMain.Size = new Size(this.ClientSize.Width - 210, this.ClientSize.Height - top - bot);
            }
            if (pnlContent != null && pnlHeader != null)
            {
                pnlContent.Location = new Point(0, pnlHeader.Height);
                pnlContent.Size = new Size(pnlMain.Width, pnlMain.Height - pnlHeader.Height);
            }
            LayoutContent();
        }

        // ── Ticker ────────────────────────────────────────────────
        private void StartTicker()
        {
            ticker = new System.Windows.Forms.Timer { Interval = 1600 };
            ticker.Tick += (s, e) =>
            {
                livePrice += (rng.NextDouble() - 0.49) * 0.00007;
                double chg = livePrice - 1.08291;
                if (lblPrice != null) lblPrice.Text = livePrice.ToString("F5");
                if (lblPriceChg != null)
                {
                    lblPriceChg.Text = (chg >= 0 ? "▲ +" : "▼ ") + chg.ToString("F5");
                    lblPriceChg.ForeColor = chg >= 0 ? ColLondon : Color.FromArgb(255, 100, 120);
                }
                if (lblClock != null) lblClock.Text = "UTC " + DateTime.UtcNow.ToString("HH:mm:ss") + "  ·  " + DateTime.Now.ToString("dd.MM.yyyy");
                pnlTitle?.Invalidate();
                pnlChart?.Invalidate();
            };
            ticker.Start();
        }

        // ── Apply ─────────────────────────────────────────────────
        private async void BtnApply_Click(object sender, EventArgs e)
        {
            btnApply.Text = "◌ Applying...";
            btnApply.BackColor = Color.FromArgb(0, 160, 130);
            btnApply.Enabled = false;
            await System.Threading.Tasks.Task.Delay(1200);
            btnApply.Text = "✓  Applied";
            if (lblStatusTxt != null) lblStatusTxt.Text = "● Sessions applied";
            await System.Threading.Tasks.Task.Delay(2000);
            btnApply.Text = "▶  Apply to Chart";
            btnApply.BackColor = ColLondon;
            btnApply.Enabled = true;
            if (lblStatusTxt != null) lblStatusTxt.Text = "● London Active";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ticker?.Stop();
            base.OnFormClosed(e);
        }
    }

    // ── Native ────────────────────────────────────────────────────
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    }
}