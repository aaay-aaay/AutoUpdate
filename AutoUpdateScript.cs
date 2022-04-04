using System;
using Partiality.Modloader;
using UnityEngine;

namespace PastebinMachine.AutoUpdate
{
    public class AutoUpdateScript : MonoBehaviour
    {
        public AutoUpdateScript Initialize(AutoUpdateMod au, Mod mod, string url, int version)
        {
            this.au = au;
            this.mod = mod;
            Debug.Log("Creating WWW at " + url);
            this.www = new WWW(url);
            this.version = version;
            this.ready = true;
            return this;
        }

        public void Update()
        {
            if (this.ready && this.www.isDone)
            {
                this.ready = false;
                this.text = this.www.text;
                this.done = true;
                lock (this.au.lockObj)
                {
                    this.au.scripts.Remove(this);
                    this.au.ProcessResult(this.mod, this.text, this.version);
                }
            }
        }

        public AutoUpdateMod au;

        public Mod mod;

        public WWW www;

        public bool ready = false;

        public string text = "";

        public bool done = false;

        public int version;
    }
}
