using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Estuary.Actors;
using Estuary.Services;
using PinkUmbrella.Models.Auth;
using PinkUmbrella.Repositories;
using Tides.Models.Auth;

namespace PinkUmbrella.Services.NoSql
{
    public class PeerService : IPeerService
    {
        private readonly IAuthService _auth;
        private readonly StringRepository _strings;

        public PeerService(IAuthService auth, StringRepository strings)
        {
            _auth = auth;
            _strings = strings;
        }

        public async Task<IActivityStreamRepository> Open(IPAddressModel address, int? port = null)
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

        protected async Task<IActivityStreamRepository> Open(SavedIPAddressModel savedAddress, int port)
        {
            var peer = new Peer()
            {
                Address = savedAddress,
                AddressPort = port,
            };
            return new HttpActivityStreamRepository(null, peer);
        }

        public async Task<List<Peer>> GetPeers()
        {
            var ret = new List<Peer>();
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

        public async Task AddPeer(Estuary.Actors.Peer peer)
        {
            if (_strings.ValidHandleRegex.IsMatch(peer.name))
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
                throw new ArgumentException("Peer display name is invalid", nameof(peer.name));
            }
        }

        public async Task<Estuary.Actors.Peer> GetPeer(SavedIPAddressModel address, int? port = null)
        {
            port = port ?? 443;
            var dir = PeerDirectory(address, port);
            if (System.IO.Directory.Exists(dir))
            {
                Estuary.Actors.Peer peer = null;
                using (var stream = new FileStream($"{dir}/peer.json", FileMode.Open))
                {
                    peer = await JsonSerializer.DeserializeAsync<Estuary.Actors.Peer>(stream);
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

        public Task RemovePeer(Estuary.Actors.Peer peer)
        {
            var dir = PeerDirectory(peer.Address, peer.AddressPort);
            if (System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.Delete(dir);
            }
            return Task.CompletedTask;
        }

        public async Task ReplacePeer(Estuary.Actors.Peer peer)
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