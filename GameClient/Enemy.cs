using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace Sprites
{
    class Enemy : AnimatedSprite
    {
        protected Game myGame;
        private float _velocity = 4.0f;

        public float Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }
        protected Vector2 startPosition;
        protected Vector2 TargetPosition;

        public Enemy(Game g, Texture2D texture, Vector2 userPosition, int framecount) : base(texture,userPosition,framecount)
        {
            // Need to see the Game to access Viewport
            myGame = g; 
            startPosition = userPosition;
        }
        
    }
}
