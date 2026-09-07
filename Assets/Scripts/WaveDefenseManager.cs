using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine;

namespace FpsPractice.FPS
{
    public class WaveDefenseManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private WaveSpawnZone[] spawnZones;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private LayerMask visibilityMask = ~0;
        [SerializeField] private WeaponShopInventory shopInventory;
        [Tooltip("좀비 처치 시 현재 들고 있는 무기의 예비 탄약을 10발 얻을 확률입니다.")]
        [SerializeField, Range(0f, 100f)] private float ammoRewardChancePercent = 10f;
        [SerializeField] private WeaponLoadout playerWeaponLoadout;
        [SerializeField] private PlayerProgression playerProgression;
        [SerializeField] private TMP_Text statusText;

        [Header("Audio")]
        [Tooltip("시작음과 웨이브 임박음 재생에 사용할 AudioSource입니다.")]
        [SerializeField] private AudioSource sfxSource;
        [Tooltip("웨이브 BGM 재생에 사용할 AudioSource입니다.")]
        [SerializeField] private AudioSource bgmSource;
        [Tooltip("MainFps 씬에 처음 들어왔을 때 한 번 재생할 효과음입니다.")]
        [SerializeField] private AudioClip initialStartClip;
        [Tooltip("다음 웨이브 시작 5초 전에 한 번 재생할 효과음입니다.")]
        [SerializeField] private AudioClip nextWaveWarningClip;
        [Tooltip("웨이브가 시작될 때 재생할 BGM입니다.")]
        [SerializeField] private AudioClip waveBgmClip;
        [Tooltip("다음 웨이브 시작 전에 경고음을 재생할 기준 시간입니다.")]
        [SerializeField, Min(0f)] private float waveWarningTime = 5f;
        [Tooltip("웨이브가 끝날 때 BGM이 작아지는 시간입니다.")]
        [SerializeField, Min(0f)] private float bgmFadeOutDuration = 1f;

