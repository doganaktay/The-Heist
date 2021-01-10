using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Renderer))]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int secondaryColorId = Shader.PropertyToID("_SecondaryColor");
    static readonly int blendFactorId = Shader.PropertyToID("_BlendFactor");

    static MaterialPropertyBlock block;

    Renderer myRenderer;

    [Header("Settings")]
    [SerializeField] Color BaseColor;
    [SerializeField] Color SecondaryColor;

    private void Awake()
    {
        if (block == null)
        { block = new MaterialPropertyBlock(); Debug.Log($"{gameObject.name} creating new material property block"); }

        block.Clear();
        block.SetColor(baseColorId, BaseColor);
        myRenderer = GetComponent<Renderer>();
        myRenderer.SetPropertyBlock(block);
    }

    public void SetBaseColor()
    {
        //block.Clear();
        //myRenderer.GetPropertyBlock(block);

        block.SetColor(baseColorId, BaseColor);
        block.SetFloat(blendFactorId, 0f);
        myRenderer.SetPropertyBlock(block);
    }

    public void SetSecondaryColor()
    {
        //block.Clear();
        //myRenderer.GetPropertyBlock(block);

        block.SetColor(secondaryColorId, SecondaryColor);
        block.SetFloat(blendFactorId, 1f);
        myRenderer.SetPropertyBlock(block);

        Debug.Log($"Setting secondary color for {gameObject.name}");
    }

    public void SetBlendFactor(float factor)
    {
        //block.Clear();
        //myRenderer.GetPropertyBlock(block);

        block.SetFloat(blendFactorId, factor);
        myRenderer.SetPropertyBlock(block);
    }
}
