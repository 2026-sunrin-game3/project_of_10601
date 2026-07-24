using UnityEngine;
using UnityEngine.UI;

public static class HealthBarStyler
{
    public static void Apply(Slider slider, Color fillColor, Color accentColor)
    {
        if (slider == null) return;

        RectTransform root = slider.GetComponent<RectTransform>();
        if (root == null) return;

        Image rootImage = slider.GetComponent<Image>();
        if (rootImage == null)
            rootImage = slider.gameObject.AddComponent<Image>();

        rootImage.sprite = CreateBackgroundSprite();
        rootImage.type = Image.Type.Simple;
        rootImage.color = Color.white;
        rootImage.raycastTarget = false;

        if (slider.fillRect == null)
        {
            GameObject fillObject = new GameObject("Fill Area", typeof(RectTransform));
            fillObject.transform.SetParent(slider.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, 0f);
            fillRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0f, 0f);
            fillRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0f, 0f);
            fillRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0f, 0f);
            slider.fillRect = fillRect;
        }

        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage == null)
            fillImage = slider.fillRect.gameObject.AddComponent<Image>();

        fillImage.sprite = CreateFillSprite(fillColor, accentColor);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = Color.white;
        fillImage.raycastTarget = false;

        slider.targetGraphic = rootImage;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    static Sprite CreateBackgroundSprite()
    {
        Texture2D texture = new Texture2D(256, 64, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                bool border = x < 6 || x > texture.width - 7 || y < 6 || y > texture.height - 7;
                Color color = border ? new Color(0.18f, 0.2f, 0.24f, 1f) : new Color(0.08f, 0.09f, 0.12f, 1f);

                if (!border)
                {
                    float glow = Mathf.Sin((x + y) * 0.08f) * 0.03f + 0.02f;
                    color = new Color(0.06f + glow, 0.07f + glow * 0.8f, 0.1f + glow, 1f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite CreateFillSprite(Color fillColor, Color accentColor)
    {
        Texture2D texture = new Texture2D(256, 64, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                bool border = x < 4 || x > texture.width - 5 || y < 4 || y > texture.height - 5;
                float nx = (float)x / (texture.width - 1);
                float ny = (float)y / (texture.height - 1);
                Color baseColor = Color.Lerp(fillColor, accentColor, 0.25f + ny * 0.35f);
                Color color = border ? Color.Lerp(baseColor, Color.white, 0.28f) : baseColor;

                if (!border)
                {
                    float shine = Mathf.Clamp01(0.85f - Mathf.Abs(nx - 0.5f) * 0.8f + 0.08f);
                    color = Color.Lerp(color, new Color(1f, 1f, 1f, 0.55f), shine * 0.18f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
