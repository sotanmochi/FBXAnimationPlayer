<template>
  <div class="app">
    <!-- ヘッダー -->
    <header class="header">
      <span class="title">FBX Animation Player Demo</span>
      <FileLoader
        :is-unity-ready="isUnityReady"
        :vrm="vrm"
        :anim="anim"
      />
    </header>

    <!-- Unity Web キャンバス（相対配置の親） -->
    <div class="canvas-wrapper">
      <UnityCanvas
        :loader-url="unityConfig.loaderUrl"
        :data-url="unityConfig.dataUrl"
        :framework-url="unityConfig.frameworkUrl"
        :code-url="unityConfig.codeUrl"
        :streaming-assets-url="unityConfig.streamingAssetsUrl"
        @ready="onUnityReady"
        @progress="loadProgress = $event"
        @error="unityError = $event"
      />

      <!-- ローディングオーバーレイ -->
      <LoadingOverlay
        :visible="!isUnityReady || vrm.isLoading.value || anim.isLoading.value"
        :message="overlayMessage"
        :progress="loadProgress"
      />

      <!-- Unity ロードエラー表示 -->
      <div v-if="unityError" class="unity-error">
        Unity の読み込みに失敗しました: {{ unityError }}
      </div>
    </div>

    <!-- アニメーションコントロールパネル -->
    <AnimationPanel :anim="anim" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import UnityCanvas    from './components/UnityCanvas.vue'
import FileLoader     from './components/FileLoader.vue'
import AnimationPanel from './components/AnimationPanel.vue'
import LoadingOverlay from './components/LoadingOverlay.vue'

import { useUnityMessageBus }     from './composables/useUnityMessageBus'
import { useVrmController }       from './composables/useVrmController'
import { useAnimationController } from './composables/useAnimationController'

import type { UnityInstance } from './types/unity'

// ── Unity ビルドファイルのパス設定 ─────────────────────────────────────────
// Vite の base に合わせて public/UnityWeb/ 以下を参照する。
// GitHub Pages では base が '/FBXAnimationPlayer/' になる（vite.config.ts 参照）。
const unityConfig = {
  loaderUrl:          'UnityWeb/Build/UnityWeb.loader.js',
  dataUrl:            'UnityWeb/Build/UnityWeb.data.br',
  frameworkUrl:       'UnityWeb/Build/UnityWeb.framework.js.br',
  codeUrl:            'UnityWeb/Build/UnityWeb.wasm.br',
  streamingAssetsUrl: 'UnityWeb/StreamingAssets',
}

// ── 状態 ──────────────────────────────────────────────────────────────────
const isUnityReady = ref(false)
const loadProgress = ref(0)
const unityError   = ref<string | null>(null)

// ── メッセージバス + 機能別 Composable ────────────────────────────────────
const bus  = useUnityMessageBus()
const vrm  = useVrmController(bus)
const anim = useAnimationController(bus)

// Unity 初期化完了通知を受け取る
bus.on('app/ready', () => { isUnityReady.value = true })

// ── ローディングメッセージ ────────────────────────────────────────────────
const overlayMessage = computed(() => {
  if (!isUnityReady.value)       return 'Unity を読み込んでいます...'
  if (vrm.isLoading.value)       return 'VRM モデルを読み込んでいます...'
  if (anim.isLoading.value)      return 'FBX アニメーションを読み込んでいます...'
  return ''
})

// ── Unity 初期化完了コールバック ──────────────────────────────────────────
function onUnityReady(instance: UnityInstance) {
  bus.unityInstance.value = instance
}
</script>

<style>
/* グローバルリセット */
*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

body {
  background: #0c0e14;
  color: #fff;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  height: 100dvh;
  overflow: hidden;
}
</style>

<style scoped>
.app {
  display: flex;
  flex-direction: column;
  height: 100dvh;
}

.header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 16px;
  background: rgba(12, 14, 20, 0.95);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  flex-wrap: wrap;
  flex-shrink: 0;
}

.title {
  font-size: 14px;
  font-weight: 600;
  color: #40c4a8;
  white-space: nowrap;
}

.canvas-wrapper {
  flex: 1;
  position: relative;
  overflow: hidden;
  min-height: 0;
}

.unity-error {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(12, 14, 20, 0.9);
  color: #ff6b6b;
  font-size: 14px;
  padding: 24px;
  text-align: center;
}
</style>
