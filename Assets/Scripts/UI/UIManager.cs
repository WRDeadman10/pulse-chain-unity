using LeTai.Asset.TranslucentImage;

using MPUIKIT;

using PulseChain.Core;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace PulseChain.Gameplay {
    public sealed class UIManager : MonoBehaviour {
        private Canvas _canvas;
        private CanvasScaler _canvasScaler;
        private GraphicRaycaster _raycaster;
        private RectTransform _root;
        private RectTransform _gameplayRoot;
        private Camera _uiCamera;
        private TranslucentImageSource _translucentImageSource;
        private ScalableBlurConfig _blurConfig;
        private Material _translucentMaterial;
        private Image _background;
        private MPImage _backgroundGlowLeft;
        private MPImage _backgroundGlowRight;
        private TextMeshProUGUI _scoreText;
        private TextMeshProUGUI _comboText;
        private TextMeshProUGUI _metaText;
        private ProceduralImage _energyFill;
        private Image _screenFlash;
        private GameObject _tutorialPanel;
        private TextMeshProUGUI _tutorialTitleText;
        private TextMeshProUGUI _tutorialBodyText;
        private Button _tutorialStartButton;
        private TextMeshProUGUI _tutorialStartButtonText;
        private Button _tutorialSkipButton;
        private TextMeshProUGUI _tutorialSkipButtonText;
        private TextMeshProUGUI _tutorialHintText;
        private GameObject _gameOverPanel;
        private TextMeshProUGUI _gameOverText;
        private Button _restartButton;
        private TextMeshProUGUI _restartButtonText;
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

        private void OnDestroy() {
            if (_translucentMaterial != null) {
                Destroy(_translucentMaterial);
            }

            if (_blurConfig != null) {
                Destroy(_blurConfig);
            }
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
                float scale = 1.0f + (Mathf.Sin(_successPunchTime * 20.0f) * 0.03f);
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
            _tutorialStartButtonText.text = actionLabel;
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

            EnsureUiCamera();

            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _uiCamera;
            _canvas.planeDistance = 100.0f;
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

        private void EnsureUiCamera() {
            if (_uiCamera == null) {
                _uiCamera = Camera.main;
            }

            if (_uiCamera == null) {
                _uiCamera = FindFirstObjectByType<Camera>();
            }

            if (_uiCamera == null) {
                GameObject cameraObject = new GameObject("PulseChainUICamera");
                cameraObject.transform.SetParent(transform.parent, false);
                _uiCamera = cameraObject.AddComponent<Camera>();
                _uiCamera.tag = "MainCamera";
                AudioListener audioListener = cameraObject.AddComponent<AudioListener>();
                audioListener.enabled = true;
            }

            _uiCamera.clearFlags = CameraClearFlags.SolidColor;
            _uiCamera.backgroundColor = new Color(0.03f, 0.05f, 0.10f, 1.0f);
            _uiCamera.orthographic = true;
            _uiCamera.nearClipPlane = 0.1f;
            _uiCamera.farClipPlane = 500.0f;

            _translucentImageSource = _uiCamera.GetComponent<TranslucentImageSource>();
            if (_translucentImageSource == null) {
                _translucentImageSource = _uiCamera.gameObject.AddComponent<TranslucentImageSource>();
            }

            if (_blurConfig == null) {
                _blurConfig = ScriptableObject.CreateInstance<ScalableBlurConfig>();
                _blurConfig.Radius = 5.0f;
                _blurConfig.Iteration = 3;
            }

            _translucentImageSource.BlurConfig = _blurConfig;
            _translucentImageSource.Downsample = 1;
            _translucentImageSource.MaxUpdateRate = 30.0f;
            _translucentImageSource.SkipCulling = false;
            _translucentImageSource.BlurRegion = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
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

            RectTransform topCardRect = CreateRoundedPanel("TopHudCard", hudRect, new Vector2(940.0f, 260.0f), new Vector2(0.0f, -140.0f), new Color(0.03f, 0.08f, 0.14f, 0.42f), 42.0f);
            CreatePanelAccent("TopHudAccent", topCardRect, new Color(0.20f, 0.88f, 1.0f, 0.16f), new Vector2(-300.0f, 60.0f), new Vector2(240.0f, 240.0f));

            _scoreText = CreateText("ScoreText", topCardRect, 56.0f, TextAlignmentOptions.TopLeft, new Vector2(70.0f, -42.0f));
            _comboText = CreateText("ComboText", topCardRect, 40.0f, TextAlignmentOptions.TopLeft, new Vector2(70.0f, -116.0f));
            _metaText = CreateText("MetaText", topCardRect, 28.0f, TextAlignmentOptions.TopRight, new Vector2(-60.0f, -42.0f));

            GameObject energyBackObject = new GameObject("EnergyBack");
            energyBackObject.transform.SetParent(hudRect, false);
            ProceduralImage energyBack = energyBackObject.AddComponent<ProceduralImage>();
            energyBack.color = new Color(1.0f, 1.0f, 1.0f, 0.10f);
            UniformModifier energyBackModifier = energyBackObject.AddComponent<UniformModifier>();
            energyBackModifier.Radius = 13.0f;
            RectTransform energyBackRect = energyBackObject.GetComponent<RectTransform>();
            energyBackRect.anchorMin = new Vector2(0.5f, 0.0f);
            energyBackRect.anchorMax = new Vector2(0.5f, 0.0f);
            energyBackRect.anchoredPosition = new Vector2(0.0f, 120.0f);
            energyBackRect.sizeDelta = new Vector2(420.0f, 26.0f);

            GameObject energyFillObject = new GameObject("EnergyFill");
            energyFillObject.transform.SetParent(energyBackObject.transform, false);
            _energyFill = energyFillObject.AddComponent<ProceduralImage>();
            _energyFill.type = Image.Type.Filled;
            _energyFill.fillMethod = Image.FillMethod.Horizontal;
            _energyFill.fillAmount = 1.0f;
            UniformModifier energyFillModifier = energyFillObject.AddComponent<UniformModifier>();
            energyFillModifier.Radius = 13.0f;
            RectTransform energyFillRect = energyFillObject.GetComponent<RectTransform>();
            StretchRect(energyFillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _tutorialHintText = CreateFloatingCallout(hudRect);
            _tutorialHintText.gameObject.SetActive(false);
        }

        private void BuildTutorialPanel() {
            _tutorialPanel = new GameObject("TutorialPanel");
            _tutorialPanel.transform.SetParent(transform, false);
            Image tutorialDim = _tutorialPanel.AddComponent<Image>();
            tutorialDim.color = new Color(0.02f, 0.03f, 0.08f, 0.56f);
            RectTransform panelRect = _tutorialPanel.GetComponent<RectTransform>();
            StretchRect(panelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform cardRect = CreateGlassCard("TutorialCard", panelRect, new Vector2(920.0f, 1180.0f), new Color(0.05f, 0.11f, 0.20f, 0.76f), new Color(0.28f, 0.82f, 1.0f, 0.18f));
            cardRect.anchoredPosition = new Vector2(0.0f, 10.0f);

            _tutorialTitleText = CreateText("TutorialTitle", cardRect, 72.0f, TextAlignmentOptions.Center, Vector2.zero);
            _tutorialTitleText.text = "Pulse Chain";
            RectTransform titleRect = _tutorialTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1.0f);
            titleRect.anchorMax = new Vector2(0.5f, 1.0f);
            titleRect.anchoredPosition = new Vector2(0.0f, -120.0f);
            titleRect.sizeDelta = new Vector2(720.0f, 120.0f);

            _tutorialBodyText = CreateText("TutorialBody", cardRect, 34.0f, TextAlignmentOptions.Center, Vector2.zero);
            _tutorialBodyText.text =
                "Tap when the pulse reaches the next node and its green accept zone is aligned.\n\n" +
                "Tap on the right half of the screen to take the faster risk path.\n\n" +
                "Hold to slow time, but your energy bar will drain.\n\n" +
                "A mistimed tap or a missed transfer breaks the chain.";
            _tutorialBodyText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform bodyRect = _tutorialBodyText.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0.0f, 70.0f);
            bodyRect.sizeDelta = new Vector2(760.0f, 460.0f);

            _tutorialStartButton = CreateActionButton("TutorialStartButton", cardRect, new Vector2(0.0f, -360.0f), new Vector2(380.0f, 110.0f), new Color(0.24f, 0.82f, 1.0f, 1.0f), ContinueTutorialPrompt, out _tutorialStartButtonText);
            _tutorialStartButtonText.text = "Start Run";

            _tutorialSkipButton = CreateActionButton("TutorialSkipButton", cardRect, new Vector2(0.0f, -500.0f), new Vector2(280.0f, 82.0f), new Color(1.0f, 1.0f, 1.0f, 0.10f), SkipTutorial, out _tutorialSkipButtonText);
            _tutorialSkipButtonText.text = "Skip Tutorial";
        }

        private void BuildGameOverPanel() {
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(transform, false);
            Image dimImage = _gameOverPanel.AddComponent<Image>();
            dimImage.color = new Color(0.02f, 0.03f, 0.08f, 0.58f);
            RectTransform panelRect = _gameOverPanel.GetComponent<RectTransform>();
            StretchRect(panelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform cardRect = CreateGlassCard("GameOverCard", panelRect, new Vector2(860.0f, 760.0f), new Color(0.08f, 0.10f, 0.18f, 0.78f), new Color(1.0f, 0.55f, 0.32f, 0.16f));

            _gameOverText = CreateText("GameOverText", cardRect, 54.0f, TextAlignmentOptions.Center, Vector2.zero);
            RectTransform textRect = _gameOverText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0.0f, 120.0f);
            textRect.sizeDelta = new Vector2(700.0f, 340.0f);

            _restartButton = CreateActionButton("RestartButton", cardRect, new Vector2(0.0f, -190.0f), new Vector2(320.0f, 100.0f), new Color(0.34f, 0.82f, 1.0f, 1.0f), RestartGame, out _restartButtonText);
            _restartButtonText.text = "Restart";
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

        private MPImage CreateBackgroundOrb(string objectName, Color color, Vector2 anchoredPosition, Vector2 size) {
            GameObject orbObject = new GameObject(objectName);
            orbObject.transform.SetParent(transform, false);
            MPImage orbImage = orbObject.AddComponent<MPImage>();
            ConfigureCircleShape(orbImage, color, 0.0f, Color.clear, 4.0f);
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

        private RectTransform CreateRoundedPanel(string name, RectTransform parent, Vector2 size, Vector2 anchoredPosition, Color color, float radius) {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            ProceduralImage proceduralImage = panelObject.AddComponent<ProceduralImage>();
            proceduralImage.color = color;
            proceduralImage.FalloffDistance = 1.0f;
            UniformModifier modifier = panelObject.AddComponent<UniformModifier>();
            modifier.Radius = radius;
            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
            rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            return rectTransform;
        }

        private RectTransform CreateGlassCard(string name, RectTransform parent, Vector2 size, Color tintColor, Color accentColor) {
            GameObject cardObject = new GameObject(name);
            cardObject.transform.SetParent(parent, false);
            RectTransform cardRect = cardObject.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = size;

            GameObject blurObject = new GameObject("Blur");
            blurObject.transform.SetParent(cardRect, false);
            TranslucentImage translucentImage = blurObject.AddComponent<TranslucentImage>();
            translucentImage.color = tintColor;
            translucentImage.material = GetTranslucentMaterial();
            translucentImage.source = _translucentImageSource;
            RectTransform blurRect = blurObject.GetComponent<RectTransform>();
            StretchRect(blurRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject shellObject = new GameObject("Shell");
            shellObject.transform.SetParent(cardRect, false);
            ProceduralImage shellImage = shellObject.AddComponent<ProceduralImage>();
            shellImage.color = new Color(1.0f, 1.0f, 1.0f, 0.06f);
            shellImage.BorderWidth = 0.014f;
            UniformModifier shellModifier = shellObject.AddComponent<UniformModifier>();
            shellModifier.Radius = 42.0f;
            RectTransform shellRect = shellObject.GetComponent<RectTransform>();
            StretchRect(shellRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CreatePanelAccent("AccentOrb", cardRect, accentColor, new Vector2((size.x * -0.32f), (size.y * 0.24f)), new Vector2(220.0f, 220.0f));
            CreatePanelAccent("AccentOrbWarm", cardRect, new Color(1.0f, 0.58f, 0.24f, 0.10f), new Vector2(size.x * 0.28f, size.y * -0.26f), new Vector2(180.0f, 180.0f));
            return cardRect;
        }

        private void CreatePanelAccent(string name, RectTransform parent, Color color, Vector2 anchoredPosition, Vector2 size) {
            GameObject accentObject = new GameObject(name);
            accentObject.transform.SetParent(parent, false);
            MPImage accentImage = accentObject.AddComponent<MPImage>();
            ConfigureCircleShape(accentImage, color, 0.0f, Color.clear, 4.0f);
            accentImage.raycastTarget = false;
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0.5f, 0.5f);
            accentRect.anchorMax = new Vector2(0.5f, 0.5f);
            accentRect.anchoredPosition = anchoredPosition;
            accentRect.sizeDelta = size;
        }

        private TextMeshProUGUI CreateFloatingCallout(RectTransform parent) {
            RectTransform hintCard = CreateGlassCard("TutorialHintCard", parent, new Vector2(860.0f, 144.0f), new Color(0.08f, 0.11f, 0.20f, 0.74f), new Color(0.40f, 1.0f, 0.72f, 0.12f));
            hintCard.anchorMin = new Vector2(0.5f, 0.0f);
            hintCard.anchorMax = new Vector2(0.5f, 0.0f);
            hintCard.anchoredPosition = new Vector2(0.0f, 230.0f);

            TextMeshProUGUI hintText = CreateText("TutorialHint", hintCard, 34.0f, TextAlignmentOptions.Center, Vector2.zero);
            hintText.color = new Color(1.0f, 0.97f, 0.78f, 1.0f);
            RectTransform hintTextRect = hintText.GetComponent<RectTransform>();
            StretchRect(hintTextRect, Vector2.zero, Vector2.one, new Vector2(32.0f, 20.0f), new Vector2(-32.0f, -20.0f));
            return hintText;
        }

        private Button CreateActionButton(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor, UnityEngine.Events.UnityAction action, out TextMeshProUGUI labelText) {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            ProceduralImage buttonImage = buttonObject.AddComponent<ProceduralImage>();
            buttonImage.color = fillColor;
            buttonImage.FalloffDistance = 1.0f;
            UniformModifier modifier = buttonObject.AddComponent<UniformModifier>();
            modifier.Radius = size.y * 0.5f;
            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(action);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = size;

            CreatePanelAccent("ButtonAccent", buttonRect, new Color(1.0f, 1.0f, 1.0f, 0.10f), new Vector2(size.x * -0.22f, 0.0f), new Vector2(size.y * 1.1f, size.y * 1.1f));

            labelText = CreateText(name + "Text", buttonRect, 42.0f, TextAlignmentOptions.Center, Vector2.zero);
            StretchRect(labelText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private Material GetTranslucentMaterial() {
            if (_translucentMaterial == null) {
                Shader translucentShader = Shader.Find("UI/TranslucentImage");
                if (translucentShader != null) {
                    _translucentMaterial = new Material(translucentShader);
                }
            }

            return _translucentMaterial;
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

        private void StretchRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}