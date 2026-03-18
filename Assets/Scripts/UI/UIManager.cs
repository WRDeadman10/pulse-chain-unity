using PulseChain.Core;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PulseChain.Gameplay {
    public sealed class UIManager : MonoBehaviour {
        private Canvas _canvas;
        private CanvasScaler _canvasScaler;
        private GraphicRaycaster _raycaster;
        private RectTransform _root;
        private RectTransform _gameplayRoot;
        private Image _background;
        private Text _scoreText;
        private Text _comboText;
        private Text _metaText;
        private Image _energyFill;
        private Image _screenFlash;
        private GameObject _gameOverPanel;
        private Text _gameOverText;
        private Button _restartButton;
        private float _screenShakeTime;
        private float _successPunchTime;
        private GameManager _gameManager;

        public RectTransform GameplayRoot {
            get {
                return _gameplayRoot;
            }
        }

        public void Initialize(GameManager gameManager) {
            _gameManager = gameManager;
            EnsureCanvas();
            EnsureEventSystem();
            BuildHud();
            BuildGameOverPanel();
        }

        public void Tick(float deltaTime, int combo, bool nearMissActive, bool overloadActive) {
            if (_gameplayRoot == null) {
                return;
            }

            if (_screenShakeTime > 0.0f) {
                _screenShakeTime -= deltaTime;
                Vector2 shakeOffset = Random.insideUnitCircle * 12.0f;
                _gameplayRoot.anchoredPosition = shakeOffset;
            } else {
                _gameplayRoot.anchoredPosition = Vector2.Lerp(_gameplayRoot.anchoredPosition, Vector2.zero, deltaTime * 10.0f);
            }

            if (_successPunchTime > 0.0f) {
                _successPunchTime -= deltaTime;
                float scale = 1.0f + Mathf.Sin(_successPunchTime * 20.0f) * 0.03f;
                _gameplayRoot.localScale = new Vector3(scale, scale, 1.0f);
            } else {
                _gameplayRoot.localScale = Vector3.Lerp(_gameplayRoot.localScale, Vector3.one, deltaTime * 8.0f);
            }

            Color targetBackground = overloadActive ? new Color(0.07f, 0.04f, 0.12f, 1.0f) : new Color(0.05f, 0.09f, 0.16f, 1.0f);
            _background.color = Color.Lerp(_background.color, targetBackground, deltaTime * 2.5f);
            float flashTarget = overloadActive ? 0.12f : 0.0f;
            if (nearMissActive) {
                flashTarget = 0.22f;
                _screenShakeTime = 0.15f;
            }

            _screenFlash.color = Color.Lerp(_screenFlash.color, new Color(1.0f, 1.0f, 1.0f, flashTarget), deltaTime * 8.0f);
            _comboText.color = combo > 0 ? new Color(1.0f, 0.9f, 0.45f, 1.0f) : new Color(1.0f, 1.0f, 1.0f, 0.5f);
        }

        public void ShowRunHud() {
            if (_gameOverPanel != null) {
                _gameOverPanel.SetActive(false);
            }
        }

        public void UpdateScore(int score, int combo, int highScore, int dailyBest) {
            _scoreText.text = "Score " + score;
            _comboText.text = combo > 0 ? "Combo x" + (combo + 1) : "Combo x1";
            _metaText.text = "High " + highScore + "   Daily " + dailyBest + "\nTap Right = Risk   Hold = Slow";
        }

        public void UpdateEnergy(float normalizedValue, bool active) {
            _energyFill.fillAmount = normalizedValue;
            _energyFill.color = active ? new Color(0.45f, 1.0f, 0.75f, 1.0f) : new Color(0.35f, 0.75f, 1.0f, 1.0f);
        }

        public void ShowTapFeedback(bool perfect, bool nearMiss) {
            if (perfect) {
                _screenFlash.color = new Color(0.35f, 1.0f, 0.55f, 0.18f);
            } else if (nearMiss) {
                _screenFlash.color = new Color(1.0f, 0.55f, 0.35f, 0.20f);
                _screenShakeTime = 0.12f;
            }
        }

        public void PlaySuccessPunch() {
            _successPunchTime = 0.25f;
        }

        public void ShowGameOver(GameOverData gameOverData) {
            _gameOverPanel.SetActive(true);
            string nearMissLine = gameOverData.NearMiss ? "\nNear miss" : string.Empty;
            _gameOverText.text = "Run Over\nScore " + gameOverData.Score + "\nHigh " + gameOverData.HighScore + "\nDaily " + gameOverData.DailyBest + nearMissLine;
        }

        private void EnsureCanvas() {
            if (_canvas != null) {
                return;
            }

            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = false;

            _canvasScaler = gameObject.GetComponent<CanvasScaler>();
            if (_canvasScaler == null) {
                _canvasScaler = gameObject.AddComponent<CanvasScaler>();
            }

            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = new Vector2(1080.0f, 1920.0f);
            _canvasScaler.matchWidthOrHeight = 0.5f;

            _raycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (_raycaster == null) {
                _raycaster = gameObject.AddComponent<GraphicRaycaster>();
            }

            _root = gameObject.GetComponent<RectTransform>();
            if (_root == null) {
                _root = gameObject.AddComponent<RectTransform>();
            }

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(transform, false);
            _background = backgroundObject.AddComponent<Image>();
            _background.color = new Color(0.05f, 0.09f, 0.16f, 1.0f);
            StretchRect(backgroundObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject gameplayObject = new GameObject("Gameplay");
            gameplayObject.transform.SetParent(transform, false);
            _gameplayRoot = gameplayObject.AddComponent<RectTransform>();
            StretchRect(_gameplayRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject flashObject = new GameObject("ScreenFlash");
            flashObject.transform.SetParent(transform, false);
            _screenFlash = flashObject.AddComponent<Image>();
            _screenFlash.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            _screenFlash.raycastTarget = false;
            StretchRect(flashObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void EnsureEventSystem() {
            EventSystem existingEventSystem = FindFirstObjectByType<EventSystem>();
            if (existingEventSystem != null) {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void BuildHud() {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject hudObject = new GameObject("HUD");
            hudObject.transform.SetParent(transform, false);
            RectTransform hudRect = hudObject.AddComponent<RectTransform>();
            StretchRect(hudRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _scoreText = CreateText("ScoreText", hudRect, font, 56, TextAnchor.UpperLeft, new Vector2(60.0f, -70.0f));
            _comboText = CreateText("ComboText", hudRect, font, 40, TextAnchor.UpperLeft, new Vector2(60.0f, -145.0f));
            _metaText = CreateText("MetaText", hudRect, font, 28, TextAnchor.UpperRight, new Vector2(-60.0f, -70.0f));

            GameObject energyBackObject = new GameObject("EnergyBack");
            energyBackObject.transform.SetParent(hudRect, false);
            Image energyBack = energyBackObject.AddComponent<Image>();
            energyBack.color = new Color(1.0f, 1.0f, 1.0f, 0.12f);
            RectTransform energyBackRect = energyBackObject.GetComponent<RectTransform>();
            energyBackRect.anchorMin = new Vector2(0.5f, 0.0f);
            energyBackRect.anchorMax = new Vector2(0.5f, 0.0f);
            energyBackRect.anchoredPosition = new Vector2(0.0f, 120.0f);
            energyBackRect.sizeDelta = new Vector2(420.0f, 26.0f);

            GameObject energyFillObject = new GameObject("EnergyFill");
            energyFillObject.transform.SetParent(energyBackObject.transform, false);
            _energyFill = energyFillObject.AddComponent<Image>();
            _energyFill.type = Image.Type.Filled;
            _energyFill.fillMethod = Image.FillMethod.Horizontal;
            _energyFill.fillAmount = 1.0f;
            RectTransform energyFillRect = energyFillObject.GetComponent<RectTransform>();
            StretchRect(energyFillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void BuildGameOverPanel() {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(transform, false);
            Image panelImage = _gameOverPanel.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.03f, 0.08f, 0.88f);
            RectTransform panelRect = _gameOverPanel.GetComponent<RectTransform>();
            StretchRect(panelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _gameOverText = CreateText("GameOverText", panelRect, font, 54, TextAnchor.MiddleCenter, Vector2.zero);
            RectTransform textRect = _gameOverText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0.0f, 180.0f);
            textRect.sizeDelta = new Vector2(800.0f, 400.0f);

            GameObject buttonObject = new GameObject("RestartButton");
            buttonObject.transform.SetParent(_gameOverPanel.transform, false);
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.34f, 0.82f, 1.0f, 1.0f);
            _restartButton = buttonObject.AddComponent<Button>();
            _restartButton.onClick.AddListener(RestartGame);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0.0f, -120.0f);
            buttonRect.sizeDelta = new Vector2(320.0f, 100.0f);

            Text buttonText = CreateText("RestartText", buttonRect, font, 42, TextAnchor.MiddleCenter, Vector2.zero);
            buttonText.text = "Restart";
            StretchRect(buttonText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _gameOverPanel.SetActive(false);
        }

        private Text CreateText(string name, RectTransform parent, Font font, int fontSize, TextAnchor anchor, Vector2 anchoredPosition) {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(-120.0f, 80.0f);
            return text;
        }

        private void RestartGame() {
            _gameManager.RestartRun();
        }

        private void StretchRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}