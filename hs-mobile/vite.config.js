import { defineConfig } from 'vite'
import uni from '@dcloudio/vite-plugin-uni'
import { fileURLToPath, URL } from 'node:url'

// uni-app + Vue3 + Vite 构建配置
export default defineConfig({
  plugins: [uni()],
  // 路径别名：@ → src（Vite 官方 ESM 推荐写法）
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  // 开发服务器配置（仅 H5 端生效）
  server: {
    host: '0.0.0.0',
    port: 8090,
    // 后端 API 代理（H5 端避免跨域）：
    //   /api/user/* → Auth 服务 5000（登录认证）
    //   /api/*      → WebAPI 5001（业务数据/文件/外部接口）
    // 注意：proxy 按顺序匹配，/api/user 必须在 /api 之前
    proxy: {
      '/api/user': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false
      },
      '/api': {
        target: 'http://localhost:5001',
        changeOrigin: true,
        secure: false
      }
    }
  },
  // 生产构建优化
  build: {
    // 小程序端分包体积优化
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: process.env.NODE_ENV === 'production'
      }
    }
  }
})
