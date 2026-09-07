using UnityEngine;
using UnityEngine.UI;

namespace FpsPractice.FPS
{
    public sealed class WeaponShopPurchaseButton : MonoBehaviour
    {
        [Tooltip("구매를 처리할 WeaponShopInventory입니다.")]
        [SerializeField] private WeaponShopInventory shopInventory;
        [Tooltip("WeaponShopInventory의 Shop Items 목록 인덱스입니다. 첫 아이템은 0입니다.")]
        [SerializeField] private int itemIndex;
        [SerializeField] private Button purchaseButton;

        private void Awake()
        {
            if (purchaseButton == null)
            {
                purchaseButton = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            RefreshInteractable();
        }

        private void Update()
        {
            RefreshInteractable();
        }

        private void RefreshInteractable()
        {
            if (purchaseButton != null && shopInventory != null)
            {
                purchaseButton.interactable = shopInventory.CanPurchase(itemIndex);
            }
        }

        public void Purchase()
        {
            if (shopInventory != null)
            {
                shopInventory.PurchaseAndEquip(itemIndex);
            }
        }
    }
}