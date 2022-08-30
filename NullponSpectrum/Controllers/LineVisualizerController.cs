using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class LineVisualizerController : IInitializable, IDisposable
    {
        private Transform floorTransform;
        private GameObject lineVisualizer;
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

        private void OnUpdatedRawSpectrums(AudioSpectrum4 obj)
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

        private void UpdateAudioSpectrums(AudioSpectrum4 audio)
        {
            if (!audio)
            {
                return;
            }

            foreach (var item in linePositions.Select((x, i) => (x, i)).Skip(1).Take(linePositions.Length - 2))
            {
                var alpha = this._audioSpectrum.PeakLevels[item.i] * 5f;
                var line = item.x;
                line.z = alpha;
                //line.y = floorTransform.localPosition.y;
                lineRenderer.SetPosition(item.i, line);

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

            this.floorTransform = FloorAdjustorUtil.NullponSpectrumFloor.transform;

            this._audioSpectrum.Band = AudioSpectrum4.BandType.FourBand;
            this._audioSpectrum.fallSpeed = 0.3f;
            this._audioSpectrum.sensibility = 5f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            lineVisualizer = new GameObject("lineVisualizer");
            lineVisualizer.transform.SetParent(FloorViewController.visualizerFloorRoot.transform);
            lineVisualizer.transform.localPosition = new Vector3(0f, 0.016f, -0.8f);
            lineVisualizer.transform.localScale = new Vector3(3f, 1f, 2f);

            lineRenderer = lineVisualizer.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = linePositions.Length;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.startWidth = 0.025f;
            lineRenderer.endWidth = 0.025f;
            lineRenderer.startColor = this._colorScheme.saberAColor;
            lineRenderer.endColor = this._colorScheme.saberBColor;
            lineRenderer.SetPositions(linePositions);

            
        }

        private bool _disposedValue;
        private ColorScheme _colorScheme;
        private AudioSpectrum4 _audioSpectrum;

        [Inject]
        public void Constructor(ColorScheme scheme, AudioSpectrum4 audioSpectrum)
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
