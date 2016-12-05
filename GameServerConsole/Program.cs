using GameData;
using Lidgren.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace GameServerConsole
{
    class Program
    {
        static NetPeerConfiguration config = new NetPeerConfiguration("GameServer")
        {
            Port = 12345
        };
        static NetServer server;

        struct worldSize { public int x; public int y; };
        static worldSize World = new worldSize() { x = 2000, y = 2000 };
        static TimeSpan gameStart = new TimeSpan(0, 0, 0, 20);
        static TimeSpan gameDuration = new TimeSpan(0, 0, 3, 0);
        static GameState myGameSate = GameState.WAITING;
        static System.Timers.Timer gameTimer;
        static List<PlayerData> Players = new List<PlayerData>();
        static List<PlayerData> RegisteredPlayers = new List<PlayerData>();
        #region setup Collectables
        static string[] CollectableNames = new string[5] { "One", "Two", "Three", "Four", "Five" };
        static List<CollectableData> Collectables = new List<CollectableData>();
        
        #endregion



        static List<string> badges = new List<string>();
        
        static void Main(string[] args)
        {
            // Create Badge name
            for (int i = 0; i < 13; i++)
                badges.Add("Badges_" + i.ToString());

            RegisteredPlayers = MakePlayers();

            MakeCollectables();

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);

            server = new NetServer(config);
            config.AutoFlushSendQueue = true;
            server.Start();
            // Create a game timer
            gameTimer = new System.Timers.Timer();
            TimeSpan interval = new TimeSpan(0, 0, 0, 1);
            // repeat elapsed event until stop
            gameTimer.AutoReset = true;
            // Interval has to be specified in milliseconds!!
            gameTimer.Interval = interval.TotalMilliseconds;
            // catch elapsed event
            gameTimer.Elapsed += GameTimer_Elapsed;

            //server.RegisterReceivedCallback(new SendOrPostCallback(GotMessage));
            //server.MessageReceivedEvent.WaitOne();
            //for the server
            for (;;)
            {
                // Stop the fan from going around needlessly
                server.MessageReceivedEvent.WaitOne();
                NetIncomingMessage msgIn;
                while ((msgIn = server.ReadMessage()) != null)
                {
                    //create message type handling with a switch
                    switch (msgIn.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            //This type handles all data that has been sent by you.
                            // broadcast message to all clients
                            var inMess = msgIn.ReadString();
                            process(msgIn,inMess);
                            break;
                        //All other types are for library related events (some examples)
                        case NetIncomingMessageType.DiscoveryRequest:
                                Console.WriteLine("Discovery Request from Client");
                                NetOutgoingMessage msg = server.CreateMessage();
                                //add a string as welcome text
                                msg.Write(config.AppIdentifier);
                                //send a response
                                server.SendDiscoveryResponse(msg, msgIn.SenderEndPoint);
                            break;
                        case NetIncomingMessageType.ConnectionApproval:
                            msgIn.SenderConnection.Approve();
                            break;

                        
                        case NetIncomingMessageType.StatusChanged:

                            switch ((NetConnectionStatus)msgIn.ReadByte())
                            {
                                case NetConnectionStatus.Connected:
                                    Console.WriteLine("{0} Connected", msgIn.SenderConnection);
                                    break;
                                case NetConnectionStatus.Disconnected:
                                    Console.WriteLine("{0} Disconnected", msgIn.SenderConnection);

                                    break;
                                case NetConnectionStatus.RespondedAwaitingApproval:
                                    msgIn.SenderConnection.Approve();
                                    break;
                            }
                            break;
                        default:
                            Console.WriteLine("unhandled message with type: "
                                + msgIn.MessageType);
                            break;
                    }
                    //Recycle the message to create less garbage
                    server.Recycle(msgIn);
                }
            }

        }
        // Handle count down to start and Game time
        private static void GameTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (myGameSate == GameState.STARTING)
            {
                gameStart -= new TimeSpan(0, 0, 0, 1);
                // if we have counted down
                if (myGameSate == GameState.STARTING && gameStart.TotalSeconds <= 0)
                {
                    //gameStart = new TimeSpan(0, 0, 0, 20);
                    myGameSate = GameState.STARTED;
                }
                // is we are ounting down
                if (myGameSate == GameState.STARTING && gameStart.TotalSeconds >= 0)
                {
                    Console.WriteLine("Time to Start {0} ", gameStart.TotalSeconds);
                    DataHandler.sendNetMess<TimerData>(server,
                        new TimerData { gamestate = GameState.STARTING, Seconds = (int)gameStart.TotalSeconds },
                        SENT.TOALL);
                }
            }
            if (myGameSate == GameState.STARTED)
            {
                gameDuration -= new TimeSpan(0, 0, 0, 1);
                // if we have started the game and counted down
                if (myGameSate == GameState.STARTED && gameDuration.TotalSeconds <= 0)
                {
                    DataHandler.sendNetMess<TimerData>(server,
                        new TimerData { gamestate = GameState.FINISHED,
                                    Seconds = (int)gameDuration.TotalSeconds },
                        SENT.TOALL);
                }

                if (myGameSate == GameState.STARTED && gameDuration.TotalSeconds >= 0)
                {
                    Console.WriteLine("Game Time remaining {0} ", gameDuration.TotalSeconds);
                    DataHandler.sendNetMess<TimerData>(server,
                        new TimerData { gamestate = GameState.STARTED, Seconds = (int)gameDuration.TotalSeconds },
                        SENT.TOALL);
                }
            }

        }

        private static CollectableData makeSingleCollectable()
        {
            return new CollectableData
            {
                ACTION = COLLECTABLE_ACTION.DELIVERED,
                collectableId = Guid.NewGuid().ToString(),
                CollectableName = CollectableNames[Utility.NextRandom(0,CollectableNames.Length-1)],
                X = Utility.NextRandom(100, (World.x - 100)),
                Y = Utility.NextRandom(100, (World.x - 100)),
            };
        }

        private static void MakeCollectables()
        {
            foreach (string s in CollectableNames)
            {
                Collectables.Add(new CollectableData
                {
                    ACTION = COLLECTABLE_ACTION.DELIVERED,
                     collectableId = Guid.NewGuid().ToString(),
                      CollectableName = s,
                       X = Utility.NextRandom(100,(World.x - 100)),
                        Y = Utility.NextRandom(100, (World.x - 100)),
                });

            }
        }

        private static void process(NetIncomingMessage msgIn, string inMess)
        {
            //Console.WriteLine("Data " + inMess);
            if ((process(DataHandler.ExtractMessage<TestMess>(inMess)) != null))
                return;
            if ((process(DataHandler.ExtractMessage<PlayerData>(inMess)) != null))
                return;
            if ((process(DataHandler.ExtractMessage<LeavingData>(inMess)) != null))
                return;
            if ((process(DataHandler.ExtractMessage<JoinRequestMessage>(inMess)) != null))
                return;
            if ((process(DataHandler.ExtractMessage<MoveMessage>(inMess)) != null))
                return;
            if ((process(DataHandler.ExtractMessage<CollectableData>(inMess)) != null))
                return;
            if ((process(DataHandler.ExtractMessage<ScoreData>(inMess)) != null))
                return;


        }

        private static object process(ScoreData scoreData)
        {
            if (scoreData == null) return null;
            DataHandler.sendNetMess<ScoreData>(server, scoreData, SENT.TOALL);
            return scoreData;
        }

        private static object process(MoveMessage moveMessage)
        {
            if (moveMessage == null) return null;
            var movingPlayer = Players.FirstOrDefault(plyr => plyr.playerID == moveMessage.playerID);
            movingPlayer.X = moveMessage.NewX;
            movingPlayer.Y = moveMessage.NewY;
            Console.WriteLine("{0} has moved to X: {1} Y: {2} at {3}", 
                                    movingPlayer.GamerTag, 
                                    movingPlayer.X.ToString(), 
                                    movingPlayer.Y.ToString(), 
                                    DateTime.Now.ToString());

            if (movingPlayer != null)
            {
                //movingPlayer.X = moveMessage.NewX;
                //movingPlayer.Y = moveMessage.NewY;
                //movingPlayer.header = "Moved_To";
                //NetOutgoingMessage MovedMessage = server.CreateMessage();
                //string json = JsonConvert.SerializeObject(movingPlayer);
                //MovedMessage.Write(json);
                
                //server.SendToAll(MovedMessage, NetDeliveryMethod.ReliableOrdered);
                DataHandler.sendNetMess<MoveMessage>(server, moveMessage, SENT.TOALL);
            }
            return moveMessage;


        }


        private static object process(CollectableData cd)
        {
            if (cd == null) return null;
            // only process messages where the Header is collected
            if (cd.ACTION != COLLECTABLE_ACTION.COLLECTED) return null;
            CollectableData found = Collectables.FirstOrDefault(c => c.collectableId == cd.collectableId);
            if (found == null) return null;
            // remove from server collection 
            Collectables.Remove(found);
            // remove from all clients
            cd.ACTION = COLLECTABLE_ACTION.DELETED;
            DataHandler.sendNetMess<CollectableData>(server, cd, SENT.TOALL);
            return cd;

        }

        private static object process(TestMess obj)
        {
            if (obj == null) return null;

            Console.WriteLine("{0}",obj.message);
            return obj;

        }

        private static object process(LeavingData leaving)
        {
            if (leaving == null) return null;
            var found = Players.FirstOrDefault(p => p.playerID == leaving.playerID);
            if (found != null)
                Players.Remove(found);
            Console.WriteLine("{0} has Left ", leaving.playerID);
            return found;
        }

        private static object process(JoinRequestMessage joinRequest)
        {
            if (joinRequest == null) return null;
            PlayerData found = RegisteredPlayers
                       .FirstOrDefault(p => p.GamerTag.ToUpper() == joinRequest.TagName.ToUpper()
                                      && p.Password.ToUpper() == joinRequest.Password.ToUpper());
            if (found == null)
                DataHandler.sendNetMess<ErrorMess>(server, 
                    new ErrorMess { message = " Illegal login attempt by " + joinRequest.TagName }, 
                                    SENT.TOALL);
            else
            {
                PlayerData nextchoice = null;
                // Add the player to the server copy
                // Dummy to fake the same player being used twice or more
                if (Players.FirstOrDefault(player => player.playerID == found.playerID) != null)
                {
                    // Take the first player that does not already exist in the list
                    var existing = new HashSet<string>(Players.Select(x => x.GamerTag));
                    nextchoice = RegisteredPlayers.Where(player => !existing.Contains(player.GamerTag)).First();
                    found = nextchoice;
                }
                found.header = "Accepted";
                found.X = Utility.NextRandom(100, World.x - 100);
                found.Y = Utility.NextRandom(100, World.y - 100);
                DataHandler.sendNetMess<SetWorldSize>(server,
                    new SetWorldSize
                    {
                        X = World.x,
                        Y = World.y
                    },
                        SENT.TOALL);
                DataHandler.sendNetMess<PlayerData>(server, found, SENT.TOALL);
            }
            return found;
        }

        private static object process(PlayerData p)
        {
            if (p == null) return null;
            //PlayerData extracted = p as PlayerData;
            //Console.WriteLine("{0}", extracted.header);
            
            switch (p.header)
            {
                case "Join":
                    if (Players.Count() == 0)
                    {
                        myGameSate = GameState.STARTING;
                        gameTimer.Start();
                    }
                    Players.Add(p);
                    // send the message to all clients that players are joined
                    foreach (PlayerData player in Players)
                    {
                        PlayerData joined = new PlayerData("Joined", player.imageName, player.playerID, player.GamerTag, player.X, player.Y);
                        DataHandler.sendNetMess<PlayerData>(server, joined, SENT.TOALL);
                    }
                    //Send Collectables still active in case some joins while the game is on play
                    List<CollectableData> left = Collectables
                        .Where(c => c.ACTION == COLLECTABLE_ACTION.DELIVERED)
                        .ToList();

                    foreach (CollectableData collectable in left)
                        DataHandler.sendNetMess<CollectableData>(server, collectable, SENT.TOALL);
                    // Signal the game to started
                    break;


                default:
                    break;
            }
            return p;
        }

        private static List<PlayerData> MakePlayers()
            {

            return File.ReadAllLines("random Names with scores.csv")
                                           //.Skip(1) // Only needed if the first row contains the Field names
                                           .Select(v => FromCsv(v))
                                           .OrderByDescending(s => s.PlayerName)
                                           .ToList(); ;
            }

        public static PlayerData FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');
            PlayerData player = new
                PlayerData("created",
                            badges[Utility.NextRandom(0, badges.Count() - 1)],
                            Guid.NewGuid().ToString(), values[2], 0, 0);
            player.XP = Int32.Parse(values[3]);
            player.PlayerName = values[0] + " " + values[1];
            player.Password = values[4];
            return player;

        }

    }
}
