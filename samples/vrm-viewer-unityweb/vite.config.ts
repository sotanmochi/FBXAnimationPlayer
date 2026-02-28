import { defineConfig, type Plugin } from 'vite'
import vue from '@vitejs/plugin-vue'

/**
 * Unity Web の Brotli 圧縮ファイルを dev server で正しく配信するプラグイン。
 * `.br` ファイルに `Content-Encoding: br` と適切な `Content-Type` を付与する。
 */
function unityBrotliPlugin(): Plugin {
  return {
    name: 'unity-brotli',
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const url = (req as { url?: string }).url ?? ''
        if (url.endsWith('.br')) {
          res.setHeader('Content-Encoding', 'br')
          if (url.endsWith('.js.br')) {
            res.setHeader('Content-Type', 'application/javascript')
          } else if (url.endsWith('.wasm.br')) {
            res.setHeader('Content-Type', 'application/wasm')
          } else {
            res.setHeader('Content-Type', 'application/octet-stream')
          }
        }
        next()
      })
    },
  }
}

// https://vitejs.dev/config/
// mode: 'development' | 'production' (process.env.NODE_ENV の代わりに使用)
export default defineConfig(({ mode }) => ({
  plugins: [vue(), unityBrotliPlugin()],
  // GitHub Pages のサブパスに合わせる（本番ビルド時のみ適用）
  base: mode === 'production' ? '/FBXAnimationPlayer/' : '/',
}))
