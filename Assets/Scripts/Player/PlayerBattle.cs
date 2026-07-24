using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public struct AttackRange
{
    public Vector2 offset, size;
    public bool drawGizmos;
}

public class PlayerAmmoHUD : MonoBehaviour
{
    PlayerBattle battle;
    Text ammoCount;

    public void Initialize(PlayerBattle owner, Canvas canvas)
    {
        battle = owner;
        if (canvas == null) return;
        GameObject panel = new GameObject("Ammo Magazine HUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = canvas.gameObject.layer;
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();
        RectTransform root = panel.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-32f, 28f);
        root.sizeDelta = new Vector2(190f, 76f);
        panel.GetComponent<Image>().color = new Color(.035f, .04f, .05f, .95f);

        GameObject bullet = new GameObject("Bullet Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bullet.layer = panel.layer;
        bullet.transform.SetParent(panel.transform, false);
        RectTransform bulletRect = bullet.GetComponent<RectTransform>();
        bulletRect.anchorMin = bulletRect.anchorMax = new Vector2(0f, .5f);
        bulletRect.pivot = new Vector2(.5f, .5f);
        bulletRect.anchoredPosition = new Vector2(42f, 0f);
        bulletRect.sizeDelta = new Vector2(20f, 46f);
        Image bulletImage = bullet.GetComponent<Image>();
        Sprite bulletSprite = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(.5f, .5f), 1f);
        bulletSprite.name = "Runtime Ammo Icon Sprite";
        bulletImage.sprite = bulletSprite;
        bulletImage.type = Image.Type.Simple;
        bulletImage.color = new Color(1f, .67f, .12f, 1f);

        GameObject tip = new GameObject("Bullet Tip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tip.layer = panel.layer;
        tip.transform.SetParent(bullet.transform, false);
        RectTransform tipRect = tip.GetComponent<RectTransform>();
        tipRect.anchorMin = tipRect.anchorMax = new Vector2(.5f, 1f);
        tipRect.pivot = new Vector2(.5f, .5f);
        tipRect.anchoredPosition = new Vector2(0f, 2f);
        tipRect.sizeDelta = new Vector2(14f, 14f);
        tipRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image tipImage = tip.GetComponent<Image>();
        tipImage.sprite = bulletSprite;
        tipImage.type = Image.Type.Simple;
        tipImage.color = bulletImage.color;

        GameObject countObject = new GameObject("Remaining Ammo Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        countObject.layer = panel.layer;
        countObject.transform.SetParent(panel.transform, false);
        RectTransform countRect = countObject.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(.34f, .1f);
        countRect.anchorMax = new Vector2(.96f, .9f);
        countRect.offsetMin = countRect.offsetMax = Vector2.zero;
        ammoCount = countObject.GetComponent<Text>();
        ammoCount.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ammoCount.fontSize = 38;
        ammoCount.fontStyle = FontStyle.Bold;
        ammoCount.alignment = TextAnchor.MiddleLeft;
        ammoCount.color = Color.white;
        Refresh();
    }

    public void Refresh()
    {
        if (battle == null) return;
        if (ammoCount != null) ammoCount.text = "x " + battle.CurrentAmmo;
    }
}

public class PlayerBattle : MonoBehaviour
{
    const string PhaseOneSceneName = "SampleScene";

    public EntityHealth health;
    public PlayerMovement movement;
    public PlayerStat stat;
    public float atkCool;
    public bool inDash;
    public AttackRange defaultAttack;

    [Header("Melee Skills")]
    [SerializeField] AttackRange launchAttackRange = new AttackRange { offset = new Vector2(1f, .7f), size = new Vector2(1.3f, 1.6f), drawGizmos = true };
    [SerializeField] AttackRange sweepRange = new AttackRange { offset = new Vector2(1f, .25f), size = new Vector2(2f, .55f), drawGizmos = true };
    [SerializeField] AttackRange dashThrustRange = new AttackRange { offset = new Vector2(2f, .55f), size = new Vector2(4f, .8f), drawGizmos = true };
    [SerializeField] AttackRange gunPlantRange = new AttackRange { offset = new Vector2(.8f, .65f), size = new Vector2(1.4f, 1.3f), drawGizmos = true };
    [SerializeField] float swordPlantDamageMultiplier = 1.35f;
    [SerializeField] float launchPower = 10f;
    [SerializeField] float wallSlamBonus = 2.2f;

    [Header("Gun Skills")]
    [SerializeField] float gunRange = 18f;
    [SerializeField] float bulletDamage = 10f;
    [SerializeField] float burstInterval = .09f;
    [SerializeField] float chargeStartDelay = 1f;
    [SerializeField] float chargeDuration = 2f;
    [SerializeField] float recoilPower = 9f;
    [SerializeField] int magazineSize = 30;
    [SerializeField, Min(1)] int swordPlantShotAmmoCost = 1;
    [SerializeField] AudioClip[] assaultRifleShotSounds;
    [SerializeField] AudioClip chargedShotgunSound;
    const float ReloadDuration = 2.5f;

    [Header("Skill Cooldowns")]
    [SerializeField] float launchSkillCooldown = 2.5f;
    [SerializeField] float gunPlantSkillCooldown = 8f;
    [SerializeField] float secondarySkillCooldown = 1.2f;
    [SerializeField] float legSweepCooldown = 0.8f;
    [SerializeField] float dashThrustCooldown = 1f;

    [Header("E Empower")]
    [SerializeField] float empowerDuration = 5f;
    [SerializeField] float empowerAttackIncrease = 60f;
    [SerializeField] float empowerAttackSpeedIncrease = 60f;

    [SerializeField] LayerMask enermymask;
    [SerializeField] DamageIndicator indicator;
    [SerializeField] Slider healthbar;
    float secondaryPressedAt;
    bool secondaryHeld;
    Coroutine secondaryChargeRoutine;
    int currentAmmo;
    bool reloading;
    Coroutine reloadRoutine;
    PlayerAmmoHUD ammoHUD;
    float launchSkillTimer;
    float gunPlantSkillTimer;
    float secondarySkillTimer;
    float legSweepTimer;
    float dashThrustTimer;
    Coroutine empowerRoutine;
    EntityHealth swordPlantedTarget;
    int swordPlantDirection;
    int swordPlantHitCount;
    GameObject plantedSwordVisual;
    AudioSource weaponAudioSource;
    int swordPlantSoundIndex;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => magazineSize;
    public bool IsReloading => reloading;
    public bool IsSwordPlanted => swordPlantedTarget != null;

    void Start()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<PlayerStat>();
        movement = GetComponent<PlayerMovement>();
        weaponAudioSource = gameObject.AddComponent<AudioSource>();
        weaponAudioSource.playOnAwake = false;
        weaponAudioSource.spatialBlend = 0f;
        weaponAudioSource.volume = .8f;
        magazineSize = 30;
        currentAmmo = magazineSize;
        StartCoroutine(CreateAmmoHUD());
        health.Ondamage(OnHurt);
        health.OnDeath(OnPlayerDeath);
    }

    void OnPlayerDeath(EntityHealth.Context ctx)
    {
        SceneManager.LoadScene(PhaseOneSceneName);
    }

    IEnumerator CreateAmmoHUD()
    {
        // Wait until the scene Canvas and its camera have completed initialization.
        yield return null;
        Canvas canvas = healthbar == null ? null : healthbar.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) yield break;

        GameObject existing = GameObject.Find("Ammo Magazine HUD");
        if (existing != null) Destroy(existing);
        ammoHUD = gameObject.AddComponent<PlayerAmmoHUD>();
        ammoHUD.Initialize(this, canvas);
    }

