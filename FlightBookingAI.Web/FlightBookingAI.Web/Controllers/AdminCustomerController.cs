using FlightBookingAI.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FlightBookingAI.Web.Controllers
{
    public class AdminCustomerController : Controller
    {
        private bool IsAdmin()
        {
            return Session["AdminId"] != null &&
                   Convert.ToString(Session["AdminRole"]) == "Admin";
        }

        // GET: AdminCustomer
        public ActionResult Index(string keyword)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            AdminCustomerListViewModel model =
                new AdminCustomerListViewModel();

            model.Keyword = keyword;

            string connectionString =
                ConfigurationManager.ConnectionStrings[
                    "FlightBookingAIConnection"
                ].ConnectionString;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT
                            UserID,
                            FullName,
                            Email,
                            Phone,
                            Role,
                            CreatedAt
                        FROM Users
                        WHERE Role = @Role
                          AND (
                              @Keyword = ''
                              OR FullName LIKE @SearchKeyword
                              OR Email LIKE @SearchKeyword
                              OR Phone LIKE @SearchKeyword
                          )
                        ORDER BY CreatedAt DESC";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        string searchKeyword =
                            keyword ?? "";

                        cmd.Parameters.AddWithValue(
                            "@Role", "Customer");

                        cmd.Parameters.AddWithValue(
                            "@Keyword", searchKeyword);

                        cmd.Parameters.AddWithValue(
                            "@SearchKeyword",
                            "%" + searchKeyword + "%");

                        conn.Open();

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                AdminCustomerItem customer =
                                    new AdminCustomerItem();

                                customer.UserID =
                                    Convert.ToInt32(reader["UserID"]);

                                customer.FullName =
                                    Convert.ToString(reader["FullName"]);

                                customer.Email =
                                    Convert.ToString(reader["Email"]);

                                customer.Phone =
                                    Convert.ToString(reader["Phone"]);

                                customer.Role =
                                    Convert.ToString(reader["Role"]);

                                customer.CreatedAt =
                                    Convert.ToDateTime(reader["CreatedAt"]);

                                model.Customers.Add(customer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["CustomerError"] =
                    "Không thể tải danh sách khách hàng: " + ex.Message;
            }

            return View("CustomerIndex", model);
        }

        // GET: AdminCustomer/Details/5
        public ActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            AdminCustomerDetailViewModel model = null;

            string connectionString =
                ConfigurationManager.ConnectionStrings[
                    "FlightBookingAIConnection"
                ].ConnectionString;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT
                            UserID,
                            FullName,
                            Email,
                            Phone,
                            Role,
                            CreatedAt
                        FROM Users
                        WHERE UserID = @UserID
                          AND Role = @Role";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", id);
                        cmd.Parameters.AddWithValue("@Role", "Customer");

                        conn.Open();

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model = new AdminCustomerDetailViewModel
                                {
                                    UserID = Convert.ToInt32(reader["UserID"]),
                                    FullName = Convert.ToString(reader["FullName"]),
                                    Email = Convert.ToString(reader["Email"]),
                                    Phone = Convert.ToString(reader["Phone"]),
                                    Role = Convert.ToString(reader["Role"]),
                                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                                };
                            }
                        }
                    }
                }

                if (model == null)
                {
                    return HttpNotFound();
                }

                return View("CustomerDetails", model);
            }
            catch (Exception ex)
            {
                TempData["CustomerError"] =
                    "Không thể tải thông tin khách hàng: " + ex.Message;

                return RedirectToAction("Index");
            }
        }

    }
}