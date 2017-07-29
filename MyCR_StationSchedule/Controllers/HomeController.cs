using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using Newtonsoft.Json;
using System.Net;
using MyCR_StationSchedule.Models;
using System.Data.SqlClient;
using System.Data;
using System.Data.Entity.Infrastructure;
using MyCR_StationSchedule.Common;
using MyCR_StationSchedule.ViewModels;
using System.IO;
using System.Text;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MyCR_StationSchedule.Controllers
{
    public class HomeController : Controller
    {
        protected string serviceUrlBase = ConfigurationManager.AppSettings["serviceUrlBase"];
        protected string apiKey = ConfigurationManager.AppSettings["api_key"];
        protected Logging log = new Logging();
        static string routesFile = ConfigurationManager.AppSettings["routesFile"];
        protected string stopsFile = ConfigurationManager.AppSettings["stopsFile"];
        protected string routeOutput ="";
        protected string stopOutput = "";

        // GET: Schedule
        public ActionResult Index()
        {
            string url = serviceUrlBase + "routes?api_key=" + apiKey + "&format=json";
            List<Routes> routeList = new List<Routes>();
            WebClient client = new WebClient();

            List<SelectListItem> routeListDD = new List<SelectListItem>();
            routeListDD.Add(new SelectListItem { Text = "Select your route", Value = "-1" });

            try
            {
                //output = client.DownloadString(url);
                routeOutput = GetJsonDataFromFile(routesFile);
                Route routes = JsonConvert.DeserializeObject<Route>(routeOutput);
                foreach (var item in routes.mode)
                {
                    if (item.route_type == "2")
                    {
                        foreach (var thisRoute in item.route)
                        {
                            //routeList.Add(new Routes() { route_id = thisRoute.route_id, route_name = thisRoute.route_name });
                            routeListDD.Add(new SelectListItem { Text = thisRoute.route_name, Value = thisRoute.route_id });
                            System.Diagnostics.Debug.WriteLine("Text: {0}, Valkue: {1}", thisRoute.route_name, thisRoute.route_id);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                routeListDD.Add(new SelectListItem { Text = "No route response from the Commuter Rail.", Value = "-1" });
                routeListDD.Add(new SelectListItem { Text = e.Message, Value = "-2" });
            }

            ViewData["routes"] = routeListDD;
            return View(routeList);
        }

        public JsonResult GetStops(string id, string inOrOut)
        {
            string url = serviceUrlBase + "stopsbyroute?api_key=" + apiKey + "&route=" + id + "&format=json";
            WebClient client = new WebClient();
            List<SelectListItem> stopsList = new List<SelectListItem>();
            stopsList.Add(new SelectListItem { Value = "-1", Text="Select your stop" });
            try
            {
                string output = client.DownloadString(url);
                stopOutput = GetJsonDataFromFile(stopsFile);

                StationStops stops = JsonConvert.DeserializeObject<StationStops>(output);
                RouteStops stops2 = JsonConvert.DeserializeObject<RouteStops>(output);

                //var theseStops = from stop in stops2.route
                //                 where stop.route_id == id
                //                 select stop;

                foreach (var stop in stops.direction)
                {
                    //foreach (var thisStop in stop.stop)
                    //{
                    //    stopsList.Add(new SelectListItem { Text = thisStop.stop_name, Value = thisStop.stop_id });
                    //}
                    //foreach (var stop in item)
                        if (stop.direction_id == inOrOut)
                        {
                            foreach (var thisStop in stop.stop)
                            {
                                stopsList.Add(new SelectListItem { Text = thisStop.stop_name, Value = thisStop.stop_order });
                            }
                        }
                }
            }
            catch (Exception e)
            {
                stopsList.Add(new SelectListItem { Text = "No stop response from the Commuter Rail.", Value = "-1" });
                stopsList.Add(new SelectListItem { Text = e.Message, Value = "-2" });
            }
            return Json(new SelectList(stopsList, "Value", "Text"));

        }

        public JsonResult GetSchedules(string from, string to, string fromSeq, string toSeq)
        {
            string sql = "EXEC usp_GetTrainStopData @StopFrom, @StopTo, @TripDirection";
            List<OutputStops> scheduledStops = new List<OutputStops>();

            int tripDirection = (int)Common.Enums.TripDirection.OUTBOUND;
            int fromSeqNum = Convert.ToInt16(fromSeq);
            int toSeqNum = Convert.ToInt16(toSeq);

            // based on the "stop order" from the stops dropdown, check if user selected an inbound or outbound trip
            if (fromSeqNum > toSeqNum)
            {
                tripDirection = (int)Common.Enums.TripDirection.INBOUND;
            }

            try
            {
                using (var context = new MyStationScheduleEntities1())
                {
                    
                    SqlParameter stop1 = new SqlParameter("@Stop", from);
                    var stops = context.Database.SqlQuery<usp_GetTrainStopData>(sql,
                        new SqlParameter("@StopFrom", from),
                        new SqlParameter("@StopTo", to),
                        new SqlParameter("@TripDirection", tripDirection)
                        ).ToList();

                    bool prevWasBeforeNow = true;
                    int dayOfWeek = (int)DateTime.Now.DayOfWeek;

                    foreach (usp_GetTrainStopData item in stops)
                    {
                        OutputStops thisStop = new OutputStops();
                        thisStop.NextStop = false;
                        // check if the previous train was before the current time
                        if (prevWasBeforeNow==true && item.departure_time.CompareTo(DateTime.Now.TimeOfDay.ToString()) > 0)
                        {
                            if ((dayOfWeek==6 && item.Timing == "Saturday") || (dayOfWeek==7 && item.Timing == "Sunday") || (dayOfWeek<6 && item.Timing == "Weekday"))
                            {
                                thisStop.NextStop = true;
                                prevWasBeforeNow = false;
                            }
                        }
                        //System.Diagnostics.Debug.WriteLine("train #: " + item.train_number);
                        //System.Diagnostics.Debug.WriteLine("depart time: " + item.departure_time);
                        //System.Diagnostics.Debug.WriteLine("arrive time: " + item.origination_time);
                        thisStop.ArrivalTime = GetTime(item.arrival_time);
                        thisStop.Direction = item.direction;
                        thisStop.Timing = item.Timing;
                        thisStop.TrainNumber = item.train_number;
                        thisStop.Origination = item.origination_stop;
                        thisStop.OriginationTime = GetTime(item.origination_time);
                        thisStop.DepartureTime = GetTime(item.departure_time);
                        thisStop.Destination = item.trip_headsign;
                        scheduledStops.Add(thisStop);
                    }
                }
                return Json(scheduledStops, "Text");
            }            
            catch (DataException ex)
            {
                log.LogString("Message: " + ex.Message);
                log.LogString("Inner: " + ex.InnerException.ToString());
                log.LogString("Source: " + ex.Source);
                throw ex;
                //return Json("");
            }
            catch (Exception ex)
            {
                log.LogString("Message: " + ex.Message);
                log.LogString("Inner: " + ex.InnerException.ToString());
                log.LogString("Source: " + ex.Source);
                throw ex;
            }
        }

        private string GetTime(string inputTime)
        {

            // if a time is after midnight, the schedule shows the 
            // hour as 24 or higher; this cannot be parsed, so we
            // need to adjust the hour value.
            try
            {
                System.Diagnostics.Debug.WriteLine(inputTime);
                //inputTime = DateTime.Parse(inputTime).ToString(@"HH\:mm\:ss tt");
                string hour = inputTime.Substring(0, 2);
                if (hour.Contains(":"))
                {
                    hour = "0" + hour.Substring(1, 1);
                } 
                if (hour.CompareTo("23") > 0)
                {
                    int intHour = Int16.Parse(hour);
                    intHour = intHour - 24;
                    inputTime = intHour.ToString() + inputTime.Substring(2, 6);
                }
                return DateTime.Parse(inputTime).ToString(@"hh\:mm\:ss tt");
            }
            catch (Exception ex)
            {
                return inputTime;
            }
        }

        private string GetJsonDataFromFile (string fileName)
        {
            string text;
            var fileStream = new FileStream(Server.MapPath(fileName), FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                text = streamReader.ReadToEnd();
            }
            return text;
        }

    }
}