using System;
using Partiality.Modloader;
using UnityEngine;

namespace PastebinMachine.AutoUpdate
{
    public class DownloadHashKeyScript : MonoBehaviour
    {
        public void Initialize(AutoUpdateMod au, string hash, int key, int mod, string sig)
        {
            this.au = au;
            this.www = new WWW("http://beestuff.pythonanywhere.com/keydb/api/keys/" + key);
            this.ready = true;
            this.hash = hash;
            this.key = key;
            this.mod = mod;
            this.sig = sig;
        }

        public void Update()
        {
            if (this.ready && this.www.isDone)
            {
                this.ready = false;
                this.done = true;
                this.text = this.www.text;
                lock (this.au.lockObj)
                {
                    this.au.ProcessKeyData(this.text, this.hash, this.key, this.mod, this.sig);
                }
            }
        }

        public AutoUpdateMod au;
        public WWW www;
        public bool ready;
        public bool done;
        public string text;
        public string hash;
        public int key;
        public int mod;
        public string sig;
    }
}
