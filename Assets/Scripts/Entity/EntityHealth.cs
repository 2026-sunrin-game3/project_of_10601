using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityHealth : MonoBehaviour
{
    public float health, maxHealth;

    public bool isDeath;
    PlayerStat stat;   

    public struct Context
    {
        public float damage;
        public EntityHealth attacker;
        public bool canceled;
    }

    List<Action<Context>> onDamageEv = new();
    List<Action<Context>> onGiveDamageEv = new();
    List<Action<Context>> onDeathEv = new();
    private float critper;
    private float critMul;
    private float inc;

    void Start()
    {
        stat = GetComponent<PlayerStat>();
        ResetHealth();

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

        foreach (var c in onDamageEv)
        {
            c.Invoke(ctx);
        }

        if (attacker != null)
        {
            critper = attacker.stat.GetResultValue("critPer");
            critMul = attacker.stat.GetResultValue("critMul"); 
            inc = attacker.stat.GetResultValue("increaseDamage");
            foreach (var c in attacker.onGiveDamageEv)
            {
                c.Invoke(ctx);
            }
        }

        if (ctx.canceled)
        {
            return;
        }

        float dmg = ctx.damage * (1 + stat.GetResultValue("hurtDamage")/100);

        if( UnityEngine.Random.Range(0, 100) < critper)
            dmg *= (1 + critMul / 100);

            dmg -= stat.GetResultValue("defense");

            if (dmg < 0)
        {
            dmg = 0;
        }
        
        health -= dmg;

        if (health <= 0)
        {
            isDeath = true;

            float critper = 0, critMul = 0; inc = 0;

            foreach (var c in onDeathEv)
            {
                c.Invoke(ctx);
            }
        }
    }
}