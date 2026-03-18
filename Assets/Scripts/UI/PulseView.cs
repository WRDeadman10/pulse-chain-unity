using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace PulseChain.Gameplay {
    public sealed class PulseView : MonoBehaviour {
        private readonly Queue<Vector2> _trailPositions = new Queue<Vector2>(8);
        private readonly List<Image> _trailImages = new List<Image>(6);
        private RectTransform _rectTransform;
        private Image _image;

        public void Configure(float size) {
            EnsureVisuals(size);
        }

        public void SetPosition(Vector2 position) {
            EnsureVisuals(30.0f);
            _rectTransform.anchoredPosition = position;
            AddTrailPosition(position);
        }

        public void SetVisualState(PulseVisualStyle style, bool overload, float progress) {
            EnsureVisuals(30.0f);
            Color pulseColor = overload ? new Color(1.0f, 0.45f, 0.2f, 1.0f) : new Color(0.95f, 1.0f, 1.0f, 1.0f);
            _image.color = pulseColor;

            if (style == PulseVisualStyle.Square) {
                _image.sprite = SpriteFactory.GetSquareSprite();
            } else {
                _image.sprite = SpriteFactory.GetCircleSprite();
            }

            float scale = 1.0f + (Mathf.Sin(progress * Mathf.PI) * 0.18f);
            _rectTransform.localScale = new Vector3(scale, scale, 1.0f);
            UpdateTrailImages(pulseColor);
        }

        private void EnsureVisuals(float size) {
            if (_rectTransform != null) {
                return;
            }

            _rectTransform = gameObject.GetComponent<RectTransform>();
            if (_rectTransform == null) {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }

            _rectTransform.sizeDelta = new Vector2(size, size);
            _image = gameObject.GetComponent<Image>();
            if (_image == null) {
                _image = gameObject.AddComponent<Image>();
            }

            _image.sprite = SpriteFactory.GetCircleSprite();
            _image.raycastTarget = false;

            for (int i = 0; i < 6; i++) {
                GameObject trailObject = new GameObject("Trail");
                trailObject.transform.SetParent(transform.parent != null ? transform.parent : transform, false);
                Image trailImage = trailObject.AddComponent<Image>();
                RectTransform trailRect = trailObject.GetComponent<RectTransform>();
                trailRect.sizeDelta = new Vector2(size * 0.6f, size * 0.6f);
                trailImage.sprite = SpriteFactory.GetCircleSprite();
                trailImage.raycastTarget = false;
                _trailImages.Add(trailImage);
            }
        }

        private void AddTrailPosition(Vector2 position) {
            _trailPositions.Enqueue(position);
            while (_trailPositions.Count > _trailImages.Count) {
                _trailPositions.Dequeue();
            }
        }

        private void UpdateTrailImages(Color color) {
            Vector2[] cachedPositions = _trailPositions.ToArray();
            int trailCount = _trailImages.Count;
            for (int i = 0; i < trailCount; i++) {
                Image trailImage = _trailImages[i];
                int sourceIndex = cachedPositions.Length - 1 - i;
                if (sourceIndex >= 0) {
                    trailImage.gameObject.SetActive(true);
                    trailImage.rectTransform.anchoredPosition = cachedPositions[sourceIndex];
                    float alpha = Mathf.Lerp(0.0f, 0.4f, 1.0f - ((float)i / trailCount));
                    trailImage.color = new Color(color.r, color.g, color.b, alpha);
                } else {
                    trailImage.gameObject.SetActive(false);
                }
            }
        }
    }
}