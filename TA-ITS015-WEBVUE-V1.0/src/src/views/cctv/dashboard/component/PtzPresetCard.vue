<template>
	<div class="preset-card" :class="{ active }" @click="$emit('goto', preset.id)">
		<div class="preset-info">
			<strong>{{ preset.name }}</strong>
			<span>{{ preset.description }}</span>
		</div>
		<el-button class="preset-remove" circle text type="danger" @mousedown.stop @click.stop.prevent="$emit('remove', preset.id)">
			<el-icon><ele-Delete /></el-icon>
		</el-button>
	</div>
</template>

<script setup lang="ts">
export type PtzPreset = {
	id: string;
	name: string;
	description: string;
};

defineProps<{
	preset: PtzPreset;
	active?: boolean;
}>();

defineEmits<{
	(e: 'goto', id: string): void;
	(e: 'remove', id: string): void;
}>();
</script>

<style scoped lang="scss">
.preset-card {
	min-width: 0;
	padding: 10px;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	border: 1px solid rgba(255, 255, 255, 0.1);
	border-radius: 8px;
	background: rgba(255, 255, 255, 0.05);
	cursor: pointer;
	transition:
		border-color 0.16s ease,
		background 0.16s ease;
}
.preset-card:hover,
.preset-card.active {
	border-color: rgba(64, 158, 255, 0.8);
	background: rgba(64, 158, 255, 0.14);
}
.preset-info {
	min-width: 0;
	display: grid;
	gap: 2px;

	strong,
	span {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	strong {
		color: #ffffff;
		font-size: 13px;
	}

	span {
		color: #cbd5e1;
		font-size: 11px;
	}
}
.preset-remove {
	flex-shrink: 0;
}
</style>
