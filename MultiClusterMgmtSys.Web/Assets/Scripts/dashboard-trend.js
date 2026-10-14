import * as echarts from "echarts/core";
import { LineChart } from "echarts/charts";
import { GridComponent, TooltipComponent, MarkAreaComponent } from "echarts/components";
import { CanvasRenderer } from "echarts/renderers";

echarts.use([LineChart, GridComponent, TooltipComponent, MarkAreaComponent, CanvasRenderer]);

const INK = "#111111";
const HAIRLINE = "#E2DED5";
const MUTED = "#6E675C";
const MONO = "'IBM Plex Mono', ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";

// 无数据区的最小渲染宽度:窗口起点到首个快照点不足 2 分钟时不画斜纹区(首点贴边闪条)。
const NO_DATA_MIN_WIDTH = 2 * 60 * 1000;

// 无数据区斜纹底:markArea 是标记组件、不经过序列的 decal 管线(markArea.itemStyle.decal 实测不渲染),
// 改用 zrender fill 原生支持的 canvas 图案(45° 墨线平铺);canvas 不可用时降级 ink 4% 纯色(design 降级条款)。
const HATCH_TILE_SIZE = 8;
const HATCH_FALLBACK = "rgba(17, 17, 17, 0.04)";

const charts = new Map();

// 堆叠顶边(就绪 + 未就绪)的峰值;以节点总数兜底,真实数据超出时以峰值为准防裁切。
function computePeak(series, totalNodes) {
    let peak = totalNodes;
    const ready = series[0];
    const notReady = series[1];
    if (ready && notReady) {
        const length = Math.min(ready.points.length, notReady.points.length);
        for (let i = 0; i < length; i++) {
            peak = Math.max(peak, ready.points[i].v + notReady.points[i].v);
        }
    }

    return peak;
}

// Y 轴整数刻度(契约:整数刻度 + 上界贴近峰值):刻度至多 5 段,步长取 1-2-5 进位。
// 3 节点 → 上界 4、刻度 0..4;取代旧 peak*1.25 公式的 0/2/4 三档悬空感。
function computeScale(peak) {
    const span = Math.max(3, peak + 1);
    const ladder = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000];
    let step = ladder[ladder.length - 1];
    for (const candidate of ladder) {
        if (Math.ceil(span / candidate) <= 5) {
            step = candidate;
            break;
        }
    }

    return { step, max: Math.ceil(span / step) * step };
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

function formatSecond(timestamp) {
    const date = new Date(timestamp);
    return `${formatTick(timestamp)}:${pad2(date.getSeconds())}`;
}

// 四小时一刻:窗口起点对齐到 4h 边界,使刻度落点稳定。
function majorTickInterval() {
    const fourHours = 4 * 60 * 60 * 1000;
    return fourHours;
}

function hatchFill() {
    try {
        const tile = document.createElement("canvas");
        tile.width = HATCH_TILE_SIZE;
        tile.height = HATCH_TILE_SIZE;
        const context = tile.getContext("2d");
        if (!context) {
            return HATCH_FALLBACK;
        }

        context.strokeStyle = "rgba(17, 17, 17, 0.14)";
        context.lineWidth = 1.5;
        context.beginPath();
        context.moveTo(0, HATCH_TILE_SIZE);
        context.lineTo(HATCH_TILE_SIZE, 0);
        context.stroke();
        return { image: tile, repeat: "repeat" };
    } catch {
        return HATCH_FALLBACK;
    }
}

