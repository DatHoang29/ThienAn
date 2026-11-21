import { getAPI } from '/@/utils/axios-utils';
import { RptEmailScheduleApi, RptEmailTemplateApi } from '/@/api-services/mail';
import type { RptEmailTemplateOutput, MailTagInfo } from '/@/api-services/mail';

/** Marks mail tag drags separately from ordinary text, images and files. */
export const MAIL_TAG_MIME = 'application/x-mail-tag';

let cacheVersion = 0;
let templates: RptEmailTemplateOutput[] | null = null;
let templatesInflight: Promise<RptEmailTemplateOutput[]> | null = null;

/** Templates and their editable content from the template CRUD API. */
export function loadMailTemplates(): Promise<RptEmailTemplateOutput[]> {
	if (templates) return Promise.resolve(templates);
	if (!templatesInflight) {
		const version = cacheVersion;
		templatesInflight = getAPI(RptEmailTemplateApi)
			.apiMailRptemailtemplateListGet(undefined, 1)
			.then((res) => {
				const result = (res.data.result ?? []).filter((tpl) => tpl.code && tpl.status === 1);
				if (version === cacheVersion) templates = result;
				return result;
			})
			.finally(() => { if (version === cacheVersion) templatesInflight = null; });
	}
	return templatesInflight;
}

let tags: MailTagInfo[] | null = null;
let tagsInflight: Promise<MailTagInfo[]> | null = null;

export function loadMailTags(): Promise<MailTagInfo[]> {
	if (tags) return Promise.resolve(tags);
	if (!tagsInflight) {
		const version = cacheVersion;
		tagsInflight = getAPI(RptEmailScheduleApi)
			.apiMailRptemailscheduleTagsGet()
			.then((res) => {
				const result = res.data.result ?? [];
				if (version === cacheVersion) tags = result;
				return result;
			})
			.finally(() => { if (version === cacheVersion) tagsInflight = null; });
	}
	return tagsInflight;
}

/** Earlier requests cannot refill the cache after template CRUD. */
export function clearMailTemplateCache(): void {
	cacheVersion++;
	templates = null;
	templatesInflight = null;
	tags = null;
	tagsInflight = null;
}
