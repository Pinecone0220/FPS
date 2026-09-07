using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FpsPractice.FPS
{
    public sealed class PlayerProgression : MonoBehaviour
    {
        public enum RewardCategory
        {
            PrimaryWeapon,
            SecondaryWeapon,
            BattleEffect
        }

        public enum RewardRarity
        {
            Common,
            Rare,
            Legendary
        }
        public enum RewardEffectType
        {
            Automatic,
            DamageUp,
            FireRateUp,
            InfiniteAmmo,
            Heal,
            MaxHealthUp,
            AmmoUp
        }

        [Serializable]
        public sealed class LevelUpReward
        {
            [Tooltip("보상 선택창에 표시할 이름입니다.")]
            public string rewardName = "New Reward";
            [Tooltip("보상의 종류입니다.")]
            public RewardCategory category;
            [Tooltip("보상의 등급입니다. 일반은 흰색, 희귀는 보라색, 전설은 노란색 카드로 표시됩니다.")]
            public RewardRarity rarity;
            [Tooltip("실제 적용할 효과입니다. Automatic은 보상 이름을 보고 자동으로 결정합니다.")]
            public RewardEffectType effectType;
            [Tooltip("보상이 처음 등장할 때의 수치입니다.")]
            public float baseValue = 1f;
            [Tooltip("같은 보상이 다시 등장할 때마다 더해지는 수치입니다.")]
            public float valueIncreasePerDuplicate = 1f;
            [TextArea]
            [Tooltip("보상 선택창에 표시할 설명입니다.")]
            public string description;
            [Tooltip("이 보상을 선택했을 때 실행할 효과입니다. 나중에 무기/전장 효과 함수를 연결할 수 있습니다.")]
            public UnityEvent onSelected;
        }

        [Serializable]
        public sealed class RewardChoiceSlot
        {
            [Tooltip("보상 선택 버튼입니다.")]
            public Button button;

            [Tooltip("보상 설명을 표시할 텍스트입니다.")]
            public TMP_Text descriptionText;
            [Tooltip("보상 카드의 배경 Image입니다. 비워두면 버튼의 기본 Image를 사용합니다.")]
            public Image cardBackground;
        }
        [Header("Level")]
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0f)] private float currentExperience;
        [SerializeField, Min(1f)] private float maxExperience = 100f;
        [Tooltip("레벨업할 때마다 최대 경험치에 더해지는 값입니다.")]
        [SerializeField, Min(0f)] private float maxExperienceIncreasePerLevel = 25f;

        [Header("UI")]
        [Tooltip("현재 레벨을 표시할 TextMeshPro 텍스트입니다.")]
        [SerializeField] private TMP_Text levelText;
        [Tooltip("현재 경험치와 최대 경험치를 표시할 TextMeshPro 텍스트입니다.")]
        [SerializeField] private TMP_Text experienceText;
        [Header("Level Up UI")]
        [Tooltip("레벨업 시 표시할 특성 선택 패널입니다. 패널 버튼에서 CloseLevelUpPanel을 호출하면 게임이 재개됩니다.")]
        [SerializeField] private GameObject levelUpPanel;
        [Header("Reward References")]
        [Tooltip("주무장/부무장 보상을 적용할 플레이어 무장입니다. 비워두면 자동으로 찾습니다.")]
        [SerializeField] private WeaponLoadout weaponLoadout;
        [Tooltip("회복 및 최대 체력 보상을 적용할 플레이어 체력입니다. 비워두면 자동으로 찾습니다.")]
        [SerializeField] private Health playerHealth;
        [Header("Reward Lists")]
        [Tooltip("레벨업 보상으로 등장할 주무장 목록입니다.")]
        [SerializeField] private List<LevelUpReward> primaryWeaponRewards = new List<LevelUpReward>();
        [Tooltip("레벨업 보상으로 등장할 부무장 목록입니다.")]
        [SerializeField] private List<LevelUpReward> secondaryWeaponRewards = new List<LevelUpReward>();
        [Tooltip("레벨업 보상으로 등장할 전장 효과 목록입니다.")]
        [SerializeField] private List<LevelUpReward> battleEffectRewards = new List<LevelUpReward>();

        [Header("Reward Choice UI")]
        [Tooltip("보상 3개를 표시할 버튼/텍스트 슬롯입니다. 슬롯은 최대 3개를 사용합니다.")]
        [SerializeField] private RewardChoiceSlot[] rewardChoiceSlots = new RewardChoiceSlot[3];
        [Tooltip("보상 패널이 열린 뒤 버튼을 선택할 수 있을 때까지 기다리는 시간입니다.")]
        [SerializeField, Min(0f)] private float rewardSelectionDelay = 1f;

        public int Level => level;
        public float CurrentExperience => currentExperience;
        public float MaxExperience => maxExperience;
        private int pendingLevelUps;
        private float previousTimeScale = 1f;
        private readonly LevelUpReward[] currentRewards = new LevelUpReward[3];
        private readonly UnityAction[] rewardButtonHandlers = new UnityAction[3];
        private readonly Dictionary<LevelUpReward, int> rewardAppearances =
            new Dictionary<LevelUpReward, int>();
        private readonly HashSet<LevelUpReward> removedLegendaryRewards =
            new HashSet<LevelUpReward>();
        private readonly RewardRarity[] currentRewardRarities = new RewardRarity[3];
        private readonly float[] currentRewardValues = new float[3];
        private readonly float[] pendingDamagePercent = new float[2];
        private readonly float[] pendingFireRatePercent = new float[2];
        private readonly float[] pendingMagazineCapacityPercent = new float[2];
        private readonly int[] pendingAmmo = new int[2];
        private readonly bool[] pendingInfiniteReserveAmmo = new bool[2];

        public LevelUpReward SelectedReward { get; private set; }
        public float SelectedRewardValue { get; private set; }
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;
        private bool levelUpPaused;
        private bool rewardSelectionReady;
        private Coroutine rewardSelectionCoroutine;
        private readonly List<Behaviour> pausedBehaviours = new List<Behaviour>();
        private readonly List<bool> pausedBehaviourStates = new List<bool>();

        private void Awake()
        {
            if (weaponLoadout == null)
            {
                weaponLoadout = GetComponentInParent<WeaponLoadout>();
            }

            if (playerHealth == null)
            {
                playerHealth = GetComponentInParent<Health>();
            }

            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(false);
            }

            WireRewardButtons();

            RefreshTexts();
        }

        private void Update()
        {
            if (levelUpPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            ApplyPendingWeaponRewards();
        }
        public void AddExperience(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            int levelUpCount = 0;
            currentExperience += amount;
            while (currentExperience >= maxExperience)
            {
                currentExperience -= maxExperience;
                level++;
                maxExperience += maxExperienceIncreasePerLevel;
                levelUpCount++;
            }

            RefreshTexts();

            if (levelUpCount > 0)
            {
                pendingLevelUps += levelUpCount;
                ShowLevelUpPanel();
            }
        }

        public void CloseLevelUpPanel()
        {
            if (levelUpPanel == null)
            {
                return;
            }

            if (rewardSelectionCoroutine != null)
            {
                StopCoroutine(rewardSelectionCoroutine);
                rewardSelectionCoroutine = null;
            }
            rewardSelectionReady = false;
            levelUpPanel.SetActive(false);
            pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);

            if (pendingLevelUps > 0)
            {
                ShowLevelUpPanel();
                return;
            }

            SetWeaponsInputPaused(false);
            SetPausedBehavioursEnabled(true);
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
            levelUpPaused = false;
        }

        private void ShowLevelUpPanel()
        {
            if (levelUpPanel == null || pendingLevelUps <= 0 || levelUpPanel.activeSelf)
            {
                return;
            }

            if (!levelUpPaused)
            {
                previousTimeScale = Time.timeScale;
                previousCursorLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                CachePausedBehaviours();
                levelUpPaused = true;
            }

            SetPausedBehavioursEnabled(false);
            SetWeaponsInputPaused(true);
            rewardSelectionReady = false;
            PrepareRewardChoices();
            SetRewardButtonsInteractable(false);
            rewardSelectionCoroutine = StartCoroutine(EnableRewardSelectionAfterDelay());
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            levelUpPanel.SetActive(true);
        }
        private void CachePausedBehaviours()
        {
            pausedBehaviours.Clear();
            pausedBehaviourStates.Clear();

            AddPausedBehaviour(GetComponentInChildren<PlayerLook>(true));
            AddPausedBehaviour(GetComponent<PlayerController>());
            AddPausedBehaviour(GetComponent<WeaponLoadout>());
            AddPausedBehaviour(GetComponent<PlayerSetupHelper>());

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
        private void AddPausedBehaviour(Behaviour behaviour)
        {
            if (behaviour == null || pausedBehaviours.Contains(behaviour))
            {
                return;
            }

            pausedBehaviours.Add(behaviour);
            pausedBehaviourStates.Add(behaviour.enabled);
        }

        private void SetPausedBehavioursEnabled(bool enabled)
        {
            for (int i = 0; i < pausedBehaviours.Count; i++)
            {
                Behaviour behaviour = pausedBehaviours[i];
                if (behaviour != null)
                {
                    behaviour.enabled = enabled ? pausedBehaviourStates[i] : false;
                }
            }
        }
        private void WireRewardButtons()
        {
            if (rewardChoiceSlots == null)
            {
                return;
            }

            for (int i = 0; i < rewardChoiceSlots.Length && i < currentRewards.Length; i++)
            {
                if (rewardChoiceSlots[i] == null || rewardChoiceSlots[i].button == null)
                {
                    continue;
                }

                int rewardIndex = i;
                rewardButtonHandlers[i] = () => ChooseReward(rewardIndex);
                rewardChoiceSlots[i].button.onClick.AddListener(rewardButtonHandlers[i]);
            }
        }

        private void PrepareRewardChoices()
        {
            List<LevelUpReward> availableRewards = new List<LevelUpReward>();
            AddRewards(availableRewards, primaryWeaponRewards);
            AddRewards(availableRewards, secondaryWeaponRewards);
            AddRewards(availableRewards, battleEffectRewards);

            for (int i = 0; i < currentRewards.Length; i++)
            {
                currentRewards[i] = null;
                currentRewardRarities[i] = RewardRarity.Common;
                currentRewardValues[i] = 0f;
                if (i < availableRewards.Count)
                {
                    int randomIndex = UnityEngine.Random.Range(0, availableRewards.Count);
                    LevelUpReward reward = availableRewards[randomIndex];
                    int previousAppearances = GetAppearanceCount(reward);
                    currentRewards[i] = reward;
                    currentRewardRarities[i] = GetDisplayedRarity(reward, previousAppearances);
                    currentRewardValues[i] = GetDisplayedValue(reward, previousAppearances);
                    rewardAppearances[reward] = previousAppearances + 1;
                    availableRewards.RemoveAt(randomIndex);
                }

                UpdateRewardSlot(i, currentRewards[i]);
            }
        }

        private void AddRewards(List<LevelUpReward> target, List<LevelUpReward> source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && !removedLegendaryRewards.Contains(source[i]))
                {
                    target.Add(source[i]);
                }
            }
        }

        private IEnumerator EnableRewardSelectionAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, rewardSelectionDelay));

            if (!levelUpPaused || levelUpPanel == null || !levelUpPanel.activeSelf)
            {
                rewardSelectionCoroutine = null;
                yield break;
            }

            rewardSelectionReady = true;
            SetRewardButtonsInteractable(true);
            rewardSelectionCoroutine = null;
        }

        private void SetRewardButtonsInteractable(bool interactable)
        {
            if (rewardChoiceSlots == null)
            {
                return;
            }

            for (int i = 0; i < rewardChoiceSlots.Length && i < currentRewards.Length; i++)
            {
                RewardChoiceSlot slot = rewardChoiceSlots[i];
                if (slot != null && slot.button != null)
                {
                    slot.button.interactable = interactable && currentRewards[i] != null;
                }
            }
        }
        private void UpdateRewardSlot(int index, LevelUpReward reward)
        {
            if (rewardChoiceSlots == null || index < 0 || index >= rewardChoiceSlots.Length ||
                rewardChoiceSlots[index] == null)
            {
                return;
            }

            RewardChoiceSlot slot = rewardChoiceSlots[index];
            bool hasReward = reward != null;
            if (slot.button != null)
            {
                slot.button.gameObject.SetActive(hasReward);
                slot.button.interactable = hasReward && rewardSelectionReady;
            }

            if (!hasReward)
            {
                return;
            }


            if (slot.descriptionText != null)
            {
                slot.descriptionText.text = reward.description.Replace("{value}", FormatRewardValue(currentRewardValues[index]));
            }

            Image cardBackground = slot.cardBackground;
            if (cardBackground == null && slot.button != null)
            {
                cardBackground = slot.button.targetGraphic as Image;
            }

            if (cardBackground != null)
            {
                cardBackground.color = GetRarityColor(currentRewardRarities[index]);
            }
        }

        private void ChooseReward(int index)
        {
            if (!rewardSelectionReady || index < 0 || index >= currentRewards.Length || currentRewards[index] == null)
            {
                return;
            }

            SelectedReward = currentRewards[index];
            SelectedRewardValue = currentRewardValues[index];
            ApplyReward(currentRewards[index], SelectedRewardValue);
            currentRewards[index].onSelected?.Invoke();
            if (currentRewardRarities[index] == RewardRarity.Legendary)
            {
                removedLegendaryRewards.Add(currentRewards[index]);
            }

            CloseLevelUpPanel();
        }

        private void ApplyReward(LevelUpReward reward, float value)
        {
            RewardEffectType effectType = ResolveEffectType(reward);
            switch (effectType)
            {
                case RewardEffectType.DamageUp:
                    ApplyOrQueueDamageReward(reward.category, value);
                    break;
                case RewardEffectType.FireRateUp:
                    ApplyOrQueueFireRateReward(reward.category, value);
                    break;
                case RewardEffectType.InfiniteAmmo:
                    ApplyOrQueueInfiniteAmmoReward(reward.category);
                    break;
                case RewardEffectType.Heal:
                    playerHealth?.Heal(value);
                    break;
                case RewardEffectType.MaxHealthUp:
                    if (playerHealth != null)
                    {
                        playerHealth.SetMaxHealth(playerHealth.MaxHealth + value, false);
                        playerHealth.Heal(value);
                    }
                    break;
                case RewardEffectType.AmmoUp:
                    int ammoAmount = Mathf.RoundToInt(value);
                    if (weaponLoadout != null)
                    {
                        AddAmmoToWeaponSlot(0, ammoAmount);
                        AddAmmoToWeaponSlot(1, ammoAmount);
                    }
                    break;
            }
        }

        private void AddAmmoToWeaponSlot(int slotIndex, int amount)
        {
            if (amount <= 0 || weaponLoadout == null)
            {
                return;
            }

            Weapon weapon = weaponLoadout.GetWeaponComponent(slotIndex) as Weapon;
            if (weapon != null)
            {
                weapon.AddAmmo(amount);
                return;
            }

            if (slotIndex >= 0 && slotIndex < pendingAmmo.Length)
            {
                pendingAmmo[slotIndex] += amount;
            }
        }

        private void ApplyOrQueueDamageReward(RewardCategory category, float value)
        {
            int slotIndex = GetRewardSlotIndex(category);
            if (slotIndex < 0)
            {
                return;
            }

            Weapon weapon = GetRewardWeapon(category);
            if (weapon != null)
            {
                weapon.AddDamagePercent(value);
            }
            else
            {
                pendingDamagePercent[slotIndex] += value;
            }
        }

        private void ApplyOrQueueFireRateReward(RewardCategory category, float value)
        {
            int slotIndex = GetRewardSlotIndex(category);
            if (slotIndex < 0)
            {
                return;
            }

            Weapon weapon = GetRewardWeapon(category);
            if (weapon != null)
            {
                weapon.AddFireRatePercent(value);
            }
            else
            {
                pendingFireRatePercent[slotIndex] += value;
            }
        }

        private void ApplyOrQueueInfiniteAmmoReward(RewardCategory category)
        {
            int slotIndex = GetRewardSlotIndex(category);
            if (slotIndex < 0)
            {
                return;
            }

            Weapon weapon = GetRewardWeapon(category);
            if (weapon != null)
            {
                weapon.AddMagazineCapacityPercent(-10f);
                weapon.SetInfiniteReserveAmmo(true);
            }
            else
            {
                pendingMagazineCapacityPercent[slotIndex] -= 10f;
                pendingInfiniteReserveAmmo[slotIndex] = true;
            }
        }

        private void ApplyPendingWeaponRewards()
        {
            if (weaponLoadout == null)
            {
                return;
            }

            for (int slotIndex = 0; slotIndex < pendingAmmo.Length; slotIndex++)
            {
                Weapon weapon = weaponLoadout.GetWeaponComponent(slotIndex) as Weapon;
                if (weapon == null)
                {
                    continue;
                }

                if (!Mathf.Approximately(pendingDamagePercent[slotIndex], 0f))
                {
                    weapon.AddDamagePercent(pendingDamagePercent[slotIndex]);
                    pendingDamagePercent[slotIndex] = 0f;
                }

                if (!Mathf.Approximately(pendingFireRatePercent[slotIndex], 0f))
                {
                    weapon.AddFireRatePercent(pendingFireRatePercent[slotIndex]);
                    pendingFireRatePercent[slotIndex] = 0f;
                }

                if (!Mathf.Approximately(pendingMagazineCapacityPercent[slotIndex], 0f))
                {
                    weapon.AddMagazineCapacityPercent(pendingMagazineCapacityPercent[slotIndex]);
                    pendingMagazineCapacityPercent[slotIndex] = 0f;
                }

                if (pendingInfiniteReserveAmmo[slotIndex])
                {
                    weapon.SetInfiniteReserveAmmo(true);
                    pendingInfiniteReserveAmmo[slotIndex] = false;
                }

                if (pendingAmmo[slotIndex] > 0)
                {
                    weapon.AddAmmo(pendingAmmo[slotIndex]);
                    pendingAmmo[slotIndex] = 0;
                }
            }
        }

        private RewardEffectType ResolveEffectType(LevelUpReward reward)
        {
            if (reward.effectType != RewardEffectType.Automatic)
            {
                return reward.effectType;
            }

            string rewardName = (reward.rewardName ?? string.Empty).ToLowerInvariant();
            if (rewardName.Contains("damage"))
            {
                return RewardEffectType.DamageUp;
            }

            if (rewardName.Contains("speed"))
            {
                return RewardEffectType.FireRateUp;
            }

            if (rewardName.Contains("infinite") || rewardName.Contains("infinity"))
            {
                return RewardEffectType.InfiniteAmmo;
            }

            if (rewardName.Contains("maxhp"))
            {
                return RewardEffectType.MaxHealthUp;
            }

            if (rewardName.Contains("heal"))
            {
                return RewardEffectType.Heal;
            }

            if (rewardName.Contains("ammo"))
            {
                return RewardEffectType.AmmoUp;
            }

            return RewardEffectType.Automatic;
        }

        private Weapon GetRewardWeapon(RewardCategory category)
        {
            int slotIndex = GetRewardSlotIndex(category);
            if (weaponLoadout == null || slotIndex < 0)
            {
                return null;
            }

            return weaponLoadout.GetWeaponComponent(slotIndex) as Weapon;
        }

        private int GetRewardSlotIndex(RewardCategory category)
        {
            switch (category)
            {
                case RewardCategory.PrimaryWeapon:
                    return 0;
                case RewardCategory.SecondaryWeapon:
                    return 1;
                default:
                    return -1;
            }
        }
        private int GetAppearanceCount(LevelUpReward reward)
        {
            return rewardAppearances.TryGetValue(reward, out int count) ? count : 0;
        }

        private RewardRarity GetDisplayedRarity(LevelUpReward reward, int appearanceCount)
        {
            int rarityValue = Mathf.Clamp((int)reward.rarity + appearanceCount, 0, (int)RewardRarity.Legendary);
            return (RewardRarity)rarityValue;
        }

        private float GetDisplayedValue(LevelUpReward reward, int appearanceCount)
        {
            return reward.baseValue + reward.valueIncreasePerDuplicate * appearanceCount;
        }

        private string FormatRewardValue(float value)
        {
            return value.ToString("0.##");
        }

        private string GetRarityName(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Rare:
                    return "희귀";
                case RewardRarity.Legendary:
                    return "전설";
                default:
                    return "일반";
            }
        }
        private Color GetRarityColor(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Rare:
                    return new Color32(170, 80, 255, 255);
                case RewardRarity.Legendary:
                    return new Color32(255, 215, 0, 255);
                default:
                    return Color.white;
            }
        }
        private string GetCategoryName(RewardCategory category)
        {
            switch (category)
            {
                case RewardCategory.PrimaryWeapon:
                    return "주무장";
                case RewardCategory.SecondaryWeapon:
                    return "부무장";
                default:
                    return "전장 효과";
            }
        }
        private void RefreshTexts()
        {
            if (levelText != null)
            {
                levelText.SetText("Level: {0}", level);
            }

            if (experienceText != null)
            {
                experienceText.text = "EXP: " + currentExperience.ToString("0.##")
                    + " / " + maxExperience.ToString("0.##");
            }
        }
    }
}