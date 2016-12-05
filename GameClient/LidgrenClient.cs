using Lidgren.Network;
using GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sprites;
using Microsoft.Xna.Framework;
using LingrenGame;

namespace Client
{
    // This class handles lidgren messages coming in an out of the client
    public static class LidgrenClient
    {
        public static  NetPeerConfiguration ClientConfig;
        public static  NetClient client;
        public static string gameMessage = string.Empty;
        private static Vector2 worldSize;
        private static Rectangle worldRect;
        public static Player player;
        public static string timerMessage = string.Empty;

        #region Lidgren Methods
        // Start the server and connect
        public static void StartServer()
        {
            ClientConfig = new NetPeerConfiguration("GameServer");
            //for the client
            ClientConfig.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            client = new NetClient(ClientConfig);
            client.Start();
            client.DiscoverLocalPeers(12345);

        }
        // Process incoming messages if they are data messages then they are 
        public static object CheckMessages()
        {
            NetIncomingMessage ServerMessage;
            if ((ServerMessage = client.ReadMessage()) != null)
            {
                switch (ServerMessage.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // handle custom messages
                        string message = ServerMessage.ReadString();
                        //InGameMessage = "Data In " + message;
                        return(process(message));
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        gameMessage = ServerMessage.ReadString();
                        // Make sure the response matches secret  
                        if (gameMessage == ClientConfig.AppIdentifier)
                        {
                            client.Connect(ServerMessage.SenderEndPoint);
                            gameMessage = "Hit F10 to Login ";
                        }
                        else gameMessage = "Cannot Find server with the same identifier as the client ";
                        return null;
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        switch (ServerMessage.SenderConnection.Status)
                        {
                            /* .. */
                        }
                        return null;
                        break;

                    case NetIncomingMessageType.DebugMessage:
                        // handle debug messages
                        // (only received when compiled in DEBUG mode)
                        //InGameMessage = ServerMessage.ReadString();
                        return null;
                        break;

                }
                
            }
            return null;
        }

        private static object process(string inMess)
        {
            Console.WriteLine(inMess);
            object reply = null;
            if ((reply = process(DataHandler.ExtractMessage<PlayerData>(inMess))) != null)
                return reply;
            if ((reply = process(DataHandler.ExtractMessage<SetWorldSize>(inMess))) != null)
                return reply;
            if ((reply = process(DataHandler.ExtractMessage<MoveMessage>(inMess))) != null)
                return reply;
            if ((reply = process(DataHandler.ExtractMessage<LeavingData>(inMess))) != null)
                return reply;
            if ((reply = process(DataHandler.ExtractMessage<ErrorMess>(inMess))) != null)
                return reply;
            if ((reply = process(DataHandler.ExtractMessage<CollectableData>(inMess))) != null)
                return reply;
            if ((reply = process(DataHandler.ExtractMessage<ScoreData>(inMess))) != null)
                return reply;
            if((reply = process(DataHandler.ExtractMessage<TimerData>(inMess))) != null)
                return reply;
            return reply;
        }

        private static object process(TimerData timerData)
        {
            if (timerData == null) return null;
            return timerData;
        }

        private static object process(CollectableData collectableData)
        {
            if (collectableData== null) return null;
            return collectableData;

        }

        // Incoming messages 
        private static object process(PlayerData playerData)
        {
            if (playerData == null) return null;
            return playerData;
        }

        private static object process(ScoreData scoreData)
        {
            if (scoreData == null) return null;
            return scoreData;
        }

        private static object process(MoveMessage moveMessage)
        {
            if (moveMessage== null) return null;
            return moveMessage;
        }

        private static object process(ErrorMess errorMess)
        {
            if (errorMess == null) return null;
            return errorMess;
        }

        private static object process(SetWorldSize wsize)
        {
            if (wsize == null) return null;
            return wsize;
        }

        private static object process(TestMess obj)
        {
            if (obj == null) return null;
            return obj;
        }

        private static object process(LeavingData obj)
        {
            if (obj == null) return null;
            return new GameMessage {  message = obj.playerID + " has left the Game "};
        }
        
        // Outgoing Messages defined
    public static void RequeustToJoin()
        {
            JoinRequestMessage joinRequest = new JoinRequestMessage()
            { TagName = "MrCharming", Password = "STreanor" };
            DataHandler.sendNetMess<JoinRequestMessage>(client, joinRequest, SENT.FROMCLIENT);

        }
    public static void Join( PlayerData playerData)
        {
            playerData.header = "Join";
            DataHandler.sendNetMess<PlayerData>(client, playerData, SENT.FROMCLIENT);
        }
    public static void Move(string playerID, Vector2 newPos)
        {
            DataHandler.sendNetMess<MoveMessage>(client, 
                    new MoveMessage { playerID = playerID,
                    NewX = newPos.X, NewY = newPos.Y }, 
                        SENT.FROMCLIENT);
        }
    public static void Collected(CollectableData collectableData)
        {
            collectableData.ACTION = COLLECTABLE_ACTION.COLLECTED;
            DataHandler.sendNetMess<CollectableData>(client, collectableData, SENT.FROMCLIENT);
        }

        public static void SendScore(ScoreData scoreData)
        {
            DataHandler.sendNetMess<ScoreData>(client, scoreData, SENT.FROMCLIENT);
        }

        #endregion

    }
}
