using UnityEngine;

namespace FpsPractice.FPS
{
    public readonly struct AimLabSessionResult
    {
        public readonly float TimeLimit;
        public readonly int ShotsFired;
        public readonly int Hits;
        public readonly int MissedTargets;
        public readonly int MissedShots;
        public readonly int TotalTargets;
        public readonly float AverageReactionTimeMs;

        public AimLabSessionResult(float timeLimit, int shotsFired, int hits, int missedTargets, int missedShots, int totalTargets, float averageReactionTimeMs)
        {
            TimeLimit = Mathf.Max(0f, timeLimit);
            ShotsFired = Mathf.Max(0, shotsFired);
            Hits = Mathf.Max(0, hits);
            MissedTargets = Mathf.Max(0, missedTargets);
            MissedShots = Mathf.Max(0, missedShots);
            TotalTargets = Mathf.Max(0, totalTargets);
            AverageReactionTimeMs = Mathf.Max(0f, averageReactionTimeMs);
        }

        public float AccuracyPercent => TotalTargets <= 0 ? 0f : (float)Hits / TotalTargets * 100f;
    }
}
