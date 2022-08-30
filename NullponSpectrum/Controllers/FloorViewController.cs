using NullponSpectrum.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;

namespace NullponSpectrum.Controllers
{
    class FloorViewController : IInitializable
    {
        public static GameObject visualizerFloorRoot { get; set; }

        public enum FramePosition
        {
            Front,
            Back,
            Left,
            Right,
        };

        public void Initialize()
        {
            visualizerFloorRoot = new GameObject("visualizerFloorRoot");

            if (PluginConfig.Instance.enableFloorObject)
            {
                CreateFloorObject();
            }
            CreateFrameObject();

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = visualizerFloorRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                visualizerFloorRoot.transform.localPosition = rootPosition;
            }

            visualizerFloorRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform, false);
        }

        private void CreateFloorObject()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
            floorRenderer.material = new Material(Shader.Find("Custom/Glowing"));
            floorRenderer.material.SetColor("_Color", Color.black.ColorWithAlpha(0f));

            floor.transform.SetParent(visualizerFloorRoot.transform);
            floor.transform.localScale = new Vector3(0.3f, 0.01f, 0.2f);
            floor.transform.localPosition = new Vector3(0f, 0.005f, 0f);
        }

        private void CreateFrameObject()
        {
            GameObject parent = new GameObject("ponponVisualizerFrame");

            for (int i = 0; i < 4; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                parent.transform.SetParent(visualizerFloorRoot.transform);

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
                        cubeTransform.localPosition = new Vector3(0f, 0.005f, 1f);
                        break;
                    case (int)FramePosition.Back:
                        cubeTransform.localPosition = new Vector3(0f, 0.005f, -1f);
                        break;
                    case (int)FramePosition.Left:
                        cubeTransform.localPosition = new Vector3(-1.5f, 0.005f, 0f);
                        break;
                    case (int)FramePosition.Right:
                        cubeTransform.localPosition = new Vector3(1.5f, 0.005f, 0f);
                        break;
                    default:
                        break;
                }
                child.transform.SetParent(parent.transform);
            }
        }
    }
}
