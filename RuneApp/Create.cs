﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RuneOptim;

namespace RuneApp
{
    // Specifying a new build, are we?
    public partial class Create : Form
    {
        public Build build = null;
        
        // Keep track of the runeset groups for speed swaps
        private ListViewGroup rsInc;
        private ListViewGroup rsExc;
        private ListViewGroup rsReq;

        // keep track of the rune hex controls
        private RuneControl[] runes;

        // the current rune to look at
        private Rune runeTest = null;
        // is the form loading? wouldn't want to trigger any OnChanges, eh?
        private bool loading = false;

        private static string[] tabNames = new string[] { "g", "o", "e", "2", "4", "6", "1", "3", "5" };
        private static string[] statNames = new string[] { "HP", "ATK", "DEF", "SPD", "CR", "CD", "RES", "ACC" };

        public Create()
        {
            InitializeComponent();
            // when show, check we have stuff
            Shown += Create_Shown;
            
            // declare the truthyness of the groups and track them
            listView1.Groups[1].Tag = true;
            rsInc = listView1.Groups[1];
            listView1.Groups[2].Tag = false;
            rsExc = listView1.Groups[2];
            listView1.Groups[0].Tag = false;
            rsReq = listView1.Groups[0];

            // for each runeset, put it in the list as excluded
            foreach (var rs in Enum.GetNames(typeof(RuneSet)))
			{
                if (rs != "Null")
                {
                    ListViewItem li = new ListViewItem(rs);
                    li.Name = rs;
                    li.Tag = Enum.Parse(typeof(RuneSet), rs);
                    li.Group = listView1.Groups[2];
                    listView1.Items.Add(li);
                }
			}

            // track all the clickable rune things
            runes = new RuneControl[] { runeControl1, runeControl2, runeControl3, runeControl4, runeControl5, runeControl6 };

            toolStripButton1.Tag = 0;

            // there are lists on the rune filter tabs 2,4, and 6
            var lists = new ListView[]{listView2, listView4, listView6};
            for(int j = 0; j < lists.Length; j++)
            {
                // mess 'em up
                var lv = lists[j];

                // for all the attributes that may appears as primaries on runes
                for (int i = 0; i < statNames.Length; i++)
                {
					ListViewItem li = null;

                    string stat = statNames[i];
                    if (i < 3)
                    {
                        li = new ListViewItem(stat);
                        li.Name = stat + "flat";
                        li.Text = stat;
                        li.Tag = stat + "flat";
                        li.Group = lv.Groups[1];
                        
                        lv.Items.Add(li);
                        
                        li = new ListViewItem(stat);
                        li.Name = stat + "perc";
                        li.Text = stat + "%";
                        li.Tag = stat + "perc";
                        li.Group = lv.Groups[1];

                        lv.Items.Add(li);
                        
                    }
                    else
                    {
                        // only allow cool stats on the right slots
                        if (j == 0 && stat != "SPD")
                            continue;
                        if (j == 1 && (stat != "CR" && stat != "CD"))
                            continue;
                        if (j == 2 && (stat != "ACC" && stat != "RES"))
                            continue;

                        li = new ListViewItem(stat);
                        // put the right type on it
                        li.Name = stat + (stat == "SPD" ? "flat" : "perc");
                        li.Text = stat;
                        li.Tag = stat + (stat == "SPD" ? "flat" : "perc");
                        li.Group = lv.Groups[1];
                        
                        lv.Items.Add(li);
                    }
					 
                }
            }

            Control textBox;
            Label label;

            // make a grid for the monsters base, min/max stats and the scoring
            
            int x = 0;
            int y = 0;

            y = 94;

            int colWidth = 50;
            int rowHeight = 24;

            foreach (var stat in statNames)
            {
                x = 4; 
                label = new Label();
                groupBox1.Controls.Add(label);
                label.Name = stat + "Label";
                label.Text = stat;
                label.Size = new Size(50, 20);
                label.Location = new Point(x, y);
                x += colWidth;

                label = new Label();
                groupBox1.Controls.Add(label);
                label.Name = stat + "Base";
                label.Text = stat;
                label.Size = new Size(50, 20);
                label.Location = new Point(x, y);
                x += colWidth;

                label = new Label();
                groupBox1.Controls.Add(label);
                label.Name = stat + "Bonus";
                label.Text = "+0";
                label.Size = new Size(50, 20);
                label.Location = new Point(x, y);
                x += colWidth;

                textBox = new TextBox();
                groupBox1.Controls.Add(textBox);
                textBox.Name = stat + "Total";
                textBox.Size = new Size(40, 20);
                textBox.Location = new Point(x, y);
                textBox.TextChanged += new System.EventHandler(this.global_TextChanged);
                textBox.TextChanged += Total_TextChanged;
                x += colWidth;

                label = new Label();
                groupBox1.Controls.Add(label);
                label.Name = stat + "Current";
                label.Text = stat;
                label.Size = new Size(50, 20);
                label.Location = new Point(x, y);
                x += colWidth;

                textBox = new TextBox();
                groupBox1.Controls.Add(textBox);
                textBox.Name = stat + "Worth";
                textBox.Size = new Size(40, 20);
                textBox.Location = new Point(x, y);
                textBox.TextChanged += new System.EventHandler(this.global_TextChanged);
                x += colWidth;

                label = new Label();
                groupBox1.Controls.Add(label);
                label.Name = stat + "CurrentPts";
                label.Text = stat;
                label.Size = new Size((int)(50 * 0.8), 20);
                label.Location = new Point(x, y);
                x += (int)(colWidth * 0.8);

                textBox = new TextBox();
                groupBox1.Controls.Add(textBox);
                textBox.Name = stat + "Max";
                textBox.Size = new Size(40, 20);
                textBox.Location = new Point(x, y);
                textBox.TextChanged += new System.EventHandler(this.global_TextChanged);

                y += rowHeight;
            }

            // put the grid on all the tabs
            foreach (var tab in tabNames)
            {
                TabPage page = tabControl1.TabPages["tab" + tab];
                page.Tag = tab;

                label = new Label();
                page.Controls.Add(label);
                label.Text = "Divide stats into points:";
                label.Name = tab + "divprompt";
                label.Size = new Size(140, 14);
                label.Location = new Point(6, 6);

                label = new Label();
                page.Controls.Add(label);
                label.Text = "Inherited";
                label.Name = tab + "inhprompt";
                label.Size = new Size(60, 14);
                label.Location = new Point(134, 6);

                label = new Label();
                page.Controls.Add(label);
                label.Text = "Current";
                label.Name = tab + "curprompt";
                label.Size = new Size(60, 14);
                label.Location = new Point(214, 6);

                label = new Label();
                page.Controls.Add(label);
                label.Text = "YES";
                label.Name = tab + "Check";
                label.Size = new Size(60, 14);
                label.Location = new Point(370, 250);


                ComboBox filterJoin = new ComboBox();
                filterJoin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                filterJoin.FormattingEnabled = true;
                filterJoin.Items.AddRange(new object[] {
                "Or",
                "And",
                "Sum"});
                filterJoin.Location = new System.Drawing.Point(298, 6);
                filterJoin.Name = tab + "join";
                filterJoin.Size = new System.Drawing.Size(72, 21);
                filterJoin.SelectedIndex = 0;

                filterJoin.SelectionChangeCommitted += filterJoin_SelectedIndexChanged;
                filterJoin.Tag = tab;
                page.Controls.Add(filterJoin);

                bool first = true;

                rowHeight = 25;
                colWidth = 42;

                int testX = 0;
                int testY = 0;
                y = 45;
                foreach (var stat in statNames)
                {
                    label = new Label();
                    page.Controls.Add(label);
                    label.Name = tab + stat;
                    label.Location = new Point(5, y);
                    label.Text = stat;
                    label.Size = new Size(30, 20);

                    x = 35;
                    foreach (var pref in new string[] { "", "i", "c" })
                    {
                        foreach (var type in new string[] { "flat", "perc" })
                        {
                            if (first)
                            {
                                label = new Label();
                                page.Controls.Add(label);
                                label.Name = tab + pref + type;
                                label.Location = new Point(x, 25);
                                label.Text = pref + type;
                                if (type == "flat")
                                    label.Text = "Flat";
                                if (type == "perc")
                                    label.Text = "Percent";
                                label.Size = new System.Drawing.Size(45, 20);


                            }
                            
                            if (type == "perc" && stat == "SPD")
                            {
                                x += colWidth;
                                continue;
                            }
                            if (type == "flat" && (stat == "ACC" || stat == "RES" || stat == "CD" || stat == "CR"))
                            {
                                x += colWidth;
                                continue;
                            }

                            if (pref == "")
                            {
                                textBox = new TextBox();
                                textBox.TextChanged += new System.EventHandler(this.global_TextChanged);
                            }
                            else
                                textBox = new Label();
                            page.Controls.Add(textBox);
                            textBox.Name = tab + pref + stat + type;
                            textBox.Location = new Point(x, y);

                            textBox.Size = new System.Drawing.Size(40, 20);
                            x += colWidth;
                        }
                    }

                    label = new Label();
                    page.Controls.Add(label);
                    label.Name = tab + stat + "gt";
                    label.Location = new Point(x, y);
                    label.Text = ">=";
                    label.Size = new System.Drawing.Size(30, 20);
                    x += colWidth;

                    testX = x;

                    textBox = new TextBox();
                    page.Controls.Add(textBox);
                    textBox.Name = tab + stat + "test";
                    textBox.Location = new Point(x, y);
                    textBox.Text = "";
                    textBox.Size = new System.Drawing.Size(40, 20);
                    textBox.TextChanged += new System.EventHandler(this.global_TextChanged);
                    x += colWidth;

                    label = new Label();
                    page.Controls.Add(label);
                    label.Name = tab + "r" + stat + "test";
                    label.Location = new Point(x, y);
                    label.Text = "10";
                    label.Size = new System.Drawing.Size(30, 20);
                    x += colWidth;

                    y += rowHeight;
                    testY = y;
                    first = false;
                }

                textBox = new TextBox();
                page.Controls.Add(textBox);
                textBox.Name = tab + "test";
                textBox.Location = new Point(testX, testY);
                textBox.Text = "";
                textBox.Size = new System.Drawing.Size(40, 20);
                textBox.TextChanged += new System.EventHandler(this.global_TextChanged);
                // default scoring is OR which doesn't need this box
                textBox.Enabled = false;
                x += colWidth;


            }
        }

