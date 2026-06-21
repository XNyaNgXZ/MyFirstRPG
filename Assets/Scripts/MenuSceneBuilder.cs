using UnityEngine;

public class MenuSceneBuilder : MonoBehaviour
{
    [Header("Материал камней")]
    public Material stoneMaterial;

    [Header("Факелы")]
    public int torchCount = 4;

    void Start()
    {
        BuildScene();
        SetupFog();
    }

    void BuildScene()
    {
        // ── Пол ──────────────────────────────────────────────────────
        // Пол без отблеска
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(20f, 0.3f, 20f);
        var floorRend = floor.GetComponent<Renderer>();
        var floorMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        floorMat.color = new Color(0.1f, 0.09f, 0.1f);
        floorRend.material = floorMat;
        floorRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        floorRend.receiveShadows = false;
        Destroy(floor.GetComponent<Collider>());

        // ── Колонны по кругу ─────────────────────────────────────────
        int colCount = 8;
        float colRadius = 5f;
        for (int i = 0; i < colCount; i++)
        {
            float angle = i * (360f / colCount) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                Mathf.Sin(angle) * colRadius,
                1.5f,
                Mathf.Cos(angle) * colRadius
            );
            float height = Random.Range(2.5f, 4.5f);
            float width = Random.Range(0.4f, 0.7f);
            MakeStone($"Column_{i}", pos, new Vector3(width, height, width),
                new Color(0.08f, 0.06f, 0.08f));

            // Шапка колонны
            MakeStone($"ColumnTop_{i}", pos + Vector3.up * (height * 0.5f + 0.15f),
                new Vector3(width + 0.15f, 0.25f, width + 0.15f),
                new Color(0.1f, 0.08f, 0.1f));
        }

        // ── Случайные камни разного размера ──────────────────────────
        Vector3[] stonePositions = {
            // Дальние от центра
            new Vector3( 3.5f, 0.1f,  3.5f),
            new Vector3(-3.5f, 0.1f,  3f),
            new Vector3( 4f,   0.1f, -2.5f),
            new Vector3(-3f,   0.1f, -4f),
            new Vector3(-4.5f, 0.1f,  4f),
            new Vector3( 3f,   0.1f,  1.5f),
            new Vector3(-1.5f, 0.1f,  4.5f),
            new Vector3( 2.5f, 0.1f, -3.5f),
            // Мелкие камешки
            new Vector3( 1.5f, 0.05f, 2.5f),
            new Vector3(-2f,   0.05f, 1.5f),
            new Vector3( 2f,   0.05f,-1.5f),
            new Vector3(-1.5f, 0.05f,-2.5f),
            new Vector3( 0.8f, 0.05f, 3f),
            new Vector3(-2.5f, 0.05f,-1f),
            new Vector3( 3f,   0.05f,-1f),
            new Vector3(-0.8f, 0.05f,-3f),
        };

        for (int ri = 0; ri < stonePositions.Length; ri++)
        {
            var sp = stonePositions[ri];
            bool small = ri >= 8; // последние 8 — мелкие
            float s = small ? Random.Range(0.08f, 0.18f) : Random.Range(0.15f, 0.4f);
            float h = small ? Random.Range(0.06f, 0.12f) : Random.Range(0.15f, 0.5f);
            MakeStone("Rock", sp,
                new Vector3(s * Random.Range(0.8f, 1.4f), h, s * Random.Range(0.8f, 1.4f)),
                new Color(
                    Random.Range(0.12f, 0.20f),
                    Random.Range(0.10f, 0.16f),
                    Random.Range(0.11f, 0.18f)
                ));
        }

        // ── Стены ─────────────────────────────────────────────────────
        MakeStone("WallN", new Vector3(0, 2f, 9f), new Vector3(18f, 4f, 0.5f), new Color(0.12f, 0.10f, 0.13f));
        MakeStone("WallS", new Vector3(0, 2f, -9f), new Vector3(18f, 4f, 0.5f), new Color(0.12f, 0.10f, 0.13f));
        MakeStone("WallE", new Vector3(9f, 2f, 0), new Vector3(0.5f, 4f, 18f), new Color(0.12f, 0.10f, 0.13f));
        MakeStone("WallW", new Vector3(-9f, 2f, 0), new Vector3(0.5f, 4f, 18f), new Color(0.12f, 0.10f, 0.13f));

