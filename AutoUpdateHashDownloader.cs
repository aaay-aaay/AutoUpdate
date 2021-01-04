using System;
using Partiality.Modloader;
using UnityEngine;

namespace PastebinMachine.AutoUpdate
{
    public class AutoUpdateHashDownloader : MonoBehaviour
    {
        public void Initialize(AutoUpdateMod au)
        {
            this.au = au;
            this.www = new WWW("http://beestuff.pythonanywhere.com/audb/api/hashes");
            this.ready = true;
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
                    this.au.ProcessHashes(this.text);
                }
            }
        }
        
        public AutoUpdateMod au;
        public WWW www;
        public bool ready;
        public bool done;
        public string text;
    }
}