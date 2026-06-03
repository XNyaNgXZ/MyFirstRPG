using UnityEngine;
using System.Collections;

public class HitSpark : MonoBehaviour
{
    public static void Spawn(Vector3 position, bool isWeapon)
    {
        int count = isWeapon ? 6 : 3;
        Color color = isWeapon
            ? new Color(1f, 0.85f, 0.1f)
            : new Color(1f, 0.4f, 0.1f);

        for (int i = 0; i < count; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * Random.Range(0.03f, 0.06f);

            Renderer rend = spark.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            rend.material.color = color;

            Destroy(spark.GetComponent<Collider>());

            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y);
            float speed = Random.Range(1.5f, 3.5f);

            spark.AddComponent<HitSpark>().StartCoroutine(
                MoveSpark(spark.transform, rend, dir * speed));
        }
    }

    static IEnumerator MoveSpark(Transform t, Renderer rend, Vector3 velocity)
    {
        float elapsed = 0f;
        float lifetime = 0.25f;
        Color baseColor = rend.material.color;

        while (elapsed < lifetime && t != null)
        {
            elapsed += Time.deltaTime;
            velocity += Vector3.down * 8f * Time.deltaTime; // гравитация вручную
            t.position += velocity * Time.deltaTime;
            t.localScale *= 1f - Time.deltaTime * 4f;

            Color c = baseColor;
            c.a = 1f - (elapsed / lifetime);
            rend.material.color = c;

            yield return null;
        }

        if (t != null) Destroy(t.gameObject);
    }
}