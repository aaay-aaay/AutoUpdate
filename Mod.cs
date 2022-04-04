using System;

namespace PastebinMachine.AutoUpdate
{
    public class Mod
    {
        public Mod(object modObj, string identifier)
        {
            this.modObj = modObj;
            this.identifier = identifier;
        }
        
        public object modObj;
        public string identifier;
    }
}
