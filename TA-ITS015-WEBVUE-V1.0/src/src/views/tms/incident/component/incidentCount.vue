<template>
    <div class="div-incident-count">
        <div class="item bg-color--primary">
            <div class="icon">
                <el-icon :size="30"><Bell /></el-icon>
            </div>
            <div class="content">
                <p class="title">{{ $t('lz.label.tmsIncident.incidentTotal') }}</p>
                <h3 class="amount">{{ props.total ?? 0 }}</h3>
            </div>
        </div>
        <div class="item bg-color--warning">
            <div class="icon">
                <el-icon :size="30"><WarnTriangleFilled /></el-icon>
            </div>
            <div class="content">
                <p>{{ $t('lz.label.tmsIncident.newTotal') }}</p>
                <h3 class="amount">{{ props.new ?? 0 }}</h3>
            </div>
        </div>
        <div class="item bg-color--danger">
            <div class="icon">
                <el-icon :size="30"><CircleClose /></el-icon>
            </div>
            <div class="content">
                <p>{{ $t('lz.label.tmsIncident.inValidTotal') }}</p>
                <h3 class="amount">{{ props.inValid ?? 0 }}</h3>
            </div>
        </div>
        <div class="item bg-color--success">
            <div class="icon">
                <el-icon :size="30"><StarFilled /></el-icon>
            </div>
            <div class="content">
                <p>{{ $t('lz.label.tmsIncident.validTotal') }}</p>
                <h3 class="amount">{{ props.valid ?? 0 }}</h3>
            </div>
        </div>
    </div>
</template>
<script setup lang="ts">
	import { Bell } from '@element-plus/icons-vue';
	import { WarnTriangleFilled } from '@element-plus/icons-vue';
	import { CircleClose } from '@element-plus/icons-vue';
	import { StarFilled } from '@element-plus/icons-vue';

    import { useI18n } from 'vue-i18n';
    const { t: $t } = useI18n();

    const props = defineProps({
        chartDataTypeName: {
            type: String,
            default: ''
        },
        total: {
            type: Number,
            default: 0
        },
        new: {
            type: Number,
            default: 0
        },
        valid: {
            type: Number,
            default: 0
        },
        inValid: {
            type: Number,
            default: 0
        },
    });
</script>
<style lang="scss" scoped>
	.div-incident-count{
		.item{
			margin-top: 5px;
			height: 55px;
			width: 100%;
			color: white;
			border-radius: 5px;
			display: flex;
			flex-direction: row;
			gap: 10px;

			.icon{
				height: 55px;
				width: 55px;
				display: flex;
				justify-content: center;
				align-items: center;
			}

			.content{
				height: 55px;
				display: flex;
				flex-direction: column;
				justify-content: center;
				gap: 5px;

				.title{
					font-size: 14px;
					margin: 0px;
				}

				.amount{
					font-size: 20px;
					margin: 0px;
					font-weight: bold;
				}
			}
		}
	}
</style>