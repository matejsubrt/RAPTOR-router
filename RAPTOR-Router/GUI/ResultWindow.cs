using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RAPTOR_Router.Models.Results;

namespace GUI
{
    public partial class ResultWindow : UserControl
    {
        SearchResult result;
        Form1 parent;

        public void Generate()
        {
            int currY = 100;
            int currX = 50;
            foreach (var seg in result.UsedSegments)
            {
                switch (seg.segmentType)
                {
                    case SearchResult.SegmentType.Trip:
                        SearchResult.UsedTrip trip = (SearchResult.UsedTrip)seg;
                        TripWindow tripWindow = new(trip);
                        tripWindow.Location = new Point(currX, currY);
                        this.Controls.Add(tripWindow);
                        currY += tripWindow.Height + 2;
                        break;
                    case SearchResult.SegmentType.Transfer:
                        SearchResult.UsedTransfer transfer = (SearchResult.UsedTransfer)seg;
                        TransferWindow transferWindow = new(transfer);
                        transferWindow.Location = new Point(currX, currY);
                        this.Controls.Add(transferWindow);
                        currY += transferWindow.Height + 2;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            returnToSearchButton.Location = new Point(currX, currY + 2);
            currY += returnToSearchButton.Height;

            if (currY > this.Size.Height)
            {
                this.Size = new Size(this.Size.Width, currY + 10);
                this.parent.ClientSize = new Size(this.Size.Width + 10, currY + 10 + 10);
            }
        }
        public ResultWindow(SearchResult result, Form1 parent)
        {
            this.result = result;
            this.parent = parent;
            InitializeComponent();
            Generate();
        }

        private void returnToSearchButton_Click(object sender, EventArgs e)
        {
            parent.ShowSearch();
        }

        private void ResultWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                parent.ShowSearch();
            }
        }

        private void ResultWindow_Enter(object sender, EventArgs e)
        {
            returnToSearchButton.Focus();
        }
    }
}
