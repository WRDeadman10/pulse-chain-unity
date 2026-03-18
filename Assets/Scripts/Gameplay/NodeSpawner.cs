using System.Collections.Generic;

using PulseChain.Core;

using UnityEngine;

namespace PulseChain.Gameplay {
    public sealed class NodeSpawner : MonoBehaviour {
        private readonly List<PulseChainNode> _activeNodes = new List<PulseChainNode>(64);
        private readonly List<NodeView> _activeViews = new List<NodeView>(64);
        private readonly List<ConnectionView> _activeConnections = new List<ConnectionView>(64);
        private RectTransform _root;
        private PulseChainDifficultySettings _settings;
        private ComponentPool<NodeView> _nodePool;
        private ComponentPool<ConnectionView> _connectionPool;
        private System.Random _random;
        private int _nextNodeId;

        public PulseChainNode StartNode {
            get {
                return _activeNodes.Count > 0 ? _activeNodes[0] : null;
            }
        }

        public void Initialize(RectTransform root, PulseChainDifficultySettings settings) {
            _root = root;
            _settings = settings;
            _nodePool = new ComponentPool<NodeView>(CreateNodeView);
            _connectionPool = new ComponentPool<ConnectionView>(CreateConnectionView);
        }

        public void BeginRun(int seed) {
            _random = new System.Random(seed);
            _nextNodeId = 0;
            ClearViews();
            _activeNodes.Clear();
            CreateInitialChain();
            RebuildViews();
        }

        public void Tick(float time, int score) {
            int nodeCount = _activeNodes.Count;
            for (int i = 0; i < nodeCount; i++) {
                PulseChainNode node = _activeNodes[i];
                node.CurrentPosition = node.BasePosition;

                if (node.IsMoving) {
                    float yOffset = Mathf.Sin((time * node.MovementFrequency) + node.MovementPhase) * node.MovementAmplitude;
                    node.CurrentPosition += new Vector2(0.0f, yOffset);
                }

                _activeViews[i].SetPosition(node.CurrentPosition);
                _activeViews[i].UpdateVisuals(node, time, _settings.ZoneCycleSpeed);
            }

            UpdateConnections();
        }

        public PulseChainNode GetNextNode(PulseChainNode currentNode, bool chooseRiskPath, int score) {
            EnsureChildren(currentNode, score);
            if (chooseRiskPath && currentNode.RiskNext != null) {
                return currentNode.RiskNext;
            }

            if (currentNode.SafeNext == null) {
                EnsureChildren(currentNode, score);
            }

            return currentNode.SafeNext;
        }

        public float GetZoneWindowValue(PulseChainNode node, AcceptZoneState zone, float time) {
            float baseValue = (time * _settings.ZoneCycleSpeed * zone.Speed) + zone.Phase;
            if (zone.MotionType == AcceptZoneMotionType.Rotating) {
                return Mathf.Repeat(baseValue, 1.0f);
            }

            if (zone.MotionType == AcceptZoneMotionType.PingPong) {
                return Mathf.PingPong(baseValue, 1.0f);
            }

            return zone.Center;
        }

        private void CreateInitialChain() {
            PulseChainNode startNode = CreateNode(new Vector2(-420.0f, 0.0f), 0.0f, false, false, false);
            _activeNodes.Add(startNode);

            PulseChainNode previousNode = startNode;
            for (int i = 0; i < _settings.InitialNodesAhead; i++) {
                PulseChainNode nextNode = CreateSequentialNode(previousNode, false, i);
                previousNode.SafeNext = nextNode;
                _activeNodes.Add(nextNode);
                previousNode = nextNode;
            }
        }

        private void EnsureChildren(PulseChainNode currentNode, int score) {
            bool tutorialActive = GameManager.Instance != null && GameManager.Instance.IsTutorialActive;
            if (currentNode.SafeNext != null) {
                if (!tutorialActive && currentNode.IsBranchNode && currentNode.RiskNext == null) {
                    currentNode.RiskNext = CreateSequentialNode(currentNode, true, score);
                    _activeNodes.Add(currentNode.RiskNext);
                    RebuildViews();
                }

                return;
            }

            bool allowBranch = !tutorialActive && (score >= 25) && (currentNode.Id > 1);
            bool createBranch = allowBranch && (_random.NextDouble() <= _settings.BranchChance);
            currentNode.IsBranchNode = createBranch;
            currentNode.SafeNext = CreateSequentialNode(currentNode, false, score);
            _activeNodes.Add(currentNode.SafeNext);

            if (createBranch) {
                currentNode.RiskNext = CreateSequentialNode(currentNode, true, score);
                _activeNodes.Add(currentNode.RiskNext);
            }

            RebuildViews();
        }

        private PulseChainNode CreateSequentialNode(PulseChainNode parentNode, bool riskPath, int score) {
            bool tutorialActive = GameManager.Instance != null && GameManager.Instance.IsTutorialActive;
            float horizontalOffset = _settings.BaseNodeDistance + GetRandomRange(-_settings.NodeDistanceVariance, _settings.NodeDistanceVariance);
            if (tutorialActive) {
                horizontalOffset = _settings.BaseNodeDistance * 0.9f;
            }

            if (riskPath) {
                horizontalOffset += 40.0f;
            }

            float verticalOffset = riskPath ? 150.0f : GetRandomRange(-80.0f, 80.0f);
            if (tutorialActive) {
                verticalOffset = 0.0f;
            }

            Vector2 position = parentNode.BasePosition + new Vector2(horizontalOffset, verticalOffset);
            bool isMoving = !tutorialActive && score >= 60 && (_random.NextDouble() > 0.5d);
            bool hasFakeBranch = !tutorialActive && score >= 80 && (_random.NextDouble() > 0.55d);
            float difficultyFactor = tutorialActive ? 0.0f : Mathf.Clamp01(score / 180.0f);
            PulseChainNode node = CreateNode(position, difficultyFactor, isMoving, riskPath, hasFakeBranch);
            return node;
        }

