using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace FpsPractice.FPS
{
    public class PlayerSetupHelper : MonoBehaviour
    {
        private const string MouseSensitivityKey = "Settings.MouseSensitivity";
        private const string FieldOfViewKey = "Settings.FieldOfView";
        private const string CrouchKeySettingKey = "Settings.CrouchKey";

        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Weapon weapon;
        [SerializeField] private WeaponLoadout weaponLoadout;

        [Header("Pause Menu")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TMP_Text mouseSensitivityValueText;
        [SerializeField] private Slider fieldOfViewSlider;
        [SerializeField] private TMP_Text fieldOfViewValueText;
        [SerializeField] private Button crouchKeyButton;
        [SerializeField] private TMP_Text crouchKeyButtonText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private float mouseSensitivityMin = 0.1f;
        [SerializeField] private float mouseSensitivityMax = 10f;
        [SerializeField] private float fieldOfViewMin = 40f;
        [SerializeField] private float fieldOfViewMax = 110f;
        [SerializeField] private float defaultMouseSensitivity = 2f;
        [SerializeField] private float defaultFieldOfView = 60f;
        [SerializeField] private KeyCode defaultCrouchKey = KeyCode.LeftControl;

        private readonly List<Behaviour> pauseTargets = new List<Behaviour>();
        private readonly List<bool> pauseTargetStates = new List<bool>();
        private bool isPaused;
        private bool isWiring;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
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

            if (Input.GetKeyDown(pauseKey))
            {
                ApplyPauseState(!isPaused);
            }
        }

        private void CacheReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerLook == null)
            {
                playerLook = GetComponentInChildren<PlayerLook>();
            }

            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }

            if (weapon == null)
            {
                weapon = GetComponentInChildren<Weapon>();
            }

            if (weaponLoadout == null)
            {
                weaponLoadout = GetComponent<WeaponLoadout>();
            }

            if (playerLook != null && playerCamera != null)
            {
                playerLook.SetCamera(playerCamera);
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

            if (crouchKeyButton != null)
            {
                crouchKeyButton.onClick.RemoveListener(ToggleCrouchKey);
                crouchKeyButton.onClick.AddListener(ToggleCrouchKey);
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

            if (lobbyButton != null)
            {
                lobbyButton.onClick.RemoveListener(LoadLobbyScene);
                lobbyButton.onClick.AddListener(LoadLobbyScene);
            }
            if (quitButton == null && pauseMenuRoot != null)
            {
                Transform quit = pauseMenuRoot.transform.Find("QuitGame");
                if (quit != null) quitButton = quit.GetComponent<Button>();
            }
            if (quitButton == null)
            {
                quitButton = CreateQuitButton(resetDefaultsButton);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
                quitButton.onClick.AddListener(QuitGame);
            }
            UpdateCrouchKeyLabel();
            isWiring = false;
        }

        private void LoadSettings()
        {
            float sensitivityValue = defaultMouseSensitivity;
            sensitivityValue = PlayerPrefs.GetFloat(MouseSensitivityKey, sensitivityValue);
            ApplySensitivity(sensitivityValue, false);

            float fovValue = defaultFieldOfView;
            fovValue = PlayerPrefs.GetFloat(FieldOfViewKey, fovValue);
            ApplyFieldOfView(fovValue, false);

            KeyCode crouchKey = LoadCrouchKey();
            if (playerController != null)
            {
                playerController.SetCrouchKey(crouchKey);
            }

            UpdateCrouchKeyLabel();
        }

        private void OnSensitivityChanged(float value)
        {
            ApplySensitivity(value, true);
        }

        private void OnFieldOfViewChanged(float value)
        {
            ApplyFieldOfView(value, true);
        }

        private void UpdateSensitivityLabel(float value)
        {
            if (mouseSensitivityValueText != null)
            {
                mouseSensitivityValueText.SetText("{0:0.00}", value);
            }
        }

        private void UpdateFieldOfViewLabel(float value)
        {
            if (fieldOfViewValueText != null)
            {
                fieldOfViewValueText.SetText("{0:0.00}", value);
            }
        }

        private void ToggleCrouchKey()
        {
            if (playerController == null)
            {
                return;
            }

            KeyCode nextKey = playerController.GetCrouchKey() == KeyCode.LeftControl ? KeyCode.C : KeyCode.LeftControl;
            ApplyCrouchKey(nextKey, true);
        }

        private void UpdateCrouchKeyLabel()
        {
            if (crouchKeyButtonText == null || playerController == null)
            {
                return;
            }

            crouchKeyButtonText.SetText(playerController.GetCrouchKey() == KeyCode.LeftControl ? "Ctrl" : "C");
        }

        private void ResumeGame()
        {
            ApplyPauseState(false);
        }

        private void ResetSettingsToDefault()
        {
            ApplySensitivity(defaultMouseSensitivity, true);
            ApplyFieldOfView(defaultFieldOfView, true);
            ApplyCrouchKey(defaultCrouchKey, true);
        }

        public static Button CreateQuitButton(Button template)
        {
            if (template == null)
            {
                return null;
            }

            Button button = Instantiate(template, template.transform.parent);
            button.name = "QuitGame";
            button.onClick = new Button.ButtonClickedEvent();
            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(0f, -455f);
            }
            TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (tmpLabel != null) tmpLabel.text = "Quit Game";
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "Quit Game";
            return button;
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

        public void LoadLobbyScene()
        {
            ApplyPauseState(false, true);
            SceneManager.LoadScene("Lobby");
        }
        public void SetPaused(bool paused)
        {
            ApplyPauseState(paused);
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

            Time.timeScale = paused ? 0f : 1f;

            if (paused)
            {
                CachePauseTargets();
                SetPauseTargetsEnabled(false);
                SetWeaponsInputPaused(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            SetWeaponsInputPaused(false);
            SetPauseTargetsEnabled(true);

            if (playerLook != null)
            {
                playerLook.SetCursorLocked(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void CachePauseTargets()
        {
            pauseTargets.Clear();
            pauseTargetStates.Clear();

            AddPauseTarget(playerLook);
            AddPauseTarget(playerController);
            AddPauseTarget(weaponLoadout);
        }

        private void SetWeaponsInputPaused(bool paused)
        {
            Weapon[] weapons = GetComponentsInChildren<Weapon>(true);
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                {
                    weapons[i].SetInputPaused(paused);
                }
            }
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

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.SetValueWithoutNotify(clampedValue);
            }

            UpdateSensitivityLabel(clampedValue);

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

            UpdateFieldOfViewLabel(clampedValue);

            if (save)
            {
                PlayerPrefs.SetFloat(FieldOfViewKey, clampedValue);
                PlayerPrefs.Save();
            }
        }

        private void ApplyCrouchKey(KeyCode keyCode, bool save)
        {
            if (playerController != null)
            {
                playerController.SetCrouchKey(keyCode);
            }

            UpdateCrouchKeyLabel();

            if (save)
            {
                PlayerPrefs.SetInt(CrouchKeySettingKey, (int)keyCode);
                PlayerPrefs.Save();
            }
        }

        private KeyCode LoadCrouchKey()
        {
            int storedKey = PlayerPrefs.GetInt(CrouchKeySettingKey, (int)defaultCrouchKey);
            return storedKey == (int)KeyCode.C ? KeyCode.C : KeyCode.LeftControl;
        }
    }
}

