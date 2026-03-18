using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace PulseChain.Gameplay {
    public sealed class NodeView : MonoBehaviour {
        private readonly List<Image> _zoneMarkers = new List<Image>(3);
        private RectTransform _rectTransform;
        private Image _coreImage;
        private Outline _outline;

        public void Configure(PulseChainNode node) {
            EnsureVisuals();

            int zoneCount = node.Zones.Count;
            while (_zoneMarkers.Count < zoneCount) {
                GameObject markerObject = new GameObject("ZoneMarker");
                markerObject.transform.SetParent(transform, false);
                Image markerImage = markerObject.AddComponent<Image>();
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
            _coreImage.color = node.IsRiskPath ? new Color(1.0f, 0.56f, 0.38f, 1.0f) : new Color(0.35f, 0.88f, 1.0f, 1.0f);
            _outline.effectColor = node.IsBranchNode ? new Color(1.0f, 0.95f, 0.5f, 0.85f) : new Color(1.0f, 1.0f, 1.0f, 0.35f);

            for (int i = 0; i < node.Zones.Count; i++) {
                AcceptZoneState zone = node.Zones[i];
                Image marker = _zoneMarkers[i];
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
            _coreImage = gameObject.GetComponent<Image>();
            if (_coreImage == null) {
                _coreImage = gameObject.AddComponent<Image>();
            }

            _coreImage.sprite = SpriteFactory.GetCircleSprite();
            _coreImage.raycastTarget = false;
            _outline = gameObject.GetComponent<Outline>();
            if (_outline == null) {
                _outline = gameObject.AddComponent<Outline>();
            }

            _outline.effectDistance = new Vector2(3.0f, 3.0f);
        }
    }
}