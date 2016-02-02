using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBot.Csgotm
{
    class CsgotmException : Exception
    {
        public CsgotmException()
        {
            
        }

        public CsgotmException(string message)
            : base(message)
        {
        }
        public CsgotmException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
