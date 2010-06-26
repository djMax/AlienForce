using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.Utilities.NaturalLanguageSchedules
{
	public class ScheduleRuleSpan
	{
		public IDateSpan[] Dates;
		public ScheduleRule[] Rules;

		public ScheduleRuleSpan(IDateSpan[] dates, ScheduleRule[] rules)
		{
			Dates = dates;
			Rules = rules;
		}

		public bool IsMatch(DateTime d)
		{
			if (Dates != null)
			{
				bool matchesDate = false;
				for (int i = 0; i < Dates.Length; i++)
				{
					IDateSpan dateSpan = Dates[i];
					if (dateSpan.IsMatch(d))
					{
						matchesDate = true;
						break;
					}
				}
				if (!matchesDate)
				{
					return false;
				}
			}
			RuleDayOfWeek day = (RuleDayOfWeek)(1 << (short)d.DayOfWeek);
			short minutes = (short)(d.Hour * 60 + d.Minute);
			for (int i = 0; i < Rules.Length; i++)
			{
				ScheduleRule rule = Rules[i];
				if (rule.IsMatch(minutes, day))
				{
					return true;
				}
			}
			return false;
		}

		public void ToString(StringBuilder o)
		{
			foreach (ScheduleRule rule in Rules)
			{
				rule.ToString(o);
			}
		}
	}
}
