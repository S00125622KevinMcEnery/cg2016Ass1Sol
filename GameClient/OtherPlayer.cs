using GameData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using textInput;

namespace Client
{
    public class OtherPlayer : AnimatedSprite
    {
        // Embedded player data
        public PlayerData playerData;


        public OtherPlayer(Texture2D tx, Vector2 pos, int frameCount, PlayerData data): base(tx,pos,frameCount)
        {
            playerData = data;
        }

        public override void Update(GameTime gametime)
        {
            base.Update(gametime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if(playerData!= null)
                spriteBatch.DrawString(Helpers.Font, playerData.GamerTag, Position + new Vector2(0, 50), Color.White);
            base.Draw(spriteBatch);

        }

    }
}
