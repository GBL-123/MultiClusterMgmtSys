import { defineConfig } from "vite";
import { fileURLToPath } from "node:url";

// 拓扑图 bundle:与 vite.config.mjs(dashboard-trend)完全同构的独立配置。
// 不合并进多入口 build —— 多入口 lib 模式会把两个入口共享的 echarts 模块抽成
// 带 hash 的公共 chunk,破坏「每个 bundle 单文件自托管」契约与 BuildChartBundle
// 的单文件 Outputs 增量追踪。npm run build 顺序执行两份配置,产物互不相干。
export default defineConfig({
    appType: "custom",
    define: {
        "process.env.NODE_ENV": '"production"'
    },
    build: {
        outDir: "wwwroot/js",
        emptyOutDir: false,
        manifest: false,
        sourcemap: false,
        lib: {
            entry: fileURLToPath(new URL("./Assets/Scripts/topology-graph.js", import.meta.url)),
            formats: ["es"],
            fileName: "topology-graph"
        },
        rollupOptions: {
            output: {
                entryFileNames: "topology-graph.js"
            }
        }
    }
});
