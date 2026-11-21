
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const FALLBACK_COLOR = '#5b6472';

export function sourceTypeColor(sourceType?: string | null): string {
	if (!sourceType) return FALLBACK_COLOR;
	const item = useBaseConfig().getConfigItemByCode(BaseConfigTypeEnum.VideoWallSourceType, sourceType) as { value?: string | null };
	return item?.value || FALLBACK_COLOR;
}
