using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;

namespace AlienForce.Utilities.NaturalLanguageSchedules
{
	public class ScheduleRule
	{
		public RuleDayOfWeek Days;
		public short StartMinute;
		public short EndMinute;

		public ScheduleRule(RuleDayOfWeek days, short startMinute, short endMinute)
		{
			Days = days;
			StartMinute = startMinute;
			EndMinute = endMinute;
		}

		public bool IsMatch(short minute, RuleDayOfWeek day)
		{
			if (StartMinute < EndMinute)
			{
				if ((Days & day) == day && minute > StartMinute && minute < EndMinute)
				{
					return true;
				}
			}
			else
			{
				if ((Days & day) == day && minute > StartMinute)
				{
					return true;
				}
				else
				{
					if (day == RuleDayOfWeek.Sunday)
					{
						day = RuleDayOfWeek.Saturday;
					}
					else
					{
						day = (RuleDayOfWeek)((short)day >> 1);
					}
					if ((Days & day) == day && minute < EndMinute)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void Compile(ILGenerator il)
		{
			Label done = il.DefineLabel();
			Label noMatch = il.DefineLabel();
			if (StartMinute < EndMinute)
			{
				Console.WriteLine("Simple rule");
				il.Emit(OpCodes.Ldc_I4, (int)Days);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.And);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Brfalse, noMatch);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, (int)StartMinute);
				il.Emit(OpCodes.Ble, noMatch);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, (int)EndMinute);
				il.Emit(OpCodes.Bge, noMatch);

				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Br, done);

				/*if ((Days & day) == day && minute > StartMinute && minute < EndMinute)
				{
					return true;
				}*/
			}
			else
			{
				Label earlierMatch = il.DefineLabel();
				il.Emit(OpCodes.Ldc_I4, (int)Days);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.And);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Brfalse, earlierMatch);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, (int)StartMinute);
				il.Emit(OpCodes.Ble, earlierMatch);

				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Br, done);

				il.MarkLabel(earlierMatch);

				Label checkEarlier = il.DefineLabel();
				Label shiftDay = il.DefineLabel();

				LocalBuilder newDay = il.DeclareLocal(typeof(int));

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldc_I4, (int)RuleDayOfWeek.Sunday);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Brfalse, shiftDay);
				il.Emit(OpCodes.Ldc_I4, (int)RuleDayOfWeek.Saturday);
				il.Emit(OpCodes.Stloc, newDay);
				il.Emit(OpCodes.Br, checkEarlier);

				il.MarkLabel(shiftDay);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldc_I4_1);

				il.Emit(OpCodes.Shr);
				il.Emit(OpCodes.Stloc, newDay);

				il.MarkLabel(checkEarlier);
				il.Emit(OpCodes.Ldc_I4, (int)Days);
				il.Emit(OpCodes.Ldloc, newDay);
				il.Emit(OpCodes.And);
				il.Emit(OpCodes.Ldloc, newDay);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Brfalse, noMatch);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, (int)EndMinute);
				il.Emit(OpCodes.Bge, noMatch);

				/*Label noMatch = il.DefineLabel();
				il.Emit(OpCodes.Ldc_I4, (int)Days);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.And);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Brfalse, noMatch);*/
				/*if ((Days & day) == day && minute > StartMinute)
				{
					return true;
				}
				else
				{
					if (day == RuleDayOfWeek.Sunday)
					{
						day = RuleDayOfWeek.Saturday;
					}
					else
					{
						day = (RuleDayOfWeek)((short)day << 1);
					}
					if ((Days & day) == day && minute < EndMinute)
					{
						return true;
					}
				}*/
				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Br, done);
			}
			il.MarkLabel(noMatch);
			il.Emit(OpCodes.Ldc_I4_0);
			il.MarkLabel(done);
		}

		public void ToString(StringBuilder o)
		{
			if (Days == RuleDayOfWeek.Weekdays)
			{
				o.Append("Weekdays ");
			}
			else if (Days == RuleDayOfWeek.AllWeek)
			{
				o.Append("Daily ");
			}
			else
			{
				o.Append(Days.ToString());
				o.Append(" ");
			}
			GetTime(StartMinute, o);
			o.Append('-');
			GetTime(EndMinute, o);
		}

		public static void GetTime(short time, StringBuilder o)
		{
			int h = (time / 60), m = time % 60;
			o.Append((h % 12) == 0 ? 12 : (h % 12));
			if (m != 0)
			{
				o.Append(':');
				if (m < 10)
				{
					o.Append('0');
				}
				o.Append(m);
			}
			o.Append(h >= 12 ? "pm" : "am");
		}
	}

	public interface IDateSpan
	{
		bool IsMatch(DateTime d);
	}

	public class DateRange : IDateSpan
	{
		public IScheduleDate StartDate;
		public IScheduleDate EndDate;

		#region IDateSpan Members

		public bool IsMatch(DateTime d)
		{
			return (StartDate.IsAfterOrOn(d) && EndDate.IsBeforeOrOn(d));
		}

		#endregion
	}

	public interface IScheduleDate
	{
		bool IsBeforeOrOn(DateTime d);
		bool IsAfterOrOn(DateTime d);
	}

	public class SimpleDate : IScheduleDate, IDateSpan
	{
		private int mMonth;
		private int mDay;

		public SimpleDate(int month, int day)
		{
			mMonth = month;
			mDay = day;
		}

		#region IScheduleDate Members

		public virtual bool IsMatch(DateTime d)
		{
			return (d.Month == mMonth && d.Day == mDay);
		}

		public virtual bool IsBeforeOrOn(DateTime d)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual bool IsAfterOrOn(DateTime d)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion
	}

	public class SpecificDate : SimpleDate
	{
		private int mYear;

		public SpecificDate(int month, int day, int year)
			: base(month, day)
		{
			mYear = year;
		}

		#region IScheduleDate Members

		public override bool IsMatch(DateTime d)
		{
			return (base.IsMatch(d) && d.Year == mYear);
		}

		public override bool IsBeforeOrOn(DateTime d)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public override bool IsAfterOrOn(DateTime d)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		#endregion
	}
}
