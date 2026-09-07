using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FpsPractice.FPS
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [FormerlySerializedAs("slowSpeed")]
        [Tooltip("Shift를 누르고 이동할 때 적용되는 달리기 속도입니다.")]
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;
        [Tooltip("�ɱ� Ű�Դϴ�. �⺻�� Left Control�̰�, ���� �޴����� C �Ǵ� Ctrl�� �ٲ� �� �ֽ��ϴ�.")]
        [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
        [Tooltip("���� ���� �̵� �ӵ��Դϴ�. �÷��� �� �ν����Ϳ��� Ȯ���� �� �ִ� ����׿� ���Դϴ�.")]
        [SerializeField] private float currentMoveSpeed;
        [FormerlySerializedAs("moveResponse")]
        [Tooltip("������ �󸶳� ���� �ٰ� �������� ���մϴ�. ���� Ŭ���� ������ ������, �������� �� �̲������ϴ�.")]
        [SerializeField] private float inertiaResponse = 12f;

        [Header("References")]
        [SerializeField] private WeaponLoadout weaponLoadout;
        [SerializeField] private PlayerSetupHelper playerSetupHelper;
        [SerializeField] private Button aimLabModeButton;
        [SerializeField] private Button mainGameButton;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private GameObject stagePanel;

        private CharacterController controller;
        private Vector3 velocity;
        private Vector3 horizontalVelocity;
        private bool isGrounded;
        private bool isCrouching;
        private bool isWeaponAiming;

        public bool IsGrounded => isGrounded;
        public bool IsCrouching => isCrouching;
        public bool IsMoving => horizontalVelocity.sqrMagnitude > 0.01f;
        public bool IsSprinting { get; private set; }
        private bool isStagePanelOpen;
        private bool isInsideStageTrigger;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (weaponLoadout == null)
            {
                weaponLoadout = GetComponentInParent<WeaponLoadout>();
            }

            if (playerSetupHelper == null)
            {
                playerSetupHelper = GetComponent<PlayerSetupHelper>();
            }

            if (aimLabModeButton != null)
            {
                aimLabModeButton.onClick.RemoveListener(LoadAimLabScene);
                aimLabModeButton.onClick.AddListener(LoadAimLabScene);
            }

            if (mainGameButton != null)
            {
                mainGameButton.onClick.RemoveListener(LoadMainGameScene);
                mainGameButton.onClick.AddListener(LoadMainGameScene);
            }

            if (stagePanel != null)
            {
                stagePanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (isStagePanelOpen)
            {
                IsSprinting = false;
                return;
            }

            UpdateStageInteraction();
            UpdateGroundedState();
            HandleMovement();
            HandleJumpAndGravity();
            HandleCrouch();
        }

        private void UpdateGroundedState()
        {
            if (groundCheck != null)
            {
                isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
                return;
            }

            isGrounded = controller.isGrounded;
        }

        private void HandleMovement()
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            Vector3 move = transform.right * inputX + transform.forward * inputZ;
            IsSprinting = move.sqrMagnitude > 0.0001f && Input.GetKey(KeyCode.LeftShift)
                && !Input.GetKey(crouchKey) && !isWeaponAiming;
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
            isCrouching = Input.GetKey(crouchKey);
            controller.height = isCrouching ? 1.2f : 2f;
        }

        private float GetCurrentMoveSpeed()
        {
            if (isCrouching)
            {
                return crouchSpeed;
            }

            if (Input.GetKey(KeyCode.LeftShift) && !isWeaponAiming)
            {
                return Mathf.Max(0.1f, sprintSpeed + GetWeaponSpeedModifier());
            }

            return Mathf.Max(0.1f, walkSpeed + GetWeaponSpeedModifier());
        }

        private float GetWeaponSpeedModifier()
        {
            if (weaponLoadout == null)
            {
                return 0f;
            }

            IWeapon currentWeapon = weaponLoadout.CurrentWeapon;
            return currentWeapon != null ? currentWeapon.MovementSpeedModifier : 0f;
        }

        public void SetWeaponAiming(bool aiming)
        {
            isWeaponAiming = aiming;
            if (aiming)
            {
                IsSprinting = false;
            }
        }

        public void SetCrouchKey(KeyCode keyCode)
        {
            crouchKey = keyCode;
        }

        public KeyCode GetCrouchKey()
        {
            return crouchKey;
        }

        private void UpdateStageInteraction()
        {
            if (!isStagePanelOpen && isInsideStageTrigger && Input.GetKeyDown(KeyCode.E))
            {
                OpenStagePanel();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && other.gameObject.CompareTag("Stage"))
            {
                isInsideStageTrigger = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other != null && other.gameObject.CompareTag("Stage"))
            {
                isInsideStageTrigger = false;
            }
        }
        public void CloseStagePanel()
        {
            isStagePanelOpen = false;

            if (stagePanel != null)
            {
                stagePanel.SetActive(false);
            }

            if (playerSetupHelper != null)
            {
                playerSetupHelper.SetPaused(false);
            }
        }

        public void LoadAimLabScene()
        {
            isStagePanelOpen = false;
            if (stagePanel != null)
            {
                stagePanel.SetActive(false);
            }

            if (playerSetupHelper != null)
            {
                playerSetupHelper.SetPaused(false);
            }

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene("AimLab");
        }
public void LoadMainGameScene()
        {
            isStagePanelOpen = false;
            if (stagePanel != null)
            {
                stagePanel.SetActive(false);
            }

            if (playerSetupHelper != null)
            {
                playerSetupHelper.SetPaused(false);
            }

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene("MainFps");
        }

        private void OpenStagePanel()
        {
            isStagePanelOpen = true;

            if (stagePanel != null)
            {
                stagePanel.SetActive(true);
            }

            if (playerSetupHelper != null)
            {
                playerSetupHelper.SetPaused(true);
            }
        }
    }
}

