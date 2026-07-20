using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour
{

    [SerializeField] Text text;
    [SerializeField] float time, floatingScale;

    public static DamageIndicator Instance = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        transform.Translate(Vector2.up * floatingScale * Time.deltaTime);

        if (time > 0.65f)
        {
            Destroy(gameObject);
        }   
    }

    public void indicateDamage(float damage, Vector2 pos, Color color)
    {
        DamageIndicator indicator = Instantiate(this, pos, Quaternion.identity);
        indicator.text.text = Mathf.Round(damage).ToString();
        indicator.text.color = color;
    }
}
