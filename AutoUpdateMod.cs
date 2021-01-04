using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Partiality;
using Partiality.Modloader;
using RWCustom;
using Steamworks;
using UnityEngine;

/*
EXAMPLE:
        {
        	"hash": "gNbFtOMl+DSxp5MGfWO70QjGMEc8WQ8Wc2muddSjbroZB9RCThvFbMLs2EAoVhCsHGLwPpWtkrl9LZukV1Nszg==",
        	"key": 2,
        	"mod": 0,
        	"sig": "FAFwZ3/G737KZstm5Kvt8sDnJImHcFn+IYl4ZXjCaVbzV45+UimkDUAack+UwV6Tjtq+bZEYBbz/GmZnbyruR8782XYfT7iKuy4oI8oNpJHxIcmO0Xyz+T0rsBNcwm5Mcrwe+t5IGCoZ+2RyIu7Tq+9f2t4ow5bU8Zfc3d1UIMWW2OICkYhJ9wnF+soHsALX9RlGqvyGMxv0YaTDrSKxllg/zna4DvokCh8lhPTZPLAN0K/UqK3QH2aAs66RS/dYcgEpEnAp5N5YWLTgbtnGswCAlWzXrJ8RM/aCr9f9UGvgezPEyJDN2UfYspHrpf6eOkScfpzgNN06WCIskOx/RmMygERYwzd90VuxhAGCStO8abVLb+vPwWnUignxup52w10quBLTsWCA1XCfN6xy0rBDlxB297FkxH4mPVeHq0BFM5ve5l/HCnfRpuYV1Y02mHuo/aN+r9QvbbYzHtr+DJhe3YpUn4BKEeRyNKKu/PkRiQtWLa1+H3hy93lukwonmzCxgznKXfKxnZ4TAGtj8WZUP1uYyC9Xdlbfu1fKmZ0/sVYluS5hiT3k5X8sLXREazOCmlY8GoQfTH9gatQZm/Jgf+9io3OdkvFISax8wj1x+sR+/NONvpoEPQxyGr8wHPx8H0NwNIXVDCpuPDYdwA2Ju3XmQebGepdpXBE/9Ng="
        }
*/

namespace PastebinMachine.AutoUpdate
{
	// Token: 0x02000002 RID: 2
	public class AutoUpdateMod : PartialityMod
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public override void Init()
		{
			this.ModID = "Auto Update";
		}

