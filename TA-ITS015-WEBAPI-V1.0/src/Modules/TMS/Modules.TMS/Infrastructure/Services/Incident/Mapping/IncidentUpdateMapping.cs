using Modules.TMS.Core.Constants;
using Mapster;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Infrastructure.Services.Incident.Mapping;

public static class IncidentUpdateMapping
{
    private static readonly TypeAdapterConfig Mapping = CreateMapping();

    private static TypeAdapterConfig CreateMapping()
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.ForType<UpdateTmsIncidentInput, TmsIncident>()
            .Ignore(x => x.EventId)
            .Ignore(x => x.ConditionKey)
            .Ignore(x => x.Metadata);
        return config;
    }

    // Preserve worker-owned keys and metadata, including when request fields are null.
    public static void Apply(UpdateTmsIncidentInput input, TmsIncident existing)
    {
        var originalRemark = existing.Remark;
        var remark = input.Remark ?? originalRemark;
        // Policy/migration reasons are audit data; an unrelated edit must not silently remove them.
        foreach (var reason in new[] { IncidentRecordingConst.EventReason, IncidentRecordingConst.LegacyReason })
            if (originalRemark?.Contains(reason, StringComparison.Ordinal) == true)
                remark = IncidentRecordingConst.AppendReason(remark, reason);
        foreach (var reason in (originalRemark ?? "").Split('\n').Where(x => x.StartsWith("[Duplicate] ", StringComparison.Ordinal)))
            remark = IncidentRecordingConst.AppendReason(remark, reason);
        IncidentRemarkConst.Validate(remark);
        // Check the complete note before mutating any fields on the existing entity.
        input.Adapt(existing, Mapping);
        existing.Remark = remark;
    }
}
