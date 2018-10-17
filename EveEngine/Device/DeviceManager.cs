using System;
using EveAgain.Render;
using EveAgain.SceneObjects;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.Device
{
    /// <summary>
    /// Summary description for DeviceManager.
    /// </summary>
    public class DeviceManager : DeviceHost
    {
        public static readonly DeviceManager Manager = new DeviceManager();

        public RenderChain RenderChain { get; set; } = new RenderChain();

        public bool ForceLighting = false;

        private void Clear(IRenderSurface renderSurface)
        {
            Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, renderSurface.ClearColor, 1.0f, 0);
        }

        private void Present(IRenderSurface renderSurface)
        {
            renderSurface.SwapChain.Present();
        }

        private void Activate(IRenderSurface renderSurface)
        {
            if (Device != null)
            {
                // set surfaces
                Device.SetRenderTarget(0, renderSurface.RenderTarget);
                Device.DepthStencilSurface = renderSurface.DepthStencil;

                // set matrices
                Device.Transform.View = renderSurface.Camera.View;
                Device.Transform.Projection = renderSurface.Camera.Projection;
                Device.Transform.World = renderSurface.Camera.World;
            }
        }

        private void RenderObject(ISceneObject obj)
        {
            Matrix matWorld = Device.Transform.World;
            Device.Transform.World = Matrix.Multiply(obj.Transform, matWorld);

            RenderState.FillMode = obj.FillMode;
            RenderState.CullMode = obj.CullMode;
            if (!ForceLighting)
            {
                RenderState.Lighting = obj.Lighting;
            }

            Device.SetTexture(0, obj.Texture);

            Device.Transform.World.Multiply(obj.Transform);

            Device.VertexFormat = obj.FVF;

            Device.DrawIndexedUserPrimitives(obj.PrimitiveType, 0, obj.VertexCount,
                                            obj.PrimitiveCount, obj.Indices, true, obj.Vertices);

            Device.Transform.World = matWorld;

        }

        public void RenderSingle(IRenderSurface renderSurface)
        {
            if (Device != null && renderSurface.SwapChain != null)
            {
                try
                {
                    Device.TestCooperativeLevel();
                }
                catch (DeviceLostException)
                {
                    return;
                }
                catch (DeviceNotResetException)
                {
                    CreatePresentationParameters();
                    Device.Reset(PresentParameters);
                    UpdateViews();
                    return;
                }
            }

            Activate(renderSurface);
            Clear(renderSurface);

            Device.BeginScene();

            for (int i = 0; i < RenderChain.Count; i++)
            {
                RenderChain.RenderObject ro = RenderChain[i];
                if (ro.Enable)
                    RenderObject(ro.Object);
            }

            Device.EndScene();

            Present(renderSurface);
        }

        public EventHandler Render;

        public void UpdateViews()
        {
            if (Render != null)
                Render(null, null);
        }
    }
}

