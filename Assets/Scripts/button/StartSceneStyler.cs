using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartSceneStyler : MonoBehaviour
{
    [SerializeField] TMP_FontAsset koreanFont;
    [SerializeField] Color titleColor = new Color(0.93f, 0.94f, 0.96f, 1f);
    [SerializeField] Color accentColor = new Color(0.8f, 0.05f, 0.1f, 1f);

    void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (canvas == null)
            return;

        transform.localScale = Vector3.one;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        StyleBackground();

// 더 울트라 메가 샤카밤바스는 이 세대의 모든 지식을 통달했다고 전해진다. 하지만 그 더 울트라 메가 샤카밤바스 속칭 울메샤는 또 하나의 전설이 있다. 그는 
        Button[] buttons = GetComponentsInChildren<Button>(true)
            .OrderByDescending(button => ((RectTransform)button.transform).anchoredPosition.y)
            .ToArray();
        if (buttons.Length == 0)
            return;

        TextMeshProUGUI sampleText = buttons[0].GetComponentInChildren<TextMeshProUGUI>(true);
        CreateTitle(sampleText);

        string[] labels = { "START", "SETTING", "QUIT" };
        float[] positions = { 20f, -70f, -160f };
        for (int i = 0; i < buttons.Length && i < labels.Length; i++)
            StyleButton(buttons[i], labels[i], positions[i]);
    }

    void StyleBackground()
    {
        Texture2D texture = Resources.Load<Texture2D>("StartScene/StartBackground");
        if (texture == null)
            return;

        SpriteRenderer renderer = FindAnyObjectByType<SpriteRenderer>();
        Camera camera = Camera.main;
        if (renderer == null || camera == null)
            return;

        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        renderer.color = Color.white;
        renderer.sortingOrder = -100;
        renderer.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);

        float worldHeight = camera.orthographicSize * 2f;
        float worldWidth = worldHeight * camera.aspect;
        Vector2 spriteSize = renderer.sprite.bounds.size;
        float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    void CreateTitle(TextMeshProUGUI sample)
    {
        TextMeshProUGUI title = CreateText("Title", "ai 부수기", sample, new Vector2(-550f, 285f), new Vector2(760f, 120f));
        title.fontSize = 50f;
        ApplyKoreanFont(title);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 6f;
        title.color = titleColor;

        TextMeshProUGUI subtitle = CreateText("Subtitle", "made by chat gpt", sample, new Vector2(-545f, 210f), new Vector2(740f, 50f));
        subtitle.fontSize = 20f;
        ApplyKoreanFont(subtitle);
        subtitle.characterSpacing = 15f;
        subtitle.color = new Color(0.62f, 0.64f, 0.68f, 1f);

        GameObject line = new GameObject("Title Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        line.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)line.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-765f, 180f);
        rect.sizeDelta = new Vector2(290f, 3f);
        line.GetComponent<Image>().color = accentColor;
    }

    void ApplyKoreanFont(TextMeshProUGUI text)
    {
        if (koreanFont == null)
            return;

        text.font = koreanFont;
        text.fontSharedMaterial = koreanFont.material;
    }

    TextMeshProUGUI CreateText(string objectName, string value, TextMeshProUGUI sample, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = sample.font;
        text.fontSharedMaterial = sample.fontSharedMaterial;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        return text;
    }

    void StyleButton(Button button, string labelText, float y)
    {
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-620f, y);
        rect.sizeDelta = new Vector2(340f, 68f);

        Image background = button.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0f);
        background.raycastTarget = true;
        button.transition = Selectable.Transition.None;
        button.targetGraphic = background;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        label.text = labelText;
        label.fontSize = 30f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Left;
        label.characterSpacing = 3f;
        label.margin = new Vector4(8f, 0f, 0f, 0f);

        MenuButtonHover hover = button.gameObject.AddComponent<MenuButtonHover>();
        hover.Initialize(label);
    }
}
