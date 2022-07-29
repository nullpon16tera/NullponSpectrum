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
        private int size = 31;

        private List<GameObject> leftSpheres = new List<GameObject>(31);
        private List<GameObject> rightSpheres = new List<GameObject>(31);
        private List<Material> sphereMaterials = new List<Material>(31);
        private List<Vector3> leftSphereVector = new List<Vector3>(31);
        private List<Vector3> rightSphereVector = new List<Vector3>(31);

        private GameObject sphereRoot = new GameObject("sphereVisualizerRoot");


        private void OnUpdatedRawSpectrums(AudioSpectrum31 obj)
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

        private void UpdateAudioSpectrums(AudioSpectrum31 audio)
        {
            if (!audio)
            {
                return;
            }

            for (int i = 0; i < size; i++)
            {
                var alpha = (this._audioSpectrum.PeakLevels[i] * 10f) % 1f;
                sphereMaterials[i].SetColor("_Color", Color.HSVToRGB(alpha, 1f, Lighting(alpha, 1f)).ColorWithAlpha(Lighting(alpha, 0.8f)));

                var positionSize = this._audioSpectrum.PeakLevels[i] * 5f;

                Transform leftSphere = leftSpheres[i].transform;
                var leftPos = leftSphereVector[i];
                leftSphere.localPosition = new Vector3((leftPos.x + positionSize), (leftPos.y + positionSize), (leftPos.z + positionSize));
                leftSphere.localScale = new Vector3(0.05f, this._audioSpectrum.PeakLevels[i] * (5f + leftPos.z), this._audioSpectrum.PeakLevels[i] * (5f + leftPos.z));

                Transform rightSphere = rightSpheres[i].transform;
                var rightPos = rightSphereVector[i];
                rightSphere.localPosition = new Vector3((rightPos.x - positionSize), (rightPos.y + positionSize), (rightPos.z + positionSize));
                rightSphere.localScale = new Vector3(0.05f, this._audioSpectrum.PeakLevels[i] * (5f + rightPos.z), this._audioSpectrum.PeakLevels[i] * (5f + rightPos.z));
            }

        }

        private float Lighting(float alpha, float withAlpha)
        {
            return 0.15f < alpha ? withAlpha : 0f;
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


            this._audioSpectrum.Band = AudioSpectrum31.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW

            UnityEngine.Random.InitState(RandomSeed());

            for (int i = 0; i < size; i++)
            {
                Material sphereMaterial = new Material(Shader.Find("Custom/Glowing"));
                sphereMaterial.SetColor("_Color", new Color(1f, 1f, 1f).ColorWithAlpha(1f));
                sphereMaterial.SetFloat("_EnableColorInstancing", 1f);
                sphereMaterial.SetFloat("_WhiteBoostType", 1f);
                sphereMaterial.SetFloat("_NoiseDithering", 1f);
                sphereMaterials.Add(sphereMaterial);


                // Left Sphere GameObject create
                GameObject leftSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                MeshRenderer leftMeshRenderer = leftSphere.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = sphereMaterials[i];

                float randX = UnityEngine.Random.Range(5f, 15f);
                float randY = UnityEngine.Random.Range(-1f, 10f);
                float randZ = UnityEngine.Random.Range(8f, 35f);


                Transform leftSphereTransform = leftSphere.transform;
                leftSphereTransform.localPosition = new Vector3(-(randX), randY, randZ);
                leftSphereTransform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                leftSphereTransform.localRotation = Quaternion.Euler(-45f, 45f, 25f);
                leftSphereVector.Add(leftSphereTransform.localPosition);

                leftSphere.transform.SetParent(sphereRoot.transform);

                leftSpheres.Add(leftSphere);

                // Right Sphere GameObject create
                GameObject rightSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                MeshRenderer rightMeshRenderer = rightSphere.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = sphereMaterials[i];

                Transform rightSphereTransform = rightSphere.transform;
                rightSphereTransform.localPosition = new Vector3(randX, randY, randZ);
                rightSphereTransform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                rightSphereTransform.localRotation = Quaternion.Euler(-45f, -45f, -25f);
                rightSphereVector.Add(rightSphereTransform.localPosition);

                rightSphere.transform.SetParent(sphereRoot.transform);

                rightSpheres.Add(rightSphere);
            }

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = sphereRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                sphereRoot.transform.localPosition = rootPosition;
            }

            this.sphereRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
        }

        public int RandomSeed()
        {
            System.Random rand = new System.Random();
            int next = rand.Next(0, 101);
            int seed = next * (int)this.Currentmap.level.beatsPerMinute;
            return seed;
        }

        private bool _disposedValue;
        public IDifficultyBeatmap Currentmap { get; private set; }
        private AudioSpectrum31 _audioSpectrum;

        [Inject]
        public void Constructor(IDifficultyBeatmap level, AudioSpectrum31 audioSpectrum)
        {
            this.Currentmap = level;
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
