static const float PI = 3.14159265f;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinates: TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
}; 

float2 probePosition;
int echoPass;
float2 texelSize;
float occlusionRingRadius;

Texture2D mainTexture;
sampler s0;

sampler2D MainTextureSampler = sampler_state
{
	Texture = <mainTexture>;
};

Texture2D echoOcclusionTexture;
sampler2D EchoOcclusionTextureSampler = sampler_state
{
	Texture = <echoOcclusionTexture>;
};

VertexShaderOutput IdentityVertexShader(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    output.Position = input.Position;
    output.TextureCoordinates = input.TextureCoordinates;
    
    return output;
}

float getGrayscale(float4 color)
{
    return (color.r + color.g + color.b) / 3;
}

float4 EdgeDetectionPixelShader(VertexShaderOutput input) : COLOR
{
    float2 normalizedToSource = normalize(probePosition - input.TextureCoordinates);
    float2 upstreamTextureCoordinates = input.TextureCoordinates + float2(normalizedToSource.x * texelSize.x, normalizedToSource.y * texelSize.y);

    float local = getGrayscale(tex2D(MainTextureSampler, input.TextureCoordinates));
    float upstream = getGrayscale(tex2D(MainTextureSampler, upstreamTextureCoordinates));

    float d = abs(local - upstream) * 10;
    return float4(d, d, d, 1);
}

float4 EchoOcclusionPixelShader(VertexShaderOutput input) : COLOR
{
    float output = echoPass == 0 ? 0.0f : tex2D(EchoOcclusionTextureSampler, input.TextureCoordinates).r;
    float2 toSource = probePosition - input.TextureCoordinates;
    float2 normalizedToSource = normalize(toSource);
    float2 texelToSource = float2(normalizedToSource.x * texelSize.x, normalizedToSource.y * texelSize.y);

    int numberOfIterations = 8;
    int factor = echoPass * numberOfIterations;
    int maxNonOvershootingFactor = (int)/*sqrt*/((length(toSource) - 0.1f) / length(texelToSource));
    float2 currentTextureCoordinates = input.TextureCoordinates + texelToSource * factor;
    for (int i = 0; i < numberOfIterations; i++) {
        float edgeDetectionValue = tex2D(MainTextureSampler, currentTextureCoordinates).r / 5.0f;
        output += factor < maxNonOvershootingFactor ? edgeDetectionValue : 0.0f;
        currentTextureCoordinates += texelToSource;
        factor += 1;
    }
    
    return float4(output, output, output, 1);
}

float4 ApplyOcclusionPixelShader(VertexShaderOutput input) : COLOR
{
    float edgeDetection = tex2D(MainTextureSampler, input.TextureCoordinates).r;
    float echoOcclusion = tex2D(EchoOcclusionTextureSampler, input.TextureCoordinates).r;
    
    float distanceToOcclusionRing = abs(length(probePosition - input.TextureCoordinates) - occlusionRingRadius);
    
    float ringThickness = 0.02f;
    float ringOcclusion = 1.0f - max(0, (ringThickness - distanceToOcclusionRing) * 10);
    
    float result = edgeDetection * (1.0f - echoOcclusion) * ringOcclusion;
    return float4(result, result, result, 1);
}

float4 RadialBlurPixelShader(VertexShaderOutput input) : COLOR
{
    float4 output = float4(0, 0, 0, 1);

    float2 toSource = probePosition - input.TextureCoordinates;
    float2 normal = float2(-toSource.y * texelSize.x, toSource.x * texelSize.y);

    float blurSamples = 9;
    float sampleDistanceInTexel = 0.25f;
    float sigmaInTexel = 3.0f;
    float a = 1 / sqrt(2 * PI * sigmaInTexel);
    
    float totalWeights = 0.0f;
    for(int i = -blurSamples/2; i <= blurSamples/2; i++)
    {   
        float offsetOfCurrentSampleInTexel = i*sampleDistanceInTexel;
        float x = offsetOfCurrentSampleInTexel / sigmaInTexel;
        float weight = a * exp(-(x*x)/2);
        totalWeights += weight;
        output += weight * tex2D(MainTextureSampler, input.TextureCoordinates + normal * offsetOfCurrentSampleInTexel);
    }
    
    return output / totalWeights;
}

technique EdgeDetection
{
	pass Pass0
	{
        VertexShader = compile vs_4_0_level_9_1 IdentityVertexShader();
		PixelShader = compile ps_4_0_level_9_1 EdgeDetectionPixelShader();
	}
};

technique EchoOcclusion
{
    pass Pass0
    {
        VertexShader = compile vs_4_0_level_9_1 IdentityVertexShader();
		PixelShader = compile ps_4_0_level_9_1 EchoOcclusionPixelShader();
    }
};

technique ApplyOcclusion
{
    pass Pass0
    {
        VertexShader = compile vs_4_0_level_9_1 IdentityVertexShader();
		PixelShader = compile ps_4_0_level_9_1 ApplyOcclusionPixelShader();
    }
};

technique RadialBlur
{
    pass Pass0
    {
        VertexShader = compile vs_4_0_level_9_1 IdentityVertexShader();
		PixelShader = compile ps_4_0_level_9_1 RadialBlurPixelShader();
    }
};