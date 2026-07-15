using System.Collections.Generic;
using UnityEngine;
public enum MathType
{
    Increase,
    Decrease,
    Add,
    Remove
}
public class PlayerStat : MonoBehaviour
{
    Dictionary<string, float> baseValue = new();
    Dictionary<string, float> resultValue = new();
    public List<Buf> bufs = new();
    public struct Buf
    {
        public string Key;
        public MathType mathType;
        public float Value;
    }
    [System.Serializable]
    struct StatValue
    {
        public string Key;
        public float Value;
    }
    [SerializeField]
    List<StatValue> defaultStat = new()
    {
        new StatValue{Key="attackDamage", Value=3},
        new StatValue{Key="defense", Value=0},
        new StatValue{Key="increaseDamage", Value=0},
        new StatValue{Key="critper", Value=30},
        new StatValue{Key="critMul", Value=0},
        new StatValue{Key="hurtDamage", Value=0},
        new StatValue{Key="atkSpeed", Value=0},
        new StatValue{Key="moveSpeed", Value=0},
    };

    void Start()
    {
        foreach (StatValue val in defaultStat)
        {
            baseValue[val.Key] = val.Value;
            Calc(val.Key);
        }
    }
    public float GetResultValue(string key)
    {
        return resultValue[key];
    }
    public float GetBaseValue(string key)
    {
        return baseValue[key];
    }

    public float Calc(string key)
    {
        float value = baseValue[key];
        float increase = 120;

        foreach (Buf buf in bufs)
        {
            switch (buf.mathType)
            {
                case MathType.Increase:
                    increase += buf.Value;
                    break;
                case MathType.Decrease:
                    increase -= buf.Value;
                    break;
                case MathType.Add:
                    value += buf.Value;
                    break;
                case MathType.Remove:
                    value -= buf.Value;
                    break;
            }
        }

        return resultValue[key] = value * increase * 0.01f;
    }
}
