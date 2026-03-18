using UnityEngine;

namespace PulseChain.Gameplay {
    public sealed class AdHooksSystem : MonoBehaviour {
        private bool _interstitialPending;

        public bool InterstitialPending {
            get {
                return _interstitialPending;
            }
        }

        public void Initialize(object owner) {
            _interstitialPending = false;
        }

        public bool CanOfferRevive(int runCount) {
            return runCount > 0;
        }

        public bool TryReviveLastRun() {
            Debug.Log("Rewarded revive hook triggered. Connect ad SDK here.");
            return false;
        }

        public bool ShouldShowInterstitial(int runCount) {
            return runCount > 0 && (runCount % 3 == 0);
        }

        public void MarkInterstitialOpportunity() {
            _interstitialPending = true;
            Debug.Log("Interstitial hook ready. Connect ad SDK here.");
        }
    }
}