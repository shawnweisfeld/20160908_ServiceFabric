using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Linq;
using System.Threading;
using CaptainSmackDown2.BackEnd.Models;

namespace CaptainSmackDown2.BackEnd.Controllers
{
    public class VoteController : ApiController
    {
        private readonly IReliableStateManager stateManager;

        public VoteController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        [HttpGet]
        [Route("test")]
        public async Task<IHttpActionResult> Test()
        {
            await AddWord("Test");
            return this.Ok("Hello World!");
        }


        [HttpGet]
        [Route("Summary")]
        public async Task<IHttpActionResult> Count()
        {
            IReliableDictionary<string, long> voteCountDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, long>>("voteCountDictionary");

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                var voteDictionaryEnumerator = (await voteCountDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                var ct = new CancellationToken();
                var list = new List<VoteSummary>();

                while (await voteDictionaryEnumerator.MoveNextAsync(ct))
                {
                    var item = voteDictionaryEnumerator.Current;
                    list.Add(new VoteSummary() { Captain = item.Key, Votes = item.Value });
                }

                return this.Ok(list);
            }
        }

        [HttpGet]
        [Route("Vote/{captain}")]
        public async Task<IHttpActionResult> AddWord(string captain)
        {
            IReliableQueue<string> queue = await this.stateManager.GetOrAddAsync<IReliableQueue<string>>("inputQueue");

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await queue.EnqueueAsync(tx, captain);

                await tx.CommitAsync();
            }

            return this.Ok();
        }
    }
}