        private PulseChainNode CreateNode(Vector2 position, float difficultyFactor, bool isMoving, bool riskPath, bool fakeBranch) {
            PulseChainNode node = new PulseChainNode();
            node.Id = _nextNodeId++;
            node.BasePosition = position;
            node.CurrentPosition = position;
            node.IsMoving = isMoving;
            node.MovementAmplitude = _settings.MovingNodeAmplitude;
            node.MovementFrequency = _settings.MovingNodeFrequency + (difficultyFactor * 0.75f);
            node.MovementPhase = (float)_random.NextDouble() * Mathf.PI * 2.0f;
            node.DifficultyFactor = difficultyFactor;
            node.IsRiskPath = riskPath;
            node.IsFakeBranch = fakeBranch;
            node.Zones = CreateZones(difficultyFactor, fakeBranch);
            node.CorrectZoneIndex = 0;
            return node;
        }

        private List<AcceptZoneState> CreateZones(float difficultyFactor, bool includeFakeZones) {
            List<AcceptZoneState> zones = new List<AcceptZoneState>(3);
            AcceptZoneState primaryZone = new AcceptZoneState();
            primaryZone.MotionType = difficultyFactor > 0.45f ? AcceptZoneMotionType.PingPong : AcceptZoneMotionType.Rotating;
            primaryZone.Center = 0.5f;
            primaryZone.Width = Mathf.Lerp(0.28f, 0.12f, difficultyFactor);
            primaryZone.Speed = Mathf.Lerp(0.8f, 1.35f, difficultyFactor);
            primaryZone.Phase = (float)_random.NextDouble();
            zones.Add(primaryZone);

            if (includeFakeZones) {
                AcceptZoneState fakeZone = new AcceptZoneState();
                fakeZone.MotionType = AcceptZoneMotionType.Rotating;
                fakeZone.Center = 0.25f;
                fakeZone.Width = Mathf.Lerp(0.16f, 0.08f, difficultyFactor);
                fakeZone.Speed = Mathf.Lerp(0.65f, 1.55f, difficultyFactor);
                fakeZone.Phase = (float)_random.NextDouble();
                zones.Add(fakeZone);
            }

            if (difficultyFactor > 0.7f) {
                AcceptZoneState secondFakeZone = new AcceptZoneState();
                secondFakeZone.MotionType = AcceptZoneMotionType.PingPong;
                secondFakeZone.Center = 0.75f;
                secondFakeZone.Width = 0.10f;
                secondFakeZone.Speed = 1.75f;
                secondFakeZone.Phase = (float)_random.NextDouble();
                zones.Add(secondFakeZone);
            }

            return zones;
        }

        private void RebuildViews() {
            ClearViews();

            int nodeCount = _activeNodes.Count;
            for (int i = 0; i < nodeCount; i++) {
                NodeView nodeView = _nodePool.Get();
                nodeView.transform.SetParent(_root, false);
                nodeView.Configure(_activeNodes[i]);
                nodeView.SetPosition(_activeNodes[i].CurrentPosition);
                _activeViews.Add(nodeView);
            }

            UpdateConnections();
        }

        private void UpdateConnections() {
            for (int i = 0; i < _activeConnections.Count; i++) {
                _connectionPool.Release(_activeConnections[i]);
            }

            _activeConnections.Clear();

            int nodeCount = _activeNodes.Count;
            for (int i = 0; i < nodeCount; i++) {
                PulseChainNode node = _activeNodes[i];
                if (node.SafeNext != null) {
                    ConnectionView safeConnection = _connectionPool.Get();
                    safeConnection.transform.SetParent(_root, false);
                    safeConnection.Draw(node.CurrentPosition, node.SafeNext.CurrentPosition, node.IsRiskPath ? new Color(0.7f, 1.0f, 0.9f, 0.55f) : new Color(0.55f, 0.9f, 1.0f, 0.35f));
                    _activeConnections.Add(safeConnection);
                }

                if (node.RiskNext != null) {
                    ConnectionView riskConnection = _connectionPool.Get();
                    riskConnection.transform.SetParent(_root, false);
                    riskConnection.Draw(node.CurrentPosition, node.RiskNext.CurrentPosition, new Color(1.0f, 0.5f, 0.35f, 0.45f));
                    _activeConnections.Add(riskConnection);
                }
            }
        }

        private void ClearViews() {
            for (int i = 0; i < _activeViews.Count; i++) {
                _nodePool.Release(_activeViews[i]);
            }

            _activeViews.Clear();

            for (int i = 0; i < _activeConnections.Count; i++) {
                _connectionPool.Release(_activeConnections[i]);
            }

            _activeConnections.Clear();
        }

        private NodeView CreateNodeView() {
            GameObject nodeObject = new GameObject("NodeView");
            NodeView nodeView = nodeObject.AddComponent<NodeView>();
            return nodeView;
        }

        private ConnectionView CreateConnectionView() {
            GameObject connectionObject = new GameObject("ConnectionView");
            ConnectionView connectionView = connectionObject.AddComponent<ConnectionView>();
            return connectionView;
        }

        private float GetRandomRange(float min, float max) {
            double sample = _random.NextDouble();
            return min + ((float)sample * (max - min));
        }
    }
}