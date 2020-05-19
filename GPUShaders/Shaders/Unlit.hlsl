struct VS_INPUT
{
    float4 Pos : POSITION;
    float2 TexCoord : TEXCOORD0;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

Texture2D texMap : register(t0);
SamplerState sampState  : register(s0);

cbuffer perPass : register(b1)
{
    float4x4 WVP;
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT ret;
    ret.Pos = mul(WVP, input.Pos);
    ret.TexCoord = input.TexCoord;
    return ret;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    return texMap.Sample(sampState, input.TexCoord);
}