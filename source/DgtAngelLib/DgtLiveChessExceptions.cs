using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgtAngelLib
{
    public class LiveChessDisconnectedException : Exception
    {
        public LiveChessDisconnectedException()
        {
        }

        public LiveChessDisconnectedException(string message)
            : base(message)
        {
        }

        public LiveChessDisconnectedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

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