    void Update()
    {
        if (IsSwordPlanted)
        {
            if (swordPlantedTarget.isDeath)
                ReleaseSwordPlant(false);
            else
                transform.position = swordPlantedTarget.transform.position
                    + new Vector3(-swordPlantDirection * .82f, .18f, 0f);
        }
        inDash = movement != null && movement.IsDashing;
        if (healthbar != null && health != null)
            healthbar.value = health.maxHealth <= 0f ? 0f : health.health / health.maxHealth;
        if (atkCool > 0f)
            atkCool -= Time.deltaTime * (1f + stat.GetResultValue("atkSpeed") / 100f);
        launchSkillTimer = Mathf.Max(0f, launchSkillTimer - Time.deltaTime);
        gunPlantSkillTimer = Mathf.Max(0f, gunPlantSkillTimer - Time.deltaTime);
        secondarySkillTimer = Mathf.Max(0f, secondarySkillTimer - Time.deltaTime);
        legSweepTimer = Mathf.Max(0f, legSweepTimer - Time.deltaTime);
        dashThrustTimer = Mathf.Max(0f, dashThrustTimer - Time.deltaTime);
    }

    void OnHurt(EntityHealth.Context ctx)
    {
        if (inDash) ctx.canceled = true;
        if (IsSwordPlanted && !ctx.canceled)
        {
            swordPlantHitCount++;
            if (swordPlantHitCount >= 3)
                ReleaseSwordPlant(true);
        }
        if (!ctx.canceled && indicator != null)
            indicator.indicateDamage(ctx.damage, transform.position + Vector3.up,
                ctx.isCritical ? Color.yellow : Color.red, ctx.isCritical ? 1.55f : 1f);
    }

