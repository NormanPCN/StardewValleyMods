using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasierMonsterEradication
{
    public interface IEasierMonsterEradicationApi
    {
        /// <summary>Return the modified monster eradication goal value. returns -1 if the passed monster could not be identified.</summary>
        /// <param name="nameOfMonster">You pass the generic monster name as indentified by the game code.
        /// "Slimes", "DustSprites", "Bats", "Serpent", "VoidSpirits", "MagmaSprite", "CaveInsects", "Mummies", "RockCrabs", "Skeletons", "PepperRex", "Duggies".
        /// You can also pass specific game monster names like "Green Slime" if that is more convenient.
        /// </param>
        public int GetMonsterGoal(string nameOfMonster);

    }
}
