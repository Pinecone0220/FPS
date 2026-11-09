using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FpsPractice.FPS
{
    public class AimLabSettingsHelper : MonoBehaviour
    {
        private const string MouseSensitivityKey = "Settings.MouseSensitivity";
        private const string FieldOfViewKey = "Settings.FieldOfView";

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private AimLabPlayerController aimLabPlayerController;
        [SerializeField] private AimLabModeManager aimLabModeManager;
        [SerializeField] private AimLabTargetSpawner targetSpawner;

        [Header("Pause Menu")]
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TMP_Text mouseSensitivityValueText;
        [SerializeField] private Slider fieldOfViewSlider;
        [SerializeField] private TMP_Text fieldOfViewValueText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private Button quitButton;

        [Header("Limits")]
        [SerializeField] private float mouseSensitivityMin = 0.1f;
        [SerializeField] private float mouseSensitivityMax = 10f;
        [SerializeField] private float fieldOfViewMin = 40f;
        [SerializeField] private float fieldOfViewMax = 110f;
        [SerializeField] private float defaultMouseSensitivity = 2f;
        [SerializeField] private float defaultFieldOfView = 60f;

        private readonly List<Behaviour> pauseTargets = new List<Behaviour>();
        private readonly List<bool> pauseTargetStates = new List<bool>();
        private bool isPaused;
        private bool isWiring;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            CacheReferences();
            AutoWireMenuControls();
            WireMenu();
            LoadSettings();
            ApplyPauseState(false, true);
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        private void Update()
        {
            if (isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void TogglePause()
        {
            ApplyPauseState(!isPaused);
        }

        public void SetPaused(bool paused)
        {
            ApplyPauseState(paused);
        }

        private void CacheReferences()
        {
            if (aimLabModeManager == null)
            {
                aimLabModeManager = GetComponentInChildren<AimLabModeManager>(true);
            }

            if (targetSpawner == null)
            {
                targetSpawner = GetComponentInChildren<AimLabTargetSpawner>(true);
            }

            if (playerLook == null)
            {
                playerLook = GetComponentInChildren<PlayerLook>(true);
            }

            if (aimLabPlayerController == null)
            {
                aimLabPlayerController = FindFirstObjectByType<AimLabPlayerController>();
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>(true);
            }

            if (playerLook != null && playerCamera != null)
            {
                playerLook.SetCamera(playerCamera);
            }
        }

        private void AutoWireMenuControls()
        {
            if (pauseMenuRoot == null)
            {
                return;
            }

            if (mouseSensitivitySlider == null || fieldOfViewSlider == null)
            {
                Slider[] sliders = pauseMenuRoot.GetComponentsInChildren<Slider>(true);
                if (mouseSensitivitySlider == null && sliders.Length > 0)
                {
                    mouseSensitivitySlider = sliders[0];
                }

                if (fieldOfViewSlider == null && sliders.Length > 1)
                {
                    fieldOfViewSlider = sliders[1];
                }
            }

            if (closeButton == null || resetDefaultsButton == null)
            {
                Button[] buttons = pauseMenuRoot.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    string buttonName = buttons[i].name.ToLowerInvariant();
                    if (resetDefaultsButton == null && buttonName.Contains("reset"))
                    {
                        resetDefaultsButton = buttons[i];
                        continue;
                    }

                    if (closeButton == null && buttonName.Contains("close"))
                    {
                        closeButton = buttons[i];
                    }
                }
            }
        }

        private void WireMenu()
        {
            if (isWiring)
            {
                return;
            }

            isWiring = true;

            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(false);
            }

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.minValue = mouseSensitivityMin;
                mouseSensitivitySlider.maxValue = mouseSensitivityMax;
                mouseSensitivitySlider.wholeNumbers = false;
                mouseSensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
                mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (fieldOfViewSlider != null)
            {
                fieldOfViewSlider.minValue = fieldOfViewMin;
                fieldOfViewSlider.maxValue = fieldOfViewMax;
                fieldOfViewSlider.wholeNumbers = false;
                fieldOfViewSlider.onValueChanged.RemoveListener(OnFieldOfViewChanged);
                fieldOfViewSlider.onValueChanged.AddListener(OnFieldOfViewChanged);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ResumeGame);
                closeButton.onClick.AddListener(ResumeGame);
            }

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.onClick.RemoveListener(ResetSettingsToDefault);
                resetDefaultsButton.onClick.AddListener(ResetSettingsToDefault);
            }
            if (quitButton == null && pauseMenuRoot != null)
            {
                Transform quit = pauseMenuRoot.transform.Find("QuitGame");
                if (quit != null) quitButton = quit.GetComponent<Button>();
            }
            if (quitButton == null)
            {
                quitButton = PlayerSetupHelper.CreateQuitButton(resetDefaultsButton);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
                quitButton.onClick.AddListener(QuitGame);
            }

            isWiring = false;
        }

        public void QuitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadSettings()
        {
            float sensitivityValue = PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);
            ApplySensitivity(sensitivityValue, false);

            float fovValue = PlayerPrefs.GetFloat(FieldOfViewKey, defaultFieldOfView);
            ApplyFieldOfView(fovValue, false);
        }

        private void OnSensitivityChanged(float value)
        {
            ApplySensitivity(value, true);
        }

        private void OnFieldOfViewChanged(float value)
        {
            ApplyFieldOfView(value, true);
        }

        private void ResumeGame()
        {
            ApplyPauseState(false);
        }

        private void ResetSettingsToDefault()
        {
            ApplySensitivity(defaultMouseSensitivity, true);
            ApplyFieldOfView(defaultFieldOfView, true);
        }

        private void ApplyPauseState(bool paused, bool force = false)
        {
            if (!force && isPaused == paused)
            {
                return;
            }

            isPaused = paused;

            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(paused);
            }

            if (aimLabModeManager != null)
            {
                aimLabModeManager.SetPaused(paused);
            }

            if (targetSpawner != null)
            {
                targetSpawner.SetPaused(paused);
            }

            Time.timeScale = paused ? 0f : 1f;

            if (playerLook != null)
            {
                playerLook.enabled = !paused;
                if (!paused)
                {
                    playerLook.SetCursorLocked(true);
                }
            }
            else
            {
                Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = paused;
            }

            if (paused)
            {
                CachePauseTargets();
                SetPauseTargetsEnabled(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            SetPauseTargetsEnabled(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void CachePauseTargets()
        {
            pauseTargets.Clear();
            pauseTargetStates.Clear();

            AddPauseTarget(playerLook);
        }

        private void AddPauseTarget(Behaviour behaviour)
        {
            if (behaviour == null || pauseTargets.Contains(behaviour))
            {
                return;
            }

            pauseTargets.Add(behaviour);
            pauseTargetStates.Add(behaviour.enabled);
        }

        private void SetPauseTargetsEnabled(bool enabled)
        {
            for (int i = 0; i < pauseTargets.Count; i++)
            {
                Behaviour behaviour = pauseTargets[i];
                if (behaviour == null)
                {
                    continue;
                }

                behaviour.enabled = enabled ? pauseTargetStates[i] : false;
            }
        }

        private void ApplySensitivity(float value, bool save)
        {
            float clampedValue = Mathf.Clamp(value, mouseSensitivityMin, mouseSensitivityMax);

            if (playerLook != null)
            {
                playerLook.SetMouseSensitivity(clampedValue);
            }

            if (aimLabPlayerController != null)
            {
                aimLabPlayerController.SetMouseSensitivity(clampedValue);
            }

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.SetValueWithoutNotify(clampedValue);
            }

            if (mouseSensitivityValueText != null)
            {
                mouseSensitivityValueText.SetText("{0:0.00}", clampedValue);
            }

            if (save)
            {
                PlayerPrefs.SetFloat(MouseSensitivityKey, clampedValue);
                PlayerPrefs.Save();
            }
        }

        private void ApplyFieldOfView(float value, bool save)
        {
            float clampedValue = Mathf.Clamp(value, fieldOfViewMin, fieldOfViewMax);

            if (playerLook != null)
            {
                playerLook.SetFieldOfView(clampedValue);
            }

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = clampedValue;
            }

            if (fieldOfViewSlider != null)
            {
                fieldOfViewSlider.SetValueWithoutNotify(clampedValue);
            }

            if (fieldOfViewValueText != null)
            {
                fieldOfViewValueText.SetText("{0:0.00}", clampedValue);
            }

            if (save)
            {
                PlayerPrefs.SetFloat(FieldOfViewKey, clampedValue);
                PlayerPrefs.Save();
            }
        }
    }
}