// 阶梯口径:任一时刻的值保持到下一个快照事件。窗口末点之后无真实事件,
// 渲染层补一个与末点同值的"现在"延点,使面积带贯通到右缘(不改变数据口径)。
// 无数据区起点同步补一个 null 引导点:让 tooltip 在斜纹区吸附到"空值"而非首个真实点。
function buildChartData(item, windowEnd, padStartTimestamp) {
    const points = [];
    if (padStartTimestamp !== null) {
        points.push([padStartTimestamp, NaN]);
    }

    for (const point of item.points) {
        points.push([toTimestamp(point.t), point.v]);
    }

    // 延点标记为纯渲染点:渲染层用它贯通到右缘,交互层不认它是数据。
    let extended = false;
    if (points.length > 0 && points[points.length - 1][0] < windowEnd) {
        points.push([windowEnd, points[points.length - 1][1]]);
        extended = true;
    }

    return { points, extended };
}

// 延点降级为无 symbol 的数据项:悬停吸附到延点时不显示高亮圈,
// 数值/时刻由 formatter 折算回最近真实快照(不把"现在"读成新数据)。
function toSeriesData(chartData) {
    if (!chartData.extended) {
        return chartData.points;
    }

    const data = chartData.points.slice();
    data[data.length - 1] = {
        value: data[data.length - 1],
        symbol: "none",
        emphasis: { disabled: true }
    };
    return data;
}

