using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;
using Svg;

namespace OCOverlay {
    class SVGParser {

        public static BitmapImage parse(String path) {
            return SvgDocument.Open(path).Draw().convertToBitmap();
        }
    }
}
