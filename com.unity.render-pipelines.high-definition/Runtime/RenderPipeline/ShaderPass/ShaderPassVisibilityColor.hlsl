#if (SHADERPASS != SHADERPASS_VISIBILITY_COLOR)
#error SHADERPASS_is_not_correctly_define
#endif

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

    output.positionCS = pos;
    output.texcoord   = uv;
    return output;
}

struct PerRendererData
{
    float4x4 localToWorld;
    uint indexOffset;
};

struct FBarycentrics
{
    float3 UVW;
    float3 UVW_dx;
    float3 UVW_dy;
};

StructuredBuffer<uint> _GlobalIndexBuffer;
StructuredBuffer<float3> _GlobalVertexBuffer;
StructuredBuffer<float3> _GlobalNormalBuffer;
StructuredBuffer<float4> _GlobalTangentBuffer;
StructuredBuffer<float2> _GlobalUV0Buffer;
StructuredBuffer<PerRendererData> _PerInstanceData;

TEXTURE2D_X(_VisibilityBuffer);
TEXTURE2D_X(_MaterialDepth);

FBarycentrics CalculateTriangleBarycentrics(float2 PixelClip, float4 PointClip0, float4 PointClip1, float4 PointClip2)
{
    FBarycentrics Result;

    float3 Pos0 = PointClip0.xyz / PointClip0.w;
    float3 Pos1 = PointClip1.xyz / PointClip1.w;
    float3 Pos2 = PointClip2.xyz / PointClip2.w;

    float3 RcpW = rcp(float3(PointClip0.w, PointClip1.w, PointClip2.w));

    float3 Pos120X = float3(Pos1.x, Pos2.x, Pos0.x);
    float3 Pos120Y = float3(Pos1.y, Pos2.y, Pos0.y);
    float3 Pos201X = float3(Pos2.x, Pos0.x, Pos1.x);
    float3 Pos201Y = float3(Pos2.y, Pos0.y, Pos1.y);

    float3 C_dx = Pos201Y - Pos120Y;
    float3 C_dy = Pos120X - Pos201X;

    float3 C = C_dx * (PixelClip.x - Pos120X) + C_dy * (PixelClip.y - Pos120Y); // Evaluate the 3 edge functions
    float3 G = C * RcpW;

    float H = dot(C, RcpW);
    float RcpH = rcp(H);

    // UVW = C * RcpW / dot(C, RcpW)
    Result.UVW = G * RcpH;

    // Texture coordinate derivatives:
    // UVW = G / H where G = C * RcpW and H = dot(C, RcpW)
    // UVW' = (G' * H - G * H') / H^2
    // float2 TexCoordDX = UVW_dx.y * TexCoord10 + UVW_dx.z * TexCoord20;
    // float2 TexCoordDY = UVW_dy.y * TexCoord10 + UVW_dy.z * TexCoord20;
    float3 G_dx = C_dx * RcpW;
    float3 G_dy = C_dy * RcpW;

    float H_dx = dot(C_dx, RcpW);
    float H_dy = dot(C_dy, RcpW);

    Result.UVW_dx = (G_dx * H - G * H_dx) * (RcpH * RcpH) *  2.0f;
    Result.UVW_dy = (G_dy * H - G * H_dy) * (RcpH * RcpH) * -2.0f;

    return Result;
}

#define INTERPOLATE(valA, valB, valC, Barycentrics) (valA + Barycentrics.UVW.y * (valB - valA) + Barycentrics.UVW.z * (valC - valA))
#define INTERPOLATE_BUFFER(Buf, indices, Barycentrics) INTERPOLATE(Buf[indices.x], Buf[indices.y], Buf[indices.z], Barycentrics);

FragInputs UnpackVaryingsToFragInputs(Varyings packedInput)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    float2 positionNDC = packedInput.texcoord.xy;
    uint2 positionSS = positionNDC * _ScreenSize.xy;

    const float2 packedVisibility = _VisibilityBuffer[COORD_TEXTURE2D_X(positionSS)];
    const uint rendererId = asuint(packedVisibility.x) >> 16;
    const uint primitiveId = asuint(packedVisibility.x) & 0xFFFF; // * 3 -> 3 vert per tri
    const float depth = asuint(packedVisibility.y) / (float)UINT_MAX;

    const uint index = primitiveId * 3 + _PerInstanceData[rendererId].indexOffset;
    const uint3 indices = uint3(_GlobalIndexBuffer[index], _GlobalIndexBuffer[index+1], _GlobalIndexBuffer[index+2]);

    const float3 triA = _GlobalVertexBuffer[indices.x];
    const float3 triB = _GlobalVertexBuffer[indices.y];
    const float3 triC = _GlobalVertexBuffer[indices.z];

    float4x4 localToWorld = _PerInstanceData[rendererId].localToWorld;
    float4x4 localToClip = UNITY_MATRIX_VP * localToWorld;
    const float4 PointClipA = mul(UNITY_MATRIX_VP, mul(localToWorld, float4(triA, 1.0)));
    const float4 PointClipB = mul(UNITY_MATRIX_VP, mul(localToWorld, float4(triB, 1.0)));
    const float4 PointClipC = mul(UNITY_MATRIX_VP, mul(localToWorld, float4(triC, 1.0)));

    const float2 PixelClip = positionNDC * float2(2, -2) + float2(-1, 1);
    const FBarycentrics Barycentrics = CalculateTriangleBarycentrics(PixelClip, PointClipA, PointClipB, PointClipC);

    output.tangentToWorld = k_identity3x3;
    output.positionSS = float4(positionSS, depth, 0.0);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 positionOS = INTERPOLATE(triA, triB, triC, Barycentrics);
    output.positionRWS.xyz = mul(localToWorld, float4(positionOS, 1.0)).xyz;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    float4 tangentOS = INTERPOLATE_BUFFER(_GlobalTangentBuffer, indices, Barycentrics);
    float3 normalOS = INTERPOLATE_BUFFER(_GlobalNormalBuffer, indices, Barycentrics);
    float4 tangentWS = float4(TransformObjectToWorldDir(tangentOS.xyz), tangentOS.w);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    output.tangentToWorld = BuildTangentToWorld(tangentWS, normalWS);
