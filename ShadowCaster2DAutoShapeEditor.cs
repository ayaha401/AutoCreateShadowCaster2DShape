using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngine.Rendering.Universal;
using System;

[CustomEditor(typeof(ShadowCaster2D))]
public class ShadowCaster2DAutoShapeEditor : Editor
{
    private ShadowCaster2D shadowCaster2d;

    static readonly FieldInfo meshField = typeof(ShadowCaster2D).GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly MethodInfo generateShadowMeshMethod = typeof(ShadowCaster2D)
                                    .Assembly
                                    .GetType("UnityEngine.Rendering.Universal.ShadowUtility")
                                    .GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);
    static readonly FieldInfo selfShadowsField = typeof(ShadowCaster2D).GetField("m_SelfShadows", BindingFlags.NonPublic | BindingFlags.Instance);

    private void OnEnable()
    {
        shadowCaster2d = (ShadowCaster2D)target;
    }

    public override void OnInspectorGUI()
    {
        var loadAssembly = Assembly.Load("Unity.RenderPipelines.Universal.Editor");
        var type = loadAssembly.GetType("UnityEditor.Rendering.Universal.ShadowCaster2DEditor");
        if (type == null)
        {
            return;
        }
        var editor = CreateEditor(target, type);
        editor?.OnInspectorGUI();

        using (new EditorGUILayout.VerticalScope())
        {
            if (GUILayout.Button("Auto Create Shadow Shape"))
            {
                if (shadowCaster2d == null)
                {
                    return;
                }

                GameObject obj = shadowCaster2d.gameObject;

                bool hasSpriteRenderer = obj.GetComponent<SpriteRenderer>() != null;
                if (hasSpriteRenderer)
                {
                    UseSpriteRendererMesh(obj);
                    EditorUtility.SetDirty((ShadowCaster2D)target);
                    return;
                }

                bool hasCompositeCollider2D = obj.GetComponent<CompositeCollider2D>() != null;
                if (!hasSpriteRenderer && hasCompositeCollider2D)
                {
                    DestroyTilemapShadowCasterObjs(obj);
                    UseTilemap(obj);
                    EditorUtility.SetDirty((ShadowCaster2D)target);
                    return;
                }

                Debug.LogError("SpriteRenderer or CompositeCollider2D is not attached");
            }

            if(GUILayout.Button("Delete Shadow Shape"))
            {
                if (shadowCaster2d == null)
                {
                    return;
                }

                GameObject obj = shadowCaster2d.gameObject;

                bool hasSpriteRenderer = obj.GetComponent<SpriteRenderer>() != null;
                if(hasSpriteRenderer)
                {
                    shapePathField.SetValue(shadowCaster2d, new Vector3[0]);
                    meshField.SetValue(shadowCaster2d, new Mesh());
                    EditorUtility.SetDirty((ShadowCaster2D)target);
                    return;
                }

                bool hasCompositeCollider2D = obj.GetComponent<CompositeCollider2D>() != null;
                if(hasCompositeCollider2D)
                {
                    DestroyTilemapShadowCasterObjs(obj);
                    EditorUtility.SetDirty((ShadowCaster2D)target);
                    return;
                }

                Debug.LogError("SpriteRenderer or CompositeCollider2D is not attached");
            }
        }
    }

    /// <summary>
    /// SpriteRendererのMesh情報からShadowShapeを形成
    /// </summary>
    private void UseSpriteRendererMesh(GameObject obj)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        Vector2[] vertices = spriteRenderer.sprite.vertices;

        // 重心を使って頂点を時計回りにソートする
        Vector2 center = Vector2.zero;
        foreach (Vector2 vertex in vertices)
        {
            center += vertex;
        }
        center /= vertices.Length;

        Array.Sort(vertices, (v1, v2) =>
        {
            float angle1 = Mathf.Atan2(v1.y - center.y, v1.x - center.x);
            float angle2 = Mathf.Atan2(v2.y - center.y, v2.x - center.x);
            return angle2.CompareTo(angle1);
        });

        // MeshにShapeの形を合わせる
        Vector3[] hogePath = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            hogePath[i] = vertices[i];
        }
        shapePathField.SetValue(shadowCaster2d, hogePath);
        shapePathHashField.SetValue(shadowCaster2d, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        meshField.SetValue(shadowCaster2d, new Mesh());
    }

    /// <summary>
    /// TilemapのCompositeCollider2Dから形成
    /// </summary>
    private void UseTilemap(GameObject obj)
    {
        CompositeCollider2D tilemapCollider = obj.GetComponent<CompositeCollider2D>();

        for (int i = 0; i < tilemapCollider.pathCount; i++)
        {
            Vector2[] pathVertices = new Vector2[tilemapCollider.GetPathPointCount(i)];
            tilemapCollider.GetPath(i, pathVertices);
            GameObject shadowCaster = new GameObject("shadow_caster_" + i);
            shadowCaster.transform.parent = obj.transform;
            ShadowCaster2D shadowCasterComponent = shadowCaster.AddComponent<ShadowCaster2D>();
            shadowCasterComponent.selfShadows = (bool)selfShadowsField.GetValue(shadowCaster2d);

            Vector3[] testPath = new Vector3[pathVertices.Length];
            for (int j = 0; j < pathVertices.Length; j++)
            {
                testPath[j] = pathVertices[j] + (Vector2)obj.transform.position;
            }

            shapePathField.SetValue(shadowCasterComponent, testPath);
            shapePathHashField.SetValue(shadowCasterComponent, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            meshField.SetValue(shadowCasterComponent, new Mesh());
            generateShadowMeshMethod.Invoke(shadowCasterComponent,
            new object[] { meshField.GetValue(shadowCasterComponent), shapePathField.GetValue(shadowCasterComponent) });
        }
    }

    /// <summary>
    /// Tilemapで作ったShadowCasterを消す
    /// </summary>
    private void DestroyTilemapShadowCasterObjs(GameObject obj)
    {
        var shadowShapeObjs = obj.GetComponentsInChildren<ShadowCaster2D>();
        int nonTargetParent = 1;
        for (int i = nonTargetParent; i < shadowShapeObjs.Length; i++)
        {
            DestroyImmediate(shadowShapeObjs[i].gameObject);
        }
    }
}
