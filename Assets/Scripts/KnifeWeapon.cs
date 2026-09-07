using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FpsPractice.FPS
{
    public class KnifeWeapon : MonoBehaviour, IWeapon
    {
        [Header("Weapon")]
        [SerializeField] private Camera attackCamera;
        [Tooltip("ì¹?ëª¨ë¸??ë³´ì—¬ì£¼ëŠ” ?„ìš© ì¹´ë©”?¼ìž…?ˆë‹¤. ë¹„ì–´ ?ˆìœ¼ë©??ë™?¼ë¡œ ?ì„±?©ë‹ˆ??")]
        [SerializeField] private Camera viewModelCamera;
        [SerializeField] private string playerLayerName = "Player";
        [SerializeField] private string weaponLayerName = "Weapon";
        [Tooltip("ì¹?ëª¨ë¸ ì¹´ë©”?¼ì˜ ê³ ì • ?œì•¼ê°ìž…?ˆë‹¤.")]
        [SerializeField] private float viewModelFieldOfView = 60f;
        [SerializeField] private Transform hitOrigin;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float hitRadius = 0.25f;

        [Header("Damage")]
        [SerializeField] private float lightDamage = 15f;
        [SerializeField] private float heavyDamage = 25f;
        [SerializeField] private float backstabMultiplier = 2f;
        [SerializeField] private bool heavyBackstabInstantKill = true;
        [SerializeField] private float backstabDotThreshold = 0.25f;

        [Header("Timing")]
        [SerializeField] private float equipTime = 0.2f;
        [SerializeField] private float lightAttackCooldown = 0.28f;
        [SerializeField] private float heavyAttackCooldown = 0.65f;

        [Header("Motion")]
        [Tooltip("Ä® ¸ðµ¨ÀÇ ¿òÁ÷ÀÓÀ» ¾ó¸¶³ª ºÎµå·´°Ô µû¶ó°¥Áö Á¤ÇÕ´Ï´Ù.")]
        [SerializeField] private float weaponMotionSmoothing = 18f;
        [Tooltip("¸¶¿ì½º ¿òÁ÷ÀÓ¿¡ µû¶ó Ä®ÀÌ ÁÂ¿ì·Î Èçµé¸®´Â Á¤µµÀÔ´Ï´Ù.")]
        [SerializeField] private float weaponSwayPositionAmount = 0.012f;
        [Tooltip("¸¶¿ì½º ¿òÁ÷ÀÓ¿¡ µû¶ó Ä®ÀÌ È¸ÀüÇÏ´Â Á¤µµÀÔ´Ï´Ù.")]
        [SerializeField] private float weaponSwayRotationAmount = 2f;
        [Tooltip("ÀÌµ¿ Áß Ä®ÀÌ À§¾Æ·¡·Î Èçµé¸®´Â Á¤µµÀÔ´Ï´Ù.")]
        [SerializeField] private float weaponBobAmount = 0.008f;
        [Tooltip("ÀÌµ¿ Áß Ä® Èçµé¸²ÀÇ ¼ÓµµÀÔ´Ï´Ù.")]
        [SerializeField] private float weaponBobSpeed = 11f;

        [Header("Input")]
        [SerializeField] private KeyCode lightAttackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode heavyAttackKey = KeyCode.Mouse1;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [Tooltip("?¥ì°© ? ë‹ˆë©”ì´???¸ë¦¬ê±??´ë¦„?…ë‹ˆ??")]
        [SerializeField] private string equipTrigger = "Equip";
        [SerializeField] private string lightAttackTrigger = "LightAttack";
        [SerializeField] private string heavyAttackTrigger = "HeavyAttack";

        [Header("Feedback")]
        [SerializeField] private Color debugColor = Color.green;
        [SerializeField] private float debugDuration = 0.08f;

        public int CurrentAmmo => 0;
        public bool ShouldShowAmmo => false;
        public bool IsReadyToFire => IsEquipped && Time.time >= equipReadyTime;
        public float MovementSpeedModifier => 0f;
        public bool IsEquipped { get; private set; }

        private float equipReadyTime;
        private float nextLightAttackTime;
        private float nextHeavyAttackTime;
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 currentMotionPosition;
        private Vector3 currentMotionRotation;
        private Vector3 currentKickbackPosition;
        private Vector3 currentKickbackRotation;
        private Vector2 smoothedLookInput;
        private float smoothedMoveAmount;
        private float bobTimer;

        private void Awake()
        {
            if (attackCamera == null)
            {
                attackCamera = GetComponentInParent<Camera>();
            }

            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            EnsureRenderingSetup();
            BeginEquip();
        }

        private void OnDisable()
        {
            IsEquipped = false;
        }

        private void Update()
        {
            if (attackCamera == null || !IsReadyToFire)
            {
                return;
            }

            if (Input.GetKeyDown(lightAttackKey))
            {
                TryAttack(false);
            }
            else if (Input.GetKeyDown(heavyAttackKey))
            {
                TryAttack(true);
            }
        }

        private void LateUpdate()
        {
            SyncViewModelCamera();
            UpdateWeaponMotion();
        }

        public void BeginEquip()
        {
            IsEquipped = true;
            equipReadyTime = Time.time + Mathf.Max(0f, equipTime);

            if (animator != null && !string.IsNullOrWhiteSpace(equipTrigger))
            {
                animator.SetTrigger(equipTrigger);
            }
        }

        private void EnsureRenderingSetup()
        {
            Camera baseCamera = attackCamera != null ? attackCamera : GetComponentInParent<Camera>();
            if (baseCamera == null)
            {
                return;
            }

            int playerLayer = LayerMask.NameToLayer(playerLayerName);
            int weaponLayer = LayerMask.NameToLayer(weaponLayerName);

            if (playerLayer >= 0)
            {
                SetLayerRecursively(baseCamera.transform.root, playerLayer);
            }

            if (weaponLayer >= 0)
            {
                SetLayerRecursively(transform, weaponLayer);
            }

            if (playerLayer >= 0)
            {
                baseCamera.cullingMask &= ~(1 << playerLayer);
            }

            if (weaponLayer >= 0)
            {
                baseCamera.cullingMask &= ~(1 << weaponLayer);
                viewModelCamera = EnsureViewModelCamera(baseCamera, weaponLayer);
            }
        }

        private void TryAttack(bool heavyAttack)
        {
            if (heavyAttack)
            {
                if (Time.time < nextHeavyAttackTime)
                {
                    return;
                }

                nextHeavyAttackTime = Time.time + heavyAttackCooldown;
            }
            else
            {
                if (Time.time < nextLightAttackTime)
                {
                    return;
                }

                nextLightAttackTime = Time.time + lightAttackCooldown;
            }

            if (animator != null)
            {
                string trigger = heavyAttack ? heavyAttackTrigger : lightAttackTrigger;
                if (!string.IsNullOrWhiteSpace(trigger))
                {
                    animator.SetTrigger(trigger);
                }
            }

            float baseDamage = heavyAttack ? heavyDamage : lightDamage;
            Transform origin = hitOrigin != null ? hitOrigin : attackCamera.transform;
            Ray ray = new Ray(origin.position, origin.forward);
            RaycastHit[] hits = Physics.SphereCastAll(ray, hitRadius, attackRange, hitMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (IsSelfHit(hit.collider))
                {
                    continue;
                }

                bool isBackstab = IsBackstab(hit.collider.transform);
                float damage = baseDamage * (isBackstab ? backstabMultiplier : 1f);

                if (heavyAttack && isBackstab && heavyBackstabInstantKill)
                {
                    ApplyInstantKill(hit.collider, hit.point, hit.normal);
                }
                else
                {
                    ApplyDamage(hit.collider, damage, hit.point, hit.normal);
                }

                Debug.DrawLine(ray.origin, hit.point, debugColor, debugDuration);
                return;
            }

            Debug.DrawRay(ray.origin, ray.direction * attackRange, debugColor, debugDuration);
        }

        private void ApplyDamage(Collider hitCollider, float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (hitCollider == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageable damageable)
                {
                    damageable.TakeDamage(damage, hitPoint, hitNormal);
                    break;
                }
            }
        }

        private void ApplyInstantKill(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (hitCollider == null)
            {
                return;
            }

            Health health = hitCollider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(health.CurrentHealth, hitPoint, hitNormal);
                return;
            }

            ApplyDamage(hitCollider, 9999f, hitPoint, hitNormal);
        }

        private bool IsBackstab(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return false;
            }

            Transform root = targetTransform.root;
            Vector3 toAttacker = transform.position - root.position;
            toAttacker.y = 0f;

            if (toAttacker.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            toAttacker.Normalize();

            Vector3 targetForward = root.forward;
            targetForward.y = 0f;

            if (targetForward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            targetForward.Normalize();
            return Vector3.Dot(targetForward, toAttacker) < -backstabDotThreshold;
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

        private Camera EnsureViewModelCamera(Camera baseCamera, int weaponLayer)
        {
            Transform cameraRoot = baseCamera.transform;
            Camera camera = viewModelCamera;

            if (camera == null)
            {
                Transform existing = cameraRoot.Find("Knife Camera");
                if (existing != null)
                {
                    camera = existing.GetComponent<Camera>();
                }

                if (camera == null)
                {
                    GameObject cameraObject = new GameObject("Knife Camera");
                    cameraObject.transform.SetParent(cameraRoot, false);
                    cameraObject.transform.localPosition = Vector3.zero;
                    cameraObject.transform.localRotation = Quaternion.identity;
                    cameraObject.transform.localScale = Vector3.one;
                    camera = cameraObject.AddComponent<Camera>();
                }
            }

            camera.cullingMask = 1 << weaponLayer;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = baseCamera.farClipPlane;
            camera.fieldOfView = Mathf.Clamp(viewModelFieldOfView, 1f, 179f);
            camera.allowHDR = baseCamera.allowHDR;
            camera.allowMSAA = baseCamera.allowMSAA;
            camera.depth = baseCamera.depth + 1f;

            UniversalAdditionalCameraData baseData = baseCamera.GetComponent<UniversalAdditionalCameraData>();
            if (baseData != null)
            {
                baseData.renderType = CameraRenderType.Base;
            }

            UniversalAdditionalCameraData overlayData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (overlayData == null)
            {
                overlayData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            overlayData.renderType = CameraRenderType.Overlay;

            if (baseData != null && baseData.cameraStack != null && !baseData.cameraStack.Contains(camera))
            {
                baseData.cameraStack.Add(camera);
            }

            return camera;
        }

        private void SyncViewModelCamera()
        {
            if (attackCamera == null || viewModelCamera == null)
            {
                return;
            }

            viewModelCamera.nearClipPlane = Mathf.Max(0.01f, attackCamera.nearClipPlane);
            viewModelCamera.farClipPlane = attackCamera.farClipPlane;
        }

        private void UpdateWeaponMotion()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            Vector2 lookInputTarget = new Vector2(mouseX, mouseY);
            smoothedLookInput = Vector2.Lerp(smoothedLookInput, lookInputTarget, Time.deltaTime * weaponMotionSmoothing);

            float bobAmountTarget = Mathf.Clamp01(Mathf.Abs(moveX) + Mathf.Abs(moveY));
            smoothedMoveAmount = Mathf.Lerp(smoothedMoveAmount, bobAmountTarget, Time.deltaTime * weaponMotionSmoothing);
            bobTimer += Time.deltaTime * weaponBobSpeed * smoothedMoveAmount;

            Vector3 bobOffset = new Vector3(
                Mathf.Sin(bobTimer * 0.5f) * weaponBobAmount * moveX,
                Mathf.Sin(bobTimer) * weaponBobAmount * 0.5f * smoothedMoveAmount,
                0f);

            Vector3 swayPosition = new Vector3(
                -smoothedLookInput.x * weaponSwayPositionAmount,
                -smoothedLookInput.y * weaponSwayPositionAmount,
                0f);

            Vector3 swayRotation = new Vector3(
                -smoothedLookInput.y * weaponSwayRotationAmount * 0.75f,
                -smoothedLookInput.x * weaponSwayRotationAmount * 0.75f,
                -smoothedLookInput.x * weaponSwayRotationAmount * 0.25f);

            currentMotionPosition = Vector3.Lerp(currentMotionPosition, swayPosition + bobOffset, Time.deltaTime * weaponMotionSmoothing);
            currentMotionRotation = Vector3.Lerp(currentMotionRotation, swayRotation, Time.deltaTime * weaponMotionSmoothing);
            currentKickbackPosition = Vector3.Lerp(currentKickbackPosition, Vector3.zero, Time.deltaTime * weaponMotionSmoothing);
            currentKickbackRotation = Vector3.Lerp(currentKickbackRotation, Vector3.zero, Time.deltaTime * weaponMotionSmoothing);

            transform.localPosition = baseLocalPosition + currentMotionPosition + currentKickbackPosition;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(currentMotionRotation + currentKickbackRotation);
        }
        private void SetLayerRecursively(Transform root, int layer)
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

