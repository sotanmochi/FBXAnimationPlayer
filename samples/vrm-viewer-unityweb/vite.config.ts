import { defineConfig, type Plugin } from 'vite'
import vue from '@vitejs/plugin-vue'
import { readFileSync, writeFileSync, readdirSync } from 'fs'
import { join } from 'path'
import { brotliDecompressSync } from 'zlib'

/**
 * Unity Web の Brotli 圧縮ファイルを dev server で正しく配信するプラグイン。
 * `.br` ファイルに `Content-Encoding: br` と適切な `Content-Type` を付与する。
 *
 * 本番ビルド時は `Content-Encoding` ヘッダーを付与できない静的ホスティング
 * （GitHub Pages 等）向けに、出力先の `.br` ファイルを展開済みデータで上書きする。
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
    writeBundle(options) {
      const outDir = options.dir
      if (!outDir) return
      const buildDir = join(outDir, 'UnityWeb', 'Build')
      let files: string[]
      try {
        files = readdirSync(buildDir)
      } catch {
        return
      }
      for (const file of files) {
        if (!file.endsWith('.br')) continue
        const filePath = join(buildDir, file)
        const compressed = readFileSync(filePath)
        const decompressed = brotliDecompressSync(compressed)
        writeFileSync(filePath, decompressed)
        console.log(`  Decompressed ${file} (${compressed.length} -> ${decompressed.length} bytes)`)
      }
    },
  }
}

// https://vitejs.dev/config/
// mode: 'development' | 'production' (process.env.NODE_ENV の代わりに使用)
export default defineConfig(({ mode }) => ({
  plugins: [vue(), unityBrotliPlugin()],
  // GitHub Pages のサブパスに合わせる（本番ビルド時のみ適用）
  base: mode === 'production' ? '/FBXAnimationPlayer/vrm-viewer-unityweb/' : '/',
}))
