using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utilities
{
    static public class Utility
    {
        static Random r = new Random();

        public static int NextRandom(int max)
        {
            return r.Next(max);
        }

        public static int NextRandom(int min, int max)
        {
            return r.Next(min,max);

        }
    }
}
