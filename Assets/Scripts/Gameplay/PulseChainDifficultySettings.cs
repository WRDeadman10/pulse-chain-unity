using UnityEngine;

namespace PulseChain.Gameplay {
    [CreateAssetMenu(menuName = "Pulse Chain/Difficulty Settings", fileName = "PulseChainDifficultySettings")]
    public sealed class PulseChainDifficultySettings : ScriptableObject {
        [SerializeField] private int initialNodesAhead = 10;
        [SerializeField] private float baseNodeDistance = 220.0f;
        [SerializeField] private float nodeDistanceVariance = 80.0f;
        [SerializeField] private float basePulseSpeed = 340.0f;
        [SerializeField] private float pulseSpeedGrowthPerScore = 2.4f;
        [SerializeField] private float overloadScoreThreshold = 160.0f;
        [SerializeField] private float arrivalWindow = 0.20f;
        [SerializeField] private float perfectArrivalWindow = 0.07f;
        [SerializeField] private float zoneCycleSpeed = 0.85f;
        [SerializeField] private float movingNodeAmplitude = 45.0f;
        [SerializeField] private float movingNodeFrequency = 1.15f;
        [SerializeField] private float branchChance = 0.25f;
        [SerializeField] private float multiPulseScoreThreshold = 40.0f;
        [SerializeField] private float triplePulseScoreThreshold = 100.0f;
        [SerializeField] private float slowMotionScale = 0.45f;
        [SerializeField] private float energyDrainPerSecond = 0.32f;
        [SerializeField] private float energyRecoverPerSecond = 0.20f;

        public int InitialNodesAhead {
            get {
                return initialNodesAhead;
            }
        }

        public float BaseNodeDistance {
            get {
                return baseNodeDistance;
            }
        }

        public float NodeDistanceVariance {
            get {
                return nodeDistanceVariance;
            }
        }

        public float BasePulseSpeed {
            get {
                return basePulseSpeed;
            }
        }

        public float PulseSpeedGrowthPerScore {
            get {
                return pulseSpeedGrowthPerScore;
            }
        }

        public float OverloadScoreThreshold {
            get {
                return overloadScoreThreshold;
            }
        }

        public float ArrivalWindow {
            get {
                return arrivalWindow;
            }
        }

        public float PerfectArrivalWindow {
            get {
                return perfectArrivalWindow;
            }
        }

        public float ZoneCycleSpeed {
            get {
                return zoneCycleSpeed;
            }
        }

        public float MovingNodeAmplitude {
            get {
                return movingNodeAmplitude;
            }
        }

        public float MovingNodeFrequency {
            get {
                return movingNodeFrequency;
            }
        }

        public float BranchChance {
            get {
                return branchChance;
            }
        }

        public float MultiPulseScoreThreshold {
            get {
                return multiPulseScoreThreshold;
            }
        }

        public float TriplePulseScoreThreshold {
            get {
                return triplePulseScoreThreshold;
            }
        }

        public float SlowMotionScale {
            get {
                return slowMotionScale;
            }
        }

        public float EnergyDrainPerSecond {
            get {
                return energyDrainPerSecond;
            }
        }

        public float EnergyRecoverPerSecond {
            get {
                return energyRecoverPerSecond;
            }
        }

        public static PulseChainDifficultySettings CreateRuntimeInstance() {
            PulseChainDifficultySettings settings = CreateInstance<PulseChainDifficultySettings>();
            settings.hideFlags = HideFlags.HideAndDontSave;
            return settings;
        }
    }

    [CreateAssetMenu(menuName = "Pulse Chain/Pulse Style Config", fileName = "PulseStyleConfig")]
    public sealed class PulseStyleConfig : ScriptableObject {
        [SerializeField] private PulseVisualStyle defaultStyle = PulseVisualStyle.Round;
        [SerializeField] private float baseSize = 30.0f;
        [SerializeField] private float zigZagAmplitude = 18.0f;

        public PulseVisualStyle DefaultStyle {
            get {
                return defaultStyle;
            }
        }

        public float BaseSize {
            get {
                return baseSize;
            }
        }

        public float ZigZagAmplitude {
            get {
                return zigZagAmplitude;
            }
        }

        public static PulseStyleConfig CreateRuntimeInstance() {
            PulseStyleConfig config = CreateInstance<PulseStyleConfig>();
            config.hideFlags = HideFlags.HideAndDontSave;
            return config;
        }
    }

    public enum PulseVisualStyle {
        Round = 0,
        Square = 1,
        ZigZag = 2,
        Ghost = 3
    }
}