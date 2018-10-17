using System.Drawing;
using System.Windows.Forms;
using EveAgain.Cameras;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.Render
{
	public interface IRenderSurface
	{
		SwapChain SwapChain { get; }
		Camera Camera { get; set; }
		Color ClearColor { get; set; }
		string Description { get; }
		int ClientWidth { get; }
		int ClientHeight { get; }
		Surface DepthStencil { get; }
		Surface RenderTarget { get; }
		Control Viewport { get; }
	}
}
