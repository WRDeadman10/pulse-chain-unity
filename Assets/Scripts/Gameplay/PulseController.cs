using System.Collections.Generic;

using PulseChain.Core;

using UnityEngine;

namespace PulseChain.Gameplay {
    public sealed class PulseController : MonoBehaviour {
        private readonly List<PulseRuntimeState> _pulses = new List<PulseRuntimeState>(3);
        private RectTransform _root;
        private PulseChainDifficultySettings _difficultySettings;
        private PulseStyleConfig _pulseStyleConfig;
        private NodeSpawner _nodeSpawner;
        private ComponentPool<PulseView> _pulsePool;
        private PulseChainNode _lastSuccessfulNode;
        private bool _ghostShieldConsumed;
        private bool _isNearMissActive;
        private bool _isOverloadActive;

        public bool IsNearMissActive {
            get {
                return _isNearMissActive;
            }
        }

        public bool IsOverloadActive {
            get {
                return _isOverloadActive;
            }
        }

        public void Initialize(RectTransform root, PulseChainDifficultySettings difficultySettings, PulseStyleConfig pulseStyleConfig, NodeSpawner nodeSpawner) {
            _root = root;
            _difficultySettings = difficultySettings;
            _pulseStyleConfig = pulseStyleConfig;
            _nodeSpawner = nodeSpawner;
            _pulsePool = new ComponentPool<PulseView>(CreatePulseView);
        }

        public void BeginRun() {
            ClearPulses();
            _lastSuccessfulNode = _nodeSpawner.StartNode;
            _ghostShieldConsumed = false;
            _isNearMissActive = false;
            _isOverloadActive = false;
            EnsurePulseCount(1, _nodeSpawner.StartNode);
        }

        public void Tick(float deltaTime, float unscaledTime, int score, bool slowMotionActive) {
            _isNearMissActive = false;
            _isOverloadActive = score >= _difficultySettings.OverloadScoreThreshold;

            int desiredPulseCount = 1;
            if (score >= _difficultySettings.MultiPulseScoreThreshold) {
                desiredPulseCount = 2;
            }

            if (score >= _difficultySettings.TriplePulseScoreThreshold) {
                desiredPulseCount = 3;
            }

            EnsurePulseCount(desiredPulseCount, _lastSuccessfulNode);

            float baseSpeed = _difficultySettings.BasePulseSpeed + (_difficultySettings.PulseSpeedGrowthPerScore * score);
            if (_isOverloadActive) {
                baseSpeed *= 1.25f;
            }

            if (slowMotionActive) {
                baseSpeed *= 0.8f;
            }

            int pulseCount = _pulses.Count;
            for (int i = 0; i < pulseCount; i++) {
                PulseRuntimeState pulse = _pulses[i];
                if (pulse.TargetNode == null) {
                    pulse.TargetNode = _nodeSpawner.GetNextNode(pulse.CurrentNode, false, score);
                }

                float segmentLength = Vector2.Distance(pulse.CurrentNode.CurrentPosition, pulse.TargetNode.CurrentPosition);
                float normalizedStep = (baseSpeed * deltaTime) / Mathf.Max(1.0f, segmentLength);
                pulse.Progress += normalizedStep;

                float visualProgress = pulse.Progress;
                Vector2 startPosition = pulse.CurrentNode.CurrentPosition;
                Vector2 endPosition = pulse.TargetNode.CurrentPosition;
                Vector2 pulsePosition = Vector2.Lerp(startPosition, endPosition, Mathf.Clamp01(visualProgress));

                if (pulse.Style == PulseVisualStyle.ZigZag) {
                    float zigZagOffset = Mathf.Sin((unscaledTime * 18.0f) + pulse.ZigZagPhase) * _pulseStyleConfig.ZigZagAmplitude;
                    pulsePosition += new Vector2(0.0f, zigZagOffset);
                }

                pulse.View.SetPosition(pulsePosition);
                pulse.View.SetVisualState(pulse.Style, _isOverloadActive, pulse.Progress);

                if (pulse.Progress > 1.12f) {
                    GameManager.Instance.EndRun(true);
                    return;
                }
            }
        }

