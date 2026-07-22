
using Furion.FriendlyException;
using System.ComponentModel;

namespace Shared.DTO.Enums;

/// <summary>
/// System error codes
/// </summary>
[ErrorCodeType]
[Description("System error codes")]
public enum ErrorCodeEnum
{
    /// <summary>
    /// Invalid verification code
    /// </summary>
    [ErrorCodeItemMetadata("Invalid verification code")]
    D0008,

    /// <summary>
    /// Account does not exist
    /// </summary>
    [ErrorCodeItemMetadata("Account does not exist")]
    D0009,

    /// <summary>
    /// Key does not match
    /// </summary>
    [ErrorCodeItemMetadata("Key does not match")]
    D0010,

    /// <summary>
    /// Incorrect password
    /// </summary>
    [ErrorCodeItemMetadata("Incorrect password")]
    D1000,

    /// <summary>
    /// Illegal operation! Deleting oneself is prohibited
    /// </summary>
    [ErrorCodeItemMetadata("Illegal operation! Deleting oneself is prohibited")]
    D1001,

    /// <summary>
    /// Record does not exist
    /// </summary>
    [ErrorCodeItemMetadata("Record does not exist")]
    D1002,

    /// <summary>
    /// Account already exists
    /// </summary>
    [ErrorCodeItemMetadata("Account already exists")]
    D1003,

    /// <summary>
    /// Incorrect old password
    /// </summary>
    [ErrorCodeItemMetadata("Incorrect old password")]
    D1004,

    /// <summary>
    /// Changing the password for the 'admin' user is prohibited for test data
    /// </summary>
    [ErrorCodeItemMetadata("Changing the password for the 'admin' user is prohibited for test data")]
    D1005,

    /// <summary>
    /// Data already exists
    /// </summary>
    [ErrorCodeItemMetadata("Data already exists")]
    D1006,

    /// <summary>
    /// Data does not exist or has associated references, deletion is prohibited
    /// </summary>
    [ErrorCodeItemMetadata("Data does not exist or has associated references, deletion is prohibited")]
    D1007,

    /// <summary>
    /// Prohibited from assigning roles to administrators
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from assigning roles to administrators")]
    D1008,

    /// <summary>
    /// Duplicate data or record contains non-existent data
    /// </summary>
    [ErrorCodeItemMetadata("Duplicate data or record contains non-existent data")]
    D1009,

    /// <summary>
    /// Prohibited from assigning permissions to super administrators
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from assigning permissions to super administrators")]
    D1010,

    /// <summary>
    /// Illegal operation, not logged in
    /// </summary>
    [ErrorCodeItemMetadata("Illegal operation, not logged in")]
    D1011,

    /// <summary>
    /// Id cannot be empty
    /// </summary>
    [ErrorCodeItemMetadata("Id cannot be empty")]
    D1012,

    /// <summary>
    /// The organization does not fall within the data scope of oneself
    /// </summary>
    [ErrorCodeItemMetadata("No permission to operate on this data")]
    D1013,

    /// <summary>
    /// Prohibited from deleting super administrators
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from deleting super administrators")]
    D1014,

    /// <summary>
    /// Prohibited from modifying the status of super administrators
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from modifying the status of super administrators")]
    D1015,

    /// <summary>
    /// No permission
    /// </summary>
    [ErrorCodeItemMetadata("No permission")]
    D1016,

    /// <summary>
    /// Account is frozen
    /// </summary>
    [ErrorCodeItemMetadata("Account is frozen")]
    D1017,

    /// <summary>
    /// Prohibited from deleting administrators
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from deleting administrators")]
    D1018,

    /// <summary>
    /// Prohibited from deleting system administrator roles
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from deleting system administrator roles")]
    D1019,

    /// <summary>
    /// Prohibited from modifying system administrator roles
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from modifying system administrator roles")]
    D1020,

    /// <summary>
    /// Prohibited from assigning permissions to system administrators
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from assigning permissions to system administrators")]
    D1021,

    /// <summary>
    /// Prohibited from assigning roles to super administrators
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from assigning roles to super administrators")]
    D1022,

    /// <summary>
    /// Prohibited from deleting the default tenant
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from deleting the default tenant")]
    D1023,

    /// <summary>
    /// Logged out the account from other locations
    /// </summary>
    [ErrorCodeItemMetadata("Logged out the account from other locations")]
    D1024,

    /// <summary>
    /// Prohibited from deleting accounts under this role
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from deleting accounts under this role")]
    D1025,

    /// <summary>
    /// Prohibited from modifying the account status of oneself
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from modifying the account status of oneself")]
    D1026,

    /// <summary>
    /// Too many incorrect password attempts, the account is locked, please retry after half an hour!
    /// </summary>
    [ErrorCodeItemMetadata("Too many incorrect password attempts, the account is locked, please retry after half an hour!")]
    D1027,

    /// <summary>
    /// The new password cannot be the same as the old password
    /// </summary>
    [ErrorCodeItemMetadata("The new password cannot be the same as the old password")]
    D1028,

    /// <summary>
    /// Prohibited from deleting the default system account
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from deleting the default system account")]
    D1029,

    /// <summary>
    /// Parent organization does not exist
    /// </summary>
    [ErrorCodeItemMetadata("Parent organization does not exist")]
    D2000,

    /// <summary>
    /// Current organization Id cannot be the same as the parent organization Id
    /// </summary>
    [ErrorCodeItemMetadata("Current organization Id cannot be the same as the parent organization Id")]
    D2001,

