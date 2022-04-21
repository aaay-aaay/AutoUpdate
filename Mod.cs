using System;
using System.Linq;

namespace PastebinMachine.AutoUpdate
{
    public class Mod
    {
        public Mod(object modObj, string identifier, params string[] shortIDs)
        {
            this.modObj = modObj;
            this.identifier = identifier;
            this.shorthands = shortIDs;
        }

        public object modObj;
        public string identifier;
        //used for upd exclusions
        public string[] shorthands;
    }
}
