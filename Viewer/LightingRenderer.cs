using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WPF;

namespace Viewer
{
    // Tutorial 6: Lighting
    // http://msdn.microsoft.com/en-us/library/ff729723(v=VS.85).aspx
    public class LightingRenderer : D3D11
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VectorNormal
        {
            public VectorNormal(Vector3 p, Vector3 n)
            {
                Point = p;
                Normal = n;
            }
            public Vector3 Point;
            public Vector3 Normal;

            public const int SizeInBytes = (3 + 3) * 4;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Projections
        {
            public Matrix World;
            public Matrix View;
            public Matrix Projection;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Vector4[] LightDirection;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Color4[] LightColor;
            public Color4 OutputColor;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // NOTE: SharpDX 1.3 requires explicit dispose of everything
            Set(ref g_pPixelShader, null);
            Set(ref g_pPixelShaderSolid, null);
            Set(ref g_pVertexShader, null);
            Set(ref g_pConstantBuffer, null);
            Set(ref depthStencil, null);
            Set(ref depthStencilView, null);
        }

        PixelShader g_pPixelShader, g_pPixelShaderSolid;
        VertexShader g_pVertexShader;

        ConstantBuffer<Projections> g_pConstantBuffer;

        public LightingRenderer() : base()
        {
            using (var dg = new DisposeGroup())
            {
                // --- init shaders
                ShaderFlags sFlags = ShaderFlags.EnableStrictness;
#if DEBUG
                sFlags |= ShaderFlags.Debug;
#endif

                var pVSBlob = dg.Add(ShaderBytecode.CompileFromFile("T6_Lighting.fx", "VS", "vs_4_0", sFlags, EffectFlags.None));
                var inputSignature = dg.Add(ShaderSignature.GetInputSignature(pVSBlob));
                Set(ref g_pVertexShader, new VertexShader(Device, pVSBlob));

                var g_pVertexLayout = dg.Add(new InputLayout(Device, inputSignature, new[]{
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                    new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                }));

                Device.ImmediateContext.InputAssembler.InputLayout = g_pVertexLayout;
                //Device.ImmediateContext.InputAssembler.SetInputLayout(g_pVertexLayout);

                var pPSBlob = dg.Add(ShaderBytecode.CompileFromFile("T6_Lighting.fx", "PS", "ps_4_0", sFlags, EffectFlags.None));
                Set(ref g_pPixelShader, new PixelShader(Device, pPSBlob));

                pPSBlob = dg.Add(ShaderBytecode.CompileFromFile("T6_Lighting.fx", "PSSolid", "ps_4_0", sFlags, EffectFlags.None));
                Set(ref g_pPixelShaderSolid, new PixelShader(Device, pPSBlob));

                // --- init vertices
                var vertexBuffer = dg.Add(DXUtils.CreateBuffer(Device, new VectorNormal[]{
                    new VectorNormal(new Vector3( -1.0f, 1.0f, -1.0f ),  new Vector3( 0.0f, 1.0f, 0.0f ) ),
                    new VectorNormal(new Vector3( 1.0f, 1.0f, -1.0f ),   new Vector3( 0.0f, 1.0f, 0.0f ) ),
                    new VectorNormal(new Vector3( 1.0f, 1.0f, 1.0f ),    new Vector3( 0.0f, 1.0f, 0.0f ) ),
                    new VectorNormal(new Vector3( -1.0f, 1.0f, 1.0f ),   new Vector3( 0.0f, 1.0f, 0.0f ) ),

                    new VectorNormal(new Vector3( -1.0f, -1.0f, -1.0f ), new Vector3( 0.0f, -1.0f, 0.0f )),
                    new VectorNormal(new Vector3( 1.0f, -1.0f, -1.0f ),  new Vector3( 0.0f, -1.0f, 0.0f )),
                    new VectorNormal(new Vector3( 1.0f, -1.0f, 1.0f ),   new Vector3( 0.0f, -1.0f, 0.0f )),
                    new VectorNormal(new Vector3( -1.0f, -1.0f, 1.0f ),  new Vector3( 0.0f, -1.0f, 0.0f )),

                    new VectorNormal(new Vector3( -1.0f, -1.0f, 1.0f ),  new Vector3( -1.0f, 0.0f, 0.0f )),
                    new VectorNormal(new Vector3( -1.0f, -1.0f, -1.0f ), new Vector3( -1.0f, 0.0f, 0.0f )),
                    new VectorNormal(new Vector3( -1.0f, 1.0f, -1.0f ),  new Vector3( -1.0f, 0.0f, 0.0f )),
                    new VectorNormal(new Vector3( -1.0f, 1.0f, 1.0f ),   new Vector3( -1.0f, 0.0f, 0.0f )),

                    new VectorNormal(new Vector3( 1.0f, -1.0f, 1.0f ),   new Vector3( 1.0f, 0.0f, 0.0f ) ),
                    new VectorNormal(new Vector3( 1.0f, -1.0f, -1.0f ),  new Vector3( 1.0f, 0.0f, 0.0f ) ),
                    new VectorNormal(new Vector3( 1.0f, 1.0f, -1.0f ),   new Vector3( 1.0f, 0.0f, 0.0f ) ),
                    new VectorNormal(new Vector3( 1.0f, 1.0f, 1.0f ),    new Vector3( 1.0f, 0.0f, 0.0f ) ),

                    new VectorNormal(new Vector3( -1.0f, -1.0f, -1.0f ), new Vector3( 0.0f, 0.0f, -1.0f )),
                    new VectorNormal(new Vector3( 1.0f, -1.0f, -1.0f ),  new Vector3( 0.0f, 0.0f, -1.0f )),
                    new VectorNormal(new Vector3( 1.0f, 1.0f, -1.0f ),   new Vector3( 0.0f, 0.0f, -1.0f )),
                    new VectorNormal(new Vector3( -1.0f, 1.0f, -1.0f ),  new Vector3( 0.0f, 0.0f, -1.0f )),

                    new VectorNormal(new Vector3( -1.0f, -1.0f, 1.0f ),  new Vector3( 0.0f, 0.0f, 1.0f ) ),
                    new VectorNormal(new Vector3( 1.0f, -1.0f, 1.0f ),   new Vector3( 0.0f, 0.0f, 1.0f ) ),
                    new VectorNormal(new Vector3( 1.0f, 1.0f, 1.0f ),    new Vector3( 0.0f, 0.0f, 1.0f ) ),
                    new VectorNormal(new Vector3( -1.0f, 1.0f, 1.0f ),   new Vector3( 0.0f, 0.0f, 1.0f ) ),
                }));
                Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VectorNormal.SizeInBytes, 0));

                // --- init indices
                var indicesBuffer = dg.Add(DXUtils.CreateBuffer(Device, new ushort[] {
                    3,1,0,
                    2,1,3,

                    6,4,5,
                    7,4,6,

                    11,9,8,
                    10,9,11,

                    14,12,13,
                    15,12,14,

                    19,17,16,
                    18,17,19,

                    22,20,21,
                    23,20,22
                }));
                Device.ImmediateContext.InputAssembler.SetIndexBuffer(indicesBuffer, Format.R16_UInt, 0);

                Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                //Device.ImmediateContext.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

                // --- create the constant buffer
                Set(ref g_pConstantBuffer, new ConstantBuffer<Projections>(Device));
            }

