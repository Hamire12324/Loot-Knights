using UnityEngine;
using UnityEngine.UI;

public class UICircleGraphic : MaskableGraphic
{
    [SerializeField, Range(0f, 1f)] private float fillAmount = 1f;
    [SerializeField, Range(0f, 1f)] private float innerRadius = 0f;
    [SerializeField, Min(8)] private int segments = 64;
    [SerializeField] private float startAngle = 90f;
    [SerializeField] private bool clockwise = true;

    public float FillAmount
    {
        get => fillAmount;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(fillAmount, clamped)) return;

            fillAmount = clamped;
            SetVerticesDirty();
        }
    }

    public float InnerRadius
    {
        get => innerRadius;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(innerRadius, clamped)) return;

            innerRadius = clamped;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (fillAmount <= 0f) return;

        Rect rect = rectTransform.rect;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        if (radius <= 0f) return;

        Vector2 center = rect.center;
        int arcSegments = Mathf.Max(1, Mathf.CeilToInt(segments * fillAmount));
        float signedArc = 360f * fillAmount * (clockwise ? -1f : 1f);
        float inner = radius * innerRadius;

        if (inner <= 0.001f)
        {
            AddFilledCircle(vh, center, radius, arcSegments, signedArc);
            return;
        }

        AddRing(vh, center, radius, inner, arcSegments, signedArc);
    }

    private void AddFilledCircle(VertexHelper vh, Vector2 center, float radius, int arcSegments, float signedArc)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = center;
        vh.AddVert(vertex);

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            float angle = (startAngle + signedArc * t) * Mathf.Deg2Rad;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            vertex.position = point;
            vh.AddVert(vertex);
        }

        for (int i = 1; i <= arcSegments; i++)
            vh.AddTriangle(0, i, i + 1);
    }

    private void AddRing(VertexHelper vh, Vector2 center, float outerRadius, float innerRadius, int arcSegments, float signedArc)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            float angle = (startAngle + signedArc * t) * Mathf.Deg2Rad;
            Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));

            vertex.position = center + dir * outerRadius;
            vh.AddVert(vertex);

            vertex.position = center + dir * innerRadius;
            vh.AddVert(vertex);
        }

        for (int i = 0; i < arcSegments; i++)
        {
            int index = i * 2;
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 1, index + 3);
        }
    }
}
