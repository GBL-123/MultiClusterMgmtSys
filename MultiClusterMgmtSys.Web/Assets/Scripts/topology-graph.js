import * as echarts from "echarts/core";
import { GraphChart } from "echarts/charts";
import { TooltipComponent } from "echarts/components";
import { CanvasRenderer } from "echarts/renderers";

echarts.use([GraphChart, TooltipComponent, CanvasRenderer]);

const INK = "#111111";
const HAIRLINE = "#E2DED5";
const MUTED = "#6E675C";
const AMBER = "#D97706";
const PAPER = "#FCFBF7";
const CANVAS = "#F4F4F0";
const MONO = "'IBM Plex Mono', ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";

const SPACING_X = 170;
const ROW_HEIGHT = 150;
const NODE_WIDTH = 110;
const NODE_HEIGHT = 34;

const charts = new Map();

function edgeLabelStyle() {
    return {
        show: true,
        formatter: "{c}",
        color: MUTED,
        fontFamily: MONO,
        fontSize: 10,
        backgroundColor: CANVAS,
        padding: [1, 3]
    };
}

function nodeItemStyle(node) {
    return {
        color: PAPER,
        borderColor: node.isCenter ? AMBER : INK,
        borderWidth: node.isCenter ? 2 : 1,
        borderType: node.isMissing ? "dashed" : "solid",
        opacity: node.isMissing ? 0.65 : 1
    };
}

function buildOption(data) {
    const nodes = (data.nodes || []).map((node) => ({
        id: node.id,
        name: node.label,
        kind: node.kind,
        kindText: node.kindText,
        status: node.statusHint || "",
        x: (node.order || 0) * SPACING_X,
        y: (node.layer || 0) * ROW_HEIGHT,
        symbol: "rect",
        symbolSize: [NODE_WIDTH, NODE_HEIGHT],
        itemStyle: nodeItemStyle(node),
        label: {
            show: true,
            position: "inside",
            formatter: [node.label, node.kindText].join("\n"),
            color: INK,
            fontFamily: MONO,
            fontSize: 10,
            lineHeight: 13
        }
    }));

    const edges = (data.edges || []).map((edge) => ({
        source: edge.from,
        target: edge.to,
        value: edge.relation,
        lineStyle: { color: HAIRLINE, width: 1, curveness: 0 },
        symbol: ["none", "arrow"],
        symbolSize: 6,
        label: edgeLabelStyle()
    }));

    return {
        animation: false,
        tooltip: {
            backgroundColor: PAPER,
            borderColor: HAIRLINE,
            textStyle: { color: INK, fontFamily: MONO, fontSize: 12 },
            formatter: (params) => {
                if (params.dataType !== "node") {
                    return "";
                }
                const lines = [params.data.name, params.data.kindText];
                if (params.data.status) {
                    lines.push(params.data.status);
                }
                return lines.join("\n");
            }
        },
        series: [
            {
                type: "graph",
                layout: "none",
                roam: true,
                data: nodes,
                links: edges,
                top: 30,
                bottom: 30,
                left: 40,
                right: 40,
                emphasis: { focus: "adjacency", itemStyle: { borderColor: AMBER } }
            }
        ]
    };
}

export function mount(container, data) {
    dispose(container);
    const chart = echarts.init(container);
    chart.setOption(buildOption(data));
    const onResize = () => chart.resize();
    window.addEventListener("resize", onResize);
    chart.on("click", (params) => {
        if (params.dataType !== "node") {
            return;
        }
        const entry = charts.get(container);
        const ref = entry?.onNodeClick;
        if (ref) {
            ref.invokeMethodAsync("OnNodeClicked", params.data.id).catch(() => {});
        }
    });
    charts.set(container, { chart, onResize, onNodeClick: data.dotnetRef });
}

export function update(container, data) {
    const entry = charts.get(container);
    if (!entry) {
        return;
    }
    entry.onNodeClick = data.dotnetRef;
    entry.chart.setOption(buildOption(data));
}

export function dispose(container) {
    const entry = charts.get(container);
    if (!entry) {
        return;
    }
    window.removeEventListener("resize", entry.onResize);
    entry.chart.dispose();
    charts.delete(container);
}