    public void Dash(int direction)
    {
        if (reloading) CancelReload();
        if (health == null || !health.IsStunned) movement.TryDash(direction);
    }

    public PlayerAnimator.CombatMotion ContextualAttack(int direction)
    {
        if (IsSwordPlanted || reloading || atkCool > 0f || (health != null && health.IsStunned)) return PlayerAnimator.CombatMotion.None;
        direction = NormalizeDirection(direction);

        if (movement.IsDashing)
        {
            if (dashThrustTimer > 0f) return PlayerAnimator.CombatMotion.None;
            dashThrustTimer = dashThrustCooldown;
            atkCool = .55f;
            AttackRange thrustRange = dashThrustRange;
            // Cover the distance swept during the dash, including a small area
            // behind the current position in case the player crossed the target
            // between physics frames.
            thrustRange.offset = new Vector2(1.4f, Mathf.Approximately(thrustRange.offset.y, 0f) ? .55f : thrustRange.offset.y);
            thrustRange.size = new Vector2(Mathf.Max(5.5f, thrustRange.size.x), Mathf.Max(1.15f, thrustRange.size.y));
            HitRange(thrustRange, direction, 1.8f, 0f, true);
            return PlayerAnimator.CombatMotion.DashThrust;
        }
        if (movement.IsCrouching)
        {
            if (legSweepTimer > 0f) return PlayerAnimator.CombatMotion.None;
            legSweepTimer = legSweepCooldown;
            atkCool = .55f;
            HitRange(sweepRange, direction, 1.15f, 0f, false);
            return PlayerAnimator.CombatMotion.LegSweep;
        }

        atkCool = .5f;
        HitRange(defaultAttack, direction, 1f, 0f, true);
        return PlayerAnimator.CombatMotion.Basic;
    }

    public bool BasicAttack(int direction) => ContextualAttack(direction) != PlayerAnimator.CombatMotion.None;

    public PlayerAnimator.CombatMotion TrySwordPlant(int direction)
    {
        if (IsSwordPlanted || reloading || atkCool > 0f || (health != null && health.IsStunned))
            return PlayerAnimator.CombatMotion.None;

        direction = NormalizeDirection(direction);
        if (!TryFindMeleeTarget(defaultAttack, direction, out EntityHealth target))
            return PlayerAnimator.CombatMotion.None;

        Boss boss = target.GetComponent<Boss>();
        if (boss != null && !boss.CanBeSwordPlanted)
            return PlayerAnimator.CombatMotion.None;

        swordPlantedTarget = target;
        swordPlantDirection = direction;
        swordPlantHitCount = 0;
        swordPlantSoundIndex = 0;
        atkCool = .65f;
        secondaryHeld = false;
        CancelSecondaryCharge();
        movement.SetCrouching(false);
        movement.ActionLocked = true;
        movement.SetVelocity(Vector2.zero);
        transform.position = target.transform.position + new Vector3(-direction * .82f, .18f, 0f);
        target.GetDamage(stat.GetResultValue("attackDamage") * swordPlantDamageMultiplier, health);
        CreatePlantedSwordVisual(direction);
        return PlayerAnimator.CombatMotion.SwordEnemyPlant;
    }

