using FlightBookingAI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FlightBookingAI.Web.Controllers
{
    public class HomeController : Controller
    {
        private FlightBookingAIEntities db = new FlightBookingAIEntities();

        public ActionResult Index()
        {
            var flights = db.Flights
                .Include("Airline")
                .Include("Airport1")
                .Include("Airport")
                .Where(f =>
                    f.Status == "Scheduled" &&
                    f.DepartureTime > DateTime.Now &&
                    f.AvailableSeats > 0)
                .OrderBy(f => f.DepartureTime)
                .Take(10)
                .ToList();

            return View(flights);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}