        // when the scoring type changes
        void filterJoin_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            string tabName = (string)box.Tag;
            // figure out which tab it's on
            int tabId = tabName == "g" ? 0 : tabName == "e" ? -2 : tabName == "o" ? -1 : int.Parse(tabName);
            TabPage tab = null;
            if (tabId <= 0)
                tab = tabControl1.TabPages[-tabId];
            if (tabId > 0)
            {
                if (tabId % 2 == 0)
                    tab = tabControl1.TabPages[2 + tabId/2];
                else
                    tab = tabControl1.TabPages[6 + tabId / 2];
            }
            Control ctrl;

            foreach (var stat in statNames)
            {
                foreach (var type in new string[] { "flat", "perc" })
                {
                    if (type == "perc" && stat == "SPD")
                        continue;
                    if (type == "flat" && (stat == "ACC" || stat == "RES" || stat == "CD" || stat == "CR"))
                        continue;
                    ctrl = tab.Controls.Find(tabName + stat + type, false).FirstOrDefault();
                    //ctrl.Enabled = (box.SelectedIndex == 2);
                }
                ctrl = tab.Controls.Find(tabName + stat + "test", false).FirstOrDefault();
                ctrl.Enabled = (box.SelectedIndex != 2);
                ctrl = tab.Controls.Find(tabName + "test", false).FirstOrDefault();
                ctrl.Enabled = (box.SelectedIndex == 2);

            }