        public TapEvaluation TryTransfer(bool chooseRiskPath) {
            bool allSuccessful = true;
            bool allPerfect = true;
            bool nearMiss = false;

            int pulseCount = _pulses.Count;
            for (int i = 0; i < pulseCount; i++) {
                PulseRuntimeState pulse = _pulses[i];
                if (pulse.TargetNode == null) {
                    allSuccessful = false;
                    allPerfect = false;
                    continue;
                }

                float arrivalDelta = Mathf.Abs(1.0f - pulse.Progress);
                bool withinArrival = arrivalDelta <= _difficultySettings.ArrivalWindow;
                bool perfectArrival = arrivalDelta <= _difficultySettings.PerfectArrivalWindow;
                AcceptZoneState acceptZone = pulse.TargetNode.Zones[pulse.TargetNode.CorrectZoneIndex];
                float windowValue = _nodeSpawner.GetZoneWindowValue(pulse.TargetNode, acceptZone, Time.unscaledTime);
                float windowCenter = 0.5f;
                float zoneDelta = Mathf.Abs(windowCenter - windowValue);
                float zoneWidth = acceptZone.Width * 0.5f;
                bool zoneValid = zoneDelta <= zoneWidth;

                if (!withinArrival || !zoneValid) {
                    allSuccessful = false;
                    allPerfect = false;
                    if ((arrivalDelta <= (_difficultySettings.ArrivalWindow + 0.06f)) || (zoneDelta <= (zoneWidth + 0.08f))) {
                        nearMiss = true;
                    }
                    continue;
                }

                if (!perfectArrival || zoneDelta > (_difficultySettings.PerfectArrivalWindow * 0.5f)) {
                    allPerfect = false;
                }
            }

            if (!allSuccessful) {
                bool canGhostRevive = false;
                if (!_ghostShieldConsumed && (_pulseStyleConfig.DefaultStyle == PulseVisualStyle.Ghost)) {
                    canGhostRevive = true;
                }

                _isNearMissActive = nearMiss;
                return new TapEvaluation(false, false, nearMiss, canGhostRevive);
            }

            AdvancePulses(chooseRiskPath, GameManager.Instance.Score);
            return new TapEvaluation(true, allPerfect, false, false);
        }

        public void ApplySuccessfulTransfer(int score) {
            _isOverloadActive = score >= _difficultySettings.OverloadScoreThreshold;
        }

        public bool ConsumeGhostShield() {
            if (_ghostShieldConsumed || (_pulseStyleConfig.DefaultStyle != PulseVisualStyle.Ghost)) {
                return false;
            }

            _ghostShieldConsumed = true;
            ReviveAtLastSuccessfulNode();
            return true;
        }

        public void ReviveAtLastSuccessfulNode() {
            int pulseCount = _pulses.Count;
            for (int i = 0; i < pulseCount; i++) {
                PulseRuntimeState pulse = _pulses[i];
                pulse.CurrentNode = _lastSuccessfulNode;
                pulse.TargetNode = _nodeSpawner.GetNextNode(_lastSuccessfulNode, false, GameManager.Instance.Score);
                pulse.Progress = -pulse.SpawnOffset;
                pulse.View.SetPosition(_lastSuccessfulNode.CurrentPosition);
            }
        }

        private void AdvancePulses(bool chooseRiskPath, int score) {
            int pulseCount = _pulses.Count;
            for (int i = 0; i < pulseCount; i++) {
                PulseRuntimeState pulse = _pulses[i];
                PulseChainNode landedNode = pulse.TargetNode;
                pulse.CurrentNode = landedNode;
                pulse.TargetNode = _nodeSpawner.GetNextNode(landedNode, chooseRiskPath, score);
                pulse.Progress = -pulse.SpawnOffset;
            }

            _lastSuccessfulNode = _pulses[0].CurrentNode;
        }

        private void EnsurePulseCount(int desiredPulseCount, PulseChainNode startNode) {
            while (_pulses.Count < desiredPulseCount) {
                PulseRuntimeState pulse = new PulseRuntimeState();
                pulse.Style = ResolvePulseStyleForIndex(_pulses.Count);
                pulse.CurrentNode = startNode;
                pulse.TargetNode = _nodeSpawner.GetNextNode(startNode, false, 0);
                pulse.SpawnOffset = 0.10f * _pulses.Count;
                pulse.Progress = -pulse.SpawnOffset;
                pulse.ZigZagPhase = 0.75f * _pulses.Count;
                pulse.View = _pulsePool.Get();
                pulse.View.transform.SetParent(_root, false);
                pulse.View.Configure(_pulseStyleConfig.BaseSize);
                pulse.View.SetPosition(startNode.CurrentPosition);
                _pulses.Add(pulse);
            }

            while (_pulses.Count > desiredPulseCount) {
                int lastIndex = _pulses.Count - 1;
                PulseRuntimeState pulse = _pulses[lastIndex];
                _pulsePool.Release(pulse.View);
                _pulses.RemoveAt(lastIndex);
            }
        }

        private void ClearPulses() {
            int pulseCount = _pulses.Count;
            for (int i = 0; i < pulseCount; i++) {
                _pulsePool.Release(_pulses[i].View);
            }

            _pulses.Clear();
        }

        private PulseVisualStyle ResolvePulseStyleForIndex(int index) {
            if (index == 0) {
                return _pulseStyleConfig.DefaultStyle;
            }

            if (index == 1) {
                return PulseVisualStyle.Square;
            }

            return PulseVisualStyle.ZigZag;
        }

        private PulseView CreatePulseView() {
            GameObject pulseObject = new GameObject("PulseView");
            PulseView pulseView = pulseObject.AddComponent<PulseView>();
            return pulseView;
        }

        private sealed class PulseRuntimeState {
            public PulseChainNode CurrentNode;
            public PulseChainNode TargetNode;
            public float Progress;
            public float SpawnOffset;
            public float ZigZagPhase;
            public PulseVisualStyle Style;
            public PulseView View;
        }
    }
}