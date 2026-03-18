using System;

using PulseChain.Gameplay;

using UnityEngine;

namespace PulseChain.Core {
    public sealed class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        private UIManager _uiManager;
        private NodeSpawner _nodeSpawner;
        private PulseController _pulseController;
        private AudioSystem _audioSystem;
        private PersistenceSystem _persistenceSystem;
        private AdHooksSystem _adHooksSystem;
        private PulseChainDifficultySettings _difficultySettings;
        private PulseStyleConfig _pulseStyleConfig;
        private GameRunState _runState;
        private bool _isConfigured;
        private bool _slowMotionActive;
        private bool _isTutorialActive;
        private int _score;
        private int _combo;
        private int _runCount;
        private float _energy = 1.0f;
        private float _sessionTime;
        private float _tutorialHoldTime;
        private TutorialStep _tutorialStep;

        public int Score {
            get {
                return _score;
            }
        }

        public int Combo {
            get {
                return _combo;
            }
        }

        public float Energy {
            get {
                return _energy;
            }
        }

        public bool IsGameRunning {
            get {
                return _runState == GameRunState.Running;
            }
        }

        public bool IsTutorialActive {
            get {
                return _isTutorialActive;
            }
        }

        public PulseChainDifficultySettings DifficultySettings {
            get {
                return _difficultySettings;
            }
        }

        public PulseStyleConfig PulseStyleConfig {
            get {
                return _pulseStyleConfig;
            }
        }

        public event Action<int, int> ScoreChanged;
        public event Action<bool> OverloadStateChanged;
        public event Action RunStarted;
        public event Action<GameOverData> RunEnded;

        public void Configure(
            UIManager uiManager,
            NodeSpawner nodeSpawner,
            PulseController pulseController,
            AudioSystem audioSystem,
            PersistenceSystem persistenceSystem,
            AdHooksSystem adHooksSystem) {
            _uiManager = uiManager;
            _nodeSpawner = nodeSpawner;
            _pulseController = pulseController;
            _audioSystem = audioSystem;
            _persistenceSystem = persistenceSystem;
            _adHooksSystem = adHooksSystem;
            _difficultySettings = PulseChainDifficultySettings.CreateRuntimeInstance();
            _pulseStyleConfig = PulseStyleConfig.CreateRuntimeInstance();
            _isConfigured = true;
        }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start() {
            if (!_isConfigured) {
                return;
            }

            _uiManager.Initialize(this);
            _audioSystem.Initialize();
            _persistenceSystem.Initialize();
            _adHooksSystem.Initialize(this);
            _nodeSpawner.Initialize(_uiManager.GameplayRoot, _difficultySettings);
            _pulseController.Initialize(_uiManager.GameplayRoot, _difficultySettings, _pulseStyleConfig, _nodeSpawner);
            if (_persistenceSystem.IsTutorialCompleted()) {
                StartRun();
            } else {
                StartFirstLaunchTutorial();
            }
        }

        private void Update() {
            if (_runState != GameRunState.Running) {
                return;
            }

            float unscaledDeltaTime = Time.unscaledDeltaTime;
            _sessionTime += unscaledDeltaTime;
            UpdateTimeScale(unscaledDeltaTime);
            HandleInput();
            _nodeSpawner.Tick(Time.unscaledTime, _score);
            _pulseController.Tick(Time.deltaTime, Time.unscaledTime, _score, _slowMotionActive);
            _uiManager.Tick(Time.unscaledDeltaTime, _combo, _pulseController.IsNearMissActive, _pulseController.IsOverloadActive);
            _audioSystem.Tick(Time.unscaledDeltaTime, _combo, _pulseController.IsOverloadActive);
            TickTutorialFlow();
        }

        public void StartRun() {
            _isTutorialActive = false;
            _runState = GameRunState.Running;
            _score = 0;
            _combo = 0;
            _energy = 1.0f;
            _sessionTime = 0.0f;
            _slowMotionActive = false;
            _tutorialHoldTime = 0.0f;
            _tutorialStep = TutorialStep.None;
            _nodeSpawner.BeginRun(_persistenceSystem.GetDailySeed());
            _pulseController.BeginRun();
            _uiManager.ShowRunHud();
            _uiManager.UpdateScore(_score, _combo, _persistenceSystem.HighScore, _persistenceSystem.GetDailyBest());
            ScoreChanged?.Invoke(_score, _combo);
            OverloadStateChanged?.Invoke(false);
            RunStarted?.Invoke();
        }

