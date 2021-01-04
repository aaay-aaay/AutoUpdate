using System;
using Partiality;
using UnityEngine;

namespace PastebinMachine.AutoUpdate
{
	// Token: 0x02000005 RID: 5
	public class InitializerScript : MonoBehaviour
	{
		// Token: 0x0600000F RID: 15 RVA: 0x00002D74 File Offset: 0x00000F74
		public void Initialize(AutoUpdateMod au)
		{
			this.au = au;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002D80 File Offset: 0x00000F80
		public void Update()
		{
			if (PartialityManager.Instance != null && !this.done)
			{
				this.done = true;
				this.au.Initialize();
			}
		}

		// Token: 0x0400001E RID: 30
		public AutoUpdateMod au;

		// Token: 0x0400001F RID: 31
		public bool done;
	}
}
