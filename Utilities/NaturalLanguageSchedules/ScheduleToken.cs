using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.Utilities.NaturalLanguageSchedules
{
	public class ScheduleToken
	{
		public ScheduleTokenType Type;
		public ScheduleTokenClass Class;

		// Single day
		public RuleDayOfWeek Day;

		// Day range
		public RuleDayOfWeek StartDay;
		public RuleDayOfWeek EndDay;

		// Single time
		public byte? Hour;
		public byte? Minute;
		public HourMeridiem Meridiem;

		// Time range
		public byte? StartHour;
		public byte? StartMinute;
		public HourMeridiem StartMeridiem;
		public byte? EndHour;
		public byte? EndMinute;
		public HourMeridiem EndMeridiem;

		// Single date
		public RuleMonth Month;
		public byte? Date;
		public short? Year;

		// Date range
		public RuleMonth StartMonth;
		public byte? StartDate;
		public short? StartYear;
		public RuleMonth EndMonth;
		public byte? EndDate;
		public short? EndYear;

		public ScheduleToken(ScheduleTokenType type)
		{
			Type = type;
			switch (type)
			{
				case ScheduleTokenType.AllDay:
				case ScheduleTokenType.HourRange:
				case ScheduleTokenType.SingleHour:
				case ScheduleTokenType.Until:
					Class = ScheduleTokenClass.Hours;
					break;
				case ScheduleTokenType.SingleDate:
				case ScheduleTokenType.SingleMonth:
				case ScheduleTokenType.DateRange:
				case ScheduleTokenType.MonthRange:
					Class = ScheduleTokenClass.Dates;
					break;
				case ScheduleTokenType.SingleDay:
				case ScheduleTokenType.DayRange:
					Class = ScheduleTokenClass.DaysOfWeek;
					break;
				case ScheduleTokenType.Open:
				case ScheduleTokenType.Closed:
				case ScheduleTokenType.Separator:
					Class = ScheduleTokenClass.Meta;
					break;
				default:
					throw new Exception("No class defined for " + type);
			}
		}

		public ScheduleToken(RuleDayOfWeek singleDay)
			: this(ScheduleTokenType.SingleDay)
		{
			Day = singleDay;
		}

		public ScheduleToken(RuleDayOfWeek startDay, RuleDayOfWeek endDay)
			: this(ScheduleTokenType.DayRange)
		{
			StartDay = startDay;
			EndDay = endDay;
		}

		public ScheduleToken(byte? hour, byte? minute, HourMeridiem meridiem)
			: this(ScheduleTokenType.SingleHour)
		{
			Hour = hour;
			Minute = minute;
		}

		public ScheduleToken(byte? startHour, byte? startMinute, HourMeridiem startMeridiem, byte? endHour, byte? endMinute, HourMeridiem endMeridiem)
			: this(ScheduleTokenType.HourRange)
		{
			StartHour = startHour;
			StartMinute = startMinute;
			StartMeridiem = startMeridiem;
			EndHour = endHour;
			EndMinute = endMinute;
			EndMeridiem = endMeridiem;
		}

		public ScheduleToken(RuleMonth month)
			: this(ScheduleTokenType.SingleMonth)
		{
			Month = month;
		}

		public ScheduleToken(RuleMonth month, byte date, short? year)
			: this(ScheduleTokenType.SingleDate)
		{
			Month = month;
			Date = date;
			Year = year;
		}

		public ScheduleToken(RuleMonth startMonth, RuleMonth endMonth)
			: this(ScheduleTokenType.MonthRange)
		{
			StartMonth = startMonth;
			EndMonth = endMonth;
		}

		public ScheduleToken(RuleMonth startMonth, byte startDate, short? startYear, RuleMonth endMonth, byte endDate, short? endYear)
			: this(ScheduleTokenType.DateRange)
		{
			StartMonth = startMonth;
			StartDate = startDate;
			StartYear = startYear;
			EndMonth = endMonth;
			EndDate = endDate;
			EndYear = endYear;
		}
	}

	public enum ScheduleTokenType
	{
		Unknown = 0,
		AllDay = 1,
		SingleDay = 2,
		DayRange = 3,
		SingleHour = 4,
		HourRange = 5,
		Separator = 6,
		Closed = 7,
		Open = 8,
		Until = 9,
		SingleDate = 10,
		DateRange = 11,
		SingleMonth = 12,
		MonthRange = 13,
	}

	public enum ScheduleTokenClass
	{
		Unknown = 0,
		Hours = 1,
		DaysOfWeek = 2,
		Dates = 3,
		Meta = 4,
	}

	public enum HourMeridiem
	{
		None = 0,
		AM = 1,
		PM = 2,
	}

	public enum RuleMonth : short
	{
		None = 0,
		January = 1,
		February = 2,
		March = 3,
		April = 4,
		May = 5,
		June = 6,
		July = 7,
		August = 8,
		September = 9,
		October = 10,
		November = 11,
		December = 12,
	}

	[Flags]
	public enum RuleDayOfWeek : short
	{
		None = 0,
		Sunday = 1,
		Monday = Sunday * 2,
		Tuesday = Monday * 2,
		Wednesday = Tuesday * 2,
		Thursday = Wednesday * 2,
		Friday = Thursday * 2,
		Saturday = Friday * 2,
		Weekends = Sunday | Saturday,
		Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
		AllWeek = Weekends | Weekdays,
	}
}
