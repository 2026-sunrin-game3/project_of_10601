using UnityEngine;
using System.Collections.Generic;


public enum MathType
{
    Increase,
    Decrease,
    Add,
    Remove
}



public class PlayerStat : MonoBehaviour
{

    Dictionary<string, float> basestate = new();
    Dictionary<string, float> resultValue = new();
    public List<Buf> bufList = new();

    public struct Buf
    {
        public string key;
        public float value;
        public MathType mathType;
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
        new StatValue() { Key = "attackDamage", Value = 3},
        new StatValue() { Key = "defense", Value = 0},
        new StatValue() { Key = "increaseDamage", Value = 0},
        new StatValue() { Key = "critper", Value = 3},
        new StatValue() { Key = "critMul", Value = 0},
        new StatValue() { Key = "hurtDamage", Value = 0 },
        new StatValue() { Key = "atkSpeed", Value = 0},
        new StatValue() { Key = "moveSpeed", Value = 0}
    };
   

    //공격력 방어력, 가하는 피해 증가, 치명타 확률 / 피해 증가, 공속, 이속
    void Start()
    {
        foreach (StatValue val in defaultStat)
        {
            basestate[val.Key] = val.Value;
            Calc(val.Key);
        }
        
    }

    public float GetResultValue(string key)
    {
        return resultValue[key];
    }

    // Update is called once per frame
    public float Calc(string key)
    {
        float value = basestate[key];
        float increase = 100;

        foreach (Buf buf in bufList)
        {
            switch (buf.mathType)
            {
                case MathType.Increase:
                    increase += buf.value;
                    break;
                case MathType.Decrease:
                    increase -= buf.value;
                    break;
                case MathType.Add:
                    value += buf.value;
                    break;
                case MathType.Remove:
                    value -= buf.value;
                    break;
            }
        }

        return resultValue[key] = value * increase * 0.01f;

        
    }

}
