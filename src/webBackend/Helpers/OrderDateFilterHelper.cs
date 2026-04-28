namespace webBackend.Helpers
{
    public static class OrderDateFilterHelper
    {
        public static (DateTime? start, DateTime? end, string error)
            Resolve(DateTime? startDate, DateTime? endDate, string range)
        {
            DateTime today = DateTime.Today;

            switch (range)
            {
                case "7":
                    startDate = today.AddDays(-7);
                    endDate = today;
                    break;

                case "30":
                    startDate = today.AddDays(-30);
                    endDate = today;
                    break;

                case "thisMonth":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = today;
                    break;

                case "lastMonth":
                    var lastMonth = today.AddMonths(-1);
                    startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    endDate = startDate.Value.AddMonths(1).AddDays(-1);
                    break;

                case "thisYear":
                    startDate = new DateTime(today.Year, 1, 1);
                    endDate = today;
                    break;
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                if (startDate > endDate)
                    return (null, null, "Başlangıç tarihi bitiş tarihinden büyük olamaz.");
            }

            if (startDate > today || endDate > today)
                return (null, null, "Gelecek tarih seçilemez.");

            return (startDate, endDate, null);
        }
    }
}