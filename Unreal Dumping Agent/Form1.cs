using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unreal_Dumping_Agent.UI;
using Unreal_Dumping_Agent.UI.Controls;

namespace Unreal_Dumping_Agent
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Load Font Awesome
            if (!FontAwesome5.LoadFont())
            {
                MessageBox.Show(@"Can't load font files !!", @"FontAwesome", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            // Get all controls
            const BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            var fieldValues = this.GetType()
                        .GetFields(bindFlags)
                        .Select(field => field.GetValue(this))
                        .ToList().OfType<Control>();

            foreach (var control in fieldValues)
            {
                InitStyle(control);

                foreach (Control controlControl in control.Controls)
                    InitStyle(controlControl);
            }

            // Change tables(ToolStripItems, etc) highlight color
            ToolStripManager.Renderer = MyColorTable.ToolStripRenderColor(Color.FromArgb(30, 30, 30));

            // Init text and some other properties for controls 
            InitControls();
        }

        private static void InitStyle(Control control)
        {
            control.Font = FontAwesome5.FontAwesome;
            control.MouseEnter += (sender, e) => ((Control)sender).BackColor = Color.FromArgb(30, 30, 30);
            control.MouseLeave += (sender, e) => ((Control)sender).BackColor = Color.FromArgb(20, 20, 20);
        }

        private void InitControls()
        {
            // menuButton
            menuButton.Text = FontAwesome5.Bars;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void MenuButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                settingsMenu.Show(menuButton,
                            menuButton.Location.X - 8 + menuButton.Size.Width, 
                            menuButton.Location.Y - 6);
        }
    }
}
