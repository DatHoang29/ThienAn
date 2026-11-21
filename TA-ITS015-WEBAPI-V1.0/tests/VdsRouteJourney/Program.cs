using Modules.TMS.Core.Constants;
using Modules.VDS.Core.Topology;
using Services.Vds.Features.Journey;
using Shared.Utility.Counting;
using System.Text.Json;

var checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
var cameras = new[] {
    new TopoCamera("06", "VDS-06", "main_route", 1, 44, 920),
    new TopoCamera("04", "VDS-04", "main_route", 1, 17, 0),
    new TopoCamera("02", "VDS-02", "main_route", 1, 5, 800),
    new TopoCamera("01", "VDS-01", "main_route", 2, 4, 800),
    new TopoCamera("03", "VDS-03", "main_route", 2, 8, 100),
    new TopoCamera("05", "VDS-05", "main_route", 2, 17, 900),
    new TopoCamera("08", "VDS-08", "second_route", 1, 8, 140),
    new TopoCamera("10", "VDS-10", "second_route", 1, 14, 140)
};
var config = new List<TopoConfigRow> {
    new("vds_segment", "vds_km_dir_default_1", "desc"),
    new("vds_segment", "vds_km_dir_default_2", "asc"),
    new("vds_segment", "vds_km_dir_second_route_1", "asc"),
    new("vds_segment", "vds_km_dir_second_route_2", "desc")
};
var errors = new List<string>();
var sign = VdsRouteOrientation.Create(config, errors);
Check(errors.Count == 0 && sign("main_route", 1) == -1 && sign("main_route", 2) == 1, "Main route orientation");
Check(sign("second_route", 1) == 1 && sign("second_route", 2) == -1, "Branch overrides geographic direction");
Check(sign("SECOND_ROUTE", 1) == 1, "Route code normalization");
var plan = VdsSegmentTopology.Plan(cameras, config);
Check(!plan.HasErrors && plan.Segments.Count == 7, "All ordered same-route pairs, no arbitrary cross-route pairs");
PlannedSegment Pair(string a, string b) => plan.Segments.Single(p => p.FromId == a && p.ToId == b);
Check(Pair("06", "02").DistanceMeter == 39120 && Pair("01", "05").DistanceMeter == 13100, "Long distances");
Check(Pair("06", "04").DistanceMeter + Pair("04", "02").DistanceMeter == Pair("06", "02").DistanceMeter, "Adjacent pairs remain available");
Check(Pair("08", "10").DistanceMeter == 6000 && !plan.Segments.Any(p => p.FromId == "10" && p.ToId == "08"), "Branch direction is route-specific");
config.Add(new("vds_segment", "vds_junction_J", "{\"a\":\"main_route\",\"aPos\":10000,\"b\":\"second_route\",\"bPos\":7000}"));
config.Add(new("vds_segment", "vds_turn_J1", "{\"junction\":\"J\",\"in\":\"main_route\",\"inDir\":1,\"out\":\"second_route\",\"outDir\":1}"));
var turns = VdsSegmentTopology.Plan(cameras, config);
Check(!turns.HasErrors && turns.Segments.Count(p => p.Kind == "T") == 4, "All valid endpoints of a configured turn");
var turn = turns.Segments.Single(p => p.FromId == "06" && p.ToId == "10");
Check(turn.DistanceMeter == 42060 && turn.Direction == 1, "Long turn distance via junction");
config.Add(new("vds_segment", "vds_turn_J2", "{\"junction\":\"J\",\"in\":\"main_route\",\"inDir\":1,\"out\":\"second_route\",\"outDir\":1}"));
Check(VdsSegmentTopology.Plan(cameras, config).HasErrors, "Ambiguous paths rejected instead of picking shortest");
SegmentInfo Segment(PlannedSegment p) => new(p.Code, p.Code, p.FromId, p.ToId, p.Direction.ToString(), p.DistanceMeter, 1, 3600, true);
var auto = plan.Segments.Select(Segment).ToList();
var declared = Segment(Pair("06", "02")) with { SegmentId = "existing-id", IsAuto = false };
var merged = JourneySegmentResolver.Merge([declared], auto);
Check(merged.Count == auto.Count && merged.Single(s => s.FromEquipmentId == "06" && s.ToEquipmentId == "02").SegmentId == "existing-id", "Declared exact pair wins without changing ID");
Check(merged.Any(s => s.FromEquipmentId == "04" && s.ToEquipmentId == "02"), "Declared long pair does not remove adjacent pair ending at same B");
var utc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
var sightings = new[] {
    new JourneySighting("02", "30A12345", utc.AddSeconds(1800), "short"),
    new JourneySighting("04", "30A12345", utc.AddSeconds(1200), "short"),
    new JourneySighting("06", "30A12345", utc, "short")
};
var journey = JourneyMatcher.Match(declared, sightings).Single();
Check(journey.DistanceMeter == 39120 && journey.TravelSeconds == 1800 && Math.Abs(journey.AvgSpeedKmh - 78.24) < 0.001, "Whole journey measured from A/B rather than averaging leg speeds");
Check(JourneyMatcher.Match(declared, sightings.Where(s => s.EquipmentId != "04").ToList()).Single() == journey, "Missing intermediate camera does not discard full journey");
Check(JourneyMatcher.Match(declared, sightings.Where(s => s.EquipmentId != "06").ToList()).Count == 0, "Missing start cannot produce invented journey");
Check(JourneyArrivalProcessor.PickCandidate(declared, sightings, new("02", "30A12345", utc.AddSeconds(1800))) == journey, "Arrival and reconciliation agree");
Check(JourneyArrivalProcessor.PickDepartureCandidate(declared, sightings, new("06", "30A12345", utc)) == journey, "Late A and normal B agree");
Check(JourneyMatcher.Match(declared, sightings).SequenceEqual(JourneyMatcher.Match(declared, sightings.Reverse().ToList())), "Replay preserves deterministic candidate");
Check(!JourneyMatcher.Match(declared, [sightings[2], sightings[0] with { DetectTime = utc.AddSeconds(3601) }]).Single().IsValid, "Configured max window is respected on long routes");
Check(EquipmentDirectionCoverage.CanServe("1", "2", "1", true) && EquipmentDirectionCoverage.CanServe("2", "1", "2", true), "Mapped PTZ can serve opposite direction");
Check(!EquipmentDirectionCoverage.CanServe("1", "2", "1", false) && !EquipmentDirectionCoverage.CanServe("1", "2", "2", true), "Shared PTZ coverage stays limited to declared zone");
Check(EquipmentDirectionCoverage.CanServe("0", "2", "1", true) && EquipmentDirectionCoverage.CanServe(null, "1", "1", false), "All/unspecified target uses zone coverage");
TrafficDirectionJourney Measure(int distance, int seconds, double speed) => new() {
    FromEquipmentId = "06", ToEquipmentId = "02", Direction = "1", ToTime = utc.AddSeconds(1800),
    IsValid = true, DistanceMeter = distance, TravelSeconds = seconds, AvgSpeedKmh = speed
};
var averaged = TrafficDirectionSpeed.Calculate([Measure(39120, 1800, 78.24), Measure(200, 1800, 0.4), Measure(39120, 1800, 100)],
    ["06", "02"], "1", utc, utc.AddHours(1), 39120);
