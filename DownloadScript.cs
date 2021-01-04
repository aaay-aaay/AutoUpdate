using System;
using System.IO;
using Partiality.Modloader;
using UnityEngine;

namespace PastebinMachine.AutoUpdate
{
	// Token: 0x02000004 RID: 4
	public class DownloadScript : MonoBehaviour
	{
		// Token: 0x0600000C RID: 12 RVA: 0x00002BE8 File Offset: 0x00000DE8
		public void Initialize(AutoUpdateMod au, PartialityMod mod, string path, string url, string filename)
		{
			this.au = au;
			this.mod = mod;
			this.path = path + Path.DirectorySeparatorChar + filename;
			this.www = new WWW(url);
			this.ready = true;
			this.done = false;
			this.filename = filename;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002C40 File Offset: 0x00000E40
		public void Update()
		{
			if (this.ready && this.www.isDone)
			{
				this.ready = false;
				if (this.au.VerifySignature(this.mod.ModID, this.www.bytes))
				{
					File.WriteAllBytes(this.path, this.www.bytes);
				}
				else
				{
					Debug.LogError(this.mod.ModID + " UPDATE HAS INCORRECT SIGNATURE!");
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

		// Token: 0x04000017 RID: 23
		public AutoUpdateMod au;

		// Token: 0x04000018 RID: 24
		public PartialityMod mod;

		// Token: 0x04000019 RID: 25
		public string path;

		// Token: 0x0400001A RID: 26
		public WWW www;

		// Token: 0x0400001B RID: 27
		public bool ready;

		// Token: 0x0400001C RID: 28
		public bool done;

		// Token: 0x0400001D RID: 29
		public string filename;
	}
}