function buildOption(data) {
    const windowStart = toTimestamp(data.windowStart);
    const windowEnd = toTimestamp(data.windowEnd);
    const peak = computePeak(data.series, data.totalNodes);
    const { step, max } = computeScale(peak);
    const firstPointTime = data.series[0] && data.series[0].points.length > 0
        ? toTimestamp(data.series[0].points[0].t)
        : null;
    const lastPoint = data.series[0] && data.series[0].points.length > 0
        ? data.series[0].points[data.series[0].points.length - 1]
        : null;
    const lastRealTime = lastPoint !== null ? toTimestamp(lastPoint.t) : null;
    const hasNoDataZone = firstPointTime !== null && firstPointTime - windowStart >= NO_DATA_MIN_WIDTH;

    const chartItems = data.series.map(item => ({
        item,
        chartData: buildChartData(item, windowEnd, hasNoDataZone ? windowStart : null)
    }));

    const series = chartItems.map(({ item, chartData }, index) => {
        const config = {
            name: item.name,
            type: "line",
            stack: "nodes",
            step: "end",
            showSymbol: false,
            // 未就绪(上层堆叠系列)只作红色填充、不描线:其线位恒等于堆叠顶边,
            // 描线会在未就绪=0 时整段盖住绿带顶边;顶边由「合计」墨线统一表达。
            lineStyle: index === 1 ? { width: 0 } : { color: item.color, width: 1.5 },
            itemStyle: { color: item.color },
            areaStyle: {
                color: item.color,
                // 绿 0.32/红 0.45:健康带必须一眼读绿(0.14 实测过淡,读作灰底),
                // 红权重仍高于绿,大舰队下未就绪窄带可辨。
                opacity: index === 0 ? 0.32 : 0.45
            },
            data: toSeriesData(chartData)
        };

        // 无数据区仅就绪系列挂一次:斜纹底 + 居中说明,与「未就绪=0」严格区分。
        if (index === 0 && hasNoDataZone) {
            config.markArea = {
                itemStyle: {
                    color: hatchFill(),
                    borderWidth: 0
                },
                label: {
                    show: true,
                    position: "inside",
                    color: MUTED,
                    fontFamily: MONO,
                    fontSize: 11,
                    formatter: "无快照数据"
                },
                data: [[{ xAxis: windowStart }, { xAxis: firstPointTime }]]
            };
        }

        return config;
    });

    // 未就绪红色区只作填充、不描线:堆叠上层系列在未就绪=0 时其线与顶边重合,
    // 红描线会整段盖住绿带顶边(实际走查)。顶边由独立「合计」墨色描线表达,
    // 红色仅作为顶边与就绪顶界之间的填充出现,永不覆盖绿色。
    chartItems.forEach(({ item, chartData }, index) => {
        if (index !== 1) {
            return;
        }

        const edgeData = {
            points: chartData.points.map((point, i) => [
                point[0],
                point[1] + chartItems[0].chartData.points[i][1]
            ]),
            extended: chartData.extended
        };
        series.push({
            name: "合计",
            type: "line",
            step: "end",
            showSymbol: false,
            lineStyle: { color: INK, width: 1.5 },
            itemStyle: { color: INK },
            data: toSeriesData(edgeData)
        });
    });

    return {
        animation: false,
        grid: { left: 36, top: 16, right: 12, bottom: 24 },
        tooltip: {
            trigger: "axis",
            axisPointer: { type: "line", lineStyle: { color: HAIRLINE } },
            backgroundColor: "#FCFBF7",
            borderColor: HAIRLINE,
            borderWidth: 1,
            padding: [6, 10],
            textStyle: { color: INK, fontFamily: MONO, fontSize: 12 },
            formatter: params => {
                const first = params.length > 0 ? params[0] : null;
                // 无数据区(首个快照点之前)悬停:不给读数,只说明原因。
                // 引导点与吸附都落在首点之前:按时间戳过滤,不计入读数。
                if (first && firstPointTime !== null) {
                    const hovered = Number(first.axisValue);
                    const noValue = first.value[1] === null
                        || first.value[1] === undefined
                        || Number.isNaN(Number(first.value[1]));
                    if (noValue || hovered < firstPointTime) {
                        return "无快照数据";
                    }
                }

                // 窗口末延点是纯渲染点:时刻折算回最近真实快照,避免"现在"被误读为新读数。
                const hoveredRaw = Number(first ? first.axisValue : Date.now());
                const hovered = lastRealTime !== null ? Math.min(hoveredRaw, lastRealTime) : hoveredRaw;
                const readyValue = first ? Number(first.value[1]) : 0;
                const notReadyValue = params.length > 1 ? Number(params[1].value[1]) : 0;
                const total = readyValue + notReadyValue;
                const ratio = total === 0 ? "—" : `${(readyValue / total * 100).toFixed(1)}%`;
                const rows = params
                    .filter(item => item.seriesName === "就绪" || item.seriesName === "未就绪")
                    .map(item => `${item.seriesName} ${item.value[1]}`);
                return [
                    `<div style="font-size:12px">${formatSecond(hovered)}</div>`,
                    ...rows.map(row => `<div style="margin-top:2px">${row}</div>`),
                    `<div style="margin-top:2px;border-top:1px solid ${HAIRLINE};padding-top:2px">合计 ${total}</div>`,
                    `<div style="margin-top:2px">就绪率 ${ratio}</div>`
                ].join("");
            }
        },
        xAxis: {
            type: "time",
            min: windowStart,
            max: windowEnd,
            axisLine: { lineStyle: { color: INK } },
            axisTick: {
                show: true,
                inside: false,
                lineStyle: { color: INK, width: 1 },
                interval: majorTickInterval
            },
            minorTick: {
                show: true,
                splitNumber: 3,
                lineStyle: { color: HAIRLINE, width: 1 }
            },
            axisLabel: {
                color: MUTED,
                fontFamily: MONO,
                fontSize: 11,
                hideOverlap: true,
                formatter: formatTick,
                interval: majorTickInterval
            },
            splitLine: { show: false }
        },
        yAxis: {
            type: "value",
            min: 0,
            max,
            interval: step,
            minInterval: 1,
            axisLabel: { color: MUTED, fontFamily: MONO, fontSize: 11 },
            axisLine: { lineStyle: { color: INK, width: 1 } },
            axisTick: {
                show: true,
                inside: false,
                lineStyle: { color: INK, width: 1 }
            },
            splitLine: {
                show: true,
                lineStyle: { color: HAIRLINE, type: [3, 3] }
            }
        },
        series
    };
}

// 挂载 ECharts 堆叠阶梯面积图;实例与 resize 句柄以容器元素为键存于模块级 Map,
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
        // notMerge:无数据区随快照推进可能消失,整option替换避免 markArea 残留。
        entry.chart.setOption(buildOption(data), true);
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
