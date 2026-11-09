using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using MainModule = UnityEngine.ParticleSystem.MainModule;
using MinMaxCurve = UnityEngine.ParticleSystem.MinMaxCurve;

namespace FpsPractice.FPS{

public class Weapon : MonoBehaviour, IWeapon
{
	public enum WeaponType
	{
		Standard,
		Shotgun,
		Sniper
	}

	public enum ReloadType
	{
		Magazine,
		IndividualShell
	}

	public enum FireMode
	{
		Automatic,
		SemiAutomatic
	}

	[Header("Weapon")]
	[Tooltip("무기 종류를 선택합니다. 발사 방식은 Fire Mode에서 따로 설정합니다.")]
	[SerializeField]
	private WeaponType weaponType;

	[Tooltip("샷건이 한 번 발사할 때 나가는 펠릿 수입니다.")]
	[SerializeField]
	private int shotgunPelletCount;

	[Tooltip("샷건 펠릿이 퍼지는 최대 각도입니다.")]
	[SerializeField]
	private float shotgunSpreadAngle;

	[Tooltip("샷건 펠릿 하나가 입히는 대미지입니다.")]
	[SerializeField]
	private float shotgunPelletDamage;

	[SerializeField]
	private Camera weaponCamera;

	[Tooltip("\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\u07fb\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd ó\ufffd\ufffd\ufffd\ufffd ī\ufffd\u07b6\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private Camera viewModelCamera;

	[Tooltip("\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\u05b4\ufffd ī\ufffd\u07b6\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffdþ\u07f0\ufffd\ufffdԴϴ\ufffd. \ufffd\ufffd\ufffd\ufffd FOV\ufffd\ufffd \ufffdٲ㵵 \ufffd\ufffd \ufffd\ufffdġ\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd鸳\ufffdϴ\ufffd.")]
	[SerializeField]
	private float viewModelFieldOfView;

	[Tooltip("ź \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd۵Ǵ\ufffd \ufffd\ufffdġ\ufffdԴϴ\ufffd. \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd ī\ufffd\u07b6\ufffd \ufffd\ufffdġ\ufffd\ufffd \ufffd\ufffd\ufffd\ufffdմϴ\ufffd.")]
	[SerializeField]
	private Transform tracerSpawnPoint;

	[Tooltip("ź \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private GameObject tracerPrefab;

	[SerializeField]
	private string playerLayerName;

	[SerializeField]
	private string weaponLayerName;

	[SerializeField]
	private float damage;

	[SerializeField]
	private float range;

	[Tooltip("Ʈ\ufffd\ufffd\ufffd\u033c\ufffd\ufffd\ufffd ȭ\ufffd鿡 \ufffd\ufffd\ufffd\ufffd \ufffd\u05b4\ufffd \ufffdð\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private float tracerLifetime;

	[Tooltip("Ʈ\ufffd\ufffd\ufffd\u033c\ufffd\ufffd\ufffd \ufffd\ufffdǥ \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\u0335\ufffd\ufffdϴ\ufffd \ufffdӵ\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private float tracerSpeed;

	[Header("Impact")]
	[Tooltip("\ufffd\ufffd\ufffd\u0333\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffdƮ\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd ǥ\ufffd\ufffd\ufffd\ufffd ź\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffdԴϴ\ufffd. \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd ǥ\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffdʽ\ufffd\ufffdϴ\ufffd.")]
	[SerializeField]
	private GameObject impactPrefab;

	[Tooltip("ź\ufffd\ufffd\ufffd\ufffd ǥ\ufffd鿡\ufffd\ufffd \ufffd\ufffd¦ \ufffdߴ\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private float impactSurfaceOffset;

	[Tooltip("ź\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd ȸ\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private Vector3 impactRotationOffsetEuler;

	[Tooltip("ź\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffdð\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private float impactLifetime;

	[Header("Casing Ejection")]
	[Tooltip("탄피가 생성되는 위치입니다. 보통 총의 탄피 배출구에 배치합니다.")]
	[SerializeField]
	private Transform casingEjectPoint;

	[Tooltip("탄두가 없는 탄피 프리팹입니다. Rigidbody와 Collider를 포함해야 바닥에 떨어집니다.")]
	[SerializeField]
	private GameObject casingPrefab;

	[Tooltip("탄피가 배출되는 로컬 방향과 힘입니다. X가 오른쪽, Y가 위쪽 배출 힘입니다.")]
	[SerializeField]
	private Vector3 casingEjectForce;

	[Tooltip("탄피가 씬에 남아있는 시간입니다. 최대 30개가 순서대로 재사용됩니다.")]
	[SerializeField]
	private float casingLifetime;

	[Tooltip("탄피 회전력의 최소값입니다. 각 축별로 지정합니다.")]
	[SerializeField]
	private Vector3 casingTorqueMin;

	[Tooltip("탄피 회전력의 최대값입니다. 각 축별로 지정합니다.")]
	[SerializeField]
	private Vector3 casingTorqueMax;

	[SerializeField]
	private float fireRate;
	private float damageMultiplier = 1f;
	private float fireRateMultiplier = 1f;
	private float magazineCapacityMultiplier = 1f;
	private bool infiniteReserveAmmo;
	[Header("Ammo")]
[Tooltip("일반 무기의 최대 예비 탄약 수입니다.")]
[SerializeField]
private int maxReserveAmmo = 90;

[Tooltip("샷건의 최대 예비 쉘 수입니다. 샷건일 때만 사용합니다.")]
[SerializeField]
private int maxReserveShells = 24;

[Tooltip("탄창에 들어가는 최대 탄약 수입니다.")]
[SerializeField]
private int magazineSize;

	[SerializeField]
	private float reloadTime;

	[Tooltip("Reload duration when the magazine is completely empty.")]
	[SerializeField]
	private float reloadEmptyTime;

	[Tooltip("재장전 방식입니다. Individual Shell은 한 발씩 장전합니다.")]
	[SerializeField]
	private ReloadType reloadType;

	[SerializeField]
	private LayerMask hitMask;

	[Header("Input")]
	[SerializeField]
	private KeyCode fireKey;

	[SerializeField]
	private KeyCode reloadKey;

	[SerializeField]
	private FireMode fireMode;

	[Header("Aim")]
	[Tooltip("Hold or toggle the aim key.")]
	[SerializeField]
	private KeyCode aimKey;

	[Tooltip("When enabled, press the aim key once to toggle aiming. When disabled, hold it to aim.")]
	[SerializeField]
	private bool aimToggle;

	[Tooltip("Aim zoom multiplier. 1 means no zoom, 1.5 means 1.5x zoom.")]
	[SerializeField]
	private float aimZoomMultiplier;

	[Tooltip("How quickly the camera changes to the aim field of view.")]
	[SerializeField]
	private float aimTransitionSpeed;

	[Tooltip("정조준 시 조준기를 화면 중앙에 맞추기 위한 무기별 위치 보정입니다.")]
	[SerializeField] private Vector3 aimPositionOffset;

	[Tooltip("정조준 시 가늠자와 가늠쇠의 시선을 맞추는 무기별 회전 보정입니다.")]
	[SerializeField] private Vector3 aimRotationOffsetEuler;

	[Tooltip("Hip-fire bullet spread in degrees. Set this to 0 for perfectly accurate hip-fire.")]
	[SerializeField]
	private float hipSpreadAngle;

	[Tooltip("Aim spread in degrees. Keep this at 0 for center-screen accuracy while aiming.")]
	[SerializeField]
	private float aimSpreadAngle;

	[Tooltip("Crosshair object shown outside of aiming.")]
	[SerializeField]
	private GameObject crosshairObject;

	[Header("Recoil")]
	[Tooltip("Upward camera kick per hip-fire shot.")]
	[SerializeField]
	private float hipRecoilPitch;

	[Tooltip("Maximum random horizontal camera kick per hip-fire shot.")]
	[SerializeField]
	private float hipRecoilYaw;

	[Header("Hip-fire State Recoil")]
	[SerializeField]
	private float crouchRecoilPitch;

	[SerializeField]
	private float crouchRecoilYaw;

	[SerializeField]
	private float movingRecoilPitch;

	[SerializeField]
	private float movingRecoilYaw;

	[SerializeField]
	private float airborneRecoilPitch;

	[SerializeField]
	private float airborneRecoilYaw;

	[Tooltip("Upward camera kick per aimed shot.")]
	[SerializeField]
	private float aimRecoilPitch;

	[Tooltip("Horizontal camera kick per aimed shot. Keep this at 0 for upward-only aim recoil.")]
	[SerializeField]
	private float aimRecoilYaw;

	[Header("Equip")]
	[Tooltip("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffdٲ\ufffd \ufffd\ufffd \ufffd\u07fb\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffdɸ\ufffd\ufffd\ufffd \ufffdð\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private float equipTime;

	[Tooltip("\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\u0335\ufffd\ufffdӵ\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffdԴϴ\ufffd. \ufffd\u05b9\ufffd\ufffd\ufffd\ufffd\ufffd -1, \ufffdι\ufffd\ufffd\ufffd\ufffd\ufffd -0.5 \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffdմϴ\ufffd.")]
	[SerializeField]
	private float movementSpeedModifier;

	[Header("Feedback")]
	[SerializeField]
	private ParticleSystem muzzleFlash;

	[Tooltip("\ufffd\ufffd\ufffd\ufffd \ufffd÷\ufffd\ufffdø\ufffd \ufffd\ufffd\ufffd\ufffd FirePoint\ufffdԴϴ\ufffd. \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffdڵ\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffdմϴ\ufffd.")]
	[SerializeField]
	private Transform firePoint;

	[Tooltip("총구 위치가 따라갈 무기 애니메이션 본입니다.")]
	[SerializeField] private Transform muzzleAnchorBone;
	[Tooltip("무기 본 기준 총구 위치입니다.")]
	[SerializeField] private Vector3 muzzleAnchorOffset;

	[Tooltip("\ufffd\ufffd\ufffd\ufffd \ufffd÷\ufffd\ufffdð\ufffd \ufffd\u07fb\ufffd\ufffdϴ\ufffd \ufffd\ufffdġ\ufffdԴϴ\ufffd. \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd ź \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\u0333\ufffd \ufffd\ufffd \ufffd\ufffdġ\ufffd\ufffd \ufffd\ufffd\ufffd\ufffdմϴ\ufffd.")]
	[SerializeField]
	private Transform muzzleFlashSpawnPoint;

	[Tooltip("\ufffd\ufffd\ufffd\ufffd \ufffd÷\ufffd\ufffdð\ufffd \ufffdݴ\ufffd\ufffd \ufffd\ufffd\ufffd\u0338\ufffd \ufffd\ufffd ȸ\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffdϼ\ufffd\ufffd\ufffd. \ufffd\ufffd\ufffd\ufffd Y\ufffd\ufffd 180\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Ȯ\ufffd\ufffd\ufffdϸ\ufffd \ufffd\u02f4ϴ\ufffd.")]
	[SerializeField]
	private Vector3 muzzleFlashRotationOffsetEuler;

	[SerializeField]
	private AudioSource audioSource;

[SerializeField]
	private AudioClip fireClip;



	[Header("Sound Alert")]
	[Tooltip("발사 시 이 반경 안에 있는 좀비를 경보 상태로 만듭니다. 0이면 소음이 발생하지 않습니다.")]
	[SerializeField]
	private float gunshotNoiseRadius = 20f;

	[Tooltip("Sound played when the weapon draw animation starts.")]
	[SerializeField]
	private AudioClip equipClip;

	[Tooltip("장착 애니메이션 이벤트가 없는 무기만 장착 시 소리를 재생합니다.")]
	[SerializeField] private bool playEquipSoundWithoutAnimationEvent;

	[Tooltip("Sound played when the aim-in animation starts.")]
	[SerializeField]
	private AudioClip aimInClip;

	[Tooltip("Sound played when the aim-out animation starts.")]
	[SerializeField]
	private AudioClip aimOutClip;

	[Tooltip("Sound played when the magazine is removed.")]
	[SerializeField]
	private AudioClip reloadMagazineOutClip;

	[Tooltip("Sound played when the new magazine is inserted.")]
	[SerializeField]
	private AudioClip reloadMagazineInClip;

	[Tooltip("Sound played when an empty reload pulls the charging handle.")]
	[SerializeField]
	private AudioClip reloadChamberClip;

	[Tooltip("빈 탄창 재장전에서 슬라이드 또는 장전손잡이가 전진할 때 재생할 소리입니다.")]
	[SerializeField]
	private AudioClip reloadChamberReturnClip;

	[Tooltip("Optional light that flashes at the muzzle when firing.")]
	[SerializeField]
	private Light muzzleLight;

	[Tooltip("How long the muzzle light stays on.")]
	[SerializeField]
	private float muzzleLightDuration;

	[Header("Gizmo")]
	[Tooltip("\ufffd\ufffd\ufffd信\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\u0334\ufffd \ufffdǽð\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private Color aimGizmoColor;

	[Tooltip("\ufffd\ufffd\ufffd\ufffd \ufffd\u07fb\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\u0334\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd/\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffdԴϴ\ufffd.")]
	[SerializeField]
	private Color shotDebugColor;

	[SerializeField]
	private float shotDebugDuration;

	private float nextFireTime;

	private float equipReadyTime;

	private Vector3 baseLocalPosition;

	private float aimAlignmentWeight;

	private Quaternion baseLocalRotation;

	private Vector3 baseLocalScale;

	private CharacterController characterController;

	private PlayerLook playerLook;

	private PlayerController playerController;

	private Animator weaponAnimator;

	private bool isAiming;
	private bool inputPaused;
	private bool reserveAmmoConsumptionBlocked;

	private float defaultFieldOfView;

	private float aimFovVelocity;

	private bool crosshairDefaultActive;

	private float muzzleLightEndTime;

	private Coroutine reloadCoroutine;

	private const string AimAnimatorParameterName = "IsAiming";

	private const string FireAnimatorParameterName = "AimFire";

	private const string HipFireAnimatorParameterName = "Fire";

private const string SprintAnimatorParameterName = "IsSprinting";

	private const string AimStateName = "Aim";

	private const string AimFireStateName = "AimFire";

	private const string ReloadAnimatorParameterName = "Reload";

	private const string ReloadEmptyAnimatorParameterName = "ReloadEmpty";

	private const string ReloadingAnimatorParameterName = "IsReloading";

	private int aimAnimatorParameterHash;

	private int fireAnimatorParameterHash;

	private int hipFireAnimatorParameterHash;

private int sprintAnimatorParameterHash;

	private int reloadAnimatorParameterHash;

	private int reloadEmptyAnimatorParameterHash;

	private int reloadingAnimatorParameterHash;

	public WeaponType Type => weaponType;

	public bool IsShotgun => weaponType == WeaponType.Shotgun;

	public bool IsSniper => weaponType == WeaponType.Sniper;

	public int CurrentAmmo { get; private set; }

public int ReserveAmmo { get; private set; }

public int MaxReserveAmmo => weaponType == WeaponType.Shotgun ? maxReserveShells : maxReserveAmmo;

public int MagazineSize => Mathf.Max(1, Mathf.RoundToInt(magazineSize * magazineCapacityMultiplier));

	public bool IsReloading { get; private set; }

	public bool IsEquipped { get; private set; }

	public bool ShouldShowAmmo => true;

	public bool IsReadyToFire
	{
		get
		{
			if (IsEquipped && Time.time >= equipReadyTime)
			{
				return !IsReloading;
			}
			return false;
		}
	}

		public float MovementSpeedModifier => movementSpeedModifier;

	public int ReloadAmmoNeeded => IsReloading ? Mathf.Max(0, MagazineSize - CurrentAmmo) : 0;

	public int AmmoPurchaseLimit => MaxReserveAmmo + ReloadAmmoNeeded;

	public bool CanAddAmmo(int amount)
	{
		return !infiniteReserveAmmo && amount > 0 && ReserveAmmo < AmmoPurchaseLimit;
	}

		public bool HasInfiniteReserveAmmo => infiniteReserveAmmo;

	public void AddDamagePercent(float percent)
	{
		damageMultiplier += Mathf.Max(0f, percent) * 0.01f;
	}

	public void AddFireRatePercent(float percent)
	{
		fireRateMultiplier += Mathf.Max(0f, percent) * 0.01f;
	}

	public void AddMagazineCapacityPercent(float percent)
	{
		magazineCapacityMultiplier = Mathf.Max(0.1f, magazineCapacityMultiplier + percent * 0.01f);
		CurrentAmmo = Mathf.Min(CurrentAmmo, MagazineSize);
	}

	public void SetInfiniteReserveAmmo(bool enabled)
	{
		infiniteReserveAmmo = enabled;
		if (enabled)
		{
			ReserveAmmo = MaxReserveAmmo;
		}
	}

	public bool AddAmmo(int amount)
	{
		if (!CanAddAmmo(amount))
		{
			return false;
		}

		ReserveAmmo = Mathf.Min(AmmoPurchaseLimit, ReserveAmmo + amount);
		return true;
	}
private void Awake()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		characterController = ((Component)this).GetComponentInParent<CharacterController>();
		baseLocalPosition = ((Component)this).transform.localPosition;
		baseLocalRotation = ((Component)this).transform.localRotation;
		baseLocalScale = ((Component)this).transform.localScale;
		CurrentAmmo = MagazineSize;
		ReserveAmmo = MaxReserveAmmo;
		reserveAmmoConsumptionBlocked = SceneManager.GetActiveScene().name == "Lobby";
		if ((Object)(object)crosshairObject != (Object)null)
		{
			crosshairDefaultActive = crosshairObject.activeSelf;
		}
		playerLook = ((Component)this).GetComponentInParent<PlayerLook>();
		playerController = ((Component)this).GetComponentInParent<PlayerController>();
		if (weaponCamera == null)
		{
			weaponCamera = GetComponentInParent<Camera>();
			if (weaponCamera == null && playerLook != null)
			{
				weaponCamera = playerLook.GetComponentInChildren<Camera>();
			}
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if ((Object)(object)weaponAnimator == (Object)null)
		{
			weaponAnimator = ((Component)this).GetComponentInChildren<Animator>(true);
		}
		aimAnimatorParameterHash = Animator.StringToHash("IsAiming");
		fireAnimatorParameterHash = Animator.StringToHash("AimFire");
		hipFireAnimatorParameterHash = Animator.StringToHash("Fire");
sprintAnimatorParameterHash = Animator.StringToHash(SprintAnimatorParameterName);
		reloadAnimatorParameterHash = Animator.StringToHash("Reload");
		reloadEmptyAnimatorParameterHash = Animator.StringToHash("ReloadEmpty");
		reloadingAnimatorParameterHash = Animator.StringToHash("IsReloading");
		if ((Object)(object)weaponCamera != (Object)null)
		{
			defaultFieldOfView = weaponCamera.fieldOfView;
		}
		EnsureFirePoint();
	}

	private void ResetWeaponPose()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).transform.localPosition = baseLocalPosition;
		aimAlignmentWeight = 0f;
		((Component)this).transform.localRotation = baseLocalRotation;
		((Component)this).transform.localScale = baseLocalScale;
		if ((Object)(object)weaponAnimator != (Object)null)
		{
			weaponAnimator.Rebind();
			weaponAnimator.Update(0f);
		}
	}

	private void OnEnable()
	{
		ResetWeaponPose();
		SetupFirstPersonRendering();
		if ((Object)(object)crosshairObject != (Object)null)
		{
			crosshairObject.SetActive(crosshairDefaultActive);
		}
		BeginEquip();
	}

	private void OnDisable()
	{
		IsEquipped = false;
		IsReloading = false;
		ReserveAmmo = Mathf.Min(MaxReserveAmmo, ReserveAmmo);
		isAiming = false;
		if ((Object)(object)crosshairObject != (Object)null)
		{
			crosshairObject.SetActive(crosshairDefaultActive);
		}
		RestoreDefaultFieldOfView();
	}

	private void Update()
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)muzzleLight != (Object)null && ((Behaviour)muzzleLight).enabled && Time.time >= muzzleLightEndTime)
		{
			((Behaviour)muzzleLight).enabled = false;
		}
		if ((Object)(object)weaponCamera == (Object)null || !IsEquipped)
		{
			return;
		}
		if (inputPaused)
		{
			return;
		}
		UpdateAimState();
		SetSprintAnimation();
		if (IsReloading)
		{
			if (IsIndividualShellReload() && Input.GetKeyDown(fireKey))
			{
				CancelReload();
			}
		}
		else if (IsReadyToFire)
		{
			if (Input.GetKeyDown(reloadKey) && CurrentAmmo < MagazineSize && (ReserveAmmo > 0 || infiniteReserveAmmo || IsReserveAmmoConsumptionBlocked()))
			{
				reloadCoroutine = ((MonoBehaviour)this).StartCoroutine(Reload());
			}
			else if ((playerController == null || !playerController.IsSprinting) && ((fireMode == FireMode.Automatic) ? Input.GetKey(fireKey) : Input.GetKeyDown(fireKey)) && !IsFireBlockedByUI())
			{
				TryFire();
			}
		}
	}

	private void LateUpdate()
	{
		if (aimPositionOffset != Vector3.zero || aimRotationOffsetEuler != Vector3.zero)
		{
			aimAlignmentWeight = Mathf.MoveTowards(aimAlignmentWeight,
				isAiming && !IsReloading ? 1f : 0f, Mathf.Max(0.01f, aimTransitionSpeed) * Time.deltaTime);
			transform.localPosition = baseLocalPosition + aimPositionOffset * aimAlignmentWeight;
			transform.localRotation = Quaternion.Slerp(baseLocalRotation,
				baseLocalRotation * Quaternion.Euler(aimRotationOffsetEuler), aimAlignmentWeight);
		}
		SyncViewModelCamera();
	}

	private void TryFire()
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		if (Time.time < nextFireTime || CurrentAmmo <= 0)
		{
			return;
		}
		nextFireTime = Time.time + fireRate / Mathf.Max(0.01f, fireRateMultiplier);
		CurrentAmmo--;
		SetFireAnimation();
		EjectCasing();
		Transform val = (((Object)(object)tracerSpawnPoint != (Object)null) ? tracerSpawnPoint : ((Component)weaponCamera).transform);
		if ((Object)(object)muzzleLight != (Object)null)
		{
			((Behaviour)muzzleLight).enabled = true;
			muzzleLightEndTime = Time.time + Mathf.Max(0.01f, muzzleLightDuration);
		}
		if ((Object)(object)muzzleFlash != (Object)null)
		{
			SpawnMuzzleFlash(val.position, ((Component)weaponCamera).transform.forward);
		}
		if ((Object)(object)audioSource != (Object)null && (Object)(object)fireClip != (Object)null)
		{
			audioSource.PlayOneShot(fireClip);
		}
		Vector3 noisePosition = ((Object)(object)firePoint != (Object)null) ? firePoint.position : val.position;
		GunshotNoise.Emit(noisePosition, gunshotNoiseRadius);
		int num = ((weaponType != WeaponType.Shotgun) ? 1 : Mathf.Max(1, shotgunPelletCount));
		float spreadAngle = ((weaponType == WeaponType.Shotgun) ? shotgunSpreadAngle : (isAiming ? aimSpreadAngle : hipSpreadAngle));
		float amount = ((weaponType == WeaponType.Shotgun) ? shotgunPelletDamage : damage) * damageMultiplier;
		Vector3 endPosition = ((Component)weaponCamera).transform.position + ((Component)weaponCamera).transform.forward * range;
		Ray val3 = default(Ray);
		for (int i = 0; i < num; i++)
		{
			Vector3 val2 = ApplySpread(GetShotDirection(), spreadAngle);
			val3 = new Ray(((Component)weaponCamera).transform.position, val2);
			Vector3 val4 = val3.origin + val3.direction * range;
			RaycastHit[] array = Physics.RaycastAll(val3, range, hitMask, (QueryTriggerInteraction)1);
			Array.Sort(array, (RaycastHit left, RaycastHit right) => left.distance.CompareTo(right.distance));
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				RaycastHit val5 = array[num2];
				if (IsSelfHit(val5.collider))
				{
					continue;
				}
				val4 = val5.point;
				DamageHitbox damageHitbox = val5.collider.GetComponent<DamageHitbox>();
                if (damageHitbox != null)
                {
                    damageHitbox.TakeDamage(amount, val5.point, val5.normal);
                }
                else
                {
                    MonoBehaviour[] componentsInParent = ((Component)val5.collider).GetComponentsInParent<MonoBehaviour>();
                    for (int num3 = 0; num3 < componentsInParent.Length; num3++)
                    {
                        if (componentsInParent[num3] is IDamageable damageable)
                        {
                            damageable.TakeDamage(amount, val5.point, val5.normal);
                            break;
                        }
                    }
                }TargetDummy componentInParent = ((Component)val5.collider).GetComponentInParent<TargetDummy>();
				if ((Object)(object)componentInParent != (Object)null)
				{
					componentInParent.FlashHit();
				}
				else
				{
					SpawnImpact(val5.point, val5.normal);
				}
				break;
			}
			if (i == 0)
			{
				endPosition = val4;
			}
			Debug.DrawLine(val3.origin, val4, shotDebugColor, shotDebugDuration);
		}
		SpawnTracer(val.position, endPosition);
		ApplyRecoil();
	}

	private void EjectCasing()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)casingPrefab == (Object)null) && !((Object)(object)casingEjectPoint == (Object)null))
		{
			Vector3 velocity = casingEjectPoint.TransformDirection(casingEjectForce);
			Vector3 val = default(Vector3);
			val = new Vector3(Random.Range(casingTorqueMin.x, casingTorqueMax.x), Random.Range(casingTorqueMin.y, casingTorqueMax.y), Random.Range(casingTorqueMin.z, casingTorqueMax.z));
			Vector3 angularVelocity = casingEjectPoint.TransformDirection(val);
			CasingPool.Spawn(casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation, velocity, angularVelocity, casingLifetime, ((Component)this).gameObject);
		}
	}

	private void UpdateAimState()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (IsReloading)
		{
			isAiming = false;
		}
		else if (aimToggle)
		{
			if (Input.GetKeyDown(aimKey))
			{
				isAiming = !isAiming;
			}
		}
		else
		{
			isAiming = Input.GetKey(aimKey);
		}
		if (!((Object)(object)weaponCamera == (Object)null))
		{
			SetAimAnimation(isAiming);
        if ((Object)(object)playerController != (Object)null)
        {
            playerController.SetWeaponAiming(isAiming);
        }
			if (!isAiming && (Object)(object)crosshairObject != (Object)null)
			{
				crosshairObject.SetActive(crosshairDefaultActive);
			}
			float num = Mathf.Max(1f, aimZoomMultiplier);
			float num2 = ((IsAimAnimationPlaying() && !IsReloading) ? Mathf.Clamp(defaultFieldOfView / num, 1f, 179f) : defaultFieldOfView);
			float num3 = 1f / Mathf.Max(0.01f, aimTransitionSpeed);
			weaponCamera.fieldOfView = Mathf.SmoothDamp(weaponCamera.fieldOfView, num2, ref aimFovVelocity, num3, float.PositiveInfinity, Time.deltaTime);
		}
	}

	private bool IsAimAnimationPlaying()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)weaponAnimator == (Object)null)
		{
			return false;
		}
		AnimatorStateInfo currentAnimatorStateInfo = weaponAnimator.GetCurrentAnimatorStateInfo(0);
		if (HasAnimatorBoolParameter(weaponAnimator, aimAnimatorParameterHash) && weaponAnimator.GetBool(aimAnimatorParameterHash))
		{
			return true;
		}
		if (currentAnimatorStateInfo.IsName("Aim") || currentAnimatorStateInfo.IsName("AimFire"))
		{
			return true;
		}
		if (weaponAnimator.IsInTransition(0))
		{
			AnimatorStateInfo nextAnimatorStateInfo = weaponAnimator.GetNextAnimatorStateInfo(0);
			if (!nextAnimatorStateInfo.IsName("Aim"))
			{
				return nextAnimatorStateInfo.IsName("AimFire");
			}
			return true;
		}
		return false;
	}

	public void HideCrosshair()
	{
		if ((Object)(object)crosshairObject != (Object)null)
		{
			crosshairObject.SetActive(false);
		}
	}

	private void SetSprintAnimation()
{
    if ((Object)(object)weaponAnimator == (Object)null || (Object)(object)playerController == (Object)null
        || !HasAnimatorParameter(weaponAnimator, sprintAnimatorParameterHash, (AnimatorControllerParameterType)4))
    {
        return;
    }

    bool sprinting = playerController.IsSprinting && !isAiming && !IsReloading;
    weaponAnimator.SetBool(sprintAnimatorParameterHash, sprinting);
}
private void SetAimAnimation(bool aiming)
	{
		if (!((Object)(object)weaponAnimator == (Object)null) && HasAnimatorBoolParameter(weaponAnimator, aimAnimatorParameterHash))
		{
			weaponAnimator.SetBool(aimAnimatorParameterHash, aiming);
		}
	}

	private static bool HasAnimatorBoolParameter(Animator animator, int parameterHash)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		for (int i = 0; i < animator.parameters.Length; i++)
		{
			AnimatorControllerParameter val = animator.parameters[i];
			if (val.nameHash == parameterHash && (int)val.type == 4)
			{
				return true;
			}
		}
		return false;
	}

	private void SetFireAnimation()
	{
		if (!((Object)(object)weaponAnimator == (Object)null))
		{
			int num = (isAiming ? fireAnimatorParameterHash : hipFireAnimatorParameterHash);
			if (HasAnimatorParameter(weaponAnimator, num, (AnimatorControllerParameterType)9))
			{
				weaponAnimator.SetTrigger(num);
			}
		}
	}

	private void SetReloadingAnimation(bool reloading)
	{
		if ((Object)(object)weaponAnimator != (Object)null && HasAnimatorParameter(weaponAnimator, reloadingAnimatorParameterHash, (AnimatorControllerParameterType)4))
		{
			weaponAnimator.SetBool(reloadingAnimatorParameterHash, reloading);
		}
	}

	private void SetReloadAnimation(bool emptyReload)
	{
		if (!((Object)(object)weaponAnimator == (Object)null))
		{
			int num = (emptyReload ? reloadEmptyAnimatorParameterHash : reloadAnimatorParameterHash);
			if (HasAnimatorParameter(weaponAnimator, num, (AnimatorControllerParameterType)9))
			{
				weaponAnimator.SetTrigger(num);
			}
		}
	}

	private static bool HasAnimatorParameter(Animator animator, int parameterHash, AnimatorControllerParameterType parameterType)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < animator.parameters.Length; i++)
		{
			AnimatorControllerParameter val = animator.parameters[i];
			if (val.nameHash == parameterHash && val.type == parameterType)
			{
				return true;
			}
		}
		return false;
	}

