using ShareDataWorker.Infrastructure.Services.DataOutbound.Mapping;

namespace Tests.ShareData.Services
{
    /// <summary>
    /// Description: Bộ kiểm thử đơn vị cho tính năng hàm tổng hợp In-Memory (SUM, AVG, COUNT, MIN, MAX) trong DataMappingProcess (SV-14).
    /// Created date: 06/10/2026
    /// </summary>
    public class DataMappingAggregateTests
    {
        [Fact]
        public void Transform_WithHierarchicalShape_ComputesSumAndAvg_Test()
        {
            // Arrange
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["speed"] = 50 },
                new Dictionary<string, object?> { ["speed"] = 60 },
                new Dictionary<string, object?> { ["speed"] = 70 },
                new Dictionary<string, object?> { ["speed"] = 80 },
                new Dictionary<string, object?> { ["speed"] = 90 }
            };

            var targetShapeJson = """
            {
                "header": {
                    "totalSpeed": { "$field": "speed", "$extend": { "aggregate": "SUM" } },
                    "avgSpeed": { "$field": "speed", "$extend": { "aggregate": "AVG" } }
                },
                "data": [
                    { "speed": { "$field": "speed" } }
                ]
            }
            """;

            // Act
            var result = DataMappingProcess.Transform(rawRows, targetShapeJson);

            // Assert
            Assert.Single(result);
            var root = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.True(root.ContainsKey("header"));
            var header = Assert.IsAssignableFrom<IDictionary<string, object?>>(root["header"]);

            Assert.Equal(350.0, Convert.ToDouble(header["totalSpeed"]));
            Assert.Equal(70.0, Convert.ToDouble(header["avgSpeed"]));

            Assert.True(root.ContainsKey("data"));
            var dataList = Assert.IsAssignableFrom<IEnumerable<object>>(root["data"]);
            Assert.Equal(5, dataList.Count());
        }

        [Fact]
        public void Transform_WithAggregateCount_CountsRecordsAccurately_Test()
        {
            // Arrange
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["id"] = "A1" },
                new Dictionary<string, object?> { ["id"] = "A2" },
                new Dictionary<string, object?> { ["id"] = null },
                new Dictionary<string, object?> { ["id"] = "A4" },
                new Dictionary<string, object?> { ["id"] = "" }
            };

            var targetShapeJson = """
            {
                "totalCount": { "$extend": { "aggregate": "COUNT" } },
                "nonEmptyIdCount": { "$field": "id", "$extend": { "aggregate": "COUNT" } }
            }
            """;

            // Act
            var result = DataMappingProcess.Transform(rawRows, targetShapeJson);

            // Assert
            Assert.Single(result);
            var root = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(5, Convert.ToInt32(root["totalCount"]));
            Assert.Equal(3, Convert.ToInt32(root["nonEmptyIdCount"]));
        }

        [Fact]
        public void Transform_WhenRawRowsEmpty_HandlesAggregatesSafelyWithoutDivisionByZero_Test()
        {
            // Arrange
            var rawRows = new List<object>();

            var targetShapeJson = """
            {
                "count": { "$extend": { "aggregate": "COUNT" } },
                "sum": { "$field": "val", "$extend": { "aggregate": "SUM" } },
                "avg": { "$field": "val", "$extend": { "aggregate": "AVG" } }
            }
            """;

            // Act
            var result = DataMappingProcess.Transform(rawRows, targetShapeJson);

            // Assert
            Assert.Single(result);
            var root = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(0, Convert.ToInt32(root["count"]));
            Assert.Equal(0.0, Convert.ToDouble(root["sum"]));
            Assert.Null(root["avg"]);
        }

        [Fact]
        public void Transform_WithNullAndNonNumericValues_IgnoresThemSafely_Test()
        {
            // Arrange
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["speed"] = 100 },
                new Dictionary<string, object?> { ["speed"] = null },
                new Dictionary<string, object?> { ["speed"] = "abc" },
                new Dictionary<string, object?> { ["speed"] = 50 }
            };

            var targetShapeJson = """
            {
                "sum": { "$field": "speed", "$extend": { "aggregate": "SUM" } },
                "avg": { "$field": "speed", "$extend": { "aggregate": "AVG" } },
                "min": { "$field": "speed", "$extend": { "aggregate": "MIN" } },
                "max": { "$field": "speed", "$extend": { "aggregate": "MAX" } }
            }
            """;

            // Act
            var result = DataMappingProcess.Transform(rawRows, targetShapeJson);

            // Assert
            Assert.Single(result);
            var root = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(150.0, Convert.ToDouble(root["sum"]));
            Assert.Equal(75.0, Convert.ToDouble(root["avg"]));
            Assert.Equal(50.0, Convert.ToDouble(root["min"]));
            Assert.Equal(100.0, Convert.ToDouble(root["max"]));
        }

        [Fact]
        public void Transform_WithNumberFormat_FormatsAggregateResultCorrectly_Test()
        {
            // Arrange
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["speed"] = 10 },
                new Dictionary<string, object?> { ["speed"] = 20 },
                new Dictionary<string, object?> { ["speed"] = 25 }
            };

            var targetShapeJson = """
            {
                "avgSpeed": {
                    "$field": "speed",
                    "$extend": {
                        "aggregate": "AVG",
                        "numberFormat": "0.00"
                    }
                }
            }
            """;

            // Act
            var result = DataMappingProcess.Transform(rawRows, targetShapeJson);

            // Assert
            Assert.Single(result);
            var root = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("18.33", root["avgSpeed"]?.ToString());
        }

        [Fact]
        public void Transform_WithExpressionAggregate_ComputesCorrectly_Test()
        {
            // Arrange
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["speed"] = 10 },
                new Dictionary<string, object?> { ["speed"] = 30 }
            };

            var targetShapeJson = """
            {
                "avgSpeed": {
                    "$extend": {
                        "expression": "AVG(speed)"
                    }
                },
                "totalSpeed": {
                    "$extend": {
                        "expression": "SUM(speed)"
                    }
                }
            }
            """;

            // Act
            var result = DataMappingProcess.Transform(rawRows, targetShapeJson);

            // Assert
            Assert.Single(result);
            var root = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(20.0, Convert.ToDouble(root["avgSpeed"]));
            Assert.Equal(40.0, Convert.ToDouble(root["totalSpeed"]));
        }
    }
}
