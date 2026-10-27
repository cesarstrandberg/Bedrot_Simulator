using UnityEngine;

// Weekly rent - the money pressure the whole loop hangs off. Ticked by DayClock. At noon on the due
// day it's taken automatically if you can afford it. If not: late fee, a couple of days' grace, and
// the landlord gets less patient each time. State is static so it survives scene changes.
public static class Rent
{
    public const float WeeklyRent = 5000f;
    public const float LateFee = 500f;
    public const int GraceDays = 2;
    public const float DueHour = 12f;

    public static float amountDue = WeeklyRent;
    public static int dueDay = 7;
    public static int missedPayments;
    static int remindedForDay;

    public static void ResetAll()
    {
        amountDue = WeeklyRent;
        dueDay = 7;
        missedPayments = 0;
        remindedForDay = 0;
    }

    public static void Tick(int day, float hour)
    {
        // Heads-up the morning before
        if (day == dueDay - 1 && hour >= 8f && remindedForDay != dueDay)
        {
            remindedForDay = dueDay;
            Notify.Show($"Rent due tomorrow at {DueHour:00}:00 - {amountDue:0} kr");
        }

        if (day > dueDay || (day == dueDay && hour >= DueHour)) TryPay();
    }

    static void TryPay()
    {
        PlayerStats stats = Object.FindFirstObjectByType<PlayerStats>();
        if (stats == null) return;

        if (stats.money >= amountDue)
        {
            stats.money -= amountDue;
            GameState.rentPaid = true;
            Notify.Show($"Rent paid: -{amountDue:0} kr");
            missedPayments = 0;
            amountDue = WeeklyRent;
            dueDay += 7;
            Notify.Show($"Next rent: {amountDue:0} kr, Day {dueDay}");
            return;
        }

        missedPayments++;
        amountDue += LateFee;
        dueDay += GraceDays;

        string landlord = missedPayments switch
        {
            1 => "Landlord: \"Rent's late. I've added a fee. Don't make me come up there.\"",
            2 => "Landlord: \"Second time. Last warning.\"",
            _ => "Landlord: \"That's it. Eviction papers are being drawn up.\""
        };
        Notify.Show($"Couldn't pay rent! Now {amountDue:0} kr, due Day {dueDay}", 5f);
        Notify.Show(landlord, 6f);
    }
}
