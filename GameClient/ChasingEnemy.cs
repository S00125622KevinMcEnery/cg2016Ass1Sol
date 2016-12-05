using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace Sprites
{
    class ChasingEnemy: Enemy
    {
        // Rectangle to detect if the player is close by
        private Rectangle collisionField;

       
        int collisionDistance = 1;

        public int CollisionDistance
        {
          get { return collisionDistance; }
          set { collisionDistance = value; }
        }
        bool viewCollisionField = false;
        Texture2D collisionRectTexture;
        
        public ChasingEnemy(Game g, Texture2D texture, Vector2 Position1, int framecount) 
             : base(g,texture,Position1,framecount)
        {
            //Image for the rectangle only viewed if viewCollisionField is on
            collisionRectTexture = g.Content.Load<Texture2D>("rectangle");

            startPosition = Position1;
            // calculate the collision field
            collisionField = new Rectangle((int)Position.X - texture.Width/framecount * collisionDistance,
                                            (int)Position.Y - texture.Height * collisionDistance,
                                              texture.Width/framecount * 3 * collisionDistance,
                                             texture.Height * 3 * collisionDistance);
        }

        public void follow(Player p)
        {
            // if the player passed is contained in the chase enemy collision field rectangle
            if (this.collisionField.Intersects(p.BoundingRect))
            {
                // The direction that the player is in is calculated
                // by subtracting the position vector of this enemy sprite 
                // from the position of the player
                Vector2 direction = p.Position - this.Position;
                // We have to normalize the vector otherwise the jump is too big
                direction.Normalize();
                // we update the position of the Enemy Sprite based on the 
                this.Position += direction * Velocity;
            }            
            
        }

        public override void Update(GameTime g)
        {
            // recalculate the collision field as the Chase Enemy may have moved
            // if it is following the Player
            collisionField = new Rectangle((int)Position.X - SpriteWidth * collisionDistance,
                                            (int)Position.Y - SpriteHeight * collisionDistance,
                                            SpriteWidth * 3 * collisionDistance,
                                             SpriteHeight * 3 * collisionDistance);
            // call the base update => try Enemy Class => try Sprite Class
            base.Update(g);
        }

        public override void Draw(SpriteBatch sp)
        {
            
                //sp.Begin();
                if (viewCollisionField)
                    sp.Draw(collisionRectTexture,collisionField,Color.White);
                //sp.End();
            base.Draw(sp);
        }

    }
}
