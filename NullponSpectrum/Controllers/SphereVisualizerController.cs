using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class SphereVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private int size = 31;

        private List<GameObject> leftSpheres = new List<GameObject>(31);
        private List<GameObject> rightSpheres = new List<GameObject>(31);
        private List<Material> leftSphereMaterials = new List<Material>(31);
        private List<Material> rightSphereMaterials = new List<Material>(31);
        private List<Vector3> leftSphereVector = new List<Vector3>(31);
        private List<Vector3> rightSphereVector = new List<Vector3>(31);

        private GameObject sphereRoot = new GameObject("sphereVisualizerRoot");

        private float leftHSV;
        private float rightHSV;

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.SphereVisualizer)
            {
                return;
            }
            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum audio)
        {
            if (!audio)
            {
                return;
            }

            for (int i = 0; i < size; i++)
            {
                var alpha = (this._audioSpectrum.PeakLevels[i] * 10f) % 1f;
                var leftColor = i % 2 == 0 ? Color.HSVToRGB(leftHSV, 1f, alpha) : Color.HSVToRGB(rightHSV, 1f, alpha);
                var rightColor = i % 2 == 0 ? Color.HSVToRGB(rightHSV, 1f, alpha) : Color.HSVToRGB(leftHSV, 1f, alpha);
                leftSphereMaterials[i].SetColor("_Color", leftColor.ColorWithAlpha(alpha));
                rightSphereMaterials[i].SetColor("_Color", rightColor.ColorWithAlpha(alpha));

                Transform leftSphere = leftSpheres[i].transform;
                var leftPos = leftSphereVector[i];
                leftSphere.localPosition = new Vector3(leftPos.x, (leftPos.y + (this._audioSpectrum.PeakLevels[i] * 2f)), leftPos.z);
                leftSphere.localScale = new Vector3(0.05f, this._audioSpectrum.PeakLevels[i] * 10f, this._audioSpectrum.PeakLevels[i] * 10f);

                Transform rightSphere = rightSpheres[i].transform;
                var rightPos = rightSphereVector[i];
                rightSphere.localPosition = new Vector3(rightPos.x, (rightPos.y + (this._audioSpectrum.PeakLevels[i] * 2f)), rightPos.z);
                rightSphere.localScale = new Vector3(0.05f, this._audioSpectrum.PeakLevels[i] * 10f, this._audioSpectrum.PeakLevels[i] * 10f);
            }

        }



        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.SphereVisualizer)
            {
                return;
            }

            // セイバーの色取得
            float leftH, leftS, leftV;
            float rightH, rightS, rightV;

            Color.RGBToHSV(this._colorScheme.saberAColor, out leftH, out leftS, out leftV);
            Color.RGBToHSV(this._colorScheme.saberBColor, out rightH, out rightS, out rightV);
            this.leftHSV = leftH;
            this.rightHSV = rightH;


            this._audioSpectrum.Band = AudioSpectrum.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW
            
            //childMeshRenderer.material.SetColor("_Color", Color.HSVToRGB(1f, 1f, 1f));
            //childMeshRenderer.material.SetColor("_AddColor", Color.HSVToRGB(amp, 1f, 1f));
            //childMeshRenderer.material.SetFloat("_TintColorAlpha", 0f);

            for (int i = 0; i < size; i++)
            {
                Material sphereMaterial = new Material(Shader.Find("Custom/Glowing"));
                sphereMaterial.SetColor("_Color", new Color(1f, 1f, 1f).ColorWithAlpha(1f));
                sphereMaterial.SetFloat("_EnableColorInstancing", 1f);
                sphereMaterial.SetFloat("_WhiteBoostType", 1f);
                sphereMaterial.SetFloat("_NoiseDithering", 1f);
                leftSphereMaterials.Add(sphereMaterial);

                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                MeshRenderer childMeshRenderer = child.GetComponent<MeshRenderer>();
                childMeshRenderer.material = leftSphereMaterials[i];

                Transform sphereTransform = child.transform;
                sphereTransform.localPosition = new Vector3(UnityEngine.Random.Range(-3.5f, -6f), UnityEngine.Random.Range(-1.5f, 5f), UnityEngine.Random.Range(3f, 25f));
                sphereTransform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                sphereTransform.localRotation = Quaternion.Euler(-45f, 45f, 15f);
                leftSphereVector.Add(sphereTransform.localPosition);

                child.transform.SetParent(sphereRoot.transform);

                leftSpheres.Add(child);
            }

            for (int i = 0; i < size; i++)
            {
                Material sphereMaterial = new Material(Shader.Find("Custom/Glowing"));
                sphereMaterial.SetColor("_Color", new Color(1f, 1f, 1f).ColorWithAlpha(1f));
                sphereMaterial.SetFloat("_EnableColorInstancing", 1f);
                sphereMaterial.SetFloat("_WhiteBoostType", 1f);
                sphereMaterial.SetFloat("_NoiseDithering", 1f);
                rightSphereMaterials.Add(sphereMaterial);

                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                MeshRenderer childMeshRenderer = child.GetComponent<MeshRenderer>();
                childMeshRenderer.material = rightSphereMaterials[i];

                Transform sphereTransform = child.transform;
                sphereTransform.localPosition = new Vector3(UnityEngine.Random.Range(3.5f, 6f), UnityEngine.Random.Range(-1.5f, 5f), UnityEngine.Random.Range(3f, 25f));
                sphereTransform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                sphereTransform.localRotation = Quaternion.Euler(-45f, -45f, -15f);
                rightSphereVector.Add(sphereTransform.localPosition);

                child.transform.SetParent(sphereRoot.transform);

                rightSpheres.Add(child);
            }

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = sphereRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                sphereRoot.transform.localPosition = rootPosition;
            }

            this.sphereRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
        }

        private bool _disposedValue;
        private ColorScheme _colorScheme;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor(ColorScheme scheme, AudioSpectrum audioSpectrum)
        {
            this._colorScheme = scheme;
            this._audioSpectrum = audioSpectrum;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this._audioSpectrum.UpdatedRawSpectrums -= this.OnUpdatedRawSpectrums;
                }
                this._disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
