﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Didache {
	public class Utility {

		public static string StripHtml(string input) {
			if (string.IsNullOrEmpty(input))
				return "";

			input = Regex.Replace(input, @"<(.|\n)*?>", " ");
			input = Regex.Replace(input, @"\s\s", " ");
			input = input.Replace("&nbsp;", " ").Trim();

			return input;
		}
	}
}
