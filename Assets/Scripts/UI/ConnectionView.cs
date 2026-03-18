using UnityEngine;
using UnityEngine.UI;

namespace PulseChain.Gameplay {
    public sealed class ConnectionView : MonoBehaviour {
        private RectTransform _rectTransform;
        private Image _image;

        public void Draw(Vector2 from, Vector2 to, Color color) {
            EnsureVisuals();
            Vector2 direction = to - from;
            float length = direction.magnitude;
            _rectTransform.sizeDelta = new Vector2(length, 8.0f);
            _rectTransform.anchoredPosition = from + (direction * 0.5f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);
            _image.color = color;
        }

        private void EnsureVisuals() {
            if (_rectTransform != null) {
                return;
            }

            _rectTransform = gameObject.GetComponent<RectTransform>();
            if (_rectTransform == null) {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }

            _image = gameObject.GetComponent<Image>();
            if (_image == null) {
                _image = gameObject.AddComponent<Image>();
            }

            _image.sprite = SpriteFactory.GetSquareSprite();
            _image.raycastTarget = false;
        }
    }
}