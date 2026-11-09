using UnityEngine;

namespace FpsPractice.FPS
{
    [RequireComponent(typeof(CharacterController))]
    public class AimLabPlayerController : MonoBehaviour
    {
        private const string MouseSensitivityKey = "Settings.MouseSensitivity";
        private const string FieldOfViewKey = "Settings.FieldOfView";
        private const string CrouchKeySettingKey = "Settings.CrouchKey";

        [Header("Look")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool lockCursor = true;

        [Header("Settings")]
        [Tooltip("설정창을 여닫는 키입니다. 인스펙터에서 바꿀 수 있습니다.")]
        [SerializeField] private KeyCode settingsToggleKey = KeyCode.Escape;
        [SerializeField] private AimLabSettingsHelper settingsHelper;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [Tooltip("Shift를 누르고 이동할 때 적용되는 달리기 속도입니다.")]
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
        [SerializeField] private float currentMoveSpeed;
        [SerializeField] private float inertiaResponse = 12f;
        [SerializeField] private Vector3 sessionLockedPosition = new Vector3(6.64f, 1.23f, -2.4f);

        [Header("Shooting")]
        [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;
        [SerializeField] private float fireRate = 0.12f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float range = 150f;
        [SerializeField] private LayerMask hitMask = ~0;

        private CharacterController controller;
        private Vector3 velocity;
        private Vector3 horizontalVelocity;
        private float pitch;
        private float nextFireTime;
        private bool isGrounded;
        private bool isCrouching;
        private bool sessionMovementLocked;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (playerBody == null)
            {
                playerBody = transform;
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (settingsHelper == null)
            {
                settingsHelper = GetComponentInChildren<AimLabSettingsHelper>(true);
            }

            mouseSensitivity = Mathf.Max(0f, PlayerPrefs.GetFloat(MouseSensitivityKey, mouseSensitivity));
            crouchKey = LoadSharedCrouchKey();

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = PlayerPrefs.GetFloat(FieldOfViewKey, playerCamera.fieldOfView);
            }
        }

        private void Start()
        {
            ApplyCursorState(lockCursor);
        }

        private void Update()
        {
            if (settingsHelper != null && Input.GetKeyDown(settingsToggleKey))
            {
                settingsHelper.TogglePause();
                return;
            }

            if (settingsHelper != null && settingsHelper.IsPaused)
            {
                return;
            }

            UpdateGroundedState();
            HandleMovement();
            HandleJumpAndGravity();
            HandleCrouch();
            HandleLook();
            HandleShooting();
        }

        private void UpdateGroundedState()
        {
            isGrounded = controller.isGrounded;
        }

        private void HandleLook()
        {
            if (playerBody == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            playerBody.Rotate(Vector3.up * mouseX);
        }

        private void HandleMovement()
        {
            if (sessionMovementLocked)
            {
                horizontalVelocity = Vector3.zero;
                currentMoveSpeed = 0f;
                return;
            }

            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            Vector3 move = transform.right * inputX + transform.forward * inputZ;
            float targetSpeed = GetCurrentMoveSpeed();
            Vector3 desiredVelocity = move.sqrMagnitude > 0.0001f ? move.normalized * targetSpeed : Vector3.zero;
            float response = Mathf.Max(0f, inertiaResponse);

            if (move.sqrMagnitude > 0.0001f
                && horizontalVelocity.sqrMagnitude > 0.0001f
                && Vector3.Dot(horizontalVelocity.normalized, desiredVelocity.normalized) < 0f)
            {
                response *= 3f;
            }

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, response * Time.deltaTime);
            currentMoveSpeed = horizontalVelocity.magnitude;

            controller.Move(horizontalVelocity * Time.deltaTime);
        }

        private void HandleJumpAndGravity()
        {
            if (sessionMovementLocked)
            {
                velocity.y = 0f;
                return;
            }

            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleCrouch()
        {
            if (sessionMovementLocked)
            {
                isCrouching = false;
                controller.height = 2f;
                return;
            }

            isCrouching = Input.GetKey(crouchKey);
            controller.height = isCrouching ? 1.2f : 2f;
        }

        private float GetCurrentMoveSpeed()
        {
            if (isCrouching)
            {
                return crouchSpeed;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                return sprintSpeed;
            }

            return walkSpeed;
        }

        private void HandleShooting()
        {
            if (!Input.GetKey(fireKey) || Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + fireRate;

            if (playerCamera == null)
            {
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IDamageable damageable)
                    {
                        damageable.TakeDamage(damage, hit.point, hit.normal);
                        break;
                    }
                }
            }

            Debug.DrawRay(ray.origin, ray.direction * range, Color.cyan, 0.08f);
        }

        public void SetCrouchKey(KeyCode keyCode)
        {
            crouchKey = keyCode;
        }

        public void SetMouseSensitivity(float value)
        {
            mouseSensitivity = Mathf.Max(0f, value);
        }

        public KeyCode GetCrouchKey()
        {
            return crouchKey;
        }

        private KeyCode LoadSharedCrouchKey()
        {
            int storedKey = PlayerPrefs.GetInt(CrouchKeySettingKey, (int)crouchKey);
            return storedKey == (int)KeyCode.C ? KeyCode.C : KeyCode.LeftControl;
        }

        public void BeginSession()
        {
            ApplyCursorState(true);
            sessionMovementLocked = true;
            horizontalVelocity = Vector3.zero;
            velocity = Vector3.zero;

            if (controller != null)
            {
                controller.enabled = false;
                transform.position = sessionLockedPosition;
                controller.enabled = true;
            }
            else
            {
                transform.position = sessionLockedPosition;
            }
        }

        public void EndSession()
        {
            sessionMovementLocked = false;
            ApplyCursorState(false);
        }

        private void ApplyCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
