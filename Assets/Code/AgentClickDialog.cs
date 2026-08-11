using UnityEngine;

namespace Biocrowds.Core
{
    public class AgentClickDialog : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Input.mousePosition;
                if (_mainCamera == null)
                {
                    _mainCamera = Camera.main;
                    if (_mainCamera == null)
                        return;
                }

                Ray ray = _mainCamera.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hitInfo))
                {
                    Agent clickedAgent = hitInfo.collider.GetComponentInParent<Agent>();
                    if (clickedAgent != null)
                    {
                        AgentDialogManager.Instance?.ShowDialog(clickedAgent);
                        return;
                    }
                }

                AgentDialogManager.Instance?.HideDialog();
            }
        }
    }
}