            int test = 0;
            ctrl = tab.Controls.Find(tabName + "test", false).FirstOrDefault();
            int.TryParse(ctrl.Text, out test);
            
            if (!build.runeScoring.ContainsKey(tabName))
                build.runeScoring.Add(tabName, new KeyValuePair<int, int>(box.SelectedIndex, test));
            var kv = build.runeScoring[tabName] = new KeyValuePair<int, int>(box.SelectedIndex, test);

            // TODO: trim the ZERO nodes on the tree

            // retest the rune
            TestRune(runeTest);
        }

        // When the window is told to appear (hopefully we have everything)
        void Create_Shown(object sender, EventArgs e)
        {
            if (Tag == null)
            {
                // don't have. :(
                Close();
                return;
            }
            // warning, now loading
            loading = true;

			build = (Build)Tag;
            Monster mon = (Monster)build.mon;
            Stats cur = mon.GetStats();
            monLabel.Text = "Build for " + mon.Name + " (" + mon.ID + ")";

            // move the sets around in the list a little
            foreach (RuneSet s in build.BuildSets)
            {
                ListViewItem li = listView1.Items.Find(s.ToString(), true).FirstOrDefault();
                if (li == null)
                    li.Group = rsExc;
                if (li != null)
                    li.Group = rsInc;
            }
            foreach (RuneSet s in build.RequiredSets)
            {
                ListViewItem li = listView1.Items.Find(s.ToString(), true).FirstOrDefault();
                if (li != null)
                    li.Group = rsReq;
            }

            // for 2,4,6 - make sure that the Attrs are set up
            var lists = new ListView[]{listView2, listView4, listView6};
            for (int j = 0; j < lists.Length; j++)
            {
                var lv = lists[j];

				var attrs = Enum.GetValues(typeof(Attr));
				for (int i = 0; i < statNames.Length; i++)
                {
                    var bl = build.slotStats[(j+1)*2 - 1];

					
                    string stat = statNames[i];
                    ListViewItem li = null;
                    if (i < 3)
                    {
                        if (bl.Contains(stat + "flat"))
                            lv.Items.Find(stat + "flat", true).FirstOrDefault().Group = lv.Groups[0];

                        if (bl.Contains(stat + "perc") || build.New)
                            lv.Items.Find(stat + "perc", true).FirstOrDefault().Group = lv.Groups[0];
                    }
                    else
                    {
                        if (j == 0 && stat != "SPD")
                            continue;
                        if (j == 1 && (stat != "CR" && stat != "CD"))
                            continue;
                        if (j == 2 && (stat != "ACC" && stat != "RES"))
                            continue;

                        if (bl.Contains(stat + (stat == "SPD" ? "flat" : "perc")) || build.New)
                            lv.Items.Find(stat + (stat == "SPD" ? "flat" : "perc"), true).FirstOrDefault().Group = lv.Groups[0];
                    }
                }

            }


            statName.Text = mon.Name;
            statID.Text = mon.ID.ToString();
            statLevel.Text = mon.level.ToString();

            // read a bunch of numbers
            foreach (var stat in statNames)
            {
                var ctrlBase = (Label)groupBox1.Controls.Find(stat + "Base", true).FirstOrDefault();
                ctrlBase.Text = mon[stat].ToString();

                var ctrlBonus = (Label)groupBox1.Controls.Find(stat + "Bonus", true).FirstOrDefault();
                var ctrlTotal = (TextBox)groupBox1.Controls.Find(stat + "Total", true).FirstOrDefault();

                ctrlTotal.Tag = new KeyValuePair<Label, Label>(ctrlBase, ctrlBonus);

                var ctrlCurrent = groupBox1.Controls.Find(stat + "Current", true).FirstOrDefault();
                ctrlCurrent.Text = cur[stat].ToString();

                var ctrlWorth = groupBox1.Controls.Find(stat + "Worth", true).FirstOrDefault();
                
                var ctrlMax = groupBox1.Controls.Find(stat + "Max", true).FirstOrDefault();
                
                if (build.Minimum[stat] > 0)
                    ctrlTotal.Text = build.Minimum[stat].ToString();
                if (build.Sort[stat] != 0)
                    ctrlWorth.Text = build.Sort[stat].ToString();
                if (build.Maximum[stat] != 0)
                    ctrlMax.Text = build.Maximum[stat].ToString();

            }

            // do we allow broken sets?
            ChangeBroken(build.AllowBroken);

            // for each tabs filter
            foreach (var rs in build.runeFilters)
            {
                var tab = rs.Key;
                // for each stats filter
                foreach (var f in rs.Value)
                {
                    var stat = f.Key;
                    // for each stat type
                    foreach (var type in new string[] { "flat", "perc", "test" })
                    {
                        // find the controls and shove the value in it
                        var ctrl = this.Controls.Find(tab + stat + type, true).FirstOrDefault();
                        if (ctrl != null)
                        {
                            int val = f.Value[type];
                            // unless it's zero, I don't want zeros
                            if (val != 0)
                                ctrl.Text = val.ToString();
                            else
                                ctrl.Text = "";
                        }
                    }
                }
            }
            // for each tabs scoring
            foreach (var tab in build.runeScoring)
            {
                // find that box
                ComboBox box = (ComboBox)this.Controls.Find(tab.Key + "join", true).FirstOrDefault();
                if (box != null)
                {
                    // manually kajigger it
                    box.SelectedIndex = tab.Value.Key;
                    foreach (var stat in statNames)
                    {
                        Control ctrl;
                        foreach (var type in new string[] { "flat", "perc" })
                        {
                            if (type == "perc" && stat == "SPD")
                                continue;
                            if (type == "flat" && (stat == "ACC" || stat == "RES" || stat == "CD" || stat == "CR"))
                                continue;
                            ctrl = Controls.Find(tab.Key + stat + type, true).FirstOrDefault();
                        }
                        ctrl = Controls.Find(tab.Key + stat + "test", true).FirstOrDefault();
                        ctrl.Enabled = (box.SelectedIndex != 2);
                        ctrl = Controls.Find(tab.Key + "test", true).FirstOrDefault();
                        ctrl.Enabled = (box.SelectedIndex == 2);
                    }
                }

                var tb = (TextBox)this.Controls.Find(tab.Key + "test", true).FirstOrDefault();
                if (tab.Value.Value != 0)
                    tb.Text = tab.Value.Value.ToString();
            }
            // done loading!
            loading = false;
            // this build is no longer considered new
            build.New = false;
            // oh yeah, update everything now that it's finally loaded
            UpdateGlobal();
        }

