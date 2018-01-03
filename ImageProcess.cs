using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidOperate
{
    class ImageProcess
    {
        public ImageProcess(string path)
        {
            Init(path);
        }

        public Point NextPoint { get; set; }
        public Point CurrentPoint { get; set; }

        public void Init(string path)
        {
            CurrentPoint = GetCurrentPoint(path);
            NextPoint = GetNextPoint(path);
        }

        private Point GetCurrentPoint(string path)
        {
            var bmp = new Bitmap(path);
            Color ColorLeft;
            Color ColorRight;
            Color ColorCenter = Color.FromArgb(55, 56, 97);
            Point PointLeft = new Point(bmp.Width,bmp.Height);
            for (int i = 1400; i > 900; i=i-10)
            {
                for (int j = 100; j < 1000; j=j+10)
                {
                    ColorLeft = bmp.GetPixel(j, i);
                    ColorRight = bmp.GetPixel(j+10, i-10);
                    if (ColorSub(ColorCenter, ColorLeft) > 1500 && ColorSub(ColorCenter, ColorRight) < 100)
                    {
                        if (j + 5 >= PointLeft.X)
                        {
                            break;
                        }
                        PointLeft.X = j + 5;
                        PointLeft.Y = i - 16;
                    }
                }
            }

            Point p = new Point(PointLeft.X+30, PointLeft.Y);
            return p;
        }

        private Point GetNextPoint(string path)
        {
            var bmp = new Bitmap(path);
            Color ColorTop = new Color() ;
            Color ColorButtom;
            Point PointTop = new Point(bmp.Width, bmp.Height);
            for (int i = 350; i < 1100; i++)
            {
                bool NeedBreak = false;
                for (int j = bmp.Width-1; j > 5; j = j - 5)
                {
                    ColorTop = bmp.GetPixel(j, i);
                    ColorButtom = bmp.GetPixel(j, i+1);
                    if (ColorSub(ColorTop, ColorButtom) > 800 && (ColorButtom.ToArgb() == Color.FromArgb(72, 72, 72).ToArgb() || ColorSub(Color.FromArgb(52, 53, 60), ColorButtom) > 3000))
                    {
                        PointTop.X = j;
                        PointTop.Y = i + 1;
                        NeedBreak = true;
                        break;
                    }
                    //if (ColorSub(ColorTop, ColorButtom) > 1500 && ColorSub(ColorCenter, ColorTop) < 550)
                    //{
                    //    PointTop.X = j;
                    //    PointTop.Y = i+1;
                    //    NeedBreak = true;
                    //    break;
                    //}
                }
                if (NeedBreak)
                {
                    break;
                }
            }
            if (PointTop.Y >= 1915)
            {
                PointTop.Y = 1910;
            }
            if (PointTop.X >= 1080)
            {
                PointTop.X = 1075;
            }
            ColorTop = bmp.GetPixel(PointTop.X, PointTop.Y+5);
            var ColorBack = bmp.GetPixel(PointTop.X, PointTop.Y-10);
            Point PointButtom = new Point(PointTop.X, CurrentPoint.Y);
            for (int i = CurrentPoint.Y - PointTop.Y - 1; i > 5; i--)
            {
                ColorButtom = bmp.GetPixel(PointTop.X, PointTop.Y+i);
                if (ColorSub(ColorTop, ColorButtom) < 88)
                {
                    PointButtom.Y = PointTop.Y + i;
                    break;
                }
            }
            //Color ColorRight;
            //Color ColorLeft;
            //Point PointRight = new Point(bmp.Width - 1, 1);
            //for (int i = 400; i < 1100; i = i + 1)
            //{
            //    bool NeedBreak = false;
            //    for (int j = bmp.Width - 1; j > 1; j--)
            //    {
            //        ColorRight = bmp.GetPixel(j, i);
            //        ColorLeft = bmp.GetPixel(j - 1, i);
            //        if (ColorSub(ColorRight, ColorLeft) > 1200 && ColorSub(ColorLeft, ColorTop) < 1000)
            //        {
            //            if (PointRight.X <= j)
            //            {
            //                NeedBreak = true;
            //                break;
            //            }
            //            PointRight.X = j;
            //            PointRight.Y = i;
            //        }
            //    }
            //    if (NeedBreak)
            //    {
            //        break;
            //    }
            //}

            //Point p = new Point(PointTop.X, ((PointButtom.Y - PointTop.Y) / 4) + PointTop.Y);
            Point p = new Point(PointTop.X, (PointButtom.Y + PointTop.Y) / 2);
            return p;
        }

        private double ColorSub(Color color1, Color color2)
        {
            double temp = Math.Pow((color1.R - color2.R), 2) + Math.Pow((color1.G - color2.G), 2) + Math.Pow((color1.B - color2.B), 2);
            return temp;
        }
    }
}
