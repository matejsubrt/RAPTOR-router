using Microsoft.Extensions.Configuration;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.RouteFinders;
using System.Windows.Forms;

namespace GUI
{
    public partial class Form1 : Form
    {
        static RouteFinderBuilder builder = new();
        ResultWindow currResultWindow;
        SearchWindow searchWindow;

        public void HideResult()
        {
            this.Controls.Remove(currResultWindow);
            currResultWindow = null;
        }
        public void HideSearch()
        {
            searchWindow.Hide();
        }
        public void ShowResult(SearchResult result)
        {
            HideSearch();
            ResultWindow window = new(result, this);
            window.Location = new Point(0, 0);
            window.Name = "result";
            currResultWindow = window;            

            this.Controls.Add(window);
            window.Focus();
        }        
        public void ShowSearch()
        {
            if(currResultWindow != null)
            {
                HideResult();
            }
            if(searchWindow is null)
            {
                SearchWindow window = new(builder, this);
                window.Location = new Point(0, 0);
                window.Name = "search";
                searchWindow = window;

                this.Controls.Add(window);
            }
            else
            {
                searchWindow.Show();
                searchWindow.Focus();
            }
            
        }
        void ParseGtfs()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\..")
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
            string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];

            if (gtfsZipArchiveLocation == null)
            {
                Console.WriteLine("No gtfs archive found in following location: " + config["gtfsLocation"]);
                Console.WriteLine("Change the gtfs location in the config.json file, so that the path is correct");
                return;
            }

            builder.LoadGtfsData(gtfsZipArchiveLocation);
        }
        public Form1()
        {
            InitializeComponent();
            ParseGtfs();
            ShowSearch();
        }
    }
}
