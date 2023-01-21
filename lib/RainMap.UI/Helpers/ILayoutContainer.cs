using RainMap.UI.Elements;
using RainMap.UI.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap.UI.Helpers
{
    public interface ILayoutContainer
    {
        public void LayoutChild(UIElement child, ref Rect screenRect);
    }
}
