using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;

namespace VstsDashboard
{
    public class VstsClient
    {
        private readonly string _projectName;
        private readonly ReleaseHttpClient _client;
        private readonly Config _config;

        public VstsClient(Config config)
        {
            _config = config;
            _projectName = config.ProjectName;
            var baseUrl = $"https://{config.AccountName}.visualstudio.com";
            var connection = new VssConnection(new Uri(baseUrl), new VssBasicCredential("username", config.AccessToken));
            _client = connection.GetClient<ReleaseHttpClient>();
        }

        public async Task<List<ReleaseDefinition>> GetReleaseDefinitions()
        {
            return await _client.GetReleaseDefinitionsAsync(_projectName, null, ReleaseDefinitionExpands.Environments);
        }

        public async Task<ReleaseDefinitionSummary> GetMostRecentReleases(int defId)
        {
            return await _client.GetReleaseDefinitionSummaryAsync(_projectName, defId, 1, false);
        }

        public string ReleaseUrl(int? releaseNumber)
        {
            return $"https://{_config.AccountName}.visualstudio.com/{_config.ProjectName}/_release?releaseId={releaseNumber}";
        }
    }
}
