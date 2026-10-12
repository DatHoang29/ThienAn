export interface MockUserInfo {
    account: string;
    realName: string;
    accountType?: string;
    roles: string[];
    authBtnList: string[];
    defaultMenu?: string;
    [key: string]: any;
}

export interface MockAuthOptions {
    token?: string;
    language?: string; // 'vi-VN' | 'en-US' | 'vi-vn' | 'en-us'
    userInfo?: Partial<MockUserInfo>;
    baseURL?: string;
}
