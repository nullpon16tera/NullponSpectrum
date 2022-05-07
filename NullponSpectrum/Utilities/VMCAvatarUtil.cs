using IPA.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NullponSpectrum.Utilities
{
    public class VMCAvatarUtil : MonoBehaviour
    {
        public static bool IsInstallVMCAvatar { get; private set; }

        public static GameObject NullponSpectrumFloor = new GameObject("NullponSpectrumFloor");

        private void Awake()
        {
            IsInstallVMCAvatar = PluginManager.GetPluginFromId("VMCAvatar") != null;
        }

        private void Start()
        {
            if (!IsInstallVMCAvatar)
            {
                return;
            }

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
                    Plugin.Log.Debug("AdjustFloorHeight: " + floorObjectName + " found.");
                    Vector3 a;
                    if (this._initialPositions.TryGetValue(gameObject, out a))
                    {
                        Plugin.Log.Debug("AdjustFloorHeight: Found initial position " + a.ToString("F3"));
                        gameObject.transform.localPosition = a;
                        NullponSpectrumFloor.transform.localPosition = a;
                    }
                    else
                    {
                        Plugin.Log.Debug("AdjustFloorHeight: Register initial position " + gameObject.transform.localPosition.ToString("F3"));
                        this._initialPositions[gameObject] = gameObject.transform.localPosition;
                        NullponSpectrumFloor.transform.localPosition = gameObject.transform.localPosition;
                    }
                }

            }
        }

        private Dictionary<GameObject, Vector3> _initialPositions = new Dictionary<GameObject, Vector3>();
    }
}
