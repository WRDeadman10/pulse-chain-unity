using PulseChain.Core;

using TMPro;

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
        private Image _backgroundGlowLeft;
        private Image _backgroundGlowRight;
        private TextMeshProUGUI _scoreText;
        private TextMeshProUGUI _comboText;
        private TextMeshProUGUI _metaText;
        private Image _energyFill;
        private Image _screenFlash;
        private GameObject _tutorialPanel;
        private TextMeshProUGUI _tutorialTitleText;
        private TextMeshProUGUI _tutorialBodyText;
        private Button _tutorialStartButton;
        private Button _tutorialSkipButton;
        private TextMeshProUGUI _tutorialHintText;
        private GameObject _gameOverPanel;
        private TextMeshProUGUI _gameOverText;
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
            BuildTutorialPanel();
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
            AnimateBackgroundOrbs(deltaTime, overloadActive);
            float flashTarget = overloadActive ? 0.12f : 0.0f;
            if (nearMissActive) {
                flashTarget = 0.22f;
                _screenShakeTime = 0.15f;
            }

            _screenFlash.color = Color.Lerp(_screenFlash.color, new Color(1.0f, 1.0f, 1.0f, flashTarget), deltaTime * 8.0f);
            _comboText.color = combo > 0 ? new Color(1.0f, 0.9f, 0.45f, 1.0f) : new Color(1.0f, 1.0f, 1.0f, 0.5f);
        }

        public void ShowRunHud() {
            HideTutorial();
            if (_gameOverPanel != null) {
                _gameOverPanel.SetActive(false);
            }

            if (_tutorialHintText != null) {
                _tutorialHintText.gameObject.SetActive(false);
            }
        }

        public void ShowTutorialPrompt(string title, string body, string actionLabel, bool allowSkip) {
            if (_tutorialPanel != null) {
                _tutorialPanel.SetActive(true);
            }

            ShowTutorialHint(string.Empty);
            _tutorialTitleText.text = title;
            _tutorialBodyText.text = body;
            _tutorialStartButton.GetComponentInChildren<TextMeshProUGUI>().text = actionLabel;
            _tutorialSkipButton.gameObject.SetActive(allowSkip);

            if (_gameOverPanel != null) {
                _gameOverPanel.SetActive(false);
            }
        }

        public void HideTutorial() {
            if (_tutorialPanel != null) {
                _tutorialPanel.SetActive(false);
            }
        }

        public void ShowTutorialHint(string message) {
            if (_tutorialHintText == null) {
                return;
            }

            if (string.IsNullOrEmpty(message)) {
                _tutorialHintText.gameObject.SetActive(false);
                return;
            }

            _tutorialHintText.text = message;
            _tutorialHintText.gameObject.SetActive(true);
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

            _backgroundGlowLeft = CreateBackgroundOrb("BackgroundGlowLeft", new Color(0.18f, 0.75f, 1.0f, 0.12f), new Vector2(-220.0f, 420.0f), new Vector2(620.0f, 620.0f));
            _backgroundGlowRight = CreateBackgroundOrb("BackgroundGlowRight", new Color(1.0f, 0.46f, 0.32f, 0.10f), new Vector2(260.0f, -280.0f), new Vector2(700.0f, 700.0f));

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
            GameObject hudObject = new GameObject("HUD");
            hudObject.transform.SetParent(transform, false);
            RectTransform hudRect = hudObject.AddComponent<RectTransform>();
            StretchRect(hudRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _scoreText = CreateText("ScoreText", hudRect, 56.0f, TextAlignmentOptions.TopLeft, new Vector2(60.0f, -70.0f));
            _comboText = CreateText("ComboText", hudRect, 40.0f, TextAlignmentOptions.TopLeft, new Vector2(60.0f, -145.0f));
            _metaText = CreateText("MetaText", hudRect, 28.0f, TextAlignmentOptions.TopRight, new Vector2(-60.0f, -70.0f));

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

            _tutorialHintText = CreateText("TutorialHint", hudRect, 34.0f, TextAlignmentOptions.Center, Vector2.zero);
            _tutorialHintText.gameObject.SetActive(false);
            _tutorialHintText.color = new Color(1.0f, 0.97f, 0.78f, 1.0f);
            RectTransform hintRect = _tutorialHintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0.0f);
            hintRect.anchorMax = new Vector2(0.5f, 0.0f);
            hintRect.anchoredPosition = new Vector2(0.0f, 220.0f);
            hintRect.sizeDelta = new Vector2(860.0f, 120.0f);
        }

        private void BuildTutorialPanel() {
            _tutorialPanel = new GameObject("TutorialPanel");
            _tutorialPanel.transform.SetParent(transform, false);
            Image tutorialPanelImage = _tutorialPanel.AddComponent<Image>();
            tutorialPanelImage.color = new Color(0.02f, 0.03f, 0.08f, 0.92f);
            RectTransform panelRect = _tutorialPanel.GetComponent<RectTransform>();
            StretchRect(panelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _tutorialTitleText = CreateText("TutorialTitle", panelRect, 72.0f, TextAlignmentOptions.Center, Vector2.zero);
            _tutorialTitleText.text = "Pulse Chain";
            RectTransform titleRect = _tutorialTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0.0f, 340.0f);
            titleRect.sizeDelta = new Vector2(900.0f, 120.0f);

            _tutorialBodyText = CreateText("TutorialBody", panelRect, 34.0f, TextAlignmentOptions.Center, Vector2.zero);
            _tutorialBodyText.text =
                "Tap when the pulse reaches the next node and its green accept zone is aligned.\n\n" +
                "Tap on the right half of the screen to take the faster risk path.\n\n" +
                "Hold to slow time, but your energy bar will drain.\n\n" +
                "A mistimed tap or a missed transfer breaks the chain.";
            _tutorialBodyText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform bodyRect = _tutorialBodyText.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0.0f, 30.0f);
            bodyRect.sizeDelta = new Vector2(920.0f, 520.0f);

            GameObject buttonObject = new GameObject("TutorialStartButton");
            buttonObject.transform.SetParent(_tutorialPanel.transform, false);
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.34f, 0.82f, 1.0f, 1.0f);
            _tutorialStartButton = buttonObject.AddComponent<Button>();
            _tutorialStartButton.onClick.AddListener(ContinueTutorialPrompt);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0.0f, -360.0f);
            buttonRect.sizeDelta = new Vector2(380.0f, 110.0f);

            TextMeshProUGUI buttonText = CreateText("TutorialStartText", buttonRect, 42.0f, TextAlignmentOptions.Center, Vector2.zero);
            buttonText.text = "Start Run";
            StretchRect(buttonText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject skipButtonObject = new GameObject("TutorialSkipButton");
            skipButtonObject.transform.SetParent(_tutorialPanel.transform, false);
            Image skipButtonImage = skipButtonObject.AddComponent<Image>();
            skipButtonImage.color = new Color(1.0f, 1.0f, 1.0f, 0.12f);
            _tutorialSkipButton = skipButtonObject.AddComponent<Button>();
            _tutorialSkipButton.onClick.AddListener(SkipTutorial);
            RectTransform skipButtonRect = skipButtonObject.GetComponent<RectTransform>();
            skipButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
            skipButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
            skipButtonRect.anchoredPosition = new Vector2(0.0f, -500.0f);
            skipButtonRect.sizeDelta = new Vector2(280.0f, 82.0f);

            TextMeshProUGUI skipButtonText = CreateText("TutorialSkipText", skipButtonRect, 30.0f, TextAlignmentOptions.Center, Vector2.zero);
            skipButtonText.text = "Skip Tutorial";
            StretchRect(skipButtonText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void BuildGameOverPanel() {
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(transform, false);
            Image panelImage = _gameOverPanel.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.03f, 0.08f, 0.88f);
            RectTransform panelRect = _gameOverPanel.GetComponent<RectTransform>();
            StretchRect(panelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _gameOverText = CreateText("GameOverText", panelRect, 54.0f, TextAlignmentOptions.Center, Vector2.zero);
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

            TextMeshProUGUI buttonText = CreateText("RestartText", buttonRect, 42.0f, TextAlignmentOptions.Center, Vector2.zero);
            buttonText.text = "Restart";
            StretchRect(buttonText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _gameOverPanel.SetActive(false);
        }

        private TextMeshProUGUI CreateText(string name, RectTransform parent, float fontSize, TextAlignmentOptions alignment, Vector2 anchoredPosition) {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = GetFontAsset();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(-120.0f, 80.0f);
            return text;
        }

        private TMP_FontAsset GetFontAsset() {
            TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
            if (fontAsset != null) {
                return fontAsset;
            }

            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return fontAsset;
        }

        private void RestartGame() {
            _gameManager.RestartRun();
        }

        private void ContinueTutorialPrompt() {
            _gameManager.ContinueTutorialPrompt();
        }

        private void SkipTutorial() {
            _gameManager.SkipTutorial();
        }

        private Image CreateBackgroundOrb(string objectName, Color color, Vector2 anchoredPosition, Vector2 size) {
            GameObject orbObject = new GameObject(objectName);
            orbObject.transform.SetParent(transform, false);
            Image orbImage = orbObject.AddComponent<Image>();
            orbImage.sprite = SpriteFactory.GetCircleSprite();
            orbImage.color = color;
            orbImage.raycastTarget = false;
            RectTransform orbRect = orbObject.GetComponent<RectTransform>();
            orbRect.anchorMin = new Vector2(0.5f, 0.5f);
            orbRect.anchorMax = new Vector2(0.5f, 0.5f);
            orbRect.anchoredPosition = anchoredPosition;
            orbRect.sizeDelta = size;
            return orbImage;
        }

        private void AnimateBackgroundOrbs(float deltaTime, bool overloadActive) {
            if (_backgroundGlowLeft == null || _backgroundGlowRight == null) {
                return;
            }

            float time = Time.unscaledTime;
            RectTransform leftRect = _backgroundGlowLeft.rectTransform;
            RectTransform rightRect = _backgroundGlowRight.rectTransform;
            leftRect.anchoredPosition = new Vector2(-220.0f + (Mathf.Sin(time * 0.55f) * 55.0f), 420.0f + (Mathf.Cos(time * 0.35f) * 45.0f));
            rightRect.anchoredPosition = new Vector2(260.0f + (Mathf.Cos(time * 0.45f) * 60.0f), -280.0f + (Mathf.Sin(time * 0.40f) * 70.0f));
            float leftScale = 1.0f + (Mathf.Sin(time * 0.8f) * 0.08f);
            float rightScale = 1.0f + (Mathf.Cos(time * 0.7f) * 0.1f);
            leftRect.localScale = new Vector3(leftScale, leftScale, 1.0f);
            rightRect.localScale = new Vector3(rightScale, rightScale, 1.0f);
            Color leftColor = overloadActive ? new Color(1.0f, 0.56f, 0.28f, 0.15f) : new Color(0.18f, 0.75f, 1.0f, 0.12f);
            Color rightColor = overloadActive ? new Color(1.0f, 0.15f, 0.15f, 0.14f) : new Color(1.0f, 0.46f, 0.32f, 0.10f);
            _backgroundGlowLeft.color = Color.Lerp(_backgroundGlowLeft.color, leftColor, deltaTime * 2.0f);
            _backgroundGlowRight.color = Color.Lerp(_backgroundGlowRight.color, rightColor, deltaTime * 2.0f);
        }

        private void StretchRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}