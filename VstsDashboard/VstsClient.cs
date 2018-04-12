using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ExternalEvent;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace VstsDashboard
{
    public class VstsClient
    {
        private readonly HttpClient _prClient = new HttpClient();

        private readonly string _projectName;
        private readonly string _baseUrl;
        private readonly ReleaseHttpClient _client;
        private readonly Config _config;
        
        public VstsClient(Config config)
        {
            _config = config;
            _projectName = config.ProjectName;
            _baseUrl = $"https://{config.AccountName}.visualstudio.com";
            _client = new VssConnection(new Uri(_baseUrl), new VssBasicCredential("username", config.AccessToken)).GetClient<ReleaseHttpClient>();

            _prClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(config.AccessToken)));
        }

        public async Task<List<ReleaseDefinition>> GetReleaseDefinitions() => 
            await _client.GetReleaseDefinitionsAsync(_projectName, null, ReleaseDefinitionExpands.Environments);

        public async Task<ReleaseDefinitionSummary> GetMostRecentReleases(int defId) => 
            await _client.GetReleaseDefinitionSummaryAsync(_projectName, defId, 1, false);

        public string ReleaseUrl(int? releaseNumber) => 
            $"https://{_config.AccountName}.visualstudio.com/{_config.ProjectName}/_release?releaseId={releaseNumber}";

        private string PullRequests(string project, string version = "3.0") =>
            $"{_baseUrl}/DefaultCollection/{project}/_apis/git/pullRequests?api-version={version}";

        public async Task<List<PullRequest>> GetPullRequests(bool isOpen)
        {
            var uri = PullRequests(_projectName);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_config.AccessToken}")));

                using (var response = await client.GetAsync(uri))
                {
                    if (response.IsSuccessStatusCode == false)
                    {
                        return new List<PullRequest>();
                    }

                    var responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<PullRequests>(responseBody).Value;
                }
            }
        }
    }

    public class PullRequests
    {
        public List<PullRequest> Value { get; set; }
    }
    
    public class PullRequest
    {
        public Repository Repository { get; set; }
        public int PullRequestId { get; set; }
        public int CodeReviewId { get; set; }
        public string Status { get; set; }
        public CreatedBy CreatedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SourceRefName { get; set; }
        public string TargetRefName { get; set; }
        public string MergeStatus { get; set; }
        public string MergeId { get; set; }
        public Commit LastMergeSourceCommit { get; set; }
        public Commit LastMergeTargetCommit { get; set; }
        public Commit LastMergeCommit { get; set; }
        public Reviewer[] Reviewers { get; set; }
        public string Url { get; set; }
        public bool SupportsIterations { get; set; }
    }

    public class Repository
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public Project Project { get; set; }
    }

    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
    }

    public class CreatedBy
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string UniqueName { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
    }

    public class Commit
    {
        public string CommitId { get; set; }
        public string Url { get; set; }
    }

    public class Reviewer
    {
        public string ReviewerUrl { get; set; }
        public int Vote { get; set; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string UniqueName { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
    }
}
