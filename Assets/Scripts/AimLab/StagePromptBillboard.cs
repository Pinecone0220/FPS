using UnityEngine;

namespace FpsPractice.FPS
{
    public class StagePromptBillboard : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool useCameraMain = true;
        [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 180f, 0f);

        private void LateUpdate()
        {
            Camera cameraToFace = targetCamera;
            if (cameraToFace == null && useCameraMain)
            {
                cameraToFace = Camera.main;
            }

            if (cameraToFace == null)
            {
                return;
            }

            Vector3 toCamera = cameraToFace.transform.position - transform.position;
            if (toCamera.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up) * Quaternion.Euler(rotationOffset);
        }
    }
}
