using Lidgren.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameData
{
    public enum SENT { FROMCLIENT,TOALL,TOOTHERS,}
    public static class DataHandler
    {
        public static T ExtractMessage<T>(string inMess)
        {
            try
            {
                GamePacket<T> t = (GamePacket<T>)JsonConvert.DeserializeObject<GamePacket<T>>(inMess);
                Type typ = t.type;
                switch (t.type.Name.ToString())
                {
                    case "TestMess":
                    case "PlayerData":
                    case "LeavingData":
                    case "ErrorMess":
                    case "GameMessage":
                    case "JoinRequestMessage":
                    case "MoveMessage":
                    case "SetWorldSize":
                    case "CollectableData":
                    case "ScoreData":
                    case "TimerData":
                        return (T)Convert.ChangeType(t.val, t.type);
                    default:
                        return default(T);
                }
            }
            // return null if it's not a valid game data packet
            catch { return default(T);
            }

        }
        public static void sendNetMess<T>(NetClient client, T gameDataObject, SENT messageType)
        {
            GamePacket<T> gt = new GamePacket<T>(gameDataObject);
            gt.type = gameDataObject.GetType();
            NetOutgoingMessage sendMsg = client.CreateMessage();
            string json = JsonConvert.SerializeObject(gt);
            sendMsg.Write(json);
            switch (messageType)
            {
                case SENT.FROMCLIENT:
                    client.SendMessage(sendMsg, NetDeliveryMethod.ReliableUnordered);
                    break;

                default:
                    break;
            }
        }
        public static void sendNetMess<T>(NetServer server, T gameDataObject, SENT messageType)
        {
            GamePacket<T> gt = new GamePacket<T>(gameDataObject);
            gt.type = gameDataObject.GetType();
            NetOutgoingMessage sendMsg = server.CreateMessage();
            string json = JsonConvert.SerializeObject(gt);
            sendMsg.Write(json);
            switch (messageType)
            {
                case SENT.TOALL:
                    server.SendToAll(sendMsg, NetDeliveryMethod.ReliableUnordered);
                    break;

                default:
                    break;
            }
        }

    }
}
