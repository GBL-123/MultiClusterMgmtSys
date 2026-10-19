import { Graph, register, Rect, ExtensionCategory } from "@antv/g6";

const NAME_FONT = "'Space Grotesk', 'Microsoft YaHei', 'PingFang SC', system-ui, sans-serif";
const MONO_FONT = "'IBM Plex Mono', ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";

const AMBER = "#D97706";
const CARD_FILL = "#FFFFFF";
const CARD_STROKE = "#C0C0C0";
const EDGE_STROKE = "#C0C0C0";
const NAME_BLUE = "#2078B4";
const NAME_DARK = "#343F4A";
const NAME_MUTED = "#8C8C8C";
const LINE3_GRAY = "#8C8C8C";
const STATUS_OK = "#17BEBB";
const STATUS_WARN = "#E36397";
const STATUS_UNKNOWN = "#B7AD99";

const KIND_TAG = {
    Pod: "POD", Service: "SVC", ConfigMap: "CM", Secret: "SEC",
    PersistentVolumeClaim: "PVC", PersistentVolume: "PV", Node: "NODE",
    Deployment: "DEPLOY", ReplicaSet: "RS", DaemonSet: "DS", StatefulSet: "STS",
    Job: "JOB", Ingress: "ING"
};

const GROUP_LABEL = {
    Pod: "容器组", Service: "服务", ConfigMap: "配置", Secret: "密钥",
    PersistentVolumeClaim: "存储", PersistentVolume: "存储卷", Ingress: "入口",
    Node: "节点"
};

const OK_STATUS = new Set(["Running", "Ready", "Bound", "Active", "Succeeded", "Completed"]);
const WARN_STATUS = new Set(["Pending", "NotReady", "Failed", "Lost"]);

/* 卡片场景容器高度按图自适应:内容高度 + 呼吸边距,并夹紧在 [420, 900] */
const AUTO_MIN_HEIGHT = 420;
const AUTO_MAX_HEIGHT = 900;
const AUTO_HEIGHT_PADDING = 120;

const graphs = new Map();

/* 挂载代次:同容器的 mount/update/dispose 互相抢占时,只允许最新一代生效(防并发挂载在同一容器叠加出双实例残影) */
const mountSeqs = new Map();

function beginMount(container) {
    const seq = (mountSeqs.get(container) || 0) + 1;
    mountSeqs.set(container, seq);
    return seq;
}

function isMountCurrent(container, seq) {
    return mountSeqs.get(container) === seq;
}

/* 仅当容器仍登记着该实例时才销毁;已被更新的挂载接管/已删除时不做处理(接管方负责销毁) */
function destroyIfOwned(container, graph) {
    const entry = graphs.get(container);
    if (entry && entry.graph === graph) {
        graphs.delete(container);
        delete container.__graph;
        delete container.dataset.topologyReady;
        graph.destroy();
    }
}

function escapeHtml(text) {
    return String(text)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;");
}

/* 悬停提示:展示未截断的完整名称与类型/命名空间/关系(卡片上的名称按像素预算截断) */
function tooltipContent(items) {
    const item = items && items[0];
    const data = (item && item.data) || {};
    if (!data.name) {
        return "";
    }
    const sub = [data.kindEn, data.kindCn].filter(Boolean).join(" · ");
    const line3 = data.line3 || "";
    return "<div class=\"topo-tt\">" +
        "<div class=\"topo-tt-name\">" + escapeHtml(data.name) + "</div>" +
        "<div class=\"topo-tt-sub\">" + escapeHtml(sub) + (line3 ? "  " + escapeHtml(line3) : "") + "</div>" +
        "</div>";
}

/* 按像素预算截断(CJK 记 1em,其余 0.62em);超长时保留头尾(保留 Pod 名哈希后缀等区分部分) */
function fitText(text, budgetPx, fontSize) {
    const cjkW = fontSize;
    const latinW = fontSize * 0.62;
    const charW = (ch) => (ch.charCodeAt(0) > 255 ? cjkW : latinW);

    let total = 0;
    for (const ch of text) {
        total += charW(ch);
    }
    if (total <= budgetPx) {
        return text;
    }

    const ellipsisW = latinW * 3;
    const tailBudget = Math.max(budgetPx * 0.38, fontSize * 3);
    let tail = "";
    let tailUsed = 0;
    for (let i = text.length - 1; i >= 0; i--) {
        const w = charW(text[i]);
        if (tailUsed + w > tailBudget) {
            break;
        }
        tail = text[i] + tail;
        tailUsed += w;
    }

    let head = "";
    let headUsed = 0;
    const headBudget = budgetPx - ellipsisW - tailUsed;
    for (const ch of text) {
        const w = charW(ch);
        if (headUsed + w > headBudget) {
            break;
        }
        head += ch;
        headUsed += w;
    }
    return head + "…" + tail;
}

