using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BarotraumaLarp.Shader
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D texture;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private RenderTarget2D renderTargetFinal;
        private UltrasoundRenderer ultrasoundRenderer;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            Window.IsBorderless = true;
            int width = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int height = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();

            vertexBuffer = new VertexBuffer(_graphics.GraphicsDevice, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly);
            indexBuffer = new IndexBuffer(_graphics.GraphicsDevice, IndexElementSize.SixteenBits, 4, BufferUsage.WriteOnly);

            VertexPositionTexture[] vertices =
            {
                new VertexPositionTexture(new Vector3(-1f, -1f, 1f), new Vector2(0f, 1f)),
                new VertexPositionTexture(new Vector3(-1f, 1f, 1f), new Vector2(0f, 0f)),
                new VertexPositionTexture(new Vector3(1f, -1f, 1f), new Vector2(1f, 1f)),
                new VertexPositionTexture(new Vector3(1f, 1f, 1f), new Vector2(1f, 0f))
            };
            vertexBuffer.SetData(vertices);
            indexBuffer.SetData(new ushort[] { 0, 1, 2, 3 });

            renderTargetFinal = new RenderTarget2D(_graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);

            base.Initialize();
            Window.Position = new Point(0, 0);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            texture = Content.Load<Texture2D>("debugscreen0"); 
            //texture = Content.Load<Texture2D>("debugscreen1");


            ultrasoundRenderer = new UltrasoundRenderer(
                Content, 
                _graphics.GraphicsDevice, 

                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height,
                RenderQuad);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        private void RenderQuad()
        {
            _graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
            _graphics.GraphicsDevice.Indices = indexBuffer;
            _graphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 2);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);

            GraphicsDevice.SetRenderTarget(renderTargetFinal);
            spriteBatch.Begin();
            spriteBatch.Draw(texture, new Rectangle(0, 0, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height), Color.White);
            spriteBatch.End();

            var mouseState = Mouse.GetState(Window);

            ultrasoundRenderer.Render(
                gameTime.ElapsedGameTime.TotalSeconds,
                spriteBatch,
                new Vector2(mouseState.X, mouseState.Y),
                new Vector2(1.0f / Window.ClientBounds.Width, 1.0f / Window.ClientBounds.Height),
                renderTargetFinal,
                renderTargetFinal);

            GraphicsDevice.SetRenderTarget(null);
            spriteBatch.Begin();
            spriteBatch.Draw(renderTargetFinal, new Rectangle(0, 0, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
