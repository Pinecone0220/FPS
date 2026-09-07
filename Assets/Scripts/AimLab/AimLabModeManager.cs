using System;
using UnityEngine;

namespace FpsPractice.FPS
{
    public class AimLabModeManager : MonoBehaviour
    {
        public enum AimLabMode
        {
            ReactionSpeed,
            SprayControl
        }

        [Header("References")]
        [SerializeField] private AimLabPlayerController aimLabPlayer;
        [SerializeField] private AimLabTargetSpawner targetSpawner;
        [SerializeField] private GameObject[] weaponModels = new GameObject[2];

        [Header("Mode")]
        [SerializeField] private AimLabMode defaultMode = AimLabMode.ReactionSpeed;
        [SerializeField] private int defaultWeaponIndex = 0;
        [SerializeField, Range(10f, 30f)] private float sessionTimeLimit = 10f;

        public bool IsRunning { get; private set; }
        public AimLabMode CurrentMode { get; private set; }
        public int CurrentWeaponIndex { get; private set; }
        public float TimeRemaining => timeRemaining;
        public float SessionTimeLimit => sessionTimeLimit;
        public int ShotsFired => shotsFired;
        public int Hits => hits;
        public int MissedTargets => missedTargets;
        public int TotalTargets => totalTargets;
        public int MissedShots => Mathf.Max(0, shotsFired - hits);
        public float AverageReactionTimeMs => hits <= 0 ? 0f : totalReactionTimeMs / hits;
        public event Action<AimLabSessionResult> SessionFinished;

        private float timeRemaining;
        private int shotsFired;
        private int hits;
        private int missedTargets;
        private int totalTargets;
        private float totalReactionTimeMs;
        private bool sessionPrepared;
        private bool isPaused;

        private void Awake()
        {
            if (aimLabPlayer == null)
            {
                aimLabPlayer = GetComponentInChildren<AimLabPlayerController>(true);
            }

            if (targetSpawner == null)
            {
                targetSpawner = FindFirstObjectByType<AimLabTargetSpawner>(FindObjectsInactive.Include);
            }

            if (targetSpawner == null)
            {
                targetSpawner = GetComponentInChildren<AimLabTargetSpawner>(true);
            }

            SelectWeapon(defaultWeaponIndex);
            ResetSessionStats();
            IsRunning = false;
            CurrentMode = defaultMode;
        }

        private void Update()
        {
            if (!IsRunning || isPaused)
            {
                return;
            }

            if (sessionTimeLimit <= 0f)
            {
                FinishSession();
                return;
            }

            timeRemaining -= Time.unscaledDeltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                FinishSession();
            }
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }


        public void SetSessionTimeLimit(float value)
        {
            sessionTimeLimit = Mathf.Clamp(value, 10f, 30f);
        }

        public void StartCurrentMode()
        {
            StartMode(defaultMode);
        }

        public void StartMode(AimLabMode mode)
        {
            CurrentMode = mode;
            ResetSessionStats();
            timeRemaining = sessionTimeLimit;
            IsRunning = true;

            if (!sessionPrepared && aimLabPlayer != null)
            {
                aimLabPlayer.BeginSession();
            }

            sessionPrepared = false;

            if (targetSpawner != null)
            {
                targetSpawner.BeginPractice();
            }

            switch (mode)
            {
                case AimLabMode.ReactionSpeed:
                    SelectWeapon(0);
                    break;
                case AimLabMode.SprayControl:
                    SelectWeapon(1);
                    break;
            }
        }

        public void PrepareSession()
        {
            sessionPrepared = true;

            if (aimLabPlayer != null)
            {
                aimLabPlayer.BeginSession();
            }
        }

        public void StopMode()
        {
            IsRunning = false;

            if (targetSpawner != null)
            {
                targetSpawner.StopPractice();
            }

            if (aimLabPlayer != null)
            {
                aimLabPlayer.EndSession();
            }
        }

        public void ReportShotFired()
        {
            if (!IsRunning)
            {
                return;
            }

            shotsFired++;
        }

        public void ReportTargetSpawned()
        {
            if (!IsRunning)
            {
                return;
            }

            totalTargets++;
        }

        public void ReportTargetMissed()
        {
            if (!IsRunning)
            {
                return;
            }

            missedTargets++;
        }

        public void ReportHit(float reactionTimeSeconds)
        {
            if (!IsRunning)
            {
                return;
            }

            hits++;
            totalReactionTimeMs += Mathf.Max(0f, reactionTimeSeconds) * 1000f;
        }

        public void ReportHit()
        {
            ReportHit(0f);
        }

        public void SelectWeapon(int index)
        {
            if (weaponModels == null || weaponModels.Length == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, weaponModels.Length - 1);
            CurrentWeaponIndex = index;

            for (int i = 0; i < weaponModels.Length; i++)
            {
                if (weaponModels[i] != null)
                {
                    weaponModels[i].SetActive(i == index);
                }
            }
        }

        private void FinishSession()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;

            if (targetSpawner != null)
            {
                targetSpawner.StopPractice();
            }

            if (aimLabPlayer != null)
            {
                aimLabPlayer.EndSession();
            }

            SessionFinished?.Invoke(new AimLabSessionResult(sessionTimeLimit, shotsFired, hits, missedTargets, MissedShots, totalTargets, AverageReactionTimeMs));
        }

        private void ResetSessionStats()
        {
            timeRemaining = sessionTimeLimit;
            shotsFired = 0;
            hits = 0;
            missedTargets = 0;
            totalTargets = 0;
            totalReactionTimeMs = 0f;
            sessionPrepared = false;
            isPaused = false;
        }
    }
}
