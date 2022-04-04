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
using BepInEx;

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
    public class AutoUpdateMod : PartialityMod
    {
        public override void Init()
        {
            this.ModID = "Auto Update";
            this.Version = this.version.ToString();
        }

        public override void OnLoad()
        {
            new GameObject("AutoUpdate").AddComponent<InitializerScript>().Initialize(this);
        }

        public void Initialize()
        {
            List<Mod> mods = new List<Mod>();
            /*
            foreach (Mod partialityMod in PartialityManager.Instance.modManager.loadedMods)
            {
                mods.append(new Mod(partialityMod, partialityMod.identifier));
            }
            */
            if (PartialityExists()) AddMods(mods);
            if (BepinexExists()) AddBepinexPlugins(mods);
            foreach (Mod mod in mods)
            {
                Debug.Log("Checking " + mod.identifier);
                FieldInfo updateURL = mod.modObj.GetType().GetField("updateURL");
                FieldInfo version = mod.modObj.GetType().GetField("version");
                FieldInfo keyE = mod.modObj.GetType().GetField("keyE");
                FieldInfo keyN = mod.modObj.GetType().GetField("keyN");
                if (updateURL == null && version == null && keyE == null && keyN == null)
                {
                    Debug.Log(mod.identifier + " does not support AutoUpdate.");
                }
                else if (updateURL == null || version == null || keyE == null || keyN == null)
                {
                    Debug.LogError("Cannot update " + mod.identifier + ", one or more required fields are missing.");
                }
                else if (updateURL.FieldType != typeof(string) || version.FieldType != typeof(int) || keyE.FieldType != typeof(string) || keyN.FieldType != typeof(string))
                {
                    Debug.LogError("Cannot update " + mod.identifier + ", one or more fields have the incorrect type.");
                }
                else
                {
                    RSAParameters value = default(RSAParameters);
                    value.Exponent = Convert.FromBase64String((string)keyE.GetValue(mod.modObj));
                    value.Modulus = Convert.FromBase64String((string)keyN.GetValue(mod.modObj));
                    this.modKeys[mod.identifier] = value;
                    this.scripts.Add(new GameObject("AutoUpdateMod_" + mod.identifier).AddComponent<AutoUpdateScript>().Initialize(this, mod, (string)updateURL.GetValue(mod.modObj), (int)version.GetValue(mod.modObj)));
                }

                try
                {
                    byte[] data = File.ReadAllBytes(mod.modObj.GetType().Assembly.Location);
                    using (SHA512 shaM = new SHA512Managed())
                    {
                        string hash = Convert.ToBase64String(shaM.ComputeHash(File.ReadAllBytes(mod.modObj.GetType().Assembly.Location)));
                        Debug.Log("Got hash: " + hash);
                        hashes[hash] = mod;
                    }
                }
                catch
                {
                }
            }
            new GameObject("AutoUpdateHashChecker").AddComponent<AutoUpdateHashDownloader>().Initialize(this);
        }

        public bool PartialityExists()
        {
            return true; // this is a partiality mod so partiality probably exists
        }

        public void AddMods(List<Mod> mods)
        {
            foreach (PartialityMod partialityMod in PartialityManager.Instance.modManager.loadedMods)
            {
                mods.Add(new Mod(partialityMod, "partiality:" + partialityMod.ModID));
            }
        }

        public bool BepinexExists()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "BepInEx")
                {
                    return true; // close enough
                }
            }
            return false;
        }

        public void AddBepinexPlugins(List<Mod> mods)
        {
            foreach (BaseUnityPlugin bepinPlugin in UnityEngine.Object.FindObjectsOfType<BaseUnityPlugin>())
            {
                mods.Add(new Mod(bepinPlugin, "bepinex:" + bepinPlugin.Info.Metadata.GUID));
            }
        }

        public void ProcessResult(Mod amod, string text, int version)
        {
            Debug.Log("loading json " + text + " for mod " + amod.identifier);
            Dictionary<string, object> dictionary = text.dictionaryFromJson();
            Debug.Log("loaded json " + text + " for mod " + amod.identifier);
            Debug.Log(string.Concat(new object[]
            {
                "version is ",
                dictionary["version"],
                " of type ",
                dictionary["version"].GetType()
            }));
            this.modSigs[amod.identifier] = Convert.FromBase64String((string)dictionary["sig"]);
            this.modURLs[amod.identifier] = (string)dictionary["url"];
            if ((int)((long)dictionary["version"]) > version)
            {
                Debug.Log("Update required for " + amod.identifier);
                this.needUpdate.Add(amod);
            }
            else
            {
                Debug.Log("No update required for " + amod.identifier);
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
                    foreach (Mod partialityMod in this.needUpdate)
                    {
                        new GameObject("Download_" + partialityMod.identifier).AddComponent<DownloadScript>().Initialize(this, partialityMod, Custom.RootFolderDirectory() + "UpdatedMods", this.modURLs[partialityMod.identifier], Path.GetFileName(partialityMod.modObj.GetType().Assembly.Location));
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
                string hash = (string)(asDict["hash"]);
                int key = (int)(long)(asDict["key"]);
                int mod = (int)(long)(asDict["mod"]);
                string sig = (string)(asDict["sig"]);
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
            rsacryptoServiceProvider.ImportParameters(this.modKeys["partiality:" + this.ModID]);
            if (!rsacryptoServiceProvider.VerifyData(Encoding.ASCII.GetBytes(signedData), "SHA512", sigData))
            {
                Debug.LogError("INVALID HASH SIGNATURE! " + hash);
                return;
            }
            Mod partialityMod = hashes[hash];

            RSAParameters rsaParams = default(RSAParameters);
            rsaParams.Exponent = Convert.FromBase64String(keyE);
            rsaParams.Modulus = Convert.FromBase64String(keyN);
            this.modKeys[partialityMod.identifier] = rsaParams;

            this.scripts.Add(new GameObject("AutoUpdateMod_" + partialityMod.identifier).AddComponent<AutoUpdateScript>().Initialize(this, partialityMod, "http://beestuff.pythonanywhere.com/audb/api/mods/" + key + "/" + mod, -1));
            // new GameObject("Download_" + partialityMod.identifier).AddComponent<DownloadScript>().Initialize(this, mod, Custom.RootFolderDirectory() + "UpdatedMods", "http://beestuff.pythonanywhere.com/audb/api/mods/", Path.GetFileName(partialityMod.GetType().Assembly.Location));
        }

        public bool VerifySignature(string modid, byte[] data)
        {
            RSACryptoServiceProvider rsaCsp = new RSACryptoServiceProvider();
            rsaCsp.ImportParameters(this.modKeys[modid]);
            Debug.Log(string.Concat(new object[]
            {
                "Verifying signature ",
                this.modSigs[modid],
                " for mod ",
                modid
            }));
            return rsaCsp.VerifyData(data, "SHA512", this.modSigs[modid]);
        }

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

        public void Done()
        {
            Debug.Log("Calling Done()");
            if (this.actuallyUpdated)
            {
                Environment.SetEnvironmentVariable("DOORSTOP_DISABLE", null);
                System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                /*processStartInfo.Arguments*/ string procArgs = string.Concat(new string[]
                {
                    "/c (xcopy /Y /R UpdatedMods \"",
                    DirectoryToLoadModsFrom(),
                    "\" && rd /S /Q UpdatedMods && start ",
                    GetLaunchCommand(),
                    ") || (echo \"Something went wrong\" && pause)"
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

        public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/0/0";

        public int version = 19;

        public string keyE = "AQAB";

        public string keyN = "yu7XMmICrzuavyZRGWoknFIbJX4N4zh3mFPOyfzmQkil2axVIyWx5ogCdQ3OTdSZ0xpQ3yiZ7zqbguLu+UWZMfLOBKQZOs52A9OyzeYm7iMALmcLWo6OdndcMc1Uc4ZdVtK1CRoPeUVUhdBfk2xwjx+CvZUlQZ26N1MZVV0nq54IOEJzC9qQnVNgeeHxO1lRUTdg5ZyYb7I2BhHfpDWyTvUp6d5m6+HPKoalC4OZSfmIjRAi5UVDXNRWn05zeT+3BJ2GbKttwvoEa6zrkVuFfOOe9eOAWO3thXmq9vJLeF36xCYbUJMkGR2M5kDySfvoC7pzbzyZ204rXYpxxXyWPP5CaaZFP93iprZXlSO3XfIWwws+R1QHB6bv5chKxTZmy/Imo4M3kNLo5B2NR/ZPWbJqjew3ytj0A+2j/RVwV9CIwPlN4P50uwFm+Mr0OF2GZ6vU0s/WM7rE78+8Wwbgcw6rTReKhVezkCCtOdPkBIOYv3qmLK2S71NPN2ulhMHD9oj4t0uidgz8pNGtmygHAm45m2zeJOhs5Q/YDsTv5P7xD19yfVcn5uHpSzRIJwH5/DU1+aiSAIRMpwhF4XTUw73+pBujdghZdbdqe2CL1juw7XCa+XfJNtsUYrg+jPaCEUsbMuNxdFbvS0Jleiu3C8KPNKDQaZ7QQMnEJXeusdU=";

        public List<AutoUpdateScript> scripts = new List<AutoUpdateScript>();

        public List<Mod> needUpdate = new List<Mod>();

        public List<Mod> needRename = new List<Mod>();

        public Dictionary<Mod, string> newNames = new Dictionary<Mod, string>();

        public bool actuallyUpdated = false;

        public Dictionary<string, RSAParameters> modKeys = new Dictionary<string, RSAParameters>();

        public Dictionary<string, byte[]> modSigs = new Dictionary<string, byte[]>();

        public Dictionary<string, string> modURLs = new Dictionary<string, string>();

        public Dictionary<string, Mod> hashes = new Dictionary<string, Mod>();
    }
}
