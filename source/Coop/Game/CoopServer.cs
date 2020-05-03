﻿using System;
using Coop.Common;
using Coop.Multiplayer;
using Coop.Multiplayer.Network;
using Coop.Network;

namespace Coop.Game
{
    public class CoopServer
    {
        private static readonly Lazy<CoopServer> m_Instance =
            new Lazy<CoopServer>(() => new CoopServer());

        private LiteNetManagerServer m_NetManager;
        private CoopServerRail m_RailServer;

        private CoopServer()
        {
        }

        public static CoopServer Instance => m_Instance.Value;

        public Server Current { get; private set; }

        public void StartServer()
        {
            if (Current == null)
            {
                Current = new Server();
                m_RailServer = new CoopServerRail(Current, new GameEnvironment());
                Current.OnClientConnected += OnClientConnected;
                Main.Instance.Updateables.Add(m_RailServer);
                Current.Start(new ServerConfiguration());
                Log.Debug("Created server.");
            }

            if (m_NetManager == null)
            {
                m_NetManager = new LiteNetManagerServer(Current, new SaveData());
                m_NetManager.StartListening();
                Log.Debug("Setup network connection for server.");
            }
        }

        public void ShutDownServer()
        {
            Current?.Stop();
            m_NetManager?.Stop();
            m_NetManager = null;
            Current = null;
        }

        public override string ToString()
        {
            if (Current == null)
            {
                return "Server not running.";
            }

            return Current.ToString();
        }

        private void OnClientConnected(ConnectionServer connection)
        {
            connection.OnClientJoined += m_RailServer.ClientJoined;
            connection.OnDisconnected += m_RailServer.Disconnected;
        }
    }
}