    /// <summary>
    /// Duplicate organization, same code or name already exists
    /// </summary>
    [ErrorCodeItemMetadata("Duplicate organization, same code or name already exists")]
    D2002,

    /// <summary>
    /// No permission to operate on the organization
    /// </summary>
    [ErrorCodeItemMetadata("No permission to operate on the organization")]
    D2003,

    /// <summary>
    /// Cannot delete the organization with users under it
    /// </summary>
    [ErrorCodeItemMetadata("Cannot delete the organization with users under it")]
    D2004,

    /// <summary>
    /// Cannot delete the subsidiary organization with users under it
    /// </summary>
    [ErrorCodeItemMetadata("Cannot delete the subsidiary organization with users under it")]
    D2005,

    /// <summary>
    /// Can only add subordinate organizations
    /// </summary>
    [ErrorCodeItemMetadata("Can only add subordinate organizations")]
    D2006,

    /// <summary>
    /// Cannot delete the organization with subordinate organizations under it
    /// </summary>
    [ErrorCodeItemMetadata("Cannot delete the organization with subordinate organizations under it")]
    D2007,

    /// <summary>
    /// Prohibited from deleting the default system organization
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from deleting the default system organization")]
    D2008,

    /// <summary>
    /// Prohibited from adding root node organizations
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from adding root node organizations")]
    D2009,

    /// <summary>
    /// Dictionary type does not exist
    /// </summary>
    [ErrorCodeItemMetadata("Dictionary type does not exist")]
    D3000,

    /// <summary>
    /// Dictionary type already exists, name or code is duplicated
    /// </summary>
    [ErrorCodeItemMetadata("Dictionary type already exists, name or code is duplicated")]
    D3001,

    /// <summary>
    /// Cannot delete the dictionary values under the dictionary type
    /// </summary>
    [ErrorCodeItemMetadata("Cannot delete the dictionary values under the dictionary type")]
    D3002,

    /// <summary>
    /// Dictionary value already exists, name or code is duplicated
    /// </summary>
    [ErrorCodeItemMetadata("Dictionary value already exists, name or code is duplicated")]
    D3003,

    /// <summary>
    /// Dictionary value does not exist
    /// </summary>
    [ErrorCodeItemMetadata("Dictionary value does not exist")]
    D3004,

    /// <summary>
    /// Incorrect dictionary status
    /// </summary>
    [ErrorCodeItemMetadata("Incorrect dictionary status")]
    D3005,

    /// <summary>
    /// Menu already exists
    /// </summary>
    [ErrorCodeItemMetadata("Menu already exists")]
    D4000,

    /// <summary>
    /// Route address is empty
    /// </summary>
    [ErrorCodeItemMetadata("Route address is empty")]
    D4001,

    /// <summary>
    /// Open mode is empty
    /// </summary>
    [ErrorCodeItemMetadata("Open mode is empty")]
    D4002,

    /// <summary>
    /// Permission identifier format is empty
    /// </summary>
    [ErrorCodeItemMetadata("Permission identifier format is empty")]
    D4003,

    /// <summary>
    /// Incorrect permission identifier format, e.g. xxx:xxx
    /// </summary>
    [ErrorCodeItemMetadata("Incorrect permission identifier format, e.g. xxx:xxx")]
    D4004,

    /// <summary>
    /// Permission does not exist
    /// </summary>
    [ErrorCodeItemMetadata("Permission does not exist")]
    D4005,

    /// <summary>
    /// The parent menu cannot be the current node, please select a different parent menu
    /// </summary>
    [ErrorCodeItemMetadata("The parent menu cannot be the current node, please select a different parent menu")]
    D4006,

    /// <summary>
    /// Cannot move the root node
    /// </summary>
    [ErrorCodeItemMetadata("Cannot move the root node")]
    D4007,

    /// <summary>
    /// Prohibited from having the same parent node as the current node
    /// </summary>
    [ErrorCodeItemMetadata("Prohibited from having the same parent node as the current node")]
    D4008,

    /// <summary>
    /// Duplicate route name
    /// </summary>
    [ErrorCodeItemMetadata("Duplicate route name")]
    D4009,

    /// <summary>
    /// The parent node cannot be of button type
    /// </summary>
    [ErrorCodeItemMetadata("The parent node cannot be of button type")]
    D4010,

    /// <summary>
    /// Application with the same name or code already exists
    /// </summary>
    [ErrorCodeItemMetadata("Application with the same name or code already exists")]
    D5000,

    /// <summary>
    /// Only one default active system is allowed
    /// </summary>
    [ErrorCodeItemMetadata("Only one default active system is allowed")]
    D5001,

    /// <summary>
    /// Cannot delete the application with menus under it
    /// </summary>
    [ErrorCodeItemMetadata("Cannot delete the application with menus under it")]
    D5002,

    /// <summary>
    /// Application with the same name or code already exists
    /// </summary>
    [ErrorCodeItemMetadata("Application with the same name or code already exists")]
    D5003,

    /// <summary>
    /// A position with the same name or code already exists
    /// </summary>
    [ErrorCodeItemMetadata("A position with the same name or code already exists")]
    D6000,

    /// <summary>
    /// This position cannot be deleted as it has associated users
    /// </summary>
    [ErrorCodeItemMetadata("This position cannot be deleted as it has associated users")]
    D6001,

    /// <summary>
    /// No permission to modify this position
    /// </summary>
    [ErrorCodeItemMetadata("No permission to modify this position")]
    D6002,

    /// <summary>
    /// Position does not exist
    /// </summary>
    [ErrorCodeItemMetadata("Position does not exist")]
    D6003,


}
