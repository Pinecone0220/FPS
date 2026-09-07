using UnityEngine;

namespace FpsPractice.FPS
{
    public class AimLabStageTriggerRelay : MonoBehaviour
    {
        [SerializeField] private AimLabModeTerminal terminal;

        private void Awake()
        {
            if (terminal == null)
            {
                terminal = GetComponentInParent<AimLabModeTerminal>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (terminal != null)
            {
                terminal.NotifyPlayerEnter(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (terminal != null)
            {
                terminal.NotifyPlayerExit(other);
            }
        }
    }
}
