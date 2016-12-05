using Engine.Engines;
using GameData;
using Lidgren.Network;
using LingrenGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NSLoader;
using Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using textInput;
using Utilities;

namespace Client
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        #region Game Variables
        // Graphics objects
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SoundEffect collectSound;
        SpriteFont timeFont;
        SpriteFont playerFont;


        string timeMessage = string.Empty;
        // Collectables which have collectable Data objects inside
        List<Collectable> Collectables = new List<Collectable>();
        int collectablesAliveCount;

        Vector2 BackCameraPos = Vector2.Zero;
        Player player;
        //ChasingEnemy[] chasers = new ChasingEnemy[5];
        FollowCamera followCamera;
        Vector2 worldSize = Vector2.Zero; //= new Vector2(2000, 2000);
        Rectangle worldRect;
        private Texture2D txbackground;

        List<OtherPlayer> OtherPlayers = new List<OtherPlayer>();
        Dictionary<string, Texture2D> playerTextures = new Dictionary<string, Texture2D>();
        List<ScoreData> scores = new List<ScoreData>();
        private Texture2D background;
        private InputEngine inputEngine;
        private bool playerJoined;

        GameState myState = GameState.WAITING;
        #endregion
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Helpers.GraphicsDevice = GraphicsDevice;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            //worldRect = new Rectangle(new Point(0, 0), worldSize.ToPoint());
            //followCamera = new FollowCamera(this, Vector2.Zero, worldSize);
            inputEngine = new InputEngine(this);
            LidgrenClient.StartServer();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            txbackground = Content.Load<Texture2D>(@"Textures\background");
            timeFont = Content.Load<SpriteFont>(@"Fonts\ScoreFont");
            playerFont = Content.Load<SpriteFont>(@"Fonts\PlayerFont");
            collectSound = Content.Load<SoundEffect>(@"Audio\2b");

            Services.AddService<SpriteFont>(timeFont);
            Helpers.Font = timeFont;
            Services.AddService<SpriteBatch>(spriteBatch);
            new FadeTextManager(this);
            playerTextures = Loader.ContentLoad<Texture2D>(Content, @".\PlayerImages\");
            background = Content.Load<Texture2D>(@"Textures\background");

       }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            
            if(InputEngine.IsKeyPressed(Keys.F10) && !playerJoined)
            {
                LidgrenClient.RequeustToJoin();
            }

            #region update player
            if (player != null)
            {
                player.Update(gameTime);
                player.Position = Vector2.Clamp(player.Position, Vector2.Zero, (worldSize - new Vector2(player.SpriteWidth, player.SpriteHeight)));
            }
            foreach (OtherPlayer p in OtherPlayers)
                p.Update(gameTime);
            #endregion
            #region Update Collectables
            if(player!=null)
                foreach (Collectable c  in Collectables)
                    c.Update(gameTime);

            if (player != null)
                foreach (Collectable item in Collectables)
            {
                if (item.Alive && player.collisionDetect(item))
                {
                    collectablesAliveCount--;
                    player.Score += item.Score;
                    item.Alive = false;
                    collectSound.Play();
                    LidgrenClient.Collected(item.collectableData);
                    LidgrenClient.SendScore(new ScoreData { playerID = player.playerData.playerID, Tag = player.playerData.GamerTag, score = player.Score });
                    }
                item.Update(gameTime);
            }
            #endregion Collectables
            #region Camera Control
            if(player!= null & followCamera != null)
                    followCamera.Follow(player);    
            #endregion
            catchMessage(LidgrenClient.CheckMessages());

            base.Update(gameTime);
        }

        

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (followCamera != null)
                spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, followCamera.CameraTransform);
            else spriteBatch.Begin();
            spriteBatch.Draw(txbackground, worldRect, Color.White);
            if(player!=null)
                player.Draw(spriteBatch);

            foreach (OtherPlayer p in OtherPlayers)
                p.Draw(spriteBatch);

            foreach (Collectable c in Collectables)
               c.Draw(spriteBatch);

            spriteBatch.End();

            spriteBatch.Begin();
            spriteBatch.DrawString(timeFont, timeMessage, new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(timeFont, LidgrenClient.gameMessage, 
                new Vector2(GraphicsDevice.Viewport.Width - timeFont.MeasureString(LidgrenClient.gameMessage).X,
                GraphicsDevice.Viewport.Height - 30), Color.White);
            // get the longest string and offest to the top right of the Viewport
            Vector2 StringSize = Vector2.Zero;
            var scoreList = scores.Select(p => new { str = p.Tag + " " +  p.score.ToString() }).
                                        OrderBy(p => p.str.Length).ToList();
            if (scoreList.Count() > 0)
            {
                StringSize = timeFont.MeasureString(scoreList.First().str);
                
                float Yoffset = StringSize.Y;
                int i = 1;
                foreach (var score in scoreList)
                    spriteBatch.DrawString(timeFont, score.str,
                        new Vector2(GraphicsDevice.Viewport.Width - (StringSize.X + 50), Yoffset * i++), 
                        Color.White
                        );
           }
            spriteBatch.End();


            base.Draw(gameTime);
        }

        #region process game messages
        private void catchMessage(object obj)
        {
            if (obj == null) return;
            Type type = obj.GetType();
            switch (type.Name.ToString())
            {
                case "CollectableData":
                    CollectableData cd = (CollectableData)Convert.ChangeType(obj, type);
                    process(cd);
                    break;
                case "ScoreData":
                    ScoreData sd = (ScoreData)Convert.ChangeType(obj, type);
                    process(sd);
                    break;

                case "PlayerData":
                    PlayerData pd = (PlayerData)Convert.ChangeType(obj, type);
                    process(pd);
                    break;
                //case "LeavingData":
                //    process(ld);
                //    LeavingData ld = (LeavingData)Convert.ChangeType(obj, type);
                //    break;
                case "ErrorMess":
                    ErrorMess err = (ErrorMess)Convert.ChangeType(obj, type);
                    process(err);
                    break;
                case "GameMessage":
                    GameMessage msg = (GameMessage)Convert.ChangeType(obj, type);
                    process(msg);
                    break;
         
                case "MoveMessage":
                    MoveMessage moveMsg = (MoveMessage)Convert.ChangeType(obj, type);
                    process(moveMsg);
                    break;

                case "SetWorldSize":
                    SetWorldSize wsize = (SetWorldSize)Convert.ChangeType(obj, type);
                    process(wsize);
                    break;

                case "TimerData":
                    TimerData tmess = (TimerData)Convert.ChangeType(obj, type);
                    process(tmess);
                    break;

                default:
                    break;
            }

        }

        private object process(TimerData timerData)
        {
            switch (timerData.gamestate)
            {
                case GameState.STARTING:
                    timeMessage = "Time to Start " + timerData.Seconds.ToString();
                    myState = timerData.gamestate;                    
                    break;
                case GameState.STARTED:
                    timeMessage = "Time Remaining " + timerData.Seconds.ToString();
                    myState = timerData.gamestate;
                    break;
                case GameState.FINISHED:
                    break;
            }
            return timerData;
        }

        private void process(ScoreData sd)
        {
            ScoreData found;
            if ((found = scores.FirstOrDefault(s => s.playerID == sd.playerID)) != null)
                found.score = sd.score;
            else scores.Add(sd);
        }
        // Get score data for player Tag


        private void process(CollectableData cd)
        {
            switch(cd.ACTION)
            {
                // if a collectable is delivered and it is not in the collection 
                // Then add it
                case COLLECTABLE_ACTION.DELIVERED:
                    if (Collectables.FirstOrDefault(c => c.collectableData.CollectableName == cd.CollectableName) != null)
                        return;
                    Collectables.Add(new Collectable(
                        Content.Load<Texture2D>(@"Collectables\" + cd.CollectableName), 
                        new Vector2(cd.X, cd.Y), 1)
                    { collectableData = cd });
                    collectablesAliveCount = Collectables.Count();
                    break;

                case COLLECTABLE_ACTION.DELETED:
                    Collectable found;
                    if ( (found = Collectables.FirstOrDefault(c => c.collectableData.collectableId== cd.collectableId)) != null)
                        Collectables.Remove(found);
                    collectablesAliveCount = Collectables.Count();
                    break;
            }
        }
        private void process(MoveMessage moveMsg)
        {
            OtherPlayer found = OtherPlayers.FirstOrDefault(o => o.playerData.playerID == moveMsg.playerID);
            if(found!= null)
            {
                found.Position = new Vector2(moveMsg.NewX, moveMsg.NewY);
            }
        }
        private object process(ErrorMess errorMess)
        {
            if (errorMess == null) return null;
            new FadeText(this, new Vector2(20, 20), errorMess.message);
            //pwd = string.Empty;
            //userName = string.Empty;
            //loginKeyboard.Clear();
            return errorMess;
        }
        private object process(GameMessage Mess)
        {
            if (Mess == null) return null;
            new FadeText(this, new Vector2(20, 20), Mess.message);
            return Mess;
        }
        private object process(SetWorldSize wsize)
        {
            if (wsize == null) return null;
            //if (player != null)
            //{
            // Camera must not be reset on a broadcast message
            if (worldSize == Vector2.Zero)
            {
                worldSize = new Vector2(wsize.X, wsize.Y);
                worldRect = new Rectangle(new Point(0, 0), worldSize.ToPoint());
                Vector2 CenterView = new Vector2(GraphicsDevice.Viewport.Width / 2,
                                                    GraphicsDevice.Viewport.Height / 2);

                followCamera = new FollowCamera(this, Vector2.Zero,
                                                       worldSize);
            }
            //}
            //else new FadeText(this, new Vector2(20, 20), "Problem setting up world size for camera");
            return wsize;
        }
        private object process(TestMess obj)
        {
            if (obj == null) return null;
            Console.WriteLine("{0}", obj.message);
            return obj;
        }
        private object process(LeavingData obj)
        {
            if (obj == null) return null;
            new FadeText(this, Vector2.Zero, obj.Tag + " has left the Game ");
            return obj;
        }
        private object process(PlayerData playerData)
        {
            //PlayerData otherPlayer = DataHandler.ExtractMessage<PlayerData>(v);
            if (playerData == null) return null;
            if (player == null)
            {
                if (playerData.header == "Accepted")
                {
                    setupPlayer(playerData);
                }
            }
            // if it's the same player back just ignore it
            //if ((playerData.playerID == player.playerData.playerID))
            //    return null;

            switch (playerData.header)
            {
                case "Joined":
                    // Current Player getting Joined Message Back
                    if (playerData.playerID == player.playerData.playerID)
                    {
                        playerJoined = true;
                    }
                    else
                    {
                        // Add the player to this game as another player
                        string ImageName = "Badges_" + Utility.NextRandom(0, playerTextures.Count - 1);
                        // Create other players
                        OtherPlayer newPlayer = createOtherPlayer(playerData);
                        new FadeText(this, Vector2.Zero, playerData.GamerTag + " has Joined the Game ");
                        OtherPlayers.Add(newPlayer);
                    }
                    break;
                case "Moved_To":
                    // Ignore Move to for this client
                    if (playerData.playerID == player.playerData.playerID) return null;
                    // Add the player to this game as another player
                    var movedPlayer = OtherPlayers.Find(p => p.playerData.playerID == playerData.playerID);
                    if (movedPlayer != null)
                    {
                        movedPlayer.Position = new Vector2(playerData.X, playerData.Y);
                    }
                    break;

                case "Left":
                    OtherPlayer found = OtherPlayers
                        .Find(o => o.playerData.playerID == playerData.playerID);
                    if (found != null)
                    {
                        new FadeText(this, Vector2.Zero, found.playerData.GamerTag + " has left the Game ");
                        OtherPlayers.Remove(found);
                    }
                    break;
                default:
                    break;
            }
            return playerData;
        }
        private OtherPlayer createOtherPlayer(PlayerData playerData)
        {
            Texture2D tx = Content.Load<Texture2D>(@"PlayerImages\" + playerData.imageName);
            OtherPlayer otherPlayer = new OtherPlayer(tx, new Vector2(playerData.X, playerData.Y), 1,playerData);
            // Create a score for the other player in all clients
            LidgrenClient.SendScore(new ScoreData { playerID = playerData.playerID,
                                        Tag = playerData.GamerTag,
                                        score = 0 });
            return (otherPlayer);
        }
        private void setupPlayer(PlayerData playerData)
        {
            

            Texture2D[] txs = new Texture2D[5];
            SoundEffect[] sounds = new SoundEffect[5];
            txs[(int)Player.DIRECTION.LEFT] = Content.Load<Texture2D>(@"Textures\right");
            txs[(int)Player.DIRECTION.RIGHT] = Content.Load<Texture2D>(@"Textures\right");
            txs[(int)Player.DIRECTION.UP] = Content.Load<Texture2D>(@"Textures\up");
            txs[(int)Player.DIRECTION.DOWN] = Content.Load<Texture2D>(@"Textures\down");
            txs[(int)Player.DIRECTION.STANDING] = Content.Load<Texture2D>(@"Textures\stand");


            for (int i = 0; i < sounds.Length; i++)
                sounds[i] = Content.Load<SoundEffect>(@"Audio\PlayerDirection\" + i.ToString());
            player = new Player(txs, sounds, new Vector2(0, 0), 8, 0, 5);
            player.Position = player.PreviousPosition = new Vector2(GraphicsDevice.Viewport.Width / 2 - player.SpriteWidth / 2,
                                          GraphicsDevice.Viewport.Height / 2 - player.SpriteHeight / 2);
            player.TargetPos = new Vector2(worldSize.X / 2, worldSize.Y / 2);
            //PrevPlayerPosition = player.Position;
            player.playerData = playerData;
            // Join the current Game
            LidgrenClient.Join(player.playerData);
            LidgrenClient.SendScore(new ScoreData { playerID = playerData.playerID, Tag = playerData.GamerTag, score = player.Score });
        

        }
        #endregion


    }
}
