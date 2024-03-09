using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RAPTOR_Router.RAPTORStructures;

namespace GUI
{
    public partial class TransferWindow : UserControl
    {
        public TransferWindow(SearchResult.UsedTransfer transfer)
        {            
            InitializeComponent();
            distance.Text = transfer.distance.ToString() + " m";
        }
    }
}
