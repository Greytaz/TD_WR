using UnityEngine;
using TMPro;

namespace TowerDefense.Effects
{
    public class FloatingText : MonoBehaviour
    {
        [Header("Settings")]
        public float speed = 1.5f;
        public float duration = 1.0f;
        public Color defaultColor = Color.red;

        private TextMeshPro textMesh;
        private float timer;
        private Transform camTransform;

        private void Awake()
        {
            textMesh = GetComponent<TextMeshPro>();
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TextMeshPro>();
            }
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontSize = 4f;
            textMesh.color = defaultColor;
        }

        public void Setup(string text, Color color, float fontSize = 4f)
        {
            if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.color = color;
            textMesh.fontSize = fontSize;
            timer = duration;
            
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            // Float upwards
            transform.position += Vector3.up * speed * Time.deltaTime;

            // Fade out
            timer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / duration);
            Color c = textMesh.color;
            c.a = alpha;
            textMesh.color = c;

            // Face the camera (Billboard)
            if (camTransform != null)
            {
                transform.LookAt(transform.position + camTransform.forward);
            }
            else if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }

            if (timer <= 0f)
            {
                gameObject.SetActive(false);
            }
        }
    }
}