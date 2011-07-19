﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Didache.Web.Areas.Students.Controllers
{
    public class CoursesController : Controller
    {
		DidacheDb db = new DidacheDb();

		[Authorize]
		public ActionResult Index() {

			List<Course> courses = null;
			User profile = Users.GetLoggedInUser();

			if (profile != null)
				courses = Didache.Courses.GetUsersCourses(profile.UserID, CourseUserRole.Student);
			else
				courses = new List<Course>();

			return View(courses);
		}

		[Authorize]
		public ActionResult Dashboard(string slug) {

			Course course = Didache.Courses.GetCourseBySlug(slug);

			return View(course);
		}

		[Authorize]
		public ActionResult Tools(string slug) {

			Course course = Didache.Courses.GetCourseBySlug(slug);

			return View(course);
		}

		[Authorize]
		public ActionResult Schedule(string slug, int? id) {

			
			Course course = Didache.Courses.GetCourseBySlug(slug);
			User user = Users.GetLoggedInUser();
			List<Unit> units = Didache.Units.GetCourseUnits(course.CourseID);
			Unit currentUnit = null;
			List<UserTaskData> userTasks = null;			

			// (1) figure out which Unit to show (either by ID or date)
			if (id.HasValue) {
				// pick unit specified in URL
				currentUnit = units.AsQueryable().SingleOrDefault(u => u.UnitID == id.Value);
			} else {
				// pick current URL by date
				currentUnit = units.AsQueryable().SingleOrDefault(u => u.StartDate <= DateTime.Now && u.EndDate >= DateTime.Now);
			}

			// fallback to first unit
			if (currentUnit == null && units.Count > 0) {
				currentUnit = units[0];
			}


			// (2) get tasks
			if (currentUnit != null) {
				userTasks = Tasks.GetUserTaskDataInUnit(currentUnit.UnitID, user.UserID);
			}


			// (3) all other data
			// TODO: more efficient
			
			ViewBag.AllUserTasks = db.UserTasks.Where(ut => ut.UserID == user.UserID && ut.CourseID == course.CourseID).ToList();
            ViewBag.UserGroups = Didache.Courses.GetCourseUserGroups(course.CourseID);
			ViewBag.Units = units;
			ViewBag.CurrentUnit = currentUnit;
			ViewBag.UserTasks = userTasks;

			return View(course);
		}

		[Authorize]
		public ActionResult Files(string slug) {

			Course course = Didache.Courses.GetCourseBySlug(slug);

			ViewBag.CourseFileGroups = CourseFiles.GetCourseFileGroups(course.CourseID);

			return View(course);
		}

		[Authorize]
		public ActionResult Roster(string slug) {

			//List<CourseUser> users = Didache.Courses.GetUsersInCourse(Didache.Courses.GetCourseBySlug(slug).CourseID, CourseUserRole.Student);

			//List<CourseUserGroup> users = Didache.Courses.GetUsersInCourse(Didache.Courses.GetCourseBySlug(slug).CourseID, CourseUserRole.Student);

			Course course = Didache.Courses.GetCourseBySlug(slug);

			ViewBag.UserGroups = Didache.Courses.GetCourseUserGroups(course.CourseID);

			return View(course);
		}

		[Authorize]
		public ActionResult Assignments(string slug) {

			User user = Users.GetLoggedInUser();
            Course course = Didache.Courses.GetCourseBySlug(slug);

            //ViewBag.CourseFileGroups = CourseFiles.GetCourseFileGroups(course.CourseID);
            //ViewBag.StudentFiles = db.StudentFiles;


			ViewBag.AllUserTasks = db.UserTasks
										.Include("Task.Unit")
										.Include("StudentFile")
										.Include("GradedFile")
										.Where(ut => ut.UserID == user.UserID && ut.CourseID == course.CourseID).ToList();


			return View(course);
		}

		[Authorize]
		public ActionResult CourseFile(int id, string filename) {
			
			CourseFile file = new DidacheDb().CourseFiles.Find(id);

			string basePath = System.Configuration.ConfigurationManager.AppSettings["CourseFilesLocation"];
			string path = System.IO.Path.Combine(basePath, file.Filename);

			if (System.IO.File.Exists(path)) {

				return File(path, file.ContentType);
			} else {
				return HttpNotFound("Cannot find: " + file.Filename);
			}
		}

		[Authorize]
		public ActionResult StudentFile(int id, string filename) {

			StudentFile file = new DidacheDb().StudentFiles.Find(id);

			string path = file.PhysicalPath;

			if (System.IO.File.Exists(path)) {

				return File(path, file.ContentType);
			} else {
				return HttpNotFound("Cannot find: " + file.Filename);
			}
		}

		[Authorize]
		public ActionResult GradedFile(int id, string filename) {

			CourseFile file = new DidacheDb().CourseFiles.Find(id);

			string basePath = System.Configuration.ConfigurationManager.AppSettings["GradedFilesLocation"];
			string path = System.IO.Path.Combine(basePath, file.Filename);

			if (System.IO.File.Exists(path)) {

				return File(path, file.ContentType);
			} else {
				return HttpNotFound("Cannot find: " + file.Filename);
			}
		}

		[Authorize]
		public ActionResult DownloadAll(string slug) {

			Course course = Didache.Courses.GetCourseBySlug(slug);

			string basePath = System.Configuration.ConfigurationManager.AppSettings["StudentFilesLocation"];

			List<CourseFileGroup> groups = CourseFiles.GetCourseFileGroups(course.CourseID);

			// TODO: create zip file	
			
			return HttpNotFound("This doesn't work yet");
			
		}

		//
		// GET: /Feeds/
		public List<VideoInfo> GetVideoInfo(Course course) {
			List<VideoInfo> videosList = new List<VideoInfo>();

			string externalThumbBase = "http://dtsoe.s3.amazonaws.com"; // "http://oefiles.dts.edu";
			string xmlPath = System.Web.HttpContext.Current.Server.MapPath(String.Format("~/supportfiles/{0}/Titles/en-US.xml", course.CourseCode));


			if (Request.Url.Host == "online.dts.edu") {
				xmlPath = @"e:\websites\my.dts.edu\web\playerfiles\" + course.CourseCode + @"\titles\en-US.xml";
			}


			// load XML
			int selectedUnitNumber = -1;

			if (System.IO.File.Exists(xmlPath)) {
				System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
				doc.Load(xmlPath);

				Random random = new Random();


				System.Xml.XmlNodeList unitNodes = null;
				if (selectedUnitNumber > 0)
					doc.SelectNodes("//unit[@number=" + selectedUnitNumber + "]");
				else
					doc.SelectNodes("//unit");

				foreach (XmlNode uNode in unitNodes) {
					int unitNumber = 0;
					Int32.TryParse(uNode.Attributes["number"].Value, out unitNumber);
					System.Xml.XmlNodeList videoNodes = doc.SelectNodes("//unit[@number=" + unitNumber + "]/video");

					foreach (System.Xml.XmlNode vNode in videoNodes) {
						int videoNumber = 0;
						Int32.TryParse(vNode.Attributes["number"].Value, out videoNumber);

						videosList.Add(new VideoInfo {
							SortOrder = videoNumber++,
							UnitTaskInfo = "Unit " + unitNumber + ". Task " + videoNumber + ". ",
							Title = vNode.Attributes["name"].Value,
							Duration = vNode.Attributes["duration"].Value,
							VideoUrl = String.Format("{0}/{1}/{2}_u{3}_v{4}.mp4",
								externalThumbBase,
								course.CourseCode.ToLower(),
								course.CourseCode.ToUpper(),
								unitNumber.ToString().PadLeft(3, '0'),
								videoNumber.ToString().PadLeft(3, '0')),

							ThumbnailFilename = String.Format("{0}_u{1}_v{2}_thumb.jpg",
								course.CourseCode.ToUpper(),
								unitNumber.ToString().PadLeft(3, '0'),
								videoNumber.ToString().PadLeft(3, '0')),

							//ThumbnailUrl = String.Format("{0}/{1}/videos/{2}_u{3}_v{4}_thumb.jpg", locaThumbBase, Model.Course.CourseCode.ToLower(), Model.Course.CourseCode.ToUpper(), Model.Unit.UnitNumber.ToString().PadLeft(3, '0'), vNode.Attributes["number"].Value.ToString().PadLeft(3, '0')),
							ThumbnailUrl = String.Format("{0}/{1}/{2}_u{3}_v{4}_thumb.jpg",
								externalThumbBase,
								course.CourseCode.ToLower(),
								course.CourseCode.ToUpper(),
								unitNumber.ToString().PadLeft(3, '0'),
								videoNumber.ToString().PadLeft(3, '0')),

							PercentComplete = random.Next(100)
						});
					}
				}
			}
			else {
				//Response.Write("can't find: " + xmlPath);	
			}


			return videosList;

		}



		public ActionResult iCal(string slug, string type, int userID, string extension) {

			DidacheDb db = new DidacheDb();

			Course course = Didache.Courses.GetCourseBySlug(slug);
			User user = Users.GetUser(userID);

			List<UserTaskData> userTask = db.UserTasks
											.Include("Task.Unit")
											.Where(ut => ut.UserID == user.UserID && ut.CourseID == course.CourseID)
											.OrderBy(t => t.Task.Unit.SortOrder)
												.ThenBy(t => t.Task.SortOrder)
											.ToList();

			List<ICalEvent> iEvents = new List<ICalEvent>();

			foreach (UserTaskData task in userTask) {
				DateTime dueDate;

				if (task.Task.DueDate.HasValue)
					dueDate = task.Task.DueDate.Value;
				else
					dueDate = task.Task.Unit.EndDate;

				iEvents.Add(new ICalEvent() {
					Summary = task.Task.Name + ((task.TaskCompletionStatus == TaskCompletionStatus.Completed) ? " [completed]" : ""),
					Description = Utility.StripHtml(task.Task.Instructions),
					Location = "",
					StartUtc = dueDate.ToUniversalTime(),
					EndUtc = dueDate.ToUniversalTime()
				});
			}

			return new ICalEventResult(
						course.ToString() + " Tasks",
						"Dallas Theological Seminary",
						"", //"DTS-" + course.CourseCode + course.Section + ".ics",
						iEvents);

		}

		public ActionResult Rss(string slug, int userID, string type, string extension) {

			DidacheDb db = new DidacheDb();
			Course course = Didache.Courses.GetCourseBySlug(slug);
			User user = Users.GetUser(userID);

			List<SyndicationItem> items = new List<SyndicationItem>();

			foreach (VideoInfo videoInfo in GetVideoInfo(course)) {
				SyndicationItem item = new SyndicationItem(
						videoInfo.Title,
						videoInfo.Title,
						new Uri("http://site.com/"), // new Uri(Utility.BaseFullUrl + post.PostUrl),
						Guid.NewGuid().ToString(), // ?
						DateTime.Now // need to hook to unit start date
					);

				//item.PublishDate = post.PostDate;

				items.Add(item);
			}

			SyndicationFeed feed = new SyndicationFeed(
										course.ToString() + " :: RSS",
										"Video, Slides, and Transcripts",
										new Uri("http://online.dts.edu/"),
										items);



			return new SyndicationResult(feed, extension);
		}



		/*
		public ActionResult iCalUnits(string slug) {

			Course course = Didache.Courses.GetCourseBySlug(slug);

		}
		*/

    }


	public class SyndicationResult : ActionResult {
		public SyndicationFeed Feed { get; set; }
		public string Type { get; set; }

		public SyndicationResult() { }

		public SyndicationResult(SyndicationFeed feed, string type) {
			this.Feed = feed;
			this.Type = type;
		}

		public override void ExecuteResult(ControllerContext context) {

			SyndicationFeedFormatter formatter = null;

			switch (Type) {
				default:
				case "rss":
					context.HttpContext.Response.ContentType = "application/rss+xml";
					formatter = new Rss20FeedFormatter(this.Feed);


					break;
				case "atom":
					context.HttpContext.Response.ContentType = "application/atom+xml";
					formatter = new Atom10FeedFormatter(this.Feed);

					break;
			}

			using (XmlWriter writer = XmlWriter.Create(context.HttpContext.Response.Output)) {
				formatter.WriteTo(writer);
			}
		}
	}
}
