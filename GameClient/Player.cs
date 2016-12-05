using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sprites;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using GameData;
using textInput;
using System.Timers;

namespace Sprites
{
    public class Player : AnimatedSprite
    {
        public enum DIRECTION { LEFT, RIGHT, UP, DOWN,STANDING };
        DIRECTION _direction = DIRECTION.STANDING;
        
        public DIRECTION Direction
        {
            get { return _direction; }
            set { _direction = value; }
        }
        int _score;
        float _speed;
        Texture2D[] _textures;
        SoundEffect[] _directionSounds;
        SoundEffectInstance _soundPlayer;
        SpriteFont font;

        public bool Placed = false;
        public Vector2 TargetPos = Vector2.Zero;
        public bool Other = false;
        public int Score
        {
            get { return _score; }
            set { _score = value;
                   }
        }
        int _health;
        public int Health { get { return _health; } set { _health = value; } }

        // Embedded player data
        public PlayerData playerData;
        // Timer to control the flow of messages to the server
        public Timer messageTimer = new Timer(100);

        public Player(Texture2D[] tx, SoundEffect[] sounds,
            Vector2 pos, int frameCount, 
            int startScore, float speed) 
            : base(tx[0],pos,frameCount)
        {
            
            _speed = speed;
            _textures = tx;
            _directionSounds = sounds;
            _health = 100;
            messageTimer.AutoReset = true;
            messageTimer.Elapsed += MessageTimer_Elapsed;
            messageTimer.Start();
        }

        private void MessageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Placed && Position != PreviousPosition)
                Client.LidgrenClient.Move(playerData.playerID, Position);
        }

        public void MoveToStart()
        {
            PreviousPosition = Position;
            // Move quickly to the start position to avoid lagging the server with unecessary moves
            if (!Placed && Vector2.DistanceSquared(TargetPos, Position) > _speed)
            {
                Vector2 direction = TargetPos - Position;
                direction.Normalize(); // uncomment for smoother movement to start position and 
                Position += direction * _speed;
                //if(!Other)
                //    Client.LidgrenClient.Move(playerData.playerID, Position);
            }
            else
            {
                Position = TargetPos;
                //if(!Other)
                //    Client.LidgrenClient.Move(playerData.playerID, Position);
                Placed = true;
            }

        }

        public override void Update(GameTime gameTime)
        {
                PreviousPosition = Position;
                if (!Placed && TargetPos != Vector2.Zero)
                    MoveToStart();

                base.Update(gameTime);
                // TODO: Add your update logic here
                _direction = DIRECTION.STANDING;
                if (Keyboard.GetState().IsKeyDown(Keys.Left))
                {
                    _direction = DIRECTION.LEFT;
                    base.Move(new Vector2(-1, 0) * _speed);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Up))
                {
                    _direction = DIRECTION.UP;
                    base.Move(new Vector2(0, -1) * _speed);
                }
                if
                (Keyboard.GetState().IsKeyDown(Keys.Down))
                {
                    _direction = DIRECTION.DOWN;
                    base.Move(new Vector2(0, 1) * _speed);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Right))
                {
                    _direction = DIRECTION.RIGHT;
                    base.Move(new Vector2(1, 0) * _speed);
                }
                //else
                //{
                //    //Position = PreviousPosition;
                //    _direction = DIRECTION.STANDING;
                //}

                SpriteImage = _textures[(int)_direction];
                // Update Movement in all clients
                //if (Placed && Position != PreviousPosition)
                //    Client.LidgrenClient.Move(playerData.playerID, Position);
            //if (_soundPlayer == null || _soundPlayer.State == SoundState.Stopped)
            //{
            //    if (_direction != DIRECTION.STANDING)
            //    {
            //        _soundPlayer = _directionSounds[(int)_direction].CreateInstance();
            //        _soundPlayer.Play();
            //    }
            //}
        }

        public override void Draw(SpriteBatch spriteBatch)
        {

            spriteBatch.DrawString(Helpers.Font, playerData.GamerTag, Position + new Vector2(0, 50), Color.White);
            base.Draw(spriteBatch);
        }
    }
    
}
