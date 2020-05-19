struct VS_INPUT
{
    float4 Pos : POSITION;
    float4 Color : COLOR0;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR0;
};


cbuffer perPass : register(b0)
{
    float4x4 World;
}

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT ret;
    ret.Pos =  mul(World, input.Pos);
    ret.Color = input.Color;
    return ret;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    return input.Color;
}