﻿using System;
using System.Windows.Forms;
using System.Drawing;

namespace FigmaSharp
{
    public class ImageWrapper : IImageWrapper
    {
        public object NativeObject => image;

        protected Image image;
        public ImageWrapper(Image image)
        {
            this.image = image;
        }
    }
}