        // switch the cool icon on the button (and the bool in the build)
        private void ChangeBroken(bool state)
        {
            toolStripButton2.Tag = state;
            if (state)
            {
                toolStripButton2.Image = global::RuneApp.App.broken;
            }
            else
            {
                toolStripButton2.Image = global::RuneApp.App.whole;
            }
            if (build != null)
                build.AllowBroken = state;
        }

        // The minimum value was changed
        private void Total_TextChanged(object sender, EventArgs e)
        {
            var total = (TextBox)sender;

            if (total.Tag == null)
                return;

            // pull the BASE and BONUS controls from the tag
            var kv = (KeyValuePair<Label, Label>)total.Tag;
            var bonus = kv.Value;
            var mBase = kv.Key;

            // did someone put a k in?
            if (total.Text.ToLower().Contains("k"))
            {
                total.Text = (int.Parse(total.Text.ToLower().Replace("k", "")) * 1000).ToString();
            }

            // calculate the difference between base and minimum, put it in bonus
            var hasPercent = mBase.Text.Contains("%");
			int val = 0;
			if (int.TryParse(total.Text.Replace("%", ""), out val))
			{
				int sub = 0;
				int.TryParse(mBase.Text.Replace("%", ""), out sub);
				val -= sub;
			}
            if (val < 0)
                val = 0;

            bonus.Text = "+" + (val) + (hasPercent ? "%" : "");
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (build == null)
                return;

            var list = (ListView)sender;

            var items = list.SelectedItems;
            if (items.Count == 0)
                return;

            ListViewItem item = items[0];

            ChangeMove(build.BuildSets.Contains((RuneSet)item.Tag));
            ChangeReq(build.RequiredSets.Contains((RuneSet)item.Tag));
        }

