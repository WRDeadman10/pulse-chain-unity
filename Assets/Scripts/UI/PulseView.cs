using System.Collections.Generic;

using MPUIKIT;

using UnityEngine;
using UnityEngine.UI;

namespace PulseChain.Gameplay {
    public sealed class PulseView : MonoBehaviour {
        private readonly Queue<Vector2> _trailPositions = new Queue<Vector2>(8);
        private readonly List<MPImage> _trailImages = new List<MPImage>(6);
        private RectTransform _rectTransform;
        private MPImage _image;
        private MPImage _glowImage;

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
            Color pulseColor = overload ? new Color(1.0f, 0.45f, 0.2f, 1.0f) : Color.Lerp(new Color(0.55f, 0.95f, 1.0f, 1.0f), new Color(0.92f, 1.0f, 0.88f, 1.0f), Mathf.Clamp01(progress));
            _image.color = pulseColor;
            _glowImage.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, overload ? 0.28f : 0.20f);

            if (style == PulseVisualStyle.Square) {
                ConfigureRectangleShape(_image, pulseColor, new Vector4(8.0f, 8.0f, 8.0f, 8.0f), 1.3f);
            } else {
                ConfigureCircleShape(_image, pulseColor, 0.0f, Color.clear, 1.3f);
            }

            float scale = 1.0f + (Mathf.Sin(progress * Mathf.PI) * 0.18f);
            _rectTransform.localScale = new Vector3(scale, scale, 1.0f);
            _glowImage.rectTransform.localScale = new Vector3(1.6f + (scale * 0.25f), 1.6f + (scale * 0.25f), 1.0f);
            UpdateTrailImages(pulseColor, style);
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
            GameObject glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(transform, false);
            _glowImage = glowObject.AddComponent<MPImage>();
            ConfigureCircleShape(_glowImage, new Color(0.55f, 0.95f, 1.0f, 0.20f), 0.0f, Color.clear, 2.5f);
            _glowImage.raycastTarget = false;
            _glowImage.rectTransform.sizeDelta = new Vector2(size * 2.0f, size * 2.0f);

            _image = gameObject.GetComponent<MPImage>();
            if (_image == null) {
                _image = gameObject.AddComponent<MPImage>();
            }

            ConfigureCircleShape(_image, new Color(0.55f, 0.95f, 1.0f, 1.0f), 0.0f, Color.clear, 1.3f);
            _image.raycastTarget = false;

            for (int i = 0; i < 6; i++) {
                GameObject trailObject = new GameObject("Trail");
                trailObject.transform.SetParent(transform.parent != null ? transform.parent : transform, false);
                MPImage trailImage = trailObject.AddComponent<MPImage>();
                ConfigureCircleShape(trailImage, new Color(0.55f, 0.95f, 1.0f, 0.10f), 0.0f, Color.clear, 1.2f);
                RectTransform trailRect = trailObject.GetComponent<RectTransform>();
                trailRect.sizeDelta = new Vector2(size * 0.6f, size * 0.6f);
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

        private void UpdateTrailImages(Color color, PulseVisualStyle style) {
            Vector2[] cachedPositions = _trailPositions.ToArray();
            int trailCount = _trailImages.Count;
            for (int i = 0; i < trailCount; i++) {
                MPImage trailImage = _trailImages[i];
                int sourceIndex = cachedPositions.Length - 1 - i;
                if (sourceIndex >= 0) {
                    trailImage.gameObject.SetActive(true);
                    trailImage.rectTransform.anchoredPosition = cachedPositions[sourceIndex];
                    float alpha = Mathf.Lerp(0.0f, 0.4f, 1.0f - ((float)i / trailCount));
                    trailImage.color = new Color(color.r, color.g, color.b, alpha);
                    if (style == PulseVisualStyle.Square) {
                        ConfigureRectangleShape(trailImage, trailImage.color, new Vector4(6.0f, 6.0f, 6.0f, 6.0f), 1.0f);
                    } else {
                        ConfigureCircleShape(trailImage, trailImage.color, 0.0f, Color.clear, 1.0f);
                    }
                } else {
                    trailImage.gameObject.SetActive(false);
                }
            }
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

        private void ConfigureRectangleShape(MPImage image, Color color, Vector4 cornerRadius, float falloffDistance) {
            image.type = Image.Type.Simple;
            image.DrawShape = DrawShape.Rectangle;
            Rectangle rectangle = image.Rectangle;
            rectangle.CornerRadius = cornerRadius;
            image.Rectangle = rectangle;
            image.color = color;
            image.OutlineWidth = 0.0f;
            image.OutlineColor = Color.clear;
            image.FalloffDistance = falloffDistance;
        }
    }
}