function pillOf(node) {
    if (node.isMissing) {
        return { text: "未找到", color: STATUS_UNKNOWN };
    }
    const text = node.statusHintText || node.statusHint || "";
    if (!text) {
        return null;
    }
    const raw = node.statusHint || "";
    const color = OK_STATUS.has(raw) ? STATUS_OK : (WARN_STATUS.has(raw) ? STATUS_WARN : STATUS_UNKNOWN);
    return { text: text, color: color };
}

function cardOf(node) {
    if (node.isGroup) {
        return {
            name: node.name,
            kindEn: (node.kind || "").toUpperCase(),
            kindCn: "分组",
            line3: "聚合节点",
            pill: { text: node.count + " 项", color: STATUS_UNKNOWN },
            nameColor: NAME_DARK,
            isGroup: true
        };
    }
    const parts = [];
    if (node.namespace) {
        parts.push(node.namespace.toUpperCase());
    }
    if (node.relation) {
        parts.push(node.relation);
    }
    return {
        name: node.label || "",
        kindEn: KIND_TAG[node.kind] || node.kind.toUpperCase(),
        kindCn: node.kindText || node.kind,
        line3: parts.join(" · "),
        pill: pillOf(node),
        nameColor: node.isMissing ? NAME_MUTED : NAME_BLUE,
        isCenter: !!node.isCenter,
        isMissing: !!node.isMissing
    };
}

/* 由扁平节点/边构建渲染图:下行侧分组收敛成组卡片,边方向按渲染侧重排 */
function buildGraphData(data) {
    const nodes = (data.nodes || []).slice().sort((a, b) => (a.order || 0) - (b.order || 0));
    const byId = new Map(nodes.map((n) => [n.id, n]));
    const center = nodes.find((n) => n.isCenter);
    if (!center) {
        return { nodes: [], edges: [] };
    }

    const grouped = new Map();
    for (const n of nodes) {
        if (n === center || !n.group) {
            continue;
        }
        if (!grouped.has(n.group)) {
            grouped.set(n.group, []);
        }
        grouped.get(n.group).push(n);
    }

    const groupOf = new Map();
    const g6Nodes = nodes.map((n) => ({ id: n.id, data: cardOf(n) }));
    for (const [key, members] of grouped) {
        const gid = "g:" + key;
        g6Nodes.push({
            id: gid,
            data: cardOf({
                isGroup: true,
                name: GROUP_LABEL[members[0].kind] || members[0].kindText || members[0].kind,
                kind: members[0].kind,
                count: members.length
            })
        });
        for (const m of members) {
            groupOf.set(m.id, gid);
        }
    }

    const edges = [];
    const seen = new Set();
    const pushEdge = (source, target) => {
        const key = source + "\u2192" + target;
        if (seen.has(key)) {
            return;
        }
        seen.add(key);
        edges.push({ source: source, target: target });
    };

    for (const e of data.edges || []) {
        const s = e.from;
        const t = e.to;
        if (!byId.has(s) || !byId.has(t)) {
            continue;
        }
        if (groupOf.has(s) || groupOf.has(t)) {
            continue;
        }
        if (s === center.id) {
            pushEdge(s, t);
        } else if (t === center.id) {
            const side = byId.get(s);
            if (side && side.isAbove) {
                pushEdge(s, t);
            } else {
                pushEdge(t, s);
            }
        } else {
            pushEdge(s, t);
        }
    }
    for (const [key, members] of grouped) {
        const gid = "g:" + key;
        pushEdge(center.id, gid);
        for (const m of members) {
            pushEdge(gid, m.id);
        }
    }

    return { nodes: g6Nodes, edges: edges };
}

