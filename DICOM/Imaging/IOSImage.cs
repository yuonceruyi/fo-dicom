﻿// Copyright (c) 2012-2015 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace Dicom.Imaging
{
    using System;
    using System.Collections.Generic;

    using CoreGraphics;

    using CoreImage;

    using Dicom.Imaging.Render;
    using Dicom.IO;

    using UIKit;

    /// <summary>
    /// <see cref="IImage"/> implementation of a Windows Forms <see cref="CGImage"/>.
    /// </summary>
    public sealed class IOSImage : ImageDisposableBase<CGImage>
    {
        #region CONSTRUCTORS

        /// <summary>
        /// Initializes an instance of the <see cref="IOSImage"/> object.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="pixels">Array of pixels.</param>
        /// <param name="image">Image object.</param>
        public IOSImage(int width, int height)
            : base(width, height, new PinnedIntArray(width * height), null)
        {
        }

        /// <summary>
        /// Initializes an instance of the <see cref="IOSImage"/> object.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="pixels">Array of pixels.</param>
        /// <param name="image">Image object.</param>
        private IOSImage(int width, int height, PinnedIntArray pixels, CGImage image)
            : base(width, height, pixels, image)
        {
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Cast <see cref="IImage"/> object to specific (real image) type.
        /// </summary>
        /// <typeparam name="T">Real image type to cast to.</typeparam>
        /// <returns><see cref="IImage"/> object as specific (real image) type.</returns>
        public override T As<T>()
        {
            if (typeof(T) == typeof(UIImage))
            {
                return (T)(object)new UIImage(this.image);
            }

            if (typeof(T) == typeof(CIImage))
            {
                return (T)(object)new CIImage(this.image);
            }

            return base.As<T>();
        }

        /// <summary>
        /// Renders the image given the specified parameters.
        /// </summary>
        /// <param name="components">Number of components.</param>
        /// <param name="flipX">Flip image in X direction?</param>
        /// <param name="flipY">Flip image in Y direction?</param>
        /// <param name="rotation">Image rotation.</param>
        public override void Render(int components, bool flipX, bool flipY, int rotation)
        {
            using (
                var context = new CGBitmapContext(
                    this.pixels.Pointer,
                    this.width,
                    this.height,
                    8,
                    4 * this.width,
                    CGColorSpace.CreateDeviceRGB(),
                    CGImageAlphaInfo.PremultipliedLast))
            {
                var transform = CGAffineTransform.MakeRotation((float)(rotation * Math.PI / 180.0));
                transform.Scale(flipX ? -1.0f : 1.0f, flipY ? -1.0f : 1.0f);
                transform.Translate(flipX ? this.width : 0.0f, flipY ? this.height : 0.0f);
                context.ConcatCTM(transform);

                this.image = context.ToImage();
            }
        }

        /// <summary>
        /// Draw graphics onto existing image.
        /// </summary>
        /// <param name="graphics">Graphics to draw.</param>
        public override void DrawGraphics(IEnumerable<IGraphic> graphics)
        {
            using (
                var context = new CGBitmapContext(
                    IntPtr.Zero,
                    this.image.Width,
                    this.image.Height,
                    this.image.BitsPerComponent,
                    this.image.BytesPerRow,
                    this.image.ColorSpace,
                    this.image.AlphaInfo))
            {
                context.DrawImage(new CGRect(0.0, 0.0, this.image.Width, this.image.Height), this.image);

                foreach (var graphic in graphics)
                {
                    var layer = graphic.RenderImage(null).As<CGImage>();
                    context.DrawImage(
                        new CGRect(
                            graphic.ScaledOffsetX,
                            graphic.ScaledOffsetY,
                            graphic.ScaledWidth,
                            graphic.ScaledHeight),
                        layer);
                }

                this.image = context.ToImage();
            }
        }

        /// <summary>
        /// Creates a deep copy of the image.
        /// </summary>
        /// <returns>Deep copy of this image.</returns>
        public override IImage Clone()
        {
            return new IOSImage(
                this.width,
                this.height,
                new PinnedIntArray(this.pixels.Data),
                this.image == null ? null : this.image.Clone());
        }

        #endregion
    }
}