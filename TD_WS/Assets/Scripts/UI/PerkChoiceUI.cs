using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TowerDefense.Core;

namespace TowerDefense.UI
{
    public class PerkChoiceUI : MonoBehaviour
    {
        public static PerkChoiceUI Instance { get; private set; }

        private GameObject panelObj;
        private List<PerkData> currentOptions;

        // UI references for card 1
        private TextMeshProUGUI titleText1;
        private TextMeshProUGUI descText1;
        private Button chooseButton1;

        // UI references for card 2
        private TextMeshProUGUI titleText2;
        private TextMeshProUGUI descText2;
        private Button chooseButton2;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeUI();
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
        }

        private void InitializeUI()
        {
            if (panelObj != null) return;

            // Find main Canvas
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[PerkUI] Canvas not found!");
                return;
            }

            // Create main full screen panel
            panelObj = new GameObject("PerkChoicePanel", typeof(RectTransform));
            panelObj.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Add background overlay image
            Image bgImage = panelObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.85f); // dark dim overlay

            // Add GraphicRaycaster block so player can't click things behind it
            CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;

            // Add Title Text
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.8f);
            titleRt.anchorMax = new Vector2(0.5f, 0.8f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, 0f);
            titleRt.sizeDelta = new Vector2(600f, 60f);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CHOOSE A RUN PERK";
            titleTmp.fontSize = 32f;
            titleTmp.color = Color.yellow;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;

            // Copy font asset if available
            Transform mainTitle = canvasObj.transform.Find("MainMenuPanel/Title");
            if (mainTitle != null)
            {
                TextMeshProUGUI source = mainTitle.GetComponent<TextMeshProUGUI>();
                if (source != null) titleTmp.font = source.font;
            }

            // Create Cards Container
            GameObject containerObj = new GameObject("CardsContainer", typeof(RectTransform));
            containerObj.transform.SetParent(panelObj.transform, false);
            RectTransform containerRt = containerObj.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.45f);
            containerRt.anchorMax = new Vector2(0.5f, 0.45f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = Vector2.zero;
            containerRt.sizeDelta = new Vector2(800f, 320f);

            HorizontalLayoutGroup hlg = containerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 50f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Create Card 1
            GameObject card1 = CreateCard("Card1", containerObj.transform, out titleText1, out descText1, out chooseButton1);
            chooseButton1.onClick.AddListener(() => OnChooseClicked(0));

            // Create Card 2
            GameObject card2 = CreateCard("Card2", containerObj.transform, out titleText2, out descText2, out chooseButton2);
            chooseButton2.onClick.AddListener(() => OnChooseClicked(1));

            panelObj.SetActive(false); // Hide by default
        }

        private GameObject CreateCard(string name, Transform parent, out TextMeshProUGUI titleTxt, out TextMeshProUGUI descTxt, out Button btn)
        {
            GameObject cardObj = new GameObject(name, typeof(RectTransform));
            cardObj.transform.SetParent(parent, false);

            // Card Background
            Image bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.15f, 1.0f); // Dark Slate background

            // Vertical Layout for Card content
            VerticalLayoutGroup vlg = cardObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15f;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(cardObj.transform, false);
            titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.fontSize = 22f;
            titleTxt.color = Color.white;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.fontStyle = FontStyles.Bold;

            // Description
            GameObject descObj = new GameObject("Description", typeof(RectTransform));
            descObj.transform.SetParent(cardObj.transform, false);
            descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.fontSize = 16f;
            descTxt.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            descTxt.alignment = TextAlignmentOptions.Center;

            // Spacer
            GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(cardObj.transform, false);
            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 30f);

            // Button
            GameObject btnObj = new GameObject("ChooseButton", typeof(RectTransform));
            btnObj.transform.SetParent(cardObj.transform, false);
            btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 45f);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.1f, 0.6f, 0.9f, 1f); // Neon Cyan

            btn = btnObj.AddComponent<Button>();
            
            // Button Text
            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRt = btnTextObj.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = Vector2.zero;
            btnTextRt.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "CHOOSE";
            btnText.fontSize = 16f;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontStyle = FontStyles.Bold;

            // Try to match fonts
            GameObject sampleObj = GameObject.Find("Canvas/MainMenuPanel/StartButton/Label");
            if (sampleObj != null)
            {
                TextMeshProUGUI src = sampleObj.GetComponent<TextMeshProUGUI>();
                if (src != null)
                {
                    titleTxt.font = src.font;
                    descTxt.font = src.font;
                    btnText.font = src.font;
                }
            }

            return cardObj;
        }

        public void Show(List<PerkData> options)
        {
            if (panelObj == null)
            {
                InitializeUI();
            }

            if (panelObj == null) return;

            currentOptions = options;
            if (currentOptions == null || currentOptions.Count < 2)
            {
                Debug.LogWarning("[PerkUI] Invalid perk options list passed!");
                return;
            }

            Debug.Log($"[PerkUI] Show: titleText1={titleText1 != null}, descText1={descText1 != null}, titleText2={titleText2 != null}, descText2={descText2 != null}");

            // Populate Card 1
            if (titleText1 != null) titleText1.text = currentOptions[0].title;
            if (descText1 != null) descText1.text = currentOptions[0].description;

            // Populate Card 2
            if (titleText2 != null) titleText2.text = currentOptions[1].title;
            if (descText2 != null) descText2.text = currentOptions[1].description;

            panelObj.SetActive(true);
        }

        public void Hide()
        {
            if (panelObj != null)
            {
                panelObj.SetActive(false);
            }
        }

        private void OnChooseClicked(int index)
        {
            if (currentOptions != null && index < currentOptions.Count)
            {
                string chosenId = currentOptions[index].id;
                if (RunPerkManager.Instance != null)
                {
                    RunPerkManager.Instance.ChoosePerk(chosenId);
                }
            }
        }
    }
}