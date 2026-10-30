using FlightBookingAI.Web.Models;
using System.Linq;
using System.Web.Mvc;

namespace FlightBookingAI.Web.Controllers
{
    public class FlightsController : Controller
    {
        private FlightBookingAIEntities db = new FlightBookingAIEntities();
        // GET: Flights
        public ActionResult Index()
        {
            var flights = db.Flights.ToList();

            return View(flights);
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