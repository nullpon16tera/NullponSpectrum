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
    public class CubeVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private int size = 4;

        private List<GameObject> cubes = new List<GameObject>(4);
        private Material[] cubeMaterials = new Material[2];
        private Material cubeMaterial;
        private GameObject cubeRoot = new GameObject("cubeVisualizerRoot");

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.CubeVisualizer)
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

            var bandType = this._audioSpectrum.Band;

            int j = 0;

            if (bandType == AudioSpectrum.BandType.TwentySixBand)
            {
                j = 6;
            }
            if (bandType == AudioSpectrum.BandType.ThirtyOneBand)
            {
                j = 8;
            }

            var alpha = (this._audioSpectrum.PeakLevels[j] * 4f) % 1f;
            var alphaLerp = Mathf.Lerp(0f, 1f, alpha * 30f);
            var colorLerp = Mathf.Lerp(0.45f, 1f, alpha);
            var peak = this._audioSpectrum.PeakLevels[j] * scale;
            var cubeSize = 0.2f + peak * 1.3f;
            var bpm = _timeSource.songTime * (60f / this.Currentmap.level.beatsPerMinute);
            
            for (int i = 0; i < cubes.Count; i++)
            {
                var cube = cubes[i];
                var rotate = bpm * 360f + 45f;
                var rotateRog = (i == 0 || i == 2 ? rotate : -(rotate));
                var cubePosition = cube.transform.localPosition;
                cubePosition.y = cubeSize;
                cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
                cube.transform.localRotation = Quaternion.Euler(rotateRog, rotateRog, rotateRog);
                cube.transform.localPosition = cubePosition;

                var color = Color.HSVToRGB(colorLerp, 1f, alphaLerp);
                //meshRenderers[i].material.SetColor("_Color", Color.HSVToRGB(amp, 1f, peakAmp));
                /*meshRenderers[i].material.SetColor("_Color", Color.HSVToRGB(0f, 1f, 0f));
                meshRenderers[i].material.SetColor("_AddColor", Color.HSVToRGB(amp, 1f, 1f));
                meshRenderers[i].material.SetFloat("_TintColorAlpha", alpha);*/
                //cubeMaterial.SetColor("_Color", color.ColorWithAlpha(0.01f + alpha));
                //cubeMaterial.SetFloat("_EnableColorInstancing ", 1f);
                cubeMaterials[0].SetColor("_Color", color.ColorWithAlpha(0.01f + alpha));
                cubeMaterials[0].SetFloat("_SpectrogramScale", cubeSize);
                //cubeMaterials[1].SetColor("_TintColor", color);
                cubeMaterials[1].SetFloat("_Metallic", (0.01f + alpha));
                cubeMaterials[1].SetFloat("_Smoothness", (0.01f + alpha));
                cubeMaterials[1].SetFloat("_ReflectionIntensity", (0.01f + alpha));
                cubeMaterials[1].SetFloat("_BumpIntensity", 1f);
                //cubeMaterials[1].SetColor("_Color", Color.black.ColorWithAlpha(0.01f + alpha));
            }

        }



        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.CubeVisualizer)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum.BandType.FourBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW
            // Custom/Glowing
            // Custom/UnlitSpectrogram
            /*cubeMaterial = new Material(Shader.Find("Custom/Glowing"));
            cubeMaterial.SetColor("_Color", new Color(1f, 1f, 1f).ColorWithAlpha(1f));
            cubeMaterial.SetFloat("_EnableColorInstancing", 1f);
            cubeMaterial.SetFloat("_WhiteBoostType", 0f);
            cubeMaterial.SetFloat("_NoiseDithering", 0f);*/

            cubeMaterials[0] = new Material(Shader.Find("Custom/UnlitSpectrogram"));
            cubeMaterials[0].SetColor("_Color", Color.white.ColorWithAlpha(1f));
            cubeMaterials[1] = new Material(Shader.Find("Custom/Mirror"));
            cubeMaterials[1].SetColor("_TintColor", Color.black.ColorWithAlpha(0f));
            cubeMaterials[1].SetFloat("_Metallic", 1f);
            cubeMaterials[1].SetFloat("_Smoothness", 0f);
            cubeMaterials[1].SetFloat("_EnableSpecular", 1f);
            cubeMaterials[1].SetFloat("_SpecularIntensity", 0f);
            cubeMaterials[1].SetFloat("_ReflectionIntensity", 1f);
            cubeMaterials[1].SetFloat("_EnableLightmap", 1f);
            cubeMaterials[1].SetFloat("_EnableDiffuse", 1f);
            //cubeMaterials[2] = new Material(Shader.Find("Custom/CustomParticles"));
            //cubeMaterials[1].SetColor("_Color", Color.white.ColorWithAlpha(0.1f));
            //childMeshRenderer.material.SetColor("_Color", Color.HSVToRGB(1f, 1f, 1f));
            //childMeshRenderer.material.SetColor("_AddColor", Color.HSVToRGB(amp, 1f, 1f));
            //childMeshRenderer.material.SetFloat("_TintColorAlpha", 0f);

            for (int i = 0; i < size; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer childMeshRenderer = child.GetComponent<MeshRenderer>();
                childMeshRenderer.materials = cubeMaterials;
                
                Transform cubeTransform = child.transform;
                cubeTransform.localPosition = new Vector3(0f, 0.3f, 0f);
                cubeTransform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                cubeTransform.localRotation = Quaternion.Euler(45f, 45f, 45f);

                switch (i)
                {
                    case 0:
                        cubeTransform.localPosition = new Vector3(-1.5f, 0.3f, 1f);
                        break;
                    case 1:
                        cubeTransform.localPosition = new Vector3(1.5f, 0.3f, 1f);
                        break;
                    case 2:
                        cubeTransform.localPosition = new Vector3(-1.5f, 0.3f, -1f);
                        break;
                    case 3:
                        cubeTransform.localPosition = new Vector3(1.5f, 0.3f, -1f);
                        break;
                    default:
                        break;
                }
                child.transform.SetParent(cubeRoot.transform);

                cubes.Add(child);
            }

            foreach (GameObject obj in cubes)
            {
                obj.SetActive(obj);
            }

            this.cubeRoot.transform.SetParent(Utilities.VMCAvatarUtil.NullponSpectrumFloor.transform);
        }

        private bool _disposedValue;
        private IAudioTimeSource _timeSource;
        public IDifficultyBeatmap Currentmap { get; private set; }
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor(IAudioTimeSource source, IDifficultyBeatmap level, AudioSpectrum audioSpectrum)
        {
            this._timeSource = source;
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
