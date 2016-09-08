using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Owin;
using System.Threading;
using System.Diagnostics;
using System.Fabric.Description;
using System.Globalization;
using Microsoft.ServiceFabric.Data;

namespace CaptainSmackDown2.BackEnd
{
    public class OwinCommunicationListener : ICommunicationListener
    {
        private readonly ServiceContext serviceContext;

        /// <summary>
        /// OWIN server handle.
        /// </summary>
        private IDisposable serverHandle;

        private Action<IAppBuilder, IReliableStateManager> startup;
        private IReliableStateManager stateManager;
        private string publishAddress;
        private string listeningAddress;
        private string appRoot;

        public OwinCommunicationListener(Action<IAppBuilder, IReliableStateManager> startup, ServiceContext serviceContext, IReliableStateManager stateManager)
            : this(null, startup, serviceContext, stateManager)
        {
        }

        public OwinCommunicationListener(string appRoot, Action<IAppBuilder, IReliableStateManager> startup, ServiceContext serviceContext, IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
            this.startup = startup;
            this.appRoot = appRoot;
            this.serviceContext = serviceContext;
        }


        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            Trace.WriteLine("Initialize");

            EndpointResourceDescription serviceEndpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
            int port = serviceEndpoint.Port;

            if (this.serviceContext is StatefulServiceContext)
            {
                StatefulServiceContext statefulInitParams = (StatefulServiceContext)this.serviceContext;

                this.listeningAddress = String.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}/{2}/{3}/",
                    port,
                    statefulInitParams.PartitionId,
                    statefulInitParams.ReplicaId,
                    Guid.NewGuid());
            }
            else if (this.serviceContext is StatelessServiceContext)
            {
                if (string.IsNullOrWhiteSpace(this.appRoot))
                {
                    this.listeningAddress = string.Format(
                        CultureInfo.InvariantCulture,
                        "http://+:{0}/",
                        port);
                }
                else
                {
                    this.listeningAddress = String.Format(
                        CultureInfo.InvariantCulture,
                        "http://+:{0}/{1}/",
                        port,
                        this.appRoot.TrimEnd('/'));
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            this.publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            Trace.WriteLine(String.Format("Opening on {0}", this.publishAddress));

            try
            {
                Trace.WriteLine(String.Format("Starting web server on {0}", this.listeningAddress));

                this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder => this.startup.Invoke(appBuilder, stateManager));

                return Task.FromResult(this.publishAddress);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);

                this.StopWebServer();

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Trace.WriteLine("Close");

            this.StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            Trace.WriteLine("Abort");

            this.StopWebServer();
        }

        private void StopWebServer()
        {
            if (this.serverHandle != null)
            {
                try
                {
                    this.serverHandle.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
        }
    }
}