import { ref, onUnmounted } from 'vue'
import type { UnityInstance } from '../types/unity'

export type UnityMessageHandler = (payload: unknown) => void

/**
 * Unity WebGL との汎用メッセージバス。
 * すべての JS ↔ Unity 通信はこの Composable を経由する。
 *
 * 送信: send(type, payload)
 *   → SendMessage('WebGLMessageBus', 'OnMessage', json)
 *
 * 受信: on(type, handler)
 *   → CustomEvent 'unity:message' を type でフィルタリングして受信
 *   → 解除関数を返す（onUnmounted で自動解除も可能）
 */
export function useUnityMessageBus() {
  const unityInstance = ref<UnityInstance | null>(null)

  // type → handlers のマップ（複数 handler 登録可能）
  const listeners = new Map<string, Set<UnityMessageHandler>>()

  // 'unity:message' イベントをハンドラーにルーティング
  const globalListener = (event: Event) => {
    const detail = (event as CustomEvent<{ type: string; payload: string }>).detail
    const handlers = listeners.get(detail.type)
    if (!handlers) return
    let parsed: unknown = {}
    try { parsed = JSON.parse(detail.payload) } catch { /* 空 payload は空オブジェクトとして扱う */ }
    handlers.forEach(h => h(parsed))
  }

  window.addEventListener('unity:message', globalListener)
  onUnmounted(() => window.removeEventListener('unity:message', globalListener))

  /**
   * Unity へメッセージを送信する。
   * @param type    メッセージタイプ（例: "vrm/load"）
   * @param payload 任意のオブジェクト（JSON シリアライズ可能なもの）
   */
  function send(type: string, payload: object = {}): void {
    if (!unityInstance.value) return
    const json = JSON.stringify({ type, payload: JSON.stringify(payload) })
    unityInstance.value.SendMessage('WebAppMessageBus', 'OnMessage', json)
  }

  /**
   * Unity からのイベントを購読する。
   * @returns 購読解除関数
   */
  function on(type: string, handler: UnityMessageHandler): () => void {
    if (!listeners.has(type)) listeners.set(type, new Set())
    listeners.get(type)!.add(handler)
    return () => listeners.get(type)?.delete(handler)
  }

  return { unityInstance, send, on }
}

export type UnityMessageBus = ReturnType<typeof useUnityMessageBus>
