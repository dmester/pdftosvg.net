using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal static class RectangleUtils
    {
        public static Rectangle GetA4()
        {
            return new Rectangle(0, 0, 594.96, 841.92);
        }

        public static Rectangle GetBoundingRectangleAfterTransform(Rectangle rect, Matrix transform)
        {
            if (transform.IsIdentity)
            {
                return rect;
            }

            var topLeft = transform * rect.TopLeft;
            var topRight = transform * rect.TopRight;
            var bottomLeft = transform * rect.BottomLeft;
            var bottomRight = transform * rect.BottomRight;

            var x1 = double.MaxValue;
            var x2 = double.MinValue;

            var y1 = double.MaxValue;
            var y2 = double.MinValue;

            if (x1 > topLeft.X) x1 = topLeft.X;
            if (x1 > topRight.X) x1 = topRight.X;
            if (x1 > bottomLeft.X) x1 = bottomLeft.X;
            if (x1 > bottomRight.X) x1 = bottomRight.X;

            if (x2 < topLeft.X) x2 = topLeft.X;
            if (x2 < topRight.X) x2 = topRight.X;
            if (x2 < bottomLeft.X) x2 = bottomLeft.X;
            if (x2 < bottomRight.X) x2 = bottomRight.X;

            if (y1 > topLeft.Y) y1 = topLeft.Y;
            if (y1 > topRight.Y) y1 = topRight.Y;
            if (y1 > bottomLeft.Y) y1 = bottomLeft.Y;
            if (y1 > bottomRight.Y) y1 = bottomRight.Y;

            if (y2 < topLeft.Y) y2 = topLeft.Y;
            if (y2 < topRight.Y) y2 = topRight.Y;
            if (y2 < bottomLeft.Y) y2 = bottomLeft.Y;
            if (y2 < bottomRight.Y) y2 = bottomRight.Y;

            return new Rectangle(x1, y1, x2, y2);
        }
    }
}
