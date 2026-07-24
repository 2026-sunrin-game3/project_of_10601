using System.Collections;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public enum CombatMotion { None, Basic, Launch, GunPlant, Burst, ChargedShot, LegSweep, DashThrust, Flip, SwordEnemyPlant, SwordPlantShot, SwordPlantEject }
    Animator animator;
    PlayerStat stat;
    Coroutine motionRoutine;
    Coroutine spriteRoutine;
    Vector3 baseScale;
    bool crouching;
    SpriteRenderer spriteRenderer;
    Sprite originalSprite;
    Sprite[] newMoveFrames;
    Sprite[] burstFrames;
    Sprite[] chargeFrames;
    Sprite[] reloadFrames;
    bool playingCharge;
    bool playingReload;
    int reloadFrameIndex;
    float scaleLockUntil;
    public float direction = 1f;

    void Start()
    {
        animator = GetComponent<Animator>();
        stat = GetComponent<PlayerStat>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer == null ? null : spriteRenderer.sprite;
        BuildNewMoveFrames();
        BuildBurstFrames();
        BuildChargeFrames();
        BuildReloadFrames();
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), transform.localScale.z);
    }

    void LateUpdate()
    {
        if (Time.time < scaleLockUntil)
            transform.localScale = new Vector3(baseScale.x, baseScale.y * (crouching ? .72f : 1f), baseScale.z);
        if (playingReload && reloadFrames != null && spriteRenderer != null)
            spriteRenderer.sprite = reloadFrames[Mathf.Clamp(reloadFrameIndex, 0, reloadFrames.Length - 1)];
    }

    public void SetMoving(bool value, Vector2 axis)
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", value);
            float baseMove = stat.GetBaseValue("moveSpeed");
            animator.SetFloat("moveSpeed", baseMove == 0f ? 1f : stat.GetResultValue("moveSpeed") / baseMove);
        }
        if (Mathf.Abs(axis.x) > .01f)
            FaceDirection(axis.x);
    }

    public void FaceDirection(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) <= .01f) return;
        direction = Mathf.Sign(horizontalDirection);
        if (spriteRenderer != null) spriteRenderer.flipX = direction < 0f;
        if (motionRoutine == null)
            transform.localScale = new Vector3(baseScale.x, baseScale.y * (crouching ? .72f : 1f), baseScale.z);
    }

    public void Jump() { if (animator != null) animator.SetTrigger("Jump"); }
    public void Play(string state) { if (animator != null) animator.Play(state); }
    public void PlayLaunchAttack() => PlayMotion(CombatMotion.Launch);

    public void SetCrouching(bool crouching)
    {
        if (this.crouching == crouching)
            return;

        this.crouching = crouching;
        if (spriteRoutine != null) StopCoroutine(spriteRoutine);
        spriteRoutine = null;
        if (crouching && newMoveFrames != null)
            spriteRoutine = StartCoroutine(EnterCrouchThenSitIdle());
        else if (spriteRenderer != null)
            spriteRenderer.sprite = originalSprite;
        if (animator != null)
            animator.Play(crouching ? "Crouch" : "Idle", 0, 0f);
        if (motionRoutine == null)
            transform.localScale = new Vector3(baseScale.x, baseScale.y * (crouching ? .72f : 1f), baseScale.z);
    }

    public void PlayMotion(CombatMotion motion)
    {
        if (motion == CombatMotion.None) return;
        if (motion == CombatMotion.GunPlant) scaleLockUntil = Time.time + .4f;
        string state = MotionState(motion);
        if ((motion == CombatMotion.Flip || motion == CombatMotion.DashThrust) && newMoveFrames != null)
        {
            if (spriteRoutine != null) StopCoroutine(spriteRoutine);
            spriteRoutine = StartCoroutine(PlayFrames(motion == CombatMotion.Flip ? 6 : 12, 6, .08f));
        }
        else if (motion == CombatMotion.Burst && burstFrames != null)
        {
            if (spriteRoutine != null) StopCoroutine(spriteRoutine);
            spriteRoutine = StartCoroutine(PlayBurstFrames());
        }
        else if (motion == CombatMotion.ChargedShot && chargeFrames != null)
        {
            if (spriteRoutine != null) StopCoroutine(spriteRoutine);
            spriteRoutine = StartCoroutine(PlayShotgunRecoilFrames());
        }
        if (animator != null && animator.HasState(1, Animator.StringToHash(state)))
        {
            animator.Play(state, 1, 0f);
            return;
        }
        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = StartCoroutine(ProceduralMotion(motion));
    }

    void BuildNewMoveFrames()
    {
        Texture2D texture = Resources.Load<Texture2D>("PlayerNewMoves");
        if (texture == null) return;
        int width = texture.width / 6;
        int height = texture.height / 3;
        // Match the original player's 0.805-unit sprite height so every new clip
        // keeps the same on-screen scale as idle, run and existing attacks.
        float pixelsPerUnit = NormalizedPixelsPerUnit("PlayerNewMoves", height);
        newMoveFrames = new Sprite[18];
        for (int row = 0; row < 3; row++)
        for (int col = 0; col < 6; col++)
        {
            int index = row * 6 + col;
            Rect rect = new Rect(col * width, texture.height - (row + 1) * height, width, height);
            newMoveFrames[index] = Sprite.Create(texture, rect, new Vector2(.5f, .1f), pixelsPerUnit);
            newMoveFrames[index].name = "PlayerNewMove_" + index.ToString("00");
        }
    }

    void BuildBurstFrames()
    {
        Texture2D texture = Resources.Load<Texture2D>("PlayerBurst");
        if (texture == null) return;
        int width = texture.width / 6;
        int height = texture.height;
        float pixelsPerUnit = NormalizedPixelsPerUnit("PlayerBurst", height);
        burstFrames = new Sprite[6];
        for (int i = 0; i < burstFrames.Length; i++)
        {
            Rect rect = new Rect(i * width, 0, width, height);
            burstFrames[i] = Sprite.Create(texture, rect, new Vector2(.5f, .1f), pixelsPerUnit);
            burstFrames[i].name = "PlayerBurst_" + i.ToString("00");
        }
    }

    void BuildChargeFrames()
    {
        Texture2D texture = Resources.Load<Texture2D>("PlayerCharge");
        if (texture == null) return;
        int width = texture.width / 6;
        int height = texture.height;
        float pixelsPerUnit = NormalizedPixelsPerUnit("PlayerCharge", height);
        chargeFrames = new Sprite[6];
        for (int i = 0; i < chargeFrames.Length; i++)
        {
            Rect rect = new Rect(i * width, 0, width, height);
            chargeFrames[i] = Sprite.Create(texture, rect, new Vector2(.5f, .1f), pixelsPerUnit);
            chargeFrames[i].name = "PlayerCharge_" + i.ToString("00");
        }
    }

    void BuildReloadFrames()
    {
        Texture2D texture = Resources.Load<Texture2D>("PlayerReload");
        if (texture == null) return;
        int width = texture.width / 8;
        int height = texture.height;
        float pixelsPerUnit = NormalizedPixelsPerUnit("PlayerReload", height);
        reloadFrames = new Sprite[8];
        for (int i = 0; i < reloadFrames.Length; i++)
        {
            Rect rect = new Rect(i * width, 0, width, height);
            float pivotY = ImportedBottomPivotY("PlayerReload", i, height, width);
            reloadFrames[i] = Sprite.Create(texture, rect, new Vector2(.5f, pivotY), pixelsPerUnit);
            reloadFrames[i].name = "PlayerReload_" + i.ToString("00");
        }
    }

    static float ImportedBottomPivotY(string resourceName, int frameIndex, int textureHeight, int cellWidth)
    {
        Sprite[] importedFrames = Resources.LoadAll<Sprite>(resourceName);
        float cellCenter = (frameIndex + .5f) * cellWidth;
        Sprite matchingFrame = null;
        float nearestDistance = float.MaxValue;
        foreach (Sprite frame in importedFrames)
        {
            if (frame == null || frame.rect.width < 16f || frame.rect.height < 16f) continue;
            float distance = Mathf.Abs(frame.rect.center.x - cellCenter);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            matchingFrame = frame;
        }
        return matchingFrame == null ? .267f : Mathf.Clamp01(matchingFrame.rect.yMin / textureHeight);
    }

    public void StartReloadAnimation(float duration)
    {
        if (reloadFrames == null || spriteRenderer == null) return;
        playingReload = true;
        reloadFrameIndex = 0;
        if (spriteRoutine != null) StopCoroutine(spriteRoutine);
        spriteRoutine = StartCoroutine(PlayReloadFrames(duration));
    }

    public void StopReloadAnimation()
    {
        if (!playingReload) return;
        playingReload = false;
        if (spriteRoutine != null) StopCoroutine(spriteRoutine);
        spriteRoutine = null;
        if (spriteRenderer != null) spriteRenderer.sprite = originalSprite;
    }

    public void StartChargeAnimation(float duration)
    {
        if (chargeFrames == null || spriteRenderer == null) return;
        playingCharge = true;
        if (spriteRoutine != null) StopCoroutine(spriteRoutine);
        spriteRoutine = StartCoroutine(PlayChargeFrames(duration));
    }

    static float NormalizedPixelsPerUnit(string resourceName, int fallbackHeight)
    {
        // Generated sheets contain generous transparent padding. Unity's imported
        // sub-sprites expose the real opaque-frame height, so normalize against
        // that instead of the full texture height to prevent visual shrinking.
        Sprite[] importedFrames = Resources.LoadAll<Sprite>(resourceName);
        float visibleHeight = 0f;
        foreach (Sprite frame in importedFrames)
            if (frame != null && frame.rect.height > visibleHeight)
                visibleHeight = frame.rect.height;
        if (visibleHeight < 16f) visibleHeight = fallbackHeight * .38f;
        return visibleHeight / .805f;
    }

    public void StopChargeAnimation()
    {
        if (!playingCharge) return;
        playingCharge = false;
        if (spriteRoutine != null) StopCoroutine(spriteRoutine);
        spriteRoutine = null;
        if (spriteRenderer != null)
            spriteRenderer.sprite = crouching && newMoveFrames != null ? newMoveFrames[5] : originalSprite;
    }

    IEnumerator EnterCrouchThenSitIdle()
    {
        // Play the sit-down transition once.
        for (int i = 0; i < 5 && crouching; i++)
        {
            spriteRenderer.sprite = newMoveFrames[i];
            yield return new WaitForSeconds(.09f);
        }

        if (crouching && animator != null && animator.HasState(0, Animator.StringToHash("SitIdle")))
            animator.Play("SitIdle", 0, 0f);

        // SitIdle owns the repeating frames. This coroutine only waits for stand-up.
        while (crouching) yield return null;
        spriteRenderer.sprite = originalSprite;
        spriteRoutine = null;
    }

    IEnumerator PlayFrames(int start, int count, float frameTime)
    {
        for (int i = 0; i < count; i++)
        {
            spriteRenderer.sprite = newMoveFrames[start + i];
            yield return new WaitForSeconds(frameTime);
        }
        spriteRenderer.sprite = crouching ? newMoveFrames[5] : originalSprite;
        spriteRoutine = null;
    }

    IEnumerator PlayBurstFrames()
    {
        for (int i = 0; i < burstFrames.Length; i++)
        {
            spriteRenderer.sprite = burstFrames[i];
            yield return new WaitForSeconds(.09f);
        }
        spriteRenderer.sprite = crouching && newMoveFrames != null ? newMoveFrames[5] : originalSprite;
        spriteRoutine = null;
    }

    IEnumerator PlayChargeFrames(float duration)
    {
        float frameTime = duration / chargeFrames.Length;
        for (int i = 0; i < chargeFrames.Length && playingCharge; i++)
        {
            spriteRenderer.sprite = chargeFrames[i];
            yield return new WaitForSeconds(frameTime);
        }
        int pulseFrame = chargeFrames.Length - 2;
        while (playingCharge)
        {
            spriteRenderer.sprite = chargeFrames[pulseFrame];
            pulseFrame = pulseFrame == chargeFrames.Length - 1 ? chargeFrames.Length - 2 : chargeFrames.Length - 1;
            yield return new WaitForSeconds(.12f);
        }
    }

    IEnumerator PlayShotgunRecoilFrames()
    {
        int[] recoilFrames = { 5, 3, 1, 0 };
        foreach (int frame in recoilFrames)
        {
            spriteRenderer.sprite = chargeFrames[frame];
            yield return new WaitForSeconds(.065f);
        }
        spriteRenderer.sprite = crouching && newMoveFrames != null ? newMoveFrames[5] : originalSprite;
        spriteRoutine = null;
    }

    IEnumerator PlayReloadFrames(float duration)
    {
        float frameTime = duration / reloadFrames.Length;
        for (int i = 0; i < reloadFrames.Length && playingReload; i++)
        {
            reloadFrameIndex = i;
            yield return new WaitForSeconds(frameTime);
        }
        while (playingReload) yield return null;
    }

    static string MotionState(CombatMotion motion)
    {
        switch (motion)
        {
            case CombatMotion.Basic: return "Attack1";
            case CombatMotion.Launch: return "LaunchAttack";
            case CombatMotion.GunPlant: return "GunPlant";
            case CombatMotion.Burst: return "Burst";
            case CombatMotion.ChargedShot: return "ChargedShot";
            case CombatMotion.LegSweep: return "LegSweep";
            case CombatMotion.DashThrust: return "DashThrust";
            case CombatMotion.Flip: return "Flip";
            case CombatMotion.SwordEnemyPlant: return "SwordEnemyPlant";
            case CombatMotion.SwordPlantShot: return "SwordPlantShot";
            case CombatMotion.SwordPlantEject: return "SwordPlantEject";
            default: return "Attack Idle";
        }
    }

    IEnumerator ProceduralMotion(CombatMotion motion)
    {
        bool swordPlant = motion == CombatMotion.SwordEnemyPlant || motion == CombatMotion.SwordPlantShot;
        float duration = motion == CombatMotion.Flip ? .5f
            : motion == CombatMotion.SwordEnemyPlant ? .34f
            : motion == CombatMotion.SwordPlantShot ? .16f
            : motion == CombatMotion.SwordPlantEject ? .38f : .22f;
        float rotation = motion == CombatMotion.LegSweep ? -18f
            : motion == CombatMotion.DashThrust ? -8f
            : swordPlant ? -7f
            : motion == CombatMotion.SwordPlantEject ? 14f : 0f;
        float stretch = motion == CombatMotion.DashThrust ? 1.35f : motion == CombatMotion.GunPlant ? 1.18f : 1.08f;
        float time = 0f;
        while (time < duration)
        {
            float phase = time / duration;
            if (motion == CombatMotion.Flip) transform.rotation = Quaternion.Euler(0f, 0f, direction * -360f * phase);
            else transform.rotation = Quaternion.Euler(0f, 0f, rotation * Mathf.Sin(phase * Mathf.PI) * direction);
            float pulse = Mathf.Sin(phase * Mathf.PI);
            // Sword planting keeps the player size fixed; only the pose and
            // separately rendered blade move toward the contact point.
            transform.localScale = swordPlant
                ? new Vector3(baseScale.x, baseScale.y, baseScale.z)
                : new Vector3(baseScale.x * (1f + (stretch - 1f) * pulse), baseScale.y * (1f - .12f * pulse), baseScale.z);
            time += Time.deltaTime;
            yield return null;
        }
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(baseScale.x, baseScale.y * (crouching ? .72f : 1f), baseScale.z);
        motionRoutine = null;
    }
}
