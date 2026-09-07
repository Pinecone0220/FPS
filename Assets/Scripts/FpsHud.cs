using UnityEngine;
using TMPro;

namespace FpsPractice.FPS
{
    public class FpsHud : MonoBehaviour
    {
        [SerializeField] private WeaponLoadout weaponLoadout;
        [SerializeField] private MonoBehaviour weaponSource;
        [SerializeField] private Health health;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text healthText;

        private IWeapon currentWeapon;

        private void Awake()
        {
            if (weaponLoadout == null)
            {
                weaponLoadout = GetComponentInParent<WeaponLoadout>();
            }

            if (weaponSource == null)
            {
                MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IWeapon)
                    {
                        weaponSource = behaviours[i];
                        break;
                    }
                }
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            RefreshWeaponReference();
        }

        private void Update()
        {
            RefreshWeaponReference();

            if (ammoText != null && currentWeapon != null && currentWeapon.ShouldShowAmmo)
            {
                ammoText.gameObject.SetActive(true);
                if (currentWeapon is Weapon weapon)
                {
                    ammoText.text = weapon.CurrentAmmo + " / " + (weapon.HasInfiniteReserveAmmo ? "INF" : weapon.ReserveAmmo.ToString());
                }
                else
                {
                    ammoText.SetText("{0}", currentWeapon.CurrentAmmo);
                }
            }
            else if (ammoText != null)
            {
                ammoText.gameObject.SetActive(false);
            }

            if (healthText != null && health != null)
            {
                float healthRatio = Mathf.Clamp01(health.CurrentHealth / Mathf.Max(1f, health.MaxHealth));
                Color healthColor = Color.Lerp(Color.red, Color.green, healthRatio);
                string healthColorHex = ColorUtility.ToHtmlStringRGB(healthColor);
                healthText.text = "<color=#" + healthColorHex + ">" + Mathf.CeilToInt(health.CurrentHealth)
                    + "</color> / <color=#3399FF>" + Mathf.CeilToInt(health.Armor) + "</color>";
            }
        }

        private void RefreshWeaponReference()
        {
            if (weaponLoadout != null && weaponLoadout.CurrentWeapon != null)
            {
                currentWeapon = weaponLoadout.CurrentWeapon;
                return;
            }

            currentWeapon = weaponSource as IWeapon;
        }
    }
}
