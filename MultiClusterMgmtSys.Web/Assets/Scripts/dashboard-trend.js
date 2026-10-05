import * as echarts from "echarts/core";
import { LineChart } from "echarts/charts";
import { GridComponent, TooltipComponent } from "echarts/components";
import { CanvasRenderer } from "echarts/renderers";

echarts.use([LineChart, GridComponent, TooltipComponent, CanvasRenderer]);

const INK = "#111111";
const HAIRLINE = "#E2DED5";
const MUTED = "#6E675C";
const MONO = "'IBM Plex Mono', ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";

const charts = new Map();

// 依据设计系统取「偶数向上取整」的niceMax,与旧 SVG 版口径一致。
function niceMaxOf(series) {
    let max = 1;
    for (const item of series) {
        for (const point of item.points) {
            max = Math.max(max, point.v);
        }
    }

    return Math.ceil(max / 2) * 2;
}

function toTimestamp(iso) {
    return Date.parse(iso);
}

// 时间轴统一「MM-dd HH:mm」标签:跨天的刻度不切换为纯日期,避免 04:00 与 10-6 混排。
function pad2(value) {
    return String(value).padStart(2, "0");
}

function formatTick(timestamp) {
    const date = new Date(timestamp);
    return `${pad2(date.getMonth() + 1)}-${pad2(date.getDate())} ${pad2(date.getHours())}:${pad2(date.getMinutes())}`;
}

// 秒级聚合在 C# DashboardService 完成(每秒至多一个点),绘制层只负责渲染。
function buildOption(data) {
    const windowStart = toTimestamp(data.windowStart);
    const windowEnd = toTimestamp(data.windowEnd);
    const yMax = niceMaxOf(data.series);
    return {
        animation: false,
        grid: { left: 44, top: 12, right: 12, bottom: 24 },
        tooltip: {
            trigger: "axis",
            axisPointer: { type: "line", lineStyle: { color: HAIRLINE } },
            backgroundColor: "#FCFBF7",
            borderColor: HAIRLINE,
            borderWidth: 1,
            padding: [6, 10],
            textStyle: { color: INK, fontFamily: MONO, fontSize: 12 }
        },
        xAxis: {
            type: "time",
            min: windowStart,
            max: windowEnd,
            axisLine: { lineStyle: { color: INK } },
            axisTick: { show: false },
            axisLabel: {
                color: MUTED,
                fontFamily: MONO,
                fontSize: 11,
                hideOverlap: true,
                formatter: formatTick
            },
            splitLine: { show: false }
        },
        yAxis: {
            type: "value",
            min: 0,
            max: yMax,
            interval: yMax / 2,
            axisLabel: { color: MUTED, fontFamily: MONO, fontSize: 11 },
            axisLine: { show: false },
            axisTick: { show: false },
            splitLine: { lineStyle: { color: HAIRLINE } }
        },
        series: data.series.map(item => ({
            name: item.name,
            type: "line",
            step: "end",
            showSymbol: false,
            lineStyle: { color: item.color, width: 2 },
            itemStyle: { color: item.color },
            data: item.points.map(point => [toTimestamp(point.t), point.v])
        }))
    };
}

// 挂载 ECharts 阶梯时间序列图;实例与 resize 句柄以容器元素为键存于模块级 Map,
// update/dispose 皆按容器寻址,便于 Blazor 端以 ElementReference 调用。
export function mount(container, data) {
    dispose(container);
    const chart = echarts.init(container);
    chart.setOption(buildOption(data));
    const onResize = () => chart.resize();
    window.addEventListener("resize", onResize);
    charts.set(container, { chart, onResize });
}

export function update(container, data) {
    const entry = charts.get(container);
    if (entry) {
        entry.chart.setOption(buildOption(data));
    }
}

export function dispose(container) {
    const entry = charts.get(container);
    if (entry) {
        window.removeEventListener("resize", entry.onResize);
        entry.chart.dispose();
        charts.delete(container);
    }
}
