using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Renderer))]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int secondaryColorId = Shader.PropertyToID("_SecondaryColor");
    static readonly int blendFactorId = Shader.PropertyToID("_BlendFactor");
    static readonly int objectWorldPosId = Shader.PropertyToID("_ObjectPos");
    static readonly int fovRadiusId = Shader.PropertyToID("_Radius");

    static MaterialPropertyBlock block;

    Renderer myRenderer;

    [Header("Settings")]
    [SerializeField] Color BaseColor;
    [SerializeField] Color SecondaryColor;

    private void Awake()
    {
        if (block == null)
            block = new MaterialPropertyBlock();

        block.Clear();
        block.SetColor(baseColorId, BaseColor);
        myRenderer = GetComponent<Renderer>();
        myRenderer.SetPropertyBlock(block);
    }

    public void SetBaseColor()
    {
        block.Clear();
        myRenderer.GetPropertyBlock(block);

        block.SetColor(baseColorId, BaseColor);
        block.SetFloat(blendFactorId, 0f);
        myRenderer.SetPropertyBlock(block);
    }

    public void SetSecondaryColor()
    {
        block.Clear();
        myRenderer.GetPropertyBlock(block);

        block.SetColor(secondaryColorId, SecondaryColor);
        block.SetFloat(blendFactorId, 1f);
        myRenderer.SetPropertyBlock(block);
    }

    public void SetBlendFactor(float factor)
    {
        block.Clear();
        myRenderer.GetPropertyBlock(block);

        block.SetFloat(blendFactorId, factor);
        myRenderer.SetPropertyBlock(block);
    }

    public void SetObjectPos(Vector3 pos)
    {
        block.Clear();
        myRenderer.GetPropertyBlock(block);

        block.SetVector(objectWorldPosId, new Vector4(pos.x, pos.y, pos.z, 1f));
        myRenderer.SetPropertyBlock(block);
    }

    public void SetFOVRadius(float radius)
    {
        block.Clear();
        myRenderer.GetPropertyBlock(block);

        block.SetFloat(fovRadiusId, radius);
        myRenderer.SetPropertyBlock(block);
    }
}