        [Header("Status Text")]
        [SerializeField] private string initialMessage = "준비하세요";
        [SerializeField] private string waveStartMessage = "웨이브 {0} 시작";
        [SerializeField] private string waveClearMessage = "웨이브 {0} 종료 - 상점 이용 가능";
        [SerializeField] private string nextWaveCountdownMessage = "다음 웨이브까지 {0:0.0}초";
        [Tooltip("Status Text의 각 상태 문구를 표시하는 공통 지속시간입니다.")]
        [SerializeField, Min(0f)] private float statusMessageDuration = 1f;
        [Header("Fade")]
        [Tooltip("상태 텍스트가 투명해지는 데 걸리는 시간입니다.")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;
        [Tooltip("새 상태 텍스트가 나타나는 데 걸리는 시간입니다.")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;

        [Header("Wave")]
        [SerializeField, Min(1)] private int startingWave = 1;
        [SerializeField, Min(1)] private int zombiesInFirstWave = 5;
        [SerializeField, Min(0)] private int extraZombiesPerWave = 2;
        [SerializeField, Min(0f)] private float timeBetweenWaves = 5f;
        [SerializeField, Min(1f)] private float minSpawnDistance = 15f;
        [SerializeField, Min(1)] private int spawnAttemptsPerZombie = 20;
        [SerializeField, Min(1f)] private float groundRayHeight = 15f;
        [SerializeField] private bool startOnAwake = true;
        [Tooltip("대기 시간 중 이 키를 누르면 즉시 다음 웨이브를 시작합니다.")]
        [SerializeField] private KeyCode nextWaveKey = KeyCode.N;
        [Header("Zombie Rewards")]
        [Tooltip("좀비 한 마리를 처치했을 때 받는 기본 돈입니다.")]
        [SerializeField, Min(0)] private int moneyPerZombie = 10;
        [Tooltip("웨이브가 1 증가할 때마다 좀비 처치 돈 보상에 더해지는 값입니다.")]
        [SerializeField, Min(0)] private int moneyRewardIncreasePerWave = 2;
        [Tooltip("좀비 한 마리를 처치했을 때 받는 기본 경험치입니다.")]
        [SerializeField, Min(0f)] private float experiencePerZombie = 10f;
        [Tooltip("웨이브가 1 증가할 때마다 좀비 처치 경험치 보상에 더해지는 값입니다.")]
        [SerializeField, Min(0f)] private float experienceRewardIncreasePerWave = 2f;

        private readonly List<EnemyAI> activeEnemies = new List<EnemyAI>();
        private Coroutine waveRoutine;
        private int currentWave;
        private bool isRunning;
        private Coroutine statusFadeRoutine;
        private Coroutine bgmFadeRoutine;
        private float statusTextBaseAlpha = 1f;
        private float bgmBaseVolume = 1f;
        private string currentStatusMessage;

        public int CurrentWave => currentWave;
        public int AliveEnemies => activeEnemies.Count;
        public bool IsRunning => isRunning;
        public bool IsWaitingForNextWave { get; private set; }

        private void Awake()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }

            if (playerWeaponLoadout == null && player != null)
            {
                playerWeaponLoadout = player.GetComponentInChildren<WeaponLoadout>(true);
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (spawnZones == null || spawnZones.Length == 0)
            {
                spawnZones = FindObjectsByType<WaveSpawnZone>(FindObjectsSortMode.None);
            }

            if (shopInventory == null)
            {
                shopInventory = FindFirstObjectByType<WeaponShopInventory>();
            }

            if (playerProgression == null && player != null)
            {
                playerProgression = player.GetComponentInParent<PlayerProgression>();
            }

            if (statusText != null)
            {
                statusTextBaseAlpha = statusText.color.a;
            }

            if (bgmSource != null)
            {
                bgmBaseVolume = bgmSource.volume;
            }
        }

        private void Start()
        {
            SetStatusText(initialMessage);
            SetShopAccess(false);
            PlaySfx(initialStartClip);

            if (startOnAwake)
            {
                StartDefense();
            }
        }

        public void StartDefense()
        {
            if (isRunning || zombiePrefab == null || spawnZones == null || spawnZones.Length == 0)
            {
                return;
            }

            isRunning = true;
            IsWaitingForNextWave = false;
            SetReserveAmmoConsumptionBlocked(false);
            SetShopAccess(false);
            currentWave = Mathf.Max(1, startingWave - 1);
            waveRoutine = StartCoroutine(WaveLoop());
        }

        public void StopDefense()
        {
            if (waveRoutine != null)
            {
                StopCoroutine(waveRoutine);
                waveRoutine = null;
            }

            isRunning = false;
            IsWaitingForNextWave = false;
            SetReserveAmmoConsumptionBlocked(false);
            SetShopAccess(false);
            StopWaveBgm();
        }

        private IEnumerator WaveLoop()
        {
            if (statusMessageDuration > 0f)
            {
                yield return new WaitForSeconds(statusMessageDuration);
            }

            while (isRunning)
            {
                currentWave++;
                SetStatusText(string.Format(waveStartMessage, currentWave));
                PlayWaveBgm();
                int enemyCount = zombiesInFirstWave + (currentWave - 1) * extraZombiesPerWave;

                for (int i = 0; i < enemyCount; i++)
                {
                    SpawnZombie();
                    yield return null;
                }

                while (activeEnemies.Count > 0 && isRunning)
                {
                    RemoveDestroyedEnemies();
                    yield return null;
                }

                if (!isRunning)
                {
                    break;
                }

                SetStatusText(string.Format(waveClearMessage, currentWave));
                FadeOutWaveBgm();

                if (timeBetweenWaves > 0f)
                {
                    IsWaitingForNextWave = true;
                    SetReserveAmmoConsumptionBlocked(true);
                    SetShopAccess(true);
                    float remainingTime = timeBetweenWaves;
                    float clearMessageRemaining = Mathf.Min(statusMessageDuration, remainingTime);
                    bool countdownStarted = false;
                    bool warningPlayed = false;
                    while (remainingTime > 0f && isRunning)
                    {
                        if (!warningPlayed && remainingTime <= waveWarningTime)
                        {
                            PlaySfx(nextWaveWarningClip);
                            warningPlayed = true;
                        }
                        if (clearMessageRemaining > 0f)
                        {
                            UpdateStatusText(string.Format(waveClearMessage, currentWave));
                        }
                        else
                        {
                            string countdownMessage = string.Format(nextWaveCountdownMessage, Mathf.Max(0f, remainingTime));
                            if (!countdownStarted)
                            {
                                SetStatusText(countdownMessage, true);
                                countdownStarted = true;
                            }
                            else
                            {
                                UpdateStatusText(countdownMessage);
                            }
                        }

                        if (Input.GetKeyDown(nextWaveKey))
                        {
                            if (shopInventory != null)
                            {
                                shopInventory.CloseShop();
                            }
                            break;
                        }

                        float countdownDelta = shopInventory != null && shopInventory.IsShopOpen
                            ? Time.unscaledDeltaTime
                            : Time.deltaTime;
                        remainingTime -= countdownDelta;
                        clearMessageRemaining -= countdownDelta;
                        yield return null;
                    }
                    IsWaitingForNextWave = false;
                    SetReserveAmmoConsumptionBlocked(false);
                    SetShopAccess(false);
                }
            }

            waveRoutine = null;
        }

        private void SetReserveAmmoConsumptionBlocked(bool blocked)
        {
            if (player == null)
            {
                return;
            }

            Weapon[] weapons = player.GetComponentsInChildren<Weapon>(true);
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                {
                    weapons[i].SetReserveAmmoConsumptionBlocked(blocked);
                }
            }
        }
        private void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        private void PlayWaveBgm()
        {
            if (bgmSource == null || waveBgmClip == null ||
                (bgmSource.isPlaying && bgmSource.clip == waveBgmClip && bgmFadeRoutine == null))
            {
                return;
            }

            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
                bgmFadeRoutine = null;
            }

