/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace Standard
{
	internal static class DpiHelper
	{
		private static Matrix _transformToDevice;
		private static Matrix _transformToDip;

		[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
		static DpiHelper()
		{
			using (var desktop = SafeDC.GetDesktop())
			{
				// Can get these in the static constructor.  They shouldn't vary window to window,
				// and changing the system DPI requires a restart.
				var pixelsPerInchX = NativeMethods.GetDeviceCaps(desktop, DeviceCap.LOGPIXELSX);
				var pixelsPerInchY = NativeMethods.GetDeviceCaps(desktop, DeviceCap.LOGPIXELSY);
				_transformToDip = Matrix.Identity;
				_transformToDip.Scale(96d / pixelsPerInchX, 96d / pixelsPerInchY);
				_transformToDevice = Matrix.Identity;
				_transformToDevice.Scale(pixelsPerInchX / 96d, pixelsPerInchY / 96d);
			}
		}

		/// <summary>
		/// Convert a point in device independent pixels (1/96") to a point in the system coordinates.
		/// </summary>
		/// <param name="logicalPoint">A point in the logical coordinate system.</param>
		/// <returns>Returns the parameter converted to the system's coordinates.</returns>
		public static Point LogicalPixelsToDevice(Point logicalPoint, IntPtr hwnd)
		{
			var (pixelsPerInchX, pixelsPerInchY) = GetDpiFromWindow(hwnd);

			_transformToDevice = Matrix.Identity;
			_transformToDevice.Scale(pixelsPerInchX / 96d, pixelsPerInchY / 96d);
			return _transformToDevice.Transform(logicalPoint);
		}

		/// <summary>
		/// Convert a point in system coordinates to a point in device independent pixels (1/96").
		/// </summary>
		/// <param name="logicalPoint">A point in the physical coordinate system.</param>
		/// <returns>Returns the parameter converted to the device independent coordinate system.</returns>
		public static Point DevicePixelsToLogical(Point devicePoint, IntPtr hwnd)
		{
			var (pixelsPerInchX, pixelsPerInchY) = GetDpiFromWindow(hwnd);

			_transformToDip = Matrix.Identity;
			_transformToDip.Scale(96d / pixelsPerInchX, 96d / pixelsPerInchY);

			return _transformToDip.Transform(devicePoint);
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static Rect LogicalRectToDevice(Rect logicalRectangle, IntPtr hwnd)
		{
			var topLeft = LogicalPixelsToDevice(new Point(logicalRectangle.Left, logicalRectangle.Top), hwnd);
			var bottomRight = LogicalPixelsToDevice(new Point(logicalRectangle.Right, logicalRectangle.Bottom), hwnd);
			return new Rect(topLeft, bottomRight);
		}

		public static Rect DeviceRectToLogical(Rect deviceRectangle, IntPtr hwnd)
		{
			var topLeft = DevicePixelsToLogical(new Point(deviceRectangle.Left, deviceRectangle.Top), hwnd);
			var bottomRight = DevicePixelsToLogical(new Point(deviceRectangle.Right, deviceRectangle.Bottom), hwnd);
			return new Rect(topLeft, bottomRight);
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static Size LogicalSizeToDevice(Size logicalSize, IntPtr hwnd)
		{
			var pt = LogicalPixelsToDevice(new Point(logicalSize.Width, logicalSize.Height), hwnd);
			return new Size { Width = pt.X, Height = pt.Y };
		}

		public static Size DeviceSizeToLogical(Size deviceSize, IntPtr hwnd)
		{
			var pt = DevicePixelsToLogical(new Point(deviceSize.Width, deviceSize.Height), hwnd);
			return new Size(pt.X, pt.Y);
		}

		private enum MonitorDpiType
		{
			MDT_EFFECTIVE_DPI = 0, // Используется чаще всего
			MDT_ANGULAR_DPI = 1,
			MDT_RAW_DPI = 2,
			MDT_DEFAULT = MDT_EFFECTIVE_DPI
		}

		[DllImport("Shcore.dll")]
		private static extern int GetDpiForMonitor(
			IntPtr hmonitor,
			MonitorDpiType dpiType,
			out uint dpiX,
			out uint dpiY);

		[DllImport("User32.dll")]
		private static extern IntPtr MonitorFromWindow(
			IntPtr hwnd,
			uint dwFlags);

		private const uint MONITOR_DEFAULTTONEAREST = 2;

		public static (uint DpiX, uint DpiY) GetDpiFromWindow(IntPtr hwnd)
		{
			IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
			if (monitor == IntPtr.Zero)
				return DefaultDPI();

			int result = GetDpiForMonitor(monitor, MonitorDpiType.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
			if (result != 0)
				return DefaultDPI();

			return (dpiX, dpiY);
		}

		private static (uint DpiX, uint DpiY) DefaultDPI()
		{
			using (var desktop = SafeDC.GetDesktop())
			{
				// Can get these in the static constructor.  They shouldn't vary window to window,
				// and changing the system DPI requires a restart.
				var pixelsPerInchX = NativeMethods.GetDeviceCaps(desktop, DeviceCap.LOGPIXELSX);
				var pixelsPerInchY = NativeMethods.GetDeviceCaps(desktop, DeviceCap.LOGPIXELSY);

				return ((uint)pixelsPerInchX, (uint)pixelsPerInchY);
			}
		}
	}
}