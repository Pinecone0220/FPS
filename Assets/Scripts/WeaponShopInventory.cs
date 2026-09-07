using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FpsPractice.FPS
{
    public sealed class WeaponShopInventory : MonoBehaviour
    {
        public enum WeaponCategory
        {
            Primary,
            Secondary,
            Knife,
            InstantEffect
        }

        public enum InstantEffectType
        {
            Heal,
            Armor,
            Ammo
        }

        [Serializable]
        public sealed class ShopItem
        {
            [Tooltip("상점과 저장에 사용할 고유 ID입니다.")]
            public string itemId;

            [Tooltip("구매 가격입니다.")]
            public int price;

            [Tooltip("무기 또는 즉시 효과의 카테고리입니다.")]
            public WeaponCategory category;

            [Tooltip("무기 상품일 때만 할당합니다. 즉시 효과 상품은 비워둡니다.")]
            public MonoBehaviour weaponPrefab;

            [Tooltip("즉시 효과 상품에 적용할 효과 종류입니다.")]
            public InstantEffectType instantEffectType;

            [Tooltip("회복량, 방어구 수치 또는 추가 탄약 수입니다.")]
            public float effectAmount;

            [Tooltip("이 상품 오브젝트의 Button입니다. 클릭하면 바로 구매합니다.")]
            public Button itemButton;
        }

        [Header("Shop UI")]
        [Tooltip("B키를 눌렀을 때 열고 닫을 상점 패널입니다.")]
        [SerializeField] private GameObject shopPanelRoot;
        [SerializeField] private KeyCode shopKey = KeyCode.B;
        [Tooltip("현재 보유 금액을 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text moneyText;

        [Header("References")]
        [SerializeField] private WeaponLoadout weaponLoadout;
        [Tooltip("게임 시작부터 지급되는 기본 권총입니다. 이미 슬롯에 넣었다면 비워도 됩니다.")]
        [SerializeField] private MonoBehaviour startingPistol;
        [SerializeField] private Health playerHealth;

        [Header("Starting State")]
        [SerializeField] private int startingMoney = 1000;
        [Tooltip("기본 권총이 들어갈 슬롯 인덱스입니다.")]
        [SerializeField] private int pistolSlotIndex = 1;
        [Tooltip("주무기 아이템이 들어갈 플레이어 손 슬롯 인덱스입니다.")]
        [SerializeField] private int primarySlotIndex = 0;
        [Tooltip("보조무기 아이템이 들어갈 플레이어 손 슬롯 인덱스입니다.")]
        [SerializeField] private int secondarySlotIndex = 1;

        [Header("Shop Items")]
        [SerializeField] private List<ShopItem> items = new List<ShopItem>();

        public int Money { get; private set; }
        public IReadOnlyList<ShopItem> Items => items;
        public bool IsShopOpen => shopOpen;
        public bool CanOpenShop => shopAccessAllowed;

        private bool shopOpen;
        private bool shopAccessAllowed = true;
        private float previousTimeScale = 1f;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;

        private void Awake()
        {
            if (weaponLoadout == null)
            {
                weaponLoadout = GetComponent<WeaponLoadout>();
            }

            if (playerHealth == null)
            {
                playerHealth = GetComponent<Health>();
            }

            if (playerHealth == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerHealth = playerObject.GetComponent<Health>();
                }
            }

            Money = startingMoney;
            EnsureStarterWeapons();
            RefreshMoneyText();

            if (weaponLoadout != null)
            {
                weaponLoadout.KeepOnlySlots(primarySlotIndex, secondarySlotIndex);
            }

            if (shopPanelRoot != null)
            {
                shopPanelRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(shopKey))
            {
                ToggleShop();
            }
        }

        public void SetShopAccess(bool allowed)
        {
            shopAccessAllowed = allowed;
            if (!allowed && shopOpen)
            {
                CloseShop();
            }
        }

        public void ToggleShop()
        {
            if (!shopAccessAllowed)
            {
                return;
            }

            if (shopOpen)
            {
                CloseShop();
            }
            else
            {
                OpenShop();
            }
        }

        public void OpenShop()
        {
            if (!shopAccessAllowed || shopPanelRoot == null || shopOpen)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            shopOpen = true;
            shopPanelRoot.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseShop()
        {
            if (!shopOpen)
            {
                if (shopPanelRoot != null)
                {
                    shopPanelRoot.SetActive(false);
                }
                return;
            }

            shopOpen = false;
            if (shopPanelRoot != null)
            {
                shopPanelRoot.SetActive(false);
            }

            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }

        public bool IsPurchased(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId)
                && PlayerPrefs.GetInt(GetPurchaseKey(itemId), 0) == 1;
        }

        public bool AcquireAndEquip(WeaponCategory category, MonoBehaviour weaponPrefab)
        {
            if (category != WeaponCategory.Primary && category != WeaponCategory.Secondary)
            {
                return false;
            }

            if (weaponLoadout == null || weaponPrefab == null)
            {
                return false;
            }

            int slotIndex = GetSlotIndex(category);
            return slotIndex >= 0 && weaponLoadout.ReplaceSlot(slotIndex, weaponPrefab);
        }

        public bool CanPurchase(int itemIndex)
        {
            if (items == null || itemIndex < 0 || itemIndex >= items.Count)
            {
                return false;
            }

            ShopItem item = items[itemIndex];
            if (item == null || Money < Mathf.Max(0, item.price))
            {
                return false;
            }

            if (item.category == WeaponCategory.InstantEffect)
            {
                return CanApplyInstantEffect(item);
            }

            if (item.category != WeaponCategory.Primary && item.category != WeaponCategory.Secondary)
            {
                return false;
            }

            if (weaponLoadout == null || item.weaponPrefab == null)
            {
                return false;
            }

            int slotIndex = GetSlotIndex(item.category);
            return slotIndex >= 0
                && !weaponLoadout.IsSameWeaponInSlot(slotIndex, item.weaponPrefab);
        }

        public bool PurchaseAndEquip(int itemIndex)
        {
            if (items == null || itemIndex < 0 || itemIndex >= items.Count)
            {
                return false;
            }

            ShopItem item = items[itemIndex];
            if (item == null)
            {
                return false;
            }

            if (item.category == WeaponCategory.InstantEffect)
            {
                return PurchaseInstantEffect(item);
            }

            if (!CanPurchase(itemIndex))
            {
                return false;
            }

            int slotIndex = GetSlotIndex(item.category);
            int cost = Mathf.Max(0, item.price);
            Money -= cost;

            if (!weaponLoadout.ReplaceSlot(slotIndex, item.weaponPrefab))
            {
                Money += cost;
                return false;
            }

            RefreshMoneyText();
            SavePurchase(item);
            weaponLoadout.SelectSlot(slotIndex);
            return true;
        }

        public bool HasMoney(int amount)
        {
            return Money >= Mathf.Max(0, amount);
        }

        public void AddMoney(int amount)
        {
            Money = Mathf.Max(0, Money + amount);
            RefreshMoneyText();
        }

        private bool PurchaseInstantEffect(ShopItem item)
        {
            if (!CanApplyInstantEffect(item) || Money < Mathf.Max(0, item.price))
            {
                return false;
            }

            if (!ApplyInstantEffect(item))
            {
                return false;
            }

            Money -= Mathf.Max(0, item.price);
            RefreshMoneyText();
            SavePurchase(item);
            return true;
        }

        private bool CanApplyInstantEffect(ShopItem item)
        {
            if (item.effectAmount <= 0f)
            {
                return false;
            }

            switch (item.instantEffectType)
            {
                case InstantEffectType.Heal:
                    return playerHealth != null && playerHealth.CurrentHealth < playerHealth.MaxHealth;
                case InstantEffectType.Armor:
                    return playerHealth != null;
                case InstantEffectType.Ammo:
                    Weapon currentWeapon = weaponLoadout != null ? weaponLoadout.CurrentWeapon as Weapon : null;
                    return currentWeapon != null && currentWeapon.CanAddAmmo(Mathf.RoundToInt(item.effectAmount));
                default:
                    return false;
            }
        }

        private bool ApplyInstantEffect(ShopItem item)
        {
            switch (item.instantEffectType)
            {
                case InstantEffectType.Heal:
                    playerHealth.Heal(item.effectAmount);
                    return true;
                case InstantEffectType.Armor:
                    playerHealth.AddArmor(item.effectAmount);
                    return true;
                case InstantEffectType.Ammo:
                    Weapon currentWeapon = weaponLoadout != null ? weaponLoadout.CurrentWeapon as Weapon : null;
                    return currentWeapon != null && currentWeapon.AddAmmo(Mathf.RoundToInt(item.effectAmount));
                default:
                    return false;
            }
        }

        private void SavePurchase(ShopItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.itemId))
            {
                PlayerPrefs.SetInt(GetPurchaseKey(item.itemId), 1);
                PlayerPrefs.Save();
            }
        }

        private void RefreshMoneyText()
        {
            if (moneyText != null)
            {
                moneyText.text = "$" + Money;
            }
        }

        private int GetSlotIndex(WeaponCategory category)
        {
            switch (category)
            {
                case WeaponCategory.Primary:
                    return primarySlotIndex;
                case WeaponCategory.Secondary:
                    return secondarySlotIndex;
                default:
                    return -1;
            }
        }

        private void EnsureStarterWeapons()
        {
            if (weaponLoadout != null && startingPistol != null)
            {
                weaponLoadout.ReplaceSlot(pistolSlotIndex, startingPistol);
            }
        }

        private string GetPurchaseKey(string itemId)
        {
            return "WeaponShop.Purchased." + itemId;
        }
    }
}