using IPA.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NullponSpectrum.Utilities
{
    internal class VMCAvatarUtil : MonoBehaviour
    {
        public static bool IsInstallVMCAvatar { get; private set; }

        public static GameObject NullponSpectrumFloor;

        private void Awake()
        {
            IsInstallVMCAvatar = PluginManager.GetPluginFromId("VMCAvatar") != null;
            NullponSpectrumFloor = new GameObject("NullponSpectrumFloor");
        }

        private void Start()
        {
            if (!IsInstallVMCAvatar)
            {
                return;
            }
            /*if (NullponSpectrumFloor.transform.localPosition.y == 0f)
            {
                return;
            }*/
            Plugin.Log.Debug($"FloorAdjust: " + NullponSpectrumFloor.transform.localPosition.ToString("F3"));

            AdjustFloorObject();
        }

        private void AdjustFloorObject()
        {
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
                    Vector3 a;
                    if (this._initialPositions.TryGetValue(gameObject, out a))
                    {
                        gameObject.transform.localPosition = a;
                        NullponSpectrumFloor.transform.localPosition = a;
                    }
                    else
                    {
                        this._initialPositions[gameObject] = gameObject.transform.localPosition;
                        NullponSpectrumFloor.transform.localPosition = gameObject.transform.localPosition;
                    }
                }

            }
        }

        private Dictionary<GameObject, Vector3> _initialPositions = new Dictionary<GameObject, Vector3>();
    }
}
