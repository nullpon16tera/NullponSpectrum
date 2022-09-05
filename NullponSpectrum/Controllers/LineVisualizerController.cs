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
        private Vector3[] linePositions = new Vector3[30];
        private float updateTime = 0;
        private static readonly float s_updateThresholdTime = 0.025f;

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(2f, 0.8f, f);
            return f * result;
        }

        private void OnUpdatedRawSpectrums(AudioSpectrum31 obj)
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

        private void UpdateAudioSpectrums(AudioSpectrum31 audio)
        {
            if (!audio)
            {
                return;
            }

            this.updateTime += Time.deltaTime;
            var bpmSpeed = -(this.Currentmap.level.beatsPerMinute * 0.00001f);
            var needUpdate = (s_updateThresholdTime + bpmSpeed) < updateTime;

            foreach (var item in linePositions.Select((x, i) => (x, i)).Skip(1).Take(linePositions.Length - 2))
            {

                if (needUpdate)
                {
                    var alpha = Mathf.Lerp(0f, 0.8f, this._audioSpectrum.PeakLevels[item.i - 1] * 5f);
                    var line = item.x;
                    line.z = this.Nomalize(alpha);
                    //line.y = floorTransform.localPosition.y;
                    lineRenderer.SetPosition(item.i, line);
                }

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

            this._audioSpectrum.Band = AudioSpectrum31.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 0.8f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            lineVisualizer = new GameObject("lineVisualizer");
            lineVisualizer.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            lineVisualizer.transform.localPosition = new Vector3(0f, 0.0001f, -0.8f);
            lineVisualizer.transform.localScale = new Vector3(3f, 1f, 2f);

            for (int i = 0; i < 30; i++)
            {
                linePositions[i] = new Vector3(-0.495f + (i * 0.03425f), 0f, 0f);
            }

            Plugin.Log.Debug($"LineVisualizer: " + linePositions.Length);

            lineRenderer = lineVisualizer.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = linePositions.Length;
            lineRenderer.numCapVertices = 10;
            lineRenderer.numCornerVertices = 10;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.startWidth = 0.055f;
            lineRenderer.endWidth = 0.055f;
            lineRenderer.startColor = this._colorScheme.saberAColor;
            lineRenderer.endColor = this._colorScheme.saberBColor;
            lineRenderer.SetPositions(linePositions);

            
        }

        private bool _disposedValue;
        public IDifficultyBeatmap Currentmap { get; private set; }
        private ColorScheme _colorScheme;
        private AudioSpectrum31 _audioSpectrum;

        [Inject]
        public void Constructor(IDifficultyBeatmap level, ColorScheme scheme, AudioSpectrum31 audioSpectrum)
        {
            this.Currentmap = level;
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
