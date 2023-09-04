
using GeoFenceCalculator;
using System.Globalization;

public class Program
{
    public static void Main(string[] args)
    {
        double totalHours = 0;

        var geofenceData = FileDataToList();
        var vehicleIds = geofenceData.Select(period => period.VehicleId).Distinct().ToList();
        var intervals = NumberOfIntervals(geofenceData);

        // Group intervals by VehicleId and calculate the sum of MinutesInterval for each group
        var vehicleIntervalSum = intervals
            .GroupBy(interval => interval.VehicleId)
            .Select(group => new { VehicleId = group.Key, TotalMinutesInterval = group.Sum(interval => interval.MinutesInterval) })
            .ToList();

        foreach (var vehicleId in vehicleIds)
        {
            var totalMinutesInterval = vehicleIntervalSum.FirstOrDefault(item => item.VehicleId == vehicleId)?.TotalMinutesInterval ?? 0;
            var totalHoursPerVehicle = totalMinutesInterval / 60.0; // Convert minutes to hours
            totalHours = totalHours + totalHoursPerVehicle;
        }

        // Print the results table header
        Console.WriteLine("Vehicles sold\tHours per week with no vehicle available");

        for (int i = 0; i < vehicleIds.Count; i++)
        {
            double weekHours = 42.5;
            double hoursAfterSell = 0;
            if (i == 0)
            {
                Console.WriteLine($"{i} \t\t {weekHours - totalHours}");
            }
            else if (i < vehicleIntervalSum.Count - 1)
            {
                totalHours += vehicleIntervalSum[i].TotalMinutesInterval / 60.0; // Convert minutes to hours
                hoursAfterSell = weekHours - totalHours;
                Console.WriteLine($"{i}\t\t{totalHours}");
            }
            else
            {
                Console.WriteLine($"{i}\t\t{totalHours}");
            }

        }
        Console.WriteLine($"{12}\t\t{42.5}");
    }

    public static List<GeofencePeriod> FileDataToList()
    {
        List<GeofencePeriod> geofenceData = new List<GeofencePeriod>();

        // Read and parse the CSV file
        using (StreamReader reader = new StreamReader("GeofencePeriods.csv"))
        {
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                string[] values = line?.Split(',');
                GeofencePeriod period = new GeofencePeriod
                {
                    VehicleId = int.Parse(values[0]),
                    EnterTime = DateTime.ParseExact(values[1], "yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    ExitTime = DateTime.ParseExact(values[2], "yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                };
                geofenceData.Add(period);
            }
        }
        return geofenceData;
    }

    public static List<TimeInterval> NumberOfIntervals(List<GeofencePeriod> geofenceData)
    {

        List<TimeInterval> vehicleCounts = new List<TimeInterval>();
        foreach (var period in geofenceData)
        {
            // Filter data to business hours for each day until exit time
            if ((period.EnterTime.DayOfWeek != DayOfWeek.Saturday && period.ExitTime.DayOfWeek != DayOfWeek.Saturday) && (period.EnterTime.DayOfWeek != DayOfWeek.Sunday && period.ExitTime.DayOfWeek != DayOfWeek.Sunday))
            {
                DateTime currentDate = period.EnterTime.Date;
                while (currentDate <= period.ExitTime.Date)
                {
                    DateTime startOfWorkingHours = currentDate.AddHours(8).AddMinutes(30);
                    DateTime endOfWorkingHours = currentDate.AddHours(17);

                    if (period.EnterTime < endOfWorkingHours && period.ExitTime > startOfWorkingHours)
                    {
                        var filteredEnterTime = (period.EnterTime >= startOfWorkingHours) && (period.EnterTime <= endOfWorkingHours) ? period.EnterTime : startOfWorkingHours;
                        var filteredExitTime = (period.ExitTime >= startOfWorkingHours) && (period.ExitTime <= endOfWorkingHours) ? period.ExitTime : endOfWorkingHours;

                        vehicleCounts.Add(MinutesIntervalsInSideGeoFence(filteredEnterTime, filteredExitTime, period.VehicleId));
                    }


                    currentDate = currentDate.AddDays(1); // Move to the next day
                }
            }

        }
        return vehicleCounts;
    }

    public static TimeInterval MinutesIntervalsInSideGeoFence(DateTime enterTime, DateTime exitTime, int vehicleId)
    {
        // Group data by 15-minutes periods

        double minutesIntervalsInSideGeoFence = Double.Parse((exitTime.TimeOfDay - enterTime.TimeOfDay).TotalMinutes.ToString()) / 15;

        var timeInterval = new TimeInterval
        {
            VehicleId = vehicleId,
            MinutesInterval = minutesIntervalsInSideGeoFence
        };

        return timeInterval;
    }

}