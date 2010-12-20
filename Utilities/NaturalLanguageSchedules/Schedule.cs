using System;
using System.Reflection.Emit;

namespace AlienForce.Utilities.NaturalLanguageSchedules
{
	public delegate bool IsDateMatch(int day, int minute);

	public class Schedule
	{
		public ScheduleRuleSpan[] mPositiveRules;
		public ScheduleRuleSpan[] mNegativeRules;
		private IsDateMatch mMatch;

		public Schedule(ScheduleRuleSpan[] positiveRules, ScheduleRuleSpan[] negativeRules)
		{
			mPositiveRules = positiveRules;
			mNegativeRules = negativeRules;
			Compile();
		}

		public bool IsMatch(DateTime time)
		{
			var day = (RuleDayOfWeek)(1 << (short)time.DayOfWeek);
			short minutes = (short)(time.Hour * 60 + time.Minute);
			for (int i = 0; i < mNegativeRules.Length; i++)
			{
				if (mNegativeRules[i].IsMatch(time))
				{
					return false;
				}
			}
			for (int i = 0; i < mPositiveRules.Length; i++)
			{
				if (mPositiveRules[i].IsMatch(time))
				{
					return true;
				}
			}
			return false;
		}

		public bool IsCompiledMatch(DateTime time)
		{
			int day = (1 << (int)time.DayOfWeek);
			int minutes = (time.Hour * 60 + time.Minute);
			return mMatch(day, minutes);
		}

		private void Compile()
		{
			DynamicMethod isMatch = new DynamicMethod("IsMatch", typeof(bool), new Type[] { typeof(int), typeof(int) }, typeof(Schedule));
			ILGenerator il = isMatch.GetILGenerator();
			Label matched = il.DefineLabel();
			Label failed = il.DefineLabel();
			Console.WriteLine(mPositiveRules.Length);
			for (int i = 0; i < mPositiveRules.Length; i++)
			{
				ScheduleRuleSpan ruleSpan = mPositiveRules[i];
				for (int j = 0; j < ruleSpan.Rules.Length; j++)
				{
					ruleSpan.Rules[j].Compile(il);
					il.Emit(OpCodes.Brtrue, matched);
				}
			}
			il.Emit(OpCodes.Br, failed);
			il.MarkLabel(matched);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(failed);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ret);
			mMatch = (IsDateMatch)isMatch.CreateDelegate(typeof(IsDateMatch));
		}
	}
}
