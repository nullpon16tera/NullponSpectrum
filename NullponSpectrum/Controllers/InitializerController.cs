using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NullponSpectrum.Utilities;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    public class InitializerController : IInitializable
    {
        internal bool existsVMCAvatar = false;
        public Vector3 FloorOffset { get; private set; } = Vector3.zero;

        public void Initialize()
        {

            existsVMCAvatar = true;

            string[] array = new string[]
            {
                "Environment/PlayersPlace",
                "CustomPlatforms"
            };
            GameObject[] source = Resources.FindObjectsOfTypeAll<GameObject>();
            string[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                string floorObjectName = array2[i];
                GameObject gameObject = (from o in source
                                         where o.GetFullPath(false) == floorObjectName
                                         select o).FirstOrDefault<GameObject>();
                if (gameObject)
                {
                    FloorOffset = gameObject.transform.localPosition;
                }

            }
        }
    }
}
