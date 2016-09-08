using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace CaptainSmackDown2.Api.Controllers
{
    public class VoteController : ApiController
    {
        private static readonly Uri serviceUri = new Uri(@"fabric:/CaptainSmackDown2/BackEnd");
        private readonly ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();
        private readonly HttpClient httpClient = new HttpClient();

        [HttpGet]
        [Route("vote/{captainType}/{captainName}")]
        public async Task<IHttpActionResult> Vote(string captainType, string captainName)
        {
            var primaryReplicaAddress = await GetReplicaAddress(captainType);

            await httpClient.GetAsync($"{primaryReplicaAddress}Vote/{captainName}");

            return this.Ok();
        }

        [HttpGet]
        [Route("summary")]
        public async Task<IHttpActionResult> Summary()
        {
            var starshipVotesTask = GetSummary("Starship");
            var crabboatVotesTask = GetSummary("CrabBoat");

            var starshipVotes = await starshipVotesTask;
            var crabboatVotes = await crabboatVotesTask;

            var allVotes = starshipVotes.Union(crabboatVotes);

            return this.Ok(allVotes);
        }

        private async Task<List<Models.VoteSummary>> GetSummary(string captainType)
        {
            var primaryReplicaAddress = await GetReplicaAddress(captainType);
            var json = await httpClient.GetStringAsync($"{primaryReplicaAddress}Summary");
            var votes = JsonConvert.DeserializeObject<List<Models.VoteSummary>>(json);

            if (votes != null && votes.Any())
            {
                foreach (var vote in votes)
                {
                    vote.CaptainType = captainType;
                }
            }

            return votes;
        }

        private async Task<string> GetReplicaAddress(string captainType)
        {
            var cancelRequest = new CancellationToken();
            ServicePartitionKey partitionKey = new ServicePartitionKey(captainType);

            ResolvedServicePartition partition = await this.servicePartitionResolver.ResolveAsync(serviceUri, partitionKey, cancelRequest);
            ResolvedServiceEndpoint ep = partition.GetEndpoint();

            JObject addresses = JObject.Parse(ep.Address);
            return (string)addresses["Endpoints"].First();
        }
    }
}
