using System;
using UnityEngine;

namespace FpsPractice.FPS
{
    public class WeaponLoadout : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            [Tooltip("이 슬롯의 이름입니다. 인스펙터에서 구분할 때 사용합니다.")]
            public string slotName = "Weapon";

            [Tooltip("이 슬롯에 들어갈 총 무장 컴포넌트입니다. 주무기 또는 부무기를 넣습니다.")]
            public MonoBehaviour weapon;
        }

        [Header("Loadout")]
        [SerializeField] private Slot[] slots = new Slot[2];
        [SerializeField] private int startSlotIndex = 0;
        [SerializeField] private bool allowMouseWheelSwitch = true;

        public int CurrentSlotIndex { get; private set; } = -1;
        public IWeapon CurrentWeapon
        {
            get
            {
                if (CurrentSlotIndex < 0 || CurrentSlotIndex >= GetPlayableSlotCount())
                {
                    return null;
                }

                return slots[CurrentSlotIndex] != null ? slots[CurrentSlotIndex].weapon as IWeapon : null;
            }
        }

        private void Start()
        {
            if (slots == null || slots.Length == 0)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                SetSlotActive(i, false);
            }

            ClearUnsupportedSlots();

            for (int i = 0; i < GetPlayableSlotCount(); i++)
            {
                if (slots[i] != null && slots[i].weapon != null)
                {
                    SelectSlot(i);
                    break;
                }
            }
        }

        private void Update()
        {
            if (slots == null || slots.Length == 0)
            {
                return;
            }

            int requestedIndex = CurrentSlotIndex;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                requestedIndex = 0;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                requestedIndex = 1;
            }
            else if (allowMouseWheelSwitch)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll > 0.01f)
                {
                    requestedIndex = GetNextValidIndex(-1);
                }
                else if (scroll < -0.01f)
                {
                    requestedIndex = GetNextValidIndex(1);
                }
            }

            SelectSlot(requestedIndex);
        }

        public void SelectSlot(int slotIndex)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= GetPlayableSlotCount())
            {
                return;
            }

            if (slots[slotIndex] == null || slots[slotIndex].weapon == null)
            {
                return;
            }

            if (CurrentSlotIndex == slotIndex)
            {
                return;
            }

            SetSlotActive(CurrentSlotIndex, false);
            CurrentSlotIndex = slotIndex;
            SetSlotActive(CurrentSlotIndex, true);
        }

        public MonoBehaviour GetWeaponComponent(int slotIndex)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= GetPlayableSlotCount() || slots[slotIndex] == null)
            {
                return null;
            }

            return slots[slotIndex].weapon;
        }

        public bool IsSameWeaponInSlot(int slotIndex, MonoBehaviour weaponPrefab)
        {
            MonoBehaviour currentWeapon = GetWeaponComponent(slotIndex);
            if (currentWeapon == null || weaponPrefab == null)
            {
                return false;
            }

            string currentName = currentWeapon.gameObject.name.Replace("(Clone)", string.Empty).Trim();
            return currentName == weaponPrefab.gameObject.name;
        }
        public void KeepOnlySlots(int firstSlotIndex, int secondSlotIndex)
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (i == firstSlotIndex || i == secondSlotIndex)
                {
                    continue;
                }

                if (slots[i] != null && slots[i].weapon != null)
                {
                    slots[i].weapon.gameObject.SetActive(false);
                    slots[i].weapon = null;
                }
            }
        }
        public bool ReplaceSlot(int slotIndex, MonoBehaviour weaponPrefab)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= GetPlayableSlotCount() || weaponPrefab == null)
            {
                return false;
            }

            Slot slot = slots[slotIndex];
            if (slot == null)
            {
                slot = new Slot();
                slots[slotIndex] = slot;
            }

            MonoBehaviour previousWeapon = slot.weapon;
            Transform parent = previousWeapon != null ? previousWeapon.transform.parent : transform;
            Vector3 localPosition = previousWeapon != null ? previousWeapon.transform.localPosition : Vector3.zero;
            Quaternion localRotation = previousWeapon != null ? previousWeapon.transform.localRotation : Quaternion.identity;
            Vector3 localScale = previousWeapon != null ? previousWeapon.transform.localScale : Vector3.one;

            if (previousWeapon != null)
            {
                previousWeapon.gameObject.SetActive(false);
            }

            MonoBehaviour newWeapon = FindExistingWeapon(weaponPrefab, previousWeapon);
            if (newWeapon == null)
            {
                newWeapon = weaponPrefab.gameObject.scene.IsValid()
                    ? weaponPrefab
                    : Instantiate(weaponPrefab, parent);

                if (newWeapon.transform.parent != parent)
                {
                    newWeapon.transform.SetParent(parent);
                }

                newWeapon.transform.localPosition = localPosition;
                newWeapon.transform.localRotation = localRotation;
                newWeapon.transform.localScale = localScale;
            }

            newWeapon.gameObject.SetActive(false);
            slot.weapon = newWeapon;
if (CurrentSlotIndex == slotIndex)
            {
                SetSlotActive(slotIndex, true);
            }

            return true;
        }
        private MonoBehaviour FindExistingWeapon(MonoBehaviour weaponPrefab, MonoBehaviour excludedWeapon)
        {
            string prefabName = weaponPrefab.gameObject.name.Replace("(Clone)", string.Empty).Trim();
            MonoBehaviour[] children = GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < children.Length; i++)
            {
                MonoBehaviour child = children[i];
                if (child == null || child == excludedWeapon || child == this ||
                    child.gameObject.name.Replace("(Clone)", string.Empty).Trim() != prefabName)
                {
                    continue;
                }

                if (child is IWeapon)
                {
                    return child;
                }
            }

            return null;
        }
        private void SetSlotActive(int index, bool active)
        {
            if (index < 0 || index >= GetPlayableSlotCount())
            {
                return;
            }

            Slot slot = slots[index];
            if (slot == null || slot.weapon == null)
            {
                return;
            }

            slot.weapon.gameObject.SetActive(active);

            if (active && slot.weapon is IWeapon equippedWeapon)
            {
                equippedWeapon.BeginEquip();
            }
        }

        private int GetPlayableSlotCount()
        {
            return slots == null ? 0 : Mathf.Min(2, slots.Length);
        }

        private void ClearUnsupportedSlots()
        {
            int firstUnsupportedSlot = GetPlayableSlotCount();
            for (int i = firstUnsupportedSlot; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].weapon != null)
                {
                    slots[i].weapon.gameObject.SetActive(false);
                    slots[i].weapon = null;
                }
            }
        }

        private int GetNextValidIndex(int direction)
        {
            int playableSlotCount = GetPlayableSlotCount();
            if (playableSlotCount == 0)
            {
                return -1;
            }

            int index = CurrentSlotIndex;
            for (int i = 0; i < playableSlotCount; i++)
            {
                index = (index + direction + playableSlotCount) % playableSlotCount;
                if (slots[index] != null && slots[index].weapon != null)
                {
                    return index;
                }
            }

            return CurrentSlotIndex;
        }
    }
}
