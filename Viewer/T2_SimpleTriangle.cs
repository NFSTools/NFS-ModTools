using System.Windows;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WPF;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Viewer
{
	// DirectX SDK: Tutorial 2: Rendering a triangle
	// http://msdn.microsoft.com/en-us/library/ff729719(v=vs.85).aspx
	// DirectX SDK: Tutorial 3: Shaders and Effect System
	// http://msdn.microsoft.com/en-us/library/ff729720(v=VS.85).aspx
	public class T2_SimpleTriangle : D3D11
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			// NOTE: SharpDX 1.3 requires explicit Dispose() of everything
			Set(ref g_pVertexShader, null);
			Set(ref g_pPixelShader, null);
		}

		VertexShader g_pVertexShader;
		PixelShader g_pPixelShader;

		public T2_SimpleTriangle()
		{
			using (var dg = new DisposeGroup())
			{
				// --- init shaders
				ShaderFlags sFlags = ShaderFlags.EnableStrictness;
#if DEBUG
				sFlags |= ShaderFlags.Debug;
#endif
				var pVSBlob = dg.Add(ShaderBytecode.CompileFromFile("T2_SimpleTriangle.fx", "VShader", "vs_4_0", sFlags, EffectFlags.None));
				var inputSignature = dg.Add(ShaderSignature.GetInputSignature(pVSBlob));
				g_pVertexShader = new VertexShader(Device, pVSBlob);

				var pPSBlob = dg.Add(ShaderBytecode.CompileFromFile("T2_SimpleTriangle.fx", "PShader", "ps_4_0", sFlags, EffectFlags.None));
				g_pPixelShader = new PixelShader(Device, pPSBlob);

				// --- let DX know about the pixels memory layout
				var layout = dg.Add(new InputLayout(Device, inputSignature, new[]{
					new InputElement("VERTEX", 0, Format.R32G32B32_Float, 0),
				}));
				Device.ImmediateContext.InputAssembler.InputLayout = layout;

				// --- init vertices
				var vertexBuffer = dg.Add(DXUtils.CreateBuffer(Device, new[] {
					new Vector3(0.0f, 0.5f, 0.5f),
					new Vector3(0.5f, -0.5f, 0.5f),
					new Vector3(-0.5f, -0.5f, 0.5f),
				}));
				Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, 12, 0));
				Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			}
		}

		public override void RenderScene(DrawEventArgs args)
		{
			Device.ImmediateContext.ClearRenderTargetView(this.RenderTargetView, new Color4(1.0f, 1f, 15f, 1f));

			Device.ImmediateContext.VertexShader.Set(g_pVertexShader);
			Device.ImmediateContext.PixelShader.Set(g_pPixelShader);
			Device.ImmediateContext.Draw(3, 0);
		}
	}
}
