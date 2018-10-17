using System;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.Device
{
	public class DeviceHost
	{

		public class ViewportException: Exception 
		{
			public ViewportException(string text) : base(text)	{}
		}

		public event EventHandler InitDeviceObjects;
		public event EventHandler InvalidateDeviceObjects;
		public event EventHandler RestoreDeviceObjects;
		public event EventHandler DeleteDeviceObjects;
		public event CancelEventHandler ViewportResized;

	    private Caps _deviceCaps;

	    public DeviceSettings DeviceSettings { get; } = new DeviceSettings();

	    public PresentParameters PresentParameters { get; } = new PresentParameters();

	    public SurfaceDescription BackBuffer { get; private set; }

	    public RenderStates RenderState { get; private set; }

	    public SamplerStates SamplerStates { get; private set; }

	    public TextureStates TextureStates { get; private set; }

	    public Microsoft.DirectX.Direct3D.Device Device { get; private set; }

	    public Control Viewport { get; set; } = null;

	    public bool AutoDeviceReset { get; set; } = true;

	    public void CreatePresentationParameters() 
		{
			PresentParameters pp = PresentParameters;
			pp.Windowed = true;
			pp.SwapEffect = DeviceSettings.SwapEffect;
			pp.EnableAutoDepthStencil = true;
			pp.AutoDepthStencilFormat = DeviceSettings.AutoDepthFormat;
			pp.DeviceWindow = Viewport;
		}

		public void InitD3D()
		{
			if(Viewport == null)
				throw new ViewportException("Viewport is not set correctly.");

			CreatePresentationParameters();
			Device = new Microsoft.DirectX.Direct3D.Device(DeviceSettings.AdapterOrdinal, DeviceSettings.DeviceType,
								Viewport, DeviceSettings.CreateFlags, PresentParameters);

		    if (Device == null) return;
		    
		    // get local cache of data
		    RenderState = Device.RenderState;
		    SamplerStates = Device.SamplerState;
		    TextureStates = Device.TextureState;
		    _deviceCaps = Device.DeviceCaps;
				
		    // get backbuffer desc
		    var backBuffer = Device.GetBackBuffer(0, 0, BackBufferType.Mono);
		    BackBuffer = backBuffer.Description;
		    backBuffer.Dispose();

		    // set events
		    Device.DeviceLost += InvalidateDeviceObjects;
		    Device.DeviceReset += RestoreDeviceObjects;
		    Device.Disposing += DeleteDeviceObjects;
		    Device.DeviceResizing += EnvironmentResized;
				
		    try 
		    {
		        InitDeviceObjects?.Invoke(null,null);
		        RestoreDeviceObjects?.Invoke(null, null);
		    } 
		    catch
		    {
		        InvalidateDeviceObjects?.Invoke(null,null);
		        DeleteDeviceObjects?.Invoke(null,null);
		        Device.Dispose();
		        Device = null;

		    }

		}

		public void ResetDevice()
		{
			Device.Reset(Device.PresentationParameters);
			EnvironmentResized(Device, null);
		}

		public void StopDirect3D()
		{
		    InvalidateDeviceObjects?.Invoke(null,null);
		    DeleteDeviceObjects?.Invoke(null,null);
		    Device.Dispose();
			Device = null;
		}


		public void EnvironmentResized(object sender, CancelEventArgs e)
		{
			if(e != null)
				e.Cancel = !AutoDeviceReset;
		    ViewportResized?.Invoke(sender, e);
		}
	}
}
