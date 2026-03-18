using System.Collections.Generic;

using UnityEngine;

namespace PulseChain.Gameplay {
    public sealed class PulseChainNode {
        public int Id;
        public Vector2 BasePosition;
        public Vector2 CurrentPosition;
        public bool IsMoving;
        public float MovementAmplitude;
        public float MovementFrequency;
        public float MovementPhase;
        public float DifficultyFactor;
        public int CorrectZoneIndex;
        public List<AcceptZoneState> Zones;
        public PulseChainNode SafeNext;
        public PulseChainNode RiskNext;
        public bool IsBranchNode;
        public bool IsRiskPath;
        public bool IsFakeBranch;
    }

    public sealed class AcceptZoneState {
        public AcceptZoneMotionType MotionType;
        public float Center;
        public float Width;
        public float Speed;
        public float Phase;
    }

    public enum AcceptZoneMotionType {
        Rotating = 0,
        PingPong = 1,
        Static = 2
    }

    public struct TapEvaluation {
        public readonly bool IsSuccess;
        public readonly bool IsPerfect;
        public readonly bool NearMiss;
        public readonly bool CanGhostRevive;

        public TapEvaluation(bool isSuccess, bool isPerfect, bool nearMiss, bool canGhostRevive) {
            IsSuccess = isSuccess;
            IsPerfect = isPerfect;
            NearMiss = nearMiss;
            CanGhostRevive = canGhostRevive;
        }
    }
}