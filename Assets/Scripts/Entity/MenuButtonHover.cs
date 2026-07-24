using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    static readonly Color NormalColor = new Color(0.78f, 0.79f, 0.82f, 1f);
    static readonly Color HoverColor = new Color(1f, 0.18f, 0.22f, 1f);

    TextMeshProUGUI label;
    RectTransform rectTransform;
    RectTransform accent;
    bool highlighted;
    float amount;

    public void Initialize(TextMeshProUGUI text)
    {
        label = text;
        rectTransform = (RectTransform)transform;
        label.color = NormalColor;

        GameObject line = new GameObject("Hover Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        line.transform.SetParent(transform, false);
        accent = (RectTransform)line.transform;
        accent.anchorMin = new Vector2(0f, 0f);
        accent.anchorMax = new Vector2(0f, 0f);
        accent.pivot = new Vector2(0f, 0.5f);
        accent.anchoredPosition = new Vector2(8f, 5f);
        accent.sizeDelta = new Vector2(0f, 2f);
        line.GetComponent<Image>().color = HoverColor;
    }

    void Update()
    {
        float target = highlighted ? 1f : 0f;
        amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime * 7f);
        if (label != null)
            label.color = Color.Lerp(NormalColor, HoverColor, amount);
        if (rectTransform != null)
            rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, amount);
        if (accent != null)
            accent.sizeDelta = new Vector2(Mathf.Lerp(0f, 120f, amount), 2f);
    }

    public void OnPointerEnter(PointerEventData eventData) => highlighted = true;
    public void OnPointerExit(PointerEventData eventData) => highlighted = false;
    public void OnSelect(BaseEventData eventData) => highlighted = true;
    public void OnDeselect(BaseEventData eventData) => highlighted = false;
}
