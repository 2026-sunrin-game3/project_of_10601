using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityHealth : MonoBehaviour
{
    public float health, maxHealth;

    public bool isDeath;
    PlayerStat stat;   

    public class Context
    {
        public float damage;
        public EntityHealth attacker;
        public bool canceled;
        public bool isCritical;
        public float hitStunDuration;
    }

    List<Action<Context>> onDamageEv = new();
    List<Action<Context>> onGiveDamageEv = new();
    List<Action<Context>> onDeathEv = new();
    float stunnedUntil;
    public bool IsStunned => Time.time < stunnedUntil;

    void Awake()
    {
        stat = GetComponent<PlayerStat>();
    }

    void Start()
    {
        ResetHealth();
    }
    
    void OnDeath(EntityHealth.Context ctx)
    {
       Destroy(gameObject);
    }

    public void ResetHealth()
    {
        health = maxHealth;
    }

    public void Ondamage(Action<Context> action)
    {
        onDamageEv.Add(action);
    }

    public void OnGiveDamage(Action<Context> action)
    {
        onGiveDamageEv.Add(action);
    }

    public void OnDeath(Action<Context> action)
    {
        onDeathEv.Add(action);
    }

    public void GetDamage(float damage, EntityHealth attacker = null)
    {
        if (isDeath)
            return;

        Context ctx = new Context();
        ctx.damage = damage;
        ctx.attacker = attacker;

        float critPer = 0f;
        float critMultiplier = 0f;
        float increaseDamage = 0f;
        if (attacker != null)
        {
            critPer = attacker.stat.GetResultValue("critPer");
            critMultiplier = attacker.stat.GetResultValue("critMul");
            increaseDamage = attacker.stat.GetResultValue("increaseDamage");
            foreach (var c in attacker.onGiveDamageEv)
                c.Invoke(ctx);
        }

        float dmg = ctx.damage * (1f + increaseDamage / 100f);
        dmg *= 1f + stat.GetResultValue("hurtDamage") / 100f;
        ctx.isCritical = attacker != null && UnityEngine.Random.Range(0f, 100f) < critPer;
        if (ctx.isCritical)
        {
            dmg *= 1f + critMultiplier / 100f;
            ctx.hitStunDuration = 0.22f;
        }
        dmg = Mathf.Max(0f, dmg - stat.GetResultValue("defense"));
        ctx.damage = dmg;

        foreach (var c in onDamageEv)
            c.Invoke(ctx);

        if (ctx.canceled) return;

        if (ctx.isCritical)
        {
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + ctx.hitStunDuration);
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        
        health -= dmg;

        if (health <= 0)
        {
            // Keep the health state consistent for UI and death listeners.
            health = 0f;
            isDeath = true;

            foreach (var c in onDeathEv)
            {
                c.Invoke(ctx);
            }
        }
    }
}
