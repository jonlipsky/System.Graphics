﻿using System.ComponentModel;
using Elevenworks.Graphics;
using Foundation;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Graphics.CoreGraphics;
using Xamarin.Graphics.Forms.iOS;

[assembly: ExportRenderer(typeof(GraphicsView), typeof(GraphicsViewRenderer))]

namespace Xamarin.Graphics.Forms.iOS
{
    [Preserve]
    public class GraphicsViewRenderer : ViewRenderer<GraphicsView, MTGraphicsView>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<GraphicsView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                // Unsubscribe from event handlers and cleanup any resources
                SetNativeControl(null);
            }

            if (e.NewElement != null)
            {
                SetNativeControl(new MTGraphicsView());
            }
        }

        protected override void OnElementPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(GraphicsView.Drawable))
                UpdateDrawable();
        }

        private void UpdateDrawable()
        {
            Control.Drawable = Element.Drawable;
        }
    }
}