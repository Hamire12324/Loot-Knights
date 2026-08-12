using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class MobileSkillAimInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
{
    [SerializeField, Min(1f)] private float dragThresholdPixels = 18f;
    [SerializeField, Min(0.02f)] private float previewLineWidth = 0.06f;
    [SerializeField, Min(0.02f)] private float previewReticleRadius = 0.28f;
    [SerializeField] private Color previewColor = new(0.25f, 0.9f, 1f, 0.9f);

    private ButtonHeroSkill skillButton;
    private Vector2 pointerDownPosition;
    private Vector2 targetPosition;
    private bool pointerDown;
    private bool isDragging;
    private GameObject previewRoot;
    private LineRenderer directionLine;
    private LineRenderer targetReticle;
    private Material previewMaterial;

    public void SetSkillButton(ButtonHeroSkill button)
    {
        skillButton = button;
    }

    private void Awake()
    {
        skillButton ??= GetComponent<ButtonHeroSkill>();
    }

    private void OnDisable()
    {
        CancelAim();
    }

    private void OnDestroy()
    {
        if (previewRoot != null)
            Destroy(previewRoot);

        if (previewMaterial != null)
            Destroy(previewMaterial);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = skillButton != null && skillButton.SupportsManualAim;
        isDragging = false;
        pointerDownPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pointerDown || skillButton == null)
            return;

        if (!isDragging)
        {
            float dragDistanceSquared = (eventData.position - pointerDownPosition).sqrMagnitude;
            if (dragDistanceSquared < dragThresholdPixels * dragThresholdPixels)
                return;
        }

        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || !TryGetWorldPosition(eventData.position, hero.transform.position.z, out Vector2 worldPosition))
            return;

        Vector2 origin = hero.transform.position;
        Vector2 offset = worldPosition - origin;
        if (offset.sqrMagnitude < 0.001f)
            return;

        targetPosition = origin + Vector2.ClampMagnitude(offset, skillButton.ManualAimRange);
        isDragging = true;
        ShowPreview(origin, targetPosition);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pointerDown)
            return;

        bool wasDragging = isDragging;
        pointerDown = false;
        isDragging = false;
        HidePreview();

        if (!wasDragging || skillButton == null)
            return;

        // A drag is always a manual action. Prevent the Button click event that
        // follows PointerUp from casting again with automatic target selection.
        skillButton.SuppressClickForCurrentGesture();
        skillButton.TryCastAtPosition(targetPosition);
    }

    public void OnCancel(BaseEventData eventData)
    {
        CancelAim();
    }

    private void CancelAim()
    {
        pointerDown = false;
        isDragging = false;
        HidePreview();
    }

    private static bool TryGetWorldPosition(Vector2 screenPosition, float worldZ, out Vector2 worldPosition)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            worldPosition = default;
            return false;
        }

        Ray ray = camera.ScreenPointToRay(screenPosition);
        Plane castPlane = new(Vector3.forward, new Vector3(0f, 0f, worldZ));
        if (!castPlane.Raycast(ray, out float distance))
        {
            worldPosition = default;
            return false;
        }

        worldPosition = ray.GetPoint(distance);
        return true;
    }

    private void ShowPreview(Vector2 origin, Vector2 target)
    {
        EnsurePreview();
        if (previewRoot == null)
            return;

        previewRoot.SetActive(true);
        directionLine.SetPosition(0, origin);
        directionLine.SetPosition(1, target);

        const int segments = 32;
        targetReticle.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * previewReticleRadius;
            targetReticle.SetPosition(i, target + offset);
        }
    }

    private void HidePreview()
    {
        if (previewRoot != null)
            previewRoot.SetActive(false);
    }

    private void EnsurePreview()
    {
        if (previewRoot != null)
            return;

        previewRoot = new GameObject("Mobile Skill Aim Preview")
        {
            hideFlags = HideFlags.DontSave
        };
        previewRoot.SetActive(false);

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            previewMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };

        directionLine = CreateLineRenderer("Direction", 2, false);
        targetReticle = CreateLineRenderer("Target Reticle", 33, true);
    }

    private LineRenderer CreateLineRenderer(string objectName, int positionCount, bool loop)
    {
        GameObject lineObject = new(objectName);
        lineObject.transform.SetParent(previewRoot.transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = positionCount;
        line.loop = loop;
        line.widthMultiplier = previewLineWidth;
        line.startColor = previewColor;
        line.endColor = previewColor;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.sortingOrder = 100;
        if (previewMaterial != null)
            line.sharedMaterial = previewMaterial;

        return line;
    }
}
