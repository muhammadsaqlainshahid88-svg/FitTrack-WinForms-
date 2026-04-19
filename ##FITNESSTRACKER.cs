

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

// ============================================================
//   FITNESS TRACKER — Upgraded Complete Single-File C# App
//   Features: Multi-activity, Daily Progress, History, JSON
// ============================================================

namespace FitnessTracker
{
    // ──────────────────────────────────────────────────────────
    //  DATA MODELS
    // ──────────────────────────────────────────────────────────
    class UserAccount
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public double CalorieGoal { get; set; }
        public int FailedAttempts { get; set; }
        public bool IsLocked { get; set; }
        public List<ActivityEntry> Activities { get; set; } = new List<ActivityEntry>();
    }

    class ActivityEntry
    {
        public string ActivityName { get; set; }
        public double Metric1 { get; set; }
        public double Metric2 { get; set; }
        public double Metric3 { get; set; }
        public double CaloriesBurned { get; set; }
        public DateTime Date { get; set; }
    }

    // ──────────────────────────────────────────────────────────
    //  PROGRAM ENTRY POINT
    // ──────────────────────────────────────────────────────────
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            UserStore.Load();
            Application.Run(new LoginForm());
        }
    }

    // ──────────────────────────────────────────────────────────
    //  THEME
    // ──────────────────────────────────────────────────────────
    static class Theme
    {
        public static Color Background  = Color.FromArgb(10, 14, 26);
        public static Color Surface     = Color.FromArgb(18, 24, 42);
        public static Color Card        = Color.FromArgb(26, 35, 60);
        public static Color Accent      = Color.FromArgb(0, 212, 170);
        public static Color AccentDark  = Color.FromArgb(0, 160, 130);
        public static Color TextPrimary = Color.FromArgb(230, 240, 255);
        public static Color TextSec     = Color.FromArgb(130, 150, 190);
        public static Color Danger      = Color.FromArgb(255, 80, 100);
        public static Color Warning     = Color.FromArgb(255, 190, 50);
        public static Color Success     = Color.FromArgb(50, 220, 140);
        public static Color Purple      = Color.FromArgb(160, 100, 255);

        public static Font FontTitle   = new Font("Segoe UI", 22f, FontStyle.Bold);
        public static Font FontHeading = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static Font FontBody    = new Font("Segoe UI", 10f, FontStyle.Regular);
        public static Font FontSmall   = new Font("Segoe UI", 8.5f, FontStyle.Regular);

        public static Button StyledButton(string text, Color? bg = null)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg ?? Accent,
                ForeColor = (bg.HasValue && bg.Value == Danger) ? Color.White : Background,
                Font = FontHeading,
                Cursor = Cursors.Hand,
                Height = 44,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = bg.HasValue ? ControlPaint.Light(bg.Value, 0.1f) : AccentDark;
            return btn;
        }

        public static TextBox StyledTextBox(bool password = false)
        {
            var tb = new TextBox
            {
                BackColor = Card,
                ForeColor = TextPrimary,
                Font = FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                Height = 30,
            };
            if (password) tb.PasswordChar = '●';
            return tb;
        }

        public static Label StyledLabel(string text, Font font = null, Color? color = null)
        {
            return new Label
            {
                Text = text,
                ForeColor = color ?? TextPrimary,
                Font = font ?? FontBody,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
        }

        public static Label StyledLabel(string text, int left, int top, Font font = null, Color? color = null)
        {
            var lbl = StyledLabel(text, font, color);
            lbl.Left = left; lbl.Top = top;
            return lbl;
        }
    }

    // ──────────────────────────────────────────────────────────
    //  USER STORE  (JSON persistence)
    // ──────────────────────────────────────────────────────────
    static class UserStore
    {
        private static readonly string FilePath = "users.json";
        public static Dictionary<string, UserAccount> Users = new Dictionary<string, UserAccount>();
        public static UserAccount CurrentUser = null;

        public static void Load()
        {
            if (!File.Exists(FilePath)) { Users = new Dictionary<string, UserAccount>(); return; }
            try
            {
                string json = File.ReadAllText(FilePath);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Users = JsonSerializer.Deserialize<Dictionary<string, UserAccount>>(json, opts)
                        ?? new Dictionary<string, UserAccount>();
            }
            catch { Users = new Dictionary<string, UserAccount>(); }
        }

        public static void Save()
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(Users, opts));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Warning: Could not save data.\n{ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FORM 1 — LOGIN
    // ══════════════════════════════════════════════════════════
    class LoginForm : Form
    {
        TextBox tbUser, tbPass;
        Label lblError;

        public LoginForm()
        {
            Text = "FitTrack — Login";
            Size = new Size(480, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BuildUI();
        }

        void BuildUI()
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.Transparent };
            pnlTop.Controls.Add(new Label { Text = "💪 FitTrack", ForeColor = Theme.Accent, Font = Theme.FontTitle, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, BackColor = Color.Transparent });
            Controls.Add(pnlTop);

            var card = new Panel { Width = 360, Height = 340, Left = 60, Top = 150, BackColor = Theme.Surface };
            Controls.Add(card);

            int y = 30;
            card.Controls.Add(Theme.StyledLabel("Username", 30, y, Theme.FontHeading, Theme.TextSec)); y += 28;
            tbUser = Theme.StyledTextBox(); tbUser.Left = 30; tbUser.Top = y; tbUser.Width = 300; card.Controls.Add(tbUser); y += 44;
            card.Controls.Add(Theme.StyledLabel("Password", 30, y, Theme.FontHeading, Theme.TextSec)); y += 28;
            tbPass = Theme.StyledTextBox(true); tbPass.Left = 30; tbPass.Top = y; tbPass.Width = 300; card.Controls.Add(tbPass); y += 50;

            lblError = new Label { Left = 30, Top = y, Width = 300, Height = 20, ForeColor = Theme.Danger, Font = Theme.FontSmall, BackColor = Color.Transparent };
            card.Controls.Add(lblError); y += 28;

            var btnLogin = Theme.StyledButton("LOGIN"); btnLogin.Left = 30; btnLogin.Top = y; btnLogin.Width = 300; card.Controls.Add(btnLogin);
            btnLogin.Click += BtnLogin_Click; y += 54;

            var lnk = new LinkLabel { Text = "Don't have an account? Register", Left = 50, Top = y, Width = 260, ForeColor = Theme.Accent, Font = Theme.FontSmall, BackColor = Color.Transparent, LinkColor = Theme.Accent };
            card.Controls.Add(lnk);
            lnk.LinkClicked += (s, e) => new RegisterForm().ShowDialog(this);
        }

        void BtnLogin_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            string user = tbUser.Text.Trim(), pass = tbPass.Text;
            if (!UserStore.Users.ContainsKey(user)) { lblError.Text = "⚠ Username not found."; return; }
            var acc = UserStore.Users[user];
            if (acc.IsLocked) { lblError.Text = "🔒 Account locked."; return; }
            if (acc.Password != pass)
            {
                acc.FailedAttempts++;
                if (acc.FailedAttempts >= 3) { acc.IsLocked = true; lblError.Text = "🔒 Locked after 3 failed attempts."; }
                else lblError.Text = $"✗ Wrong password. Attempt {acc.FailedAttempts}/3.";
                UserStore.Save(); return;
            }
            acc.FailedAttempts = 0;
            UserStore.CurrentUser = acc;
            new DashboardForm().Show();
            Hide();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FORM 2 — REGISTER
    // ══════════════════════════════════════════════════════════
    class RegisterForm : Form
    {
        TextBox tbUser, tbPass, tbConfirm;
        Label lblError, lblSuccess;

        public RegisterForm()
        {
            Text = "FitTrack — Register";
            Size = new Size(480, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BuildUI();
        }

        void BuildUI()
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.Transparent };
            pnlTop.Controls.Add(new Label { Text = "Create Account", ForeColor = Theme.Accent, Font = Theme.FontTitle, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, BackColor = Color.Transparent });
            Controls.Add(pnlTop);

            var card = new Panel { Width = 380, Height = 390, Left = 50, Top = 110, BackColor = Theme.Surface };
            Controls.Add(card);

            int y = 24;
            void Row(string lbl, ref TextBox tb, bool pw = false)
            {
                card.Controls.Add(Theme.StyledLabel(lbl, 30, y, Theme.FontHeading, Theme.TextSec));
                y += 26; tb = Theme.StyledTextBox(pw); tb.Left = 30; tb.Top = y; tb.Width = 320; card.Controls.Add(tb); y += 44;
            }
            Row("Username (letters & numbers only)", ref tbUser);
            Row("Password (12+ chars, upper & lowercase)", ref tbPass, true);
            Row("Confirm Password", ref tbConfirm, true);

            lblError = new Label { Left = 30, Top = y, Width = 320, Height = 20, ForeColor = Theme.Danger, Font = Theme.FontSmall, BackColor = Color.Transparent };
            card.Controls.Add(lblError); y += 22;
            lblSuccess = new Label { Left = 30, Top = y, Width = 320, Height = 20, ForeColor = Theme.Success, Font = Theme.FontSmall, BackColor = Color.Transparent };
            card.Controls.Add(lblSuccess); y += 28;

            var btn = Theme.StyledButton("REGISTER"); btn.Left = 30; btn.Top = y; btn.Width = 320; card.Controls.Add(btn);
            btn.Click += Btn_Click;
        }

        void Btn_Click(object sender, EventArgs e)
        {
            lblError.Text = ""; lblSuccess.Text = "";
            string user = tbUser.Text.Trim(), pass = tbPass.Text, conf = tbConfirm.Text;
            if (!Regex.IsMatch(user, @"^[a-zA-Z0-9]+$")) { lblError.Text = "✗ Letters and numbers only."; return; }
            if (user.Length < 3) { lblError.Text = "✗ Username min 3 characters."; return; }
            if (UserStore.Users.ContainsKey(user)) { lblError.Text = "✗ Username already exists."; return; }
            if (pass.Length < 12) { lblError.Text = "✗ Password min 12 characters."; return; }
            if (!Regex.IsMatch(pass, @"[A-Z]")) { lblError.Text = "✗ Need at least 1 uppercase letter."; return; }
            if (!Regex.IsMatch(pass, @"[a-z]")) { lblError.Text = "✗ Need at least 1 lowercase letter."; return; }
            if (pass != conf) { lblError.Text = "✗ Passwords do not match."; return; }
            UserStore.Users[user] = new UserAccount { Username = user, Password = pass };
            UserStore.Save();
            lblSuccess.Text = "✔ Account created! You can now login.";
            tbUser.Text = tbPass.Text = tbConfirm.Text = "";
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FORM 3 — DASHBOARD  (upgraded with daily stats)
    // ══════════════════════════════════════════════════════════
    class DashboardForm : Form
    {
        Panel pnlSide, pnlMain;
        Label lblWelcome;

        public DashboardForm()
        {
            Text = "FitTrack — Dashboard";
            Size = new Size(1050, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            FormClosed += (s, e) => Application.Exit();
            BuildUI();
            ShowDashboard();
        }

        void BuildUI()
        {
            // ── Sidebar ──
            pnlSide = new Panel { Width = 230, Dock = DockStyle.Left, BackColor = Theme.Surface };
            Controls.Add(pnlSide);

            pnlSide.Controls.Add(new Label { Text = "💪 FitTrack", ForeColor = Theme.Accent, Font = Theme.FontHeading, Left = 20, Top = 24, AutoSize = true, BackColor = Color.Transparent });
            lblWelcome = new Label { Left = 20, Top = 58, Width = 190, ForeColor = Theme.TextSec, Font = Theme.FontSmall, BackColor = Color.Transparent, AutoSize = false };
            pnlSide.Controls.Add(lblWelcome);

            int sy = 100;
            SideBtn("🏠  Dashboard",       sy, ShowDashboard);           sy += 50;
            SideBtn("🎯  Set Goal",         sy, () => OpenGoalForm());    sy += 50;
            SideBtn("🏃  Add Activities",   sy, () => OpenActivitySelectForm()); sy += 50;
            SideBtn("📊  View Results",     sy, () => new ResultsForm().ShowDialog(this));   sy += 50;
            SideBtn("📅  Progress History", sy, () => new HistoryForm().ShowDialog(this));   sy += 50;
            SideBtn("🚪  Logout",           sy, DoLogout, Theme.Danger);

            pnlMain = new Panel { Left = 230, Width = 820, Height = 680, BackColor = Theme.Background };
            Controls.Add(pnlMain);
        }

        void SideBtn(string text, int top, Action action, Color? col = null)
        {
            var btn = new Button
            {
                Text = text, Left = 0, Top = top, Width = 230, Height = 44,
                FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent,
                ForeColor = col ?? Theme.TextPrimary, Font = Theme.FontBody,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.Card;
            btn.Click += (s, e) => action();
            pnlSide.Controls.Add(btn);
        }

        public void ShowDashboard()
        {
            var u = UserStore.CurrentUser;
            lblWelcome.Text = $"Hello, {u.Username}";
            pnlMain.Controls.Clear();

            pnlMain.Controls.Add(new Label { Text = "Dashboard Overview", ForeColor = Theme.TextPrimary, Font = Theme.FontTitle, Left = 30, Top = 30, AutoSize = true, BackColor = Color.Transparent });
            pnlMain.Controls.Add(Theme.StyledLabel($"📅 {DateTime.Today:dddd, dd MMMM yyyy}", 30, 72, Theme.FontBody, Theme.TextSec));

            // ── Today's stats ──
            double todayTotal = u.Activities.Where(a => a.Date.Date == DateTime.Today).Sum(a => a.CaloriesBurned);
            int todayCount    = u.Activities.Count(a => a.Date.Date == DateTime.Today);
            double goal       = u.CalorieGoal;
            double remaining  = Math.Max(0, goal - todayTotal);
            bool achieved     = goal > 0 && todayTotal >= goal;

            AddStatCard(30,  110, "🔥 Today Burned",     $"{todayTotal:F0} kcal", Theme.Warning);
            AddStatCard(290, 110, "🎯 Goal",             goal > 0 ? $"{goal:F0} kcal" : "Not Set", Theme.Accent);
            AddStatCard(550, 110, "📋 Today Activities", todayCount.ToString(), Theme.Purple);

            // Remaining card
            if (goal > 0)
            {
                AddStatCard(30, 222, achieved ? "✅ Status" : "⚡ Remaining",
                    achieved ? "Goal Achieved!" : $"{remaining:F0} kcal", achieved ? Theme.Success : Theme.Danger);
            }

            // ── Goal status banner ──
            if (goal > 0)
            {
                int bTop = 340;
                var banner = new Panel { Left = 30, Top = bTop, Width = 760, Height = 55, BackColor = achieved ? Color.FromArgb(30, 50, 220, 140) : Color.FromArgb(30, 255, 80, 100) };
                banner.Controls.Add(new Label
                {
                    Text = achieved ? "🎉  Goal Achieved! Amazing work!" : $"⚡  Keep going! {remaining:F0} kcal more to reach your goal.",
                    ForeColor = achieved ? Theme.Success : Theme.Danger,
                    Font = Theme.FontHeading, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(16, 0, 0, 0), BackColor = Color.Transparent
                });
                pnlMain.Controls.Add(banner);
            }

            // ── Recent activities today ──
            int listTop = goal > 0 ? 415 : 340;
            pnlMain.Controls.Add(Theme.StyledLabel("Today's Activities", 30, listTop - 28, Theme.FontHeading, Theme.TextSec));
            var lst = new ListBox { Left = 30, Top = listTop, Width = 760, Height = 200, BackColor = Theme.Card, ForeColor = Theme.TextPrimary, Font = Theme.FontBody, BorderStyle = BorderStyle.None };
            var todayActs = u.Activities.Where(a => a.Date.Date == DateTime.Today).ToList();
            if (todayActs.Count == 0) lst.Items.Add("  No activities recorded today. Click 'Add Activities' to start!");
            else foreach (var a in todayActs) lst.Items.Add($"  {a.ActivityName,-16}  {a.CaloriesBurned:F1} kcal");
            pnlMain.Controls.Add(lst);
        }

        void AddStatCard(int x, int y, string title, string value, Color accent)
        {
            var card = new Panel { Left = x, Top = y, Width = 230, Height = 90, BackColor = Theme.Card };
            card.Controls.Add(new Label { Text = title, Left = 16, Top = 10, AutoSize = true, ForeColor = Theme.TextSec, Font = Theme.FontSmall, BackColor = Color.Transparent });
            card.Controls.Add(new Label { Text = value, Left = 16, Top = 34, AutoSize = false, Width = 198, ForeColor = accent, Font = new Font("Segoe UI", 17f, FontStyle.Bold), BackColor = Color.Transparent });
            pnlMain.Controls.Add(card);
        }

        void OpenGoalForm()
        {
            var gf = new GoalForm();
            gf.OnGoalSaved = () =>
            {
                gf.Close();
                OpenActivitySelectForm();
            };
            gf.ShowDialog(this);
            ShowDashboard();
        }

        void OpenActivitySelectForm()
        {
            var asf = new ActivitySelectForm();
            asf.OnActivitiesSelected = (selected) =>
            {
                asf.Close();
                var calcForm = new MultiCalcForm(selected);
                calcForm.OnSaved = () => { calcForm.Close(); ShowDashboard(); };
                calcForm.ShowDialog(this);
                ShowDashboard();
            };
            asf.ShowDialog(this);
        }

        void DoLogout() { UserStore.CurrentUser = null; new LoginForm().Show(); Hide(); }
    }

    // ══════════════════════════════════════════════════════════
    //  FORM 4 — GOAL SETTING  (with auto-navigate callback)
    // ══════════════════════════════════════════════════════════
    class GoalForm : Form
    {
        TextBox tbGoal;
        Label lblError, lblSaved;
        public Action OnGoalSaved;  // callback for auto-navigation

        public GoalForm()
        {
            Text = "FitTrack — Set Goal";
            Size = new Size(460, 420);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BuildUI();
        }

        void BuildUI()
        {
            Controls.Add(new Label { Text = "🎯 Set Calorie Goal", ForeColor = Theme.Accent, Font = Theme.FontTitle, Left = 30, Top = 24, AutoSize = true, BackColor = Color.Transparent });

            var card = new Panel { Left = 30, Top = 90, Width = 390, Height = 270, BackColor = Theme.Surface };
            Controls.Add(card);

            card.Controls.Add(Theme.StyledLabel("Daily Calorie Burn Goal (kcal)", 24, 24, Theme.FontHeading, Theme.TextSec));
            tbGoal = Theme.StyledTextBox(); tbGoal.Left = 24; tbGoal.Top = 58; tbGoal.Width = 340; card.Controls.Add(tbGoal);
            if (UserStore.CurrentUser.CalorieGoal > 0) tbGoal.Text = UserStore.CurrentUser.CalorieGoal.ToString();

            lblError = new Label { Left = 24, Top = 100, Width = 340, Height = 20, ForeColor = Theme.Danger, Font = Theme.FontSmall, BackColor = Color.Transparent };
            card.Controls.Add(lblError);
            lblSaved = new Label { Left = 24, Top = 100, Width = 340, Height = 20, ForeColor = Theme.Success, Font = Theme.FontSmall, BackColor = Color.Transparent };
            card.Controls.Add(lblSaved);

            var btnSave = Theme.StyledButton("SAVE & CONTINUE →");
            btnSave.Left = 24; btnSave.Top = 132; btnSave.Width = 340; card.Controls.Add(btnSave);
            btnSave.Click += (s, e) =>
            {
                lblError.Text = ""; lblSaved.Text = "";
                if (!double.TryParse(tbGoal.Text, out double g) || g <= 0) { lblError.Text = "✗ Please enter a valid positive number."; return; }
                UserStore.CurrentUser.CalorieGoal = g;
                UserStore.Save();
                lblSaved.Text = $"✔ Goal saved: {g} kcal — opening Activities...";
                // Auto-navigate after short delay
                var t = new System.Windows.Forms.Timer { Interval = 800 };
                t.Tick += (ts, te) => { t.Stop(); OnGoalSaved?.Invoke(); };
                t.Start();
            };

            card.Controls.Add(Theme.StyledLabel("Example: 500 = burn 500 calories today", 24, 200, Theme.FontSmall, Theme.TextSec));
            card.Controls.Add(Theme.StyledLabel("After saving, you will be taken to Add Activities.", 24, 222, Theme.FontSmall, Theme.TextSec));
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FORM 5 — ACTIVITY SELECTION  (CheckedListBox multi-select)
    // ══════════════════════════════════════════════════════════
    class ActivitySelectForm : Form
    {
        CheckedListBox clb;
        public Action<List<string>> OnActivitiesSelected;

        static readonly string[] AllActivities = { "Walking", "Running", "Cycling", "Swimming", "Pushups", "Jump Rope" };

        public ActivitySelectForm()
        {
            Text = "FitTrack — Select Activities";
            Size = new Size(500, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BuildUI();
        }

        void BuildUI()
        {
            Controls.Add(new Label { Text = "🏃 Select Activities", ForeColor = Theme.Accent, Font = Theme.FontTitle, Left = 30, Top = 20, AutoSize = true, BackColor = Color.Transparent });
            Controls.Add(Theme.StyledLabel("Tick one or more activities you did today:", 30, 72, Theme.FontBody, Theme.TextSec));

            var card = new Panel { Left = 30, Top = 100, Width = 430, Height = 370, BackColor = Theme.Surface };
            Controls.Add(card);

            // Instruction label
            card.Controls.Add(Theme.StyledLabel("✔ Check all activities you performed:", 20, 16, Theme.FontHeading, Theme.TextSec));

            clb = new CheckedListBox
            {
                Left = 20, Top = 50, Width = 390, Height = 240,
                BackColor = Theme.Card, ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.None,
                CheckOnClick = true,
            };
            foreach (var a in AllActivities) clb.Items.Add(a, false);
            card.Controls.Add(clb);

            var lblHint = new Label { Left = 20, Top = 298, Width = 390, Height = 20, ForeColor = Theme.TextSec, Font = Theme.FontSmall, BackColor = Color.Transparent, Text = "You can select multiple activities at once." };
            card.Controls.Add(lblHint);

            var btnNext = Theme.StyledButton("NEXT — Enter Metrics →");
            btnNext.Left = 20; btnNext.Top = 324; btnNext.Width = 390; card.Controls.Add(btnNext);
            btnNext.Click += (s, e) =>
            {
                var selected = clb.CheckedItems.Cast<string>().ToList();
                if (selected.Count == 0) { MessageBox.Show("Please select at least one activity.", "No Activity", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                OnActivitiesSelected?.Invoke(selected);
            };
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FORM 6 — MULTI-ACTIVITY CALCULATE & SAVE
    // ══════════════════════════════════════════════════════════
    class MultiCalcForm : Form
    {
        List<string> selectedActivities;
        List<(TextBox m1, TextBox m2, TextBox m3)> metricBoxes = new List<(TextBox, TextBox, TextBox)>();
        Panel pnlScroll;
        Label lblResult;
        public Action OnSaved;

        static readonly Dictionary<string, (string m1, string m2, string m3, Func<double, double, double, double> calc)> ActivityDefs
            = new Dictionary<string, (string, string, string, Func<double, double, double, double>)>
        {
            ["Walking"]   = ("Steps",         "Distance (km)", "Time (min)",   (m1,m2,m3) => m1 * 0.04),
            ["Swimming"]  = ("Laps",          "Time (min)",    "Heart Rate",   (m1,m2,m3) => m1 * 8.0 + m2 * 0.5),
            ["Running"]   = ("Distance (km)", "Time (min)",    "Speed (km/h)", (m1,m2,m3) => m1 * 70 * 1.036 / 1000 * 60),
            ["Cycling"]   = ("Distance (km)", "Time (min)",    "Speed (km/h)", (m1,m2,m3) => m1 * 0.5 * m3 * 0.1),
            ["Pushups"]   = ("Reps",          "Sets",          "Time (min)",   (m1,m2,m3) => m1 * m2 * 0.35),
            ["Jump Rope"] = ("Jumps",         "Time (min)",    "Heart Rate",   (m1,m2,m3) => m1 * 0.012 + m2 * 0.8),
        };

        public MultiCalcForm(List<string> activities)
        {
            selectedActivities = activities;
            Text = "FitTrack — Enter Metrics & Calculate";
            int formHeight = Math.Min(800, 160 + activities.Count * 180);
            Size = new Size(580, formHeight);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BuildUI();
        }

        void BuildUI()
        {
            Controls.Add(new Label { Text = "📊 Enter Metrics", ForeColor = Theme.Accent, Font = Theme.FontTitle, Left = 30, Top = 18, AutoSize = true, BackColor = Color.Transparent });
            Controls.Add(Theme.StyledLabel($"Activities: {string.Join(", ", selectedActivities)}", 30, 62, Theme.FontSmall, Theme.TextSec));

            // Scrollable panel for metrics
            pnlScroll = new Panel
            {
                Left = 20, Top = 88, Width = 540,
                Height = Size.Height - 220,
                AutoScroll = true, BackColor = Theme.Background
            };
            Controls.Add(pnlScroll);

            int y = 0;
            metricBoxes.Clear();
            foreach (var act in selectedActivities)
            {
                var def = ActivityDefs[act];

                // Activity header
                var hdr = new Panel { Left = 0, Top = y, Width = 520, Height = 34, BackColor = Theme.Card };
                hdr.Controls.Add(new Label { Text = $"  {GetIcon(act)} {act}", Left = 8, Top = 7, AutoSize = true, ForeColor = Theme.Accent, Font = Theme.FontHeading, BackColor = Color.Transparent });
                pnlScroll.Controls.Add(hdr); y += 38;

                // 3 metric textboxes in a row
                var box = new Panel { Left = 0, Top = y, Width = 520, Height = 120, BackColor = Theme.Surface };

                var tb1 = AddMetricField(box, def.m1, 10, 10);
                var tb2 = AddMetricField(box, def.m2, 180, 10);
                var tb3 = AddMetricField(box, def.m3, 350, 10);

                pnlScroll.Controls.Add(box);
                metricBoxes.Add((tb1, tb2, tb3));
                y += 128;
            }

            // Result label
            lblResult = new Label
            {
                Left = 20, Top = Size.Height - 130, Width = 540, Height = 24,
                ForeColor = Theme.Warning, Font = Theme.FontHeading,
                BackColor = Color.Transparent, Text = ""
            };
            Controls.Add(lblResult);

            // Buttons
            var btnCalc = Theme.StyledButton("🔥 CALCULATE", Theme.AccentDark);
            btnCalc.Left = 20; btnCalc.Top = Size.Height - 100; btnCalc.Width = 250;
            Controls.Add(btnCalc);
            btnCalc.Click += BtnCalc_Click;

            var btnSave = Theme.StyledButton("💾 SAVE ALL");
            btnSave.Left = 290; btnSave.Top = Size.Height - 100; btnSave.Width = 250;
            Controls.Add(btnSave);
            btnSave.Click += BtnSave_Click;
        }

        TextBox AddMetricField(Panel parent, string label, int x, int y)
        {
            parent.Controls.Add(new Label { Text = label, Left = x, Top = y, Width = 160, AutoSize = false, ForeColor = Theme.TextSec, Font = Theme.FontSmall, BackColor = Color.Transparent });
            var tb = Theme.StyledTextBox();
            tb.Left = x; tb.Top = y + 20; tb.Width = 158;
            parent.Controls.Add(tb);
            return tb;
        }

        string GetIcon(string act)
        {
            switch (act)
            {
                case "Walking": return "🚶";
                case "Running": return "🏃";
                case "Cycling": return "🚴";
                case "Swimming": return "🏊";
                case "Pushups": return "💪";
                case "Jump Rope": return "⚡";
                default: return "•";
            }
        }

        List<double> CalculateAll(out string error)
        {
            error = null;
            var results = new List<double>();
            for (int i = 0; i < selectedActivities.Count; i++)
            {
                var (tb1, tb2, tb3) = metricBoxes[i];
                if (!double.TryParse(tb1.Text, out double m1) || m1 <= 0 ||
                    !double.TryParse(tb2.Text, out double m2) || m2 <= 0 ||
                    !double.TryParse(tb3.Text, out double m3) || m3 <= 0)
                {
                    error = $"✗ Please fill all fields for {selectedActivities[i]} with valid positive numbers.";
                    return null;
                }
                double cal = ActivityDefs[selectedActivities[i]].calc(m1, m2, m3);
                results.Add(cal);
            }
            return results;
        }

        void BtnCalc_Click(object s, EventArgs e)
        {
            var results = CalculateAll(out string error);
            if (results == null) { lblResult.Text = error; lblResult.ForeColor = Theme.Danger; return; }
            double total = results.Sum();
            var parts = selectedActivities.Select((a, i) => $"{a}: {results[i]:F1}").ToList();
            lblResult.Text = $"🔥 Total: {total:F1} kcal  ({string.Join("  |  ", parts)})";
            lblResult.ForeColor = Theme.Warning;
        }

        void BtnSave_Click(object s, EventArgs e)
        {
            var results = CalculateAll(out string error);
            if (results == null) { lblResult.Text = error; lblResult.ForeColor = Theme.Danger; return; }

            for (int i = 0; i < selectedActivities.Count; i++)
            {
                var (tb1, tb2, tb3) = metricBoxes[i];
                double.TryParse(tb1.Text, out double m1);
                double.TryParse(tb2.Text, out double m2);
                double.TryParse(tb3.Text, out double m3);
                UserStore.CurrentUser.Activities.Add(new ActivityEntry
                {
                    ActivityName = selectedActivities[i],
                    Metric1 = m1, Metric2 = m2, Metric3 = m3,
                    CaloriesBurned = results[i],
                    Date = DateTime.Today
                });
            }
            UserStore.Save();

            double total = results.Sum();
            MessageBox.Show($"✔ {selectedActivities.Count} activit{(selectedActivities.Count == 1 ? "y" : "ies")} saved!\nTotal: {total:F1} kcal burned today.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            OnSaved?.Invoke();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FORM 7 — RESULTS
    // ══════════════════════════════════════════════════════════
    class ResultsForm : Form
    {
        public ResultsForm()
        {
            Text = "FitTrack — Results";
            Size = new Size(620, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BuildUI();
        }

        void BuildUI()
        {
            var u = UserStore.CurrentUser;
            double todayTotal = u.Activities.Where(a => a.Date.Date == DateTime.Today).Sum(a => a.CaloriesBurned);
            double allTotal   = u.Activities.Sum(a => a.CaloriesBurned);
            bool achieved     = u.CalorieGoal > 0 && todayTotal >= u.CalorieGoal;

            Controls.Add(new Label { Text = "📊 Results", ForeColor = Theme.Accent, Font = Theme.FontTitle, Left = 30, Top = 20, AutoSize = true, BackColor = Color.Transparent });

            int y = 90;
            Controls.Add(Theme.StyledLabel("Today's Activity Breakdown", 30, y, Theme.FontHeading, Theme.TextSec)); y += 28;

            var lst = new ListBox { Left = 30, Top = y, Width = 555, Height = 200, BackColor = Theme.Card, ForeColor = Theme.TextPrimary, Font = Theme.FontBody, BorderStyle = BorderStyle.None };
            var todayActs = u.Activities.Where(a => a.Date.Date == DateTime.Today).ToList();
            if (todayActs.Count == 0) lst.Items.Add("  No activities recorded today.");
            foreach (var a in todayActs) lst.Items.Add($"  {a.ActivityName,-16}  {a.Metric1:F0} | {a.Metric2:F0} | {a.Metric3:F0}   →  {a.CaloriesBurned:F1} kcal");
            Controls.Add(lst); y += 218;

            var sep = new Panel { Left = 30, Top = y, Width = 555, Height = 1, BackColor = Theme.TextSec }; Controls.Add(sep); y += 14;
            Controls.Add(Theme.StyledLabel($"Today Burned:     {todayTotal:F1} kcal", 30, y, Theme.FontHeading, Theme.Warning)); y += 28;
            Controls.Add(Theme.StyledLabel($"All-time Burned:  {allTotal:F1} kcal",   30, y, Theme.FontHeading, Theme.TextSec)); y += 28;
            Controls.Add(Theme.StyledLabel($"Calorie Goal:     {(u.CalorieGoal > 0 ? u.CalorieGoal + " kcal" : "Not set")}", 30, y, Theme.FontHeading, Theme.Accent)); y += 36;

            if (u.CalorieGoal > 0)
            {
                var banner = new Panel { Left = 30, Top = y, Width = 555, Height = 80, BackColor = achieved ? Color.FromArgb(30, 50, 220, 140) : Color.FromArgb(30, 255, 80, 100) };
                banner.Controls.Add(new Label { Text = achieved ? "🎉" : "⚡", Left = 16, Top = 12, AutoSize = true, Font = new Font("Segoe UI", 26f), BackColor = Color.Transparent });
                banner.Controls.Add(new Label { Text = achieved ? "Goal Achieved!" : "Goal Not Yet Achieved", Left = 66, Top = 10, AutoSize = true, Font = Theme.FontTitle, ForeColor = achieved ? Theme.Success : Theme.Danger, BackColor = Color.Transparent });
                banner.Controls.Add(new Label { Text = achieved ? $"Exceeded by {todayTotal - u.CalorieGoal:F0} kcal 💪" : $"{u.CalorieGoal - todayTotal:F0} kcal more needed today.", Left = 66, Top = 46, AutoSize = true, Font = Theme.FontSmall, ForeColor = Theme.TextSec, BackColor = Color.Transparent });
                Controls.Add(banner);
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FORM 8 — PROGRESS HISTORY  (grouped by date)
    // ══════════════════════════════════════════════════════════
    class HistoryForm : Form
    {
        public HistoryForm()
        {
            Text = "FitTrack — Progress History";
            Size = new Size(660, 660);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BuildUI();
        }

        void BuildUI()
        {
            var u = UserStore.CurrentUser;

            Controls.Add(new Label { Text = "📅 Progress History", ForeColor = Theme.Accent, Font = Theme.FontTitle, Left = 30, Top = 20, AutoSize = true, BackColor = Color.Transparent });
            Controls.Add(Theme.StyledLabel("Your calorie burn history — grouped by day", 30, 70, Theme.FontSmall, Theme.TextSec));

            // Group activities by date
            var grouped = u.Activities
                .GroupBy(a => a.Date.Date)
                .OrderByDescending(g => g.Key)
                .ToList();

            if (grouped.Count == 0)
            {
                Controls.Add(Theme.StyledLabel("No history yet. Start adding activities!", 30, 120, Theme.FontHeading, Theme.TextSec));
                return;
            }

            // Scrollable list panel
            var pnlScroll = new Panel { Left = 30, Top = 100, Width = 590, Height = 500, AutoScroll = true, BackColor = Theme.Background };
            Controls.Add(pnlScroll);

            int y = 0;
            foreach (var group in grouped)
            {
                DateTime date    = group.Key;
                double dayTotal  = group.Sum(a => a.CaloriesBurned);
                bool isToday     = date == DateTime.Today;
                bool isYesterday = date == DateTime.Today.AddDays(-1);
                string dayLabel  = isToday ? "Today" : isYesterday ? "Yesterday" : date.ToString("dddd");
                bool goalReached = u.CalorieGoal > 0 && dayTotal >= u.CalorieGoal;

                // Day header bar
                var hdrPanel = new Panel { Left = 0, Top = y, Width = 570, Height = 42, BackColor = Theme.Card };
                var dateLabel = new Label
                {
                    Text = $"  📅 {dayLabel}  —  {date:dd MMM yyyy}",
                    Left = 0, Top = 0, Width = 370, Height = 42,
                    ForeColor = isToday ? Theme.Accent : Theme.TextPrimary,
                    Font = Theme.FontHeading, TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };
                var kcalLabel = new Label
                {
                    Text = $"{dayTotal:F0} kcal  {(goalReached ? "✅" : "")}",
                    Left = 370, Top = 0, Width = 190, Height = 42,
                    ForeColor = goalReached ? Theme.Success : Theme.Warning,
                    Font = Theme.FontHeading, TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent
                };
                hdrPanel.Controls.Add(dateLabel);
                hdrPanel.Controls.Add(kcalLabel);
                pnlScroll.Controls.Add(hdrPanel);
                y += 46;

                // Individual activities for this day
                foreach (var a in group.OrderBy(x => x.Date))
                {
                    var row = new Panel { Left = 10, Top = y, Width = 555, Height = 30, BackColor = Color.Transparent };
                    row.Controls.Add(new Label { Text = $"  {GetIcon(a.ActivityName)} {a.ActivityName}", Left = 0, Top = 5, Width = 200, ForeColor = Theme.TextPrimary, Font = Theme.FontBody, BackColor = Color.Transparent });
                    row.Controls.Add(new Label { Text = $"{a.CaloriesBurned:F1} kcal", Left = 210, Top = 5, Width = 120, ForeColor = Theme.TextSec, Font = Theme.FontBody, BackColor = Color.Transparent });
                    pnlScroll.Controls.Add(row);
                    y += 32;
                }

                // Spacer
                y += 10;
            }

            // Summary at bottom
            double allTime = u.Activities.Sum(a => a.CaloriesBurned);
            int totalDays  = grouped.Count;
            Controls.Add(Theme.StyledLabel($"Total: {allTime:F0} kcal across {totalDays} day(s)", 30, 608, Theme.FontSmall, Theme.TextSec));
        }

        string GetIcon(string act)
        {
            switch (act)
            {
                case "Walking": return "🚶";
                case "Running": return "🏃";
                case "Cycling": return "🚴";
                case "Swimming": return "🏊";
                case "Pushups": return "💪";
                case "Jump Rope": return "⚡";
                default: return "•";
            }
        }
    }
}