        // ── Только костёр в центре ───────────────────────────────────
        SpawnCampfire(new Vector3(0f, 0.15f, 0f));


    }

    void MakeStone(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        // Случайный небольшой поворот для органичности
        go.transform.rotation = Quaternion.Euler(
            Random.Range(-3f, 3f), Random.Range(0f, 360f) * (name.StartsWith("Rock") ? 1f : 0f),
            Random.Range(-3f, 3f));

        var rend = go.GetComponent<Renderer>();
        if (stoneMaterial != null)
            rend.material = new Material(stoneMaterial);
        rend.material.color = color;

        // Убираем тени чтобы не было артефактов
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        // Убираем коллайдер — не нужен в меню
        Destroy(go.GetComponent<Collider>());
    }

    ParticleSystem SpawnFire(Transform parent, Vector3 localPos, float size = 1f)
    {
        var fireGO = new GameObject("Fire");
        fireGO.transform.SetParent(parent);
        fireGO.transform.localPosition = localPos;
        var ps = fireGO.AddComponent<ParticleSystem>();

        // ✅ URP материал для партиклей
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        rend.material.color = new Color(1f, 0.5f, 0.1f, 0.8f);

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.4f, 0f, 0.8f), new Color(1f, 0.7f, 0.1f, 0.4f));
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f * size, 0.1f * size);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f * size, 0.3f * size);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.gravityModifier = -0.15f; // ✅ медленно вверх
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 10f * size; // ✅ меньше частиц

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;  // ✅ узкий конус — вверх а не в стороны
        shape.radius = 0.02f * size;

        // Движение вверх через gravityModifier — без velocity override

        return ps;
    }

    void SpawnTorch(Vector3 pos)
    {
        var torch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        torch.name = "Torch";
        torch.transform.position = pos;
        torch.transform.localScale = new Vector3(0.06f, 0.25f, 0.06f);
        var tr = torch.GetComponent<Renderer>();
        tr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        tr.material.color = new Color(0.3f, 0.2f, 0.1f);
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(torch.GetComponent<Collider>());

        var lightGO = new GameObject("TorchLight");
        lightGO.transform.position = pos + Vector3.up * 0.35f;
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.1f);
        light.intensity = 1.2f;
        light.range = 4f;
        lightGO.AddComponent<TorchFlicker>();

        SpawnFire(torch.transform, Vector3.up * 1.1f, 1f);
    }

    void SpawnCampfire(Vector3 pos)
    {
        // Камни вокруг костра
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Vector3 rPos = pos + new Vector3(Mathf.Sin(a) * 0.25f, 0.05f, Mathf.Cos(a) * 0.25f);
            MakeStone("FireRock", rPos, new Vector3(0.12f, 0.1f, 0.1f), new Color(0.2f, 0.17f, 0.18f));
        }

        // Брёвна
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.transform.position = pos + new Vector3(Mathf.Sin(a) * 0.12f, 0.06f, Mathf.Cos(a) * 0.12f);
            log.transform.localScale = new Vector3(0.06f, 0.2f, 0.06f);
            log.transform.rotation = Quaternion.Euler(80f, a * Mathf.Rad2Deg, 0f);
            var lr = log.GetComponent<Renderer>();
            lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lr.material.color = new Color(0.25f, 0.15f, 0.08f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(log.GetComponent<Collider>());
        }

        // Огонь костра
        var fireParent = new GameObject("CampfireFire");
        fireParent.transform.position = pos + Vector3.up * 0.15f;
        SpawnFire(fireParent.transform, Vector3.zero, 2.5f);

        // Свет костра
        var lightGO = new GameObject("CampfireLight");
        lightGO.transform.position = pos + Vector3.up * 0.4f;
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.08f);
        light.intensity = 3.5f;
        light.range = 8f;
        light.color = new Color(1f, 0.6f, 0.15f);
        var flicker = lightGO.AddComponent<TorchFlicker>();
        flicker.minIntensity = 2.5f;
        flicker.maxIntensity = 4.5f;
        flicker.flickerSpeed = 4f;

        // ✅ Второй свет — синеватый ореол для атмосферы
        var glowGO = new GameObject("CampfireGlow");
        glowGO.transform.position = pos + Vector3.up * 0.8f;
        var glowLight = glowGO.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = new Color(0.8f, 0.3f, 0.05f);
        glowLight.intensity = 1.2f;
        glowLight.range = 12f;
        flicker.flickerSpeed = 5f;
    }

    void SetupFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.35f;  // ✅ густой туман
        RenderSettings.fogColor = new Color(0.02f, 0.01f, 0.03f); // почти чёрный
        RenderSettings.ambientLight = new Color(0.02f, 0.01f, 0.03f); // очень тёмно
    }
}