using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Barotrauma
{
    public class UltrasoundRenderer
    {
        private const int EchoTextureSize = 128;
        private const float ProbabilityRestartRingPerSecond = 0.5f;
        private readonly GraphicsDevice graphicsDevice;
        private readonly Action renderQuad;
        private Effect effect;
        private RenderTarget2D edgeDetectionRenderTarget;
        private RenderTarget2D smallEdgeDetectionRenderTarget;
        private RenderTarget2D[] echoOcclusionRenderTargets = new RenderTarget2D[2];
        private RenderTarget2D occludedRenderTarget;


        private float ringRadius = 0.0f;
        private Random random = new Random();

        public UltrasoundRenderer(
            Microsoft.Xna.Framework.Content.ContentManager content,
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            Action renderQuad)
        {
            effect = content.Load<Effect>("Effects/ultrasoundshader");
            edgeDetectionRenderTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            smallEdgeDetectionRenderTarget = new RenderTarget2D(graphicsDevice, EchoTextureSize, EchoTextureSize, false, SurfaceFormat.Color, DepthFormat.None);
            echoOcclusionRenderTargets[0] = new RenderTarget2D(graphicsDevice, EchoTextureSize, EchoTextureSize, false, SurfaceFormat.Color, DepthFormat.None);
            echoOcclusionRenderTargets[1] = new RenderTarget2D(graphicsDevice, EchoTextureSize, EchoTextureSize, false, SurfaceFormat.Color, DepthFormat.None);
            occludedRenderTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            this.graphicsDevice = graphicsDevice;
            this.renderQuad = renderQuad;
        }

        public void Render(
            double timeDelta,
            SpriteBatch spriteBatch, 
            Vector2 probePosition, 
            Vector2 texelSize,
            Texture2D inputTexture,
            RenderTarget2D destinationRenderTarget)
        {
            Vector2 relativeProbePosition = new Vector2(probePosition.X / inputTexture.Width, probePosition.Y / inputTexture.Height);

            if (random.NextDouble() < 1.0 - Math.Pow(ProbabilityRestartRingPerSecond, timeDelta))
            {
                ringRadius = 0.0f;
            }
            else
            {
                ringRadius += (float)timeDelta / 2.0f;
            }

            DetectEdges(spriteBatch, relativeProbePosition, texelSize, inputTexture);

            //graphicsDevice.SetRenderTarget(null);
            //spriteBatch.Begin();
            //spriteBatch.Draw(edgeDetectionRenderTarget, new Vector2(0, 0));
            //spriteBatch.End();

            DownsampleEdgeDetection(spriteBatch);

            RenderTarget2D echoOcclusionResult = ComputeEchoOcclusion(spriteBatch, relativeProbePosition, inputTexture);

            ApplyOcclusion(spriteBatch, relativeProbePosition, echoOcclusionResult);


            BlurRadially(spriteBatch, relativeProbePosition, destinationRenderTarget);
        }

        private void DetectEdges(SpriteBatch spriteBatch, Vector2 probePosition, Vector2 texelSize, Texture2D inputTexture)
        {
            graphicsDevice.SetRenderTarget(edgeDetectionRenderTarget);
            spriteBatch.Begin();
            effect.Parameters["mainTexture"].SetValue(inputTexture);
            effect.Parameters["texelSize"].SetValue(texelSize);
            effect.Parameters["probePosition"].SetValue(probePosition);

            effect.Techniques["EdgeDetection"].Passes[0].Apply();

            renderQuad();

            spriteBatch.End();
        }

        private void DownsampleEdgeDetection(SpriteBatch spriteBatch)
        {
            graphicsDevice.SetRenderTarget(smallEdgeDetectionRenderTarget);
            spriteBatch.Begin();
            spriteBatch.Draw(edgeDetectionRenderTarget, new Rectangle(0, 0, smallEdgeDetectionRenderTarget.Width, smallEdgeDetectionRenderTarget.Height), Color.White);
            spriteBatch.End();
        }

        private RenderTarget2D ComputeEchoOcclusion(SpriteBatch spriteBatch, Vector2 probePosition, Texture2D inputTexture)
        {
            int numberOfEchoPasses = (int)Math.Ceiling(new Vector2(inputTexture.Width, inputTexture.Height).Length() / 8);
            for (int i = 0; i < numberOfEchoPasses; i++)
            {
                graphicsDevice.SetRenderTarget(echoOcclusionRenderTargets[(i + 1) % 2]);
                spriteBatch.Begin();
                effect.Parameters["mainTexture"].SetValue(smallEdgeDetectionRenderTarget);
                effect.Parameters["texelSize"].SetValue(new Vector2(1.0f / EchoTextureSize, 1.0f / EchoTextureSize));
                effect.Parameters["echoOcclusionTexture"].SetValue(echoOcclusionRenderTargets[i % 2]);
                effect.Parameters["probePosition"].SetValue(probePosition);
                effect.Parameters["echoPass"].SetValue(i);

                effect.Techniques["EchoOcclusion"].Passes[0].Apply();

                renderQuad();

                spriteBatch.End();
            }
            RenderTarget2D echoOcclusionResult = echoOcclusionRenderTargets[numberOfEchoPasses % 2];
            return echoOcclusionResult;
        }

        private void ApplyOcclusion(SpriteBatch spriteBatch, Vector2 probePosition, RenderTarget2D echoOcclusionResult)
        {
            graphicsDevice.SetRenderTarget(occludedRenderTarget);
            spriteBatch.Begin();
            effect.Parameters["mainTexture"].SetValue(edgeDetectionRenderTarget);
            effect.Parameters["probePosition"].SetValue(probePosition);
            effect.Parameters["echoOcclusionTexture"].SetValue(echoOcclusionResult);
            effect.Parameters["occlusionRingRadius"].SetValue(ringRadius);

            effect.Techniques["ApplyOcclusion"].Passes[0].Apply();

            renderQuad();
            spriteBatch.End();
        }

        private void BlurRadially(SpriteBatch spriteBatch, Vector2 probePosition, RenderTarget2D destinationRenderTarget)
        {
            graphicsDevice.SetRenderTarget(destinationRenderTarget);
            spriteBatch.Begin();
            effect.Parameters["mainTexture"].SetValue(occludedRenderTarget);
            effect.Parameters["probePosition"].SetValue(probePosition);
            effect.Parameters["texelSize"].SetValue(new Vector2(1.0f / EchoTextureSize, 1.0f / EchoTextureSize));

            effect.Techniques["RadialBlur"].Passes[0].Apply();

            renderQuad();
            spriteBatch.End();
        }
    }
}
