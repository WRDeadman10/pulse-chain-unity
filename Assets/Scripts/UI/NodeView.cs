using System.Collections.Generic;

using MPUIKIT;

using UnityEngine;
using UnityEngine.UI;

namespace PulseChain.Gameplay {
    public sealed class NodeView : MonoBehaviour {
        private readonly List<MPImage> _zoneMarkers = new List<MPImage>(3);
        private RectTransform _rectTransform;
        private MPImage _coreImage;
        private MPImage _glowImage;

        public void Configure(PulseChainNode node) {
            EnsureVisuals();

            int zoneCount = node.Zones.Count;
            while (_zoneMarkers.Count < zoneCount) {
                GameObject markerObject = new GameObject("ZoneMarker");
                markerObject.transform.SetParent(transform, false);
                MPImage markerImage = markerObject.AddComponent<MPImage>();
                ConfigureCircleShape(markerImage, new Color(0.3f, 1.0f, 0.55f, 1.0f), 0.0f, Color.clear, 1.2f);
                RectTransform markerRect = markerObject.GetComponent<RectTransform>();
                markerRect.sizeDelta = new Vector2(18.0f, 18.0f);
                _zoneMarkers.Add(markerImage);
            }

            for (int i = 0; i < _zoneMarkers.Count; i++) {
                _zoneMarkers[i].gameObject.SetActive(i < zoneCount);
            }
        }

        public void SetPosition(Vector2 position) {
            EnsureVisuals();
            _rectTransform.anchoredPosition = position;
        }

        public void UpdateVisuals(PulseChainNode node, float time, float zoneCycleSpeed) {
            EnsureVisuals();
            float pulseScale = 1.0f + (Mathf.Sin((time * 2.1f) + node.Id) * 0.06f);
            _rectTransform.localScale = new Vector3(pulseScale, pulseScale, 1.0f);
            _coreImage.color = node.IsRiskPath ? new Color(1.0f, 0.56f, 0.38f, 1.0f) : Color.Lerp(new Color(0.28f, 0.74f, 1.0f, 1.0f), new Color(0.45f, 1.0f, 0.95f, 1.0f), (Mathf.Sin(time + node.Id) + 1.0f) * 0.5f);
            _coreImage.OutlineWidth = node.IsBranchNode ? 4.0f : 2.0f;
            _coreImage.OutlineColor = node.IsBranchNode ? new Color(1.0f, 0.95f, 0.5f, 0.85f) : new Color(1.0f, 1.0f, 1.0f, 0.30f);
            _glowImage.color = node.IsRiskPath ? new Color(1.0f, 0.35f, 0.22f, 0.20f) : new Color(0.20f, 0.90f, 1.0f, 0.18f);
            _glowImage.rectTransform.localScale = new Vector3(1.45f + (Mathf.Sin(time * 2.5f) * 0.08f), 1.45f + (Mathf.Sin(time * 2.5f) * 0.08f), 1.0f);

            for (int i = 0; i < node.Zones.Count; i++) {
                AcceptZoneState zone = node.Zones[i];
                MPImage marker = _zoneMarkers[i];
                float zoneValue = ResolveZoneValue(zone, time, zoneCycleSpeed);
                float angleRadians = zoneValue * Mathf.PI * 2.0f;
                float radius = 42.0f;
                RectTransform markerRect = marker.rectTransform;
                markerRect.anchoredPosition = new Vector2(Mathf.Cos(angleRadians) * radius, Mathf.Sin(angleRadians) * radius);
                marker.color = i == node.CorrectZoneIndex ? new Color(0.3f, 1.0f, 0.55f, 1.0f) : new Color(1.0f, 0.3f, 0.3f, 0.8f);
                float size = 12.0f + (zone.Width * 28.0f);
                markerRect.sizeDelta = new Vector2(size, size);
            }
        }

        private float ResolveZoneValue(AcceptZoneState zone, float time, float zoneCycleSpeed) {
            float baseValue = (time * zoneCycleSpeed * zone.Speed) + zone.Phase;
            if (zone.MotionType == AcceptZoneMotionType.Rotating) {
                return Mathf.Repeat(baseValue, 1.0f);
            }

            if (zone.MotionType == AcceptZoneMotionType.PingPong) {
                return Mathf.PingPong(baseValue, 1.0f);
            }

            return zone.Center;
        }

        private void EnsureVisuals() {
            if (_rectTransform != null) {
                return;
            }

            _rectTransform = gameObject.GetComponent<RectTransform>();
            if (_rectTransform == null) {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }

            _rectTransform.sizeDelta = new Vector2(64.0f, 64.0f);
            GameObject glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(transform, false);
            _glowImage = glowObject.AddComponent<MPImage>();
            ConfigureCircleShape(_glowImage, new Color(0.20f, 0.90f, 1.0f, 0.18f), 0.0f, Color.clear, 2.4f);
            _glowImage.raycastTarget = false;
            _glowImage.rectTransform.sizeDelta = new Vector2(92.0f, 92.0f);

            _coreImage = gameObject.GetComponent<MPImage>();
            if (_coreImage == null) {
                _coreImage = gameObject.AddComponent<MPImage>();
            }

            ConfigureCircleShape(_coreImage, new Color(0.28f, 0.74f, 1.0f, 1.0f), 2.0f, new Color(1.0f, 1.0f, 1.0f, 0.30f), 1.4f);
            _coreImage.raycastTarget = false;
        }

        private void ConfigureCircleShape(MPImage image, Color color, float outlineWidth, Color outlineColor, float falloffDistance) {
            image.type = Image.Type.Simple;
            image.DrawShape = DrawShape.Circle;
            Circle circle = image.Circle;
            circle.FitToRect = true;
            image.Circle = circle;
            image.color = color;
            image.OutlineWidth = outlineWidth;
            image.OutlineColor = outlineColor;
            image.FalloffDistance = falloffDistance;
        }
    }
}