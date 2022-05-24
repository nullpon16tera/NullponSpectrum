using IPA.Loader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NullponSpectrum.Utilities
{
    internal class FloorAdjustorUtil : MonoBehaviour
    {
        public static bool IsInstallVMCAvatar { get; private set; }
        public static bool IsInstallNalulunaAvatars { get; private set; }
        private bool FloorFlag = false;

        public static GameObject NullponSpectrumFloor;
        public static Transform FloorTransform;
        private static GameObject Environment;
        private static GameObject CustomPlatforms;
        private static GameObject PlayersPlace;

        private void Awake()
        {
            IsInstallVMCAvatar = PluginManager.GetPluginFromId("VMCAvatar") != null;
            IsInstallNalulunaAvatars = PluginManager.GetPluginFromId("NalulunaAvatars") != null;
            FloorFlag = false;
            NullponSpectrumFloor = new GameObject("NullponSpectrumFloor");
            Environment = GameObject.Find("Environment");
            CustomPlatforms = GameObject.Find("CustomPlatforms");
            PlayersPlace = GameObject.Find("Environment/PlayersPlace");
        }

        private void Start()
        {
            Plugin.Log.Debug($"AdjustFloor Before localPosition " + NullponSpectrumFloor.transform.localPosition.ToString("F3"));
            StartCoroutine(FloorAdjust());
            Plugin.Log.Debug($"AdjustFloor After localPosition " + NullponSpectrumFloor.transform.localPosition.ToString("F3"));
        }

        // VMCAvatar用にキャリブレーションし直したときのを用意したけど、要らないかもしれないからコメントアウトしとく
        /*private void FixedUpdate()
        {
            if (FloorFlag)
            {
                return;
            }
            StartCoroutine(FloorAdjust());
        }*/

        private IEnumerator FloorAdjust()
        {
            yield return new WaitForSeconds(0.5f);
            if (Environment != null && PlayersPlace != null)
            {
                if (Environment.transform.localPosition.y != NullponSpectrumFloor.transform.localPosition.y)
                {
                    NullponSpectrumFloor.transform.localPosition = Environment.transform.localPosition;
                    FloorFlag = true;
                }
                else if (PlayersPlace.transform.localPosition.y != NullponSpectrumFloor.transform.localPosition.y)
                {
                    NullponSpectrumFloor.transform.localPosition = PlayersPlace.transform.localPosition;
                    FloorFlag = true;
                }
            }

            if (CustomPlatforms != null)
            {
                if (CustomPlatforms.transform.localPosition.y != NullponSpectrumFloor.transform.localPosition.y)
                {
                    NullponSpectrumFloor.transform.localPosition = CustomPlatforms.transform.localPosition;
                    FloorFlag = true;
                }
            }
            FloorTransform = NullponSpectrumFloor.transform;
        }
    }
}