        // flip the icon for the INCLUDE set button
        private void ChangeMove(bool dir)
        {
            toolStripButton1.Tag = dir;
            if (!dir)
            {
                toolStripButton1.Image = global::RuneApp.App.add;
                toolStripButton1.Text = "Include selected set";
            }
            else
            {
                toolStripButton1.Image = global::RuneApp.App.subtract;
                toolStripButton1.Text = "Exclude selected set";
            }

        }

        // flip the icon for the REQUIRED set button
        private void ChangeReq(bool dir)
        {
            toolStripButton3.Tag = dir;
            if (!dir)
            {
                toolStripButton3.Image = global::RuneApp.App.up;
                toolStripButton3.Text = "Require selected set";
            }
            else
            {
                toolStripButton3.Image = global::RuneApp.App.down;
                toolStripButton3.Text = "Option selected set";
            }

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            ChangeBroken(!(bool)((ToolStripButton)sender).Tag);
        }

        // shuffle runesets between: included <-> excluded
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (build == null)
                return;

            var list = listView1;

            var items = list.SelectedItems;
            if (items.Count == 0)
                return;

            var selGrp = items[0].Group;
            var indGrp = selGrp.Items.IndexOf(items[0]);

            // shift all the selected things to the place
            foreach (ListViewItem item in items)
            {
                var set = (RuneSet)item.Tag;
                if (build.BuildSets.Contains(set))
                {
                    build.BuildSets.Remove(set);
                    item.Group = rsExc;
                }
                else
                {
                    build.BuildSets.Add(set);
                    item.Group = rsInc;
                }
            }

            var ind = items[0].Index;
            
            // maybe try to do cool reselecting the next entry
            if (selGrp.Items.Count > 0)
            {
                int i = indGrp;
                if (i > 0 && (i == selGrp.Items.Count || !(bool)selGrp.Tag))
                    i -= 1;
                while (selGrp.Items[i].Selected)
                {
                    i++;
                    i %= selGrp.Items.Count;
                    if (i == indGrp)
                        break;

                }

                ind = selGrp.Items[i].Index;
            }
            listView1.SelectedIndices.Clear();
            
