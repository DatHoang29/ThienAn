<template>
    <div ref="gaugeChartRef" style="width: 200px; height: 200px"></div>
</template>
<script setup>
    import * as echarts from 'echarts';
    import { ref, watch, onMounted } from 'vue';
    import { defineProps } from 'vue';

    const chartProps = defineProps({
        value: Number
    });

    const gaugeChartRef = ref(null);

    onMounted(() => {

        const gaugeChart = echarts.init(gaugeChartRef.value);

        const chartOption = {
            series: [{
                type: 'gauge',
                max: 160, // Giá trị tối đa của gauge
                splitNumber: 8, // Chia thành 5 đoạn (150 / 5 = 30)
                // radius: '75%', // Điều chỉnh kích thước đường kính hình tròn
                // Đường tròn chính thể hiện tốc độ
                axisLine: {
                    lineStyle: {
                        width: 10,
                        color: [
                            [0.27, '#66ccff'], // Tốc độ thấp: 0-40 km/h
                            [0.8, '#ff9966'],  // Tốc độ trung bình: 41-120 km/h
                            [1, '#ff3300']     // Tốc độ cao: 121-150 km/h
                        ]
                    }
                },
                pointer: {
                    itemStyle: {
                        color: 'auto'
                    }
                },
                // Đường kẻ nhỏ thể hiện đơn vị minimum
                axisTick: {
                        distance: -5,
                        length: 5,
                        lineStyle: {
                            color: '#fff',
                            width: 1
                        }
                },
                // Đường kẻ phân khúc các nhóm tốc độ
                splitLine: {
                        distance: -30,
                        length: 30,
                        lineStyle: {
                        color: '#fff',
                        width: 1
                    }
                },
                // Label hiển thị các giá trị tốc độ
                axisLabel: {
                    color: 'inherit',
                    distance: -18,
                    fontSize: 12
                },
                detail: {
                    valueAnimation: true,
                    formatter: '{value} Km/h',
                    fontSize: 14,
                    offsetCenter: [0, '70%'], // Đẩy text xuống dưới đáy vòng tròn
                    color: 'inherit'
                },
                data: [{
                    // Giá trị tốc độ hiện tại
                    // name: 'Tốc độ',
                    value: chartProps.value
                }]
            }]
        };

        gaugeChart.setOption(chartOption);

        watch(() => chartProps.value, (newValue) => {
            gaugeChart.setOption({
                series: [
                    {
                        data: [{ value: newValue }]
                    }
                ]
            });
        });
    });
</script>