/* 汇总全部节点的渲染包围盒,取内容高度(图坐标系,zoom=1 时即 CSS 像素) */
function contentHeightOf(graph) {
    let minY = Infinity;
    let maxY = -Infinity;
    for (const node of graph.getNodeData()) {
        const bbox = graph.getElementRenderBounds(node.id);
        if (!bbox || !bbox.min || !bbox.max) {
            continue;
        }
        minY = Math.min(minY, bbox.min[1]);
        maxY = Math.max(maxY, bbox.max[1]);
    }
    return Number.isFinite(minY) && Number.isFinite(maxY) ? maxY - minY : 0;
}

/* 组织架构图风格卡片(中心琥珀描边 / 缺失虚线降透明 / 右上状态徽标) */
class OrgCard extends Rect {
    get nodeData() {
        return this.context.graph.getNodeData(this.id);
    }

    get data() {
        return this.nodeData.data || {};
    }

    getLabelStyle(attributes) {
        const [w] = this.getSize(attributes);
        const pill = this.data.pill;
        const pillW = pill ? pill.text.length * 9 + 20 : 0;
        const budget = w - 20 - pillW;
        return {
            text: fitText(this.data.name || "", budget, 13),
            fill: this.data.nameColor || NAME_BLUE,
            fontSize: 13,
            fontWeight: 500,
            fontFamily: NAME_FONT,
            textAlign: "left",
            textBaseline: "middle",
            transform: [["translate", -w / 2 + 10, -14]]
        };
    }

    getKindStyle(attributes) {
        const [w] = this.getSize(attributes);
        return {
            text: this.data.kindEn + " · " + this.data.kindCn,
            fill: NAME_DARK,
            fontSize: 9,
            fontFamily: MONO_FONT,
            textAlign: "left",
            textBaseline: "middle",
            transform: [["translate", -w / 2 + 10, 2]]
        };
    }

    drawKindShape(attributes, container) {
        this.upsert("kind", "text", this.getKindStyle(attributes), container);
    }

    getLine3Style(attributes) {
        const [w, h] = this.getSize(attributes);
        return {
            text: this.data.line3,
            fill: LINE3_GRAY,
            fontSize: 9,
            fontFamily: MONO_FONT,
            textAlign: "left",
            textBaseline: "middle",
            transform: [["translate", -w / 2 + 10, h / 2 - 12]]
        };
    }

    drawLine3Shape(attributes, container) {
        this.upsert("line3", "text", this.getLine3Style(attributes), container);
    }

    drawPillShape(attributes, container) {
        const pill = this.data.pill;
        if (!pill) {
            return;
        }
        const [w, h] = this.getSize(attributes);
        const pw = pill.text.length * 9 + 12;
        const ph = 16;
        const x = w / 2 - 8 - pw;
        const y = -h / 2 + 6;
        this.upsert("pill-bg", "rect", { x: x, y: y, width: pw, height: ph, radius: 2, fill: pill.color }, container);
        this.upsert("pill-text", "text", {
            x: x + pw / 2,
            y: y + ph / 2,
            text: pill.text,
            fill: "#fff",
            fontSize: 9,
            fontFamily: MONO_FONT,
            textAlign: "center",
            textBaseline: "middle"
        }, container);
    }

    render(attributes = this.parsedAttributes, container = this) {
        super.render(attributes, container);
        this.drawKindShape(attributes, container);
        this.drawLine3Shape(attributes, container);
        this.drawPillShape(attributes, container);
    }

    /* 隐藏默认连接桩小方块(仅保留其定位功能供连线路由),卡片保持干净边界 */
    getPortsStyle(attributes) {
        const ports = super.getPortsStyle(attributes);
        for (const key of Object.keys(ports)) {
            ports[key] = false;
        }
        return ports;
    }
}

register(ExtensionCategory.NODE, "org-card", OrgCard);

/**
 * 挂载拓扑图。
 * @param {HTMLElement} container 图表容器元素
 * @param {object} data 载荷(nodes/edges/dotnetRef)
 * @param {object} [options] 渲染选项:{ autoHeight } 容器高度按图自适应(默认 true;放大查看对话框传 false,保持 CSS 高度)
 */
