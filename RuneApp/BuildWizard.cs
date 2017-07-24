﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using RuneOptim;

namespace RuneApp
{
	public partial class BuildWizard : Form
	{
		Build build;
		bool loading = true;
		ListViewItem defTemplate = null;

		static List<Build> templateList = null;

		static List<Build> TemplateList
		{
			get
			{
				if (templateList == null)
				{
					if (File.Exists(global::RuneApp.Properties.Resources.TemplatesJSON))
					{
						var bstr = File.ReadAllText(global::RuneApp.Properties.Resources.TemplatesJSON);
						templateList = JsonConvert.DeserializeObject<List<Build>>(bstr);
					}
					else
					{
						Program.log.Error(global::RuneApp.Properties.Resources.TemplatesJSON + " not found?");
						templateList = new List<Build>();
					}
				}

				return templateList;
			}
		}

		public BuildWizard(Build newB)
		{
			InitializeComponent();
			if (newB.mon == null)
			{
				MessageBox.Show("Error: Build has no monster!");
				Program.log.Error("Build has no monster!");
				this.DialogResult = DialogResult.Abort;
				this.Close();
			}
			build = newB;
			this.lbPrebuild.Text = "Prebuild Template for " + build.mon.Name;

			defTemplate = addTemplate(new Build() { MonName = "<None>" }, prebuildList.Groups[0]);
			// load more
			foreach (var b in TemplateList)
			{
				addTemplate(b, prebuildList.Groups[b.priority]);
			}

			prebuildList.Select();
			defTemplate.Selected = true;
		}

		private void BuildWizard_Load(object sender, EventArgs e)
		{
			cShowWizard.Checked = Program.Settings.ShowBuildWizard;
			statBase.Stats = build.mon;
			statScore.Stats = build.Sort;
			statCurrent.Stats = build.mon.GetStats();
			statPreview.Stats = new Stats(build.mon.GetStats(), true);
			statTotal.Stats = new Stats();

			statPreview.Stats.OnStatChanged += CalcStats_OnStatChanged;
			statScore.Stats.OnStatChanged += CalcStats_OnStatChanged;
			statScore.Stats.OnStatChanged += ScoreStats_OnStatChanged;

			loading = false;
		}

		private void BuildWizard_FormClosing(object sender, FormClosingEventArgs e)
		{
			statPreview.Stats.OnStatChanged -= CalcStats_OnStatChanged;
			statScore.Stats.OnStatChanged -= CalcStats_OnStatChanged;
			statScore.Stats.OnStatChanged -= ScoreStats_OnStatChanged;
		}

		private void ScoreStats_OnStatChanged(object sender, StatModEventArgs e)
		{
			RecheckPreview();
		}

		private void CalcStats_OnStatChanged(object sender, StatModEventArgs e)
		{
			build.CalcScore(statPreview.Stats, statTotal.Stats);
		}

		private void btnCreate_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void cShowWizard_CheckedChanged(object sender, EventArgs e)
		{
			if (loading) return;

			Program.Settings.ShowBuildWizard = cShowWizard.Checked;
			Program.Settings.Save();
		}

		private void prebuildList_SelectedIndexChanged(object sender, EventArgs e)
		{
			loading = true;
			var item = prebuildList.SelectedItems.Cast<ListViewItem>().FirstOrDefault();
			if (item == null) return;

			cShowWizard.Enabled = (item == defTemplate);
			if (!cShowWizard.Enabled && !cShowWizard.Checked)
				cShowWizard.Checked = true;

			var tb = item.Tag as Build;
			if (tb == null) return;
			
			build.AllowBroken = tb.AllowBroken;
			build.Minimum.SetTo(tb.Minimum);
			build.Maximum.SetTo(tb.Maximum);
			if (tb.Threshold.NonZero())
				build.Threshold.SetTo(tb.Threshold);
			else
				build.Threshold.SetTo(new Stats() {
					Accuracy = 85,
					Resistance = 100,
					CritRate = 100,
				});
			build.Sort.SetTo(tb.Sort);
			build.BuildSets.Clear();
			build.BuildSets.AddRange(tb.BuildSets);
			build.RequiredSets.Clear();
			build.RequiredSets.AddRange(tb.RequiredSets);
			loading = false;
			RecheckPreview();
		}

		private void RecheckPreview()
		{
			if (loading) return;
			loading = true;
			runeDial.Loadout = null;
			build.RunesUseLocked = cPreviewLocked.Checked;
			build.autoRuneSelect = true;
			build.RunesUseEquipped = true;
			build.autoRuneAmount = 4;

			build.GenRunes(Program.data);
			if (build.runes.Any(rs => rs.Length > 6))
				return;
			var res = build.GenBuilds();
			if (res == BuildResult.Success && build.Best != null)
			{
				runeDial.Loadout = build.Best.Current;
				statPreview.Stats.SetTo(build.Best.GetStats());
				statPreview.RecheckExtras();
			}
			else
			{
				// TODO: warning
			}

			build.autoRuneSelect = false;
			loading = false;
		}

		private void cPreviewLocked_CheckedChanged(object sender, EventArgs e)
		{
			RecheckPreview();
		}

		private void runeDial_RuneClick(object sender, RuneClickEventArgs e)
		{
			runeBox.SetRune(e.Rune);
			runeBox.Show();
		}
	}
}