	private void RestoreDefaultFieldOfView()
	{
		if ((Object)(object)weaponCamera != (Object)null && defaultFieldOfView > 0f)
		{
			weaponCamera.fieldOfView = defaultFieldOfView;
		}
	}

	private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (spreadAngle <= 0f)
		{
			return direction.normalized;
		}
		Vector2 val = Random.insideUnitCircle * Mathf.Tan(spreadAngle * (MathF.PI / 180f));
		Vector3 right = ((Component)weaponCamera).transform.right;
		Vector3 up = ((Component)weaponCamera).transform.up;
		Vector3 val2 = direction + right * val.x + up * val.y;
		return val2.normalized;
	}

	private void ApplyRecoil()
	{
		if ((Object)(object)playerLook == (Object)null)
		{
			playerLook = ((Component)this).GetComponentInParent<PlayerLook>();
			playerController = ((Component)this).GetComponentInParent<PlayerController>();
		}
		if (!((Object)(object)playerLook == (Object)null))
		{
			if (isAiming)
			{
				playerLook.AddRecoil(aimRecoilPitch, aimRecoilYaw);
				return;
			}
			GetHipRecoil(out var pitch, out var yaw);
			float yawKick = Random.Range(0f - Mathf.Abs(yaw), Mathf.Abs(yaw));
			playerLook.AddRecoil(pitch, yawKick);
		}
	}