            listView1.SelectedIndices.Add(ind);
            listView1_SelectedIndexChanged(listView1, null);
        }

        // shuffle rnuesets between: excluded > required <-> included
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (build == null)
                return;

            var list = listView1;

            var items = list.SelectedItems;
            if (items.Count == 0)
                return;

            var selGrp = items[0].Group;
            var indGrp = selGrp.Items.IndexOf(items[0]);

            foreach (ListViewItem item in items)
            {
                var set = (RuneSet)item.Tag;
                if (build.RequiredSets.Contains(set))
                {
                    build.RequiredSets.Remove(set);
                    if (!build.BuildSets.Contains(set))
                        build.BuildSets.Add(set);
                    item.Group = rsInc;
                }
                else
                {
                    build.RequiredSets.Add(set);
                    if (!build.BuildSets.Contains(set))
                        build.BuildSets.Add(set);
                    item.Group = rsReq;
                }
            }

            var ind = items[0].Index;

            if (selGrp.Items.Count > 0)
            {
                int i = indGrp;
                if (i > 0 && (i == selGrp.Items.Count || !(bool)selGrp.Tag))
                    i -= 1;
                while (selGrp.Items[i].Selected)
                {
                    i++;
                    i %= selGrp.Items.Count;
                }

                ind = selGrp.Items[i].Index;
            }
            listView1.SelectedIndices.Clear();

            listView1.SelectedIndices.Add(ind);
            listView1_SelectedIndexChanged(listView1, null);
        }

        // if you click on a little rune
        private void runeControl_Click(object sender, EventArgs e)
        {
            // reset all the gammas
            foreach (RuneControl t in runes)
            {
                t.Gamma = 1;
                t.Refresh();
            }

            RuneControl tc = ((RuneControl)sender);
            
            // darken? wut
            tc.Gamma = 1.4f;
            // redraw that
            tc.Refresh();

            // good idea, generate right now whenever the user clicks a... whatever
            build.GenRunes(Main.data, false, Main.useEquipped);

            // figure stuff out
            long perms = 0;
            Label ctrl;
            for (int i = 0; i < 6; i++)
            {
                int num = build.runes[i].Length;

                if (perms == 0)
                    perms = num;
                else
                    perms *= num;
                if (num == 0)
                    perms = 0;

                ctrl = (Label)Controls.Find("runeNum" + (i + 1).ToString(), true).FirstOrDefault();
                ctrl.Text = num.ToString();
                ctrl.ForeColor = Color.Black;

                // arbitrary colours for goodness/badness
                if (num < 12)
                    ctrl.ForeColor = Color.Green;
                if (num > 24)
                    ctrl.ForeColor = Color.Orange;
                if (num > 32)
                    ctrl.ForeColor = Color.Red;
            }
            ctrl = (Label)Controls.Find("runeNums", true).FirstOrDefault();
            ctrl.Text = String.Format("{0:#,##0}", perms);
            ctrl.ForeColor = Color.Black;

            // arbitrary colours for goodness/badness
            if (perms < 500000) // 500k
                ctrl.ForeColor = Color.Green;
            if (perms > 5000000) // 5m
                ctrl.ForeColor = Color.Orange;
            if (perms > 20000000 || perms == 0) // 20m
                ctrl.ForeColor = Color.Red;
        }

        private void UpdateGlobal()
        {
            // if the window is loading, try not to save the window
            if (loading)
                return;
            
            foreach (string tab in tabNames)
            {
                foreach (string stat in statNames)
                {
                    UpdateStat(tab, stat);
                }
                if (!build.runeScoring.ContainsKey(tab) && build.runeFilters.ContainsKey(tab))
                {
                    // if there is a non-zero
                    if (build.runeFilters[tab].Any(r => r.Value.NonZero))
                    {
                        build.runeScoring.Add(tab, new KeyValuePair<int, int>(0,0));
                    }
                }
                if (build.runeScoring.ContainsKey(tab))
                {
                    var kv = build.runeScoring[tab];
                    var ctrlTest = Controls.Find(tab + "test", true).FirstOrDefault();
                    if (ctrlTest.Text != "")
                        build.runeScoring[tab] = new KeyValuePair<int, int>(kv.Key, int.Parse(ctrlTest.Text));
                }
            }
            foreach (string stat in statNames)
            {
                var ctrlTotal = groupBox1.Controls.Find(stat + "Total", true).FirstOrDefault();
                int val = 0; int total = 0;
                if (int.TryParse(ctrlTotal.Text, out val))
                    total = val;
                build.Minimum[stat] = val;
                var ctrlWorth = groupBox1.Controls.Find(stat + "Worth", true).FirstOrDefault();
                val = 0; int worth = 0;
                if (int.TryParse(ctrlWorth.Text, out val))
                    worth = val;
                build.Sort[stat] = val;

                var ctrlMax = groupBox1.Controls.Find(stat + "Max", true).FirstOrDefault();
                val = 0; int max = 0;
                if (int.TryParse(ctrlMax.Text, out val))
                    max = val;
                build.Maximum[stat] = val;
                var ctrlCurrent = groupBox1.Controls.Find(stat + "Current", true).FirstOrDefault();
                val = 0; int current = 0;
                if (int.TryParse(ctrlCurrent.Text, out val))
                    current = val;
                var ctrlWorthPts = groupBox1.Controls.Find(stat + "CurrentPts", true).FirstOrDefault();
                if (worth != 0 && current != 0)
                {
                    double pts = current;
                    if (max != 0)
                        pts = Math.Min(max, current);
                    ctrlWorthPts.Text = ((int)(pts / (double)worth)).ToString();
                }
            }

            var lists = new ListView[]{listView2, listView4, listView6};
            for (int j = 0; j < lists.Length; j++)
            {
                var lv = lists[j];
                var bl = build.slotStats[(j + 1) * 2 - 1];
                bl.Clear();

                for (int i = 0; i < statNames.Length; i++)
                {
                    string stat = statNames[i];
                    if (i < 3)
                    {
                        if (lv.Items.Find(stat + "flat", true).FirstOrDefault().Group == lv.Groups[0])
                            bl.Add(stat + "flat");
                        if (lv.Items.Find(stat + "perc", true).FirstOrDefault().Group == lv.Groups[0])
                            bl.Add(stat + "perc");

                    }
                    else
                    {
                        if (j == 0 && stat != "SPD")
                            continue;
                        if (j == 1 && (stat != "CR" && stat != "CD"))
                            continue;
                        if (j == 2 && (stat != "ACC" && stat != "RES"))
                            continue;

                        if (lv.Items.Find(stat + (stat == "SPD" ? "flat" : "perc"), true).FirstOrDefault().Group == lv.Groups[0])
                            bl.Add(stat + (stat == "SPD" ? "flat" : "perc"));
                    }
                }
            }

            TestRune(runeTest);
        }

        private void UpdateStat(string tab, string stat)
        {
            Predicate<Rune> predStat = r => true;

            var ctest = this.Controls.Find(tab + stat + "test", true).First();
            int test = 0;
            int.TryParse(ctest.Text, out test);

            if (!build.runeFilters.ContainsKey(tab))
            {
                build.runeFilters.Add(tab, new Dictionary<string, RuneFilter>());
            }
            var fd = build.runeFilters[tab];
            if (!fd.ContainsKey(stat))
            {
                fd.Add(stat, new RuneFilter());
            }
            var fi = fd[stat];
            /*
            var ctrlTotal = groupBox1.Controls.Find(stat + "Total", true).FirstOrDefault();
            KeyValuePair<Label, Label> kvBaseBonus = (KeyValuePair<Label, Label>)ctrlTotal.Tag;
            int valTotal = 0;
            int.TryParse(ctrlTotal.Text, out valTotal);
            kvBaseBonus.Value.Text = "+" + (valTotal - build.mon[stat]);
            */
            foreach (string type in new string[] { "flat", "perc" })
            {
                if (type == "perc" && stat == "SPD")
                {
                    continue;
                }
                if (type == "flat" && (stat == "ACC" || stat == "RES" || stat == "CD" || stat == "CR"))
                {
                    continue;
                }

                if (tab == "g")
                    this.Controls.Find(tab + "i" + stat + type, true).First().Text = "";
                else if (tab == "e" || tab == "o")
                {
                    this.Controls.Find(tab + "i" + stat + type, true).First().Text = this.Controls.Find("gc" + stat + type, true).First().Text;
                }
                else
                {
                    int s = int.Parse(tab);
                    if (s % 2 == 0)
                        this.Controls.Find(tab + "i" + stat + type, true).First().Text = this.Controls.Find("ec" + stat + type, true).First().Text;
                    else
                        this.Controls.Find(tab + "i" + stat + type, true).First().Text = this.Controls.Find("oc" + stat + type, true).First().Text;
                }

                var c = this.Controls.Find(tab + "c" + stat + type, true).First();

                int i = 0;
                int t = 0;
                var ip = int.TryParse(this.Controls.Find(tab + "i" + stat + type, true).First().Text, out i);
                var tp = int.TryParse(this.Controls.Find(tab + stat + type, true).First().Text, out t);

                if (ip)
                {
                    if (tp)
                        c.Text = Math.Min(i, t).ToString();
                    else
                        c.Text = i.ToString();
                }
                else
                {
                    if (tp)
                        c.Text = t.ToString();
                    else
                        c.Text = "";
                }

                
                //Predicate<Rune> p = r => r[stat + type] / val >= test;
                //predStat = r => predStat.Invoke(r) && p.Invoke(r);

                fi[type] = t;
            }

            fi.Test = test;
        }

        private void TestRune(Rune rune)
        {
            if (rune == null)
                return;

            // consider moving to the slot tab for the rune
            foreach (var tab in tabNames)
            {
                TestRuneTab(rune, tab);
            }
        }

        private int DivCtrl(int val, string tab, string stat, string type)
        {
            var ctrls = this.Controls.Find(tab + "c" + stat + type, true);
            if (ctrls.Length == 0)
                return 0;

            var ctrl = ctrls[0];
            int num = 1;
            if (int.TryParse(ctrl.Text, out num))
            {
                if (num == 0)
                    return 0;

                return val / num;
            }
            else
                return 0;
        }

        private bool GetPts(Rune rune, string tab, string stat, ref int points)
        {
            int pts = 0;

            PropertyInfo[] props = typeof(Rune).GetProperties();
            foreach (var prop in props)
            {
                
            }

            pts += DivCtrl(rune[stat + "flat"], tab, stat, "flat");
            pts += DivCtrl(rune[stat + "perc"], tab, stat, "perc");
            points += pts;

            var lCtrls = this.Controls.Find(tab + "r" + stat + "test", true);
            var tbCtrls = this.Controls.Find(tab + stat + "test", true);
            if (lCtrls.Length != 0 && tbCtrls.Length != 0)
            {
                var tLab = (Label)lCtrls[0];
                var tBox = (TextBox)tbCtrls[0];
                tLab.Text = pts.ToString();
                tLab.ForeColor = Color.Black;
                int vs = 1;
                if (int.TryParse(tBox.Text, out vs))
                {
                    if (pts >= vs)
                    {
                        tLab.ForeColor = Color.Green;
                        return true;
                    }
                    else
                    {
                        tLab.ForeColor = Color.Red;
                        return false;
                    }
                }
                return true;
            }

            return false;
            //return points;
        }

        private void TestRuneTab(Rune rune, string tab)
        {
            bool res = false;
            if (!build.runeScoring.ContainsKey(tab))
                return;

            var kv = build.runeScoring[tab];
            int scoring = kv.Key;
            if (scoring == 1)
                res = true;

            int points = 0;
            foreach (var stat in statNames)
            {
                bool s = GetPts(rune, tab, stat, ref points);
                if (scoring == 1)
                    res &= s;
                else if (scoring == 0)
                    res |= s;
            }

            //var page = this.tabControl1.TabPages[tab];
            var ctrl = Controls.Find(tab + "Check", true).FirstOrDefault();

            ctrl.Text = res.ToString();

            if (scoring == 2)
                ctrl.Text = points.ToString();

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var ff = new RuneSelect())
            {
                ff.returnedRune = runeTest;
                ff.build = build;

                switch (tabControl1.SelectedTab.Text)
                {
                    case "Evens":
                        ff.runes = Main.data.Runes.Where(r => r.Slot % 2 == 0);
                        List<Rune> fr = new List<Rune>();
                        fr.AddRange(ff.runes.Where(r => r.Slot == 2 && build.slotStats[1].Contains(r.MainType.ToForms())));
                        fr.AddRange(ff.runes.Where(r => r.Slot == 4 && build.slotStats[3].Contains(r.MainType.ToForms())));
                        fr.AddRange(ff.runes.Where(r => r.Slot == 6 && build.slotStats[5].Contains(r.MainType.ToForms())));
                        ff.runes = fr;
                        break;
                    case "Odds":
                        ff.runes = Main.data.Runes.Where(r => r.Slot % 2 == 1);
                        break;
                    case "Global":
                        ff.runes = Main.data.Runes;
                        break;
                    default:
                        int slot = int.Parse(tabControl1.SelectedTab.Text);
                        ff.runes = Main.data.Runes.Where(r => r.Slot == slot);
                        break;
                }

                ff.runes = ff.runes.Where(r => build.BuildSets.Contains(r.Set));

                var res = ff.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    Rune rune = ff.returnedRune;
                    if (rune != null)
                    {
                        runeTest = rune;
                        TestRune(rune);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void global_TextChanged(object sender, EventArgs e)
        {
            TextBox text = (TextBox)sender;

            if (text.Text.ToLower().Contains("k"))
                text.Text = (int.Parse(text.Text.ToLower().Replace("k", "")) * 1000).ToString();

            UpdateGlobal();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
			if (build == null)
				return;

            if (MessageBox.Show("This will generate builds", "Continue?", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            {
                // make a new form that generates builds into it
                // have a weight panel near the stats
                // sort live on change
                // copy weights back to here

				var ff = new Generate(build);
				var res = ff.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    loading = true;
                    foreach (var stat in statNames)
                    {
                        var ctrlWorth = groupBox1.Controls.Find(stat + "Worth", true).FirstOrDefault();
                        //ctrlWorth.Text = "";
                        if (build.Sort[stat] > 0)
                            ctrlWorth.Text = build.Sort[stat].ToString();
                    }
                    loading = false;
                }
                else
                {
                    foreach (var stat in statNames)
                    {
                        var ctrlWorth = groupBox1.Controls.Find(stat + "Worth", true).FirstOrDefault();
                        int val = 0;
                        int.TryParse(ctrlWorth.Text, out val);
                        build.Sort[stat] = val;
                    }
                }
                UpdateGlobal();
            }
        }

        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            ListView lv = (ListView)sender;

            if (lv.SelectedItems.Count > 0)
            {
                ListViewItem li = lv.SelectedItems[0];

                if (li.Group == lv.Groups[0])
                    li.Group = lv.Groups[1];
                else
                    li.Group = lv.Groups[0];

                UpdateGlobal();
            }
        }
    }
}