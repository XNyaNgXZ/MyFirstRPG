using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageNumber : MonoBehaviour
{
    private TextMeshPro tmp;
    private Camera cam;

    public static void Spawn(Vector3 worldPos, int damage)
    {
        GameObject go = new GameObject("DamageNumber");

        go.transform.position = worldPos + Vector3.up * 1.8f
                              + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
        go.transform.localScale = Vector3.one * 0.5f;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = damage.ToString();
        tmp.fontSize = 6f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = damage >= 10
            ? new Color(1f, 0.3f, 0.1f, 1f)   // красно-оранжевый — большой урон
            : new Color(1f, 0.95f, 0.1f, 1f);  // жёлтый — малый урон

        // Чтобы текст был поверх всего
        tmp.sortingOrder = 10;

        FloatingDamageNumber fdn = go.AddComponent<FloatingDamageNumber>();
        fdn.tmp = tmp;
        fdn.cam = Camera.main;
    }

    void Update()
    {
        if (tmp == null) { Destroy(gameObject); return; }

        // Поднимается вверх
        transform.position += Vector3.up * 1.2f * Time.deltaTime;

        // Всегда смотрит на камеру
        if (cam != null)
            transform.LookAt(transform.position + cam.transform.forward);

        // Затухает со временем
        Color c = tmp.color;
        c.a -= Time.deltaTime * 1.2f;
        tmp.color = c;

        if (c.a <= 0f)
            Destroy(gameObject);
    }
}