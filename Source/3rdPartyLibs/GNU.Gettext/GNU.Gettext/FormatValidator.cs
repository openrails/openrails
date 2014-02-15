using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace GNU.Gettext
{
	public class FormatValidateResult
	{
		public bool Result { get { return ErrorType == FormatErrorType.None; } }
		public string ErrorMessage { get; set; }
		public FormatErrorType ErrorType { get; set; }
		
		public FormatValidateResult()
		{
			this.ErrorType = FormatErrorType.None;
		}
		
		public FormatValidateResult(FormatErrorType errorType)
		{
			this.ErrorType = errorType;
		}
	}
	
	public enum FormatErrorType
	{
		None,
		BracesCount,
		SingleBrace,
		InvalidFormatItem
	}
		
	public class FormatValidator
	{
		
		
		public string InputString { get; private set; }
		
		public FormatValidator(string inputString)
		{
			this.InputString = inputString == null ? String.Empty : inputString;
		}
		
		public bool ContainsFormat
		{
			get { return FormatItems.Length > 0; }
		}
		
		public static bool IsFormatString(string input)
		{
			return (new FormatValidator(input)).ContainsFormat;
		}

		public static bool IsValidFormatString(string input)
		{
			return (new FormatValidator(input)).Validate().Result;
		}
		
		public string[] FormatItems
		{
			get 
			{
				List<string> result = new List<string>();
				Regex r = new Regex(@"(?:[^\{]|^)\{\d+[^\{\}]*\}", RegexOptions.Multiline);
				MatchCollection matches = r.Matches(InputString);
				foreach (Match match in matches)
				{
				    result.Add(match.Value);
				}
				return result.ToArray();
			}
		}
		
		public FormatValidateResult Validate()
		{
			// Check singles braces match
			int count = 0;
			bool lastLeftBrace = false;
			bool lastRightBrace = false;
			for(int i = 0; i < InputString.Length; i++)
			{
				char c = InputString[i];
				switch(c)
				{
				case '{':
					if (lastLeftBrace)
					{
						lastLeftBrace = false; // skip "{{"
						count--;
					}
					else
					{
						lastLeftBrace = true;
						count++;
					}
					break;
				case '}':
					if (lastLeftBrace)
						return new FormatValidateResult(FormatErrorType.SingleBrace) { ErrorMessage = "Single braces are not allowed" };
					if (lastRightBrace)
					{
						lastRightBrace = false; // skip "}}"
						count++;
					}
					else
					{
						lastRightBrace = true;
						count--;
					}
					break;
				default:
					if (lastLeftBrace && !char.IsDigit(c))
						return new FormatValidateResult(FormatErrorType.InvalidFormatItem) { ErrorMessage = "Format item '{' must have a number" };
					lastLeftBrace = false;
					lastRightBrace = false;
					break;
				}
				if (count < 0 && !lastRightBrace)
					return new FormatValidateResult(FormatErrorType.BracesCount) { ErrorMessage = "Braces are not open properly" };
			}
			if (count != 0)
				return new FormatValidateResult(FormatErrorType.BracesCount) { ErrorMessage = "Braces are not closed properly" };
			
			return new FormatValidateResult();
		}
	}
}

