﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Accord.Math.Distances;
using Emgu.CV;
using Emgu.CV.Freetype;
using Emgu.CV.Structure;

namespace HueDream.Models.Util {
    public static class ImUtil {
        public static Image<Bgr, byte> DrawLabel(Image<Bgr, byte> input, string text, Point pos, MCvScalar color) {
            using Freetype2 ft = new Freetype2();
            ft.PutText(input, text, pos, 6, color, 1, Emgu.CV.CvEnum.LineType.AntiAlias, false);
            return input;
        }

        public static Image<Bgr, byte> Resize(Image<Bgr, byte> input, Emgu.CV.CvEnum.Inter inter, int width = 0, int height = 0) {
            var w = input.Width;
            var h = input.Height;

            if (width == 0 && height == 0) return input;

            if (width == 0) {
                var r = height / h;
                width = w * r;
            } else {
                var r = width / w;
                height = h * r;
            }
            
            return input.Resize(width, height, inter);
        }

        public static PointF[] OrderPoints(List<PointF> points) {
            var xPoints = points.OrderByDescending(p => p.X).ToList();
            PointF[] leftMost = { points[0], points[1] };
            PointF[] rightMost = points.Skip(Math.Max(0, points.Count() - 2)).ToArray();

            leftMost = leftMost.OrderByDescending(p => p.Y).ToArray();
            var tl = leftMost[0];
            var bl = leftMost[1];
            var br = tl;
            var tr = bl;
            double maxDistance = 0;
            var farPoint = tl;
            var dt = new Tuple<double, double>(tl.X, tl.Y);
            foreach (PointF p in points) {
                var pt = new Tuple<double, double>(p.X, p.Y);
                var e = new Euclidean();
                var dist = e.Distance(dt, pt);
                if (dist > maxDistance) {
                    maxDistance = dist;
                    farPoint = p;
                    br = p;
                }                
            }
            foreach(PointF p in points) {
                if (p != farPoint) {
                    tr = p;
                }
            }
            var outPut = new List<PointF>();
            var final = new[] { tl, tr, bl, br };
            outPut.AddRange(final);
            return outPut.ToArray();
        }

        public static Image<Bgr, byte> FourPointTransform(Image<Bgr, byte> input, List<PointF> points) {
            var rect = OrderPoints(points);
            var tl = rect[0];
            var tr = rect[1];
            var br = rect[2];
            var bl = rect[3];
            var widthA = Math.Sqrt((Math.Pow(br.X - bl.X, 2)) + (Math.Pow(br.Y - bl.Y, 2)));
            var widthB = Math.Sqrt((Math.Pow(tr.X - tl.X, 2)) + (Math.Pow(tr.Y - tl.Y, 2)));
            var maxWidth = (float) Math.Max(widthA, widthB);

            var heightA = Math.Sqrt((Math.Pow(tr.X - br.X, 2)) + (Math.Pow(tr.Y - br.Y, 2)));
            var heightB = Math.Sqrt((Math.Pow(tl.X - bl.X, 2)) + (Math.Pow(tl.Y - bl.Y, 2)));
            var maxHeight = (float) Math.Max(heightA, heightB);

            PointF[] dst = { new PointF(0, 0), new PointF(maxWidth - 1, 0), new PointF(maxWidth - 1, maxHeight - 1), new PointF(0, maxHeight - 1) };
            var m = CvInvoke.GetPerspectiveTransform(rect, dst);
            var foo = new Mat();
            Size size = new Size(Convert.ToInt32(maxWidth), Convert.ToInt32(maxHeight));
            CvInvoke.WarpPerspective(input, foo, m, size);
            return foo.ToImage<Bgr, byte>();
        }
    }
}