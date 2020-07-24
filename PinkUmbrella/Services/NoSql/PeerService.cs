using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PinkUmbrella.Models.Auth;
using PinkUmbrella.Models.Peer;
using PinkUmbrella.Repositories;
using PinkUmbrella.Services.Peer;
using PinkUmbrella.Util;
using Tides.Models.Auth;
using Tides.Models.Peer;

namespace PinkUmbrella.Services.NoSql
{
    public class PeerService : IPeerService
    {
        private readonly IAuthService _auth;
        private readonly StringRepository _strings;
        private readonly IPeerConnectionTypeResolver _typeResolver;
        private readonly ExternalDbOptions _options;

        public PeerService(IAuthService auth, StringRepository strings, IPeerConnectionTypeResolver typeResolver, ExternalDbOptions options)
        {
            _auth = auth;
            _strings = strings;
            _typeResolver = typeResolver;
            _options = options;
        }

        public async Task<ISyncPeerClient> Open(IPAddressModel address, int? port = null)
        {
            if (address is SavedIPAddressModel savedAddress)
            {
                return await Open(savedAddress, port);
            }
            else
            {
                var ip = await _auth.GetOrRememberIP(address.ToIp());
                if (ip != null)
                {
                    return await Open(ip, port.Value);
                }
                return null;
            }
        }

        protected async Task<ISyncPeerClient> Open(SavedIPAddressModel savedAddress, int port)
        {
            var peer = await GetPeer(savedAddress, port);
            DbContext db = null;
            if (peer != null)
            {
                db = await _options.OpenDbContext.Invoke(PeerHandle(savedAddress, port));
            }
            else
            {
                peer = new PeerModel()
                {
                    Address = savedAddress,
                    AddressPort = port,
                };
            }
            return _typeResolver?.Get(PeerConnectionType.RestApiV1)?.Open(peer) as ISyncPeerClient;
        }

        public async Task<List<PeerModel>> GetPeers()
        {
            var ret = new List<PeerModel>();
            var handles = System.IO.Directory.GetFileSystemEntries("Peers");
            foreach (var handle in handles)
            {
                var split = handle.Substring("Peers/".Length).Split('-');
                if (split.Length != 2)
                {
                    continue;
                }

                var address = split[0];
                var port = int.Parse(split[1]);
                ret.Add(await GetPeer(new SavedIPAddressModel()
                {
                    Address = address,
                    Type = address.StartsWith('[') ? IPType.IPv6 : IPType.IPv4,
                }, port));
            }
            return ret;
        }

        public async Task AddPeer(PeerModel peer)
        {
            if (_strings.ValidHandleRegex.IsMatch(peer.DisplayName))
            {
                var dir = PeerDirectory(peer.Address, peer.AddressPort);
                System.IO.Directory.CreateDirectory(dir);
                using (var stream = new FileStream($"{dir}/peer.json", FileMode.CreateNew))
                {
                    await JsonSerializer.SerializeAsync(stream, peer);
                }
            }
            else
            {
                throw new ArgumentException("Peer display name is invalid", nameof(peer.DisplayName));
            }
        }

        public async Task<PeerModel> GetPeer(SavedIPAddressModel address, int? port = null)
        {
            port = port ?? 443;
            var dir = PeerDirectory(address, port);
            if (System.IO.Directory.Exists(dir))
            {
                PeerModel peer = null;
                using (var stream = new FileStream($"{dir}/peer.json", FileMode.Open))
                {
                    peer = await JsonSerializer.DeserializeAsync<PeerModel>(stream);
                }

                return peer;
            }
            else
            {
                return null;
            }
        }

        private string PeerDirectory(IPAddressModel address, int? port) => $"Peers/{address}-{port}";
        
        private string PeerHandle(SavedIPAddressModel address, int? port) => $"{address}-{port}";

        public Task RemovePeer(PeerModel peer)
        {
            var dir = PeerDirectory(peer.Address, peer.AddressPort);
            if (System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.Delete(dir);
            }
            return Task.CompletedTask;
        }

        public async Task ReplacePeer(PeerModel peer)
        {
            await RemovePeer(peer);
            await AddPeer(peer);
        }

        public Task PeerAddressChanged(SavedIPAddressModel addressFrom, int portFrom, SavedIPAddressModel addressTo, int portTo)
        {
            var dirFrom = PeerDirectory(addressFrom, portFrom);
            if (System.IO.Directory.Exists(dirFrom))
            {
                var dirTo = PeerDirectory(addressTo, portTo);
                if (!System.IO.Directory.Exists(dirTo))
                {
                    System.IO.Directory.Move(dirFrom, dirTo);
                    return Task.CompletedTask;
                }
                else
                {
                    return Task.FromException(new Exception($"New address and port already in use"));
                }
            }
            else
            {
                return Task.FromException(new Exception($"Existing address and port does not exist"));
            }
        }

        public Task<int> CountAsync() => Task.FromResult(System.IO.Directory.GetFileSystemEntries("Peers").Length);
    }
}