        public void StartFirstLaunchTutorial() {
            _isTutorialActive = true;
            _tutorialStep = TutorialStep.Intro;
            _tutorialHoldTime = 0.0f;
            _runState = GameRunState.TutorialPrompt;
            Time.timeScale = 1.0f;
            _slowMotionActive = false;
            _uiManager.ShowTutorialPrompt(
                "Welcome to Pulse Chain",
                "You will learn the controls in a short guided run.\n\nWatch the pulse, hit one transfer, and use hold-to-slow-time once.",
                "Begin Tutorial",
                false);
        }

        public void ContinueTutorialPrompt() {
            if (!_isTutorialActive) {
                StartRun();
                return;
            }

            if (_tutorialStep == TutorialStep.Intro) {
                StartTutorialRun();
                return;
            }

            if (_tutorialStep == TutorialStep.Complete) {
                _persistenceSystem.SetTutorialCompleted();
                _uiManager.HideTutorial();
                StartRun();
                return;
            }

            _uiManager.HideTutorial();
            _runState = GameRunState.Running;
            if (_tutorialStep == TutorialStep.FirstTap) {
                _uiManager.ShowTutorialHint("Tap when the pulse reaches the next node.");
            } else if (_tutorialStep == TutorialStep.HoldSlowMotion) {
                _uiManager.ShowTutorialHint("Hold anywhere for a short moment to slow time.");
            }
        }

        public void SkipTutorial() {
            _persistenceSystem.SetTutorialCompleted();
            _isTutorialActive = false;
            _uiManager.HideTutorial();
            StartRun();
        }

        public void RestartRun() {
            _runCount++;
            StartRun();
        }

        public void HandleTap(float screenPositionX) {
            if (_runState != GameRunState.Running) {
                return;
            }

            if (_isTutorialActive && (_tutorialStep == TutorialStep.WatchPulse || _tutorialStep == TutorialStep.HoldSlowMotion)) {
                return;
            }

            bool chooseRiskPath = screenPositionX > (Screen.width * 0.5f);
            TapEvaluation evaluation = _pulseController.TryTransfer(chooseRiskPath);

            if (!evaluation.IsSuccess) {
                if (_isTutorialActive) {
                    ShowTutorialRetryPrompt();
                    return;
                }

                bool revived = false;
                if (evaluation.CanGhostRevive) {
                    revived = _pulseController.ConsumeGhostShield();
                } else if (_adHooksSystem.CanOfferRevive(_runCount)) {
                    revived = _adHooksSystem.TryReviveLastRun();
                    if (revived) {
                        _pulseController.ReviveAtLastSuccessfulNode();
                    }
                }

                if (!revived) {
                    EndRun(evaluation.NearMiss);
                    return;
                }
            }

            int scoreGain = evaluation.IsPerfect ? 10 + (_combo * 2) : 10;
            if (chooseRiskPath) {
                scoreGain += 5;
            }

            _score += scoreGain;
            if (evaluation.IsPerfect) {
                _combo++;
                _audioSystem.PlayPerfect(_combo);
                _uiManager.PlaySuccessPunch();
            } else {
                _combo = 0;
                _audioSystem.PlayHit();
            }

            _persistenceSystem.RegisterScore(_score);
            _uiManager.UpdateScore(_score, _combo, _persistenceSystem.HighScore, _persistenceSystem.GetDailyBest());
            _uiManager.ShowTapFeedback(evaluation.IsPerfect, evaluation.NearMiss);
            _pulseController.ApplySuccessfulTransfer(_score);
            ScoreChanged?.Invoke(_score, _combo);
            OverloadStateChanged?.Invoke(_pulseController.IsOverloadActive);

            if (_isTutorialActive && _tutorialStep == TutorialStep.FirstTap) {
                _uiManager.ShowTutorialHint(string.Empty);
                PauseTutorialPrompt(
                    TutorialStep.HoldSlowMotion,
                    "Hold To Slow Time",
                    "Press and hold anywhere for a moment. Time will slow down, but your energy bar will drain while you hold it.");
            }
        }

        public void EndRun(bool nearMiss) {
            if (_isTutorialActive) {
                ShowTutorialRetryPrompt();
                return;
            }

            if (_runState == GameRunState.GameOver) {
                return;
            }

            _runState = GameRunState.GameOver;
            Time.timeScale = 1.0f;
            _slowMotionActive = false;
            _combo = 0;
            _audioSystem.PlayMiss();
            GameOverData gameOverData = new GameOverData(_score, _persistenceSystem.HighScore, _persistenceSystem.GetDailyBest(), nearMiss);
            _uiManager.ShowGameOver(gameOverData);
            RunEnded?.Invoke(gameOverData);

            if (_adHooksSystem.ShouldShowInterstitial(_runCount)) {
                _adHooksSystem.MarkInterstitialOpportunity();
            }
        }

