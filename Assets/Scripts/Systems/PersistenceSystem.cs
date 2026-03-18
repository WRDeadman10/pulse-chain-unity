using System;

using UnityEngine;

namespace PulseChain.Gameplay {
    public sealed class PersistenceSystem : MonoBehaviour {
        private const string HighScoreKey = "PulseChain.HighScore";
        private const string DailyDateKey = "PulseChain.DailyDate";
        private const string DailyBestKey = "PulseChain.DailyBest";
        private const string TutorialCompletedKey = "PulseChain.TutorialCompleted";

        public int HighScore { get; private set; }

        public void Initialize() {
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            ResetDailyIfNeeded();
        }

        public int GetDailySeed() {
            DateTime utcNow = DateTime.UtcNow;
            int seed = (utcNow.Year * 1000) + utcNow.DayOfYear;
            return seed;
        }

        public int GetDailyBest() {
            ResetDailyIfNeeded();
            return PlayerPrefs.GetInt(DailyBestKey, 0);
        }

        public bool IsTutorialCompleted() {
            return PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
        }

        public void SetTutorialCompleted() {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }

        public void RegisterScore(int score) {
            if (score > HighScore) {
                HighScore = score;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
            }

            int dailyBest = GetDailyBest();
            if (score > dailyBest) {
                PlayerPrefs.SetInt(DailyBestKey, score);
            }

            PlayerPrefs.Save();
        }

        public LeaderboardPayload BuildLeaderboardPayload(string playerId, int score) {
            LeaderboardPayload payload = new LeaderboardPayload();
            payload.PlayerId = playerId;
            payload.Score = score;
            payload.Seed = GetDailySeed();
            payload.TimestampUtc = DateTime.UtcNow.ToString("O");
            return payload;
        }

        private void ResetDailyIfNeeded() {
            string currentDay = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string savedDay = PlayerPrefs.GetString(DailyDateKey, string.Empty);
            if (savedDay == currentDay) {
                return;
            }

            PlayerPrefs.SetString(DailyDateKey, currentDay);
            PlayerPrefs.SetInt(DailyBestKey, 0);
            PlayerPrefs.Save();
        }
    }

    [Serializable]
    public struct LeaderboardPayload {
        public string PlayerId;
        public int Score;
        public int Seed;
        public string TimestampUtc;
    }
}