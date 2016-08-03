using Frontend.Models;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Frontend.Controllers
{
    public class FormController : Controller
    {

        // GET: /form?v=variables
        // POST: /form (by 'Download results', 'View results', 'Download request' or 'Upload request')
        public ActionResult Form()
        {
            var method = HttpContext.Request.HttpMethod;
            var config = WebApiApplication.GetExtendedFetchConfiguration(DateTime.MaxValue);
            if (method == "GET")
                return View("Form", new RequestFormModel(config, Request.QueryString, false));
            else if (method == "POST")
            {
                if (Request.Form["uploadRequest"] != null)
                {
                    if (Request.Files == null || Request.Files.Count < 1)
                    {
                        var model = new RequestFormModel(config);
                        model.RequestUploadErrors = "No file with request is specified";
                        return View("Form", model);
                    }
                    else
                        return View("Form", new RequestFormModel(config, Request.Files[0].InputStream));
                }
                else
                {
                    var model = new RequestFormModel(config, Request.Form, true);
                    if (model.HasErrors)
                        return View("Form", model);

                    if (Request.Form["downloadRequest"] != null)
                        return File(
                            Encoding.UTF8.GetBytes(model.GetRequestText()),
                            "text/plain",
                            "request.txt");
                    else if (Request.Form["view"] != null)
                        return Redirect("v1/FetchClimate2.html#" + model.GetClientUrlParameters());

                    // Download data
                    int minPtsPerPartition = FrontendSettings.Current.MinPtsPerPartition;
                    int maxPtsPerPartition = FrontendSettings.Current.MaxPtsPerPartition;
                    double jobRegistrationPermitedTime = FrontendSettings.Current.AllowedJobRegistrationSpan;
                    int totalWorkers = RoleEnvironment.Roles["FetchWorker"].Instances.Count;

                    string query = "";
                    var jobManager = WebApiApplication.GetSharedJobManager(HttpContext);
                    if (model.Points.Count > 0)
                    {
                        string points = "";
                        foreach (var fr in model.GetRequestsForPoints())
                        {
                            var jobStatus = jobManager.Submit(fr, fr.GetSHAHash(), jobRegistrationPermitedTime, minPtsPerPartition, maxPtsPerPartition, totalWorkers);
                            if (points.Length > 0)
                                points += ",";
                            points += jobStatus.Hash;
                        }
                        query += "?p=" + HttpUtility.UrlEncode(points);
                    }
                    int index = 1;
                    foreach (var g in model.Grids)
                    {
                        string hashes = "";
                        foreach (var fr in model.GetRequestsForGrid(g))
                        {
                            var jobStatus = jobManager.Submit(fr, fr.GetSHAHash(), jobRegistrationPermitedTime, minPtsPerPartition, maxPtsPerPartition, totalWorkers);
                            if (hashes.Length > 0)
                                hashes += ",";
                            hashes += jobStatus.Hash;
                        }
                        if (query.Length > 0)
                            query += "&";
                        else
                            query += "?";
                        query = String.Concat(query, "g", index++, "=", HttpUtility.UrlEncode(hashes));
                    }

                    return Redirect("results" + query);
                }
            }
            else
                throw new Exception("Method is not allowed");
        }
      
        
        // GET /results?p=requests&g=requests
        public ActionResult Results()
        {           
            List<RegionResultModel> regs = new List<RegionResultModel>();

            if (!String.IsNullOrEmpty(Request.QueryString["p"]))
                regs.Add(BuildRegionResultModel(Request.QueryString["p"], true));

            for (int index = 1; ; index++)
            {
                var param = String.Format("g{0}", index);
                if (!String.IsNullOrEmpty(Request.QueryString[param]))
                    regs.Add(BuildRegionResultModel(Request.QueryString[param], false));
                else
                    break;
            }

            return View("Results", new ResultModel(regs.ToArray()));
        }   
     
        private RegionResultModel BuildRegionResultModel(string hashes, bool isPointSet)
        {
            var jobManager = WebApiApplication.GetSharedJobManager(HttpContext);
            JobStatus[] stats = HttpUtility.UrlDecode(hashes).Split(',').Select(h => jobManager.GetStatus(h)).ToArray();
            if (stats.All(s => s.State == JobOrPartState.Failed))
                return new RegionResultModel(RegionResultStatus.Failed, null, isPointSet);
            else if (stats.All(s => s.State == JobOrPartState.Pending))
                return new RegionResultModel(RegionResultStatus.Pending, stats.Min(s => s.PendingCount), isPointSet);
            else if (stats.All(s => s.State == JobOrPartState.Completed))
                return new RegionResultModel(RegionResultStatus.Succeeded, hashes, isPointSet);
            else if (stats.All(s => s.State == JobOrPartState.Completed || s.State == JobOrPartState.Failed))
                return new RegionResultModel(RegionResultStatus.PartiallySucceeded, hashes, isPointSet);
            else
            {
                var prg = stats.Where(s => s.State == JobOrPartState.InProgress).ToArray();
                var completedCount = stats.Where(s => s.State == JobOrPartState.Completed).Count();
                var otherCount = stats.Where(s => s.State != JobOrPartState.Completed && s.State != JobOrPartState.InProgress).Count();
                return new RegionResultModel(RegionResultStatus.InProgress, (prg.Select(s => s.ProgressPercent).Sum() + 100 * completedCount) / (prg.Length + completedCount + otherCount), isPointSet);
            }
        }

        // GET /datasources
        public ActionResult DataSources()
        {
            return View("DataSources", WebApiApplication.GetExtendedFetchConfiguration(DateTime.MaxValue));
        }
    }
}
