sampler chunkTexture : register(s0);

texture lightMap;

sampler2D LightSampler = sampler_state
{
    Texture = (lightMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;
};

float4 PixelShaderFunction(float2 TexCoord : TEXCOORD0) : COLOR0
{
    return tex2D(chunkTexture, TexCoord) * tex2D(LightSampler, TexCoord);
}

technique Technique1
{
    pass LightMapRendererPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
};