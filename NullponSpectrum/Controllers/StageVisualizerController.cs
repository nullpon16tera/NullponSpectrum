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


        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
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

        private void UpdateAudioSpectrums(AudioSpectrum audio)
        {
            if (!audio)
            {
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                var levels = this._audioSpectrum.PeakLevels[6 - i];
                var alpha = levels * 10f % 1f;
                float ttime = Time.time + (((float)i + 1) / 5) * Mathf.PI;
                float amp = Mathf.Cos(ttime) * 0.5f + 0.5f;
                speakerMaterials[i].SetColor("_Color", Color.HSVToRGB(0.6f, 1f, alpha).ColorWithAlpha(alpha));

                Transform leftSpeaker = speakerObjects[i].transform;
                var speakerSize = leftSpeakerSize[i].x + levels;

                var speakerPosition = 0f + levels * (i * 20f);
                Plugin.Log.Debug($"StageVisualizer: " + speakerPosition.ToString("F3"));
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


            this._audioSpectrum.Band = AudioSpectrum.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW


            for (int i = 0; i < 5; i++)
            {
                Material speakerMaterial = new Material(Shader.Find("Custom/Glowing"));
                speakerMaterial.SetColor("_Color", new Color((i + 1f % 1f), 1f, 1f).ColorWithAlpha(1f));
                speakerMaterial.SetFloat("_EnableColorInstancing", 1f);
                speakerMaterial.SetFloat("_WhiteBoostType", 1f);
                speakerMaterial.SetFloat("_NoiseDithering", 1f);
                speakerMaterials.Add(speakerMaterial);
            }

            GameObject speakerObjectRoot = new GameObject("speakerObjectRoot");
            speakerObjectRoot.transform.localScale = new Vector3(2f, 0.05f, 2f);
            speakerObjectRoot.transform.localPosition = new Vector3(0f, 0f, 0f);

            GameObject speakerObject1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            MeshRenderer meshRenderer1 = speakerObject1.GetComponent<MeshRenderer>();
            meshRenderer1.material = speakerMaterials[0];
            speakerObject1.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
            speakerObject1.transform.localPosition = new Vector3(0f, 0f, 0f);
            leftSpeakerPosition.Add(speakerObject1.transform.localPosition);

            GameObject speakerObject2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            MeshRenderer meshRenderer2 = speakerObject2.GetComponent<MeshRenderer>();
            meshRenderer2.material = speakerMaterials[1];
            speakerObject2.transform.localScale = new Vector3(1.6f, 0.05f, 1.6f);
            speakerObject2.transform.localPosition = new Vector3(0f, 0f, 0f);
            leftSpeakerPosition.Add(speakerObject2.transform.localPosition);

            GameObject speakerObject3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            MeshRenderer meshRenderer3 = speakerObject3.GetComponent<MeshRenderer>();
            meshRenderer3.material = speakerMaterials[2];
            speakerObject3.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);
            speakerObject3.transform.localPosition = new Vector3(0f, 0f, 0f);
            leftSpeakerPosition.Add(speakerObject3.transform.localPosition);

            GameObject speakerObject4 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            MeshRenderer meshRenderer4 = speakerObject4.GetComponent<MeshRenderer>();
            meshRenderer4.material = speakerMaterials[3];
            speakerObject4.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
            speakerObject4.transform.localPosition = new Vector3(0f, 0f, 0f);
            leftSpeakerPosition.Add(speakerObject4.transform.localPosition);

            GameObject speakerObject5 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            MeshRenderer meshRenderer5 = speakerObject5.GetComponent<MeshRenderer>();
            meshRenderer5.material = speakerMaterials[4];
            speakerObject5.transform.localScale = new Vector3(1f, 0.05f, 1f);
            speakerObject5.transform.localPosition = new Vector3(0f, 0f, 0f);
            leftSpeakerPosition.Add(speakerObject5.transform.localPosition);


            speakerObjects.Add(speakerObject1);
            speakerObjects.Add(speakerObject2);
            speakerObjects.Add(speakerObject3);
            speakerObjects.Add(speakerObject4);
            speakerObjects.Add(speakerObject5);
            speakerObject5.transform.SetParent(speakerObject4.transform);
            speakerObject4.transform.SetParent(speakerObject3.transform);
            speakerObject3.transform.SetParent(speakerObject2.transform);
            speakerObject2.transform.SetParent(speakerObject1.transform);
            speakerObject1.transform.SetParent(speakerObjectRoot.transform);
            speakerObjectRoot.transform.SetParent(leftSpeakerObject.transform, true);

            for (int i = 0; i < 5; i++)
            {
                leftSpeakerSize.Add(speakerObjects[i].transform.localScale);
            }

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
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor(IDifficultyBeatmap level, AudioSpectrum audioSpectrum)
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
