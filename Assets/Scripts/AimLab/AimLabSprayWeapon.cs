using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace FpsPractice.FPS
{
    public class AimLabSprayWeapon : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Camera weaponCamera;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private Transform firePoint;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private AimLabModeManager modeManager;
        [SerializeField] private Vector3 muzzleFlashRotationOffsetEuler = new Vector3(0f, 180f, 0f);

        [Header("Rendering")]
        [SerializeField] private string weaponLayerName = "Weapon";
        [SerializeField] private float weaponFieldOfView = 60f;

        [Header("Shooting")]
        [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;
        [SerializeField] private float fireRate = 0.09f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float range = 150f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float recoilPitchPerShot = 0.55f;

        [Header("Motion")]
        [SerializeField] private float weaponMotionSmoothing = 14f;
        [SerializeField] private float weaponSwayPosition = 0.01f;
        [SerializeField] private float weaponSwayRotation = 1.25f;
        [SerializeField] private float weaponKickPosition = 0.035f;
        [SerializeField] private float weaponKickRotation = 3.25f;

        [Header("Impact")]
        [Tooltip("벽이나 오브젝트에 닿았을 때 표시할 탄흔 프리팹입니다. 비어 있으면 표시하지 않습니다.")]
        [SerializeField] private GameObject impactPrefab;
        [Tooltip("탄흔이 표면에서 살짝 뜨는 오프셋입니다.")]
        [SerializeField] private float impactSurfaceOffset = 0.01f;
        [Tooltip("탄흔 프리팹 회전 보정값입니다.")]
        [SerializeField] private Vector3 impactRotationOffsetEuler = Vector3.zero;
        [Tooltip("탄흔이 사라지기까지의 시간입니다.")]
        [SerializeField] private float impactLifetime = 8f;

        private float nextFireTime;
        private Transform motionRoot;
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 currentSwayPosition;
        private Vector3 currentSwayRotation;
        private Vector3 currentKickPosition;
        private Vector3 currentKickRotation;
        private Vector3 lastCameraLocalEuler;

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = GetComponentInParent<Camera>();
            }

            if (modeManager == null)
            {
                modeManager = FindFirstObjectByType<AimLabModeManager>(FindObjectsInactive.Include);
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }

            motionRoot = transform;
            baseLocalPosition = motionRoot.localPosition;
            baseLocalRotation = motionRoot.localRotation;
            lastCameraLocalEuler = aimCamera != null ? aimCamera.transform.localEulerAngles : Vector3.zero;

            SetupRendering();
        }

        private void OnEnable()
        {
            ResetMotionState();
        }

        private void Update()
        {
            if (aimCamera == null || !Input.GetKey(fireKey) || Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + fireRate;
            Fire();
        }

        private void LateUpdate()
        {
            SyncWeaponCamera();
            UpdateWeaponMotion();
        }

        private void SetupRendering()
        {
            int weaponLayer = LayerMask.NameToLayer(weaponLayerName);
            if (weaponLayer < 0 || aimCamera == null)
            {
                return;
            }

            SetLayerRecursively(transform, weaponLayer);
            aimCamera.cullingMask &= ~(1 << weaponLayer);
            weaponCamera = EnsureWeaponCamera(weaponLayer);
        }

        private Camera EnsureWeaponCamera(int weaponLayer)
        {
            if (weaponCamera != null)
            {
                return weaponCamera;
            }

            Transform cameraRoot = aimCamera != null ? aimCamera.transform : transform;
            GameObject cameraObject = new GameObject("AimLab Weapon Camera");
            cameraObject.transform.SetParent(cameraRoot, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.transform.localScale = Vector3.one;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.cullingMask = 1 << weaponLayer;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = aimCamera.farClipPlane;
            camera.fieldOfView = Mathf.Clamp(weaponFieldOfView, 1f, 179f);
            camera.allowHDR = aimCamera.allowHDR;
            camera.allowMSAA = aimCamera.allowMSAA;
            camera.depth = aimCamera.depth + 1f;

            UniversalAdditionalCameraData baseData = aimCamera.GetComponent<UniversalAdditionalCameraData>();
            if (baseData != null)
            {
                baseData.renderType = CameraRenderType.Base;
            }

            UniversalAdditionalCameraData overlayData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            if (overlayData == null)
            {
                overlayData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            }

            overlayData.renderType = CameraRenderType.Overlay;

            if (baseData != null && baseData.cameraStack != null && !baseData.cameraStack.Contains(camera))
            {
                baseData.cameraStack.Add(camera);
            }

            return camera;
        }

        private void SyncWeaponCamera()
        {
            if (aimCamera == null || weaponCamera == null)
            {
                return;
            }

            weaponCamera.transform.position = aimCamera.transform.position;
            weaponCamera.transform.rotation = aimCamera.transform.rotation;
            weaponCamera.nearClipPlane = Mathf.Max(0.01f, aimCamera.nearClipPlane);
            weaponCamera.farClipPlane = aimCamera.farClipPlane;
        }

        private void UpdateWeaponMotion()
        {
            if (motionRoot == null || aimCamera == null)
            {
                return;
            }

            Vector3 currentCameraEuler = aimCamera.transform.localEulerAngles;
            Vector3 eulerDelta = new Vector3(
                Mathf.DeltaAngle(lastCameraLocalEuler.x, currentCameraEuler.x),
                Mathf.DeltaAngle(lastCameraLocalEuler.y, currentCameraEuler.y),
                Mathf.DeltaAngle(lastCameraLocalEuler.z, currentCameraEuler.z));

            lastCameraLocalEuler = currentCameraEuler;

            Vector3 targetSwayPosition = new Vector3(-eulerDelta.y, eulerDelta.x, 0f) * weaponSwayPosition;
            Vector3 targetSwayRotation = new Vector3(-eulerDelta.x, eulerDelta.y, -eulerDelta.y) * weaponSwayRotation;

            currentSwayPosition = Vector3.Lerp(currentSwayPosition, targetSwayPosition, Time.deltaTime * weaponMotionSmoothing);
            currentSwayRotation = Vector3.Lerp(currentSwayRotation, targetSwayRotation, Time.deltaTime * weaponMotionSmoothing);
            currentKickPosition = Vector3.Lerp(currentKickPosition, Vector3.zero, Time.deltaTime * weaponMotionSmoothing);
            currentKickRotation = Vector3.Lerp(currentKickRotation, Vector3.zero, Time.deltaTime * weaponMotionSmoothing);

            motionRoot.localPosition = baseLocalPosition + currentSwayPosition + currentKickPosition;
            motionRoot.localRotation = baseLocalRotation * Quaternion.Euler(currentSwayRotation + currentKickRotation);
        }

        private void Fire()
        {
            if (playerLook != null)
            {
                playerLook.AddRecoil(recoilPitchPerShot, 0f);
            }

            currentKickPosition += new Vector3(0f, 0f, -weaponKickPosition);
            currentKickRotation += new Vector3(weaponKickRotation, Random.Range(-0.35f, 0.35f), 0f);

            if (modeManager != null)
            {
                modeManager.ReportShotFired();
            }

            SpawnMuzzleFlash();
            PlayFireSound();

            Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
            Vector3 hitPoint = ray.origin + ray.direction * range;

            RaycastHit[] hits = Physics.RaycastAll(ray, range, hitMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (IsSelfHit(hit.collider))
                {
                    continue;
                }

                hitPoint = hit.point;
                bool hitDamageable = false;
                Health health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage, hit.point, hit.normal);
                    hitDamageable = true;
                }
                else
                {
                    MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
                    for (int k = 0; k < behaviours.Length; k++)
                    {
                        if (behaviours[k] is IDamageable damageable)
                        {
                            damageable.TakeDamage(damage, hit.point, hit.normal);
                            hitDamageable = true;
                            break;
                        }
                    }
                }

                if (!hitDamageable && hit.collider.GetComponentInParent<TargetDummy>() == null)
                {
                    SpawnImpact(hit.point, hit.normal);
                }

                break;
            }

            Debug.DrawLine(firePoint.position, hitPoint, Color.yellow, 0.08f);
        }

        private bool IsSelfHit(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return true;
            }

            Transform ownerRoot = transform.root;
            Transform hitRoot = hitCollider.transform.root;
            return hitRoot == ownerRoot || hitCollider.transform.IsChildOf(ownerRoot);
        }

        private void SpawnImpact(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (impactPrefab == null)
            {
                return;
            }

            Vector3 position = hitPoint + hitNormal * impactSurfaceOffset;
            Quaternion rotation = Quaternion.LookRotation(hitNormal) * Quaternion.Euler(impactRotationOffsetEuler);
            ImpactPool.Spawn(impactPrefab, position, rotation, Mathf.Max(0.05f, impactLifetime));
        }

        private void ResetMotionState()
        {
            if (motionRoot == null)
            {
                return;
            }

            currentSwayPosition = Vector3.zero;
            currentSwayRotation = Vector3.zero;
            currentKickPosition = Vector3.zero;
            currentKickRotation = Vector3.zero;
            motionRoot.localPosition = baseLocalPosition;
            motionRoot.localRotation = baseLocalRotation;
            lastCameraLocalEuler = aimCamera != null ? aimCamera.transform.localEulerAngles : Vector3.zero;
        }

        private void SpawnMuzzleFlash()
        {
            if (muzzleFlash == null || firePoint == null)
            {
                return;
            }

            if (!muzzleFlash.gameObject.scene.IsValid())
            {
                ParticleSystem instance = Instantiate(muzzleFlash, firePoint, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.Euler(muzzleFlashRotationOffsetEuler);
                instance.Play(true);
                Destroy(instance.gameObject, Mathf.Max(0.05f, instance.main.duration + instance.main.startLifetime.constantMax));
                return;
            }

            muzzleFlash.transform.SetParent(firePoint, false);
            muzzleFlash.transform.localPosition = Vector3.zero;
            muzzleFlash.transform.localRotation = Quaternion.Euler(muzzleFlashRotationOffsetEuler);
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play(true);
        }

        private void PlayFireSound()
        {
            if (audioSource != null && fireClip != null)
            {
                audioSource.PlayOneShot(fireClip);
            }
        }


        private bool IsFireBlockedByUI()
        {
            if (EventSystem.current == null)
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
            return keyCode == KeyCode.Mouse0
                || keyCode == KeyCode.Mouse1
                || keyCode == KeyCode.Mouse2
                || keyCode == KeyCode.Mouse3
                || keyCode == KeyCode.Mouse4
                || keyCode == KeyCode.Mouse5
                || keyCode == KeyCode.Mouse6;
        }        private void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;

            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }
    }
}









