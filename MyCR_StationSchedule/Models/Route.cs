using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace MyCR_StationSchedule.Models
{


    public class Routes
    {
        public string route_id { get; set; }
        public string route_name { get; set; }
        public string route_hide { get; set; }
    }

    public class Mode
    {
        public string route_type { get; set; }
        public string mode_name { get; set; }
        public IEnumerable<Routes> route { get; set; }
    }

    public class Route
    {
        public IEnumerable<Mode> mode { get; set; }
    }

    public class Stop
    {
        public string stop_order { get; set; }
        public string stop_id { get; set; }
        public string stop_name { get; set; }
        public string parent_station { get; set; }
        public string parent_station_name { get; set; }
        public string stop_lat { get; set; }
        public string stop_lon { get; set; }
    }

    public class Direction
    {
        public string direction_id { get; set; }
        public string direction_name { get; set; }
        public List<Stop> stop { get; set; }
    }

    public class StationStops
    {
        public string route_id { get; set; }
        public List<Direction> direction { get; set; }
    }

    public class RouteStops
    {
        public List<StationStops> route { get; set; }
    }

    public class Schedule
    {
        public string ArrivalTime { get; set; }
        public string DepartureTime { get; set; }
        public string StopName { get; set; }
        public string Destination { get; set; }
        public string Direction { get; set; }
        public string Timing { get; set; }
        public string TrainNumber { get; set; }
        public string Origination { get; set; }
        public string OriginationTime { get; set; }
    }

    public partial class usp_GetTrainStopData
    {
        public string arrival_time { get; set; }
        public string departure_time { get; set; }
        public string stop_id { get; set; }
        public string trip_headsign { get; set; }
        public string direction { get; set; }
        public string Timing { get; set; }
        public string train_number { get; set; }
        public string origination_stop { get; set; }
        public string origination_time { get; set; }
        public string trip_id { get; set; }
    }

}