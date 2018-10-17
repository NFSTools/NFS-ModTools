using System;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.Device
{
	/// <summary>
	/// Summary description for DeviceSettings.
	/// </summary>
	public class DeviceSettings
	{
		public CreateFlags CreateFlags = CreateFlags.PureDevice | CreateFlags.HardwareVertexProcessing;
		public DeviceType DeviceType = DeviceType.Hardware;
		
		public SwapEffect SwapEffect = SwapEffect.Discard;
		public DepthFormat AutoDepthFormat = DepthFormat.D24X8;
		public Int32 AdapterOrdinal = 0;
	}
}
