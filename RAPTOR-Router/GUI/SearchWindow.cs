using RAPTOR_Router.RouteFinders;
using RAPTOR_Router.Structures.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class SearchWindow : UserControl
    {
        RouteFinderBuilder builder;
        Form1 parent;
        public SearchWindow(RouteFinderBuilder builder, Form1 parent)
        {
            this.builder = builder;
            this.parent = parent;
            InitializeComponent();
        }

        private void ResetForm()
        {
            // Reset text boxes
            srcStopTextBox.Text = string.Empty;
            destStopTextBox.Text = string.Empty;

            // Reset DateTimePicker to current date and time
            departureDatePicker.Value = DateTime.Now;

            // Reset NumericUpDown (walkingPace) to default value
            walkingPaceNumericUpDown.Value = 12;

            // Reset sliders (TrackBars) to default value
            trackBar1.Value = 2;
            trackBar2.Value = 2;
            trackBar3.Value = 0;
        }
        private void FindConnection()
        {
            Settings settings = new Settings();

            string srcStop = srcStopTextBox.Text;
            string destStop = destStopTextBox.Text;
            DateTime departureDate = departureDatePicker.Value;
            DateTime departureTime = departureTimePicker.Value;
            DateTime departureDateTime = new DateTime(departureDate.Year, departureDate.Month, departureDate.Day, departureTime.Hour, departureTime.Minute, departureTime.Second);
            int walkingPace = (int)walkingPaceNumericUpDown.Value;

            int transferTime = trackBar1.Value;
            int comfortBalance = trackBar2.Value;
            int walkingPreference = trackBar3.Value;

            settings.WalkingPreference = (WalkingPreference)walkingPreference;
            settings.ComfortBalance = (ComfortBalance)comfortBalance;
            settings.TransferTime = (TransferTime)transferTime;
            settings.WalkingPace = walkingPace;
            settings.UseSharedBikes = false;

            IRouteFinder router = builder.CreateForwardRouteFinder(settings);

            var result = router.FindConnection(srcStop, destStop, departureDateTime);
            router = builder.CreateForwardRouteFinder(settings);
            if (result is null)
            {
                MessageBox.Show("Connection could not be found, please try again");
            }
            else
            {
                parent.ShowResult(result);
            }
            ResetForm();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FindConnection();
        }

        private void SearchWindow_Load(object sender, EventArgs e)
        {
            srcStopTextBox.Focus();
        }

        private void srcStopTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                destStopTextBox.Focus();
            }
        }

        private void SearchWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FindConnection();
            }
        }

        private void SearchWindow_Enter(object sender, EventArgs e)
        {
            srcStopTextBox.Focus();
        }

        private void destStopTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FindConnection();
            }
        }
    }
}
