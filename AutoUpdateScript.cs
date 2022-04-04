using System;
using Partiality.Modloader;
using UnityEngine;

namespace PastebinMachine.AutoUpdate
{
    // Token: 0x02000003 RID: 3
    public class AutoUpdateScript : MonoBehaviour
    {
        // Token: 0x06000009 RID: 9 RVA: 0x00002AC8 File Offset: 0x00000CC8
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

        // Token: 0x0600000A RID: 10 RVA: 0x00002B18 File Offset: 0x00000D18
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

        // Token: 0x04000010 RID: 16
        public AutoUpdateMod au;

        // Token: 0x04000011 RID: 17
        public Mod mod;

        // Token: 0x04000012 RID: 18
        public WWW www;

        // Token: 0x04000013 RID: 19
        public bool ready = false;

        // Token: 0x04000014 RID: 20
        public string text = "";

        // Token: 0x04000015 RID: 21
        public bool done = false;

        // Token: 0x04000016 RID: 22
        public int version;
    }
}
