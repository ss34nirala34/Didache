﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Didache {




	public class Unit {
		[Key]
		public int UnitID { get; set; }
		public int CourseID { get; set; }

		public bool IsActive { get; set; }

		[Required]
		public int SortOrder { get; set; }

		[Required]
		public string Name { get; set; }

		[Required]
		[Display(Name = "Start Date")]
		public DateTime StartDate { get; set; }

		[Required]
		[Display(Name = "End Date")]
		public DateTime EndDate { get; set; }

		[Required]
		[Display(Name = "Instructions")]
		public string Instructions { get; set; }

		public virtual Course Course { get; set; }

		public virtual ICollection<Task> Tasks { get; set; }
	}


}