export async function mount(container, data, options = {}) {
    const seq = beginMount(container);
    /* 抢占旧实例:同一容器只保留一个活动图 */
    const previous = graphs.get(container);
    if (previous) {
        graphs.delete(container);
        delete container.__graph;
        delete container.dataset.topologyReady;
        previous.graph.destroy();
    }

    await document.fonts.ready;
    if (!isMountCurrent(container, seq)) {
        return;
    }

    const graph = new Graph({
        container: container,
        autoResize: true,
        data: buildGraphData(data),
        node: {
            type: "org-card",
            style: {
                size: [160, 62],
                radius: 2,
                fill: CARD_FILL,
                stroke: (d) => (d.data.isCenter ? AMBER : CARD_STROKE),
                lineWidth: (d) => (d.data.isCenter ? 2 : 1),
                lineDash: (d) => (d.data.isMissing ? [4, 3] : [0, 0]),
                opacity: (d) => (d.data.isMissing ? 0.65 : 1),
                shadowBlur: 10,
                shadowColor: "#E0E0E0",
                shadowOffsetX: 3,
                labelPlacement: "center",
                ports: [{ placement: "top" }, { placement: "bottom" }]
            }
        },
        edge: {
            type: "polyline",
            style: {
                router: { type: "orth" },
                stroke: EDGE_STROKE,
                lineWidth: 1
            }
        },
        layout: {
            type: "dagre",
            rankdir: "TB",
            nodesep: 14,
            ranksep: 64
        },
        behaviors: ["zoom-canvas", "drag-canvas"],
        plugins: [
            {
                type: "tooltip",
                getContent: (_event, items) => tooltipContent(items)
            }
        ],
        animation: false
    });

    graph.on("node:click", (event) => {
        const id = event.target && event.target.id;
        if (!id || String(id).startsWith("g:")) {
            return;
        }
        const entry = graphs.get(container);
        if (entry && entry.onNodeClick) {
            entry.onNodeClick.invokeMethodAsync("OnNodeClicked", String(id)).catch(() => { });
        }
    });

    graphs.set(container, { graph: graph, onNodeClick: data.dotnetRef });
    /* 容器元素上保留实例引用:便于浏览器控制台排查与自动化视觉验证(非公开契约) */
    container.__graph = graph;

    try {
        await graph.render();
    } catch (error) {
        const current = isMountCurrent(container, seq);
        destroyIfOwned(container, graph);
        if (current) {
            throw error;
        }
        return;
    }
    if (!isMountCurrent(container, seq)) {
        destroyIfOwned(container, graph);
        return;
    }

    try {
        /* 高度按图自适应(卡片场景):小图收紧留白、纵向大图自动加高,夹紧 [420, 900] */
        if (options.autoHeight !== false) {
            const contentHeight = contentHeightOf(graph);
            if (contentHeight > 0) {
                const desired = Math.min(AUTO_MAX_HEIGHT, Math.max(AUTO_MIN_HEIGHT, Math.round(contentHeight + AUTO_HEIGHT_PADDING)));
                if (Math.abs(desired - container.clientHeight) > 8) {
                    container.style.height = desired + "px";
                    graph.setSize(container.clientWidth, container.clientHeight);
                }
            }
        }

        /* 初始视图:整体适配 → 缩放夹紧 [0.6, 1.15](过小看不清、过大显空)→ 居中 */
        await graph.fitView({ when: "always" }, false);
        if (!isMountCurrent(container, seq)) {
            destroyIfOwned(container, graph);
            return;
        }
        const clamped = Math.min(Math.max(graph.getZoom(), 0.6), 1.15);
        if (clamped !== graph.getZoom()) {
            await graph.zoomTo(clamped, false);
        }
        if (!isMountCurrent(container, seq)) {
            destroyIfOwned(container, graph);
            return;
        }
        await graph.fitCenter(false);
    } catch (error) {
        const current = isMountCurrent(container, seq);
        destroyIfOwned(container, graph);
        if (current) {
            throw error;
        }
        return;
    }
    if (!isMountCurrent(container, seq)) {
        destroyIfOwned(container, graph);
        return;
    }
    container.dataset.topologyReady = "1";
}

/**
 * 更新拓扑图(重建)。
 * @param {HTMLElement} container 图表容器元素
 * @param {object} data 载荷(nodes/edges/dotnetRef)
 * @param {object} [options] 渲染选项,与 mount 一致
 */
export function update(container, data, options = {}) {
    dispose(container);
    return mount(container, data, options);
}

export function dispose(container) {
    /* 使所有在途挂载失效,再销毁当前实例 */
    beginMount(container);
    const entry = graphs.get(container);
    if (entry) {
        graphs.delete(container);
        delete container.__graph;
        delete container.dataset.topologyReady;
        entry.graph.destroy();
    }
}
