matrix transformMatrix;

texture dustTexture;

float2 textureSize;

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
    return input.InstanceColor;
}

technique Technique1
{
    pass InstancedGoreRendererPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
};