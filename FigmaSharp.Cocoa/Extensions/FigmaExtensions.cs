﻿/* 
 * FigmaViewExtensions.cs - Extension methods for NSViews
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System.Collections.Generic;
using AppKit;
using System.Linq;
using CoreGraphics;
using System;
using System.Net;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Foundation;
using CoreAnimation;
using FigmaSharp.Converters;
using System.Text;

namespace FigmaSharp
{
    public static class FigmaExtensions
    {
        public static NSTextField CreateLabel(string text, NSFont font = null, NSTextAlignment alignment = NSTextAlignment.Left)
        {
            var label = new NSTextField()
            {
                StringValue = text ?? "",
                Font = font ?? GetSystemFont(false),
                Editable = false,
                Bordered = false,
                Bezeled = false,
                DrawsBackground = false,
                Selectable = false,
                Alignment = alignment
            };
            label.TranslatesAutoresizingMaskIntoConstraints = false;
            return label;
        }

        public static NSFont GetSystemFont(bool bold, float size = 0.0f)
        {
            if (size <= 0)
            {
                size = (float)NSFont.SystemFontSize;
            }
            if (bold)
                return NSFont.BoldSystemFontOfSize(size);
            return NSFont.SystemFontOfSize(size);
        }

        #region View Extensions

        public static NSColor ToNSColor(this FigmaColor color)
        {
            return NSColor.FromRgba(color.r, color.g, color.b, color.a);
        }

        public static string ToDesignerString(this float value)
        {
            return string.Concat (value.ToString(),"f");
        }

        public static NSTextAlignment ToNSTextAlignment(string value)
        {
            return value == "CENTER" ? NSTextAlignment.Center : value == "LEFT" ? NSTextAlignment.Left : NSTextAlignment.Right;
        }

        public static string ToDesignerString(this NSTextAlignment alignment)
        {
            return string.Format ("{0}.{1}", nameof(NSTextAlignment), alignment.ToString());
        }

        public static string ToDesignerString(this FigmaColor color, bool cgColor = false)
        {
            var cg = cgColor ? ".CGColor" : "";
            return $"NSColor.FromRgba({color.r.ToDesignerString ()}, {color.g.ToDesignerString ()}, {color.b.ToDesignerString ()}, {color.a.ToDesignerString ()}){cg}";
        }

        public static string ToDesignerString(this bool value)
        {
            return value ? "true" : "false";
        }

        public static CGRect ToCGRect(this FigmaRectangle rectangle)
        {
            return new CGRect(0, 0, rectangle.width, rectangle.height);
        }

        public static string ToDesignerString(this NSFontTraitMask mask)
        {
            if (mask.HasFlag (NSFontTraitMask.Bold))
            {
                return string.Format("{0}.{1}", nameof(NSFontTraitMask), nameof (NSFontTraitMask.Bold));
            }
            return "default(NSFontTraitMask)";
            //return string.Format("{0}.{1}", nameof(NSFontTraitMask), mask.ToString());
        }

        public static string ToNSFontDesignerString(this FigmaTypeStyle style)
        {
            var font = style.ToNSFont();
            var family = font.FamilyName;
            var size = font.PointSize;
            var w = NSFontManager.SharedFontManager.WeightOfFont(font);
            var traits = NSFontManager.SharedFontManager.TraitsOfFont(font);
            return string.Format("NSFontManager.SharedFontManager.FontWithFamily(\"{0}\", {1}, {2}, {3})", family, traits.ToDesignerString (), w, style.fontSize);
        }

        public static NSFont ToNSFont(this FigmaTypeStyle style)
        {
            string family = style.fontFamily;
            if (family == "SF UI Text")
            {
                family = ".SF NS Text";
            }
            else if (family == "SF Mono")
            {
                family = ".SF NS Display";
            }
            else
            {
                Console.WriteLine("FONT: {0} - {1}", family, style.fontPostScriptName);
            }

            var font = NSFont.FromFontName(family, style.fontSize);
            var w = ToAppKitFontWeight(style.fontWeight);
            NSFontTraitMask traits = default(NSFontTraitMask);
            if (style.fontPostScriptName != null && style.fontPostScriptName.EndsWith("-Bold"))
            {
                traits = NSFontTraitMask.Bold;
            }
            if (font == null)
            {
                family = ".AppleSystemUIFont";
            }
            font = NSFontManager.SharedFontManager.FontWithFamily(family, traits, w, style.fontSize);
            if (font == null)
            {
                Console.WriteLine($"[ERROR] Font not found :{family}");
                font = NSFont.LabelFontOfSize(style.fontSize);
            }
            return font;
        }

        public static CGPoint GetRelativePosition(this IAbsoluteBoundingBox parent, IAbsoluteBoundingBox node)
        {
            return new CGPoint(
                node.absoluteBoundingBox.x - parent.absoluteBoundingBox.x,
                node.absoluteBoundingBox.y - parent.absoluteBoundingBox.y
            );
        }

        public static void CreateConstraints(this NSView view, NSView parent, FigmaLayoutConstraint constraints, FigmaRectangle absoluteBoundingBox, FigmaRectangle absoluteBoundBoxParent)
        {
            System.Console.WriteLine("Create constraint  horizontal:{0} vertical:{1}", constraints.horizontal, constraints.vertical);

            if (constraints.horizontal.Contains("RIGHT"))
            {
                var endPosition1 = absoluteBoundingBox.x + absoluteBoundingBox.width;
                var endPosition2 = absoluteBoundBoxParent.x + absoluteBoundBoxParent.width;
                var value = Math.Max(endPosition1, endPosition2) - Math.Min(endPosition1, endPosition2);
                view.RightAnchor.ConstraintEqualToAnchor(parent.RightAnchor, -value).Active = true;

                var value2 = absoluteBoundingBox.x - absoluteBoundBoxParent.x;
                view.LeftAnchor.ConstraintEqualToAnchor(parent.LeftAnchor, value2).Active = true;
            }

            if (constraints.horizontal != "RIGHT")
            {
                var value2 = absoluteBoundingBox.x - absoluteBoundBoxParent.x;
                view.LeftAnchor.ConstraintEqualToAnchor(parent.LeftAnchor, value2).Active = true;
            }

            if (constraints.horizontal.Contains("BOTTOM"))
            {
                var value = absoluteBoundingBox.y - absoluteBoundBoxParent.y;
                view.TopAnchor.ConstraintEqualToAnchor(parent.TopAnchor, value).Active = true;

                var endPosition1 = absoluteBoundingBox.y + absoluteBoundingBox.height;
                var endPosition2 = absoluteBoundBoxParent.y + absoluteBoundBoxParent.height;
                var value2 = Math.Max(endPosition1, endPosition2) - Math.Min(endPosition1, endPosition2);

                view.BottomAnchor.ConstraintEqualToAnchor(parent.BottomAnchor, -value2).Active = true;
            }

            if (constraints.horizontal != "BOTTOM")
            {
                var value = absoluteBoundingBox.y - absoluteBoundBoxParent.y;
                view.TopAnchor.ConstraintEqualToAnchor(parent.TopAnchor, value).Active = true;
            }
        }

        static int[] app_kit_font_weights = {
            2,   // FontWeight100
      3,   // FontWeight200
      4,   // FontWeight300
      5,   // FontWeight400
      6,   // FontWeight500
      8,   // FontWeight600
      9,   // FontWeight700
      10,  // FontWeight800
      12,  // FontWeight900
            };

        public static int ToAppKitFontWeight(float font_weight)
        {
            float weight = font_weight;
            if (weight <= 50 || weight >= 950)
                return 5;

            var select_weight = (int)Math.Round(weight / 100) - 1;
            return app_kit_font_weights[select_weight];
        }

        public static CGPath ToGCPath(this NSBezierPath bezierPath)
        {
            var path = new CGPath();
            CGPoint[] points;
            for (int i = 0; i < bezierPath.ElementCount; i++)
            {
                var type = bezierPath.ElementAt(i, out points);
                switch (type)
                {
                    case NSBezierPathElement.MoveTo:
                        path.MoveToPoint(points[0]);
                        break;
                    case NSBezierPathElement.LineTo:
                        path.AddLineToPoint(points[0]);
                        break;
                    case NSBezierPathElement.CurveTo:
                        path.AddCurveToPoint(points[2], points[1], points[0]);
                        break;
                    case NSBezierPathElement.ClosePath:
                        path.CloseSubpath();
                        break;
                }
            }
            return path;
        }


        #endregion

    }
}
