/**
 * Token giả lập mặc định cho môi trường kiểm thử E2E
 */
export const MOCK_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQxMDI0NDQ4MDAsIm5hbWUiOiJhZG1pbiJ9.mock_signature';

/**
 * Cấu hình đầy đủ mặc định của Pinia themeConfig
 * Đảm bảo khi khởi tạo trong localStorage không bị thiếu các thuộc tính quan trọng
 * gây lỗi crash layout (như missing layout: 'defaults', isTagsview, v.v.)
 */
export const DEFAULT_THEME_CONFIG = {
    primary: '#1677FF',
    isDrawer: false,
    isIsDark: false,
    topBar: '#FFFFFF',
    topBarColor: '#000000',
    isTopBarColorGradual: false,
    menuBar: '#FFFFFF',
    menuBarColor: '#000000',
    menuBarActiveColor: 'var(--el-color-primary-light-7)',
    isMenuBarColorGradual: false,
    columnsMenuBar: '#2C3A49',
    columnsMenuBarColor: '#F0F0F0',
    isColumnsMenuBarColorGradual: false,
    isColumnsMenuHoverPreload: false,
    columnsLogoHeight: 50,
    columnsMenuWidth: 70,
    columnsMenuHeight: 50,
    isCollapse: false,
    isUniqueOpened: true,
    isFixedHeader: true,
    isFixedHeaderChange: false,
    isClassicSplitMenu: false,
    isLockScreen: false,
    lockScreenTime: 300,
    isShowLogo: true,
    isShowLogoChange: false,
    isBreadcrumb: false,
    isTagsview: true,
    isBreadcrumbIcon: false,
    numberBreadcrumb: 3,
    isTagsviewIcon: true,
    isCacheTagsView: true,
    isSortableTagsView: true,
    isShareTagsView: true,
    isFooter: true,
    isGrayscale: false,
    isInvert: false,
    isWatermark: false,
    watermarkText: '',
    tagsStyle: 'tags-style-one',
    animation: 'fade',
    columnsAsideStyle: 'columns-round',
    columnsAsideLayout: 'columns-vertical',
    layout: 'defaults',
    isRequestRoutes: true,
    globalTitle: 'TAC',
    globalViceTitle: 'TAC',
    globalViceTitleMsg: 'TAC',
    copyright: 'Copyright © 2024-TAC.',
    globalI18n: 'vi-VN',
    globalComponentSize: 'small',
    globalFont: 'Inter',
    logoUrl: '',
    loginLogo: false,
    icp: '',
    icpUrl: 'https://tacorp.vn',
};

/**
 * Thông tin người dùng mặc định có đầy đủ quyền thao tác
 */
export const DEFAULT_MOCK_USER = {
    account: 'admin',
    realName: 'Administrator',
    accountType: '111',
    roles: ['admin'],
    authBtnList: ['*'],
    defaultMenu: '/sharedata/codeSet',
};
