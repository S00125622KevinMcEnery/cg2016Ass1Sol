using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
    class SplashScreen
    {
        Texture2D _tx;
        public bool Active { get; set; }

        public Texture2D Tx
        {
            get
            {
                return _tx;
            }

            set
            {
                _tx = value;
            }
        }
        public SoundEffect BackingTrack { get; set; }
        public SoundEffectInstance  SoundPlayer { get; set; }
        public Vector2 Position { get; set; }

        public SplashScreen(Vector2 pos, Texture2D tx, SoundEffect sound)
        {
            _tx = tx;
            BackingTrack = sound;
            SoundPlayer = BackingTrack.CreateInstance();
            Position = pos;
        }

        public void Update()
        {
            if (Active)
            {
                if (SoundPlayer.State == SoundState.Stopped)
                    SoundPlayer.Play();
            }
        }
        public void Draw(SpriteBatch sp)
        {
            if(Active)
                sp.Draw(_tx, Position, Color.White);
        }

    }
}
