struct VS_INPUT
{
    float4 Pos : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 oPos : COLOR0;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

Texture2D texMap : register(t0);
SamplerState sampState  : register(s0);

cbuffer ObjectData : register(b2)
{
    float4x4 WVP;
    float4x4 World;
};

cbuffer Lighting : register(b1)
{
    float3 globalAmbient;
    float3 lightColor;
    float3 lightPosition;
    float3 eyePosition;
    float3 Ke;
    float3 Ka;
    float3 Kd;
    float3 Ks;
    float shininess;
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT ret;
    ret.oPos = mul(World, input.Pos).xyz;
    ret.Normal = input.Normal;
    ret.Pos = mul(WVP, input.Pos);
    ret.TexCoord = input.TexCoord;
    
    return ret;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    //Compute Emissive Term
    float3 emissive = Ke.x * float3(1,1,1);

    //Compute Ambient Term
    float3 ambient = Ka.x * globalAmbient;

    //Compute Diffuse Term
    float3 L = normalize(lightPosition - input.oPos);
    float diffuseLight = max(dot(input.Normal, L), 0);
    float3 diffuse = Kd.x * lightColor * diffuseLight;

    //compute Specular Term
    float3 V = normalize(eyePosition - input.oPos);
    float3 H = normalize(L + V);
    float specularLight = pow(max(dot(input.Normal, H), 0), 5);
    if (diffuseLight <= 0) specularLight = 0;
    float3 specular = Ks * lightColor * specularLight;
    float4 Color;
    Color.xyz = emissive + ambient + diffuse + specular;
    Color.w = 1;
    return Color * texMap.Sample(sampState, input.TexCoord);
}