using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int secondaryColorId = Shader.PropertyToID("_SecondaryColor");
    static readonly int blendFactor = Shader.PropertyToID("_BlendFactor");

    static MaterialPropertyBlock block;

    Renderer myRenderer;

    private void Awake()
    {
        if (block == null)
            block = new MaterialPropertyBlock();

        myRenderer = GetComponent<Renderer>();
    }

    public void SetBaseColor(Color color)
    {
        //block.Clear();
        block.SetColor(baseColorId, color);
        myRenderer.SetPropertyBlock(block);
    }

    public void SetSecondaryColor(Color color)
    {
        //block.Clear();
        block.SetColor(secondaryColorId, color);
        myRenderer.SetPropertyBlock(block);
    }

    public void SetBlendFactor(float factor)
    {
        //block.Clear();
        block.SetFloat(blendFactor, factor);
        myRenderer.SetPropertyBlock(block);
    }
}