#endif // VARYINGS_NEED_TANGENT_TO_WORLD

#ifdef VARYINGS_NEED_TEXCOORD0
    output.texCoord0.xy = INTERPOLATE_BUFFER(_GlobalUV0Buffer, indices, Barycentrics);
#endif
/*
#ifdef VARYINGS_NEED_TEXCOORD1
    output.texCoord1.xy = input.interpolators3.zw;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.texCoord2.xy = input.interpolators4.xy;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.texCoord3.xy = input.interpolators4.zw;
#endif
#ifdef VARYINGS_NEED_COLOR
    output.color = input.interpolators5;
#endif
*/

#if (defined(VARYINGS_NEED_PRIMITIVEID))
    output.primitiveID = primitiveId;
#endif
#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    //output.isFrontFace = TODO
#endif

    return output;
}

void Frag(Varyings packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    float materialId = _MaterialDepth[COORD_TEXTURE2D_X(input.positionSS.xy)].r;
    if (materialId != _MaterialId)
        discard;


    outColor = float4(0.0, 0.0, 0.0, 0.0);

    uint2 tileIndex = uint2(input.positionSS.xy) / GetTileSize();

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, tileIndex);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

#ifdef DEBUG_DISPLAY
    bool viewMaterial = false;
    int bufferSize = _DebugViewMaterialArray[0].x;
    if (bufferSize != 0)
    {
        bool needLinearToSRGB = false;
        float3 result = float3(1.0, 0.0, 1.0);

        // Loop through the whole buffer
        // Works because GetSurfaceDataDebug will do nothing if the index is not a known one
        for (int index = 1; index <= bufferSize; index++)
        {
            int indexMaterialProperty = _DebugViewMaterialArray[index].x;

            // skip if not really in use
            if (indexMaterialProperty != 0)
            {
                viewMaterial = true;

                GetPropertiesDataDebug(indexMaterialProperty, result, needLinearToSRGB);
                GetVaryingsDataDebug(indexMaterialProperty, input, result, needLinearToSRGB);
                GetBuiltinDataDebug(indexMaterialProperty, builtinData, posInput, result, needLinearToSRGB);
                GetSurfaceDataDebug(indexMaterialProperty, surfaceData, result, needLinearToSRGB);
                GetBSDFDataDebug(indexMaterialProperty, bsdfData, result, needLinearToSRGB);
            }
        }

        // TEMP!
        // For now, the final blit in the backbuffer performs an sRGB write
        // So in the meantime we apply the inverse transform to linear data to compensate, unless we output to AOVs.
        if (!needLinearToSRGB && _DebugAOVOutput == 0)
            result = SRGBToLinear(max(0, result));

        outColor = float4(result, 1.0);
    }

    if (!viewMaterial)
    {
        if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR || _DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
        {
            float3 result = float3(0.0, 0.0, 0.0);

            GetPBRValidatorDebug(surfaceData, result);

            outColor = float4(result, 1.0f);
        }
        else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
        {
            float4 result = _DebugTransparencyOverdrawWeight * float4(TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_A);
            outColor = result;
        }
        else
#endif
        {
            uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;

            LightLoopOutput lightLoopOutput;
            LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

            // Alias
            float3 diffuseLighting = lightLoopOutput.diffuseLighting;
            float3 specularLighting = lightLoopOutput.specularLighting;

            diffuseLighting *= GetCurrentExposureMultiplier();
            specularLighting *= GetCurrentExposureMultiplier();

#ifdef OUTPUT_SPLIT_LIGHTING
            outColor = float4(diffuseLighting + specularLighting, 1.0);
#else
            outColor = ApplyBlendMode(diffuseLighting, specularLighting, builtinData.opacity);
            outColor = EvaluateAtmosphericScattering(posInput, V, outColor);
#endif
        }

#ifdef DEBUG_DISPLAY
    }
#endif
}
