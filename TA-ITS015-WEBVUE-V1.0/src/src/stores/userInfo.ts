import { defineStore } from 'pinia';
import { Local, Session } from '/@/utils/storage';
import Watermark from '/@/utils/watermark';
import { useThemeConfig } from '/@/stores/themeConfig';

import { getAPI } from '/@/utils/axios-utils';
import { SysAuthApi } from '/@/api-services/system';
import { SysConfigTypeApi } from '/@/api-services/cfgSystem';
/**
 * User Information
 * @methods setUserInfos Set user information
 */
export const useUserInfo = defineStore('userInfo', {
	state: (): UserInfosState => ({
		userInfos: {} as any,
		constList: [] as any,
		dictList: {} as any,
		dictListInt: {} as any,
	}),
	getters: {
		// // Get system constant list
		// async getSysConstList(): Promise<any[]> {
		// 	var res = await getAPI(SysConstApi).apiSysConstListGet();
		// 	this.constList = res.data.result ?? [];
		// 	return this.constList;
		// },
	},
	actions: {
		// Store user information in browser cache
		async setUserInfos() {
			if (Session.get('userInfo')) {
				this.userInfos = Session.get('userInfo');
			} else {
				const userInfos = <UserInfos>await this.getApiUserInfo();
				this.userInfos = userInfos;
			}
		},

		// // Store constant information in browser cache
		// async setConstList() {
		// 	if (Session.get('constList')) {
		// 		this.constList = Session.get('constList');
		// 	} else {
		// 		const constList = <any[]>await this.getSysConstList();
		// 		Session.set('constList', constList);
		// 		this.constList = constList;
		// 	}
		// },

		// Store dictionary information in browser cache
		async setDictList() {
			var res = await getAPI(SysConfigTypeApi).apiCfgsystemSysconfigtypeListGet();
			this.dictList = res.data.result;
			if (Session.get('dictList')) {
				this.dictList = Session.get('dictList');
			} else {
				const dictList = <any[]>await this.getAllDictList();
				Session.set('dictList', dictList);
				this.dictList = dictList;
			}
			console.log(this.dictList);

		},

		// Get current user information
		getApiUserInfo() {
			return new Promise((resolve) => {
				getAPI(SysAuthApi)
					.apiSystemSysauthUserinfoGet()
					.then(async (res: any) => {
						if (res.data.result == null) return;
						var d = res.data.result;
						const userInfos = {
							id: d.id,
							account: d.account,
							realName: d.realName,
							phone: d.phone,
							idCardNum: d.idCardNum,
							email: d.email,
							accountType: d.accountType,
							avatar: d.avatar ?? '/Upload/defaultAvatar.png',
							address: d.address,
							signature: d.signature,
							orgId: d.orgId,
							orgName: d.orgName,
							posName: d.posName,
							roles: d.roleIds,
							authBtnList: d.buttons,
							time: new Date().getTime(),
							defaultMenu: d.defaultMenu,
						};

						// vue-next-admin submission Id: 225bce7 submission message: admin-23.03.26: release v2.4.32 version
						// The following code was added, causing the current session's user information not to refresh, for example: re-submitting the avatar does not update, a new page needs to be opened to display correctly
						// Session.set('userInfo', userInfos);

						// User watermark
						const storesThemeConfig = useThemeConfig();
						storesThemeConfig.themeConfig.watermarkText = d.watermarkText ?? '';
						if (storesThemeConfig.themeConfig.isWatermark) Watermark.set(storesThemeConfig.themeConfig.watermarkText);
						else Watermark.del();

						Local.remove('themeConfig');
						Local.set('themeConfig', storesThemeConfig.themeConfig);

						resolve(userInfos);
					});
			});
		},

		// // Get constant collection
		// getSysConstList() {
		// 	return new Promise((resolve) => {
		// 		getAPI(SysConstApi)
		// 			.apiSystemSysconstListGet()
		// 			.then(async (res: any) => {
		// 				resolve(res.data.result ?? []);
		// 			});
		// 	});
		// },

		// Get dictionary collection
		getAllDictList() {
			return new Promise((resolve) => {
				if (this.dictList) {
					resolve(this.dictList);
				} else {
					// 	getAPI(SysDictTypeApi)
					// 		.apiSystemSysdicttypeAlldictlistGet()
					// 		.then((res: any) => {
					// 			resolve(res.data.result ?? []);
					// 		});
				}
			});
		},

		// Get dictionary item by type and code
		getDictItemByCode(typePCode: string, val: string) {
			if (val != undefined && val !== '') {
				const _val = val.toString();
				const ds = this.getDictDatasByCode(typePCode);
				for (const element of ds) {
					if (element.code === _val) {
						return element;
					}
				}
			}
			return {};
		},

		// Get description by type and value
		getDictLabelByVal(typePCode: string, val: string) {
			if (val != undefined && val !== '') {
				const _val = val.toString();
				const ds = this.getDictDatasByCode(typePCode);
				for (const element of ds) {
					if (element.value === _val) {
						return element;
					}
				}
			}
			return {};
		},

		// Get value by type and description
		getDictValByLabel(typePCode: string, label: string) {
			if (!label) return '';
			const ds = this.getDictDatasByCode(typePCode);
			for (const element of ds) {
				if (element.name === label) {
					return element;
				}
			}
			return ''; // Explicitly return an empty string
		},

		// Get dictionary data by type
		getDictDatasByCode(dictTypeCode: string) {
			return this.dictList[dictTypeCode] || [];
		},

		// Get dictionary data by type (convert values to numeric type)
		getDictIntDatasByCode(dictTypeCode: string) {
			let ds = this.dictListInt[dictTypeCode];
			if (ds) {
				return ds;
			}

			const dictList = this.dictList[dictTypeCode];
			if (dictList) {
				ds = dictList.map((element: { code: any }) => {
					return { ...element, code: Number(element.code) };
				});
				this.dictListInt[dictTypeCode] = ds;
			}

			return ds;
		},
	},
});