        private void UpdateTimeScale(float deltaTime) {
            bool holdInput = Input.touchCount > 0;
            if (!holdInput) {
                holdInput = Input.GetMouseButton(0);
            }

            if (holdInput && _energy > 0.0f) {
                _slowMotionActive = true;
                _energy = Mathf.Max(0.0f, _energy - (_difficultySettings.EnergyDrainPerSecond * deltaTime));
            } else {
                _slowMotionActive = false;
                _energy = Mathf.Min(1.0f, _energy + (_difficultySettings.EnergyRecoverPerSecond * deltaTime));
            }

            Time.timeScale = _slowMotionActive ? _difficultySettings.SlowMotionScale : 1.0f;
            _uiManager.UpdateEnergy(_energy, _slowMotionActive);
        }

        private void HandleInput() {
            if (Input.touchCount > 0) {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) {
                    HandleTap(touch.position.x);
                }
            }

            if (Input.GetMouseButtonDown(0)) {
                HandleTap(Input.mousePosition.x);
            }
        }

        private void StartTutorialRun() {
            _score = 0;
            _combo = 0;
            _energy = 1.0f;
            _sessionTime = 0.0f;
            _slowMotionActive = false;
            _tutorialHoldTime = 0.0f;
            _tutorialStep = TutorialStep.WatchPulse;
            _runState = GameRunState.Running;
            _nodeSpawner.BeginRun(_persistenceSystem.GetDailySeed());
            _pulseController.BeginRun();
            _uiManager.HideTutorial();
            _uiManager.ShowRunHud();
            _uiManager.UpdateScore(_score, _combo, _persistenceSystem.HighScore, _persistenceSystem.GetDailyBest());
            _uiManager.ShowTutorialHint("Watch the pulse travel to the next node.");
        }

        private void TickTutorialFlow() {
            if (!_isTutorialActive) {
                return;
            }

            if (_tutorialStep == TutorialStep.WatchPulse && _pulseController.LeadPulseProgress >= 0.35f) {
                PauseTutorialPrompt(
                    TutorialStep.FirstTap,
                    "Tap On The Beat",
                    "Tap when the pulse reaches the next node and the green accept marker is aligned.");
                return;
            }

            if (_tutorialStep == TutorialStep.HoldSlowMotion) {
                if (_slowMotionActive) {
                    _tutorialHoldTime += Time.unscaledDeltaTime;
                }

                if (_tutorialHoldTime >= 0.45f) {
                    PauseTutorialPrompt(
                        TutorialStep.Complete,
                        "You Are Ready",
                        "You completed the guided run.\n\nChain taps together, use risk paths for more score, and hold only when you need extra control.");
                }
            }
        }

        private void PauseTutorialPrompt(TutorialStep nextStep, string title, string body) {
            _tutorialStep = nextStep;
            _runState = GameRunState.TutorialPrompt;
            Time.timeScale = 1.0f;
            _slowMotionActive = false;
            _uiManager.ShowTutorialHint(string.Empty);
            _uiManager.ShowTutorialPrompt(title, body, nextStep == TutorialStep.Complete ? "Start Endless Run" : "Continue", true);
        }

        private void ShowTutorialRetryPrompt() {
            _tutorialHoldTime = 0.0f;
            _tutorialStep = TutorialStep.FirstTap;
            _runState = GameRunState.TutorialPrompt;
            _nodeSpawner.BeginRun(_persistenceSystem.GetDailySeed());
            _pulseController.BeginRun();
            _uiManager.ShowTutorialPrompt(
                "Try That Again",
                "Wait for the pulse to reach the node, then tap when the green accept marker is lined up.\n\nTake your time and use the visual cue.",
                "Retry",
                true);
        }
    }

    public enum GameRunState {
        None = 0,
        TutorialPrompt = 1,
        Running = 2,
        GameOver = 3
    }

    public enum TutorialStep {
        None = 0,
        Intro = 1,
        WatchPulse = 2,
        FirstTap = 3,
        HoldSlowMotion = 4,
        Complete = 5
    }

    public struct GameOverData {
        public readonly int Score;
        public readonly int HighScore;
        public readonly int DailyBest;
        public readonly bool NearMiss;

        public GameOverData(int score, int highScore, int dailyBest, bool nearMiss) {
            Score = score;
            HighScore = highScore;
            DailyBest = dailyBest;
            NearMiss = nearMiss;
        }
    }
}