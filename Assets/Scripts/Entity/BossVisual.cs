using System.Collections;
using UnityEngine;

public class BossVisual : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    SpriteRenderer ghostRenderer;
    Boss.PhaseOneForm currentForm;
    Coroutine returnRoutine;
    Coroutine fadeRoutine;
    float lastMoveTime = -10f;
    float moveAmount;
    Color formTint = Color.white;

    public void SetTint(Color tint)
    {
        formTint = tint;
        if (spriteRenderer != null) spriteRenderer.color = formTint;
    }

    public void Initialize(SpriteRenderer sourceRenderer)
    {
        GameObject visualObject = new GameObject("Boss Visual");
        visualObject.transform.SetParent(transform, false);
        spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        CopyRendererSettings(sourceRenderer, spriteRenderer);

        GameObject ghostObject = new GameObject("Boss Visual Ghost");
        ghostObject.transform.SetParent(transform, false);
        ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
        CopyRendererSettings(sourceRenderer, ghostRenderer);
        ghostRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        ghostRenderer.color = Color.clear;

        sourceRenderer.enabled = false;
        SetForm(Boss.PhaseOneForm.Basic);
    }

    public void SetForm(Boss.PhaseOneForm form)
    {
        currentForm = form;
        if (returnRoutine != null)
            StopCoroutine(returnRoutine);
        SetSprite(GetFormFrame(0), false);
    }

    public void Play(int frame, float duration = 0.2f)
    {
        if (returnRoutine != null)
            StopCoroutine(returnRoutine);
        SetSprite(GetFormFrame(frame), true);
        returnRoutine = StartCoroutine(ReturnToIdle(duration));
    }

    public IEnumerator PlayTransformation(Boss.PhaseOneForm nextForm, float duration)
    {
        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        Sprite[] frames = BossArt.GetTransformationSprites();
        float frameDuration = duration / Mathf.Max(1, frames.Length);
        foreach (Sprite frame in frames)
        {
            SetSprite(frame, true);
            yield return new WaitForSeconds(frameDuration);
        }

        currentForm = nextForm;
        SetSprite(GetFormFrame(0), true);
    }

    public void SetMoving()
    {
        lastMoveTime = Time.time;
    }

    void Update()
    {
        bool moving = Time.time - lastMoveTime < 0.12f;
        moveAmount = Mathf.MoveTowards(moveAmount, moving ? 1f : 0f, Time.deltaTime * 8f);

        if (spriteRenderer == null)
            return;

        float bob = Mathf.Sin(Time.time * 11f) * 0.055f * moveAmount;
        float lean = Mathf.Sin(Time.time * 5.5f) * 2.2f * moveAmount;
        spriteRenderer.transform.localPosition = new Vector3(0f, bob, 0f);
        spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, lean);
        ghostRenderer.transform.localPosition = spriteRenderer.transform.localPosition;
        ghostRenderer.transform.localRotation = spriteRenderer.transform.localRotation;
    }

    Sprite GetFormFrame(int frame)
    {
        Sprite[] sprites = BossArt.GetFormSprites(currentForm);
        return sprites.Length == 0 ? null : sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
    }

    void SetSprite(Sprite nextSprite, bool crossFade)
    {
        if (spriteRenderer == null || nextSprite == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (crossFade && spriteRenderer.sprite != null)
        {
            ghostRenderer.sprite = spriteRenderer.sprite;
            ghostRenderer.color = Color.white;
            spriteRenderer.sprite = nextSprite;
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            fadeRoutine = StartCoroutine(CrossFade(0.1f));
        }
        else
        {
            spriteRenderer.sprite = nextSprite;
            spriteRenderer.color = formTint;
            ghostRenderer.color = Color.clear;
        }
    }

    IEnumerator CrossFade(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            spriteRenderer.color = new Color(formTint.r, formTint.g, formTint.b, t);
            ghostRenderer.color = new Color(1f, 1f, 1f, 1f - t);
            timer += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = formTint;
        ghostRenderer.color = Color.clear;
        fadeRoutine = null;
    }

    IEnumerator ReturnToIdle(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetSprite(GetFormFrame(0), true);
        returnRoutine = null;
    }

    static void CopyRendererSettings(SpriteRenderer source, SpriteRenderer destination)
    {
        destination.sharedMaterial = source.sharedMaterial;
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder;
        destination.flipX = source.flipX;
        destination.color = Color.white;
    }
}
