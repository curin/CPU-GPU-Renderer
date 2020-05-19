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

cbuffer ConstantBuffer : register(b1)
{
    int Rows;
    int Columns;
    int Frame;
};

Texture2D texMap : register(t0);
SamplerState sampState  : register(s0);

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT ret;
    ret.Pos = input.Pos;
    ret.TexCoord = input.TexCoord;
    return ret;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    float2 Loc = input.TexCoord;
    float2 FramePos = float2(Frame % Columns, Frame / Columns);
    Loc.x = Loc.x / Columns;
    Loc.y = Loc.y / Rows;
    FramePos.x = FramePos.x / Columns;
    FramePos.y = FramePos.y / Rows;
    Loc = Loc + FramePos;

    return texMap.Sample(sampState, Loc);
}