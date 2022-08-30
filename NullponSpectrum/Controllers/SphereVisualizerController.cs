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

        private Material _sphereMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> leftSpheres = new List<GameObject>(31);
        private List<GameObject> rightSpheres = new List<GameObject>(31);
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
                var positionSize = this._audioSpectrum.PeakLevels[i] * 5f;

                var leftSphere = leftSpheres[i].transform;
                var leftPos = leftSphereVector[i];
                leftSphere.localPosition = new Vector3((leftPos.x + positionSize), (leftPos.y + positionSize), (leftPos.z + positionSize));
                leftSphere.localScale = new Vector3(0.05f, this._audioSpectrum.PeakLevels[i] * (5f + leftPos.z), this._audioSpectrum.PeakLevels[i] * (5f + leftPos.z));

                var rightSphere = rightSpheres[i].transform;
                var rightPos = rightSphereVector[i];
                rightSphere.localPosition = new Vector3((rightPos.x - positionSize), (rightPos.y + positionSize), (rightPos.z + positionSize));
                rightSphere.localScale = new Vector3(0.05f, this._audioSpectrum.PeakLevels[i] * (5f + rightPos.z), this._audioSpectrum.PeakLevels[i] * (5f + rightPos.z));

                ChangeMaterialProperty(leftSpheres[i], positionSize, alpha);
                ChangeMaterialProperty(rightSpheres[i], positionSize, alpha);
            }

        }

        private void ChangeMaterialProperty(GameObject obj, float size, float alpha)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (0.05f < size)
            {
                var color = Color.HSVToRGB(alpha, 1f, 1f).ColorWithAlpha(0.6f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 1f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
                var color = Color.HSVToRGB(alpha, 1f, 0f).ColorWithAlpha(0f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 0f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
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


            this._audioSpectrum.Band = AudioSpectrum31.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW

            _sphereMaterial = new Material(Shader.Find("Custom/SaberBlade"));
            _sphereMaterial.SetColor("_TintColor", Color.black.ColorWithAlpha(1f));
            _sphereMaterial.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            UnityEngine.Random.InitState(RandomSeed());

            for (int i = 0; i < size; i++)
            {
                // Left Sphere GameObject create
                GameObject leftSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                MeshRenderer leftMeshRenderer = leftSphere.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _sphereMaterial;

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
                rightMeshRenderer.material = _sphereMaterial;

                Transform rightSphereTransform = rightSphere.transform;
                rightSphereTransform.localPosition = new Vector3(randX, randY, randZ);
                rightSphereTransform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                rightSphereTransform.localRotation = Quaternion.Euler(-45f, -45f, -25f);
                rightSphereVector.Add(rightSphereTransform.localPosition);

                rightSphere.transform.SetParent(sphereRoot.transform);

                rightSpheres.Add(rightSphere);
            }
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
