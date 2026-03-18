using UnityEngine;

namespace PulseChain.Gameplay {
    public static class SpriteFactory {
        private static Sprite _circleSprite;
        private static Sprite _squareSprite;

        public static Sprite GetCircleSprite() {
            if (_circleSprite == null) {
                _circleSprite = CreateSprite(true);
            }

            return _circleSprite;
        }

        public static Sprite GetSquareSprite() {
            if (_squareSprite == null) {
                _squareSprite = CreateSprite(false);
            }

            return _squareSprite;
        }

        private static Sprite CreateSprite(bool circle) {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.ARGB32, false);
            texture.name = circle ? "PulseChainCircle" : "PulseChainSquare";
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < texture.height; y++) {
                for (int x = 0; x < texture.width; x++) {
                    Color color = Color.white;
                    if (circle) {
                        float normalizedX = ((x + 0.5f) / texture.width) * 2.0f - 1.0f;
                        float normalizedY = ((y + 0.5f) / texture.height) * 2.0f - 1.0f;
                        float distance = Mathf.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));
                        color.a = distance <= 1.0f ? 1.0f : 0.0f;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 64.0f);
        }
    }
}