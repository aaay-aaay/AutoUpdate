using System;
using System.IO;
using Partiality.Modloader;
using UnityEngine;

namespace PastebinMachine.AutoUpdate
{
    public class DownloadScript : MonoBehaviour
    {
        public void Initialize(AutoUpdateMod au, Mod mod, string path, string url, string filename)
        {
            this.au = au;
            this.mod = mod;
            this.path = path + Path.DirectorySeparatorChar + filename;
            this.www = new WWW(url);
            this.ready = true;
            this.done = false;
            this.filename = filename;
        }

        public void Update()
        {
            if (this.ready && this.www.isDone)
            {
                this.ready = false;
                if (this.au.VerifySignature(this.mod.identifier, this.www.bytes))
                {
                    File.WriteAllBytes(this.path, this.www.bytes);
                }
                else
                {
                    Debug.LogError(this.mod.identifier + " UPDATE HAS INCORRECT SIGNATURE!");
                }
                lock (this.au.otherLockObj)
                {
                    if (File.Exists(this.path))
                    {
                        this.au.actuallyUpdated = true;
                    }
                    this.au.needUpdate.Remove(this.mod);
                    if (this.au.needUpdate.Count == 0)
                    {
                        Debug.Log("Calling Done");
                        this.au.Done();
                    }
                }
            }
        }

        public AutoUpdateMod au;

        public Mod mod;

        public string path;

        public WWW www;

        public bool ready;

        public bool done;

        public string filename;
    }
}
