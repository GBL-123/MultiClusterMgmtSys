import { defineConfig } from "vite";
import { fileURLToPath } from "node:url";

// 图表资源打包:把自研入口脚本(内含 echarts)构建为 wwwroot 下的自托管 ES Module。
// 必须用 lib 模式(formats ["es"]):普通 rollupOptions.input 会把入口导出当作无消费
// 整棵 tree-shake,产物没有 export 语句,浏览器 import() 解析出空命名空间
// ("mount is not a function")。entryFileNames 已由 lib.fileName 固定为无 hash。
// emptyOutDir 必须为 false —— wwwroot 是应用静态根,不能被清空。
export default defineConfig({
    appType: "custom",
    // echarts/zrender 的产物把 process.env.NODE_ENV 作运行时字面比较,
    // vite 8 lib 模式下 vite 不再自动替换该值 → 浏览器无 process 全局,
    // 模块求值即抛 "process is not defined"。显式钉为 production,
    // 让这些开发断言分支在压缩时一并消除。
    define: {
        "process.env.NODE_ENV": '"production"'
    },
    build: {
        outDir: "wwwroot/js",
        emptyOutDir: false,
        manifest: false,
        sourcemap: false,
        lib: {
            entry: fileURLToPath(new URL("./Assets/Scripts/dashboard-trend.js", import.meta.url)),
            formats: ["es"],
            fileName: "dashboard-trend"
        },
        rollupOptions: {
            output: {
                // 组件侧 import 固定是 /js/dashboard-trend.js(.js 扩展名),
                // 覆盖 lib 模式按入口扩展名生成的 .mjs。
                entryFileNames: "dashboard-trend.js"
            }
        }
    }
});