Check(averaged.Average == 78.2, "Dashboard excludes fake distances and inconsistent speed");
if (args.Length > 0)
{
    using var document = JsonDocument.Parse(File.ReadAllText(args[0]));
    var tables = document.RootElement.GetProperty("Tables");
    string? Str(JsonElement e, string key) => e.GetProperty(key).ValueKind == JsonValueKind.Null ? null : e.GetProperty(key).ToString();
    int? Int(JsonElement e, string key) => e.GetProperty(key).ValueKind == JsonValueKind.Null ? null : e.GetProperty(key).GetInt32();
    var liveCameras = tables.GetProperty("Equipment").EnumerateArray().Where(e => Str(e, "EquipmentType") == "VDS" && Int(e, "Status") == 1)
        .Select(e => new TopoCamera(Str(e, "ID")!, Str(e, "Code")!, Str(e, "Location"), Int(e, "DirectionId"), Int(e, "KmNumber"), Int(e, "MetNumber"))).ToList();
    var liveConfig = tables.GetProperty("VdsConfig").EnumerateArray().Where(e => Int(e, "Status") == 1)
        .Select(e => new TopoConfigRow(Str(e, "GroupCode"), Str(e, "Code"), Str(e, "Value"))).ToList();
    var live = VdsSegmentTopology.Plan(liveCameras, liveConfig);
    Check(!live.HasErrors, "Current DB topology is unambiguous: " + string.Join(";", live.Errors));
    Check(live.Segments.Single(s => s.FromCode == "VDS-06" && s.ToCode == "VDS-02").DistanceMeter == 39120
        && live.Segments.Single(s => s.FromCode == "VDS-01" && s.ToCode == "VDS-05").DistanceMeter == 13100, "Configured DB long pairs");
    if (args.Length > 1) File.WriteAllText(args[1], JsonSerializer.Serialize(live, new JsonSerializerOptions { WriteIndented = true }));
}
Console.WriteLine($"PASS: {checks} route, PTZ, long-journey and dashboard checks");
