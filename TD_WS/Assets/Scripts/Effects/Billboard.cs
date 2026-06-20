using UnityEngine;

namespace TowerDefense.Effects
{
    public class Billboard : MonoBehaviour
    {
        private Transform camTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (camTransform == null)
            {
                if (Camera.main != null)
                {
                    camTransform = Camera.main.transform;
                }
                else
                {
                    return;
                }
            }

            // Keep the billboard facing the camera
            transform.LookAt(transform.position + camTransform.forward);
        }
    }
}