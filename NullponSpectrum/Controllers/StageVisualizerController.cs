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
    public class StageVisualizerController : IInitializable, IDisposable
    {
        private int size = 31;

        private List<GameObject> speakerObjects = new List<GameObject>(5);
        private GameObject leftSpeakerObject = new GameObject("leftSpeakerObject");
        private List<Material> speakerMaterials = new List<Material>(5);
        private List<Vector3> leftSpeakerSize = new List<Vector3>(5);
        private List<Vector3> leftSpeakerPosition = new List<Vector3>(5);

        private GameObject stageRoot = new GameObject("stageVisualizerRoot");


        private void OnUpdatedRawSpectrums(AudioSpectrum31 obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.StageVisualizer)
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
                var levels = this._audioSpectrum.PeakLevels[i];
                var alpha = levels * 10f % 1f;
                float ttime = Time.time + (((float)i + 1) / 5) * Mathf.PI;
                float amp = Mathf.Cos(ttime) * 0.5f + 0.5f;
                speakerMaterials[i].SetColor("_Color", Color.HSVToRGB(alpha, 1f, 0.5f < alpha ? 1f : 0f).ColorWithAlpha(0.5f < alpha ? 0.6f : 0f));

                Transform leftSpeaker = speakerObjects[i].transform;
                var speakerSize = leftSpeakerSize[i].x + levels;
                var speakerPosition = 0f + levels * i;
                leftSpeaker.localPosition = new Vector3(0f, speakerPosition, 0f);
                //leftSpeaker.localScale = new Vector3(speakerSize, 0.05f, speakerSize);
            }

        }



        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.StageVisualizer)
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

            GameObject speakerObjectRoot = new GameObject("speakerObjectRoot");
            speakerObjectRoot.transform.localScale = new Vector3(2f, 0.05f, 2f);
            speakerObjectRoot.transform.localPosition = new Vector3(0f, 0f, -1.5f);

            for (int i = 0; i < size; i++)
            {
                Material speakerMaterial = new Material(Shader.Find("Custom/Glowing"));
                speakerMaterial.SetColor("_Color", new Color((i + 1f % 1f), 1f, 1f).ColorWithAlpha(1f));
                speakerMaterial.SetFloat("_EnableColorInstancing", 1f);
                speakerMaterial.SetFloat("_WhiteBoostType", 1f);
                speakerMaterial.SetFloat("_NoiseDithering", 1f);
                speakerMaterials.Add(speakerMaterial);

                GameObject speakerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer speakerMeshRenderer = speakerObject.GetComponent<MeshRenderer>();
                speakerMeshRenderer.material = speakerMaterials[i];
                speakerObject.transform.localScale = new Vector3(0.5f - (i * 0.01f), 0.05f, 0.5f - (i * 0.01f));
                speakerObject.transform.localPosition = new Vector3(0f, 0f + (i * 0.01f), 0f);
                leftSpeakerPosition.Add(speakerObject.transform.localPosition);
                leftSpeakerSize.Add(speakerObject.transform.localScale);
                speakerObjects.Add(speakerObject);
                if (i == 0)
                {
                    speakerObject.transform.SetParent(speakerObjectRoot.transform);
                }
                else
                {
                    speakerObject.transform.SetParent(speakerObjects[i == 0 ? 0 : i - 1].transform);
                }
            }

            
            speakerObjectRoot.transform.SetParent(leftSpeakerObject.transform);


            leftSpeakerObject.transform.SetParent(this.stageRoot.transform);

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = stageRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                stageRoot.transform.localPosition = rootPosition;
            }

            this.stageRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
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
