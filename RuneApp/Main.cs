﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using RuneOptim;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace RuneApp
{
    public partial class Main : Form
    {
        public static Save data;
        public static List<Build> builds = new List<Build>();
        int priority = 1;
        public static bool useEquipped = false;
        string filelink = "";
        string whatsNewText = "";
        Build currentBuild = null;
        public static Configuration config = null;

		private Task runTask = null;
		private CancellationToken runToken;
		private CancellationTokenSource runSource = null;
		bool plsDie = false;
		public static Help help = null;

        public static bool makeStats = true;
        private int linkCol = 1;

        FileInfo excelFile = new FileInfo(@"runestats.xlsx");
        public static ExcelPackage excelPack = null;
        private bool gotExcelPack = false;
        ExcelWorksheets excelSheets = null;

        public static bool MakeStats
        {
            get
            {
                if (config != null && config.AppSettings.Settings.AllKeys.Contains("nostats"))
                {
                    bool tstats = false;
                    if (bool.TryParse(Main.config.AppSettings.Settings["nostats"].Value, out tstats))
                        makeStats = !tstats;
                }
                return makeStats;
            }
        }

		public Main()
        {
            InitializeComponent();
            config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            bool dontupdate = false;
            if (config != null)
            {
                // it's stored as string, what is fasted yescompare?
                if (config.AppSettings.Settings.AllKeys.Contains("useEquipped") && config.AppSettings.Settings["useEquipped"].Value == true.ToString())
                {
                    useRunesCheck.Checked = true;
                    useEquipped = true;
                }
                // this?
                if (Main.config.AppSettings.Settings.AllKeys.Contains("noupdate"))
                    bool.TryParse(Main.config.AppSettings.Settings["noupdate"].Value, out dontupdate);
                if (Main.config.AppSettings.Settings.AllKeys.Contains("nostats"))
                {
                    bool tstats = false;
                    if (bool.TryParse(Main.config.AppSettings.Settings["nostats"].Value, out tstats))
                        makeStats = !tstats;
                }
            }

            runes = new RuneControl[] { runeControl1, runeControl2, runeControl3, runeControl4, runeControl5, runeControl6 };
            var sorter = new ListViewSort();
            sorter.OnColumnClick(MonPriority.Index);
            dataMonsterList.ListViewItemSorter = sorter;
            dataRuneList.ListViewItemSorter = new ListViewSort();

            sorter = new ListViewSort();
            sorter.OnColumnClick(0);
            buildList.ListViewItemSorter = sorter;

            if (!dontupdate)
            {
                Task.Factory.StartNew(() =>
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadStringCompleted += client_DownloadStringCompleted;
                        client.DownloadStringAsync(new Uri("https://raw.github.com/Skibisky/RuneManager/master/version.txt"));
                    }
                });
            }
            else
            {
                updateBox.Show();
                updateComplain.Text = "Updates Disabled";
                var ver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                string oldvernum = ver.ProductVersion;
                updateCurrent.Text = "Current: " + oldvernum;
                updateNew.Text = "";
            }

            for (int i = 0; i < 11; i++)
            {
                ToolStripItem it = new ToolStripMenuItem(i.ToString() + (i > 0 ? " (" + Math.Ceiling(i * 1.5).ToString() + "%)" : ""));
                it.Tag = (int)Math.Ceiling(i * 1.5);
                it.Click += ShrineClickSpeed;
                speedToolStripMenuItem.DropDownItems.Add(it);
            }

            Label l = new Label();
            l.Location = new Point(4 + 50, 400 - 18);
            l.Name = "compBefore";
            l.Text = "Before";
            l.Size = new Size(50, 14);
            groupBox1.Controls.Add(l);

            l = new Label();
            l.Location = new Point(4 + 100, 400 - 18);
            l.Name = "compAfter";
            l.Text = "After";
            l.Size = new Size(50, 14);
            groupBox1.Controls.Add(l);

            l = new Label();
            l.Location = new Point(4 + 150, 400 - 18);
            l.Name = "compDiff";
            l.Text = "Difference";
            l.Size = new Size(60, 14);
            groupBox1.Controls.Add(l);

            int xx = 0;
            int yy = 0;
            foreach (var s in Build.statNames.Concat(Build.extraNames))
            {
                l = new Label();
                l.Location = new Point(4 + xx, 400 + yy);
                l.Name = s + "compStat";
                l.Text = s;
                l.Size = new Size(50, 14);
                groupBox1.Controls.Add(l);
                xx += 50;

                l = new Label();
                l.Location = new Point(4 + xx, 400 + yy);
                l.Name = s + "compBefore";
                l.Text = "";
                l.Size = new Size(50, 14);
                groupBox1.Controls.Add(l);
                xx += 50;

                l = new Label();
                l.Location = new Point(4 + xx, 400 + yy);
                l.Name = s + "compAfter";
                l.Text = "";
                l.Size = new Size(50, 14);
                groupBox1.Controls.Add(l);
                xx += 50;

                l = new Label();
                l.Location = new Point(4 + xx, 400 + yy);
                l.Name = s + "compDiff";
                l.Text = "";
                l.Size = new Size(50, 14);
                groupBox1.Controls.Add(l);
                xx += 50;

                if (s == "SPD")
                    yy += 4;
                if (s == "ACC")
                    yy += 8;

                yy += 16;
                xx = 0;
            }
        }
        
        private void ShrineClickSpeed(object sender, EventArgs e)
        {
            var it = (ToolStripMenuItem)sender;
            if (it != null)
            {
                foreach (ToolStripMenuItem i in speedToolStripMenuItem.DropDownItems)
                {
                    i.Checked = false;
                }
                it.Checked = true;
                if (data == null)
                    return;

                data.shrines.Speed = (int)it.Tag;
                if (config != null)
                {
                    config.AppSettings.Settings.Remove("shrineSpeed");
                    config.AppSettings.Settings.Add("shrineSpeed", it.Tag.ToString());
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (File.Exists("save.json"))
            {
                LoadFile("save.json");
            }
            if (File.Exists("builds.json"))
            {
                LoadBuilds("builds.json");
            }
			if (File.Exists("basestats.json"))
			{
				LoadMons("basestats.json");
			}
        }

        private void monstertab_list_select(object sender, EventArgs e)
        {
            if (dataMonsterList.FocusedItem != null)
            {
                var item = dataMonsterList.FocusedItem;
                if (item.Tag != null)
                {
                    Monster mon = (Monster)item.Tag;

                    ShowMon(mon);
                }
            }
        }

        private void loadSaveDialogue(object sender, EventArgs e)
        {
            int size = -1;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                if (file != null)
                {
                    try
                    {
                        size = LoadFile(file);
                        if (size > 0)
                        {
                            if (File.Exists("save.json"))
                            {
                                if (MessageBox.Show("Do you want to override the existing startup save?", "Load Save" , MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    File.Copy(file, "save.json", true);
                                }
                            }
                            else
                            {
                                if (MessageBox.Show("Do you want to load this save on startup?", "Load Save", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    File.Copy(file, "save.json");
                                }
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            Console.WriteLine(size); // <-- Shows file size in debugging mode.
            Console.WriteLine(result); // <-- For debugging use.
			
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // confirm maybe?
            this.Close();
        }

        private void rune_Click(object sender, EventArgs e)
        {
            foreach (RuneControl t in runes)
            {
                t.Gamma = 1;
                t.Refresh();
            }

            RuneControl tc = ((RuneControl)sender);
            if (tc.Tag != null)
            {
                tc.Gamma = 1.4f;
                tc.Refresh();
                rune_Stats((Rune)tc.Tag);
                runeBuild.Show();
                runeShown.SetRune((Rune)tc.Tag);
            }
            else
            {
                tc.Hide();
                runeBuild.Hide();
            }
        }

        private void label8_Click(object sender, EventArgs e)
        {
            runeBuild.Hide();
            foreach (RuneControl r in runes)
            {
                r.Gamma = 1;
                r.Refresh();
            }
        }

        private void label8_Click_1(object sender, EventArgs e)
        {
            runeBox.Hide();
        }

        private void runetab_list_select(object sender, EventArgs e)
        {
            if (dataRuneList.FocusedItem != null)
            {
                var item = dataRuneList.FocusedItem;
                if (item.Tag != null)
                {
                    Rune rune = (Rune)item.Tag;

                    runeBox.Show();
                    runeInventory.SetRune(rune);

                    IRuneMain.Text = Rune.StringIt(rune.MainType, rune.MainValue);
                    IRuneInnate.Text = Rune.StringIt(rune.InnateType, rune.InnateValue);
                    IRuneSub1.Text = Rune.StringIt(rune.Sub1Type, rune.Sub1Value);
                    IRuneSub2.Text = Rune.StringIt(rune.Sub2Type, rune.Sub2Value);
                    IRuneSub3.Text = Rune.StringIt(rune.Sub3Type, rune.Sub3Value);
                    IRuneSub4.Text = Rune.StringIt(rune.Sub4Type, rune.Sub4Value);
                    IRuneLevel.Text = rune.Level.ToString();
                    IRuneMon.Text = "[" + rune.ID + "] " + rune.AssignedName;
                }
            }
            
        }

        private void listView2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var sorter = (ListViewSort)((ListView)sender).ListViewItemSorter;
            sorter.OnColumnClick(e.Column);
            ((ListView)sender).Sort();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (dataMonsterList.FocusedItem != null)
            {
                if (dataMonsterList.FocusedItem.Tag != null)
                {
                    Monster mon = (Monster)dataMonsterList.FocusedItem.Tag;
                    int maxPri = (int)data.Monsters.Max(x => x.priority);
                    if (mon.priority == 0)
                    {
                        mon.priority = maxPri + 1;
                        dataMonsterList.FocusedItem.SubItems[MonPriority.Index].Text = (maxPri + 1).ToString();
                    }
                    else if (mon.priority != 1)
                    {
                        int pri = mon.priority;
                        Monster mon2 = data.Monsters.Where(x => x.priority == pri - 1).FirstOrDefault();
                        if (mon2 != null)
                        {
                            var items = dataMonsterList.Items;
                            ListViewItem listMon = dataMonsterList.FindItemWithText(mon2.Name);
                            mon2.priority += 1;
                            listMon.SubItems[MonPriority.Index].Text = mon2.priority.ToString();
                        }
                        mon.priority -= 1;
                        dataMonsterList.FocusedItem.SubItems[MonPriority.Index].Text = (mon.priority).ToString();                        
                    }
                    dataMonsterList.Sort();
                }
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (dataMonsterList.FocusedItem != null)
            {
                if (dataMonsterList.FocusedItem.Tag != null)
                {
                    Monster mon = (Monster)dataMonsterList.FocusedItem.Tag;
                    int maxPri = (int)data.Monsters.Max(x => x.priority);
                    if (mon.priority == 0)
                    {
                        mon.priority = maxPri + 1;
                        dataMonsterList.FocusedItem.SubItems[MonPriority.Index].Text = (maxPri + 1).ToString();
                    }
                    else if (mon.priority != maxPri)
                    {
                        int pri = mon.priority;
                        Monster mon2 = data.Monsters.Where(x => x.priority == pri + 1).FirstOrDefault();
                        if (mon2 != null)
                        {
                            var items = dataMonsterList.Items;
                            ListViewItem listMon = dataMonsterList.FindItemWithText(mon2.Name);
                            mon2.priority -= 1;
                            listMon.SubItems[MonPriority.Index].Text = mon2.priority.ToString();
                        }
                        mon.priority += 1;
                        dataMonsterList.FocusedItem.SubItems[MonPriority.Index].Text = (mon.priority).ToString();
                    }
                    dataMonsterList.Sort();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tabControl1.SelectTab(tabRunes.Name);
            filterList(dataRuneList, x => ((Rune)x).Slot == ((Rune)runeShown.Tag).Slot);
        }

        private void filterList(ListView list, Predicate<object> p)
        {
            dataRuneList.Items.Clear();
            if (data == null)
                return;

            if (data.Runes != null)
            {
                foreach (Rune rune in data.Runes.Where(x => p.Invoke(x)))
                {
                    ListViewItem item = new ListViewItem(new string[]{
                        rune.Set.ToString(),
                        rune.ID.ToString(),
                        rune.Grade.ToString(),
                        Rune.StringIt(rune.MainType, true),
                        rune.MainValue.ToString()
                    });
                    item.Tag = rune;
                    item.BackColor = rune.Locked ? Color.Red : Color.Transparent;
                    dataRuneList.Items.Add(item);
                }
            }
        }

        private void runetab_clearfilter(object sender, EventArgs e)
        {
            filterList(dataRuneList, x => true);
        }
        
        private void loadout_list_select(object sender, EventArgs e)
        {
            if (loadoutList.FocusedItem != null)
            {
                var item = loadoutList.FocusedItem;
                if (item.Tag != null)
                {
                    Loadout load = (Loadout)item.Tag;

					var monid = int.Parse(item.SubItems[2].Text);
					var bid = int.Parse(item.SubItems[0].Text);

					var build = builds.Where(b => b.ID == bid).FirstOrDefault();

					Monster mon = null;
					if (build == null)
						mon = data.GetMonster(monid);
					else
						mon = build.mon;

					ShowMon(mon);

                    ShowStats(load.GetStats(mon), mon);

                    ShowRunes(load.runes);

                    var dmon = data.GetMonster(monid);
					if (dmon != null)
					{
						var dmonld = dmon.Current.Leader;
						var dmonsh = dmon.Current.Shrines;
						dmon.Current.Leader = load.Leader;
						dmon.Current.Shrines = load.Shrines;

						ShowDiff(dmon.GetStats(), load.GetStats(mon));

						dmon.Current.Leader = dmonld;
						dmon.Current.Shrines = dmonsh;
					}
                    ShowSets(load);
                }
            }
            int cost = 0;
            foreach (ListViewItem li in loadoutList.SelectedItems)
            {
                if (li.Tag != null)
                {
                    Loadout load = (Loadout)li.Tag;
					if (load != null)
					{
						var mon = data.GetMonster(int.Parse(li.SubItems[2].Text));
						if (mon != null)
							cost += mon.SwapCost(load);
					}
                }
            }
            toolStripStatusLabel2.Text = "Unequip: " + cost.ToString();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            ClearLoadouts();
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            if (dataMonsterList.SelectedItems.Count > 0)
            {
                using (var ff = new Create())
                {
                    var bb = new Build();
                    bb.New = true;
                    bb.mon = (Monster)dataMonsterList.SelectedItems[0].Tag;
					bb.ID = buildList.Items.Count + 1;
                    while (builds.Any(b => b.ID == bb.ID))
                    {
                        bb.ID++;
                    }

                    ff.Tag = bb;
                    var res = ff.ShowDialog();
                    if (res == System.Windows.Forms.DialogResult.OK)
                    {
						if (builds.Count > 0)
							ff.build.priority = builds.Max(b => b.priority) + 1;
						else
							ff.build.priority = 1;

                        ListViewItem li = new ListViewItem(new string[]{ff.build.priority.ToString(), ff.build.ID.ToString(), ff.build.mon.Name,"", ff.build.mon.ID.ToString() });
                        li.Tag = ff.build;
                        buildList.Items.Add(li);
                        builds.Add(ff.build);

						var lv1li = dataMonsterList.Items.Cast<ListViewItem>().Where(i => i.SubItems.Cast<ListViewItem.ListViewSubItem>().Where(s => s.Text == ff.build.mon.Name).Count() > 0).FirstOrDefault();
						if (lv1li != null)
							lv1li.ForeColor = Color.Green;
					
					}
				}
            }
        }

        private void listView5_DoubleClick(object sender, EventArgs e)
        {
            var items = buildList.SelectedItems;
            if (items.Count > 0)
            {
                var item = items[0];
                if (item.Tag != null)
                {
                    Build bb = (Build)item.Tag;
                    Monster before = bb.mon;
                    using (var ff = new Create())
                    {
                        ff.Tag = bb;
                        var res = ff.ShowDialog();
                        if (res == System.Windows.Forms.DialogResult.OK)
                        {
                            item.SubItems[2].Text = bb.mon.Name;
                            item.SubItems[4].Text = bb.mon.ID.ToString();
                            if (bb.mon != before)
                            {
                                var lv1li = dataMonsterList.Items.Cast<ListViewItem>().Where(i => i.SubItems.Cast<ListViewItem.ListViewSubItem>().Where(s => s.Text == before.Name).Count() > 0).FirstOrDefault();
                                if (lv1li != null)
                                    lv1li.ForeColor = before.inStorage ? Color.Gray : Color.Black;

                                lv1li = dataMonsterList.Items.Cast<ListViewItem>().Where(i => i.SubItems.Cast<ListViewItem.ListViewSubItem>().Where(s => s.Text == ff.build.mon.Name).Count() > 0).FirstOrDefault();
                                if (lv1li != null)
                                    lv1li.ForeColor = Color.Green;

                            }
                        }
                    }
                }
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            plsDie = false;
            var lis = buildList.SelectedItems;
            if (lis.Count > 0)
            {
                var li = lis[0];
                Task.Factory.StartNew(() =>
                {
                    RunBuild(li, makeStats);
                });
            }
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            SaveBuilds();
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will delete a build (not a loadout)." + Environment.NewLine + "How many minutes of work will be undone?", "Delete Build?", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                return;

            var lis = buildList.SelectedItems;
            if (lis.Count > 0)
            {
				foreach (ListViewItem li in lis)
				{
					buildList.Items.Remove(li);
					Build b = (Build)li.Tag;
					if (b != null)
					{
						var lv1li = dataMonsterList.Items.Cast<ListViewItem>().Where(i => i.SubItems.Cast<ListViewItem.ListViewSubItem>().Where(s => s.Text == b.mon.Name).Count() > 0).FirstOrDefault();
						if (lv1li != null)
						{
							lv1li.ForeColor = Color.Black;
							// can't tell is was in storage, name is mangled on load
						}
					}
				}
            }
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            if (data == null || data.Runes == null)
                return;
            foreach (Rune r in data.Runes)
            {
                r.Locked = false;
            }
            checkLocked();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in loadoutList.SelectedItems)
            { 
                Loadout l = (Loadout)li.Tag;

                foreach (Rune r in l.runes)
                {
                    r.Locked = false;
                }

                loadoutList.Items.Remove(li);
            }
            checkLocked();
		}

        private void toolStripButton12_Click(object sender, EventArgs e)
		{
            RunBuilds(false);
		}

		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
            SaveBuilds();
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            if (data == null)
                return;

            if (data.Monsters != null)
            foreach (Monster mon in data.Monsters)
            {
                for (int i = 1; i < 7; i++)
                    mon.Current.RemoveRune(i);
            }

            if (data.Runes != null)
            foreach (Rune r in data.Runes)
            {
                r.AssignedId = 0;
                r.AssignedName = "Inventory";
            }
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            if (File.Exists("save.json"))
            {
                LoadFile("save.json");
            }
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            if (buildList.FocusedItem != null)
            {
                if (buildList.FocusedItem.Tag != null)
                {
                    Build build = (Build)buildList.FocusedItem.Tag;

                    var bb = builds.OrderBy(b => b.priority).ToList();
                    var bi = bb.FindIndex(b => b == build);
                    
                    if (bi != 0)
                    {
                        var b2 = bb[bi - 1];
                        var sid = build.priority;
                        build.priority = b2.priority;
                        b2.priority = sid;

                        buildList.FocusedItem.SubItems[0].Text = build.priority.ToString();

                        var monname = b2.MonName;
                        if (monname == null || monname == "Missingno")
                        {
                            if (b2.DownloadAwake)
                                return;
                            monname = b2.mon.Name;
                        }

                        ListViewItem listmon = buildList.FindItemWithText(b2.mon.Name);
                        listmon.SubItems[0].Text = b2.priority.ToString();

                        buildList.Sort();
                    }
                }
            }
        }

        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            if (buildList.FocusedItem != null)
            {
                if (buildList.FocusedItem.Tag != null)
                {
                    Build build = (Build)buildList.FocusedItem.Tag;

                    var bb = builds.OrderBy(b => b.priority).ToList();
                    var bi = bb.FindIndex(b => b == build);

                    if (bi != builds.Max(b => b.priority) && bi+1 < bb.Count)
                    {
                        var b2 = bb[bi + 1];
                        var sid = build.priority;
                        build.priority = b2.priority;
                        b2.priority = sid;

                        buildList.FocusedItem.SubItems[0].Text = build.priority.ToString();

                        ListViewItem listmon = buildList.FindItemWithText(b2.mon.Name);
                        listmon.SubItems[0].Text = b2.priority.ToString();

                        buildList.Sort();
                    }
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            useEquipped = ((CheckBox)sender).Checked;
            if (config != null)
            {
                config.AppSettings.Settings.Remove("useEquipped");
                config.AppSettings.Settings.Add("useEquipped", useEquipped.ToString());
                config.Save(ConfigurationSaveMode.Modified);
            }
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                updateBox.Visible = true;
                try {
                    string result = e.Result.Replace("\r\n", "\n");
                    int firstline = result.IndexOf('\n');

                    string newvernum = result;
                    if (firstline != -1)
                        newvernum = newvernum.Substring(0, firstline);

                    Console.WriteLine(newvernum);
                    int ind1 = result.IndexOf('\n');

                    if (result.IndexOf('\n') != -1)
                    {
                        int ind2 = result.IndexOf('\n', ind1 + 1);
                        if (ind2 == -1)
                            filelink = e.Result.Substring(ind1 + 1);
                        else
                        {
                            filelink = e.Result.Substring(ind1 + 1, ind2 - ind1 - 1);
                            whatsNewText = e.Result.Substring(ind2 + 1);
                        }

                    }

                    var ver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                    string oldvernum = ver.ProductVersion;
                    updateCurrent.Text = "Current: " + oldvernum;
                    updateNew.Text = "New: " + newvernum;
                    int newver = VersionCompare(oldvernum, newvernum);
                    if (newver > 0)
                    {
                        //MessageBox.Show("You have a newer version than available");
                        updateComplain.Text = "You hacker";
                    }
                    else if (newver < 0)
                    {
                        updateComplain.Text = "Update available!";
                        if (filelink != "")
                        {
                            updateDownload.Enabled = true;
                            if (whatsNewText != "")
                                updateWhat.Visible = true;
                        }
                    }
                    else
                    {
                        updateComplain.Visible = false;
                    }
                }
                catch(Exception ex)
                {
                    updateComplain.Text = e.Error.ToString();
                    updateComplain.Visible = false;
                    Console.WriteLine(ex);
                }
            });
        }

        private void updateDownload_Click(object sender, EventArgs e)
        {
            if (filelink != "")
            {
                Process.Start(filelink);
            }
        }

        private void updateWhat_Click(object sender, EventArgs e)
        {
            if (whatsNewText != "")
            {
                MessageBox.Show(whatsNewText, "What's New");
            }
        }

		private void toolStripButton17_Click(object sender, EventArgs e)
		{
            GenerateStats();
            //GenerateExcel();
		}

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var f = new Options())
            {
                f.ShowDialog();
            }
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updateBox.Show();
            updateComplain.Text = "Checking...";
            Task.Factory.StartNew(() =>
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadStringCompleted += client_DownloadStringCompleted;
                    client.DownloadStringAsync(new Uri("https://raw.github.com/Skibisky/RuneManager/master/version.txt"));
                }
            });
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var ver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            string oldvernum = ver.ProductVersion;

            MessageBox.Show("Rune Manager\r\nBy Skibisky\r\nVersion " + oldvernum, "About", MessageBoxButtons.OK);
        }

        private void userManualHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            help = new Help();
            help.Show();
        }

        private void runelistSwapLocked(object sender, EventArgs e)
        {
            // swap the selected runes locked state
            foreach (ListViewItem li in dataRuneList.SelectedItems)
            {
                var rune = li.Tag as Rune;
                if (rune != null)
                {
                    if (rune.Locked)
                    {
                        rune.Locked = false;
                        li.BackColor = Color.Transparent;
                    }
                    else
                    {
                        rune.Locked = true;
                        li.BackColor = Color.Red;
                    }
                }
            }
        }

        private void runetab_savebutton_click(object sender, EventArgs e)
        {
            // only prompt and backup if the save is str8 outta proxy
            if (!data.isModified && MessageBox.Show("Changes saved will be lost if you import a new save from the proxy.\n\n", "Save save?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                data.isModified = true;

                if (File.Exists("save.json"))
                {
                    // backup, just in case
                    string destFile = Path.Combine("", string.Format("{0}.backup{1}", "save", ".json"));
                    int num = 2;
                    while (File.Exists(destFile))
                    {
                        destFile = Path.Combine("", string.Format("{0}.backup{1}{2}", "save", num, ".json"));
                        num++;
                    }

                    File.Copy("save.json", destFile);
                }
            }
            var str = JsonConvert.SerializeObject(data);
            File.WriteAllText("save.json", str);
        }
        
        private void unequipMonsterButton_Click(object sender, EventArgs e)
        {
            if (data == null)
                return;

            if (data.Monsters != null)
            {
                foreach (ListViewItem li in dataMonsterList.SelectedItems)
                {
                    Monster mon = li.Tag as Monster;
                    if (mon != null)
                    {
                        for (int i = 1; i < 7; i++)
                        {
                            var r = mon.Current.RemoveRune(i);
                            if (r != null)
                            {
                                r.AssignedId = 0;
                                r.AssignedName = "Inventory";
                            }
                        }
                    }
                }
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in loadoutList.SelectedItems)
            {
                Loadout l = (Loadout)li.Tag;

                foreach (Rune r in l.runes)
                {
                    r.Locked = true;
                }
                checkLocked();
            }
        }
        
        private void build_btn_resume_Click(object sender, EventArgs e)
        {
            RunBuilds(true);
        }

        private void build_btn_resumeto_Click(object sender, EventArgs e)
        {
            int selected = -1;
            if (buildList.SelectedItems.Count > 0)
                selected = (buildList.SelectedItems[0].Tag as Build).ID;
            RunBuilds(true, selected);
        }

        private void rune_Stats(Rune rune)
        {
            SRuneMain.Text = Rune.StringIt(rune.MainType, rune.MainValue);
            SRuneInnate.Text = Rune.StringIt(rune.InnateType, rune.InnateValue);
            SRuneSub1.Text = Rune.StringIt(rune.Sub1Type, rune.Sub1Value);
            SRuneSub2.Text = Rune.StringIt(rune.Sub2Type, rune.Sub2Value);
            SRuneSub3.Text = Rune.StringIt(rune.Sub3Type, rune.Sub3Value);
            SRuneSub4.Text = Rune.StringIt(rune.Sub4Type, rune.Sub4Value);
            SRuneLevel.Text = rune.Level.ToString();
            SRuneMon.Text = "[" + rune.ID + "] " + rune.AssignedName;
        }

        static int VersionCompare(string v1, string v2)
        {
            Version ver1;
            Version ver2;
            if (!Version.TryParse(v1, out ver1))
                return 1;
            if (!Version.TryParse(v2, out ver2))
                return -1;

            return ver1.CompareTo(ver2);

            // return 0;
        }

        private void ShowMon(Monster mon)
        {
            Stats cur = mon.GetStats();

            statName.Text = mon.Name;
            statID.Text = mon.ID.ToString();
            statLevel.Text = mon.level.ToString();

            ShowStats(cur, mon);
            ShowRunes(mon.Current.runes);
            ShowSets(mon.Current);
        }

        private void ShowSets(Loadout load)
        {
            if (load.sets != null)
            {
                if (load.sets.Length > 0)
                    Set1Label.Text = load.sets[0] == RuneSet.Null ? "" : load.sets[0].ToString();
                if (load.sets.Length > 1)
                    Set2Label.Text = load.sets[1] == RuneSet.Null ? "" : load.sets[1].ToString();
                if (load.sets.Length > 2)
                    Set3Label.Text = load.sets[2] == RuneSet.Null ? "" : load.sets[2].ToString();

                if (!load.SetsFull)
                {
                    if (load.sets[0] == RuneSet.Null)
                        Set1Label.Text = "Broken";
                    else if (load.sets[1] == RuneSet.Null)
                        Set2Label.Text = "Broken";
                    else if (load.sets[2] == RuneSet.Null)
                        Set3Label.Text = "Broken";
                }
            }
        }

        private void ShowStats(Stats cur, Stats mon)
        {
            foreach (string stat in new string[] { "HP", "ATK", "DEF", "SPD" })
            {
                groupBox1.Controls.Find(stat + "Base", false).FirstOrDefault().Text = mon[stat].ToString();
                groupBox1.Controls.Find(stat + "Total", false).FirstOrDefault().Text = cur[stat].ToString();
                groupBox1.Controls.Find(stat + "Bonus", false).FirstOrDefault().Text = "+" + (cur[stat] - mon[stat]).ToString();
            }

            foreach (string stat in new string[] { "CR", "CD", "RES", "ACC" })
            {
                groupBox1.Controls.Find(stat + "Base", false).FirstOrDefault().Text = mon[stat].ToString() + "%";
                groupBox1.Controls.Find(stat + "Total", false).FirstOrDefault().Text = cur[stat].ToString() + "%";
                groupBox1.Controls.Find(stat + "Bonus", false).FirstOrDefault().Text = "+" + (cur[stat] - mon[stat]).ToString();
            }
        }

        private void ShowDiff(Stats old, Stats load)
        {
            foreach (string stat in new string[] { "HP", "ATK", "DEF", "SPD" })
            {
                groupBox1.Controls.Find(stat + "compBefore", false).FirstOrDefault().Text = old[stat].ToString();
                groupBox1.Controls.Find(stat + "compAfter", false).FirstOrDefault().Text = load[stat].ToString();
                groupBox1.Controls.Find(stat + "compDiff", false).FirstOrDefault().Text = (load[stat] - old[stat]).ToString();
            }

            foreach (string stat in new string[] { "CR", "CD", "RES", "ACC" })
            {
                groupBox1.Controls.Find(stat + "compBefore", false).FirstOrDefault().Text = old[stat].ToString() + "%";
                groupBox1.Controls.Find(stat + "compAfter", false).FirstOrDefault().Text = load[stat].ToString() + "%";
                groupBox1.Controls.Find(stat + "compDiff", false).FirstOrDefault().Text = (load[stat] - old[stat]).ToString();
            }

            foreach (string extra in Build.extraNames)
            {
                groupBox1.Controls.Find(extra + "compBefore", false).FirstOrDefault().Text = old.ExtraValue(extra).ToString();
                groupBox1.Controls.Find(extra + "compAfter", false).FirstOrDefault().Text = load.ExtraValue(extra).ToString();
                groupBox1.Controls.Find(extra + "compDiff", false).FirstOrDefault().Text = (load.ExtraValue(extra) - old.ExtraValue(extra)).ToString();

            }
        }

        private void ShowRunes(Rune[] rune)
        {
            runeControl1.SetRune(rune[0]);
            runeControl2.SetRune(rune[1]);
            runeControl3.SetRune(rune[2]);
            runeControl4.SetRune(rune[3]);
            runeControl5.SetRune(rune[4]);
            runeControl6.SetRune(rune[5]);

            foreach (RuneControl tc in runes)
            {
                if (tc.Tag != null)
                {
                    tc.Show();
                }
                else
                {
                    tc.Hide();
                }
            }
        }

        public int LoadMons(string fname)
        {
            string text = File.ReadAllText(fname);

            MonsterStat.monStats = JsonConvert.DeserializeObject<List<MonsterStat>>(text);

            return text.Length;
        }

        public int LoadFile(string fname)
        {
            string text = File.ReadAllText(fname);
            dataMonsterList.Items.Clear();
            dataRuneList.Items.Clear();
            listView4.Items.Clear();

            LoadJSON(text);

            checkLocked();
            return text.Length;
        }

        public int LoadBuilds(string fname)
        {
            string text = File.ReadAllText(fname);
            LoadBuildJSON(text);
            return text.Length;
        }

        public void LoadBuildJSON(string text)
        {
            builds = JsonConvert.DeserializeObject<List<Build>>(text);
            if (builds.Count > 0 && (data == null || data.Monsters == null))
            {
                // backup, just in case
                string destFile = Path.Combine("", string.Format("{0}.backup{1}", "builds", ".json"));
                int num = 2;
                while (File.Exists(destFile))
                {
                    destFile = Path.Combine("", string.Format("{0}.backup{1}{2}", "builds", num, ".json"));
                    num++;
                }

                File.Copy("builds.json", destFile);
                if (MessageBox.Show("Couldn't locate save.json loading builds.json.\r\nManually locate file?", "Load Builds", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    loadSaveDialogue(null, new EventArgs());
                }
                else
                {
                    MessageBox.Show("Couldn't load monster stats, builds may not work");
                }
            }

            foreach (Build b in builds)
            {
                if (data != null)
                {
                    var bnum = buildList.Items.Cast<ListViewItem>().Select(it => it.Tag as Build).Where(d => d.MonName == b.MonName).Count();
                    // if there is a build with this monname, maybe I have 2 mons with that name?!
                    b.mon = data.GetMonster(b.MonName, bnum + 1);
                }
                else
                {
                    b.mon = new Monster();
                    b.mon.Name = b.MonName;
                }

                int id = b.ID;
                if (b.ID == 0)
                {
                    id = buildList.Items.Count + 1;
                    b.ID = id;
                }
                if (b.priority == 0)
                {
                    b.priority = buildList.Items.Count + 1;
                }
                ListViewItem li = new ListViewItem(new string[] { b.priority.ToString(), id.ToString(), b.mon.Name, "", b.mon.ID.ToString() });
                li.Tag = b;
                buildList.Items.Add(li);

                // ask the enumerable to eat Linq. Unsure why Find(b.mon.Name, false/true) failed here.
                var lv1li = dataMonsterList.Items.Cast<ListViewItem>().Where(i => i.SubItems.Cast<ListViewItem.ListViewSubItem>().Where(s => s.Text == b.mon.ID.ToString()).Count() > 0).FirstOrDefault();
                if (lv1li != null)
                {
                    lv1li.ForeColor = Color.Green;
                }

                // upgrade builds, hopefully
                while (b.VERSIONNUM < Create.VERSIONNUM)
                {
                    switch (b.VERSIONNUM)
                    {
                        case 0: // unversioned to 1
                            b.Threshold = b.Maximum;
                            b.Maximum = new Stats();
                            break;

                    }
                    b.VERSIONNUM++;
                }
            }
        }

        public void LoadJSON(string text)
        {
            //string save = System.IO.File.ReadAllText("..\\..\\save.json");

            data = JsonConvert.DeserializeObject<Save>(text);
            foreach (Monster mon in data.Monsters)
            {
                var equipedRunes = data.Runes.Where(r => r.AssignedId == mon.ID);

                mon.inStorage = (mon.Name.IndexOf("In Storage") >= 0);
                mon.Name = mon.Name.Replace(" (In Storage)", "");

                foreach (Rune rune in equipedRunes)
                {
                    mon.ApplyRune(rune);
                    rune.AssignedName = mon.Name;
                }

                if (mon.priority == 0 && mon.Current.runeCount > 0)
                {
                    mon.priority = priority++;
                }

                var stats = mon.GetStats();

                string pri = "";
                if (mon.priority != 0)
                    pri = mon.priority.ToString();

                ListViewItem item = new ListViewItem(new string[]{
                    mon.Name,
                    mon.ID.ToString(),
                    pri,
                });
                if (mon.inStorage)
                    item.ForeColor = Color.Gray;

                item.Tag = mon;
                dataMonsterList.Items.Add(item);
            }
            foreach (Rune rune in data.Runes)
            {
                ListViewItem item = new ListViewItem(new string[]{
                    rune.Set.ToString(),
                    rune.ID.ToString(),
                    rune.Grade.ToString(),
                    Rune.StringIt(rune.MainType, true),
                    rune.MainValue.ToString()
                });
                item.Tag = rune;
                if (rune.Locked)
                    item.BackColor = Color.Red;
                dataRuneList.Items.Add(item);
            }

            if (data.shrines == null)
                data.shrines = new Stats();

            if (config != null)
            {
                if (config.AppSettings.Settings.AllKeys.Contains("shrineSpeed"))
                {
                    int val = 0;
                    int.TryParse(config.AppSettings.Settings["shrineSpeed"].Value, out val);
                    data.shrines.Speed = val;
                    int level = (int)Math.Floor(val / (double)1.5);
                    ((ToolStripMenuItem)speedToolStripMenuItem.DropDownItems[level]).Checked = true;
                }
            }
        }

        public void checkLocked()
        {
            if (data == null)
                return;
            if (data.Runes != null)
                toolStripStatusLabel1.Text = "Locked: " + data.Runes.Where(r => r.Locked == true).Count().ToString();

            this.Invoke((MethodInvoker)delegate
            {
                foreach (ListViewItem li in dataRuneList.Items)
                {
                    var rune = li.Tag as Rune;
                    if (rune != null)
                        li.BackColor = rune.Locked ? Color.Red : Color.Transparent;
                }
            });
        }

        private void RunBuild(ListViewItem pli, bool saveStats = false)
        {
            if (plsDie)
                return;

            if (currentBuild != null)
                currentBuild.isRun = false;

            if (pli.Tag != null)
            {
                Stopwatch buildTime = Stopwatch.StartNew();
                Build b = (Build)pli.Tag;
                currentBuild = b;

                ListViewItem[] olvs = null;
                Invoke((MethodInvoker)delegate { olvs = loadoutList.Items.Find(b.ID.ToString(), false); });

                if (olvs.Length > 0)
                {
                    var olv = olvs.First();
                    Loadout ob = (Loadout)olv.Tag;
                    foreach (Rune r in ob.runes)
                    {
                        r.Locked = false;
                    }
                }

                b.GenRunes(data, false, useEquipped, saveStats);
                b.shrines = data.shrines;

                string nR = "";

                for (int i = 0; i < b.runes.Length; i++)
                {
                    if (b.runes[i] != null && b.runes[i].Length == 0)
                        nR += (i + 1) + " ";
                }

                if (nR != "")
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        pli.SubItems[3].Text = ":( " + nR + "Runes";
                    });
                    return;
                }

                b.GenBuilds(0, 0, (str) =>
                {
                    if (!IsDisposed && IsHandleCreated)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            pli.SubItems[3].Text = str;
                        });
                    }
                }, null, true, saveStats);

                if (b.Best == null)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (saveStats)
                        {
                            if (StatsExcelBind(true))
                            {
                                StatsExcelBuild(b, b.mon, null);
                                StatsExcelSave();
                            }
                            StatsExcelBind(true);
                        }
                    });
                    return;
                }

                builds.Add(b);

                int numchanged = 0;
                int numnew = 0;
                int powerup = 0;
                int upgrades = 0;
                foreach (Rune r in b.Best.Current.runes)
                {
                    r.Locked = true;
                    if (r.AssignedName != b.Best.Name)
                    {
                        //if (b.mon.Current.runes[r.Slot - 1] == null)
                        if (r.AssignedName == "Unknown name" || r.AssignedName == "Inventory")
                            numnew++;
                        else
                            numchanged++;
                    }
                    powerup += Math.Max(0, b.Best.Current.FakeLevel[r.Slot - 1] - r.Level);
                    if (b.Best.Current.FakeLevel[r.Slot - 1] != 0)
                    {
                        int tup = (int)Math.Floor(Math.Min(12, b.Best.Current.FakeLevel[r.Slot - 1]) / (double)3);
                        int cup = (int)Math.Floor(Math.Min(12, r.Level) / (double)3);
                        upgrades += Math.Max(0, tup - cup);
                    }
                }
                currentBuild = null;
                b.Time = buildTime.ElapsedMilliseconds;
                buildTime.Stop();

                this.Invoke((MethodInvoker)delegate
                {
                    checkLocked();

                    if (saveStats)
                    {
                        if (StatsExcelBind(true))
                        {
                            StatsExcelBuild(b, b.mon, b.Best.Current);
                            StatsExcelSave();
                        }
                        StatsExcelBind(true);
                    }
                    if (b.buildUsage != null)
                        b.buildUsage.loads.Clear();
                    if (b.runeUsage != null)
                    {
                        b.runeUsage.runesGood.Clear();
                        b.runeUsage.runesUsed.Clear();
                    }
                    b.runeUsage = null;
                    b.buildUsage = null;


                    ListViewItem nli;

                    var lvs = loadoutList.Items.Find(b.ID.ToString(), false);

                    if (lvs.Length == 0)
                        nli = new ListViewItem();
                    else
                        nli = lvs.First();

                    nli.Tag = b.Best.Current;
                    nli.Text = b.ID.ToString();
                    nli.Name = b.ID.ToString();
                    while (nli.SubItems.Count < 6)
                        nli.SubItems.Add("");
                    nli.SubItems[0] = new ListViewItem.ListViewSubItem(nli, b.ID.ToString());
                    nli.SubItems[1] = new ListViewItem.ListViewSubItem(nli, b.Best.Name);
                    nli.SubItems[2] = new ListViewItem.ListViewSubItem(nli, b.Best.ID.ToString());
                    nli.SubItems[3] = new ListViewItem.ListViewSubItem(nli, (numnew + numchanged).ToString());
                    if (Main.config.AppSettings.Settings.AllKeys.Contains("splitassign"))
                    {
                        bool check = false;
                        bool.TryParse(Main.config.AppSettings.Settings["splitassign"].Value, out check);
                        if (check)
                            nli.SubItems[3].Text = numnew.ToString() + "/" + numchanged.ToString();
                    }
                    nli.SubItems[4] = new ListViewItem.ListViewSubItem(nli, powerup.ToString());
                    //nli.SubItems[5] = new ListViewItem.ListViewSubItem(nli, upgrades.ToString());
                    nli.SubItems[5] = new ListViewItem.ListViewSubItem(nli, (b.Time / (double)1000).ToString("0.##"));
                    if (b.Time / (double)1000 > 20)
                        nli.SubItems[5].ForeColor = Color.Orange;
                    else if (b.Time / (double)1000 > 60)
                        nli.SubItems[5].ForeColor = Color.Red;

                    if (lvs.Length == 0)
                        loadoutList.Items.Add(nli);

                });

            }

        }

        private void SaveBuilds(string fname = "builds.json")
        {
            builds.Clear();

            var lbs = buildList.Items;

            foreach (ListViewItem li in lbs)
            {
                var bb = (Build)li.Tag;
                if (bb.mon.Name != "Missingno")
                {
                    if (!bb.DownloadAwake || data.GetMonster(bb.mon.Name).Name != "Missingno")
                        bb.MonName = bb.mon.Name;
                    else
                    {
                        if (data.GetMonster(bb.mon.ID).Name != "Missingno")
                            bb.MonName = data.GetMonster(bb.mon.ID).Name;
                    }
                }
                builds.Add(bb);
            }

            var str = JsonConvert.SerializeObject(builds);
            File.WriteAllText(fname, str);
        }

        private void ClearLoadouts()
        {
            foreach (ListViewItem li in loadoutList.Items)
            {
                Loadout l = (Loadout)li.Tag;

                foreach (Rune r in l.runes)
                {
                    r.Locked = false;
                }

                loadoutList.Items.Remove(li);
            }
            //builds.Clear();
            checkLocked();
        }

        private void RunBuilds(bool skipLoaded, int runTo = -1)
        {
            if (data == null)
                return;
            if (runTask != null && runTask.Status == TaskStatus.Running)
            {
                runSource.Cancel();
                if (currentBuild != null)
                    currentBuild.isRun = false;
                plsDie = true;
                return;
            }
            plsDie = false;

            List<int> loady = new List<int>();

            if (skipLoaded)
            {
                // collect loadouts
                foreach (ListViewItem li in loadoutList.Items)
                {
                    Loadout load = li.Tag as Loadout;

                    var monid = int.Parse(li.SubItems[2].Text);
                    var bid = int.Parse(li.SubItems[0].Text);

                    if (load != null)
                        loady.Add(bid);
                }
            }
            else
            {
                ClearLoadouts();
            }

            bool collect = true;
            int newPri = 1;
            // collect the builds
            List<ListViewItem> list5 = new List<ListViewItem>();
            foreach (ListViewItem li in buildList.Items)
            {
                li.SubItems[0].Text = newPri.ToString();
                (li.Tag as Build).priority = newPri++;

                if (loady.Contains((li.Tag as Build).ID))
                    continue;

                if ((li.Tag as Build).ID == runTo)
                    collect = false;

                if (collect)
                    list5.Add(li);

                li.SubItems[3].Text = "";
            }

            if (makeStats)
                StatsExcelBind();

            runSource = new CancellationTokenSource();
            runToken = runSource.Token;
            runTask = Task.Factory.StartNew(() =>
            {
                if (data.Runes != null)
                    foreach (Rune r in data.Runes)
                    {
                        r.Swapped = false;
                        r.ResetStats();
                    }

                foreach (ListViewItem li in list5)
                {
                    RunBuild(li, makeStats);
                }

                if (makeStats)
                {
                    GenerateStats();
                    this.Invoke((MethodInvoker)delegate {
                        if (!skipLoaded)
                            StatsExcelRunes();
                        //GenerateExcel(); 
                        try
                        {
                            StatsExcelSave();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    });
                }
                checkLocked();
            }, runSource.Token);
        }

        void StatsExcelClear()
        {
            if (data == null || data.Runes == null)
                return;

            FileInfo newFile = new FileInfo(@"runestats.xlsx");
            int status = 0;
            while (status != 1)
            {
                try
                {
                    newFile.Delete();
                    status = 1;
                }
                catch (Exception e)
                {
                    if (status == 0)
                    {
                        if (MessageBox.Show("Please close runestats.xlsx\r\nOr ensure you can overwrite it.", "RuneStats", MessageBoxButtons.RetryCancel) == DialogResult.Cancel)
                        {
                            status = 1;
                        }
                    }
                    else
                        Console.WriteLine(e);
                }
            }

        }

        bool StatsExcelBind(bool passive = false)
        {
            if (data == null || data.Runes == null)
                return false;

            if (gotExcelPack)
                return true;

            int status = 0;
            do
            {
                try
                {
                    excelPack = new ExcelPackage(excelFile);
                    status = 1;
                }
                catch (Exception e)
                {
                    // don't care if no bind
                    if (passive)
                        return false;
                    if (status == 0)
                    {
                        if (MessageBox.Show("Please close runestats.xlsx\r\nOr ensure you can overwrite it.", "RuneStats", MessageBoxButtons.RetryCancel) == DialogResult.Cancel)
                        {
                            status = 1;
                            return false;
                        }
                    }
                    else
                        Console.WriteLine(e);

                }
            }
            while (status != 1);

            excelSheets = excelPack.Workbook.Worksheets;

            var hws = excelSheets.Where(w => w.Name == "Home").FirstOrDefault();
            if (hws == null)
                hws = excelSheets.Add("Home");
            excelSheets.MoveBefore(hws.Index, 1);

            linkCol = 2;

            gotExcelPack = true;

            return true;
        }

        public void StatsExcelSave()
        {
            try
            {
                excelPack.Save();
                //gotExcelPack = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void StatsExcelBuild(Build build, Monster mon, Loadout load)
        {
            // TODO
            Console.WriteLine("Writing build");
            
            ExcelWorksheet ws = null;

            int ptries = 0;
            int ind = -1;

            do
            {
                ws = excelSheets.Where(w => w.Name == mon.Name + (ptries == 0 ? "" : ptries.ToString())).FirstOrDefault();

                if (ws != null)
                {
                    bool trashit = true;
                    if (ws.Cells[1, 9].Value != null)
                    {
                        string pids = ws.Cells[1, 9].Value.ToString();
                        if (pids != "")
                        {
                            int pid = 0;
                            if (int.TryParse(pids, out pid))
                            {
                                if (pid == mon.ID)
                                    trashit = true;
                                else
                                    trashit = false;
                            }
                            else
                                trashit = true;
                        }
                        else
                            trashit = true;
                    }

                    if (trashit)
                    {
                        ind = ws.Index;
                        excelSheets.Delete(mon.Name + (ptries == 0 ? "" : ptries.ToString()));
                        ws = null;
                    }
                    else
                        ptries++;
                }
            }
            while (ws != null);

            ws = excelPack.Workbook.Worksheets.Add(mon.Name + (ptries == 0 ? "" : ptries.ToString()));
            if (ind != -1)
                excelSheets.MoveBefore(ws.Index, ind);

            int row = 1;
            int col = 1;

            // number of good builds?
            ws.Cells[row, 1].Value = "Builds";
            ws.Cells[row, 2].Value = "Bad";
            ws.Cells[row, 3].Value = "Good";

            ws.Cells[row, 6].Value = DateTime.Now;
            ws.Cells[row, 6].Style.Numberformat.Format = "dd-MM-yy";

            ws.Cells[row, 7].Value = build.Time / 1000;
            ws.Cells[row, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
            if (build.Time / 1000 < 2)
                ws.Cells[row, 7].Style.Fill.BackgroundColor.SetColor(Color.MediumTurquoise);
            else if (build.Time / 1000 < 15)
                ws.Cells[row, 7].Style.Fill.BackgroundColor.SetColor(Color.LimeGreen);
            else if (build.Time / 1000 < 60)
                ws.Cells[row, 7].Style.Fill.BackgroundColor.SetColor(Color.Orange);
            else
                ws.Cells[row, 7].Style.Fill.BackgroundColor.SetColor(Color.Red);

            ws.Cells[row, 8].Hyperlink = new ExcelHyperLink("Home!A1", "Home");
            ws.Cells[row, 8].Style.Font.UnderLine = true;
            ws.Cells[row, 8].Style.Font.Color.SetColor(Color.Blue);
            
            ws.Cells[1, 9].Value = mon.ID;

            var runeSheet = excelSheets.Where(w => w.Name == "Home").FirstOrDefault();
            if (runeSheet != null)
            {
                string hl = ws.Name + "!A1";
                if (hl.IndexOf(' ') != -1)
                    hl = "'" + ws.Name + "'!A1";
                runeSheet.Cells[build.priority + 1, linkCol].Hyperlink = new ExcelHyperLink(hl, mon.Name);
                runeSheet.Cells[build.priority + 1, linkCol].Style.Font.UnderLine = true;
                runeSheet.Cells[build.priority + 1, linkCol].Style.Font.Color.SetColor(Color.Blue);

                runeSheet.Cells[build.priority + 1, linkCol + 1].Value = build.Time / (double)1000;
                runeSheet.Cells[build.priority + 1, linkCol + 1].Style.Numberformat.Format = "0.00";
            }
            else
            {
                Console.WriteLine("No Home sheet");
            }

            row++;

            ws.Cells[row, 2].Value = build.buildUsage.failed;
            ws.Cells[row, 3].Value = build.buildUsage.passed;

            build.buildUsage.loads = build.buildUsage.loads.OrderByDescending(m => Build.sort(build, m.GetStats())).ToList();

            double scoreav = 0;
            int c = 0;
            Stats minav = new Stats();
            foreach (var b in build.buildUsage.loads)
            {
                double sc = Build.sort(build, b.GetStats());
                b.score = sc;
                scoreav += sc;
                minav += b.GetStats();
                c++;
            }
            scoreav /= (double)c;
            minav /= c;

            ws.Cells[row - 1, 4].Value = scoreav;

            Stats lowQ = new Stats();
            foreach (var s in Build.statNames)
            {
                c = 0;
                foreach (var b in build.buildUsage.loads)
                {
                    if (minav[s] > b.GetStats()[s])
                    {
                        lowQ[s] += b.GetStats()[s];
                        c++;
                    }
                }
                lowQ[s] /= (double)c;
            }

            Stats versus = new Stats();
            bool enough = false;

            foreach (var s in Build.statNames)
            {
                if (build.Minimum[s] != 0)
                {
                    versus[s] = lowQ[s];
                    if (build.buildUsage.loads.Where(m => versus < m.GetStats()).Count() < 0.25 * build.buildUsage.passed)
                    {
                        enough = true;
                        break;
                    }
                }
            }

            if (!enough)
            {
                foreach (var s in Build.statNames)
                {
                    if (build.Sort[s] != 0)
                    {
                        versus[s] = lowQ[s];
                        if (build.buildUsage.loads.Where(m => versus < m.GetStats()).Count() < 0.25 * build.buildUsage.passed)
                        {
                            enough = true;
                            break;
                        }
                    }
                }
            }


            ws.Cells[row, 4].Value = build.buildUsage.loads.Where(m => versus < m.GetStats()).Count();

            var trow = row;
            row++;

            foreach (var stat in Build.statNames)
            {
                ws.Cells[row, 1].Value = stat;
                if (build.Minimum[stat] > 0 || build.Sort[stat] != 0)
                {
                    ws.Cells[row, 2].Value = build.Minimum[stat];
                    ws.Cells[row, 3].Value = build.Sort[stat];
                    ws.Cells[row, 4].Value = versus[stat];
                }
                row++;
            }

            col = 5;
            row = trow;

            foreach (var b in build.buildUsage.loads.Take(300))
            {
                row = trow;
                ws.Cells[row, col].Value = b.score;// Build.sort(build, b.GetStats());
                if (b.score < scoreav)
                {
                    ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                }
                row++;
                foreach (var stat in Build.statNames)
                {
                    ws.Cells[row, col].Value = b.GetStats()[stat];
                    row++;
                }
                col++;
            }

            row++;
            col = 1;

            ws.Cells[row, 2].Value = "Good Av";
            ws.Cells[row, 3].Value = "Bad Av";
            ws.Cells[row, 4].Value = "Good Mult";

            row++;
            StatsExcelRune(ws, ref row, ref col, 0, build, load);
            row++;
            StatsExcelRune(ws, ref row, ref col, -1, build, load);
            row++;
            StatsExcelRune(ws, ref row, ref col, -2, build, load);
            row++;
            for (int slot = 1; slot < 7; slot++)
            {
                StatsExcelRune(ws, ref row, ref col, slot, build, load);
                row++;
            }
            Console.WriteLine("Finished Writing");
        }

        public void StatsExcelRunes()
        {
            if (data == null || data.Runes == null || excelPack == null)
                return;

            var ws = excelSheets.Where(w => w.Name == "Runes").FirstOrDefault();
            if (ws == null)
            {
                ws = excelSheets.Add("Runes");
                excelSheets.MoveAfter(ws.Index, 1);
            }
            else
            {
                var ind = ws.Index;
                excelSheets.Delete("Runes");
                ws = excelPack.Workbook.Worksheets.Add("Runes");
                excelSheets.MoveBefore(ws.Index, ind);
            }

            int row = 1;
            int col = 1;
            foreach (var th in "Id,Grade,Set,Slot,MType,Level,Select,Rune,Type,Load,Gen,Eff,Used,Points,Flats,FlatPts,Sell,Action, ,Main,Innate,1,2,3,4,HPpts,ATKpts,Pts".Split(','))
            {
                ws.Cells[row, col].Value = th; col++;
            }
            //linkCol = col + 1;
            row++;
            col = 1;

            int cmax = 1;

            foreach (Rune r in data.Runes.OrderByDescending(r => r.ScoringBad))
            {
                ws.Cells[row, col].Value = r.ID; col++;

                ws.Cells[row, col].Value = r.Grade; col++;

                if (r.Rarity > 0)
                {
                    Color color = Color.FromArgb(255, 146, 208, 80);
                    if (r.Rarity == 4) color = Color.FromArgb(255, 255, 153, 0);
                    else if (r.Rarity == 3) color = Color.FromArgb(255, 204, 0, 153);
                    else if (r.Rarity == 2) color = Color.FromArgb(255, 102, 205, 255);

                    ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(color);
                }
                ws.Cells[row, col].Value = r.Set; col++;

                ws.Cells[row, col].Value = r.Slot; col++;
                ws.Cells[row, col].Value = r.MainType.ToGameString(); col++;

                ws.Cells[row, col].Value = r.Level; col++;

                //ws.Cells[row, col].Style.Numberformat.Format = "[>0]0.##;";
                ws.Cells[row, col].Value = r.manageStats_Set; col++;
                //ws.Cells[row, col].Style.Numberformat.Format = "[>0]0.##;";
                ws.Cells[row, col].Value = r.manageStats_RuneFilt; col++;
                //ws.Cells[row, col].Style.Numberformat.Format = "[>0]0.##;";
                ws.Cells[row, col].Value = r.manageStats_TypeFilt; col++;

                //ws.Cells[row, col].Style.Numberformat.Format = "[>0]0.##;";
                ws.Cells[row, col].Value = r.manageStats_LoadFilt; col++;
                //ws.Cells[row, col].Style.Numberformat.Format = "[>0]0.##;";
                ws.Cells[row, col].Value = r.manageStats_LoadGen; col++;

                ws.Cells[row, col].Style.Numberformat.Format = "0.00%";
                ws.Cells[row, col].Value = r.Efficiency; col++;

                ws.Cells[row, col].Value = (r.manageStats_In ? "TRUE" : "FALSE"); col++;

                ws.Cells[row, col].Value = r.ScoringBad; col++;

                //ws.Cells[row, col].Style.Numberformat.Format = "[>0]0.##;";
                ws.Cells[row, col].Value = r.FlatCount(); col++;
                ws.Cells[row, col].Style.Numberformat.Format = "[>0]0.00;";
                ws.Cells[row, col].Value = r.FlatPoints(); col++;

                ws.Cells[row, col].Value = r.ScoringSell; col++;

                ws.Cells[row, col].Value = r.ScoringAct; col++;

                col++;

                ws.Cells[row, col].Value = r.MainValue.ToString() + " " + r.MainType.ToGameString(); col++;
                if (r.InnateType != Attr.Null)
                    ws.Cells[row, col].Value = r.InnateValue.ToString() + " " + r.InnateType.ToGameString(); col++;
                if (r.Sub1Type != Attr.Null)
                    ws.Cells[row, col].Value = r.Sub1Value.ToString() + " " + r.Sub1Type.ToGameString(); col++;
                if (r.Sub2Type != Attr.Null)
                    ws.Cells[row, col].Value = r.Sub2Value.ToString() + " " + r.Sub2Type.ToGameString(); col++;
                if (r.Sub3Type != Attr.Null)
                    ws.Cells[row, col].Value = r.Sub3Value.ToString() + " " + r.Sub3Type.ToGameString(); col++;
                if (r.Sub4Type != Attr.Null)
                    ws.Cells[row, col].Value = r.Sub4Value.ToString() + " " + r.Sub4Type.ToGameString(); col++;

                ws.Cells[row, col].Style.Numberformat.Format = "0.00%";
                ws.Cells[row, col].Value = r.ScoringHP; col++;
                ws.Cells[row, col].Style.Numberformat.Format = "0.00%";
                ws.Cells[row, col].Value = r.ScoringATK; col++;
                ws.Cells[row, col].Style.Numberformat.Format = "0.00%";
                ws.Cells[row, col].Value = r.ScoringRune; col++;

                row++;
                cmax = col;
                col = 1;
            }

            var table = ws.Tables.Where(t => t.Name == "RuneTable").FirstOrDefault();
            if (table == null)
                table = ws.Tables.Add(ws.Cells[1, 1, row - 1, cmax - 1], "RuneTable");

            if (table.Address.Columns != cmax - 1 || table.Address.Rows != row - 1)
            {
                var start = table.Address.Start;
                var newRange = string.Format("{0}:{1}", start.Address, new ExcelAddress(1, 1, row - 1, cmax - 1));

                var tableElement = table.TableXml.DocumentElement;
                tableElement.Attributes["ref"].Value = newRange;
                tableElement["autoFilter"].Attributes["ref"].Value = newRange;

            }

            table.ShowHeader = true;
            table.StyleName = "TableStyleMedium2";
            // write rune stats

        }

        void StatsExcelRune(ExcelWorksheet ws, ref int row, ref int col, int slot, Build build, Loadout load)
        {
            string sslot = slot.ToString();
            if (slot == -1)
                sslot = "o";
            else if (slot == -2)
                sslot = "e";
            else if (slot == 0)
                sslot = "g";

            int wrote = 0;

            Func<int, bool> aslot = (a) => { return true; };
            if (slot > 0)
            {
                aslot = (a) => { return a == slot; };
            }
            else if (slot == -1)
            {
                aslot = (a) => { return a % 2 == 1; };
            }
            else if (slot == -2)
            {
                aslot = (a) => { return a % 2 == 0; };
            }
            var used = build.runeUsage.runesUsed.Select(r => r.Key);
            var usedSlot = used.Where(r => aslot(r.Slot));
            var good = build.runeUsage.runesGood.Select(r => r.Key);
            var goodSlot = good.Where(r => aslot(r.Slot));

            foreach (var r in usedSlot)
            {
                /*int pred = build.runePrediction.ContainsKey("g") ? build.runePrediction["g"].Key : 0;

                if (build.runePrediction.ContainsKey(r.Slot.ToString()))
                    pred = build.runePrediction[r.Slot.ToString()].Key;
                else if (r.Slot > 0 && build.runePrediction.ContainsKey(r.Slot % 2 == 0 ? "e" : "o"))
                    pred = build.runePrediction[r.Slot % 2 == 0 ? "e" : "o"].Key;*/

                r.buildScoreTemp = build.ScoreRune(r, build.GetPredict(r), false);
            }

            col = 5;
            ws.Cells[row, 1].Value = "Set";
            ws.Cells[row + 1, 1].Value = "Primary";
            row--;
            //ws.Cells[row, 2].Value = "Filter";
            foreach (var r in usedSlot.OrderByDescending(r => goodSlot.Contains(r)).ThenByDescending(r => r.buildScoreTemp))
            {
                ws.Cells[row, col].Value = r.buildScoreTemp;
                row++;
                ws.Cells[row, col].Value = r.Set.ToString();
                row++;
                ws.Cells[row, col].Value = r.MainType.ToGameString();
                row--;
                row--;
                col++;
            }
            row += 3;
            col = 1;


            for (int i = 0; i < Build.statNames.Length; i++)
            {
                var stat = Build.statNames[i];

                if (i <= 3) //SPD
                {
                    var qqw = new Dictionary<string, RuneFilter>();
                    bool writeIt = false;
                    if (build.runeFilters.TryGetValue(sslot, out qqw))
                    {
                        if (qqw.ContainsKey(stat))
                        {
                            writeIt = qqw[stat].Flat > 0;
                        }
                    }

                    if (true)
                    {
                        WriteRune(ws, ref row, ref col, stat, "", "flat", load, build, slot);
                        row++;
                        wrote++;
                    }
                    col = 1;
                }
                if (i != 3)
                {
                    var qqw = new Dictionary<string, RuneFilter>();
                    bool writeIt = false;
                    if (build.runeFilters.TryGetValue(sslot, out qqw))
                    {
                        if (qqw.ContainsKey(stat))
                        {
                            writeIt = qqw[stat].Percent > 0;
                        }
                    }

                    if (true)
                    {
                        WriteRune(ws, ref row, ref col, stat, " %", "perc", load, build, slot);
                        row++;
                        wrote++;
                    }
                    col = 1;
                }
            }
            if (wrote > 0)
            {
                if (slot == 0)
                    ws.Cells[row - wrote - 3, col].Value = "Global";
                else if (slot == -1)
                    ws.Cells[row - wrote - 3, col].Value = "Odds";
                else if (slot == -2)
                    ws.Cells[row - wrote - 3, col].Value = "Evens";
                else
                    ws.Cells[row - wrote - 3, col].Value = slot;
            }
        }

        void WriteRune(ExcelWorksheet ws, ref int row, ref int col, string stat, string hpref, string ssuff, Loadout load, Build build, int slot)
        {
            // slot 0 = global, -1 = odd, -2 even
            Func<int, bool> aslot = (a) => { return true; };
            string sslot = "g";
            if (slot > 0)
            {
                aslot = (a) => { return a == slot; };
                sslot = slot.ToString();
            }
            else if (slot == -1)
            {
                aslot = (a) => { return a % 2 == 1; };
                sslot = "o";
            }
            else if (slot == -2)
            {
                aslot = (a) => { return a % 2 == 0; };
                sslot = "e";
            }

            ws.Cells[row, col].Value = stat + hpref;
            col++;


            var used = build.runeUsage.runesUsed.Select(r => r.Key);
            var usedSlot = used.Where(r => aslot(r.Slot));
            var usedFilt = usedSlot.Where(r => r[stat + ssuff, build.GetPredict(r), false] != 0);

            var good = build.runeUsage.runesGood.Select(r => r.Key);
            var goodSlot = good.Where(r => aslot(r.Slot));
            var goodFilt = goodSlot.Where(r => r[stat + ssuff, build.GetPredict(r), false] != 0);

            var badFilt = usedFilt.Except(goodFilt);

            double gav = 0;
            if (goodFilt.Count() > 0)
                gav = goodFilt.Average(r => r[stat + ssuff, build.GetPredict(r), false]);
            ws.Cells[row, col].Value = gav;
            col++;

            double bav = 0;
            if (badFilt.Count() > 0)
                bav = badFilt.Average(r => r[stat + ssuff, build.GetPredict(r), false]);
            ws.Cells[row, col].Value = bav;
            col++;

            double mul = 0;
            //if (goodFilt.Count() > 0)
            if (bav != 0)
                mul = gav / bav; //goodFilt.StandardDeviation(r => r[stat + ssuff, pred, false]);
            ws.Cells[row, col].Value = mul;
            col++;

            double av = 0;
            double std = 0;
            if (goodFilt.Count() > 0)
            {
                av = goodFilt.Average(r => r[stat + ssuff, build.GetPredict(r), false]);
                std = goodFilt.StandardDeviation(r => r[stat + ssuff, build.GetPredict(r), false]);
            }

            if (mul >= 2)
            {
                //    ws.Cells[row, col].Value = "Yes";
                ws.Cells[row, 4].Style.Fill.PatternType = ExcelFillStyle.MediumGray;
                ws.Cells[row, 4].Style.Fill.BackgroundColor.SetColor(Color.LawnGreen);
            }
            //col++;

            //ws.Cells[row, col].Value = usedFilt.Count();
            //col++;

            //ws.Cells[row, col].Value = usedFilt.Where(r => r[stat + ssuff, pred, false] >= av - std).Count();
            //col++;

            //ws.Cells[row, col].Value = (av - std).ToString("0.##");
            //col++;

            //col++;
            foreach (var r in usedSlot.OrderByDescending(r => goodSlot.Contains(r)).ThenByDescending(r => r.buildScoreTemp))
            {
                /*
                int pred = build.runePrediction.ContainsKey("g") ? build.runePrediction["g"].Key : 0;

                if (build.runePrediction.ContainsKey(sslot))
                    pred = build.runePrediction[sslot].Key;
                else if (slot > 1 && build.runePrediction.ContainsKey(slot % 2 == 0 ? "e" : "o"))
                    pred = build.runePrediction[slot % 2 == 0 ? "e" : "o"].Key;
                    */

                var rval = r[stat + ssuff, build.GetPredict(r), false];

                if (rval > 0)
                {
                    ws.Cells[row, col].Value = rval;
                    if (load != null && load.runes.Contains(r))
                    {
                        ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.MediumTurquoise);
                        if (rval < av - std)
                            ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.MediumGray;
                    }
                    else if (build.runeUsage.runesGood.ContainsKey(r))
                    {
                        ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LimeGreen);
                        if (rval < av - std)
                            ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.MediumGray;

                    }
                    else if (rval < av - std)
                    {
                        ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.Red);
                    }
                }
                col++;
            }

        }

        void GenerateStats()
        {
            if (data == null || data.Runes == null)
                return;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Id,Grade,Set,Slot,MainType,Level,Select,Rune,Type,Load,Gen,Eff,Used,Points,FlatCount,FlatPts,SellScore,Action");

            foreach (Rune r in data.Runes.OrderByDescending(r => r.ScoringBad))
            {
                sb.Append(r.ID + ",");

                sb.Append(r.Grade + ",");

                sb.Append(r.Set + ",");
                sb.Append(r.Slot + ",");
                sb.Append(r.MainType + ",");

                sb.Append(r.Level + ",");

                sb.Append(r.manageStats_Set + ",");
                sb.Append(r.manageStats_RuneFilt + ",");
                sb.Append(r.manageStats_TypeFilt + ",");

                sb.Append(r.manageStats_LoadFilt + ",");
                sb.Append(r.manageStats_LoadGen + ",");

                sb.Append(r.Efficiency.ToString("0.####") + ",");

                sb.Append((r.manageStats_In ? "TRUE" : "FALSE") + ",");

                sb.Append(r.ScoringBad.ToString() + ",");

                sb.Append(r.FlatCount().ToString() + ",");
                sb.Append(r.FlatPoints().ToString("0.####") + ",");

                sb.Append(r.ScoringSell.ToString() + ",");

                sb.Append(r.ScoringAct + ",");

                sb.AppendLine();
            }
            int status = 0;
            while (status != 1)
            {
                try
                {
                    File.WriteAllText("runestats.csv", sb.ToString());
                    status = 1;
                }
                catch (Exception e)
                {
                    if (status == 0)
                    {
                        if (MessageBox.Show("Please close runestats.csv\r\nOr ensure you can create a file here.", "RuneStats", MessageBoxButtons.RetryCancel) == DialogResult.Cancel)
                        {
                            status = 1;
                        }
                    }
                    else
                        Console.WriteLine(e);

                }
            }
        }
    }
}
