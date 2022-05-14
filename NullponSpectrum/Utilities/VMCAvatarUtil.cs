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
        private static bool FloorFlag = false;

        public static GameObject NullponSpectrumFloor;
        public static Transform FloorTransform;

        private void Awake()
        {
            IsInstallVMCAvatar = PluginManager.GetPluginFromId("VMCAvatar") != null;
            FloorFlag = false;
            NullponSpectrumFloor = new GameObject("NullponSpectrumFloor");
        }

        private void Start()
        {
            if (!IsInstallVMCAvatar)
            {
                return;
            }


            Plugin.Log.Debug($"AdjustFloor Before localPosition {NullponSpectrumFloor.transform.localPosition.ToString("F3")}");
            AdjustFloorObject();
            FloorTransform = NullponSpectrumFloor.transform;
            Plugin.Log.Debug($"AdjustFloor After localPosition {NullponSpectrumFloor.transform.localPosition.ToString("F3")}");

            if (NullponSpectrumFloor.transform.localPosition.y != 0f)
            {
                FloorFlag = true;
            }
        }

        private void FixedUpdate()
        {
            if (FloorFlag)
            {
                return;
            }
            if (NullponSpectrumFloor.transform.localPosition.y == 0f)
            {
                return;
            }

            Plugin.Log.Debug("FloorAdjust Flag ok.");
            AdjustFloorObject();
            FloorFlag = true;
            FloorTransform = NullponSpectrumFloor.transform;
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
