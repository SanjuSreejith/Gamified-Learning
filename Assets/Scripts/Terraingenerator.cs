using UnityEngine;
using UnityEngine.U2D;

[ExecuteInEditMode]
public class GroundGenerator : MonoBehaviour
{
    public SpriteShapeController spriteShape;

    [Header("Level Length")]
    [Range(30, 1000)] public int levelLength = 120;
    public float xStep = 1.5f;

    [Header("Height Control")]
    public float maxHillHeight = 4f;
    public float flatHeightVariation = 0.3f;
    [Range(0.1f, 1f)] public float smoothness = 0.6f;

    [Header("Segment Chances")]
    [Range(0f, 1f)] public float flatChance = 0.45f;
    [Range(0f, 1f)] public float smallHillChance = 0.35f;
    // Big hill = remaining %

    [Header("Bottom")]
    public float bottomDepth = 25f;

    // 🔘 BUTTON IN INSPECTOR
    [ContextMenu("Generate Ground")]
    public void GenerateGround()
    {
        if (!spriteShape)
        {
            Debug.LogWarning("SpriteShape not assigned!");
            return;
        }

        var spline = spriteShape.spline;
        spline.Clear();

        float x = 0f;
        float currentHeight = 0f;
        int i = 0;

        while (i < levelLength)
        {
            float r = Random.value;

            // -------- FLAT --------
            if (r < flatChance)
            {
                int length = Random.Range(6, 12);
                for (int j = 0; j < length && i < levelLength; j++, i++)
                {
                    currentHeight += Random.Range(-flatHeightVariation, flatHeightVariation);
                    AddPoint(spline, i, x, currentHeight);
                    x += xStep;
                }
            }
            // -------- SMALL HILL --------
            else if (r < flatChance + smallHillChance)
            {
                int length = Random.Range(8, 14);
                float target = currentHeight + Random.Range(-maxHillHeight * 0.5f, maxHillHeight * 0.5f);
                GenerateSlope(spline, ref i, ref x, ref currentHeight, target, length);
            }
            // -------- BIG HILL --------
            else
            {
                int length = Random.Range(12, 20);
                float target = currentHeight + Random.Range(-maxHillHeight, maxHillHeight);
                GenerateSlope(spline, ref i, ref x, ref currentHeight, target, length);
            }
        }

        // Close bottom
        spline.InsertPointAt(levelLength, new Vector3(x, -bottomDepth));
        spline.InsertPointAt(levelLength + 1, new Vector3(0, -bottomDepth));

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(spriteShape);
#endif
    }

    void GenerateSlope(
        Spline spline,
        ref int i,
        ref float x,
        ref float currentHeight,
        float targetHeight,
        int length)
    {
        float step = (targetHeight - currentHeight) / length;

        for (int j = 0; j < length && i < levelLength; j++, i++)
        {
            currentHeight += step;
            AddPoint(spline, i, x, currentHeight);
            x += xStep;
        }
    }

    void AddPoint(Spline spline, int index, float x, float y)
    {
        spline.InsertPointAt(index, new Vector3(x, y));
        spline.SetTangentMode(index, ShapeTangentMode.Continuous);
        spline.SetLeftTangent(index, Vector3.left * xStep * smoothness);
        spline.SetRightTangent(index, Vector3.right * xStep * smoothness);
    }
}
