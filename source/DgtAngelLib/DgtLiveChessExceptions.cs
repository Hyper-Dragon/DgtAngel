using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgtAngelLib
{
    public class BoardDisconnectedException : Exception
    {
        public BoardDisconnectedException()
        {
        }

        public BoardDisconnectedException(string message)
            : base(message)
        {
        }

        public BoardDisconnectedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }


}
