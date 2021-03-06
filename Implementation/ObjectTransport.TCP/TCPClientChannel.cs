﻿using OTransport;
using OTransport.NetworkChannel.TCP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OTransport.NetworkChannel.TCP
{
    public class TCPClientChannel : INetworkChannel
    {
        private IPAddress IPAddress;
        private int port;
        private TcpClient tcpClient = null;
        private Client client = null;

        private Action<ReceivedMessage> onReceiveCallback = null;
        private Task ListenTask = null;
        Action<Client> onConnectCallBack = null;
        Action<Client> onDisconnectCallBack = null;

        public TCPClientChannel(string ipAddress, int Port)
        {
            tcpClient = new TcpClient();
            client = new Client(ipAddress, port);
            IPAddress = IPAddress.Parse(ipAddress);
            port = Port;

            ConnectToServer();
            ListenThread();
        }

        private void ListenThread()
        {
            Task clientTask = new Task((c) =>
            {
                while (true)
                {
                    Byte[] bytes;
                    try
                    {
                        if (!TCPUtilities.IsConnected(tcpClient))
                            throw new Exception("Client Disconnected");

                        NetworkStream ns = tcpClient.GetStream();
                        if (tcpClient.ReceiveBufferSize > 0)
                        {
                            bytes = new byte[tcpClient.ReceiveBufferSize];
                            ns.Read(bytes, 0, tcpClient.ReceiveBufferSize);
                            string msg = Encoding.ASCII.GetString(bytes);

                            ReceivedMessage message = new ReceivedMessage((Client)c, msg);

                            if (onReceiveCallback != null)
                                onReceiveCallback.Invoke(message);
                        }
                    }
                    catch
                    {
                        if (onDisconnectCallBack != null)
                            onDisconnectCallBack.Invoke((Client)c);

                        break;
                    }

                }
            }, client);
            clientTask.Start();
            ListenTask = clientTask;
        }

        private void ConnectToServer()
        {
            tcpClient.ConnectAsync(IPAddress, port).Wait();
        }
        public void OnClientConnect(Action<Client> callBack)
        {
            onConnectCallBack = callBack;
            if (client != null)
                onConnectCallBack.Invoke(client);
        }

        public void OnReceive(Action<ReceivedMessage> callBack)
        {
            onReceiveCallback = callBack;
        }

        public void SendReliable(Client client, string message)
        {
        }

        public void Stop()
        {
            if (tcpClient.Connected)
            {
                tcpClient.Client.Shutdown(SocketShutdown.Both);
                tcpClient.Client.Dispose();
            }
        }

        public void OnClientDisconnect(Action<Client> callBack)
        {
            onDisconnectCallBack = callBack;
        }

        public void SetReliable()
        {
        }

        public void SetUnreliable()
        {
            throw new NotSupportedException("This network channel does not support un-reliable sending");
        }

        public void Send(Client client, string payload)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(payload);

            NetworkStream stream = tcpClient.GetStream();

            stream.Write(data, 0, data.Length);
        }

        public void DisconnectClient(params Client[] client)
        {
            throw new NotImplementedException();
        }
    }
}
