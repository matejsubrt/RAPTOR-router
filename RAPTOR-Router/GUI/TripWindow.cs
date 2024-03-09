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
using RAPTOR_Router.RouteFinders;

namespace GUI
{
    public partial class TripWindow : UserControl
    {
        public TripWindow(SearchResult.UsedTrip trip)
        {
            InitializeComponent();
            lineName.Text = trip.routeName;
            lineName.ForeColor = System.Drawing.Color.FromArgb(trip.Color.R, trip.Color.G, trip.Color.B);
            fromStop.Text = trip.stops[trip.getOnStopIndex];
            toStop.Text = trip.stops[trip.getOffStopIndex];
            stopsNo.Text = (trip.getOffStopIndex - trip.getOnStopIndex).ToString();
            departureTime.Text = trip.getOnTime.ToLongTimeString();
            arrivalTime.Text = trip.getOffTime.ToLongTimeString();
        }
    }
}
