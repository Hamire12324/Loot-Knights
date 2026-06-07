using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UrpMaterialConverterTool
{
    private const string MenuRoot = "Tools/URP Materials/";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    private const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";
    private const string UrpSpriteLitShaderName = "Universal Render Pipeline/2D/Sprite-Lit-Default";
    private const string UrpSpriteUnlitShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

    [MenuItem(MenuRoot + "Convert Selected Materials", false, 100)]
    private static void ConvertSelectedMaterials()
    {
        List<Material> materials = CollectSelectedMaterials();
        if (materials.Count == 0)
        {
            EditorUtility.DisplayDialog("URP Material Converter", "No materials found in the current selection.", "OK");
            return;
        }

        ConvertMaterials(materials, "selected");
    }

    [MenuItem(MenuRoot + "Convert Selected Materials", true)]
    private static bool HasSelection()
    {
        return Selection.objects != null && Selection.objects.Length > 0;
    }

    [MenuItem(MenuRoot + "Convert All Project Materials", false, 101)]
    private static void ConvertAllProjectMaterials()
    {
        if (!EditorUtility.DisplayDialog(
                "URP Material Converter",
                "Convert every material asset in the project to URP shaders?\n\nMake sure your project is backed up or committed before continuing.",
                "Convert",
                "Cancel"))
        {
            return;
        }

        ConvertMaterials(CollectAllMaterials(), "project");
    }

    private static void ConvertMaterials(IReadOnlyList<Material> materials, string scopeName)
    {
        Shader urpLit = Shader.Find(UrpLitShaderName);
        Shader urpUnlit = Shader.Find(UrpUnlitShaderName);
        Shader urpSpriteLit = Shader.Find(UrpSpriteLitShaderName);
        Shader urpSpriteUnlit = Shader.Find(UrpSpriteUnlitShaderName);

        if (urpLit == null || urpUnlit == null)
        {
            EditorUtility.DisplayDialog(
                "URP Material Converter",
                "Could not find URP Lit/Unlit shaders. Check that Universal RP is installed in Package Manager.",
                "OK");
            return;
        }

        int converted = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    skipped++;
                    continue;
                }

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Converting Materials To URP",
                        $"{material.name} ({i + 1}/{materials.Count})",
                        (float)(i + 1) / materials.Count))
                {
                    break;
                }

                if (ConvertMaterial(material, urpLit, urpUnlit, urpSpriteLit, urpSpriteUnlit))
                {
                    EditorUtility.SetDirty(material);
                    converted++;
                }
                else
                {
                    skipped++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"URP Material Converter: converted {converted} material(s), skipped {skipped} material(s) in {scopeName} scope.");
        EditorUtility.DisplayDialog(
            "URP Material Converter",
            $"Done.\n\nConverted: {converted}\nSkipped: {skipped}",
            "OK");
    }

    private static bool ConvertMaterial(Material material, Shader urpLit, Shader urpUnlit, Shader urpSpriteLit, Shader urpSpriteUnlit)
    {
        string oldShaderName = material.shader != null ? material.shader.name : string.Empty;
        if (oldShaderName.StartsWith("Universal Render Pipeline/"))
        {
            return false;
        }

        MaterialSnapshot snapshot = MaterialSnapshot.From(material);
        Shader targetShader = ChooseTargetShader(oldShaderName, urpLit, urpUnlit, urpSpriteLit, urpSpriteUnlit);
        if (targetShader == null)
        {
            return false;
        }

        Undo.RecordObject(material, "Convert Material To URP");
        material.shader = targetShader;
        ApplySnapshotToUrpMaterial(material, snapshot);
        return true;
    }

    private static Shader ChooseTargetShader(string oldShaderName, Shader urpLit, Shader urpUnlit, Shader urpSpriteLit, Shader urpSpriteUnlit)
    {
        string shaderName = oldShaderName.ToLowerInvariant();

        if (shaderName.Contains("sprites/default") || shaderName.Contains("sprite-unlit"))
        {
            return urpSpriteUnlit != null ? urpSpriteUnlit : urpUnlit;
        }

        if (shaderName.Contains("sprites/diffuse") || shaderName.Contains("sprite-lit"))
        {
            return urpSpriteLit != null ? urpSpriteLit : urpLit;
        }

        if (shaderName.Contains("unlit") || shaderName.Contains("mobile/particles"))
        {
            return urpUnlit;
        }

        return urpLit;
    }

    private static void ApplySnapshotToUrpMaterial(Material material, MaterialSnapshot snapshot)
    {
        SetTexture(material, "_BaseMap", snapshot.MainTexture, snapshot.MainTextureScale, snapshot.MainTextureOffset);
        SetTexture(material, "_MainTex", snapshot.MainTexture, snapshot.MainTextureScale, snapshot.MainTextureOffset);
        SetColor(material, "_BaseColor", snapshot.BaseColor);
        SetColor(material, "_Color", snapshot.BaseColor);

        SetTexture(material, "_BumpMap", snapshot.NormalMap, snapshot.NormalScale, Vector2.zero);
        SetFloat(material, "_BumpScale", snapshot.NormalStrength);

        SetTexture(material, "_MetallicGlossMap", snapshot.MetallicMap, Vector2.one, Vector2.zero);
        SetFloat(material, "_Metallic", snapshot.Metallic);
        SetFloat(material, "_Smoothness", snapshot.Smoothness);

        SetTexture(material, "_SpecGlossMap", snapshot.SpecularMap, Vector2.one, Vector2.zero);
        SetColor(material, "_SpecColor", snapshot.SpecularColor);

        SetTexture(material, "_OcclusionMap", snapshot.OcclusionMap, Vector2.one, Vector2.zero);
        SetFloat(material, "_OcclusionStrength", snapshot.OcclusionStrength);

        SetTexture(material, "_EmissionMap", snapshot.EmissionMap, Vector2.one, Vector2.zero);
        SetColor(material, "_EmissionColor", snapshot.EmissionColor);

        if (material.HasProperty("_WorkflowMode"))
        {
            material.SetFloat("_WorkflowMode", snapshot.SpecularMap != null ? 0f : 1f);
        }

        ApplySurfaceSettings(material, snapshot);
        ApplyKeywords(material, snapshot);
    }

    private static void ApplySurfaceSettings(Material material, MaterialSnapshot snapshot)
    {
        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", snapshot.AlphaClip ? 1f : 0f);
        }

        if (material.HasProperty("_Cutoff"))
        {
            material.SetFloat("_Cutoff", snapshot.AlphaCutoff);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", snapshot.Transparent ? 1f : 0f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", snapshot.PremultiplyAlpha ? 1f : 0f);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", snapshot.CullMode);
        }

        int srcBlend = snapshot.PremultiplyAlpha
            ? (int)UnityEngine.Rendering.BlendMode.One
            : (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
        int dstBlend = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

        if (snapshot.Transparent)
        {
            SetFloat(material, "_SrcBlend", srcBlend);
            SetFloat(material, "_DstBlend", dstBlend);
            SetFloat(material, "_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return;
        }

        SetFloat(material, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        SetFloat(material, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        SetFloat(material, "_ZWrite", 1f);
        material.SetOverrideTag("RenderType", snapshot.AlphaClip ? "TransparentCutout" : "Opaque");
        material.renderQueue = snapshot.AlphaClip ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
    }

    private static void ApplyKeywords(Material material, MaterialSnapshot snapshot)
    {
        SetKeyword(material, "_NORMALMAP", snapshot.NormalMap != null);
        SetKeyword(material, "_METALLICSPECGLOSSMAP", snapshot.MetallicMap != null || snapshot.SpecularMap != null);
        SetKeyword(material, "_OCCLUSIONMAP", snapshot.OcclusionMap != null);
        SetKeyword(material, "_EMISSION", snapshot.HasEmission);
        SetKeyword(material, "_ALPHATEST_ON", snapshot.AlphaClip);
        SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", snapshot.Transparent);
        SetKeyword(material, "_ALPHAPREMULTIPLY_ON", snapshot.Transparent && snapshot.PremultiplyAlpha);
    }

    private static List<Material> CollectSelectedMaterials()
    {
        var materials = new List<Material>();
        var seen = new HashSet<string>();

        foreach (Object selectedObject in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (AssetDatabase.IsValidFolder(path))
            {
                AddMaterialsFromGuids(AssetDatabase.FindAssets("t:Material", new[] { path }), materials, seen);
                continue;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && seen.Add(path))
            {
                materials.Add(material);
            }
        }

        return materials;
    }

    private static List<Material> CollectAllMaterials()
    {
        var materials = new List<Material>();
        AddMaterialsFromGuids(AssetDatabase.FindAssets("t:Material", new[] { "Assets" }), materials, new HashSet<string>());
        return materials;
    }

    private static void AddMaterialsFromGuids(IEnumerable<string> guids, List<Material> materials, HashSet<string> seen)
    {
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!seen.Add(path))
            {
                continue;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                materials.Add(material);
            }
        }
    }

    private static void SetTexture(Material material, string propertyName, Texture texture, Vector2 scale, Vector2 offset)
    {
        if (!material.HasProperty(propertyName))
        {
            return;
        }

        material.SetTexture(propertyName, texture);
        if (texture != null)
        {
            material.SetTextureScale(propertyName, scale);
            material.SetTextureOffset(propertyName, offset);
        }
    }

    private static void SetColor(Material material, string propertyName, Color color)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, color);
        }
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    private readonly struct MaterialSnapshot
    {
        public readonly Texture MainTexture;
        public readonly Vector2 MainTextureScale;
        public readonly Vector2 MainTextureOffset;
        public readonly Color BaseColor;
        public readonly Texture NormalMap;
        public readonly Vector2 NormalScale;
        public readonly float NormalStrength;
        public readonly Texture MetallicMap;
        public readonly Texture SpecularMap;
        public readonly Color SpecularColor;
        public readonly float Metallic;
        public readonly float Smoothness;
        public readonly Texture OcclusionMap;
        public readonly float OcclusionStrength;
        public readonly Texture EmissionMap;
        public readonly Color EmissionColor;
        public readonly bool HasEmission;
        public readonly bool Transparent;
        public readonly bool PremultiplyAlpha;
        public readonly bool AlphaClip;
        public readonly float AlphaCutoff;
        public readonly float CullMode;

        private MaterialSnapshot(Material material)
        {
            MainTexture = GetTexture(material, "_MainTex", "_BaseMap");
            MainTextureScale = GetTextureScale(material, "_MainTex", "_BaseMap");
            MainTextureOffset = GetTextureOffset(material, "_MainTex", "_BaseMap");
            BaseColor = GetColor(material, Color.white, "_Color", "_BaseColor", "_TintColor");
            NormalMap = GetTexture(material, "_BumpMap");
            NormalScale = GetTextureScale(material, "_BumpMap");
            NormalStrength = GetFloat(material, 1f, "_BumpScale");
            MetallicMap = GetTexture(material, "_MetallicGlossMap");
            SpecularMap = GetTexture(material, "_SpecGlossMap");
            SpecularColor = GetColor(material, Color.white, "_SpecColor");
            Metallic = GetFloat(material, 0f, "_Metallic");
            Smoothness = GetFloat(material, GetFloat(material, 0.5f, "_Shininess"), "_Glossiness", "_Smoothness");
            OcclusionMap = GetTexture(material, "_OcclusionMap");
            OcclusionStrength = GetFloat(material, 1f, "_OcclusionStrength");
            EmissionMap = GetTexture(material, "_EmissionMap");
            EmissionColor = GetColor(material, Color.black, "_EmissionColor");
            HasEmission = EmissionMap != null || EmissionColor.maxColorComponent > 0.001f;
            AlphaCutoff = GetFloat(material, 0.5f, "_Cutoff");
            AlphaClip = IsAlphaClip(material);
            Transparent = IsTransparent(material, BaseColor);
            PremultiplyAlpha = IsPremultiplyAlpha(material);
            CullMode = GetFloat(material, 2f, "_Cull");
        }

        public static MaterialSnapshot From(Material material)
        {
            return new MaterialSnapshot(material);
        }
    }

    private static Texture GetTexture(Material material, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetTexture(name);
            }
        }

        return null;
    }

    private static Vector2 GetTextureScale(Material material, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetTextureScale(name);
            }
        }

        return Vector2.one;
    }

    private static Vector2 GetTextureOffset(Material material, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetTextureOffset(name);
            }
        }

        return Vector2.zero;
    }

    private static Color GetColor(Material material, Color fallback, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetColor(name);
            }
        }

        return fallback;
    }

    private static float GetFloat(Material material, float fallback, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetFloat(name);
            }
        }

        return fallback;
    }

    private static bool IsAlphaClip(Material material)
    {
        return material.IsKeywordEnabled("_ALPHATEST_ON") ||
               GetFloat(material, 0f, "_AlphaClip") > 0.5f ||
               material.GetTag("RenderType", false, string.Empty) == "TransparentCutout";
    }

    private static bool IsTransparent(Material material, Color baseColor)
    {
        string renderType = material.GetTag("RenderType", false, string.Empty);
        if (renderType == "Transparent")
        {
            return true;
        }

        if (material.HasProperty("_Mode") && material.GetFloat("_Mode") >= 2f)
        {
            return true;
        }

        if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
        {
            return true;
        }

        return baseColor.a < 0.999f;
    }

    private static bool IsPremultiplyAlpha(Material material)
    {
        return material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON") ||
               (material.HasProperty("_Mode") && Mathf.Approximately(material.GetFloat("_Mode"), 3f));
    }
}
