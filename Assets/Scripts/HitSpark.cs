using UnityEngine;
using System.Collections;

public class HitSpark : MonoBehaviour
{
    public static void Spawn(Vector3 position, bool isWeapon, bool isCharged = false)
    {
        int count = isCharged ? 18 : (isWeapon ? 6 : 3);
        float minSize = isCharged ? 0.06f : 0.02f;
        float maxSize = isCharged ? 0.12f : 0.06f;
        float minSpeed = isCharged ? 4f : 1.5f;
        float maxSpeed = isCharged ? 8f : 3.5f;
        float lifetime = isCharged ? 0.4f : 0.25f;

        Color color = isCharged
            ? new Color(1f, 1f, 0.4f)
            : isWeapon
                ? new Color(1f, 0.85f, 0.1f)
                : new Color(1f, 0.4f, 0.1f);

        for (int i = 0; i < count; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * Random.Range(minSize, maxSize);

            Renderer rend = spark.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            rend.material.color = color;
            Destroy(spark.GetComponent<Collider>());

            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y);
            float speed = Random.Range(minSpeed, maxSpeed);

            spark.AddComponent<HitSpark>().StartCoroutine(
                MoveSpark(spark.transform, rend, dir * speed, lifetime));
        }
    }

    static IEnumerator MoveSpark(Transform t, Renderer rend, Vector3 velocity, float lifetime)
    {
        float elapsed = 0f;
        Color baseColor = rend.material.color;

        while (elapsed < lifetime && t != null)
        {
            elapsed += Time.deltaTime;
            velocity += Vector3.down * 10f * Time.deltaTime;
            t.position += velocity * Time.deltaTime;
            t.localScale *= 1f - Time.deltaTime * 5f;

            Color c = baseColor;
            c.a = 1f - (elapsed / lifetime);
            rend.material.color = c;
            yield return null;
        }

        if (t != null) Destroy(t.gameObject);
    }
}