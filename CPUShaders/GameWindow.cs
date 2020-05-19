using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CPUShaders
{
    public partial class GameWindow : Form
    {
        public Label FrameLabel => frameLabel;
        public Label ControlLabel => controlLabel;
        public PictureBox FrameImage => pictureBox1;

        public GameWindow()
        {
            InitializeComponent();
        } 
    }
}
