﻿using SPB.Graphics;
using SPB.Graphics.Exceptions;
using SPB.Graphics.OpenGL;
using SPB.Windowing;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SPB.SPB.Platform.WGL
{
    public class WGLOpenGLContext : OpenGLContextBase
    {
        private IntPtr _windowHandle;
        private IntPtr _deviceContext;

        private NativeWindowBase _window;

        public WGLOpenGLContext(FramebufferFormat framebufferFormat, int major, int minor, OpenGLContextFlags flags = OpenGLContextFlags.Default, bool directRendering = true, WGLOpenGLContext shareContext = null) : base(framebufferFormat, major, minor, flags, directRendering, shareContext)
        {
            _deviceContext = IntPtr.Zero;
            _window = null;
        }

        public override bool IsCurrent => WGL.GetCurrentContext() == ContextHandle;

        public override IntPtr GetProcAddress(string procName)
        {
            return WGLHelper.GetProcAddress(procName);
        }

        public override void Initialize(NativeWindowBase window = null)
        {
            IntPtr windowHandle = IntPtr.Zero;

            if (window != null)
            {
                windowHandle = window.WindowHandle.RawHandle;
            }

            IntPtr sharedContextHandle = IntPtr.Zero;

            if (ShareContext != null)
            {
                sharedContextHandle = ShareContext.ContextHandle;
            }

            IntPtr context = WGLHelper.CreateContext(ref windowHandle, FramebufferFormat, Major, Minor, Flags, DirectRendering, sharedContextHandle);

            ContextHandle = context;

            if (ContextHandle != IntPtr.Zero)
            {
                _deviceContext = Win32.Win32.GetDC(windowHandle);

                // If there is no window provided, keep the temporary window around to free it later.
                if (window == null)
                {
                    _windowHandle = windowHandle;
                }
            }

            if (ContextHandle == IntPtr.Zero)
            {
                throw new ContextException("CreateContext() failed.");
            }
        }

        public override void MakeCurrent(NativeWindowBase window)
        {
            if (_window != null && window != null && _window.WindowHandle.RawHandle == window.WindowHandle.RawHandle && IsCurrent)
            {
                return;
            }

            bool success;

            if (window != null)
            {
                if (!(window is WGLWindow))
                {
                    throw new InvalidOperationException($"MakeCurrent() should be used with a {typeof(WGLWindow).Name}.");
                }
                if (_deviceContext != window.DisplayHandle.RawHandle)
                {
                    throw new InvalidOperationException("MakeCurrent() should be used with a window originated from the same device context.");
                }

                success = WGL.MakeCurrent(_deviceContext, ContextHandle);
            }
            else
            {
                success = WGL.MakeCurrent(IntPtr.Zero, IntPtr.Zero);
            }

            if (success)
            {
                _window = window;
            }
            else
            {
                throw new ContextException("MakeCurrent() failed.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    MakeCurrent(null);

                    if (_windowHandle != IntPtr.Zero)
                    {
                        Win32.Win32.DestroyWindow(_windowHandle);
                    }

                    WGL.DeleteContext(ContextHandle);
                }

                IsDisposed = true;
            }
        }
    }
}