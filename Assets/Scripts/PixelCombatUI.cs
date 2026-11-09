using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FpsPractice.FPS
{
    [RequireComponent(typeof(Canvas))]
    public sealed class PixelCombatUI : MonoBehaviour
    {
        [Header("Combat UI Colors")]
        [SerializeField] private Color panelColor = new Color(0.075f, 0.095f, 0.075f, 0.96f);
        [SerializeField] private Color buttonColor = new Color(0.27f, 0.30f, 0.23f, 1f);
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.48f, 0.28f, 1f);
        [SerializeField] private Color textColor = new Color(0.91f, 0.90f, 0.80f, 1f);
        [SerializeField] private Color borderColor = new Color(0.77f, 0.61f, 0.31f, 1f);

        private Sprite buttonSprite;
        private Texture2D buttonTexture;
        private readonly List<TMP_Text> hudLabels = new List<TMP_Text>();
        private readonly List<GameObject> hudFrames = new List<GameObject>();

        private void Start()
        {
            buttonSprite = CreateButtonSprite();

            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (IsRewardCard(button.transform) || !(button.targetGraphic is Image image))
                {
                    continue;
                }

                // Keep item artwork and other deliberately assigned sprites intact.
                if (image.sprite != null && image.sprite.name != "UISprite" && image.sprite.name != "Background")
                {
                    continue;
                }

                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;

                ColorBlock colors = button.colors;
                colors.normalColor = buttonColor;
                colors.highlightedColor = highlightColor;
                colors.selectedColor = highlightColor;
                colors.pressedColor = new Color(0.17f, 0.19f, 0.15f, 1f);
                colors.disabledColor = new Color(0.28f, 0.29f, 0.26f, 0.55f);
                button.colors = colors;

                foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.color = textColor;
                }
                foreach (Text label in button.GetComponentsInChildren<Text>(true))
                {
                    label.color = textColor;
                }
            }

            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (image.name == "Pause_Window")
                {
                    image.color = panelColor;
                }
            }

            foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.name == "Health_txt" || label.name == "Ammo_txt")
                {
                    AddHudFrame(label);
                }
            }
        }

        private void AddHudFrame(TMP_Text label)
        {
            RectTransform source = label.rectTransform;
            GameObject frame = new GameObject("Pixel HUD Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frame.layer = label.gameObject.layer;
            RectTransform rect = frame.GetComponent<RectTransform>();
            rect.SetParent(source.parent, false);
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;
            rect.sizeDelta = source.sizeDelta;
            rect.localRotation = source.localRotation;
            rect.localScale = source.localScale;
            rect.SetSiblingIndex(source.GetSiblingIndex());

            Image image = frame.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(panelColor.r, panelColor.g, panelColor.b, 0.8f);
            image.raycastTarget = false;
            frame.SetActive(label.gameObject.activeInHierarchy);
            hudLabels.Add(label);
            hudFrames.Add(frame);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < hudLabels.Count; i++)
            {
                if (hudFrames[i] != null)
                {
                    hudFrames[i].SetActive(hudLabels[i] != null && hudLabels[i].gameObject.activeInHierarchy);
                }
            }
        }

        private static bool IsRewardCard(Transform item)
        {
            for (Transform current = item; current != null; current = current.parent)
            {
                if (current.name == "LevelUp")
                {
                    return true;
                }
            }
            return false;
        }

        private Sprite CreateButtonSprite()
        {
            const int size = 16;
            buttonTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PixelCombatButton",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool corner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);
                    bool edge = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    buttonTexture.SetPixel(x, y, corner ? Color.clear : edge ? borderColor : Color.white);
                }
            }
            buttonTexture.Apply();
            return Sprite.Create(buttonTexture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
        }

        private void OnDestroy()
        {
            if (buttonSprite != null) Destroy(buttonSprite);
            if (buttonTexture != null) Destroy(buttonTexture);
        }
    }
}
