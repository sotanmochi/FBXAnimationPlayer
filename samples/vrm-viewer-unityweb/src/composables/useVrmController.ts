import { ref } from 'vue'
import type { UnityMessageBus } from './useUnityMessageBus'

/**
 * VRM モデルの読み込みを担当する Composable。
 * useUnityMessageBus を受け取り、VRM に特化した状態と操作を提供する。
 */
export function useVrmController(bus: UnityMessageBus) {
  const isLoaded  = ref(false)
  const isLoading = ref(false)
  const error     = ref<string | null>(null)

  // Unity → JS: 読み込み完了
  bus.on('vrm/loaded', (payload: any) => {
    isLoading.value = false
    isLoaded.value  = payload?.success === true
    if (!isLoaded.value) error.value = payload?.message ?? 'VRM load failed'
  })

  // Unity → JS: 読み込みエラー
  bus.on('vrm/error', (payload: any) => {
    isLoading.value = false
    error.value     = payload?.message ?? 'Unknown VRM error'
  })

  /** ファイルを選択して Unity へ Blob URL で送信する。 */
  function loadFile(file: File): void {
    isLoading.value = true
    error.value     = null
    const url = URL.createObjectURL(file)
    bus.send('vrm/load', { url })
    // NOTE: URL.revokeObjectURL(url) は vrm/loaded 受信後に行う。
    // Unity が UnityWebRequest で取得完了した後に revoke しないと失敗する。
    // 簡易実装として数秒後に revoke する。
    setTimeout(() => URL.revokeObjectURL(url), 30_000)
  }

  return { isLoaded, isLoading, error, loadFile }
}
