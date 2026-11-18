namespace HomeMatch.Modules.Billing.Domain.Services;

public static class InvoiceCalculator
{
    /// <summary>
    /// Tính tiền thuê phòng theo tháng hoặc prorate theo số ngày thuê thực tế (BR-BIL-08, BR-BIL-09, BR-BIL-10)
    /// </summary>
    public static long CalculateRent(
        long monthlyRent,
        DateOnly contractStartDate,
        DateOnly contractEndDate,
        int billingMonth,
        int billingYear)
    {
        if (monthlyRent <= 0)
        {
            return 0;
        }

        int daysInMonth = DateTime.DaysInMonth(billingYear, billingMonth);
        var startOfMonth = new DateOnly(billingYear, billingMonth, 1);
        var endOfMonth = new DateOnly(billingYear, billingMonth, daysInMonth);

        // Nằm ngoài thời gian thuê của hợp đồng
        if (contractEndDate < startOfMonth || contractStartDate > endOfMonth)
        {
            return 0;
        }

        // Trường hợp thuê trọn tháng
        if (contractStartDate <= startOfMonth && contractEndDate >= endOfMonth)
        {
            return monthlyRent;
        }

        // Trường hợp tháng đầu hoặc tháng cuối không trọn tháng
        var effectiveStart = contractStartDate > startOfMonth ? contractStartDate : startOfMonth;
        var effectiveEnd = contractEndDate < endOfMonth ? contractEndDate : endOfMonth;

        // Số ngày thuê thực tế bao gồm cả ngày bắt đầu và ngày kết thúc (BR-BIL-09)
        int actualDays = effectiveEnd.Day - effectiveStart.Day + 1;
        if (actualDays <= 0)
        {
            return 0;
        }

        if (actualDays >= daysInMonth)
        {
            return monthlyRent;
        }

        // Công thức: Giá thuê tháng / Tổng số ngày trong tháng * Số ngày thuê thực tế (làm tròn VND)
        double prorated = (double)monthlyRent / daysInMonth * actualDays;
        return (long)Math.Round(prorated, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Tính hạn thanh toán theo điều khoản hợp đồng (BR-BIL-11, BR-BIL-12)
    /// Nếu ngày đến hạn không tồn tại trong tháng, chuyển về ngày cuối cùng của tháng.
    /// </summary>
    public static DateOnly CalculateDueDate(int billingDueDay, int billingMonth, int billingYear)
    {
        int daysInMonth = DateTime.DaysInMonth(billingYear, billingMonth);
        int actualDueDay = Math.Clamp(billingDueDay, 1, daysInMonth);
        return new DateOnly(billingYear, billingMonth, actualDueDay);
    }
}
