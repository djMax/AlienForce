using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AlienForce.Utilities.NaturalLanguageSchedules
{
	class ScheduleParser
	{
		private static Regex mHoursPattern;
		private static Regex mDottedMeridiemPattern;

		static ScheduleParser()
		{
			StringBuilder timeDivision = new StringBuilder();

			// Don't add to these two without updating GetDay()
			string singleDays = "mo|mon|monday|mondays|tu|tue|tues|tuesday|tuesdays|wed|wednesday|wednesdays|th|thu|thur|thurs|thursday|thursdays|fr|fri|friday|fridays|sa|sat|satur|saturday|saturdays|su|sun|sunday|sundays";
			string spanDays = "m|mo|mon|monday|mondays|tu|tue|tues|tuesday|tuesdays|w|wed|wednesday|wednesday|th|thu|thur|thurs|thursday|thursdays|f|fr|fri|friday|fridays|sa|sat|satur|saturday|saturdays|su|sun|sunday|sundays";


			string rangeDelimiter = @"\s*?(\-|through|thru|to|till|until)\s*?";
			string untilDelimiter = @"\s*?(to|until|till?)\s*?";
			string singleMonths = @"jan|january|feb|february|mar|march|apr|april|may|jun|june|jul|july|aug|august|sep|sept|oct|october|nov|novem|november|dec|decemb|december";
			string date = @"(" + singleMonths + @")\.?\s*((?:0?[1-9])|(?:1[0-9])|(?:2[0-4])|(?:3[0-1]))(th|nd|st)?";

			string monthPrefixes = @"beginning of|the beginning of|mid|mid-|end of|the end of";

			string hours = @"(?:0?[1-9])|(?:1[0-9])|(?:2[0-4])";
			string minutes = @"[0-5][0-9]";
			string hoursMinutesSeparator = @"(?:[:\.h]";
			string meridiem = @"a|p|a\.?m\.?|p\.?m\.?";
			string times = @"noon|midnight|morning|evening|dusk|dawn|night"; // Various english times


			timeDivision.Append(@"(?<Hours>"); // First group is hours, like 9am-5pm
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the hours
			timeDivision.Append(@"(?:"); // The overall hours match
			timeDivision.Append(@"(?<StartTime>"); // The start time, like 9am
			timeDivision.Append(@"(?<StartHour>");
			timeDivision.Append(hours);
			timeDivision.Append(@")"); // The start hour, possibly military time
			timeDivision.Append(hoursMinutesSeparator); // A possible semi-colon or dot for minutes
			timeDivision.Append(@"(?<StartMinute>");
			timeDivision.Append(minutes);
			timeDivision.Append(@")"); // The minutes
			timeDivision.Append(@")?"); // Closing the optional minutes group
			timeDivision.Append(@"h?\s*"); // Possible spaces in between the time and the meridiem
			timeDivision.Append(@"(?<StartMeridiem>");
			timeDivision.Append(meridiem);
			timeDivision.Append(@")?"); // Possible meridiem
			timeDivision.Append(@"|"); // Or various other times
			timeDivision.Append(times);
			timeDivision.Append(@")"); // Close the start time
			timeDivision.Append(rangeDelimiter); // The dash in between, with optional spaces
			timeDivision.Append(@"(?<EndTime>"); // The end time, like 9am
			timeDivision.Append(@"(?<EndHour>(?:0?[1-9])|(?:1[0-9])|(?:2[0-4]))"); // The end hour, possibly military time
			timeDivision.Append(hoursMinutesSeparator); // A possible semi-colon for minutes
			timeDivision.Append(@"(?<EndMinute>");
			timeDivision.Append(minutes);
			timeDivision.Append(@")"); // The minutes
			timeDivision.Append(@")?"); // Closing the optional minutes group
			timeDivision.Append(@"h?\s*"); // Possible spaces in between the time and the meridiem
			timeDivision.Append(@"(?<EndMeridiem>");
			timeDivision.Append(meridiem);
			timeDivision.Append(@")?"); // Possible meridiem
			timeDivision.Append(@"|"); // Or various other times
			timeDivision.Append(times);
			timeDivision.Append(@")"); // Close the end time
			timeDivision.Append(@")"); // Close the overall hours
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the hours
			timeDivision.Append(@")"); // Close the hours group

			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<UntilHours>"); // A group that represents various ways of modifying previous hours
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the hours
			timeDivision.Append(untilDelimiter); // until
			timeDivision.Append(@"(?<UntilTime>"); // The end time, like 9am
			timeDivision.Append(@"(?<UntilHour>(?:0?[1-9])|(?:1[0-9])|(?:2[0-4]))"); // The end hour, possibly military time
			timeDivision.Append(hoursMinutesSeparator); // A possible semi-colon for minutes
			timeDivision.Append(@"(?<UntilMinute>");
			timeDivision.Append(minutes);
			timeDivision.Append(@")"); // The minutes
			timeDivision.Append(@")?"); // Closing the optional minutes group
			timeDivision.Append(@"h?\s*"); // Possible spaces in between the time and the meridiem
			timeDivision.Append(@"(?<UntilMeridiem>");
			timeDivision.Append(meridiem);
			timeDivision.Append(@")?"); // Possible meridiem
			timeDivision.Append(@"|"); // Or various other times
			timeDivision.Append(times);
			timeDivision.Append(@")"); // Close the end time
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the hours
			timeDivision.Append(@")"); // Close the UntilHours group
			
			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<AllDay>"); // A group that represents various ways of saying 24 hours
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the hours
			timeDivision.Append(@"24(\s*|-)?"); // 24, followed by optional spaces/hyphen
			timeDivision.Append(@"(?:hr|hrs|hours|/\s*7)"); // various spellings of "hours", or /7
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the hours
			timeDivision.Append(@")"); // Close the AllDay group
			
			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<Days>"); // Days of the week group
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the days
			timeDivision.Append(@"(?<StartDay>"); // Open the start day group
			timeDivision.Append(spanDays);
			timeDivision.Append(@")\.?"); // Close the start day group
			timeDivision.Append(rangeDelimiter); // The dash in between, with optional spaces
			timeDivision.Append(@"(?<EndDay>"); // Open the end day group
			timeDivision.Append(spanDays);
			timeDivision.Append(@")\.?"); // Close the end day group
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the days
			timeDivision.Append(@"|"); // Or perhaps just a single day
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the days
			timeDivision.Append(@"(?<SingleDay>"); // Open the single day group
			timeDivision.Append(singleDays);
			timeDivision.Append(@")\.?");// Close the single day group
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the days

			// Note: don't add to this list without checking GetDay()
			timeDivision.Append(@"|(?:^|\b)daily(?:\b|$)"); // Or daily
			timeDivision.Append(@"|(?:^|\b)every day(?:\b|$)"); // Or every day
			timeDivision.Append(@"|(?:^|\b)(?:seven|7) days(?: a week)?(?:\b|$)"); // Or seven days a week
			timeDivision.Append(@"|(?:^|\b)weekdays(?:\b|$)"); // Or weekdays
			timeDivision.Append(@"|(?:^|\b)weekends(?:\b|$)"); // Or weekends
			timeDivision.Append(@")"); // Close the Days group

			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<MonthSpan>"); // Span of months
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the months
			timeDivision.Append(@"(?:(?<StartMonthPrefix>");
			timeDivision.Append(monthPrefixes);
			timeDivision.Append(@")\s*)?");
			timeDivision.Append(@"(?<StartMonth>" + singleMonths + @")\.?");
			timeDivision.Append(rangeDelimiter); // The dash in between, with optional spaces
			timeDivision.Append(@"(?:(?<EndMonthPrefix>");
			timeDivision.Append(monthPrefixes);
			timeDivision.Append(@")\s*)?");
			timeDivision.Append(@"(?<EndMonth>" + singleMonths + @")\.?");
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the months
			timeDivision.Append(@")"); // Close the Single date group

			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<DateSpan>"); // Single date
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the date
			timeDivision.Append(@"(?<StartDateMonth>" + singleMonths + @")\.?\s*(?<StartDay>(?:0?[1-9])|(?:1[0-9])|(?:2[0-4])|(?:3[0-1]))(th|nd|st)?(?:\s+(?<StartYear>[0-9]{2,4}))?");
			timeDivision.Append(rangeDelimiter); // The dash in between, with optional spaces
			timeDivision.Append(@"(?<EndDateMonth>" + singleMonths + @")\.?\s*(?<EndDay>(?:0?[1-9])|(?:1[0-9])|(?:2[0-4])|(?:3[0-1]))(th|nd|st)?(?:\s+(?<EndYear>[0-9]{2,4}))?");
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the date
			timeDivision.Append(@")"); // Close the Single date group

			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<SingleDate>"); // Single date
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the date
			timeDivision.Append(@"(?<SingleDateMonth>" + singleMonths + @")\.?\s*(?<SingleDay>(?:0?[1-9])|(?:1[0-9])|(?:2[0-4])|(?:3[0-1]))(th|nd|st)?(?:\s+(?<SingleYear>\s+[0-9]{2,4}))?");
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the date
			timeDivision.Append(@")"); // Close the Single date group

			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<SingleMonth>"); // Single month
			timeDivision.Append(@"(?:^|\b)"); // word or string boundary on each side of the month
			timeDivision.Append(@"(?<SingleMonthMonth>" + singleMonths + @")\.?");
			timeDivision.Append(@"(?:\b|$)"); // word or string boundary on each side of the month
			timeDivision.Append(@")"); // Close the Single month group

			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<Separator>;|\n)");

			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<Closed>closed)");

			timeDivision.Append(@"|"); // Or...
			timeDivision.Append(@"(?<Open>open)");

			Console.WriteLine(timeDivision.ToString());
			mHoursPattern = new Regex(timeDivision.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);
			mDottedMeridiemPattern = new Regex(@"(?:(?<AM>a\.m\.)|(?<PM>p\.m\.))", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		}

		public Schedule Parse(string s)
		{
			Console.WriteLine(s);
			s = s.ToLowerInvariant();
			s = mDottedMeridiemPattern.Replace(s, delegate(Match meridienMatch)
			{
				Group am = meridienMatch.Groups["AM"];
				Group pm = meridienMatch.Groups["PM"];
				if (am.Success)
				{
					return "am";
				}
				if (pm.Success)
				{
					return "pm";
				}
				return meridienMatch.Value;
			});
			Console.WriteLine(s);
			List<ScheduleToken> tokens = new List<ScheduleToken>();
			Match m = mHoursPattern.Match(s);
			if (m.Success)
			{
				do
				{
					Console.WriteLine("\t{0}", m.Value);
					Group g = m.Groups["AllDay"];
					if (g.Success)
					{
						ScheduleToken allDayToken = new ScheduleToken(ScheduleTokenType.AllDay);
						allDayToken.StartHour = 0;
						allDayToken.EndHour = 0;
						tokens.Add(allDayToken);
						continue;
					}
					g = m.Groups["Days"];
					if (g.Success)
					{
						ParseDaysGroup(tokens, m, g);
						continue;
					}
					g = m.Groups["Hours"];
					if (g.Success)
					{
						ParseHoursGroup(tokens, m);
						continue;
					}
					g = m.Groups["UntilTime"];
					if (g.Success)
					{
						ParseUntilTimeGroup(tokens, m);
						continue;
					}
					g = m.Groups["MonthSpan"];
					if (g.Success)
					{
						ParseMonthSpanGroup(tokens, m);
						continue;
					}
					g = m.Groups["DateSpan"];
					if (g.Success)
					{
						ParseDateSpanGroup(tokens, m);
						continue;
					}
					g = m.Groups["SingleMonth"];
					if (g.Success)
					{
						ParseSingleMonthGroup(tokens, m);
						continue;
					}
					g = m.Groups["Separator"];
					if (g.Success)
					{
						tokens.Add(new ScheduleToken(ScheduleTokenType.Separator));
						continue;
					}
					g = m.Groups["Closed"];
					if (g.Success)
					{
						tokens.Add(new ScheduleToken(ScheduleTokenType.Closed));
						continue;
					}
					g = m.Groups["Open"];
					if (g.Success)
					{
						tokens.Add(new ScheduleToken(ScheduleTokenType.Open));
						continue;
					}
					
				}
				while ((m = m.NextMatch()) != null && m.Success);
			}
			PrintTokens(tokens);
			string leftovers = mHoursPattern.Replace(s, string.Empty).Trim();
			if (leftovers.Length > 0)
			{
				Console.WriteLine("Leftovers: {0}", leftovers);
			}
			Schedule schedule = ConvertTokensToSchedule(tokens);
			if (schedule != null)
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("Schedule:");
				foreach (ScheduleRuleSpan rule in schedule.mPositiveRules)
				{
					sb.Append("\t");
					rule.ToString(sb);
					sb.AppendLine();
				}
				sb.AppendLine("Negative");
				foreach (ScheduleRuleSpan rule in schedule.mNegativeRules)
				{
					sb.Append("\t");
					rule.ToString(sb);
					sb.AppendLine();
				}
				Console.WriteLine(sb.ToString());
			}
			return schedule;
		}

		private static void ParseUntilTimeGroup(List<ScheduleToken> tokens, Match m)
		{
			byte untilHour, untilMinute;
			HourMeridiem untilMeridiem;
			ParseTime(m, "UntilHour", "UntilMinute", "UntilMeridiem", "UntilTime", out untilHour, out untilMinute, out untilMeridiem);
			ScheduleToken untilToken = new ScheduleToken(ScheduleTokenType.Until);
			untilToken.EndHour = untilHour;
			untilToken.EndMinute = untilMinute;
			untilToken.EndMeridiem = untilMeridiem;
			tokens.Add(untilToken);
		}

		private static void ParseHoursGroup(List<ScheduleToken> tokens, Match m)
		{
			byte startHour, startMinute, endHour, endMinute;
			HourMeridiem startMeridiem, endMeridiem;
			ParseTime(m, "StartHour", "StartMinute", "StartMeridiem", "StartTime", out startHour, out startMinute, out startMeridiem);
			ParseTime(m, "EndHour", "EndMinute", "EndMeridiem", "EndTime", out endHour, out endMinute, out endMeridiem);
			tokens.Add(new ScheduleToken(startHour, startMinute, startMeridiem, endHour, endMinute, endMeridiem));
		}

		private static void ParseDaysGroup(List<ScheduleToken> tokens, Match m, Group daysGroup)
		{
			Group startDay = m.Groups["StartDay"];
			Group endDay = m.Groups["EndDay"];
			if (startDay.Success)
			{
				tokens.Add(new ScheduleToken(GetDay(startDay.Value), GetDay(endDay.Value)));
			}
			else
			{
				Group singleDay = m.Groups["SingleDay"];
				if (singleDay.Success)
				{
					tokens.Add(new ScheduleToken(GetDay(singleDay.Value)));
				}
				else
				{
					tokens.Add(new ScheduleToken(GetDay(daysGroup.Value)));
				}
			}
		}

		private static void ParseMonthSpanGroup(List<ScheduleToken> tokens, Match m)
		{
			Group startMonthGroup = m.Groups["StartMonth"];
			Group endMonthGroup = m.Groups["EndMonth"];
			Group startMonthPrefixGroup = m.Groups["StartMonthPrefix"];
			Group endMonthPrefixGroup = m.Groups["EndMonthPrefix"];
			if (startMonthGroup.Success)
			{
				if (startMonthPrefixGroup.Success || endMonthPrefixGroup.Success)
				{
					RuleMonth startMonth = GetMonth(startMonthGroup.Value);
					RuleMonth endMonth = GetMonth(endMonthGroup.Value);
					byte startDate;
					if (startMonthPrefixGroup.Success)
					{
						startDate = GetPrefixDate(startMonthPrefixGroup.Value, endMonth);
					}
					else
					{
						startDate = 1;
					}
					byte endDate;
					if (endMonthPrefixGroup.Success)
					{
						endDate = GetPrefixDate(endMonthPrefixGroup.Value, endMonth);
					}
					else
					{
						endDate = (byte)DateTime.DaysInMonth(DateTime.Now.Year, (int)endMonth);
					}
					tokens.Add(new ScheduleToken(startMonth, startDate, null, endMonth, endDate, null));
				}
				else
				{
					tokens.Add(new ScheduleToken(GetMonth(startMonthGroup.Value), GetMonth(endMonthGroup.Value)));
				}
			}
		}

		private static void ParseDateSpanGroup(List<ScheduleToken> tokens, Match m)
		{

			Group startMonthGroup = m.Groups["StartDateMonth"];
			Group startDayGroup = m.Groups["StartDay"];
			Group startYearGroup = m.Groups["StartYear"];
			Group endMonthGroup = m.Groups["EndDateMonth"];
			Group endDayGroup = m.Groups["EndDay"];
			Group endYearGroup = m.Groups["EndYear"];
			if (startMonthGroup.Success && startDayGroup.Success && endMonthGroup.Success && endDayGroup.Success)
			{
				short? startYear;
				if (startYearGroup.Success)
				{
					startYear = short.Parse(startYearGroup.Value);
				}
				else
				{
					startYear = null;
				}
				short? endYear;
				if (endYearGroup.Success)
				{
					endYear = short.Parse(endYearGroup.Value);
				}
				else
				{
					endYear = null;
				}
				tokens.Add(new ScheduleToken(GetMonth(startMonthGroup.Value), byte.Parse(startDayGroup.Value), startYear, GetMonth(endMonthGroup.Value), byte.Parse(endDayGroup.Value), endYear));
			}
		}

		private static void ParseSingleMonthGroup(List<ScheduleToken> tokens, Match m)
		{

			Group monthGroup = m.Groups["SingleMonthMonth"];
			if (monthGroup.Success)
			{
				tokens.Add(new ScheduleToken(GetMonth(monthGroup.Value)));
			}
		}

		private static void ParseDateGroup(List<ScheduleToken> tokens, Match m)
		{

			Group singleMonthGroup = m.Groups["SingleDateMonth"];
			Group singleDayGroup = m.Groups["SingleDay"];
			Group singleYearGroup = m.Groups["SingleYear"];
			if (singleMonthGroup.Success && singleDayGroup.Success)
			{
				short? singleYear;
				if (singleYearGroup.Success)
				{
					singleYear = short.Parse(singleYearGroup.Value);
				}
				else
				{
					singleYear = null;
				}
				tokens.Add(new ScheduleToken(GetMonth(singleMonthGroup.Value), byte.Parse(singleDayGroup.Value), singleYear));
			}
		}

		private static Schedule ConvertTokensToSchedule(List<ScheduleToken> tokens)
		{
			List<ScheduleRuleSpan> openSpans = new List<ScheduleRuleSpan>();
			List<ScheduleRuleSpan> closedSpans = new List<ScheduleRuleSpan>();
			List<ScheduleRule> openRules = new List<ScheduleRule>();
			List<ScheduleRule> closedRules = new List<ScheduleRule>();

			ScheduleTokenType firstTokenType = ScheduleTokenType.Unknown;
			ScheduleTokenClass firstTokenClass = ScheduleTokenClass.Unknown;
			ScheduleTokenType lastSeenTokenType = ScheduleTokenType.Unknown;
			ScheduleTokenClass lastSeenTokenClass = ScheduleTokenClass.Unknown;

			bool closed = false;
			Stack<ScheduleToken> dateStack = new Stack<ScheduleToken>();
			Stack<ScheduleToken> hoursStack = new Stack<ScheduleToken>();
			Stack<ScheduleToken> dayStack = new Stack<ScheduleToken>();
			foreach (ScheduleToken token in tokens)
			{
				if (firstTokenType == ScheduleTokenType.Unknown && !(token.Class == ScheduleTokenClass.Meta))
				{
					firstTokenType = token.Type;
					firstTokenClass = token.Class;
				}
				switch (token.Type)
				{
					case ScheduleTokenType.Open:
						ConvertStacksToRules(closed ? closedRules : openRules, dayStack, hoursStack);
						closed = false;
						break;
					case ScheduleTokenType.Closed:
						ConvertStacksToRules(closed ? closedRules : openRules, dayStack, hoursStack);
						closed = true;
						break;
					case ScheduleTokenType.Separator:
						ConvertStacksToRules(closed ? closedRules : openRules, dayStack, hoursStack);
						break;
					case ScheduleTokenType.AllDay:
					case ScheduleTokenType.HourRange:
					case ScheduleTokenType.SingleHour:
						if (lastSeenTokenClass == ScheduleTokenClass.DaysOfWeek)
						{
							if (firstTokenClass == ScheduleTokenClass.Hours)
							{
								ConvertStacksToRules(closed ? closedRules : openRules, dayStack, hoursStack);
							}
						}
						hoursStack.Push(token);
						break;
					case ScheduleTokenType.DayRange:
					case ScheduleTokenType.SingleDay:
						if (lastSeenTokenClass == ScheduleTokenClass.Hours)
						{
							if (firstTokenClass == ScheduleTokenClass.DaysOfWeek)
							{
								ConvertStacksToRules(closed ? closedRules : openRules, dayStack, hoursStack);
							}
						}
						dayStack.Push(token);
						break;
				}
				lastSeenTokenType = token.Type;
				lastSeenTokenClass = token.Class;
			}
			if (hoursStack.Count > 0)
			{
				if (dayStack.Count == 0)
				{
					dayStack.Push(new ScheduleToken(RuleDayOfWeek.Sunday, RuleDayOfWeek.Saturday));
				}
				ConvertStacksToRules(closed ? closedRules : openRules, dayStack, hoursStack);
			}
			if (openRules.Count > 0 || closedRules.Count > 0)
			{
				if (openRules.Count > 0)
				{
					openSpans.Add(new ScheduleRuleSpan(null, openRules.ToArray()));
				}
				if (closedRules.Count > 0)
				{
					closedSpans.Add(new ScheduleRuleSpan(null, closedRules.ToArray()));
				}
				return new Schedule(openSpans.ToArray(), closedSpans.ToArray());
			}
			return null;
		}

		private static void ConvertStacksToRules(List<ScheduleRule> rules, Stack<ScheduleToken> dayStack, Stack<ScheduleToken> hoursStack)
		{
			RuleDayOfWeek days = RuleDayOfWeek.None;
			int dayCount = dayStack.Count;
			if (dayCount > 0)
			{
				while (dayStack.Count > 0)
				{
					ScheduleToken dayToken = dayStack.Pop();
					switch (dayToken.Type)
					{
						case ScheduleTokenType.DayRange:
							days |= GetDayRange(dayToken.StartDay, dayToken.EndDay);
							break;
						case ScheduleTokenType.SingleDay:
							days |= dayToken.Day;
							break;
					}
				}
			}
			else
			{
				if (rules.Count > 0)
				{
					days = rules[rules.Count - 1].Days;
				}
				else
				{
					days = RuleDayOfWeek.AllWeek;
				}
			}
			if (hoursStack.Count == 0)
			{
				if (dayCount > 0)
				{
					rules.Add(new ScheduleRule(days, 0, 0));
				}
			}
			else
			{
				while (hoursStack.Count > 0)
				{
					ScheduleToken hourToken = hoursStack.Pop();
					ScheduleRule rule = ConvertHourTokenToRule(days, hourToken);
					if (rule != null)
					{
						rules.Add(rule);
					}
				}
			}
		}

		private static ScheduleRule ConvertHourTokenToRule(RuleDayOfWeek days, ScheduleToken hourToken)
		{
			switch (hourToken.Type)
			{
				case ScheduleTokenType.AllDay:
				case ScheduleTokenType.HourRange:
					byte startHour = hourToken.StartHour.Value;
					if (hourToken.StartMeridiem != HourMeridiem.None)
					{
						if (hourToken.StartMeridiem == HourMeridiem.AM)
						{
							if (startHour == 12)
							{
								startHour = 0;
							}
						}
						else if (startHour > 0 && startHour < 12)
						{
							startHour += 12;
						}
					}
					else if (startHour == 24)
					{
						startHour = 0;
					}
					short startMinute = (short)((startHour * 60) + (hourToken.StartMinute ?? 0));

					byte endHour = hourToken.EndHour.Value;
					if (hourToken.EndMeridiem != HourMeridiem.None)
					{
						if (hourToken.EndMeridiem == HourMeridiem.AM)
						{
							if (endHour == 12)
							{
								endHour = 0;
							}
						}
						else if (endHour > 0 && endHour < 12)
						{
							endHour += 12;
						}
					}
					else if (endHour == 24)
					{
						endHour = 0;
					}
					short endMinute = (short)((endHour * 60) + (hourToken.EndMinute ?? 0));
					if (hourToken.EndMeridiem == HourMeridiem.PM && hourToken.StartMeridiem == HourMeridiem.None && (startMinute + (12 * 60)) < endMinute && startMinute < 1440)
					{
						startMinute += (12 * 60);
					}
					else if (hourToken.EndMeridiem == HourMeridiem.None && startMinute > endMinute && startMinute < 720 && endMinute < 720)
					{
						endMinute += (12 * 60);
					}
					return new ScheduleRule(days, startMinute, endMinute);
			}
			return null;
		}

		private static RuleDayOfWeek GetDayRange(RuleDayOfWeek from, RuleDayOfWeek to)
		{
			RuleDayOfWeek totalDays = RuleDayOfWeek.None;
			int loopGuard = 0;
			while (true)
			{
				if (loopGuard++ > 500)
				{
					break;
				}
				totalDays |= from;
				if (from == to)
				{
					break;
				}
				if (from == RuleDayOfWeek.Saturday)
				{
					from = RuleDayOfWeek.Sunday;
				}
				else
				{
					from = (RuleDayOfWeek)((int)from << 1);
				}
			}
			return totalDays;
		}

		private static void ParseTime(Match m, string hourGroupName, string minuteGroupName, string meridiemGroupName, string timeGroupName, out byte hour, out byte minute, out HourMeridiem meridiem)
		{
			hour = 0;
			minute = 0;
			meridiem = HourMeridiem.None;
			Group hourGroup = m.Groups[hourGroupName];
			if (hourGroup.Success)
			{
				hour = byte.Parse(hourGroup.Value);
				Group minuteGroup = m.Groups[minuteGroupName];
				if (minuteGroup.Success)
				{
					minute = byte.Parse(minuteGroup.Value);
				}
				Group meridiemGroup = m.Groups[meridiemGroupName];
				if (meridiemGroup.Success)
				{
					meridiem = GetMeridiem(meridiemGroup.Value);
				}
			}
			else
			{
				Group timeGroup = m.Groups[timeGroupName];
				if (timeGroup.Success)
				{
					GetMinute(timeGroup.Value, out hour, out minute);
				}
			}
		}

		private void PrintTokens(List<ScheduleToken> tokens)
		{
			foreach (ScheduleToken token in tokens)
			{
				switch (token.Type)
				{
					case ScheduleTokenType.Open:
						Console.WriteLine("\t\tOpen");
						break;
					case ScheduleTokenType.Closed:
						Console.WriteLine("\t\tClosed");
						break;
					case ScheduleTokenType.Separator:
						Console.WriteLine("\t\tSeparator");
						break;
					case ScheduleTokenType.AllDay:
						Console.WriteLine("\t\tAll Day");
						break;
					case ScheduleTokenType.SingleDay:
						Console.WriteLine("\t\tDay: {0}", token.Day);
						break;
					case ScheduleTokenType.DayRange:
						Console.WriteLine("\t\tStart Day: {0}", token.StartDay);
						Console.WriteLine("\t\tEnd Day: {0}", token.EndDay);
						break;
					case ScheduleTokenType.HourRange:
						Console.WriteLine("\t\tStart Hour: {0}", token.StartHour);
						Console.WriteLine("\t\tStart Minute: {0}", token.StartMinute);
						Console.WriteLine("\t\tStart Meridiem: {0}", token.StartMeridiem);
						Console.WriteLine("\t\tEnd Hour: {0}", token.EndHour);
						Console.WriteLine("\t\tEnd Minute: {0}", token.EndMinute);
						Console.WriteLine("\t\tEnd Meridiem: {0}", token.EndMeridiem);

						break;
					case ScheduleTokenType.Until:
						Console.WriteLine("\t\tUntil");
						Console.WriteLine("\t\tEnd Hour: {0}", token.EndHour);
						Console.WriteLine("\t\tEnd Minute: {0}", token.EndMinute);
						Console.WriteLine("\t\tEnd Meridiem: {0}", token.EndMeridiem);
						break;
					case ScheduleTokenType.SingleMonth:
						Console.WriteLine("\t\tMonth: {0}", token.Month);
						break;
					case ScheduleTokenType.MonthRange:
						Console.WriteLine("\t\tStart Month: {0}", token.StartMonth);
						Console.WriteLine("\t\tEnd Month: {0}", token.EndMonth);
						break;
					case ScheduleTokenType.DateRange:
						Console.WriteLine("\t\tStart Month: {0}", token.StartMonth);
						Console.WriteLine("\t\tStart Day: {0}", token.StartDate);
						if (token.StartYear.HasValue)
						{
							Console.WriteLine("\t\tStart Year: {0}", token.StartYear);
						}
						Console.WriteLine("\t\tEnd Month: {0}", token.EndMonth);
						Console.WriteLine("\t\tEnd Day: {0}", token.EndDate);
						if (token.EndYear.HasValue)
						{
							Console.WriteLine("\t\tEnd Year: {0}", token.EndYear);
						}
						break;
					case ScheduleTokenType.SingleDate:
						Console.WriteLine("\t\tMonth: {0}", token.Month);
						Console.WriteLine("\t\tDay: {0}", token.Date);
						if (token.EndYear.HasValue)
						{
							Console.WriteLine("\t\tYear: {0}", token.Year);
						}
						break;
						break;

				}
			}
		}

		private static void GetMinute(string s, out byte hour, out byte min)
		{
			switch (s)
			{
				case "noon":
					hour = 12;
					min = 0;
					return;
				case "midnight":
					hour = 0;
					min = 0;
					return;
				case "morning":
					hour = 7;
					min = 0;
					return;
				case "evening":
					hour = 18;
					min = 0;
					return;
				case "dusk":
					hour = 17;
					min = 0;
					return;
				case "dawn":
					hour = 6;
					min = 0;
					return;
				case "night":
					hour = 20;
					min = 0;
					return;
			}
			throw new Exception("Unknown hour type: " + s);
		}

		private static HourMeridiem GetMeridiem(string meridiem)
		{
			if (meridiem[0] == 'a')
			{
				return HourMeridiem.AM;
			}
			else if (meridiem[0] == 'p')
			{
				return HourMeridiem.PM;
			}
			return HourMeridiem.None;
		}

		private static RuleDayOfWeek GetDay(string day)
		{
			switch (day[0])
			{
				case 'm': // Mondays
					return RuleDayOfWeek.Monday;
				case 't': // Tuesday and Thursday
					if (day[1] == 'u')
					{
						return RuleDayOfWeek.Tuesday;
					}
					return RuleDayOfWeek.Thursday;
				case 'w':
					if (day.Length < 2 || day[2] == 'd')
					{
						return RuleDayOfWeek.Wednesday;
					}
					if (day[4] == 'd') // "weekdays"
					{
						return RuleDayOfWeek.Weekdays;
					}
					return RuleDayOfWeek.Weekends;
				case 'f':
					return RuleDayOfWeek.Friday;
				case 's': // Saturday & Sunday && seven days
					if (day[1] == 'a')
					{
						return RuleDayOfWeek.Saturday;
					}
					if (day[1] == 'e')
					{
						return RuleDayOfWeek.AllWeek;
					}
					return RuleDayOfWeek.Sunday;
				case 'd': // Daily
				case 'e': // Every day
				case '7': // 7 days
					return RuleDayOfWeek.AllWeek;
					
			}
			/*switch (day)
			{
				case "m":
				case "mo":
				case "mon":
				case "monday":
				case "mondays":
					return HourRuleDayOfWeek.Monday;
				case "tu":
				case "tue":
				case "tues":
				case "tuesday":
				case "tuesdays":
					return HourRuleDayOfWeek.Tuesday;
				case "w":
				case "wed":
				case "wednesday":
				case "wednesdays":
					return HourRuleDayOfWeek.Wednesday;
				case "th":
				case "thu":
				case "thur":
				case "thurs":
				case "thursday":
				case "thursdays":
					return HourRuleDayOfWeek.Thursday;
				case "f":
				case "fr":
				case "fri":
				case "friday":
				case "fridays":
					return HourRuleDayOfWeek.Friday;
				case "sa":
				case "sat":
				case "satur":
				case "saturday":
				case "saturdays":
					return HourRuleDayOfWeek.Saturday;
				case "su":
				case "sun":
				case "sunday":
				case "sundays":
					return HourRuleDayOfWeek.Sunday;
				case "daily":
				case "every day":
					return HourRuleDayOfWeek.AllWeek;
				case "weekdays":
					return HourRuleDayOfWeek.Weekdays;
				case "weekends":
					return HourRuleDayOfWeek.Weekends;
			}
			return HourRuleDayOfWeek.None;*/
			throw new Exception("Unknown day pattern: " + day);
		}

		private static RuleMonth GetMonth(string month)
		{
			switch (month[0])
			{
				case 'j':
					if (month[1] == 'a')
					{
						return RuleMonth.January;
					}
					if (month[2] == 'l')
					{
						return RuleMonth.July;
					}
					return RuleMonth.June;
				case 'f':
					return RuleMonth.February;
				case 'm':
					if (month[2] == 'y')
					{
						return RuleMonth.May;
					}
					return RuleMonth.March;
				case 'a':
					if (month[1] == 'u')
					{
						return RuleMonth.August;
					}
					return RuleMonth.April;
				case 's':
					return RuleMonth.September;
				case 'o':
					return RuleMonth.October;
				case 'n':
					return RuleMonth.November;
				case 'd':
					return RuleMonth.December;
			}
			throw new Exception("Unknown month pattern: " + month);
		}


		private static RuleDayOfWeek GetDays(string fromDay, string toDay)
		{
			RuleDayOfWeek from = GetDay(fromDay);
			RuleDayOfWeek to = GetDay(toDay);
			RuleDayOfWeek totalDays = RuleDayOfWeek.None;
			int loopGuard = 0;
			while (true)
			{
				if (loopGuard++ > 500)
				{
					break;
				}
				totalDays |= from;
				if (from == to)
				{
					break;
				}
				if (from == RuleDayOfWeek.Saturday)
				{
					from = RuleDayOfWeek.Sunday;
				}
				else
				{
					from = (RuleDayOfWeek)((int)from << 1);
				}
			}
			return totalDays;
		}

		private static byte GetPrefixDate(string prefix, RuleMonth month)
		{
			switch (prefix[0])
			{
				case 'b': //beginning of
					return 1;
				case 'm': // middle
					return 15;
				case 'e': // end of
					return (byte)DateTime.DaysInMonth(DateTime.Now.Year, (int)month);
				case 't': // the beginning of or the end of
					if (prefix[4] == 'b')
						return 1;
					return (byte)DateTime.DaysInMonth(DateTime.Now.Year, (int)month);
			}
			throw new Exception("Unknown month prefix: " + prefix);
		}
	}
}
