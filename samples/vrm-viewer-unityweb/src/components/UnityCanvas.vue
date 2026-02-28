<template>
  <canvas ref="canvasRef" id="unity-canvas" class="unity-canvas" tabindex="-1" />
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import type { UnityInstance } from '../types/unity'

const props = defineProps<{
  loaderUrl: string       // 例: '/UnityWeb/Build/UnityWeb.loader.js'
  dataUrl: string         // 例: '/UnityWeb/Build/UnityWeb.data.gz'
  frameworkUrl: string    // 例: '/UnityWeb/Build/UnityWeb.framework.js.gz'
  codeUrl: string         // 例: '/UnityWeb/Build/UnityWeb.wasm.gz'
  streamingAssetsUrl?: string
}>()

const emit = defineEmits<{
  ready: [instance: UnityInstance]
  progress: [value: number]
  error: [message: string]
}>()

const canvasRef = ref<HTMLCanvasElement>()
let instance: UnityInstance | null = null
let loaderScript: HTMLScriptElement | null = null

onMounted(() => {
  loaderScript = document.createElement('script')
  loaderScript.src = props.loaderUrl
  loaderScript.onload = initUnity
  loaderScript.onerror = () => emit('error', `Failed to load Unity loader: ${props.loaderUrl}`)
  document.body.appendChild(loaderScript)
})

onUnmounted(() => {
  instance?.Quit().catch(() => {})
  loaderScript?.remove()
})

async function initUnity() {
  if (!canvasRef.value) return
  try {
    instance = await createUnityInstance(
      canvasRef.value,
      {
        dataUrl: props.dataUrl,
        frameworkUrl: props.frameworkUrl,
        codeUrl: props.codeUrl,
        streamingAssetsUrl: props.streamingAssetsUrl ?? 'StreamingAssets',
      },
      (progress) => emit('progress', progress)
    )
    emit('ready', instance)
  } catch (e) {
    emit('error', String(e))
  }
}
</script>

<style scoped>
.unity-canvas {
  width: 100%;
  height: 100%;
  display: block;
}
</style>
