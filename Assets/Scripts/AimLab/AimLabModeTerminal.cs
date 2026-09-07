using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FpsPractice.FPS
{
    public class AimLabModeTerminal : MonoBehaviour
    {
        private const float MinSessionTime = 10f;
        private const float MaxSessionTime = 30f;
        private const float SessionTimeStep = 5f;
        private const string RankPrefsKey = "AimLab_BestRank";

        [SerializeField] private AimLabModeManager modeManager;
        [SerializeField] private GameObject stagePanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button rankButton;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode abortKey = KeyCode.Escape;
        [SerializeField] private string lobbySceneName = "Lobby";

        [Header("HUD")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text abortHintText;
        [SerializeField] private TMP_Text sessionTimeText;
        [SerializeField] private Button increaseTimeButton;
        [SerializeField] private Button decreaseTimeButton;
        [SerializeField, Min(0f)] private float startCountdownSeconds = 5f;

        [Header("Results")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultDetailText;
        [SerializeField] private Button resultCloseButton;

        [Header("Rank")]
        [SerializeField] private GameObject rankPanel;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Button rankCloseButton;

        private bool playerInside;
        private bool isPanelOpen;
        private bool isResultOpen;
        private bool isRankOpen;
        private bool isWiring;
        private bool isResultWiring;
        private bool isRankWiring;
        private bool isStartCountdownActive;
        private bool isAbortingSession;
        private float selectedSessionTime = MinSessionTime;
        private float startCountdownRemaining;
        private Coroutine startCountdownRoutine;

        private void Awake()
        {
            AutoWireReferences();
            WireButtons();
            WireResultButtons();
            WireRankButtons();
            InitializeSelectedSessionTime();
            UpdateSessionTimeUI();
            RefreshRankText();
            UpdateAbortHintText();
            SetPanelVisible(false);
            SetResultVisible(false);
            SetRankVisible(false);
            SetPromptVisible(false);
            SetAbortHintVisible(false);
            SetTimerVisible(false);
            SubscribeToResults();
        }

        private void OnEnable()
        {
            SubscribeToResults();
        }

        private void OnDisable()
        {
            UnsubscribeFromResults();
        }

        private void Update()
        {
            if (isPanelOpen || isRankOpen || isResultOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            UpdateTimerDisplay();
            UpdateAbortHintText();

            if (isResultOpen)
            {
                return;
            }

            if (isRankOpen)
            {
                if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseRankPanel();
                }

                return;
            }

            if (!playerInside || isPanelOpen)
            {
                return;
            }

            if ((isStartCountdownActive || (modeManager != null && modeManager.IsRunning)) && Input.GetKeyDown(abortKey))
            {
                AbortCurrentSession();
                return;
            }

            if (isStartCountdownActive)
            {
                return;
            }

            if (Input.GetKeyDown(interactKey))
            {
                OpenPanel();
            }
        }

        public void NotifyPlayerEnter(Collider other)
        {
            if (other.GetComponentInParent<AimLabPlayerController>() == null)
            {
                return;
            }

            playerInside = true;
            if (!isPanelOpen && !isResultOpen && !isStartCountdownActive && !isRankOpen)
            {
                SetPromptVisible(true);
            }
        }

        public void NotifyPlayerExit(Collider other)
        {
            if (other.GetComponentInParent<AimLabPlayerController>() == null)
            {
                return;
            }

            playerInside = false;
            SetPromptVisible(false);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<AimLabPlayerController>() == null)
            {
                return;
            }

            playerInside = false;
            SetPromptVisible(false);
        }

        private void AutoWireReferences()
        {
            if (stagePanel == null)
            {
                GameObject foundPanel = GameObject.Find("Stage_Panel");
                if (foundPanel != null)
                {
                    stagePanel = foundPanel;
                }
            }

            if (rankPanel == null)
            {
                GameObject foundRankPanel = GameObject.Find("Rank_Panel");
                if (foundRankPanel != null)
                {
                    rankPanel = foundRankPanel;
                }
            }

            if (stagePanel != null)
            {
                Button[] buttons = stagePanel.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    string buttonName = buttons[i].name.ToLowerInvariant();
                    if (startButton == null && buttonName.Contains("aimlab"))
                    {
                        startButton = buttons[i];
                        continue;
                    }

                    if (exitButton == null && buttonName.Contains("fps"))
                    {
                        exitButton = buttons[i];
                        continue;
                    }

                    if (closeButton == null && buttonName.Contains("close"))
                    {
                        closeButton = buttons[i];
                        continue;
                    }

                    if (rankButton == null && buttonName.Contains("rank"))
                    {
                        rankButton = buttons[i];
                    }
                }
            }

            if (rankPanel != null && rankCloseButton == null)
            {
                Button[] buttons = rankPanel.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].name.ToLowerInvariant().Contains("close"))
                    {
                        rankCloseButton = buttons[i];
                        break;
                    }
                }
            }
        }

        private void SubscribeToResults()
        {
            if (modeManager == null)
            {
                modeManager = FindFirstObjectByType<AimLabModeManager>(FindObjectsInactive.Include);
            }

            if (modeManager != null)
            {
                modeManager.SessionFinished -= HandleSessionFinished;
                modeManager.SessionFinished += HandleSessionFinished;
            }
        }

        private void UnsubscribeFromResults()
        {
            if (modeManager != null)
            {
                modeManager.SessionFinished -= HandleSessionFinished;
            }
        }

        private void WireButtons()
        {
            if (isWiring)
            {
                return;
            }

            isWiring = true;

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
                exitButton.onClick.AddListener(OnExitClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (rankButton != null)
            {
                rankButton.onClick.RemoveListener(OpenRankPanel);
                rankButton.onClick.AddListener(OpenRankPanel);
            }

            if (increaseTimeButton != null)
            {
                increaseTimeButton.onClick.RemoveListener(IncreaseSessionTime);
                increaseTimeButton.onClick.AddListener(IncreaseSessionTime);
            }

            if (decreaseTimeButton != null)
            {
                decreaseTimeButton.onClick.RemoveListener(DecreaseSessionTime);
                decreaseTimeButton.onClick.AddListener(DecreaseSessionTime);
            }

            isWiring = false;
        }

        private void WireResultButtons()
        {
            if (isResultWiring)
            {
                return;
            }

            isResultWiring = true;

            if (resultCloseButton != null)
            {
                resultCloseButton.onClick.RemoveListener(CloseResultPanel);
                resultCloseButton.onClick.AddListener(CloseResultPanel);
            }

            isResultWiring = false;
        }

        private void WireRankButtons()
        {
            if (isRankWiring)
            {
                return;
            }

            isRankWiring = true;

            if (rankCloseButton != null)
            {
                rankCloseButton.onClick.RemoveListener(CloseRankPanel);
                rankCloseButton.onClick.AddListener(CloseRankPanel);
            }

            isRankWiring = false;
        }

        private void InitializeSelectedSessionTime()
        {
            float initialTime = modeManager != null ? modeManager.SessionTimeLimit : MinSessionTime;
            selectedSessionTime = Mathf.Clamp(initialTime, MinSessionTime, MaxSessionTime);
        }

        private void IncreaseSessionTime()
        {
            SetSelectedSessionTime(selectedSessionTime + SessionTimeStep);
        }

        private void DecreaseSessionTime()
        {
            SetSelectedSessionTime(selectedSessionTime - SessionTimeStep);
        }

        private void SetSelectedSessionTime(float value)
        {
            selectedSessionTime = Mathf.Clamp(value, MinSessionTime, MaxSessionTime);
            UpdateSessionTimeUI();
        }

        private void UpdateSessionTimeUI()
        {
            if (sessionTimeText != null)
            {
                sessionTimeText.text = $"{selectedSessionTime:0}s";
            }

            if (increaseTimeButton != null)
            {
                increaseTimeButton.interactable = selectedSessionTime < MaxSessionTime;
            }

            if (decreaseTimeButton != null)
            {
                decreaseTimeButton.interactable = selectedSessionTime > MinSessionTime;
            }
        }

        private void OpenPanel()
        {
            isPanelOpen = true;
            SetPanelVisible(true);
            SetResultVisible(false);
            SetRankVisible(false);
            SetTimerVisible(false);
            SetAbortHintVisible(false);
            UpdateSessionTimeUI();
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPromptVisible(false);
        }

        private void ClosePanel()
        {
            isPanelOpen = false;
            SetPanelVisible(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (playerInside)
            {
                SetPromptVisible(true);
            }
            UpdateAbortHintText();
        }

        private void OpenRankPanel()
        {
            if (isRankOpen)
            {
                return;
            }

            isRankOpen = true;
            isPanelOpen = true;
            SetPanelVisible(true);
            SetResultVisible(false);
            SetRankVisible(true);
            RefreshRankText();
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPromptVisible(false);
        }

        private void CloseRankPanel()
        {
            isRankOpen = false;
            SetRankVisible(false);
            SetPanelVisible(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPromptVisible(false);
            UpdateSessionTimeUI();
        }

        private void HandleSessionFinished(AimLabSessionResult result)
        {
            isPanelOpen = false;
            isResultOpen = true;
            SetPanelVisible(false);
            SetResultVisible(true);
            SetRankVisible(false);
            SetPromptVisible(false);
            SetAbortHintVisible(false);
            SetTimerVisible(false);
            RecordBestRank(result);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UpdateResultText(result);
        }

        private void OnStartClicked()
        {
            if (modeManager == null || isStartCountdownActive)
            {
                return;
            }

            modeManager.SetSessionTimeLimit(selectedSessionTime);
            modeManager.PrepareSession();

            if (startCountdownRoutine != null)
            {
                StopCoroutine(startCountdownRoutine);
            }

            startCountdownRoutine = StartCoroutine(BeginStartCountdown());
        }

        private IEnumerator BeginStartCountdown()
        {
            isPanelOpen = false;
            SetPanelVisible(false);
            SetResultVisible(false);
            SetRankVisible(false);
            SetPromptVisible(false);
            isStartCountdownActive = true;
            startCountdownRemaining = Mathf.Max(0f, startCountdownSeconds);

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SetTimerVisible(true);
            SetAbortHintVisible(true);

            while (startCountdownRemaining > 0f)
            {
                yield return null;
                startCountdownRemaining -= Time.unscaledDeltaTime;
            }

            startCountdownRemaining = 0f;
            isStartCountdownActive = false;
            SetTimerVisible(false);
            SetAbortHintVisible(false);
            Time.timeScale = 1f;

            if (modeManager != null)
            {
                modeManager.StartCurrentMode();
            }

            startCountdownRoutine = null;
        }

        private void AbortCurrentSession()
        {
            if (isAbortingSession || modeManager == null)
            {
                return;
            }

            isAbortingSession = true;

            if (startCountdownRoutine != null)
            {
                StopCoroutine(startCountdownRoutine);
                startCountdownRoutine = null;
            }

            isStartCountdownActive = false;
            isPanelOpen = false;
            SetPanelVisible(false);
            SetResultVisible(false);
            SetRankVisible(false);
            SetTimerVisible(false);
            SetAbortHintVisible(false);

            modeManager.StopMode();

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerInside)
            {
                SetPromptVisible(true);
            }

            isAbortingSession = false;
        }

        private void OnExitClicked()
        {
            if (startCountdownRoutine != null)
            {
                StopCoroutine(startCountdownRoutine);
                startCountdownRoutine = null;
            }

            isStartCountdownActive = false;
            SetTimerVisible(false);
            SetAbortHintVisible(false);

            if (modeManager != null)
            {
                modeManager.StopMode();
            }

            Time.timeScale = 1f;

            if (!string.IsNullOrWhiteSpace(lobbySceneName))
            {
                SceneManager.LoadScene(lobbySceneName);
                return;
            }

            ClosePanel();
        }

        private void CloseResultPanel()
        {
            isResultOpen = false;
            SetResultVisible(false);
            SetTimerVisible(false);
            SetAbortHintVisible(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (playerInside)
            {
                SetPromptVisible(true);
            }
        }

        private void UpdateResultText(AimLabSessionResult result)
        {
            if (resultTitleText != null)
            {
                resultTitleText.text = "RESULT";
            }

            if (resultDetailText != null)
            {
                resultDetailText.text = $"Time: {result.TimeLimit:0.0}s\nShots: {result.ShotsFired} | Hits: {result.Hits} | Missed Targets: {result.MissedTargets}\nMissed Shots: {result.MissedShots} | Total Targets: {result.TotalTargets}\nAccuracy: {result.AccuracyPercent:0.0}% | Avg Reaction: {result.AverageReactionTimeMs:0.0} ms";
            }
        }

        private void RecordBestRank(AimLabSessionResult result)
        {
            RankEntry currentBest = LoadBestRank();
            RankEntry candidate = new RankEntry
            {
                timeLimit = result.TimeLimit,
                shotsFired = result.ShotsFired,
                hits = result.Hits,
                missedTargets = result.MissedTargets,
                missedShots = result.MissedShots,
                totalTargets = result.TotalTargets,
                accuracyPercent = result.AccuracyPercent,
                averageReactionTimeMs = result.AverageReactionTimeMs,
                recordedAtUtcTicks = System.DateTime.UtcNow.Ticks
            };

            if (currentBest != null && !IsBetterRank(candidate, currentBest))
            {
                RefreshRankText(currentBest);
                return;
            }

            SaveBestRank(candidate);
            RefreshRankText(candidate);
        }

        private void RefreshRankText()
        {
            RefreshRankText(LoadBestRank());
        }

        private void RefreshRankText(RankEntry entry)
        {
            if (rankText == null)
            {
                return;
            }

            if (entry == null)
            {
                rankText.text = "No Rank Yet";
                return;
            }

            rankText.text = $"1. {entry.accuracyPercent:0.0}% | Hits {entry.hits} | {entry.averageReactionTimeMs:0} ms\n    Time {entry.timeLimit:0.0}s | Shots {entry.shotsFired} | Missed {entry.missedShots}";
        }

        private RankEntry LoadBestRank()
        {
            if (!PlayerPrefs.HasKey(RankPrefsKey))
            {
                return null;
            }

            string json = PlayerPrefs.GetString(RankPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<RankEntry>(json);
        }

        private void SaveBestRank(RankEntry entry)
        {
            string json = JsonUtility.ToJson(entry);
            PlayerPrefs.SetString(RankPrefsKey, json);
            PlayerPrefs.Save();
        }

        private static bool IsBetterRank(RankEntry candidate, RankEntry currentBest)
        {
            if (candidate.accuracyPercent != currentBest.accuracyPercent)
            {
                return candidate.accuracyPercent > currentBest.accuracyPercent;
            }

            if (candidate.hits != currentBest.hits)
            {
                return candidate.hits > currentBest.hits;
            }

            if (candidate.averageReactionTimeMs != currentBest.averageReactionTimeMs)
            {
                return candidate.averageReactionTimeMs < currentBest.averageReactionTimeMs;
            }

            return candidate.totalTargets > currentBest.totalTargets;
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null)
            {
                return;
            }

            if (isStartCountdownActive)
            {
                SetTimerVisible(true);
                timerText.text = $"{Mathf.Max(0f, startCountdownRemaining):0.0}s";
                return;
            }

            if (modeManager == null)
            {
                SetTimerVisible(false);
                return;
            }

            bool shouldShow = modeManager.IsRunning && !isResultOpen && !isPanelOpen && !isRankOpen;
            SetTimerVisible(shouldShow);

            if (!shouldShow)
            {
                return;
            }

            timerText.text = $"{Mathf.Max(0f, modeManager.TimeRemaining):0.0}s";
        }

        private void UpdateAbortHintText()
        {
            if (abortHintText == null)
            {
                return;
            }

            if (isStartCountdownActive || (modeManager != null && modeManager.IsRunning))
            {
                abortHintText.text = $"Press {abortKey} To End";
                SetAbortHintVisible(true);
            }
            else
            {
                SetAbortHintVisible(false);
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (stagePanel != null)
            {
                stagePanel.SetActive(visible);
            }
        }

        private void SetResultVisible(bool visible)
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(visible);
            }
        }

        private void SetRankVisible(bool visible)
        {
            if (rankPanel != null)
            {
                rankPanel.SetActive(visible);
            }

            if (rankCloseButton != null)
            {
                rankCloseButton.gameObject.SetActive(visible);
            }
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(visible);
            }
        }

        private void SetTimerVisible(bool visible)
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(visible);
            }
        }

        private void SetAbortHintVisible(bool visible)
        {
            if (abortHintText != null)
            {
                abortHintText.gameObject.SetActive(visible);
            }
        }

        [System.Serializable]
        private class RankEntry
        {
            public float timeLimit;
            public int shotsFired;
            public int hits;
            public int missedTargets;
            public int missedShots;
            public int totalTargets;
            public float accuracyPercent;
            public float averageReactionTimeMs;
            public long recordedAtUtcTicks;
        }
    }
}