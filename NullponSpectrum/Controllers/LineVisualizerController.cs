using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Utilities;
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
    public class LineVisualizerController : IInitializable, IDisposable
    {
        private Transform floorTransform;
        private GameObject lineVisualizer = new GameObject("lineVisualizer");
        private LineRenderer lineRenderer;
        private Vector3[] linePositions = new Vector3[]
        {
            new Vector3(-0.495f, 0f, 0f),
            new Vector3(-0.3f, 0f, 0f),
            new Vector3(-0.1f, 0f, 0f),
            new Vector3(0.1f, 0f, 0f),
            new Vector3(0.3f, 0f, 0f),
            new Vector3(0.495f, 0f, 0f)
        };

        private GameObject lineVisualizerRoot = new GameObject("lineVisualizerRoot");

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.LineVisualizer)
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


            for (int i = 0; i < linePositions.Length; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                if (linePositions.Length - 1 <= i)
                {
                    break;
                }

                int j = i - 1;
                if (bandType != AudioSpectrum.BandType.FourBand)
                {
                    j = i + 4;
                }

                var alpha = this._audioSpectrum.PeakLevels[j] * 5f;
                var line = linePositions[i];
                line.z = alpha;
                line.y = floorTransform.localPosition.y;
                lineRenderer.SetPosition(i, line);

            }

        }



        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.LineVisualizer)
            {
                return;
            }

            this.floorTransform = VMCAvatarUtil.NullponSpectrumFloor.transform;

            this._audioSpectrum.Band = AudioSpectrum.BandType.FourBand;
            this._audioSpectrum.fallSpeed = 0.3f;
            this._audioSpectrum.sensibility = 5f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            lineVisualizer.transform.localPosition = new Vector3(0f, 0.005f, -0.8f);
            lineVisualizer.transform.localScale = new Vector3(3f, 0.05f, 2f);
            lineVisualizer.transform.SetParent(this.lineVisualizerRoot.transform);

            lineRenderer = lineVisualizer.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = linePositions.Length;
            lineRenderer.startWidth = 0.025f;
            lineRenderer.endWidth = 0.025f;
            lineRenderer.startColor = this._colorScheme.saberAColor;
            lineRenderer.endColor = this._colorScheme.saberBColor;
            lineRenderer.SetPositions(linePositions);

            
            this.lineVisualizer.SetActive(this.lineVisualizer);
            
            this.lineVisualizerRoot.transform.SetParent(VMCAvatarUtil.NullponSpectrumFloor.transform);
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
