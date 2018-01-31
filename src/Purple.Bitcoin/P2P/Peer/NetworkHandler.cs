using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Purple.Bitcoin.Configuration;
using DotNetTor;

namespace Purple.Bitcoin.P2P.Peer
{
    public class NetworkHandler
    {
        private NodeSettings NodeSettings;
        private TotClient totClient;

        public TcpClient TcpClient { get; private set; }

        public NetworkStream Stream { get; private set; }

        public NetworkHandler(NodeSettings nodeSettings)
        {
            this.NodeSettings = nodeSettings;
            this.TcpClient = new TcpClient();
        }

        public async Task ConnectAsync(IPEndPoint endPoint, CancellationToken cancellation)
        {
            Exception error = null;

            try
            {
                if (this.NodeSettings.TorEnabled)
                {
                    await GetTotClientAsync(endPoint).ContinueWith(task =>
                     {
                         this.totClient = task.Result;
                         this.TcpClient = this.totClient.TorSocks5Client.TcpClient;
                         this.Stream = this.TcpClient.GetStream();
                     });
                }
                else
                {
                    this.TcpClient.ConnectAsync(endPoint.Address, endPoint.Port).Wait(cancellation);
                }
            }
            catch (Exception e)
            {
                // Record the error occurring in the thread pool's context.
                error = e;
            }

            if (error != null)
                throw error;
        }

        public void Disconnect()
        {
            this.TcpClient.Close();
            this.totClient.DisposeAsync();
        }

        private Task<TotClient> GetTotClientAsync(IPEndPoint endPoint)
        {
            if (this.TcpClient != null && this.TcpClient.Connected)
            {
                var connectedEndPoint = (IPEndPoint)this.TcpClient.Client.RemoteEndPoint;
                if (((IPEndPoint)this.TcpClient.Client.RemoteEndPoint).Address == endPoint.Address)
                {
                    return Task.FromResult(this.totClient);
                }
            }

            if (this.totClient != null && this.totClient.TorSocks5Client.IsConnected)
            {
                return Task.FromResult(this.totClient);
            }

            var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 9050);
            var socksManager = new TorSocks5Manager(serverEndPoint);

            if (endPoint.Address.IsIPv4MappedToIPv6)
            {
                endPoint.Address = endPoint.Address.MapToIPv4();
            }
            
            return socksManager.EstablishTotConnectionAsync(endPoint);
        }
    }
}
