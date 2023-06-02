using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.IO;

namespace CaaS.Service.Clippy
{
    // clippy renderer
    // written for kasumi.NET by Toxoid49b
    public static class Clippy
    {
        static Font clippyFont = new Font("Tahoma", 8.0f);

        [CaaSEndpoint("/clippy", "image/png")]
        public static byte[] Endpoint(string input)
        {
            // Create temporary bitmap to measure input text size
            using (Bitmap tmpBmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            using (Graphics tmpGx = Graphics.FromImage(tmpBmp))
            {
                // Windows fucks up trying to measure the text height unless you specify AA
                tmpGx.TextRenderingHint = TextRenderingHint.AntiAlias;
                SizeF textSize = tmpGx.MeasureString(input, clippyFont, 180, StringFormat.GenericTypographic);
                int textHeight = (int)Math.Ceiling(textSize.Height);

                // Create a new bitmap for the final image
                using (Bitmap imgBmp = new Bitmap(200, textHeight + 16 + 96, PixelFormat.Format32bppArgb))
                using (Graphics imgGx = Graphics.FromImage(imgBmp))
                {
                    imgGx.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

                    // Create the various pens and brushes used to draw the image
                    using (Pen borderPen = new Pen(Brushes.Black))
                    using (SolidBrush rectBrush = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0xCC)))
                    {
                        borderPen.Width = 1.0f;

                        // Fill in the background and text box parts
                        //imgGx.FillRectangle(backBrush, new Rectangle(0, 0, imgBmp.Width, imgBmp.Height));
                        imgGx.FillRectangle(rectBrush, new Rectangle(0, 8, 200, textHeight));
                        imgGx.DrawRectangle(borderPen, new Rectangle(0, 7, 199, textHeight + 1));

                        // Draw the input text
                        imgGx.DrawString(input, clippyFont, Brushes.Black, new RectangleF(10, 8, 180, textHeight), StringFormat.GenericTypographic);

                        // Draw the static image resources
                        imgGx.DrawImageUnscaled(Properties.Resources.clippytop, 0, 0);
                        imgGx.DrawImageUnscaled(Properties.Resources.clippybottom, 0, textHeight + 8);
                        imgGx.DrawImageUnscaled(Properties.Resources.clippy, 32, imgBmp.Height - 86);

                        // scale up the image
                        const int scalex = 4;
                        using (Bitmap scale = new Bitmap(imgBmp.Width * scalex, imgBmp.Height * scalex))
                        using (Graphics scalectx = Graphics.FromImage(scale))
                        {
                            scalectx.SmoothingMode = SmoothingMode.None;
                            scalectx.InterpolationMode = InterpolationMode.NearestNeighbor;
                            scalectx.DrawImage(imgBmp, new Rectangle(0, 0, imgBmp.Width * scalex, imgBmp.Height * scalex));

                            // Return final image (don't forget to dispose it when you're done)
                            using (MemoryStream ms = new MemoryStream())
                            {
                                scale.Save(ms, ImageFormat.Png);
                                ms.Seek(0, SeekOrigin.Begin);
                                return ms.ToArray();
                            }
                        }
                    }
                }
            }
        }
    }
}
