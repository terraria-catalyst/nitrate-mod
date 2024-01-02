sampler chunkTexture : register(s0);

texture lightMap;

float2 size;
float2 offset;

// Color matrix for the 3x3 tile area.
static float4 c[9];

float globalBrightness;

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
    // 2D vector containing normalised values (0..1) for the dimensions of one pixel.
    float2 onePixel = 1 / size;

    // Scale up by 16 for the dimensions of one tile.
    float2 oneTile = onePixel * 16;

    // Represents the offset of the screen tile grid from the lighting buffer grid, to fix desync.
    float2 offsetUV = float2(onePixel.x * offset.x, onePixel.y * offset.y);

    // Distance (0..1) from the current pixel to the corner of the tile.
    float2 tileUV = frac((TexCoord - offsetUV) / oneTile);

    float dx = tileUV.x;
    float dy = tileUV.y;

    // For the time being this just does 9-slice rendering. I'd like to generalise it but it would probably be much more involved than just this.

    // Sample from tiles in a 3x3 square centered on this tile.
    for (int i = 0; i < 9; i++) {
        // colorMatrix[4] would be the color of the current tile.
        int x = (i % 3) - 1;
        int y = (i / 3) - 1;

        c[i] = tex2D(LightSampler, TexCoord + float2(x * oneTile.x, y * oneTile.y));
    }

    int indexX = 0;
    indexX += (dx < 0.75f) * (dx > 0.25f);
    indexX += ((dx <= 1) * (dx > 0.75f)) * 2;

    int indexY = 0;
    indexY += (dy < 0.75f) * (dy > 0.25f);
    indexY += ((dy <= 1) * (dy > 0.75f)) * 2;

    int index = (indexY * 3) + indexX;

    return tex2D(chunkTexture, TexCoord) * ((c[4] + c[index]) / 2) * globalBrightness;
}

technique Technique1
{
    pass LightMapRendererPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
};