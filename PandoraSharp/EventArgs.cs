using System;
using System.Collections.Generic;
using System.Text;

namespace PandoraSharp
{
    public class EventArgs<T> : EventArgs
    {
        private T argumentValue;

        public EventArgs(T value)
        {
            argumentValue = value;
        }

        public T Value
        {
            get { return argumentValue; }
        }
    }
}
