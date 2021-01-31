using System;

namespace API.Extensions
{
    public static class DateTimeExtensions
    {
        public static int CalculateAge(this DateTime dob)
        {
            var today = DateTime.Now;
            var age = today.Year - dob.Year;
            {
                if(dob.Date > today.AddYears(-age))
                {
                    return age-1;
                }
                return age;
            }
        }
    }
}