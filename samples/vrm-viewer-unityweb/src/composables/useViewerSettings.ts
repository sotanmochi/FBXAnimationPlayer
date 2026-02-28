import { ref } from 'vue'
import type { UnityMessageBus } from './useUnityMessageBus'

/**
 * ビューアー設定（背景色・グリッド表示）を管理する Composable。
 * Unity WebGL 側へメッセージバス経由で設定を送信する。
 */
export function useViewerSettings(bus: UnityMessageBus) {
  const bgColor = ref({ r: 0, g: 1.0, b: 0.0 })
  const fov = ref(30)

  function setBackgroundColor(r: number, g: number, b: number) {
    bgColor.value = { r, g, b }
    bus.send('background/setColor', { r, g, b })
  }

  function setFoV(value: number) {
    fov.value = value
    bus.send('camera/setFoV', { fov: value })
  }

  function resetCamera() {
    bus.send('camera/reset', {})
  }

  return { bgColor, fov, setBackgroundColor, setFoV, resetCamera }
}

export type ViewerSettings = ReturnType<typeof useViewerSettings>
