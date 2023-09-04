
using GeoFenceCalculator;
using System.Globalization;

public class Program
{
    public static void Main(string[] args)
    {
        var geofenceData = FileDataToList();
        var vehicleIds = geofenceData.Select(period => period.VehicleId).Distinct().ToList();
        var filteredData = FilteredToBusinessHours(geofenceData, vehicleIds);

        var vehicleIdsIndex = 0;
        List<double> hoursVehiclesNotAvailable = new List<double>();
        while (vehicleIdsIndex != vehicleIds.Count)
        {

            var vehicleCounts = VehiclesOutSideGeoFence(filteredData, vehicleIds, vehicleIdsIndex);
            double totalHoursWithNoVehicles = vehicleCounts.Sum() * 15 / 60.0;// Convert to hours
            hoursVehiclesNotAvailable.Add(totalHoursWithNoVehicles);
            vehicleIdsIndex++;
        }

        Console.WriteLine("Vehicles sold\t\tHours per week with no vehicle available");
        for (int i = 0; i <= hoursVehiclesNotAvailable.Count - 1; i++)
        {
            Console.WriteLine($"{i}\t\t\t{hoursVehiclesNotAvailable[i]}");
        }

        /*// Print the results table
        Console.WriteLine("Vehicles sold\t\tHours per week with no vehicle available");
        for (int i = 0; i <= vehicleCounts.Count - 1; i++)
        {
            Console.WriteLine($"{i}\t\t\t{vehicleCounts[i] * 15 / 60.0}");// Convert to hours
        }

        // Calculate the sum of hours with no vehicles
        double totalHoursWithNoVehicles = vehicleCounts.Sum() * 15 / 60.0;// Convert to hours
        double totalWorkingHours = 42.5;
        Console.WriteLine($"Total\t{totalHoursWithNoVehicles}");*/
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

    public static List<GeofencePeriod> FilteredToBusinessHours(List<GeofencePeriod> geofenceData, List<int> vehicleIds)
    {

        // Filter data to business hours for each day until exit time
        List<GeofencePeriod> filteredData = new List<GeofencePeriod>();
        foreach (var period in geofenceData)
        {
            if ((period.EnterTime.DayOfWeek != DayOfWeek.Saturday && period.ExitTime.DayOfWeek != DayOfWeek.Saturday) && (period.EnterTime.DayOfWeek != DayOfWeek.Sunday && period.ExitTime.DayOfWeek != DayOfWeek.Sunday))
            {
                DateTime currentDate = period.EnterTime.Date;
                while (currentDate <= period.ExitTime.Date)
                {
                    DateTime startOfWorkingHours = currentDate.AddHours(8).AddMinutes(30);
                    DateTime endOfWorkingHours = currentDate.AddHours(17);

                    if (period.EnterTime < endOfWorkingHours && period.ExitTime > startOfWorkingHours)
                    {
                        DateTime filteredEnterTime = (period.EnterTime >= startOfWorkingHours) && (period.EnterTime <= endOfWorkingHours) ? period.EnterTime : startOfWorkingHours;
                        DateTime filteredExitTime = (period.ExitTime >= startOfWorkingHours) && (period.ExitTime <= endOfWorkingHours) ? period.ExitTime : endOfWorkingHours;

                        filteredData.Add(new GeofencePeriod
                        {
                            VehicleId = period.VehicleId,
                            EnterTime = filteredEnterTime,
                            ExitTime = filteredExitTime
                        });
                    }

                    currentDate = currentDate.AddDays(1); // Move to the next day
                }
            }

        }
        return filteredData;
    }

    public static List<int> VehiclesOutSideGeoFence(List<GeofencePeriod> filteredData, List<int> vehicleIds, int vehicleIdsIndex)
    {
        // Group data by 15-minutes periods and count unique vehicles
        DateTime startTime = filteredData.Min(period => period.EnterTime);
        DateTime endTime = filteredData.Max(period => period.ExitTime);
        TimeSpan interval = TimeSpan.FromMinutes(15);

        List<int> vehicleCounts = new List<int>();
        int vehicleIndex = 0;
        for (int i = 0; i <= vehicleIds.Count - 1; i++)
        {
            if (vehicleIndex == filteredData.Count())
            {
                break;
            }
            else
            {
                int hoursWithNoVehicles = 0;

                for (DateTime time = startTime; time < endTime; time += interval)
                {

                    if (vehicleIndex == filteredData.Count()) break;
                    if (filteredData.ElementAt(vehicleIndex).VehicleId == vehicleIds[i])
                    {
                        int vehiclesInside = filteredData
                        .Count(period => period.EnterTime >= time && period.ExitTime <= time.Add(interval));

                        if (vehiclesInside == 0)
                        {
                            hoursWithNoVehicles++;
                        }

                    }
                    else
                    {
                        vehicleIndex++;
                        break;

                    }
                    vehicleIndex++;

                }

                vehicleCounts.Add(hoursWithNoVehicles);
            }

        }
        return vehicleCounts;
    }
}