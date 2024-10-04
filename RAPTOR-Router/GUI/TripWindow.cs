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
            lineName.ForeColor = System.Drawing.Color.FromArgb(trip.color.R, trip.color.G, trip.color.B);
            fromStop.Text = trip.stopPasses[trip.getOnStopIndex].Name;
            toStop.Text = trip.stopPasses[trip.getOffStopIndex].Name;
            stopsNo.Text = (trip.getOffStopIndex - trip.getOnStopIndex).ToString();
            departureTime.Text = trip.stopPasses[trip.getOnStopIndex].DepartureTime.ToLongTimeString();
            arrivalTime.Text = trip.stopPasses[trip.getOffStopIndex].DepartureTime.ToLongTimeString();
        }
    }
}
