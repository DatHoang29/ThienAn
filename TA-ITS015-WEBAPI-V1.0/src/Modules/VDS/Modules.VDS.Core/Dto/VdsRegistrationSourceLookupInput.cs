namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Internal mediator lookup for a VDS-sourced registration created through the TMS API.
/// Created date: 02/10/2026
/// </summary>
public sealed record VdsRegistrationSourceLookupInput(string TenantId, long EventId, DateTime DetectTimeUtc);