		// Token: 0x06000002 RID: 2 RVA: 0x0000205E File Offset: 0x0000025E
		public override void OnLoad()
		{
			new GameObject("AutoUpdate").AddComponent<InitializerScript>().Initialize(this);
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002078 File Offset: 0x00000278
		public void Initialize()
		{
			foreach (PartialityMod partialityMod in PartialityManager.Instance.modManager.loadedMods)
			{
				Debug.Log("Checking " + partialityMod);
				FieldInfo field = partialityMod.GetType().GetField("updateURL");
				FieldInfo field2 = partialityMod.GetType().GetField("version");
				FieldInfo field3 = partialityMod.GetType().GetField("keyE");
				FieldInfo field4 = partialityMod.GetType().GetField("keyN");
				if (field == null && field2 == null && field3 == null && field4 == null)
				{
					Debug.Log(partialityMod.ModID + " does not support AutoUpdate.");
				}
				else if (field == null || field2 == null || field3 == null || field4 == null)
				{
					Debug.LogError("Cannot update " + partialityMod.ModID + ", one or more required fields are missing.");
				}
				else if (field.FieldType != typeof(string) || field2.FieldType != typeof(int) || field3.FieldType != typeof(string) || field4.FieldType != typeof(string))
				{
					Debug.LogError("Cannot update " + partialityMod.ModID + ", one or more fields have the incorrect type.");
				}
				else
				{
					RSAParameters value = default(RSAParameters);
					value.Exponent = Convert.FromBase64String((string)field3.GetValue(partialityMod));
					value.Modulus = Convert.FromBase64String((string)field4.GetValue(partialityMod));
					this.modKeys[partialityMod.ModID] = value;
					this.scripts.Add(new GameObject("AutoUpdateMod_" + partialityMod.ModID).AddComponent<AutoUpdateScript>().Initialize(this, partialityMod, (string)field.GetValue(partialityMod), (int)field2.GetValue(partialityMod)));
				}
                
                try
                {
                    byte[] data = File.ReadAllBytes(partialityMod.GetType().Assembly.Location);
                    using (SHA512 shaM = new SHA512Managed())
                    {
                        string hash = Convert.ToBase64String(shaM.ComputeHash(File.ReadAllBytes(partialityMod.GetType().Assembly.Location)));
                        Debug.Log("Got hash: " + hash);
                        hashes[hash] = partialityMod;
                    }
                }
                catch
                {
                }
			}
            new GameObject("AutoUpdateHashChecker").AddComponent<AutoUpdateHashDownloader>().Initialize(this);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000022BC File Offset: 0x000004BC
		public void ProcessResult(PartialityMod amod, string text, int version)
		{
			Debug.Log("loading json " + text + " for mod " + amod.ModID);
			Dictionary<string, object> dictionary = text.dictionaryFromJson();
			Debug.Log("loaded json " + text + " for mod " + amod.ModID);
			Debug.Log(string.Concat(new object[]
			{
				"version is ",
				dictionary["version"],
				" of type ",
				dictionary["version"].GetType()
			}));
			this.modSigs[amod.ModID] = Convert.FromBase64String((string)dictionary["sig"]);
			this.modURLs[amod.ModID] = (string)dictionary["url"];
			if ((int)((long)dictionary["version"]) > version)
			{
				Debug.Log("Update required for " + amod.ModID);
				this.needUpdate.Add(amod);
			}
			else
			{
				Debug.Log("No update required for " + amod.ModID);
			}
			if (this.scripts.Count == 0)
			{
				Debug.Log(string.Concat(new object[]
				{
					"Checked all mods, ",
					this.needUpdate.Count,
					" updates to download"
				}));
				if (this.needUpdate.Count != 0)
				{
					Directory.CreateDirectory(Custom.RootFolderDirectory() + "UpdatedMods");
					foreach (PartialityMod partialityMod in this.needUpdate)
					{
						new GameObject("Download_" + partialityMod.ModID).AddComponent<DownloadScript>().Initialize(this, partialityMod, Custom.RootFolderDirectory() + "UpdatedMods", this.modURLs[partialityMod.ModID], Path.GetFileName(partialityMod.GetType().Assembly.Location));
					}
					if (this.needUpdate.Count == 0)
					{
						this.Done();
					}
				}
			}
		}
        
        public void ProcessHashes(string text)
        {
            Debug.Log("loading hash json " + text);
            List<object> list = text.listFromJson();
            int i = 0;
            foreach (object obj in list)
            {
                Dictionary<string, object> asDict = obj as Dictionary<string, object>;
                Debug.Log("...");
                string hash = (string)(asDict["hash"]);
                Debug.Log("hash...");
                int key = (int)(long)(asDict["key"]);
                Debug.Log("key...");
                int mod = (int)(long)(asDict["mod"]);
                Debug.Log("mod...");
                string sig = (string)(asDict["sig"]);
                Debug.Log("sig!");
                if (hashes.ContainsKey(hash)) new GameObject("DownloadKeyForHash_" + (i++)).AddComponent<DownloadHashKeyScript>().Initialize(this, hash, key, mod, sig);
            }
        }
        
        public void ProcessKeyData(string text, string hash, int key, int mod, string sig)
        {
            Dictionary<string, object> obj = text.dictionaryFromJson();
            string keyE = (string)obj["e"];
            string keyN = (string)obj["n"];
            
            byte[] sigData = Convert.FromBase64String(sig);
            string signedData = "audbhash-" + hash + "-" + keyE + "-" + keyN + "-" + key + "-" + mod;
            RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider();
            rsacryptoServiceProvider.ImportParameters(this.modKeys[this.ModID]);
            if (!rsacryptoServiceProvider.VerifyData(Encoding.ASCII.GetBytes(signedData), "SHA512", sigData))
            {
                Debug.LogError("INVALID HASH SIGNATURE! " + hash);
                return;
            }
            PartialityMod partialityMod = hashes[hash];
            
            RSAParameters rsaParams = default(RSAParameters);
            rsaParams.Exponent = Convert.FromBase64String(keyE);
            rsaParams.Modulus = Convert.FromBase64String(keyN);
            this.modKeys[partialityMod.ModID] = rsaParams;
            
            this.scripts.Add(new GameObject("AutoUpdateMod_" + partialityMod.ModID).AddComponent<AutoUpdateScript>().Initialize(this, partialityMod, "http://beestuff.pythonanywhere.com/audb/api/mods/" + key + "/" + mod, -1));
            // new GameObject("Download_" + partialityMod.ModID).AddComponent<DownloadScript>().Initialize(this, mod, Custom.RootFolderDirectory() + "UpdatedMods", "http://beestuff.pythonanywhere.com/audb/api/mods/", Path.GetFileName(partialityMod.GetType().Assembly.Location));
        }

		// Token: 0x06000005 RID: 5 RVA: 0x000028C8 File Offset: 0x00000AC8
		public bool VerifySignature(string modid, byte[] data)
		{
			RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider();
			rsacryptoServiceProvider.ImportParameters(this.modKeys[modid]);
			Debug.Log(string.Concat(new object[]
			{
				"Verifying signature ",
				this.modSigs[modid],
				" for mod ",
				modid
			}));
			return rsacryptoServiceProvider.VerifyData(data, "SHA512", this.modSigs[modid]);
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002944 File Offset: 0x00000B44
		public string GetAppID()
		{
            throw new Exception();
            /*
			string text = SteamUtils.GetAppID().ToString();
			Debug.Log("App ID: " + text);
			return text;
            */
		}
        
        public string GetLaunchCommand()
        {
            try
            {
                return "steam://rungameid/" + GetAppID();
            }
            catch
            {
                Debug.Log("Failed to get appid - fall back to executable");
                return "RainWorld.exe";
            }
        }

		// Token: 0x06000007 RID: 7 RVA: 0x0000297C File Offset: 0x00000B7C
		public void Done()
		{
			Debug.Log("Calling Done()");
			if (this.actuallyUpdated)
			{
				System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe");
				/*processStartInfo.Arguments*/ string procArgs = string.Concat(new string[]
				{
					"/c xcopy /Y UpdatedMods \"",
                    DirectoryToLoadModsFrom(),
					"\" && rd /S /Q UpdatedMods"
				});
                Debug.Log(procArgs);
                processStartInfo.Arguments = procArgs;
				processStartInfo.WorkingDirectory = Custom.RootFolderDirectory();
				Debug.Log("Quitting");
				System.Diagnostics.Process.Start(processStartInfo);
				Application.Quit();
			}
            /*
            if (this.actuallyUpdated)
            {
                File.WriteAllText("__autoUpdate_internal.bat", string.Concat(new string[]
                {
                    "pause && xcopy /Y UpdatedMods \"",
                    DirectoryToLoadModsFrom(),
                    "\" && rd /S /Q UpdatedMods && start ",
                    GetLaunchCommand()
                }));
                Debug.Log("Quitting");
                System.Diagnostics.Process.Start("cmd.exe", "/c __autoUpdate_internal.bat");
                Application.Quit();
            }
            */
		}
        
        public string DirectoryToLoadModsFrom()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

		// Token: 0x04000001 RID: 1
		public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/0/0";

		// Token: 0x04000002 RID: 2
		public int version = 13;

		// Token: 0x04000003 RID: 3
		public string keyE = "AQAB";

		// Token: 0x04000004 RID: 4
		public string keyN = "yu7XMmICrzuavyZRGWoknFIbJX4N4zh3mFPOyfzmQkil2axVIyWx5ogCdQ3OTdSZ0xpQ3yiZ7zqbguLu+UWZMfLOBKQZOs52A9OyzeYm7iMALmcLWo6OdndcMc1Uc4ZdVtK1CRoPeUVUhdBfk2xwjx+CvZUlQZ26N1MZVV0nq54IOEJzC9qQnVNgeeHxO1lRUTdg5ZyYb7I2BhHfpDWyTvUp6d5m6+HPKoalC4OZSfmIjRAi5UVDXNRWn05zeT+3BJ2GbKttwvoEa6zrkVuFfOOe9eOAWO3thXmq9vJLeF36xCYbUJMkGR2M5kDySfvoC7pzbzyZ204rXYpxxXyWPP5CaaZFP93iprZXlSO3XfIWwws+R1QHB6bv5chKxTZmy/Imo4M3kNLo5B2NR/ZPWbJqjew3ytj0A+2j/RVwV9CIwPlN4P50uwFm+Mr0OF2GZ6vU0s/WM7rE78+8Wwbgcw6rTReKhVezkCCtOdPkBIOYv3qmLK2S71NPN2ulhMHD9oj4t0uidgz8pNGtmygHAm45m2zeJOhs5Q/YDsTv5P7xD19yfVcn5uHpSzRIJwH5/DU1+aiSAIRMpwhF4XTUw73+pBujdghZdbdqe2CL1juw7XCa+XfJNtsUYrg+jPaCEUsbMuNxdFbvS0Jleiu3C8KPNKDQaZ7QQMnEJXeusdU=";

		// Token: 0x04000005 RID: 5
		public List<AutoUpdateScript> scripts = new List<AutoUpdateScript>();

		// Token: 0x04000006 RID: 6
		public List<PartialityMod> needUpdate = new List<PartialityMod>();

		// Token: 0x04000007 RID: 7
		public List<PartialityMod> needRename = new List<PartialityMod>();

		// Token: 0x04000008 RID: 8
		public Dictionary<PartialityMod, string> newNames = new Dictionary<PartialityMod, string>();

		// Token: 0x0400000A RID: 10
		public bool actuallyUpdated = false;

		// Token: 0x0400000B RID: 11
		public object lockObj = new object();

		// Token: 0x0400000C RID: 12
		public object otherLockObj = new object();

		// Token: 0x0400000D RID: 13
		public Dictionary<string, RSAParameters> modKeys = new Dictionary<string, RSAParameters>();

		// Token: 0x0400000E RID: 14
		public Dictionary<string, byte[]> modSigs = new Dictionary<string, byte[]>();

		// Token: 0x0400000F RID: 15
		public Dictionary<string, string> modURLs = new Dictionary<string, string>();
        
        public Dictionary<string, PartialityMod> hashes = new Dictionary<string, PartialityMod>();
	}
}