    public bool LaunchAttack(int direction)
    {
        if (IsSwordPlanted || reloading || atkCool > 0f || launchSkillTimer > 0f || (health != null && health.IsStunned)) return false;
        launchSkillTimer = launchSkillCooldown;
        atkCool = .65f;
        HitRange(launchAttackRange, NormalizeDirection(direction), 0f, launchPower, false);
        return true;
    }

    public bool GunPlant(int direction)
    {
        if (IsSwordPlanted || reloading || atkCool > 0f || gunPlantSkillTimer > 0f || (health != null && health.IsStunned) || !TryConsumeAmmo(1)) return false;
        gunPlantSkillTimer = gunPlantSkillCooldown;
        atkCool = .9f;
        direction = NormalizeDirection(direction);
        List<EntityHealth> hits = HitRange(gunPlantRange, direction, 1.5f, 0f, true);
        foreach (EntityHealth target in hits)
        {
            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null)
                targetBody.linearVelocity = new Vector2(direction * 13f, 3f);
        }
        if (empowerRoutine != null) StopCoroutine(empowerRoutine);
        empowerRoutine = StartCoroutine(EmpowerRoutine());
        return true;
    }

    public PlayerAnimator.CombatMotion SetSecondaryHeld(bool held, Vector2 aimWorldPosition)
    {
        if (IsSwordPlanted)
        {
            if (held && !secondaryHeld)
            {
                secondaryHeld = true;
                return FireAtSwordPlant()
                    ? PlayerAnimator.CombatMotion.SwordPlantShot
                    : PlayerAnimator.CombatMotion.None;
            }
            if (!held) secondaryHeld = false;
            return PlayerAnimator.CombatMotion.None;
        }
        if (reloading)
        {
            CancelSecondaryCharge();
            return PlayerAnimator.CombatMotion.None;
        }
        if (health != null && health.IsStunned)
        {
            CancelSecondaryCharge();
            return PlayerAnimator.CombatMotion.None;
        }
        if (held)
        {
            if (secondaryHeld) return PlayerAnimator.CombatMotion.None;
            secondaryHeld = true;
            secondaryPressedAt = Time.time;
            if (secondaryChargeRoutine != null) StopCoroutine(secondaryChargeRoutine);
            secondaryChargeRoutine = StartCoroutine(SecondaryChargeRoutine());
            return PlayerAnimator.CombatMotion.None;
        }
        if (!secondaryHeld) return PlayerAnimator.CombatMotion.None;
        secondaryHeld = false;
        float heldTime = Time.time - secondaryPressedAt;
        float chargeAmount = Mathf.Clamp01((heldTime - chargeStartDelay) / chargeDuration);
        GetComponent<PlayerAnimator>()?.StopChargeAnimation();
        if (secondaryChargeRoutine != null) StopCoroutine(secondaryChargeRoutine);
        secondaryChargeRoutine = null;
        Vector2 muzzlePosition = (Vector2)transform.position + Vector2.up * .65f;
        Vector2 aimDirection = aimWorldPosition - muzzlePosition;
        if (aimDirection.sqrMagnitude < .0001f) aimDirection = Vector2.right;
        aimDirection.Normalize();

        if (heldTime >= chargeStartDelay)
        {
            return TryStartChargedShotgun(aimDirection, chargeAmount)
                ? PlayerAnimator.CombatMotion.ChargedShot
                : PlayerAnimator.CombatMotion.None;
        }

        if (heldTime < chargeStartDelay)
        {
            return TryStartThreeRoundBurst(aimDirection)
                ? PlayerAnimator.CombatMotion.Burst
                : PlayerAnimator.CombatMotion.None;
        }

        return PlayerAnimator.CombatMotion.None;
    }

    bool TryStartThreeRoundBurst(Vector2 direction)
    {
        if (atkCool > 0f || secondarySkillTimer > 0f || !TryConsumeAmmo(3)) return false;
        atkCool = .6f;
        secondarySkillTimer = secondarySkillCooldown;
        StartCoroutine(ThreeRoundBurst(direction));
        return true;
    }

    IEnumerator ThreeRoundBurst(Vector2 direction)
    {
        for (int i = 0; i < 3; i++)
        {
            FireHitscan(direction, 1f, Color.yellow, .06f);
            if (assaultRifleShotSounds != null && i < assaultRifleShotSounds.Length)
                PlayWeaponSound(assaultRifleShotSounds[i]);
            yield return new WaitForSeconds(burstInterval);
        }
    }

    bool TryStartChargedShotgun(Vector2 direction, float chargeAmount)
    {
        int shells = 1 + Mathf.FloorToInt(Mathf.Clamp01(chargeAmount) * 5f);
        if (atkCool > 0f || secondarySkillTimer > 0f || !TryConsumeAmmo(shells)) return false;
        atkCool = 1f;
        secondarySkillTimer = secondarySkillCooldown;
        PlayWeaponSound(chargedShotgunSound);
        FireShotgun(direction, shells, chargeAmount);
        float scaledRecoil = Mathf.Lerp(recoilPower * .45f, recoilPower * 1.65f, chargeAmount);
        Vector2 velocity = -direction * scaledRecoil;
        velocity.y = Mathf.Max(2f, velocity.y + movement.VerticalVelocity);
        movement.SetVelocity(velocity);
        return true;
    }

    IEnumerator SecondaryChargeRoutine()
    {
        yield return new WaitForSeconds(chargeStartDelay);
        PlayerAnimator playerAnimator = GetComponent<PlayerAnimator>();
        playerAnimator?.StartChargeAnimation(chargeDuration);
        float elapsed = 0f;
        while (secondaryHeld && elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        secondaryChargeRoutine = null;
    }

    void CancelSecondaryCharge()
    {
        GetComponent<PlayerAnimator>()?.StopChargeAnimation();
        secondaryHeld = false;
        if (secondaryChargeRoutine != null) StopCoroutine(secondaryChargeRoutine);
        secondaryChargeRoutine = null;
    }

    IEnumerator EmpowerRoutine()
    {
        PlayerStat.Buf attackBuff = new PlayerStat.Buf
        {
            Key = "attackDamage",
            mathType = MathType.Increase,
            Value = empowerAttackIncrease
        };
        PlayerStat.Buf speedBuff = new PlayerStat.Buf
        {
            Key = "atkSpeed",
            mathType = MathType.Increase,
            Value = empowerAttackSpeedIncrease
        };
        stat.bufs.Add(attackBuff);
        stat.bufs.Add(speedBuff);
        stat.Calc("attackDamage");
        stat.Calc("atkSpeed");

        ParticleSystem aura = CreateEmpowerAura();
        PlayerAnimator playerAnimator = GetComponent<PlayerAnimator>();
        float endTime = Time.time + empowerDuration;
        while (Time.time < endTime)
        {
            if (aura != null)
            {
                float facing = playerAnimator == null ? 1f : playerAnimator.direction;
                aura.transform.localPosition = new Vector3(-facing * .42f, .5f, .15f);
            }
            yield return null;
        }

        stat.bufs.Remove(attackBuff);
        stat.bufs.Remove(speedBuff);
        stat.Calc("attackDamage");
        stat.Calc("atkSpeed");
        if (aura != null) Destroy(aura.gameObject);
        empowerRoutine = null;
    }

    ParticleSystem CreateEmpowerAura()
    {
        GameObject auraObject = new GameObject("E Empower Red Flame Aura");
        auraObject.transform.SetParent(transform, false);
        ParticleSystem aura = auraObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = aura.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.25f, .55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.3f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(.08f, .22f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .05f, .02f, .9f), new Color(1f, .35f, .02f, .8f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        ParticleSystem.EmissionModule emission = aura.emission;
        emission.rateOverTime = 24f;
        ParticleSystem.ShapeModule shape = aura.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = .28f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        ParticleSystemRenderer auraRenderer = aura.GetComponent<ParticleSystemRenderer>();
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            auraRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            auraRenderer.sortingOrder = playerRenderer.sortingOrder - 1;
        }
        auraRenderer.material = new Material(Shader.Find("Sprites/Default"));
        aura.Play();
        return aura;
    }

    bool TryConsumeAmmo(int amount)
    {
        if (reloading || currentAmmo < amount) return false;
        currentAmmo -= amount;
        ammoHUD?.Refresh();
        if (IsSwordPlanted && currentAmmo <= 0)
            ReleaseSwordPlant(false);
        return true;
    }

    public bool TryStartReload()
    {
        if (IsSwordPlanted || reloading || currentAmmo >= magazineSize || (health != null && health.IsStunned)) return false;
        CancelSecondaryCharge();
        reloading = true;
        GetComponent<PlayerAnimator>()?.StartReloadAnimation(ReloadDuration);
        reloadRoutine = StartCoroutine(Reload());
        return true;
    }

    IEnumerator Reload()
    {
        yield return new WaitForSeconds(ReloadDuration);
        if (!reloading) yield break;
        currentAmmo = magazineSize;
        reloading = false;
        reloadRoutine = null;
        GetComponent<PlayerAnimator>()?.StopReloadAnimation();
        ammoHUD?.Refresh();
    }

    void CancelReload()
    {
        if (!reloading) return;
        reloading = false;
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;
        GetComponent<PlayerAnimator>()?.StopReloadAnimation();
    }

    void FireHitscan(Vector2 direction, float damageMultiplier, Color color, float width)
    {
        direction.Normalize();
        Vector2 origin = (Vector2)transform.position + Vector2.up * .65f + direction * .55f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, gunRange, enermymask);
        foreach (RaycastHit2D hit in hits)
        {
            EntityHealth target = hit.collider.GetComponentInParent<EntityHealth>();
            if (target != null && target != health)
            {
                target.GetDamage(bulletDamage * damageMultiplier, health);
                break;
            }
        }
        StartCoroutine(ShowTracer(origin, origin + direction * gunRange, color, width));
    }

    void FireShotgun(Vector2 direction, int shells, float chargeAmount)
    {
        float spread = Mathf.Lerp(11f, 5f, chargeAmount);
        for (int i = 0; i < shells; i++)
        {
            float normalized = shells == 1 ? 0f : i / (float)(shells - 1) * 2f - 1f;
            Vector2 pelletDirection = Quaternion.Euler(0f, 0f, normalized * spread) * direction;
            FireHitscan(pelletDirection, 1f, new Color(1f, .72f, .2f), .075f);
        }
    }

    IEnumerator ShowTracer(Vector2 start, Vector2 end, Color color, float width)
    {
        GameObject tracer = new GameObject("Player Gun Tracer");
        LineRenderer line = tracer.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = line.endWidth = width;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0f);
        yield return new WaitForSeconds(.07f);
        Destroy(tracer);
    }

    List<EntityHealth> HitRange(AttackRange range, int direction, float multiplier, float verticalPower, bool wallSlam)
    {
        Vector2 offset = range.offset;
        offset.x = Mathf.Abs(offset.x) * direction;
        Collider2D[] colliders = Physics2D.OverlapBoxAll((Vector2)transform.position + offset, range.size, 0f, enermymask);
        HashSet<EntityHealth> unique = new HashSet<EntityHealth>();
        foreach (Collider2D col in colliders)
        {
            EntityHealth target = col.GetComponentInParent<EntityHealth>();
            if (target == null || target == health || !unique.Add(target)) continue;
            float damage = stat.GetResultValue("attackDamage") * multiplier;
            if (wallSlam && IsPinnedToWall(target, direction)) damage *= wallSlamBonus;
            if (multiplier > 0f)
                target.GetDamage(damage, health);
            Rigidbody2D body = target.GetComponent<Rigidbody2D>();
            if (body != null && verticalPower > 0f)
            {
                Boss boss = target.GetComponent<Boss>();
                if (boss != null && !boss.CanBeLaunched)
                    continue;
                body.linearVelocity = new Vector2(body.linearVelocity.x, verticalPower);
                if (boss != null)
                    boss.InterruptForLaunch();
            }
        }
        return new List<EntityHealth>(unique);
    }

    bool IsPinnedToWall(EntityHealth target, int direction)
    {
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Vector2 origin = targetCollider == null ? target.transform.position : targetCollider.bounds.center;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, Vector2.right * direction, .65f))
            if (hit.collider.GetComponentInParent<EntityHealth>() == null && !hit.collider.isTrigger) return true;
        return false;
    }

    bool TryFindMeleeTarget(AttackRange range, int direction, out EntityHealth target)
    {
        Vector2 offset = range.offset;
        offset.x = Mathf.Abs(offset.x) * direction;
        foreach (Collider2D col in Physics2D.OverlapBoxAll((Vector2)transform.position + offset, range.size, 0f, enermymask))
        {
            EntityHealth candidate = col.GetComponentInParent<EntityHealth>();
            if (candidate == null || candidate == health || candidate.isDeath) continue;
            target = candidate;
            return true;
        }
        target = null;
        return false;
    }

    void CreatePlantedSwordVisual(int direction)
    {
        if (plantedSwordVisual != null) Destroy(plantedSwordVisual);
        plantedSwordVisual = new GameObject("Sword Planted In Enemy");
        plantedSwordVisual.transform.SetParent(transform, false);
        LineRenderer blade = plantedSwordVisual.AddComponent<LineRenderer>();
        blade.positionCount = 2;
        blade.useWorldSpace = false;
        blade.SetPosition(0, new Vector3(direction * .18f, .58f, 0f));
        blade.SetPosition(1, new Vector3(direction * 1.08f, .58f, 0f));
        blade.startWidth = .1f;
        blade.endWidth = .025f;
        blade.material = new Material(Shader.Find("Sprites/Default"));
        blade.startColor = Color.white;
        blade.endColor = new Color(.7f, .85f, 1f);
    }

    bool FireAtSwordPlant()
    {
        if (!IsSwordPlanted || atkCool > 0f) return false;

        // The planted shot is a real firearm attack: reserve its ammunition
        // before applying damage or spawning the impact effect.
        int ammoCost = Mathf.Max(1, swordPlantShotAmmoCost);
        if (!TryConsumeAmmo(ammoCost)) return false;

        atkCool = .22f;
        if (assaultRifleShotSounds != null && assaultRifleShotSounds.Length > 0)
        {
            int soundCount = Mathf.Min(6, assaultRifleShotSounds.Length);
            PlayWeaponSound(assaultRifleShotSounds[swordPlantSoundIndex % soundCount]);
            swordPlantSoundIndex = (swordPlantSoundIndex + 1) % soundCount;
        }
        swordPlantedTarget.GetDamage(bulletDamage, health);
        CreateSwordShotBurst(swordPlantedTarget.transform.position);
        return true;
    }

    void PlayWeaponSound(AudioClip clip)
    {
        if (clip == null || weaponAudioSource == null) return;
        weaponAudioSource.PlayOneShot(clip);
    }

    void CreateSwordShotBurst(Vector2 position)
    {
        GameObject burstObject = new GameObject("Sword Plant Shot Yellow Burst");
        burstObject.transform.position = position;
        ParticleSystem burst = burstObject.AddComponent<ParticleSystem>();
        // A newly added ParticleSystem can begin playing immediately. Stop and
        // clear it before changing duration, which Unity forbids while playing.
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = burst.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = .2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.16f, .32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(.035f, .11f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .95f, .15f), new Color(1f, .5f, .03f));
        ParticleSystem.EmissionModule emission = burst.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.ShapeModule shape = burst.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = .18f;
        ParticleSystemRenderer renderer = burst.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        burst.Emit(18);
        Destroy(burstObject, .6f);
    }

    public void ReleaseSwordPlant(bool knockedAway)
    {
        if (!IsSwordPlanted) return;
        swordPlantedTarget = null;
        movement.ActionLocked = false;
        if (plantedSwordVisual != null) Destroy(plantedSwordVisual);
        plantedSwordVisual = null;
        secondaryHeld = false;
        if (knockedAway)
        {
            movement.SetVelocity(new Vector2(-swordPlantDirection * 12f, 6f));
            GetComponent<PlayerAnimator>()?.PlayMotion(PlayerAnimator.CombatMotion.SwordPlantEject);
        }
    }

    int NormalizeDirection(int direction) => direction == 0 ? 1 : (int)Mathf.Sign(direction);

    void Draw(AttackRange range)
    {
        if (!range.drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + range.offset, range.size);
    }

    void OnDrawGizmos()
    {
        Draw(defaultAttack); Draw(launchAttackRange); Draw(sweepRange); Draw(dashThrustRange); Draw(gunPlantRange);
    }
}
