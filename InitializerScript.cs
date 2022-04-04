using System;
using Partiality;
using UnityEngine;

namespace PastebinMachine.AutoUpdate
{
    public class InitializerScript : MonoBehaviour
    {
        public void Initialize(AutoUpdateMod au)
        {
            this.au = au;
        }

        public void Update()
        {
            if (PartialityManager.Instance != null && !this.done)
            {
                this.done = true;
                this.au.Initialize();
            }
        }

        public AutoUpdateMod au;

        public bool done;
    }
}
