using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Unreal_Dumping_Agent.UI.Controls
{
    public class MyColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get; }
        public override Color MenuItemBorder { get; }

        private MyColorTable(Color color) : base()
        {
            MenuItemSelected = color;
            MenuItemBorder = color;
        }

        public static ToolStripProfessionalRenderer ToolStripRenderColor(Color color)
        {
            return new ToolStripProfessionalRenderer(new MyColorTable(color));
        }
    }
}
