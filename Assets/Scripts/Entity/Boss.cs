using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Boss : Enemy
{
    public enum PhaseOneForm { Basic, Small, Large, Special }
    enum PhaseThreeStance { Attack, Defense }
    enum ReflectMode { None, Melee, Ranged }

    [Header("References")]
    [SerializeField] PlayerController player;
    [SerializeField] Slider bossbar;

    [Header("Form Cycle")]
    [SerializeField] float basicFormDuration = 6f;
    [SerializeField] float transformedFormDuration = 9f;
    [SerializeField] float patternDelay = 0.7f;
    [SerializeField] float formChangeWindup = 1f;
    public PhaseOneForm currentForm = PhaseOneForm.Basic;

    [Header("Visual Size Relative To Player")]
    [SerializeField] float basicPlayerHeightRatio = 1.25f;
    [SerializeField] float smallPlayerHeightRatio = 0.8f;
    [SerializeField] float largePlayerHeightRatio = 3f;
    [SerializeField] float specialPlayerHeightRatio = 1.35f;
    [SerializeField] float phaseThreePlayerHeightRatio = 1.2f;

    [Header("Basic Form")]
    public float attackDist = 1.5f;
    [SerializeField] AttackRange defaultAttack;
    public float dashRange = 5f;
    public float dashPower = 12f;
    public float dashDuration = 0.4f;
    public float dashCoolTime = 3f;
    [SerializeField] AttackRange dashAttack;
    public float jumpPower = 7f;
    public float fallSpeed = 25f;
    public float jumpCoolTime = 6f;
    [SerializeField] AttackRange jumpAttack;
    public float retreatTime = 0.6f;

    [Header("Projectile Settings")]
    [SerializeField] float projectileDamage = 8f;
    [SerializeField] float projectileSpeed = 7f;
    [SerializeField] float turretDuration = 7f;
    [SerializeField] float rainHeight = 6f;

    [Header("Large Form")]
    [SerializeField] float largeDamage = 18f;
    [SerializeField] AttackRange largeSwingRange = new AttackRange
    {
        offset = new Vector2(1.5f, 0.5f),
        size = new Vector2(4f, 2.5f),
        drawGizmos = true
    };

    [Header("Special Form")]
    [SerializeField] AttackRange specialSlashRange = new AttackRange
    {
        offset = new Vector2(1f, 0.5f),
        size = new Vector2(2.5f, 2f),
        drawGizmos = true
    };

    float baseScaleY;
    float cachedPlayerHeight;
    float dashCool;
    float jumpCool;
    float retreatTimer;
    bool initialized;
    bool inPattern;
    Coroutine phaseRoutine;
    BossVisual visual;
    bool enteredFirstForm;
    bool phaseThreeBattle;
    bool halfHealthTransitionStarted;
    PhaseThreeStance phaseThreeStance;
    ReflectMode reflectMode;
    float phaseThreeCharge;
    float phaseThreeAttackBonus;
    GameObject stanceVisual;
    readonly List<GameObject> activeWarnings = new List<GameObject>();
    RectTransform chargeFill;
    Text chargeText;
    Image chargeFrame;
    int attackHitStreak;
    float lastAttackHitTime;
    bool forceDefenseRequested;
    float nextSurpriseDefenseTime;
    public bool IsPhaseThreeDefense => phaseThreeBattle && phaseThreeStance == PhaseThreeStance.Defense;
    public bool CanBeLaunched => currentForm != PhaseOneForm.Large && !IsPhaseThreeDefense;
    public bool CanBeSwordPlanted => currentForm != PhaseOneForm.Small && !IsPhaseThreeDefense;

    void Awake()
    {
        dashCool = dashCoolTime;
        jumpCool = jumpCoolTime;
        baseScaleY = Mathf.Abs(transform.localScale.y);
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.white;
            visual = gameObject.AddComponent<BossVisual>();
            visual.Initialize(renderer);
        }
    }

    protected override void MobUpdate()
    {
        if (health == null || stat == null || rigid == null)
            return;

        if (!initialized)
        {
            initialized = true;
            health.OnDeath(OnBossDeath);
            health.Ondamage(OnBossDamagedForTransition);
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            CachePlayerHeight();
            phaseThreeBattle = SceneManager.GetActiveScene().name == "BattleScene2";
            if (phaseThreeBattle)
            {
                health.health = health.maxHealth * .5f;
                CreatePhaseThreeChargeHUD();
                SetForm(PhaseOneForm.Special);
                phaseRoutine = StartCoroutine(PhaseThreeLoop());
            }
            else
            {
                phaseRoutine = StartCoroutine(PhaseOneLoop());
            }
        }

        if (bossbar != null)
            bossbar.value = health.maxHealth <= 0f ? 0f : health.health / health.maxHealth;
        if (phaseThreeBattle) RefreshPhaseThreeChargeHUD();

        dashCool -= Time.deltaTime;
        jumpCool -= Time.deltaTime;
        retreatTimer -= Time.deltaTime;

        if (!inPattern && player != null && currentForm == PhaseOneForm.Basic)
            BasicFallbackMovement();
    }

    void OnBossDeath(EntityHealth.Context ctx)
    {
        if (bossbar != null)
            bossbar.value = 0f;
        if (phaseThreeBattle)
            GameMusicManager.StopBattleMusic();
    }

    void OnBossDamagedForTransition(EntityHealth.Context ctx)
    {
        if (phaseThreeBattle && IsPhaseThreeDefense)
        {
            bool isMelee = ctx.attacker != null && Vector2.Distance(ctx.attacker.transform.position, transform.position) <= 2.3f;
            bool shouldReflect = reflectMode == ReflectMode.Melee && isMelee || reflectMode == ReflectMode.Ranged && !isMelee;
            // Defense stance always blocks health damage. Incoming damage still
            // feeds the stance gauge, while only the matching guard type reflects.
            phaseThreeCharge = Mathf.Clamp(phaseThreeCharge + ctx.damage * 6f, 0f, 100f);
            ctx.canceled = true;
            if (shouldReflect && ctx.attacker != null)
                ctx.attacker.GetDamage(Mathf.Max(3f, ctx.damage), health);
            return;
        }

        if (phaseThreeBattle && !IsPhaseThreeDefense)
        {
            if (Time.time - lastAttackHitTime > .7f) attackHitStreak = 0;
            lastAttackHitTime = Time.time;
            attackHitStreak++;
            if (attackHitStreak >= 6 && Time.time >= nextSurpriseDefenseTime && !forceDefenseRequested)
            {
                forceDefenseRequested = true;
                nextSurpriseDefenseTime = Time.time + 12f;
                SetPhaseThreeStance(PhaseThreeStance.Defense);
            }
            return;
        }

        if (phaseThreeBattle || halfHealthTransitionStarted || health == null || health.isDeath) return;
        if (health.health - ctx.damage > health.maxHealth * .5f) return;

        halfHealthTransitionStarted = true;
        ctx.canceled = true;
        SetVelocity(Vector2.zero);
        SceneManager.LoadScene("BattleScene2");
    }

    public void InterruptForLaunch()
    {
        if (health == null || health.isDeath || !CanBeLaunched)
            return;

        StopAllCoroutines();
        phaseRoutine = null;
        inPattern = true;
        retreatTimer = 0f;
        atkCool = 0f;
        ClearActivePatternObjects();
        StartCoroutine(WaitForLandingAndRestart());
    }

    IEnumerator WaitForLandingAndRestart()
    {
        float stunEndsAt = Time.time + 1f;
        bool leftGround = false;
        float landingSafety = 6f;
        while (health != null && !health.isDeath && landingSafety > 0f)
        {
            bool grounded = OnGround();
            if (!grounded) leftGround = true;
            if (leftGround && grounded && (rigid == null || rigid.linearVelocity.y <= .05f))
                break;
            landingSafety -= Time.deltaTime;
            yield return null;
        }

        while (Time.time < stunEndsAt && health != null && !health.isDeath)
            yield return null;

        if (health == null || health.isDeath)
            yield break;

        SetVelocity(Vector2.zero);
        dashCool = dashCoolTime;
        jumpCool = jumpCoolTime;
        retreatTimer = 0f;
        enteredFirstForm = false;
        SetForm(phaseThreeBattle ? PhaseOneForm.Special : PhaseOneForm.Basic);
        inPattern = false;
        phaseRoutine = StartCoroutine(phaseThreeBattle ? PhaseThreeLoop() : PhaseOneLoop());
    }

    void ClearActivePatternObjects()
    {
        ClearActiveWarnings();
        foreach (BossProjectile projectile in FindObjectsByType<BossProjectile>())
            Destroy(projectile.gameObject);
        foreach (BossTurret turret in FindObjectsByType<BossTurret>())
            Destroy(turret.gameObject);
    }

    IEnumerator PhaseOneLoop()
    {
        PhaseOneForm[] transformedForms =
        {
            PhaseOneForm.Small,
            PhaseOneForm.Large,
            PhaseOneForm.Special
        };

        int formIndex = 0;
        while (health != null && !health.isDeath && player != null)
        {
            yield return RunForm(PhaseOneForm.Basic, basicFormDuration);
            yield return RunForm(transformedForms[formIndex], transformedFormDuration);
            formIndex = (formIndex + 1) % transformedForms.Length;
        }
    }

    IEnumerator PhaseThreeLoop()
    {
        int attackCyclesSinceDefense = 0;
        while (health != null && !health.isDeath && player != null)
        {
            forceDefenseRequested = false;
            SetPhaseThreeStance(PhaseThreeStance.Attack);
            yield return new WaitForSeconds(.65f);
            yield return PhaseThreeBladeStorm();
            if (!forceDefenseRequested)
            {
                yield return new WaitForSeconds(PhaseThreeDelay());
                yield return PhaseThreeTeleportCombo();
            }
            if (!forceDefenseRequested)
            {
                yield return new WaitForSeconds(PhaseThreeDelay());
                yield return PhaseThreeFocusedBarrage();
            }
            attackCyclesSinceDefense++;

            // Attack form is the default. A normal defense window only appears
            // after three complete attack cycles (nine attack patterns).
            if (!forceDefenseRequested && attackCyclesSinceDefense < 3)
                continue;

            if (!forceDefenseRequested)
            {
                SetPhaseThreeStance(PhaseThreeStance.Defense);
                yield return new WaitForSeconds(.65f);
            }
            else
            {
                // The surprise guard is immediately invulnerable so a held attack
                // continues feeding the charge gauge instead of slipping through.
                yield return new WaitForSeconds(.45f);
            }
            attackCyclesSinceDefense = 0;

            int defensePattern = Random.Range(0, 3);
            if (defensePattern == 0) yield return PhaseThreeMeleeReflect();
            else if (defensePattern == 1) yield return PhaseThreeRangedReflect();
            else yield return PhaseThreeDefenseCharge();
            yield return new WaitForSeconds(.3f);
        }
    }

    float PhaseThreePower() => 1f + phaseThreeAttackBonus / 100f;
    float PhaseThreeSpeed() => 1f + phaseThreeAttackBonus / 170f;
    float PhaseThreeDelay() => Mathf.Max(.16f, patternDelay * .55f / PhaseThreeSpeed());

    void SetPhaseThreeStance(PhaseThreeStance stance)
    {
        phaseThreeStance = stance;
        reflectMode = ReflectMode.None;
        bool defense = stance == PhaseThreeStance.Defense;
        if (!defense)
        {
            phaseThreeAttackBonus = phaseThreeCharge;
            phaseThreeCharge = 0f;
        }
        SetForm(defense ? PhaseOneForm.Basic : PhaseOneForm.Special);
        visual?.SetTint(defense ? new Color(.5f, .85f, 1f) : Color.white);
        attackHitStreak = 0;
        CreateStanceVisual(defense);
    }

    void CreateStanceVisual(bool defense)
    {
        if (stanceVisual != null) Destroy(stanceVisual);
        if (!defense)
        {
            stanceVisual = null;
            return;
        }
        stanceVisual = new GameObject(defense ? "DEFENSE FORM - Blue Shield" : "ATTACK FORM - Red Aura");
        stanceVisual.transform.SetParent(transform, false);

        LineRenderer ring = stanceVisual.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 49;
        ring.startWidth = ring.endWidth = defense ? .09f : .045f;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        Color color = new Color(.1f, .8f, 1f, .9f);
        ring.startColor = ring.endColor = color;
        float radius = 1.15f;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i / 48f * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + .7f, -.1f));
        }

        GameObject labelObject = new GameObject("Form Label");
        labelObject.transform.SetParent(stanceVisual.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "DEFENSE";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 42;
        label.characterSize = .055f;
        label.color = color;
        label.GetComponent<MeshRenderer>().sortingOrder = 20;
    }

    IEnumerator PhaseThreeMeleeReflect()
    {
        inPattern = true;
        reflectMode = ReflectMode.Melee;
        visual?.Play(1, 1.8f);
        yield return new WaitForSeconds(1.8f);
        reflectMode = ReflectMode.None;
        inPattern = false;
    }

    IEnumerator PhaseThreeRangedReflect()
    {
        inPattern = true;
        reflectMode = ReflectMode.Ranged;
        visual?.Play(2, 2.1f);
        yield return new WaitForSeconds(2.1f);
        reflectMode = ReflectMode.None;
        inPattern = false;
    }

    IEnumerator PhaseThreeDefenseCharge()
    {
        inPattern = true;
        visual?.Play(3, 2.4f);
        float endTime = Time.time + 2.4f;
        while (Time.time < endTime)
        {
            if (stanceVisual != null)
                stanceVisual.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 12f) * .07f);
            yield return null;
        }
        if (stanceVisual != null) stanceVisual.transform.localScale = Vector3.one;
        inPattern = false;
    }

    IEnumerator PhaseThreeBladeStorm()
    {
        inPattern = true;
        visual?.Play(3, 1.1f);
        EntityHealth targetHealth = player.GetComponent<EntityHealth>();
        for (int wave = 0; wave < 3; wave++)
        {
            if (forceDefenseRequested) break;
            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 45f + wave * 15f) * Mathf.Deg2Rad;
                Vector2 directionVector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 spawn = (Vector2)transform.position + directionVector * 1.5f;
                BossProjectile.Spawn(spawn, directionVector * projectileSpeed * 1.25f, player.transform, targetHealth,
                    health, projectileDamage * .8f * PhaseThreePower(), 3f, .28f, new Color(.9f, .15f, 1f), new Vector2(.16f, .65f), 2f);
            }
            yield return new WaitForSeconds(.32f / PhaseThreeSpeed());
        }
        inPattern = false;
    }

    IEnumerator PhaseThreeTeleportCombo()
    {
        inPattern = true;
        for (int strike = 0; strike < 4; strike++)
        {
            if (forceDefenseRequested) break;
            float side = strike % 2 == 0 ? -1f : 1f;
            transform.position = (Vector2)player.transform.position + new Vector2(side * 1.35f, .15f);
            FacePlayer();
            yield return TelegraphMarker(transform.position, .38f, new Color(1f, .12f, .65f));
            visual?.Play(2, .28f);
            yield return new WaitForSeconds(.12f / PhaseThreeSpeed());
            DamageRange(FacingRange(specialSlashRange), projectileDamage * 1.25f * PhaseThreePower());
            yield return new WaitForSeconds(.14f / PhaseThreeSpeed());
        }
        inPattern = false;
    }

    IEnumerator PhaseThreeFocusedBarrage()
    {
        inPattern = true;
        FacePlayer();
        yield return TelegraphLine(new Color(1f, .1f, .8f), .45f);
        EntityHealth targetHealth = player.GetComponent<EntityHealth>();
        for (int i = -2; i <= 2; i++)
        {
            if (forceDefenseRequested) break;
            Vector2 baseDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, i * 7f) * baseDirection;
            BossProjectile.Spawn(transform.position, shotDirection * projectileSpeed * 1.8f, player.transform, targetHealth,
                health, projectileDamage * PhaseThreePower(), 2.5f, .35f, new Color(1f, .1f, .55f), new Vector2(.24f, .8f));
            yield return new WaitForSeconds(.1f / PhaseThreeSpeed());
        }
        inPattern = false;
    }

    void CreatePhaseThreeChargeHUD()
    {
        Canvas canvas = bossbar == null ? FindAnyObjectByType<Canvas>() : bossbar.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("Phase 3 Defense Charge Gauge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, .5f);
        panelRect.pivot = new Vector2(1f, .5f);
        panelRect.anchoredPosition = new Vector2(-28f, 0f);
        panelRect.sizeDelta = new Vector2(72f, 300f);
        chargeFrame = panel.GetComponent<Image>();
        chargeFrame.color = new Color(.025f, .04f, .07f, .92f);

        GameObject track = new GameObject("Charge Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        track.transform.SetParent(panel.transform, false);
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(.24f, .12f);
        trackRect.anchorMax = new Vector2(.76f, .88f);
        trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;
        track.GetComponent<Image>().color = new Color(.08f, .11f, .15f, 1f);

        GameObject fill = new GameObject("Defense Charge Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(track.transform, false);
        chargeFill = fill.GetComponent<RectTransform>();
        chargeFill.anchorMin = Vector2.zero;
        chargeFill.anchorMax = new Vector2(1f, 0f);
        chargeFill.offsetMin = chargeFill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(.05f, .8f, 1f, 1f);

        GameObject value = new GameObject("Charge Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        value.transform.SetParent(panel.transform, false);
        RectTransform valueRect = value.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(-.5f, .01f);
        valueRect.anchorMax = new Vector2(1.5f, .11f);
        valueRect.offsetMin = valueRect.offsetMax = Vector2.zero;
        chargeText = value.GetComponent<Text>();
        chargeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        chargeText.fontSize = 18;
        chargeText.fontStyle = FontStyle.Bold;
        chargeText.alignment = TextAnchor.MiddleCenter;
        chargeText.color = Color.white;
        RefreshPhaseThreeChargeHUD();
    }

    void RefreshPhaseThreeChargeHUD()
    {
        float normalized = Mathf.Clamp01(phaseThreeCharge / 100f);
        if (chargeFill != null) chargeFill.anchorMax = new Vector2(1f, normalized);
        if (chargeText != null) chargeText.text = Mathf.RoundToInt(phaseThreeCharge) + "%";
        if (chargeFrame != null)
            chargeFrame.color = IsPhaseThreeDefense
                ? new Color(.03f, .13f, .2f, .96f)
                : new Color(.1f, .035f, .04f, .9f);
    }

    IEnumerator RunForm(PhaseOneForm form, float duration)
    {
        if (enteredFirstForm)
            yield return StartCoroutine(ChangeForm(form));
        else
            enteredFirstForm = true;

        SetForm(form);
        float endTime = Time.time + duration;
        int patternIndex = 0;

        while (Time.time < endTime && health != null && !health.isDeath)
        {
            if (!inPattern)
            {
                yield return StartCoroutine(RunPattern(form, patternIndex));
                int patternCount = form == PhaseOneForm.Basic ? 6 : 3;
                patternIndex = (patternIndex + 1) % patternCount;
                yield return new WaitForSeconds(patternDelay);
            }
            else
            {
                yield return null;
            }
        }

        inPattern = false;
        SetVelocity(Vector2.zero);
    }

    IEnumerator ChangeForm(PhaseOneForm nextForm)
    {
        inPattern = true;
        SetVelocity(Vector2.zero);
        if (visual != null)
            yield return visual.PlayTransformation(nextForm, formChangeWindup);
        else
            yield return new WaitForSeconds(formChangeWindup);
        inPattern = false;
    }

    IEnumerator RunPattern(PhaseOneForm form, int index)
    {
        if (form == PhaseOneForm.Basic)
        {
            switch (index)
            {
                case 0: yield return FireFiveShotBurst(); break;
                case 1: yield return InstallTurret(); break;
                case 2: yield return RainProjectiles(); break;
                case 3: yield return MeleeAndRetreat(); break;
                case 4: yield return DashAttackPattern(); break;
                default: yield return JumpAttackPattern(); break;
            }
        }
        else if (form == PhaseOneForm.Small)
        {
            if (index == 0) yield return SmallOrbitBarrage();
            else if (index == 1) yield return SmallDashAttack();
            else yield return SmallSustainedFire();
        }
        else if (form == PhaseOneForm.Large)
        {
            switch (index)
            {
                case 0: yield return LargeLaser(); break;
                case 1: yield return LargeCannon(); break;
                default: yield return LargeSwing(); break;
            }
        }
        else
        {
            if (index == 0) yield return TeleportSlash();
            else if (index == 1) yield return DirectionWarningAttack();
            else yield return SummonSwords();
        }
    }

    void SetForm(PhaseOneForm form)
    {
        currentForm = form;
        if (visual != null)
            visual.SetForm(form);

        float ratio = basicPlayerHeightRatio;
        if (form == PhaseOneForm.Small) ratio = smallPlayerHeightRatio;
        else if (form == PhaseOneForm.Large) ratio = largePlayerHeightRatio;
        else if (form == PhaseOneForm.Special) ratio = specialPlayerHeightRatio;
        if (phaseThreeBattle) ratio = phaseThreePlayerHeightRatio;

        float scale = GetScaleForPlayerHeight(ratio);
        float facing = Mathf.Sign(transform.localScale.x == 0f ? 1f : transform.localScale.x);
        transform.localScale = new Vector3(scale * facing, scale, transform.localScale.z);
    }

    float GetScaleForPlayerHeight(float playerHeightRatio)
    {
        Sprite bossSprite = BossArt.GetFormSprites(currentForm).Length > 0
            ? BossArt.GetFormSprites(currentForm)[0]
            : null;

        if (cachedPlayerHeight <= 0f || bossSprite == null)
            return baseScaleY * playerHeightRatio;

        float bossHeight = bossSprite.bounds.size.y;
        return bossHeight <= 0f ? baseScaleY : cachedPlayerHeight * playerHeightRatio / bossHeight;
    }

    void CachePlayerHeight()
    {
        if (cachedPlayerHeight > 0f || player == null) return;
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        if (playerRenderer != null && playerRenderer.sprite != null)
            cachedPlayerHeight = playerRenderer.sprite.bounds.size.y * Mathf.Abs(player.transform.localScale.y);
    }

    void BasicFallbackMovement()
    {
        float dist = Vector2.Distance(player.transform.position, transform.position);
        if (retreatTimer > 0f)
        {
            MoveScaled(Vector2.right * (player.transform.position.x > transform.position.x ? -1 : 1), 1f);
        }
        else if (dist > attackDist)
        {
            MoveScaled(player.transform.position.x > transform.position.x ? Vector2.right : Vector2.left, 1f);
        }
    }

    IEnumerator FireFiveShotBurst()
    {
        inPattern = true;
        for (int i = 0; i < 5; i++)
        {
            visual?.Play(1, 0.14f);
            FireAtPlayer(projectileSpeed, projectileDamage, Color.red, 0.22f);
            yield return new WaitForSeconds(0.16f);
        }
        inPattern = false;
    }

    IEnumerator InstallTurret()
    {
        inPattern = true;
        visual?.Play(1, 0.4f);
        float side = player.transform.position.x > transform.position.x ? -1f : 1f;
        BossTurret.Spawn((Vector2)player.transform.position + new Vector2(side * 3f, 1f), player.transform,
            player.GetComponent<EntityHealth>(), health, projectileDamage * 0.7f, turretDuration, 0.85f, projectileSpeed);
        yield return new WaitForSeconds(0.4f);
        inPattern = false;
    }

    IEnumerator RainProjectiles()
    {
        inPattern = true;
        visual?.Play(3, 1.4f);
        EntityHealth targetHealth = player.GetComponent<EntityHealth>();
        for (int i = 0; i < 7; i++)
        {
            float x = player.transform.position.x + Random.Range(-3f, 3f);
            BossProjectile.Spawn(new Vector2(x, player.transform.position.y + rainHeight), Vector2.down * projectileSpeed,
                player.transform, targetHealth, health, projectileDamage, 3f, 0.3f, Color.yellow, new Vector2(0.18f, 0.45f));
            yield return new WaitForSeconds(0.18f);
        }
        inPattern = false;
    }

    IEnumerator MeleeAndRetreat()
    {
        inPattern = true;
        float timer = 0f;
        while (timer < 1.5f && Vector2.Distance(player.transform.position, transform.position) > attackDist)
        {
            MoveScaled(player.transform.position.x > transform.position.x ? Vector2.right : Vector2.left, 1f);
            timer += Time.deltaTime;
            yield return null;
        }
        FacePlayer();
        visual?.Play(2, 0.5f);
        Attack(0.5f, FacingRange(defaultAttack), transform.position);
        retreatTimer = retreatTime;
        yield return new WaitForSeconds(retreatTime);
        inPattern = false;
    }

    IEnumerator DashAttackPattern()
    {
        inPattern = true;
        FacePlayer();
        visual?.Play(1, dashDuration + 0.3f);
        yield return new WaitForSeconds(0.25f);
        SetVelocity(Vector2.right * direction * dashPower);
        float timer = 0f;
        while (timer < dashDuration && Mathf.Abs(player.transform.position.x - transform.position.x) > attackDist)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        SetVelocity(Vector2.zero);
        Attack(0f, FacingRange(ValidRange(dashAttack, new Vector2(2.5f, 1.5f))), transform.position);
        dashCool = dashCoolTime;
        inPattern = false;
    }

    IEnumerator JumpAttackPattern()
    {
        inPattern = true;
        visual?.Play(3, 1f);
        SetVelocity(Vector2.up * jumpPower);
        yield return new WaitForSeconds(0.2f);
        float safety = 2.5f;
        while (!OnGround() && safety > 0f)
        {
            float horizontal = player.transform.position.x > transform.position.x ? 1f : -1f;
            MoveScaled(Vector2.right * horizontal, 0.7f);
            if (rigid.linearVelocity.y < 0f)
                SetVelocity(new Vector2(rigid.linearVelocity.x, -fallSpeed));
            safety -= Time.deltaTime;
            yield return null;
        }
        Attack(0f, ValidRange(jumpAttack, new Vector2(3.5f, 1.8f)), transform.position);
        jumpCool = jumpCoolTime;
        inPattern = false;
    }

    IEnumerator SmallOrbitBarrage()
    {
        inPattern = true;
        float timer = 2.2f;
        float shotTimer = 0f;
        while (timer > 0f)
        {
            visual?.Play(1, 0.15f);
            float side = Mathf.Sign(transform.position.x - player.transform.position.x);
            if (side == 0f) side = 1f;
            Vector2 desired = (Vector2)player.transform.position + new Vector2(side * 3.5f, Mathf.Sin(Time.time * 5f));
            transform.position = Vector2.MoveTowards(transform.position, desired, 5.5f * Time.deltaTime);
            shotTimer -= Time.deltaTime;
            if (shotTimer <= 0f)
            {
                visual?.Play(2, 0.22f);
                shotTimer = 0.3f;
                FireAtPlayer(projectileSpeed * 1.4f, projectileDamage * 0.65f, Color.cyan, 0.16f);
            }
            timer -= Time.deltaTime;
            yield return null;
        }
        inPattern = false;
    }

    IEnumerator SmallDashAttack()
    {
        inPattern = true;
        FacePlayer();
        visual?.Play(1, 0.45f);
        SetVelocity(Vector2.right * direction * dashPower * 1.45f);
        yield return new WaitForSeconds(0.3f);
        SetVelocity(Vector2.zero);
        Attack(0f, FacingRange(ValidRange(dashAttack, new Vector2(2f, 1.2f))), transform.position);
        inPattern = false;
    }

    IEnumerator SmallSustainedFire()
    {
        inPattern = true;
        for (int i = 0; i < 10; i++)
        {
            visual?.Play(2, 0.18f);
            float away = player.transform.position.x > transform.position.x ? -1f : 1f;
            MoveScaled(Vector2.right * away, 1.5f);
            FireAtPlayer(projectileSpeed * 1.3f, projectileDamage * 0.55f, Color.cyan, 0.14f);
            yield return new WaitForSeconds(0.2f);
        }
        inPattern = false;
    }

    IEnumerator LargeLaser()
    {
        inPattern = true;
        FacePlayer();
        visual?.Play(2, 1.1f);
        yield return TelegraphLine(Color.red, 0.65f);
        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
        BossProjectile.Spawn(transform.position, directionToPlayer * 13f, player.transform, player.GetComponent<EntityHealth>(),
            health, largeDamage, 1.5f, 0.8f, Color.red, new Vector2(2.5f, 0.55f));
        yield return new WaitForSeconds(0.5f);
        inPattern = false;
    }

    IEnumerator LargeCannon()
    {
        inPattern = true;
        visual?.Play(1, 1.2f);
        Vector2 destination = player.transform.position;
        yield return TelegraphMarker(destination, 0.7f, new Color(1f, 0.4f, 0f));
        Vector2 directionToPoint = (destination - (Vector2)transform.position).normalized;
        BossProjectile projectile = BossProjectile.Spawn(transform.position, directionToPoint * 8f, player.transform,
            player.GetComponent<EntityHealth>(), health, largeDamage, 3f, 0.35f, new Color(1f, 0.35f, 0f), Vector2.one * 0.55f);
        projectile.ExplodeAt(destination, 2f);
        yield return new WaitForSeconds(0.7f);
        inPattern = false;
    }

    IEnumerator LargeSwing()
    {
        inPattern = true;
        FacePlayer();
        visual?.Play(3, 1f);
        yield return new WaitForSeconds(0.55f);
        DamageRange(FacingRange(largeSwingRange), largeDamage);
        yield return new WaitForSeconds(0.4f);
        inPattern = false;
    }

    IEnumerator TeleportSlash()
    {
        inPattern = true;
        visual?.Play(1, 0.25f);
        float behind = player.transform.localScale.x >= 0f ? -1f : 1f;
        transform.position = (Vector2)player.transform.position + new Vector2(behind * 1.5f, 0f);
        FacePlayer();
        yield return new WaitForSeconds(0.25f);
        visual?.Play(2, 0.45f);
        DamageRange(FacingRange(specialSlashRange), projectileDamage * 1.5f);
        yield return new WaitForSeconds(0.25f);
        inPattern = false;
    }

    IEnumerator DirectionWarningAttack()
    {
        inPattern = true;
        int directionIndex = Random.Range(0, 8);
        float angle = directionIndex * 45f * Mathf.Deg2Rad;
        Vector2 directionVector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 spawnPoint = (Vector2)player.transform.position + directionVector * 6f;
        yield return TelegraphMarker(spawnPoint, 0.8f, Color.magenta);
        BossProjectile.Spawn(spawnPoint, -directionVector * 12f, player.transform, player.GetComponent<EntityHealth>(),
            health, projectileDamage * 1.4f, 2f, 0.45f, Color.magenta, new Vector2(0.25f, 1.2f));
        yield return new WaitForSeconds(0.5f);
        inPattern = false;
    }

    IEnumerator SummonSwords()
    {
        inPattern = true;
        visual?.Play(3, 1.2f);
        EntityHealth targetHealth = player.GetComponent<EntityHealth>();
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f;
            Vector2 spawn = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2f;
            Vector2 velocity = ((Vector2)player.transform.position - spawn).normalized * projectileSpeed * 1.5f;
            BossProjectile.Spawn(spawn, velocity, player.transform, targetHealth, health, projectileDamage,
                3f, 0.3f, new Color(0.75f, 0.2f, 1f), new Vector2(0.18f, 0.7f), 2f);
            yield return new WaitForSeconds(0.12f);
        }
        inPattern = false;
    }

    IEnumerator TelegraphLine(Color color, float duration)
    {
        GameObject warning = CreateWarning("Laser Warning", transform.position, new Vector2(12f, 0.08f), color);
        warning.transform.right = player.transform.position - transform.position;
        yield return new WaitForSeconds(duration);
        RemoveWarning(warning);
    }

    IEnumerator TelegraphMarker(Vector2 position, float duration, Color color)
    {
        GameObject warning = CreateWarning("Attack Warning", position, Vector2.one * 1.2f, color);
        yield return new WaitForSeconds(duration);
        RemoveWarning(warning);
    }

    GameObject CreateWarning(string objectName, Vector2 position, Vector2 scale, Color color)
    {
        GameObject warning = new GameObject(objectName);
        warning.transform.position = position;
        warning.transform.localScale = scale;
        SpriteRenderer renderer = warning.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f), 1f);
        renderer.color = new Color(color.r, color.g, color.b, 0.45f);
        renderer.sortingOrder = 5;
        activeWarnings.Add(warning);
        return warning;
    }

    void RemoveWarning(GameObject warning)
    {
        activeWarnings.Remove(warning);
        if (warning != null)
            Destroy(warning);
    }

    void ClearActiveWarnings()
    {
        for (int i = activeWarnings.Count - 1; i >= 0; i--)
            if (activeWarnings[i] != null)
                Destroy(activeWarnings[i]);
        activeWarnings.Clear();
    }

    void FireAtPlayer(float speed, float damage, Color color, float size)
    {
        if (player == null) return;
        Vector2 velocity = ((Vector2)player.transform.position - (Vector2)transform.position).normalized * speed;
        BossProjectile.Spawn(transform.position, velocity, player.transform, player.GetComponent<EntityHealth>(), health,
            damage, 4f, Mathf.Max(0.25f, size * 1.5f), color, Vector2.one * size);
    }

    void DamageRange(AttackRange range, float damage)
    {
        Vector2 center = (Vector2)transform.position + range.offset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, range.size, 0f);
        foreach (Collider2D hit in hits)
        {
            EntityHealth targetHealth = hit.GetComponentInParent<EntityHealth>();
            if (targetHealth != null && targetHealth != health && targetHealth == player.GetComponent<EntityHealth>())
            {
                targetHealth.GetDamage(damage, health);
                break;
            }
        }
    }

    void DamageAtPoint(Vector2 center, Vector2 size, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (Collider2D hit in hits)
        {
            EntityHealth targetHealth = hit.GetComponentInParent<EntityHealth>();
            if (targetHealth != null && targetHealth != health && targetHealth == player.GetComponent<EntityHealth>())
            {
                targetHealth.GetDamage(damage, health);
                break;
            }
        }
    }

    void FacePlayer()
    {
        direction = player.transform.position.x >= transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction,
            transform.localScale.y, transform.localScale.z);
    }

    void MoveScaled(Vector2 axis, float multiplier)
    {
        visual?.SetMoving();
        float moveSpeed = stat.GetResultValue("moveSpeed");
        transform.Translate(axis.normalized * moveSpeed * multiplier * Time.deltaTime);
    }

    AttackRange FacingRange(AttackRange range)
    {
        range.offset.x = Mathf.Abs(range.offset.x) * direction;
        return range;
    }

    AttackRange ValidRange(AttackRange range, Vector2 fallbackSize)
    {
        if (range.size.x <= 0f || range.size.y <= 0f)
            range.size = fallbackSize;
        return range;
    }

    protected override void DrawGizmos()
    {
        Draw(defaultAttack);
        Draw(ValidRange(dashAttack, new Vector2(2.5f, 1.5f)));
        Draw(ValidRange(jumpAttack, new Vector2(3.5f, 1.8f)));
        Draw(largeSwingRange);
        Draw(specialSlashRange);
    }

    void OnDestroy()
    {
        ClearActiveWarnings();
        if (phaseRoutine != null)
            StopCoroutine(phaseRoutine);
    }
}