	private void GetHipRecoil(out float pitch, out float yaw)
	{
		pitch = hipRecoilPitch;
		yaw = hipRecoilYaw;
		if ((Object)(object)playerController == (Object)null)
		{
			playerController = ((Component)this).GetComponentInParent<PlayerController>();
		}
		if (!((Object)(object)playerController == (Object)null))
		{
			if (!playerController.IsGrounded)
			{
				pitch = airborneRecoilPitch;
				yaw = airborneRecoilYaw;
			}
			else if (playerController.IsCrouching)
			{
				pitch = crouchRecoilPitch;
				yaw = crouchRecoilYaw;
			}
			else if (playerController.IsMoving)
			{
				pitch = movingRecoilPitch;
				yaw = movingRecoilYaw;
			}
		}
	}

	private void SpawnTracer(Vector3 startPosition, Vector3 endPosition)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)tracerPrefab == (Object)null))
		{
			TracerTrail component = Object.Instantiate<GameObject>(tracerPrefab, startPosition, Quaternion.identity).GetComponent<TracerTrail>();
			if ((Object)(object)component != (Object)null)
			{
				component.Initialize(startPosition, endPosition, tracerLifetime, tracerSpeed);
			}
		}
	}

	private void SpawnImpact(Vector3 hitPoint, Vector3 hitNormal)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)impactPrefab == (Object)null))
		{
			Vector3 position = hitPoint + hitNormal * impactSurfaceOffset;
			Quaternion rotation = Quaternion.LookRotation(hitNormal) * Quaternion.Euler(impactRotationOffsetEuler);
			ImpactPool.Spawn(impactPrefab, position, rotation, Mathf.Max(0.05f, impactLifetime));
		}
	}

	private void SpawnMuzzleFlash(Vector3 position, Vector3 forward)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)muzzleFlash == (Object)null)
		{
			return;
		}
		Transform val = EnsureFirePoint();
		if (muzzleAnchorBone != null && val != null && weaponCamera != null)
		{
			val.rotation = Quaternion.LookRotation(forward, weaponCamera.transform.up);
		}
		Vector3 val2 = (((Object)(object)val != (Object)null) ? val.position : position);
		Quaternion val3 = (((Object)(object)val != (Object)null) ? val.rotation : Quaternion.LookRotation(forward, ((Component)weaponCamera).transform.up));
		Quaternion val4 = Quaternion.Euler(muzzleFlashRotationOffsetEuler);
		ParticleSystem val5 = muzzleFlash;
		int num = LayerMask.NameToLayer(weaponLayerName);
		Scene scene = ((Component)muzzleFlash).gameObject.scene;
		if (!scene.IsValid())
		{
			val5 = Object.Instantiate<ParticleSystem>(muzzleFlash, ((Object)(object)val != (Object)null) ? val : null, false);
			((Component)val5).transform.localPosition = Vector3.zero;
			((Component)val5).transform.localRotation = val4;
			if (num >= 0)
			{
				SetLayerRecursively(((Component)val5).transform, num);
			}
			val5.Play(true);
			Object.Destroy((Object)(object)((Component)val5).gameObject, GetMuzzleFlashDestroyDelay(val5));
			return;
		}
		Transform transform = ((Component)val5).transform;
		if ((Object)(object)val != (Object)null)
		{
			transform.SetParent(val, false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = val4;
		}
		else
		{
			transform.SetPositionAndRotation(val2, val3 * val4);
		}
		if (num >= 0)
		{
			SetLayerRecursively(transform, num);
		}
		if (!((Component)val5).gameObject.activeSelf)
		{
			((Component)val5).gameObject.SetActive(true);
		}
		val5.Stop(true, (ParticleSystemStopBehavior)0);
		val5.Play(true);
	}

	private float GetMuzzleFlashDestroyDelay(ParticleSystem flashInstance)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		MainModule main = flashInstance.main;
		float duration = main.duration;
		MinMaxCurve startLifetime = main.startLifetime;
		return Mathf.Max(0.05f, duration + startLifetime.constantMax);
	}

	public void PlayEquipSound()
	{
		PlayReloadSound(equipClip);
	}

	public void PlayAimInSound()
	{
		PlayReloadSound(aimInClip);
	}

	public void PlayAimOutSound()
	{
		PlayReloadSound(aimOutClip);
	}

	public void PlayReloadMagazineOutSound()
	{
		PlayReloadSound(reloadMagazineOutClip);
	}

	public void PlayReloadMagazineInSound()
	{
		PlayReloadSound(reloadMagazineInClip);
	}

	public void PlayReloadChamberSound()
	{
		PlayReloadSound(reloadChamberClip);
	}

	public void PlayReloadChamberReturnSound()
	{
		PlayReloadSound(reloadChamberReturnClip);
	}

	private void PlayReloadSound(AudioClip clip)
	{
		if ((Object)(object)audioSource != (Object)null && (Object)(object)clip != (Object)null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	private bool IsIndividualShellReload()
	{
		if (weaponType == WeaponType.Shotgun)
		{
			return reloadType == ReloadType.IndividualShell;
		}
		return false;
	}

	private void CancelReload()
	{
		if (reloadCoroutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(reloadCoroutine);
			reloadCoroutine = null;
		}
		IsReloading = false;
		ReserveAmmo = Mathf.Min(MaxReserveAmmo, ReserveAmmo);
		SetReloadingAnimation(reloading: false);
	}

	public void LoadOneShell()
	{
		if (IsReloading && IsIndividualShellReload() && CurrentAmmo < MagazineSize &&
			(ReserveAmmo > 0 || infiniteReserveAmmo || IsReserveAmmoConsumptionBlocked()))
		{
			CurrentAmmo++;
			if (!infiniteReserveAmmo && !IsReserveAmmoConsumptionBlocked())
			{
				ReserveAmmo--;
			}
		}
	}
private IEnumerator Reload()
{
    if (!IsReadyToFire)
    {
        yield break;
    }

    IsReloading = true;
    isAiming = false;
    SetAimAnimation(false);
    if (playerController != null)
    {
        playerController.SetWeaponAiming(false);
    }
    if (crosshairObject != null)
    {
        crosshairObject.SetActive(crosshairDefaultActive);
    }
    SetReloadingAnimation(reloading: true);
    if (IsIndividualShellReload())
    {
        SetReloadAnimation(CurrentAmmo <= 0);
        while (IsReloading && CurrentAmmo < MagazineSize && (ReserveAmmo > 0 || infiniteReserveAmmo || IsReserveAmmoConsumptionBlocked()))
        {
            yield return null;
        }
    }
    else
    {
        bool empty = CurrentAmmo <= 0;
        SetReloadAnimation(empty);
        float duration = Mathf.Max(0f, empty ? reloadEmptyTime : reloadTime);
        yield return new WaitForSeconds(duration);

        int ammoNeeded = MagazineSize - CurrentAmmo;
                int ammoToLoad = infiniteReserveAmmo || IsReserveAmmoConsumptionBlocked()
            ? ammoNeeded : Mathf.Min(ammoNeeded, ReserveAmmo);
        CurrentAmmo += ammoToLoad;
        if (!infiniteReserveAmmo && !IsReserveAmmoConsumptionBlocked())
        {
            ReserveAmmo -= ammoToLoad;
        }    }

    IsReloading = false;
    SetReloadingAnimation(reloading: false);
    reloadCoroutine = null;
}

private bool IsReserveAmmoConsumptionBlocked()
{
    if (reserveAmmoConsumptionBlocked)
    {
        return true;
    }

    WaveDefenseManager waveDefenseManager = FindFirstObjectByType<WaveDefenseManager>();
    return waveDefenseManager != null && waveDefenseManager.IsWaitingForNextWave;
}
public void SetReserveAmmoConsumptionBlocked(bool blocked)
        {
            reserveAmmoConsumptionBlocked = blocked;
        }

public void SetInputPaused(bool paused)
	{
		inputPaused = paused;
		if (paused)
		{
			isAiming = false;
			RestoreDefaultFieldOfView();
			if ((Object)(object)crosshairObject != (Object)null)
			{
				crosshairObject.SetActive(crosshairDefaultActive);
			}
		}
	}

public void BeginEquip()
	{
		IsEquipped = true;
		IsReloading = false;
		equipReadyTime = Time.time + Mathf.Max(0f, equipTime);
		if (playEquipSoundWithoutAnimationEvent)
		{
			PlayEquipSound();
		}
	}

	private Vector3 GetShotDirection()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)weaponCamera != (Object)null))
		{
			return ((Component)this).transform.forward;
		}
		return ((Component)weaponCamera).transform.forward;
	}

	private void SetupFirstPersonRendering()
	{
		Camera val = (((Object)(object)weaponCamera != (Object)null) ? weaponCamera : ((Component)this).GetComponentInParent<Camera>());
		if (!((Object)(object)val == (Object)null))
		{
			int num = LayerMask.NameToLayer(playerLayerName);
			int num2 = LayerMask.NameToLayer(weaponLayerName);
			if (num >= 0)
			{
				SetLayerRecursively(((Component)val).transform.root, num);
			}
			if (num2 >= 0)
			{
				SetLayerRecursively(((Component)this).transform, num2);
			}
			if (num >= 0)
			{
				val.cullingMask &= ~(1 << num);
			}
			if (num2 >= 0)
			{
				val.cullingMask &= ~(1 << num2);
				viewModelCamera = EnsureViewModelCamera(val, num2);
			}
		}
	}

	private Transform EnsureFirePoint()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)firePoint != (Object)null)
		{
			return firePoint;
		}
		if (muzzleAnchorBone != null)
		{
			Transform existing = muzzleAnchorBone.Find("FirePoint");
			if (existing != null)
			{
				firePoint = existing;
				return firePoint;
			}
			GameObject point = new GameObject("FirePoint");
			point.transform.SetParent(muzzleAnchorBone, false);
			point.transform.localPosition = muzzleAnchorOffset;
			Vector3 boneScale = muzzleAnchorBone.lossyScale;
			Vector3 weaponScale = transform.lossyScale;
			point.transform.localScale = new Vector3(
				Mathf.Abs(boneScale.x) > 0.00001f ? weaponScale.x / boneScale.x : 1f,
				Mathf.Abs(boneScale.y) > 0.00001f ? weaponScale.y / boneScale.y : 1f,
				Mathf.Abs(boneScale.z) > 0.00001f ? weaponScale.z / boneScale.z : 1f);
			firePoint = point.transform;
			return firePoint;
		}
		Transform val = (((Object)(object)muzzleFlashSpawnPoint != (Object)null) ? muzzleFlashSpawnPoint : tracerSpawnPoint);
		if ((Object)(object)val == (Object)null)
		{
			val = ((Component)this).transform;
		}
		Transform val2 = val.Find("FirePoint");
		if ((Object)(object)val2 != (Object)null)
		{
			firePoint = val2;
			return firePoint;
		}
		GameObject val3 = new GameObject("FirePoint");
		val3.transform.SetParent(val, false);
		val3.transform.localPosition = Vector3.zero;
		val3.transform.localRotation = Quaternion.identity;
		val3.transform.localScale = Vector3.one;
		firePoint = val3.transform;
		return firePoint;
	}

	private Camera EnsureViewModelCamera(Camera baseCamera, int weaponLayer)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		Transform transform = ((Component)baseCamera).transform;
		Camera val = viewModelCamera;
		if ((Object)(object)val == (Object)null)
		{
			Transform val2 = transform.Find("Weapon Camera");
			if ((Object)(object)val2 != (Object)null)
			{
				val = ((Component)val2).GetComponent<Camera>();
			}
			if ((Object)(object)val == (Object)null)
			{
				GameObject val3 = new GameObject("Weapon Camera");
				val3.transform.SetParent(transform, false);
				val3.transform.localPosition = Vector3.zero;
				val3.transform.localRotation = Quaternion.identity;
				val3.transform.localScale = Vector3.one;
				val = val3.AddComponent<Camera>();
			}
		}
		val.cullingMask = 1 << weaponLayer;
		val.clearFlags = (CameraClearFlags)3;
		val.nearClipPlane = 0.01f;
		val.farClipPlane = baseCamera.farClipPlane;
		val.fieldOfView = Mathf.Clamp(viewModelFieldOfView, 1f, 179f);
		val.allowHDR = baseCamera.allowHDR;
		val.allowMSAA = baseCamera.allowMSAA;
		val.depth = baseCamera.depth + 1f;
		UniversalAdditionalCameraData component = ((Component)baseCamera).GetComponent<UniversalAdditionalCameraData>();
		if ((Object)(object)component != (Object)null)
		{
			component.renderType = CameraRenderType.Base;
		}
		UniversalAdditionalCameraData universalAdditionalCameraData = ((Component)val).GetComponent<UniversalAdditionalCameraData>();
		if ((Object)(object)universalAdditionalCameraData == (Object)null)
		{
			universalAdditionalCameraData = ((Component)val).gameObject.AddComponent<UniversalAdditionalCameraData>();
		}
		universalAdditionalCameraData.renderType = CameraRenderType.Overlay;
		if ((Object)(object)component != (Object)null && component.cameraStack != null && !component.cameraStack.Contains(val))
		{
			component.cameraStack.Add(val);
		}
		return val;
	}

	private void SyncViewModelCamera()
	{
		if (!((Object)(object)weaponCamera == (Object)null) && !((Object)(object)viewModelCamera == (Object)null))
		{
			viewModelCamera.nearClipPlane = Mathf.Max(0.01f, weaponCamera.nearClipPlane);
			viewModelCamera.farClipPlane = weaponCamera.farClipPlane;
		}
	}

	private bool IsFireBlockedByUI()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)EventSystem.current == (Object)null)
		{
			return false;
		}
		if (!IsMouseFireKey(fireKey))
		{
			return false;
		}
		return EventSystem.current.IsPointerOverGameObject();
	}

	private static bool IsMouseFireKey(KeyCode keyCode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		if ((int)keyCode != 323 && (int)keyCode != 324 && (int)keyCode != 325 && (int)keyCode != 326 && (int)keyCode != 327 && (int)keyCode != 328)
		{
			return (int)keyCode == 329;
		}
		return true;
	}

	private void SetLayerRecursively(Transform root, int layer)
	{
		if (!((Object)(object)root == (Object)null))
		{
			((Component)root).gameObject.layer = layer;
			for (int i = 0; i < root.childCount; i++)
			{
				SetLayerRecursively(root.GetChild(i), layer);
			}
		}
	}

	private bool IsSelfHit(Collider hitCollider)
	{
		if ((Object)(object)hitCollider == (Object)null)
		{
			return true;
		}
		Transform root = ((Component)this).transform.root;
		if (!((Object)(object)((Component)hitCollider).transform.root == (Object)(object)root))
		{
			return ((Component)hitCollider).transform.IsChildOf(root);
		}
		return true;
	}

	public Weapon()
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		shotgunPelletCount = 8;
		shotgunSpreadAngle = 7f;
		shotgunPelletDamage = 10f;
		viewModelFieldOfView = 60f;
		playerLayerName = "Player";
		weaponLayerName = "Weapon";
		damage = 25f;
		range = 150f;
		tracerLifetime = 0.35f;
		tracerSpeed = 600f;
		impactSurfaceOffset = 0.01f;
		impactRotationOffsetEuler = Vector3.zero;
		impactLifetime = 8f;
		casingEjectForce = new Vector3(1.2f, 0.7f, 0f);
		casingLifetime = 8f;
		casingTorqueMin = new Vector3(-2f, -3f, -1f);
		casingTorqueMax = new Vector3(2f, 3f, 1f);
		fireRate = 0.12f;
		magazineSize = 30;
		reloadTime = 1.6f;
		reloadEmptyTime = 2.1f;
		hitMask = -1;
		fireKey = (KeyCode)323;
		reloadKey = (KeyCode)114;
		aimKey = (KeyCode)324;
		aimZoomMultiplier = 1.5f;
		aimTransitionSpeed = 12f;
		hipSpreadAngle = 3f;
		hipRecoilPitch = 1.2f;
		hipRecoilYaw = 0.45f;
		crouchRecoilPitch = 0.7f;
		crouchRecoilYaw = 0.2f;
		movingRecoilPitch = 1.8f;
		movingRecoilYaw = 0.7f;
		airborneRecoilPitch = 3f;
		airborneRecoilYaw = 1.2f;
		aimRecoilPitch = 0.65f;
		equipTime = 0.35f;
		movementSpeedModifier = -1f;
		muzzleFlashRotationOffsetEuler = new Vector3(0f, 180f, 0f);
		muzzleLightDuration = 0.05f;
		aimGizmoColor = Color.cyan;
		shotDebugColor = Color.yellow;
		shotDebugDuration = 0.1f;
		
	}
}
}