            bgmSource.clip = waveBgmClip;
            bgmSource.loop = true;
            bgmSource.volume = bgmBaseVolume;
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }

        private void FadeOutWaveBgm()
        {
            if (bgmSource == null || !bgmSource.isPlaying)
            {
                return;
            }

            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
            }

            bgmFadeRoutine = StartCoroutine(FadeOutWaveBgmCoroutine());
        }

        private IEnumerator FadeOutWaveBgmCoroutine()
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < bgmFadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f,
                    Mathf.Clamp01(elapsed / Mathf.Max(0.01f, bgmFadeOutDuration)));
                yield return null;
            }

            bgmSource.volume = 0f;
            bgmSource.Stop();
            bgmSource.volume = bgmBaseVolume;
            bgmFadeRoutine = null;
        }

        private void StopWaveBgm()
        {
            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
                bgmFadeRoutine = null;
            }

            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.volume = bgmBaseVolume;
            }
        }

        private void SpawnZombie()
        {
            if (zombiePrefab == null || player == null)
            {
                return;
            }

            for (int attempt = 0; attempt < spawnAttemptsPerZombie; attempt++)
            {
                WaveSpawnZone zone = spawnZones[Random.Range(0, spawnZones.Length)];
                if (zone == null)
                {
                    continue;
                }

                Vector3 candidate = zone.GetRandomPoint();
                if (!IsValidSpawnPoint(candidate))
                {
                    continue;
                }

                if (!Physics.Raycast(candidate + Vector3.up * groundRayHeight, Vector3.down,
                    out RaycastHit groundHit, groundRayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Vector3 spawnPosition = groundHit.point;
                Quaternion spawnRotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(player.position - spawnPosition, Vector3.up).normalized,
                    Vector3.up);
                GameObject instance = Instantiate(zombiePrefab, spawnPosition, spawnRotation);
                EnemyAI enemy = instance.GetComponent<EnemyAI>();
                Health health = instance.GetComponent<Health>();

                if (enemy == null || health == null)
                {
                    Destroy(instance);
                    return;
                }

                health.ConfigureDeathBehavior(true, false, 0f);
                health.Died += HandleEnemyDied;
                enemy.ConfigureForWave(player);
                activeEnemies.Add(enemy);
                return;
            }
        }

        private bool IsValidSpawnPoint(Vector3 point)
        {
            if (Vector3.Distance(point, player.position) < minSpawnDistance)
            {
                return false;
            }

            if (playerWeaponLoadout == null && player != null)
            {
                playerWeaponLoadout = player.GetComponentInChildren<WeaponLoadout>(true);
            }

            if (playerCamera == null)
            {
                return true;
            }

            Vector3 viewPoint = playerCamera.WorldToViewportPoint(point + Vector3.up);
            bool insideView = viewPoint.z > 0f
                && viewPoint.x >= 0f && viewPoint.x <= 1f
                && viewPoint.y >= 0f && viewPoint.y <= 1f;
            if (!insideView)
            {
                return true;
            }

            return Physics.Linecast(playerCamera.transform.position, point + Vector3.up,
                out _, visibilityMask, QueryTriggerInteraction.Ignore);
        }

        private void SetStatusText(string message, bool keepVisible = false)
        {
            if (statusText == null)
            {
                return;
            }

            if (currentStatusMessage == message && statusText.gameObject.activeSelf)
            {
                return;
            }

            if (statusFadeRoutine != null)
            {
                StopCoroutine(statusFadeRoutine);
            }

            currentStatusMessage = message;
            statusFadeRoutine = StartCoroutine(FadeStatusText(message, keepVisible));
        }

        private void UpdateStatusText(string message)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.gameObject.SetActive(true);
            statusText.text = message;
            currentStatusMessage = message;
        }

        private IEnumerator FadeStatusText(string message, bool keepVisible)
        {
            statusText.gameObject.SetActive(true);
            yield return FadeStatusAlpha(0f, fadeOutDuration);
            statusText.text = message;
            currentStatusMessage = message;
            yield return FadeStatusAlpha(statusTextBaseAlpha, fadeInDuration);

            if (!keepVisible)
            {
                yield return new WaitForSeconds(statusMessageDuration);
                if (currentStatusMessage != message)
                {
                    statusFadeRoutine = null;
                    yield break;
                }

                yield return FadeStatusAlpha(0f, fadeOutDuration);
                if (currentStatusMessage == message)
                {
                    statusText.gameObject.SetActive(false);
                }
            }

            statusFadeRoutine = null;
        }

        private IEnumerator FadeStatusAlpha(float targetAlpha, float duration)
        {
            Color color = statusText.color;
            float startAlpha = color.a;
            if (duration <= 0f)
            {
                color.a = targetAlpha;
                statusText.color = color;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                statusText.color = color;
                yield return null;
            }

            color.a = targetAlpha;
            statusText.color = color;
        }

        private void SetShopAccess(bool allowed)
        {
            if (shopInventory != null)
            {
                shopInventory.SetShopAccess(allowed);
            }
        }

        private void HandleEnemyDied()
        {
            GiveEnemyReward();
            RemoveDestroyedEnemies();
        }

        private void GiveEnemyReward()
        {
            int waveOffset = Mathf.Max(0, currentWave - 1);
            int moneyReward = moneyPerZombie + moneyRewardIncreasePerWave * waveOffset;
            float experienceReward = experiencePerZombie
                + experienceRewardIncreasePerWave * waveOffset;

            if (shopInventory != null && moneyReward > 0)
            {
                shopInventory.AddMoney(moneyReward);
            }

            if (playerProgression != null)
            {
                playerProgression.AddExperience(experienceReward);
            }

            GiveAmmoReward();
        }

        private void GiveAmmoReward()
        {
            if (playerWeaponLoadout == null || ammoRewardChancePercent <= 0f
                || Random.Range(0f, 100f) >= ammoRewardChancePercent)
            {
                return;
            }

            Weapon currentWeapon = playerWeaponLoadout.CurrentWeapon as Weapon;
            if (currentWeapon != null)
            {
                currentWeapon.AddAmmo(10);
            }
        }

        private void RemoveDestroyedEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null || activeEnemies[i].GetComponent<Health>() == null
                    || activeEnemies[i].GetComponent<Health>().IsDead)
                {
                    activeEnemies.RemoveAt(i);
                }
            }
        }
    }
}