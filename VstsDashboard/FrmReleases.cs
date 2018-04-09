using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Newtonsoft.Json;

namespace VstsDashboard
{
    public partial class FrmReleases : Form
    {
        private readonly VstsClient _client;
        private readonly AsyncManager _async;

        public FrmReleases()
        {
            InitializeComponent();
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            _client = new VstsClient(config);
            _async = new AsyncManager();
            _async.AsyncWorkBegun += (x,y) => btnRefresh.Enabled = false;
            _async.AsyncWorkComplete += (x,y) => btnRefresh.Enabled = true;
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            gridReleases.Rows.Clear();
            gridPullRequests.Rows.Clear();

            _async.Do(_client.GetReleaseDefinitions()).Then(defs =>
            {
                var dict = defs.ToDictionary(d => d, d => _client.GetMostRecentReleases(d.Id));

                _async.Do(_client.GetPullRequests(false)).Then(PopulatePrList);

                _async.Do(Task.WhenAll(dict.Values)).Then(_ => PopulateReleaseList(dict.ToDictionary(d => d.Key, d => d.Value.Result)));
            });
        }

        private void PopulatePrList(List<PullRequest> prs)
        {
            gridPullRequests.Rows.Clear();
            gridPullRequests.Columns.Clear();

            gridPullRequests.Columns.Add("Title", "Title");
            gridPullRequests.Columns.Add("Creator", "Creator");
            gridPullRequests.Columns.Add("Created On", "Created On");
            gridPullRequests.Columns.Add("Merge", "Merge");
            gridPullRequests.Columns.Add("Status", "Status");

            foreach (var pullRequest in prs)
            {
                gridPullRequests.Rows.Add(PullRequestRow(pullRequest));
            }
        }

        private static DataGridViewRow PullRequestRow(PullRequest pullRequest)
        {
            var row = new DataGridViewRow();

            var color = pullRequest.MergeStatus == "conflict" ? Color.Red : Color.Green;
            var foreGround = Color.White;
            
            row.Cells.Add(Cell(pullRequest.Title, color, foreGround));
            row.Cells.Add(Cell(pullRequest.CreatedBy.DisplayName, color, foreGround));
            row.Cells.Add(Cell(pullRequest.CreationDate.ToString("s"), color, foreGround));
            row.Cells.Add(Cell(pullRequest.MergeStatus, color, foreGround));
            row.Cells.Add(Cell(pullRequest.Status, color, foreGround));

            return row;
        }

        private void PopulateReleaseList(Dictionary<ReleaseDefinition, ReleaseDefinitionSummary> releases)
        {
            var maxEnvironments = releases.Max(r => r.Key.Environments.Count);
            gridReleases.Rows.Clear();
            gridReleases.Columns.Clear();
            gridReleases.Columns.Add("ID", "ID");
            gridReleases.Columns.Add("Name", "Name");
            for (int i = 0; i < maxEnvironments; i++)
            {
                gridReleases.Columns.Add($"Environment{i}", $"Environment {i}");
            }
            foreach (var releaseDefinition in releases)
            {
                gridReleases.Rows.Add(ReleaseRow(releaseDefinition));
            }
        }

        private DataGridViewRow ReleaseRow(KeyValuePair<ReleaseDefinition, ReleaseDefinitionSummary> releaseDefinition)
        {
            var row = new DataGridViewRow();
            row.Cells.Add(Cell(releaseDefinition.Key.Id));
            row.Cells.Add(Cell(releaseDefinition.Key.Name));
            var mostRecentRelease = releaseDefinition.Value.Environments.Max(e => e.LastReleases.FirstOrDefault()?.Id);
            foreach (var env in releaseDefinition.Value.Environments)
            {
                var lastReleaseId = env.LastReleases.FirstOrDefault()?.Id;
                row.Cells.Add(ReleaseCell(lastReleaseId, lastReleaseId != mostRecentRelease ? Color.Red : Color.Green));
            }
            return row;
        }

        private static DataGridViewCell Cell(object value, Color? color = null, Color? foreGround = null)
        {
            var cell = new DataGridViewTextBoxCell { Value = value };
            if (color.HasValue)
            {
                cell.Style.BackColor = color.Value;
                cell.Style.ForeColor = foreGround ?? Color.Black;
            }
            return cell;
        }

        private DataGridViewCell ReleaseCell(int? releaseNumber, Color? color = null)
        {
            var cell = new DataGridViewLinkCell
            {
                Value = releaseNumber,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Tag = _client.ReleaseUrl(releaseNumber),
                LinkColor = DefaultForeColor,
                VisitedLinkColor = DefaultForeColor
            };

            if (color.HasValue)
            {
                cell.Style.BackColor = color.Value;
                cell.Style.ForeColor = Color.White;
                cell.LinkColor = Color.White;
            }
            return cell;
        }

        private void GridReleases_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0)
            {
                return;
            }
            if (gridReleases[e.ColumnIndex, e.RowIndex] is DataGridViewLinkCell link)
            {
                Process.Start((string) link.Tag);
            }
        }

        private void chkAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            timerAutoUpdate.Enabled = ((CheckBox) sender).Checked;
        }

        private void timerAutoUpdate_Tick(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void FrmReleases_DoubleClick(object sender, EventArgs e)
        {
            if (this.FormBorderStyle == FormBorderStyle.Sizable)
            {
                FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
            }
        }
    }
}
