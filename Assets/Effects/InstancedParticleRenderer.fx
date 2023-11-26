matrix transformMatrix;

texture dustTexture;

sampler2D DustSampler = sampler_state
{
    Texture = (dustTexture);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;
};

struct VertexShaderInput
{
    float4 Position : POSITION;
    float2 VertexTexCoord : TEXCOORD0;
    row_major float4x4 InstanceTransform : NORMAL0;
    float4 InstanceTexCoord : TEXCOORD1;
    float4 InstanceColor : COLOR1;
};

struct VertexShaderOutput
{
    float4 Position : POSITION;
    float2 VertexTexCoord : TEXCOORD0;
    float4 InstanceTexCoord : TEXCOORD1;
    float4 InstanceColor : COLOR1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(input.Position, mul(input.InstanceTransform, transformMatrix));
    output.VertexTexCoord = input.VertexTexCoord;
    output.InstanceTexCoord = input.InstanceTexCoord;
    output.InstanceColor = input.InstanceColor;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // The InstanceTexCoord parameter is a Vector4. The first 2 elements are the top-left of the desired region while the last two are the bottom-right.
    // Using this and the VertexTexCoord argument (which is just a standard 0..1 UV) we can sample the desired region.
    float sampleX = lerp(input.InstanceTexCoord.x, input.InstanceTexCoord.z, input.VertexTexCoord.x);
    float sampleY = lerp(input.InstanceTexCoord.y, input.InstanceTexCoord.w, input.VertexTexCoord.y);

    return tex2D(DustSampler, float2(sampleX, sampleY)) * input.InstanceColor;
}

technique Technique1
{
    pass InstancedGoreRendererPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
};