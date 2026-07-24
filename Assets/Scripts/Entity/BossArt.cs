using UnityEngine;
using System.Collections.Generic;

public static class BossArt
{
    static readonly Sprite[][] formSprites = new Sprite[4][];
    static Sprite[] effectSprites;
    static Sprite[] transformationSprites;

    public static Sprite[] GetFormSprites(Boss.PhaseOneForm form)
    {
        int index = (int)form;
        if (formSprites[index] == null)
        {
            string path = "BossPhase1/" + form;
            // Large and Special sheets contain tightly imported character frames
            // whose visible heights differ. Rebuild those frames with a per-frame
            // PPU so attacks (especially teleport slash) keep the same world size.
            formSprites[index] = form == Boss.PhaseOneForm.Large || form == Boss.PhaseOneForm.Special
                ? SliceImportedFrames(path, 4, 350f, new Vector2(0.5f, 0.13f))
                : Slice(Resources.Load<Texture2D>(path), 4, 350f, new Vector2(0.5f, 0.13f));
        }
        return formSprites[index];
    }

    public static Sprite GetEffect(int index)
    {
        if (effectSprites == null)
            effectSprites = Slice(Resources.Load<Texture2D>("BossPhase1/Effects"), 8, 220f, new Vector2(0.5f, 0.5f));
        return effectSprites != null && index >= 0 && index < effectSprites.Length ? effectSprites[index] : null;
    }

    public static Sprite[] GetTransformationSprites()
    {
        if (transformationSprites == null)
            transformationSprites = Slice(Resources.Load<Texture2D>("BossPhase1/Transformation"), 4, 350f, new Vector2(0.5f, 0.13f));
        return transformationSprites;
    }

    static Sprite[] Slice(Texture2D texture, int count, float pixelsPerUnit, Vector2 pivot)
    {
        if (texture == null)
            return new Sprite[0];

        Sprite[] sprites = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            int xMin = Mathf.RoundToInt(texture.width * i / (float)count);
            int xMax = Mathf.RoundToInt(texture.width * (i + 1) / (float)count);
            sprites[i] = Sprite.Create(texture, new Rect(xMin, 0, xMax - xMin, texture.height), pivot, pixelsPerUnit);
            sprites[i].name = texture.name + "_" + i;
        }
        return sprites;
    }

    static Sprite[] SliceImportedFrames(string path, int count, float pixelsPerUnit, Vector2 pivot)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        Sprite[] imported = Resources.LoadAll<Sprite>(path);
        List<Sprite> valid = new List<Sprite>();
        foreach (Sprite sprite in imported)
            if (sprite != null && sprite.rect.width > 32f && sprite.rect.height > 32f)
                valid.Add(sprite);
        valid.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
        if (texture == null || valid.Count < count) return Slice(texture, count, pixelsPerUnit, pivot);

        Sprite[] frames = new Sprite[count];
        float referenceWorldHeight = valid[0].rect.height / pixelsPerUnit;
        for (int i = 0; i < count; i++)
        {
            // Cropped imported frames have different pixel heights. Give every
            // frame its own PPU so their world-space height stays identical.
            float normalizedPpu = referenceWorldHeight <= 0f
                ? pixelsPerUnit
                : valid[i].rect.height / referenceWorldHeight;
            frames[i] = Sprite.Create(texture, valid[i].rect, pivot, normalizedPpu);
            frames[i].name = texture.name + "_Unclipped_" + i;
        }
        return frames;
    }
}
