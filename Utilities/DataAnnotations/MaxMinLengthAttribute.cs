using AlienForce.Utilities.DataAnnotations.Resources;

namespace AlienForce.Utilities.DataAnnotations
{
	public class MaxLengthAttribute : StringLengthAttribute
	{
		public MaxLengthAttribute(int len) : base(len)
		{

		}
	}

	public class MinLengthAttribute : StringLengthAttribute
	{
		public MinLengthAttribute(int len)
			: base(int.MaxValue)
		{
			MinimumLength = len;
		}
	}
}
