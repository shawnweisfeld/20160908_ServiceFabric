using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;

namespace CaptainSmackDown2.BackEnd
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class BackEnd : StatefulService
    {
        public const string ServiceEventSourceName = "BackEnd";

        public BackEnd(StatefulServiceContext context)
            : base(context)
        {
            ServiceEventSource.Current.ServiceInstanceConstructed(ServiceEventSourceName);
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.RunAsyncInvoked(ServiceEventSourceName);

            IReliableQueue<string> inputQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<string>>("inputQueue");
            IReliableDictionary<string, long> voteCountDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("voteCountDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<string> dequeuReply = await inputQueue.TryDequeueAsync(tx);

                        if (dequeuReply.HasValue)
                        {
                            string captain = dequeuReply.Value;

                            long count = await voteCountDictionary.AddOrUpdateAsync(
                                tx,
                                captain,
                                1,
                                (key, oldValue) => oldValue + 1);

                            long queueLength = await inputQueue.GetCountAsync(tx);

                            await tx.CommitAsync();

                            ServiceEventSource.Current.RunAsyncStatus(
                                this.Partition.PartitionInfo.Id,
                                queueLength,
                                captain,
                                count);
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }
                catch (TimeoutException)
                {
                    //Service Fabric uses timeouts on collection operations to prevent deadlocks.
                    //If this exception is thrown, it means that this transaction was waiting the default
                    //amount of time (4 seconds) but was unable to acquire the lock. In this case we simply
                    //retry after a random backoff interval. You can also control the timeout via a parameter
                    //on the collection operation.
                    Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(100, 300)));

                    continue;
                }
                catch (Exception exception)
                {
                    //For sample code only: simply trace the exception.
                    ServiceEventSource.Current.Message(exception.ToString());
                }
            }
        }


        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see http://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            ServiceEventSource.Current.CreateCommunicationListener(ServiceEventSourceName);

            return new[]
            {
                new ServiceReplicaListener(initParams => new OwinCommunicationListener("votecountservice",  Startup.ConfigureApp, initParams, StateManager))
            };
        }

    }
}
