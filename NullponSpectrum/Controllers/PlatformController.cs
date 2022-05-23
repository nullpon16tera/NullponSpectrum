using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    class PlatformController : IInitializable
    {
        private GameObject npPlatformRoot = new GameObject("NPPlatformRoot");
        private GameObject leftLinePlatform = new GameObject("LeftLinePlatform");
        private GameObject rightLinePlatform = new GameObject("RightLinePlatform");
        private Material playerSpaceMaterial;

        public void Initialize()
        {
            GameObject light = new GameObject("The Light");
            Light lightComp = light.AddComponent<Light>();
            lightComp.color = Color.blue;
            light.transform.position = new Vector3(0, 5, 0);
            light.transform.SetParent(this.npPlatformRoot.transform);

            playerSpaceMaterial = new Material(Shader.Find("Custom/UnlitSpectrogram"));
            playerSpaceMaterial.SetColor("_Color", Color.red.ColorWithAlpha(0.5f));

            GameObject playerSpace = GameObject.CreatePrimitive(PrimitiveType.Plane);
            playerSpace.transform.localPosition = new Vector3(0f, 0.105f, 0f);
            playerSpace.transform.localScale = new Vector3(0.3f, 0.001f, 0.2f);

            MeshRenderer playerSpaceRenderer = playerSpace.GetComponent<MeshRenderer>();
            playerSpaceRenderer.material = playerSpaceMaterial;
            playerSpace.transform.SetParent(this.npPlatformRoot.transform);

            this.npPlatformRoot.transform.SetParent(VMCAvatarUtil.NullponSpectrumFloor.transform);
        }
    }
}
