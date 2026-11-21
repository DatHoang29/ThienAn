using Shared.Utility.Counting;

var now = new DateTime(2026, 10, 1, 7, 0, 0, DateTimeKind.Utc);
TrafficDirectionJourney Sample(string from, string to, int minutes, double? speed,
    bool? valid = true, string direction = "2") => new() {
        FromEquipmentId = from, ToEquipmentId = to, ToTime = now.AddMinutes(minutes),
        AvgSpeedKmh = speed, IsValid = valid, Direction = direction };
var rows = new[] {
    Sample("CAM1", "cam5", -5, 40), Sample("cam1", "cam5", -10, 80),
    Sample("cam5", "cam1", -5, 200), Sample("cam6", "cam2", -5, 120, direction: "1"),
    Sample("cam1", "cam5", -61, 300), Sample("cam1", "cam5", 0, 300),
    Sample("cam1", "cam5", -5, null), Sample("cam1", "cam5", -5, 300, false),
    Sample("cam1", "cam5", -5, 300, null), Sample("cam1", "cam5", -5, 300, direction: "1"),
    Sample("cam1", "cam5", -5, double.NaN), Sample("cam1", "cam5", -5, double.PositiveInfinity),
    Sample("cam1", "cam5", -5, -30), Sample("cam1", "cam3", -5, 300),
};
int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
var speed = TrafficDirectionSpeed.Calculate(rows, ["cam1", "cam5"], "2", now.AddHours(-1), now);
Check(speed.Average == 60, "Average valid completed journeys of exact ordered pair and direction");
Check(speed.LatestAt == now.AddMinutes(-5), "Latest timestamp");
var empty = TrafficDirectionSpeed.Calculate(rows, [], "2", now.AddHours(-1), now);
Check(empty == (null, null), "Unconfigured speed does not fall back to counting cameras");
var missing = TrafficDirectionSpeed.Calculate(rows, ["cam3", "cam4"], "2", now.AddHours(-1), now);
Check(missing == (null, null), "No journeys stays unknown");
var invalidPair = TrafficDirectionSpeed.Calculate(rows, ["cam1", "CAM1"], "2", now.AddHours(-1), now);
Check(invalidPair == (null, null), "Same camera pair rejected");
var three = TrafficDirectionSpeed.Calculate(rows, ["cam1", "cam5", "cam6"], "2", now.AddHours(-1), now);
Check(three == (null, null), "More than two cameras rejected");
var zero = TrafficDirectionSpeed.Calculate([Sample("cam1", "cam5", -5, 0)], ["cam1", "cam5"], "2", now.AddHours(-1), now);
Check(zero.Average == 0, "Measured zero is valid");
var boundary = TrafficDirectionSpeed.Calculate([Sample("cam1", "cam5", -60, 62.34)], ["cam1", "cam5"], "2", now.AddHours(-1), now);
Check(boundary.Average == 62.3, "Inclusive start and one-decimal rounding");
var directionOne = TrafficDirectionSpeed.Calculate(rows, ["cam6", "cam2"], "1", now.AddHours(-1), now);
Check(directionOne.Average == 120, "Second direction independently selects its ordered pair");
var reverse = TrafficDirectionSpeed.Calculate(rows, ["cam5", "cam1"], "2", now.AddHours(-1), now);
Check(reverse.Average == 200, "Pair order is significant");
Console.WriteLine($"PASS: {checks} direction speed regression checks");
