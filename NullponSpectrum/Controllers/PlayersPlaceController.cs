using NullponSpectrum.Configuration;
using NullponSpectrum.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    class PlayersPlaceController : IInitializable
    {
        public enum FramePosition
        {
            Front,
            Back,
            Left,
            Right,
        };

        private Material _frameMaterial;
        private Material _floorMaterial;
        private GameObject _npPlayerPlaceRoot = new GameObject("npPlayerPlaceRoot");
        private bool IsFloorSet = PluginConfig.Instance.MeshVisualizer || PluginConfig.Instance.StripeVisualizer;

        public void Initialize()
        {
            if (!IsFloorSet)
            {
                return;
            }

            CreateFloorObject();
            CreateFrameObject();

            this._npPlayerPlaceRoot.transform.SetParent(VMCAvatarUtil.NullponSpectrumFloor.transform);
        }
        private void CreateFloorObject()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
            _floorMaterial = new Material(Shader.Find("Custom/UnlitSpectrogram"));
            _floorMaterial.SetColor("_Color", Color.black.ColorWithAlpha(1f));
            floorRenderer.material = _floorMaterial;

            floor.transform.localScale = new Vector3(0.3f, 0.01f, 0.2f);
            floor.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            floor.SetActive(floor);
            floor.transform.SetParent(_npPlayerPlaceRoot.transform);
        }
        


        private void CreateFrameObject()
        {
            GameObject parent = new GameObject("playerPlaceFrame");
            parent.transform.localPosition = new Vector3(0f, 0.005f, 0f);

            _frameMaterial = new Material(Shader.Find("Custom/Mirror"));
            _frameMaterial.SetColor("_TintColor", Color.black.ColorWithAlpha(0f));
            _frameMaterial.SetFloat("_Metallic", 1f);
            _frameMaterial.SetFloat("_Smoothness", 0.5f);
            _frameMaterial.SetFloat("_EnableSpecular", 1f);
            _frameMaterial.SetFloat("_SpecularIntensity", 0f);
            _frameMaterial.SetFloat("_ReflectionIntensity", 0f);
            _frameMaterial.SetFloat("_EnableLightmap", 1f);
            _frameMaterial.SetFloat("_EnableDiffuse", 1f);

            for (int i = 0; i < 4; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer childMeshRenderer = child.GetComponent<MeshRenderer>();
                childMeshRenderer.material = _frameMaterial;

                Transform cubeTransform = child.transform;
                if (i == (int)FramePosition.Front || i == (int)FramePosition.Back)
                {
                    cubeTransform.localScale = new Vector3(3.015f, 0.015f, 0.015f);
                }
                if (i == (int)FramePosition.Left || i == (int)FramePosition.Right)
                {
                    cubeTransform.localScale = new Vector3(0.015f, 0.015f, 2.015f);
                }
                switch (i)
                {
                    case (int)FramePosition.Front:
                        cubeTransform.localPosition = new Vector3(0f, 0f, 1f);
                        break;
                    case (int)FramePosition.Back:
                        cubeTransform.localPosition = new Vector3(0f, 0f, -1f);
                        break;
                    case (int)FramePosition.Left:
                        cubeTransform.localPosition = new Vector3(-1.5f, 0f, 0f);
                        break;
                    case (int)FramePosition.Right:
                        cubeTransform.localPosition = new Vector3(1.5f, 0f, 0f);
                        break;
                    default:
                        break;
                }
                child.transform.SetParent(parent.transform);
            }
            parent.transform.SetParent(_npPlayerPlaceRoot.transform);
        }
    }
}