            CurrentCamera = new FirstPersonCamera();
            CurrentCamera.SetProjParams((float)Math.PI / 4, 1, 0.01f, 100.0f);
            CurrentCamera.SetViewParams(new Vector3(0.0f, 4.0f, -10.0f), new Vector3(0.0f, 1.0f, 0.0f));
        }

        public override void RenderScene(DrawEventArgs args)
        {
            float t = (float)args.TotalTime.TotalSeconds;

            var g_World = Matrix.RotationY(t);

            var vLightDirs = new Vector4[]{
                new Vector4(-0.577f, 0.577f, -0.577f, 1.0f),
                new Vector4(0.0f, 0.0f, -1.0f, 1.0f),
            };
            var vLightColors = new Color4[] {
                new Color4(1.0f, 0.5f, 0.5f, 0.5f),
                new Color4(1.0f, 0.5f, 0.0f, 0.0f),
            };

            var mRrotate = Matrix.RotationY(-2 * t);
            vLightDirs[1] = Vector4.Transform(vLightDirs[1], mRrotate);

            Device.ImmediateContext.ClearRenderTargetView(this.RenderTargetView, new Color4(1.0f, 0.3f, 0.525f, 0.8f));
            Device.ImmediateContext.ClearDepthStencilView(this.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            //
            // Update matrix variables and lighting variables
            //
            var cb1 = new Projections
            {
                World = Matrix.Transpose(g_World),
                View = Matrix.Transpose(CurrentCamera.View),
                Projection = Matrix.Transpose(CurrentCamera.Projection),
                LightDirection = vLightDirs,
                LightColor = vLightColors,
                OutputColor = new Color4(),
            };
            g_pConstantBuffer.Value = cb1;

            //
            // Render the cube
            //
            Device.ImmediateContext.VertexShader.Set(g_pVertexShader);
            Device.ImmediateContext.PixelShader.Set(g_pPixelShader);
            Device.ImmediateContext.VertexShader.SetConstantBuffer(0, g_pConstantBuffer.Buffer);
            Device.ImmediateContext.PixelShader.SetConstantBuffer(0, g_pConstantBuffer.Buffer);
            Device.ImmediateContext.DrawIndexed(36, 0, 0);

            //
            // Render each light
            //
            for (int m = 0; m < 2; m++)
            {
                var mLight = Matrix.Translation(vLightDirs[m].X * 5, vLightDirs[m].Y * 5, vLightDirs[m].Z * 5);
                var mLightScale = Matrix.Scaling(0.2f);
                mLight = mLightScale * mLight;

                // Update the world variable to reflect the current light
                cb1.World = Matrix.Transpose(mLight);
                cb1.OutputColor = vLightColors[m];

                g_pConstantBuffer.Value = cb1;
                Device.ImmediateContext.PixelShader.Set(g_pPixelShaderSolid);
                Device.ImmediateContext.DrawIndexed(36, 0, 0);
            }
        }
    }
}
