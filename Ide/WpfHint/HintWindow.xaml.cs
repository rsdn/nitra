﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfHint2.Parsing;
using WpfHint2.UIBuilding;
using System.Diagnostics;
using System.ComponentModel;

namespace WpfHint2
{
	/// <summary>
	/// Interaction logic for HintWindow.xaml
	/// </summary>
	internal partial class HintWindow
	{
    public bool IsClosing { get; private set; }
    private readonly Hint            _hint;
		private readonly HintRoot        _hintRoot;
		private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
		private          string          _text;

		public string Text
		{
			get { return _text; }
			set
			{
				_text = value;

				try
				{
					var root = HintParser.Parse(value);
					var fe = HintBuilder.Build(root, _hint);
					border.Child = fe;
				}
				catch (Exception ex)
				{
					Trace.WriteLine(ex);
				}
			}
		}

		public double WrapWidth
		{
			get { return ((HintDecorator)border.Child).WrapWidth; }
			set { ((HintDecorator)border.Child).WrapWidth = value; }
		}

		public HintWindow(Hint hint, HintRoot root)
		{
			InitializeComponent();

			this._hint    = hint;
			_hintRoot     = root;

			// window props
			Left          = 0;
			Top           = 0;
			MaxHeight     = 1200;
			SizeToContent = SizeToContent.WidthAndHeight;
			Opacity       = 1;

			_timer.Tick          += OnTimerTick;
			LayoutUpdated        += OnLayoutUpdated;
			MouseLeave           += OnMouseLeave;
			_hintRoot.MouseLeave += RestartCloseTimer;
			MouseRightButtonUp   += MouseButtonEventHandler;

			HintControl.AddClickHandler(this, OnClick);
			HintControl.AddMouseHoverHandler(this, OnMouseHover);
      TextOptions.SetTextFormattingMode((DependencyObject)this, TextFormattingMode.Display);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      IsClosing = true;
      base.OnClosing(e);
    }

    protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			RestartCloseTimer();
		}

		protected override void OnClosed(EventArgs e)
		{
			_hintRoot.MouseLeave -= RestartCloseTimer;
			MouseLeave           -= OnMouseLeave;

			_timer.Stop();
			_hintRoot.Dispose();
			base.OnClosed(e);
		}

		#region HintControl event handling

		private void MouseButtonEventHandler(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Released)
				Close();
		}

		private void OnClick(object sender, RoutedEventArgs e)
		{
			var hc = e.Source as HintControl;

			if (hc == null)
				return;

			if (hc.Handler != null)
				_hint.RaiseClick(hc.Handler);
		}

		private void OnMouseHover(object sender, RoutedEventArgs e)
		{
      try
      {
        if (!(e.Source is HintControl hc))
          return;

        if (hc.Hint != null)
				  ShowSubHint(hc, hc.Hint);
      }
      catch (Exception ex)
      {
        Trace.WriteLine(ex);
      }
    }

    private void ShowSubHint(FrameworkElement el, string hintText)
    {
      ShowSubHint(_hint, el, hintText, this);
    }

    public static Window ShowSubHint(Hint hint, FrameworkElement el, string hintText, Window owner)
		{
			var ht = HintRoot.Create(el);

      if (owner != null)
      {
        foreach (HintWindow window in owner.OwnedWindows)
        {
          if (!window._hintRoot.Equals(ht))
          {
            window.Close();
            continue;
          }

          ht.Dispose();
          return null;
        }
      }

			var wnd = new HintWindow(hint, ht) { Text = hintText, Owner = owner };

      if (hint.BackgroundResourceReference != null)
        wnd.border.SetResourceReference(Border.BackgroundProperty, hint.BackgroundResourceReference);

      if (hint.ForegroundResourceReference != null)
        wnd._textBlock.SetResourceReference(TextBlock.ForegroundProperty, hint.ForegroundResourceReference);

      wnd.Show();

      return wnd;
    }

		#endregion

		#region Mouse leave checking

		void OnMouseLeave(object sender, MouseEventArgs e)
		{
			RestartCloseTimer();
		}

		public void RestartCloseTimer()
		{
			// restart timer
			_timer.Stop();
			_timer.Start();
		}

		void OnTimerTick(object sender, EventArgs e)
		{
			_timer.Stop();

			if (OwnedWindows.Count > 0 || IsMouseOver || _hintRoot.IsMouseOver)
			{
				_timer.Start();
				return;
			}

			this.Close();

			var owner = Owner as HintWindow;

			if (owner != null)
				owner.OnTimerTick(sender, e);
		}

		#endregion

		#region Window layout relative to screen

		void OnLayoutUpdated(object sender, EventArgs e)
		{
			var dx = 1.0;
			var dy = 1.0;

			var src = PresentationSource.FromVisual(this);
			if (src != null)
			{
				var m = src.CompositionTarget.TransformToDevice;
				dx = m.M11;
				dy = m.M22;
			}

			var rect = _hintRoot.ActiveRect;

			var size = new Size(ActualWidth * dx, ActualHeight * dy);
			var scrSize = new Size(SystemParameters.VirtualScreenWidth * dx,
					                   SystemParameters.VirtualScreenHeight * dy);

			var pos = rect.BottomLeft;

			if (rect.Bottom + size.Height > scrSize.Height)
				pos.Y = rect.Top - size.Height;

			if (rect.Left + size.Width > scrSize.Width)
				pos.X = scrSize.Width - size.Width;

			// update location
			this.Left = pos.X / dx;
			this.Top  = pos.Y / dy;
		}

		#endregion
	}
}
