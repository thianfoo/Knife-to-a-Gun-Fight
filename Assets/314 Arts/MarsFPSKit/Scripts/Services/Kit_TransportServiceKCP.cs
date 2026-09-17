using System.Linq;
using kcp2k;
using Mirror;
using UnityEngine;
using Utp;

namespace MarsFPSKit.Services
{
    /// <summary>
    /// Implements KCP
    /// </summary>
    [CreateAssetMenu(menuName = "MarsFPSKit/Services/Transport/Kcp")]
    public class Kit_TransportServiceKcp : Kit_TransportServiceBase
    {
        public override void ConnectWithString(Kit_NetworkManager manager, string connectionString)
        {
            KcpTransport transport = manager.GetComponent<KcpTransport>();
            manager.transport = transport;
            Transport.active = transport;
            manager.networkAddress = connectionString;
            manager.StartClient();
        }

        public override void ConnectWithStringAndPassword(Kit_NetworkManager manager, string connectionString, string password)
        {
            base.ConnectWithStringAndPassword(manager, connectionString, password);

            KcpTransport transport = manager.GetComponent<KcpTransport>();
            manager.transport = transport;
            Transport.active = transport;
            manager.networkAddress = connectionString;
            manager.StartClient();
        }

        public override string GetConnectionString(Kit_NetworkManager manager)
        {
            KcpTransport transport = manager.GetComponent<KcpTransport>();
            if (transport != null)
            {
                //Get Local IP
                string ip = System.Net.NetworkInformation.NetworkInterface
                    .GetAllNetworkInterfaces()
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a.Address))
                    .Select(a => a.Address.ToString())
                    .FirstOrDefault() ?? "127.0.0.1";

                //Port from 
                ushort port = transport.Port;

                return $"{ip}:{port}";
            }
            return string.Empty;
        }

        public override void Initialize(Kit_NetworkManager manager)
        {
            KcpTransport transport = manager.gameObject.AddComponent<KcpTransport>();
            manager.transport = transport;
            Transport.active = transport;
        }

        public override void StartServer(Kit_NetworkManager manager)
        {
            KcpTransport transport = manager.GetComponent<KcpTransport>();

            if (!transport)
            {
                //In case that offline mode was previously on

                //Destroy transports
                Transport[] transports = manager.gameObject.GetComponents<Transport>();

                for (int i = 0; i < transports.Length; i++)
                {
                    Destroy(transports[i]);
                }

                Initialize(manager);
                transport = manager.GetComponent<KcpTransport>();
            }

            manager.transport = transport;
            Transport.active = transport;
            manager.StartServer();
        }

        public override void StartHost(Kit_NetworkManager manager)
        {
            KcpTransport transport = manager.GetComponent<KcpTransport>();

            if (!transport)
            {
                //In case that offline mode was previously on

                //Destroy transports
                Transport[] transports = manager.gameObject.GetComponents<Transport>();

                for (int i = 0; i < transports.Length; i++)
                {
                    Destroy(transports[i]);
                }

                Initialize(manager);
                transport = manager.GetComponent<KcpTransport>();
                Transport.active = transport;
                manager.transport = transport;
            }
            else
            {
                Transport.active = transport;
                manager.transport = transport;
            }

            manager.StartHost();
        }
    }
}