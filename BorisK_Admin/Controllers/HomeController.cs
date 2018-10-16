using BorisK_Admin.Models;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace BorisK_Admin.Controllers
{
    public class HomeController : Controller
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(HomeController));
        public ActionResult Index()
        {
            XmlConfigurator.Configure();
            ViewBag.Message = "";
            ViewBag.SuccessMessage = "";
            ViewBag.ErrorMessage = "";
            return View();
        }

        [HttpPost]
        public ActionResult Index(LoginModel model)
        {
            if (model.UserName == ConfigurationManager.AppSettings["app.username"].ToString() &&
                model.Password == ConfigurationManager.AppSettings["app.password"].ToString())
            {
                Session["isLogin"] = "T";
                ViewBag.Message = "";
                ViewBag.SuccessMessage = "";
                ViewBag.ErrorMessage = "";
                return RedirectToAction("FileUpload");
            }
            else
            {
                ViewBag.Message = ConfigurationManager.AppSettings["login.error.message"].ToString();
                ViewBag.SuccessMessage = "";
                ViewBag.ErrorMessage = "";
            }
            return View();
        }

        public ActionResult FileUpload()
        {
            if (Session["isLogin"] == null || Session["isLogin"].ToString() != "T")
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public ActionResult FileUpload(HttpPostedFileBase file, string Emails)
        {
            if (Session["isLogin"] == null || Session["isLogin"].ToString() != "T")
                return RedirectToAction("Index");

            try
            {
                var isEmailError = false;
                if(string.IsNullOrEmpty(Emails) && Emails.Trim().Length == 0)
                    isEmailError = true;
                else
                {
                    var emailList = Emails.Split(',');

                    if (emailList.Length == 0)
                        isEmailError = true;
                    else
                    {
                        foreach (var email in emailList)
                        {
                            if(!Regex.IsMatch(email, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase))
                                isEmailError = true;
                        }
                    }
                }

                if (isEmailError)
                {
                    ViewBag.SuccessMessage = "";
                    ViewBag.Message = "";
                    ViewBag.ErrorMessage = "Please enter valid email address/es ";
                }
                else
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        string _newFileId = Convert.ToString(Guid.NewGuid());
                        Log.Info("File uploaded: " + file.FileName + ", New FileId: " + _newFileId);

                        string _extension = System.IO.Path.GetExtension(file.FileName);
                        if (_extension.ToLower().EndsWith(".csv"))
                        {
                            string _path = Path.Combine(Server.MapPath("~/" + ConfigurationManager.AppSettings["upload.folder.name"].ToString()), _newFileId + _extension);
                            file.SaveAs(_path);

                            var reader = new StreamReader(System.IO.File.OpenRead(_path));
                            DataTable _table = new DataTable("ISBNs");
                            _table.Columns.Add("FileID");
                            _table.Columns.Add("ISBN");
                            int _failed = 0;
                            int _success = 0;
                            long _intISBN = 0;
                            bool _isHeaderPresent = false;
                            while (!reader.EndOfStream)
                            {
                                var _isbn = reader.ReadLine();
                                if (!_isHeaderPresent && _isbn.Trim() == "ISBN")
                                {
                                    _isHeaderPresent = true;
                                    continue;
                                }
                                if (_isbn.Trim().Length >= 10 && _isbn.Trim().Length < 14 && Int64.TryParse(_isbn, out _intISBN))
                                {
                                    DataRow _row = _table.NewRow();
                                    _row["FileID"] = _newFileId;
                                    _row["ISBN"] = _intISBN + "";
                                    _table.Rows.Add(_row);
                                    ++_success;
                                }
                                else if (_isbn.Trim().ToLower() != "isbn")
                                {
                                    ++_failed;
                                }
                            }

                            if (!_isHeaderPresent)
                            {
                                ViewBag.SuccessMessage = "";
                                ViewBag.Message = "";
                                ViewBag.ErrorMessage = "Error parsing CSV file";
                            }
                            else
                            {
                                bool _result = new FileUploadModel().Upload(file.FileName, _newFileId, _table, Emails);
                                if (_result)
                                {
                                    string _msg = ConfigurationManager.AppSettings["upload.success.message"].ToString();
                                    ViewBag.Message = _msg;
                                    if (_success > 0)
                                        ViewBag.SuccessMessage = " Success: " + _success + " ISBNs.";
                                    if (_failed > 0)
                                        ViewBag.ErrorMessage = " Failed: " + _failed + " ISBNs.";
                                    Log.Error("File uploaded: " + file.FileName + ", New FileId: " + _newFileId + ", Success: " + _msg + ViewBag.SuccessMessage + ViewBag.ErrorMessage);
                                }
                                else
                                {

                                    ViewBag.SuccessMessage = "";
                                    ViewBag.Message = "";
                                    ViewBag.ErrorMessage = ConfigurationManager.AppSettings["upload.error.message"].ToString();
                                }
                            }
                        }
                        else
                        {
                            ViewBag.SuccessMessage = "";
                            ViewBag.Message = "";
                            ViewBag.ErrorMessage = "Error parsing CSV file";//"Invalid File Extension";
                            Log.Error("File uploaded: " + file.FileName + ", New FileId: " + _newFileId + ", Error parsing CSV file");//Error: Invalid File Extension");
                        }
                    }
                    else
                    {
                        ViewBag.SuccessMessage = "";
                        ViewBag.Message = "";
                        ViewBag.ErrorMessage = "Please upload csv file";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception file upload. Exception: ", ex);

                ViewBag.SuccessMessage = "";
                ViewBag.ErrorMessage = ConfigurationManager.AppSettings["upload.error.message"].ToString();
                ViewBag.Message ="";
            }

            return View();
        }

        public ActionResult LogOut()
        {
            Session.Remove("isLogin");
            return RedirectToAction("Index");
        }
    }
}