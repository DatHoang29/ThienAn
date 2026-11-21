// Loại cấu hình
export const CONFIG_TYPE = {
    ORG_TYPE: 'org_type',
    STATUS_TYPE: 'status_type',
    USER_TYPE: 'user_type',
    USER_STATUS: 'user_status',
    USER_ROLE: 'user_role',
    USER_PERMISSION: 'user_permission',
    USER_GROUP: 'user_group',
    USER_GROUP_TYPE: 'user_group_type',
    USER_GROUP_STATUS: 'user_group_status',
    USER_GROUP_PERMISSION: 'user_group_permission',
    USER_GROUP_ROLE: 'user_group_role',
    USER_GROUP_USER: 'user_group_user',
    USER_GROUP_USER_STATUS: 'user_group_user_status',
    USER_GROUP_USER_ROLE: 'user_group_user_role',
}

export type ConfigType = typeof CONFIG_TYPE;
