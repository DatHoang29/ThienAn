<template>
    <v-chart :option="chartOptions" autoresize theme="dark" style="min-width: 300px; min-height: 250px;" class="chart-container" />
</template>

<script setup>
    import { use } from 'echarts/core';
    import VChart from 'vue-echarts';
    import { LineChart } from 'echarts/charts';
    import { TitleComponent, TooltipComponent, GridComponent } from 'echarts/components';
    import { CanvasRenderer } from 'echarts/renderers';
    import 'echarts/theme/dark';

    use([LineChart, TitleComponent, TooltipComponent, GridComponent, CanvasRenderer]);
    
    const chartProps = defineProps({
        chartOptions: {
            type: Object,
            required: false,
            default: () => ({
                title: {
                    text: 'Chart Title'
                },
                tooltip: {
                    trigger: 'axis'
                },
                grid: {
                    // left: '10%',
                    // right: '10%',
                    bottom: '10%',
                    // containLabel: false
                },
                legend: {
                    data: ['matcha_latte', 'milk_tea', 'cheese_cocoa', 'walnut_brownie'],
                    formatter: function (name) {
                        const names = {
                            matcha_latte: 'Matcha Latte',
                            milk_tea: 'Milk Tea',
                            cheese_cocoa: 'Cheese Cocoa',
                            walnut_brownie: 'Walnut Brownie'
                        };
                        return names[name] || name;
                    }
                },
                xAxis: {
                    type: 'category',
                    name: 'X Name',
                    data: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun']
                },
                yAxis: {
                    type: 'value',
                    name: 'Y Name',
                    data: ['matcha_latte', 'milk_tea', 'cheese_cocoa', 'walnut_brownie']
                },
                series: [
                    {
                        name: 'matcha_latte',
                        type: 'line',
                        data: [120, 132, 101, 134, 90, 230]
                    },
                    {
                        name: 'milk_tea',
                        type: 'line',
                        data: [220, 182, 191, 234, 290, 330]
                    },
                    {
                        name: 'cheese_cocoa',
                        type: 'line',
                        data: [150, 232, 201, 154, 190, 330]
                    },
                    {
                        name: 'walnut_brownie',
                        type: 'line',
                        data: [320, 332, 301, 334, 390, 330]
                    }
                ]
            })
        }
    });
</script>
<style scoped>
    .chart-container {
        width: 100%;
        height: 100%;
    }
</style>