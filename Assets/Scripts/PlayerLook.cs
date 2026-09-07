using UnityEngine;

namespace FpsPractice.FPS
{
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform playerBody;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool lockCursor = true;
        [Tooltip("반동이 시야를 얼마나 빨리 원위치로 되돌리는지 정합니다.")]
        [SerializeField] private float recoilReturnSpeed = 18f;

        private float pitch;
        private float recoilPitch;
        private float recoilYaw;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Start()
        {
            ApplyCursorState(lockCursor);
        }

        private void Update()
        {
            if (playerBody == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, recoilReturnSpeed * Time.deltaTime);
            recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, recoilReturnSpeed * Time.deltaTime);

            float finalPitch = Mathf.Clamp(pitch + recoilPitch, -89f, 89f);
            transform.localRotation = Quaternion.Euler(finalPitch, recoilYaw, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }

        public void AddRecoil(float pitchKick, float yawKick)
        {
            recoilPitch += pitchKick;
            recoilYaw += yawKick;
        }

        public void SetMouseSensitivity(float value)
        {
            mouseSensitivity = Mathf.Max(0f, value);
        }

        public float GetMouseSensitivity()
        {
            return mouseSensitivity;
        }

        public void SetFieldOfView(float value)
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Clamp(value, 1f, 179f);
            }
        }

        public float GetFieldOfView()
        {
            return playerCamera != null ? playerCamera.fieldOfView : 60f;
        }

        public void SetCamera(Camera camera)
        {
            playerCamera = camera;
        }

        public void SetCursorLocked(bool locked)
        {
            ApplyCursorState(locked);
        }

        private void ApplyCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
